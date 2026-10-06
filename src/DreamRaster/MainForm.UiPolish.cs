/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Interface v38 : alignements Image/Vidéo, cadres visuels cohérents,
     configuration regroupée par catégories et interfaces techniques optionnelles.
EN: v38 UI: aligned Image/Video workspaces, consistent visual frames,
    categorized settings and optional technical interfaces.
*/

using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;

namespace OpenCodeLocalAI;

public partial class MainForm
{
    private bool _uiPolishInitialized;
    private bool _syncingAdvancedConfiguration;
    private bool _transientOpenCodeTabPolish;
    private bool _transientComfyTabPolish;
    private bool _v38VisualHandlersInstalled;

    private Label _dashboardRuntimeInfoPolish = null!;
    private Label _dashboardModelsInfoPolish = null!;

    private Panel _configurationAdvancedPanelPolish = null!;


    private CheckBox _cfgShowOpenCodeTabPolish = null!;
    private CheckBox _cfgShowComfyTabPolish = null!;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (IsWinFormsDesigner())
            return;

        InitializeUiPolishV37();
    }

    // Legacy method name kept for regression-test compatibility; this initializes v38 UI.
    private void InitializeUiPolishV37()
    {
        if (_uiPolishInitialized)
            return;

        _uiPolishInitialized = true;

        RestoreFeatureEditorEnabledStatesPolish();
        InitializeDashboardInfoPolish();
        InitializeAdvancedConfigurationPolish();
        ConfigureV38VisualPolish();
        ApplyTechnicalTabVisibilityPolish();
        ConfigureComfyDarkModePolish();

        // Many Image/Video controls are added after the first theme pass in
        // the constructor. Re-run the recursive theme once the full UI exists.
        AppTheme.ApplyDark(this);
        ForceWhiteButtonText(this);

        RefreshUiPolishTranslations();
        RefreshAdvancedConfigurationPolish();
        RefreshDashboardInfoPolish();
        ApplyUiPolishLayout();

        Resize += (_, _) => ApplyUiPolishLayout();

        _tabs.SelectedIndexChanged += (_, _) =>
        {
            RefreshUiPolishTranslations();

            if (_tabs.SelectedTab == tabDashboard)
                RefreshDashboardInfoPolish();

            if (_tabs.SelectedTab == tabConfiguration)
                RefreshAdvancedConfigurationPolish();

            if (_tabs.SelectedTab != tabOpenCode &&
                _tabs.SelectedTab != tabComfy)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(
                        new Action(RemoveTransientTechnicalTabsPolish));
                }
                else
                {
                    // Unit tests can change tabs before the native window
                    // handle exists. In that case we are already on the UI
                    // thread, so no marshaling is required.
                    RemoveTransientTechnicalTabsPolish();
                }
            }

            tabGenerate.Invalidate();
            _tabVideo.Invalidate();
            tabConfiguration.Invalidate();
            BeginInvokePolishLayout();
        };

        Shown += (_, _) =>
        {
            BeginInvokePolishLayout();
            tabGenerate.Invalidate();
            _tabVideo.Invalidate();
            tabConfiguration.Invalidate();
        };

        btnSettingsSave.Click += (_, _) =>
        {
            RefreshDashboardInfoPolish();
            RefreshAdvancedConfigurationPolish();
        };
    }

    private void BeginInvokePolishLayout()
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        BeginInvoke(new Action(() =>
        {
            if (!IsDisposed)
                ApplyUiPolishLayout();
        }));
    }

    private static void EnableControlTreePolish(Control root)
    {
        foreach (Control child in root.Controls)
        {
            child.Enabled = true;

            // NumericUpDown, TextBox, ComboBox, buttons, etc. may own internal
            // WinForms children. Never mutate those implementation details.
            if (IsGpuWorkspaceInteractiveControl(child))
                continue;

            EnableControlTreePolish(child);
        }
    }

    private void RestoreFeatureEditorEnabledStatesPolish()
    {
        // The v37 designer snapshot contains many explicit Enabled=false values.
        // When the parent tab later becomes available those child values remain
        // false, which makes WinForms render their text with the dark disabled
        // system color. Restore the real runtime state, then re-apply the few
        // controls that are intentionally conditional.
        EnableControlTreePolish(tabGenerate);
        EnableControlTreePolish(_tabVideo);

        if (_comfyStatusPanel is not null)
            EnableControlTreePolish(_comfyStatusPanel);

        _videoCancelButton.Enabled = false;
        _imageCatalogDownloadCancelButton.Enabled = false;
        _videoCatalogDownloadCancelButton.Enabled = false;

        _videoOpenButton.Enabled =
            !string.IsNullOrWhiteSpace(_videoOutput.Text) &&
            File.Exists(_videoOutput.Text);

        UpdateGenerationModeUi();
        UpdateMaximumQualitySharpnessUi();

        _seedInput.Enabled = !_randomSeedCheck.Checked;
        _videoSeed.Enabled = !_videoRandomSeed.Checked;

        UpdatePromptEnhancementState();
        UpdateVideoPromptEnhancementStateV37();
    }

    private void ApplyTechnicalTabVisibilityPolish()
    {
        SetTechnicalTabVisibilityPolish(
            tabOpenCode,
            _s.ShowOpenCodeTab,
            ref _transientOpenCodeTabPolish);

        SetTechnicalTabVisibilityPolish(
            tabComfy,
            _s.ShowComfyUiTab,
            ref _transientComfyTabPolish);
    }

    private void SetTechnicalTabVisibilityPolish(
        TabPage page,
        bool persistent,
        ref bool transient)
    {
        if (persistent)
        {
            transient = false;
            AddMainTabInOrderPolish(page);
            return;
        }

        if (_tabs.SelectedTab == page)
            _tabs.SelectedTab = tabDashboard;

        if (_tabs.TabPages.Contains(page))
            _tabs.TabPages.Remove(page);

        transient = false;
    }

    private void AddMainTabInOrderPolish(TabPage page)
    {
        if (_tabs.TabPages.Contains(page))
            return;

        var order = new[]
        {
            tabDashboard,
            tabGenerate,
            _tabVideo,
            tabConfiguration,
            tabInstallation,
            tabOpenCode,
            tabComfy,
            tabOllama,
            tabLogs,
            tabAbout
        };

        var desiredIndex = Array.IndexOf(order, page);
        if (desiredIndex < 0)
        {
            _tabs.TabPages.Add(page);
            return;
        }

        var insertIndex = 0;
        for (var i = 0; i < desiredIndex; i++)
        {
            if (_tabs.TabPages.Contains(order[i]))
                insertIndex++;
        }

        _tabs.TabPages.Insert(
            Math.Min(insertIndex, _tabs.TabPages.Count),
            page);
    }

    private void EnsureTechnicalTabVisibleForDirectOpenPolish(
        TabPage page)
    {
        AddMainTabInOrderPolish(page);

        if (page == tabOpenCode && !_s.ShowOpenCodeTab)
            _transientOpenCodeTabPolish = true;

        if (page == tabComfy && !_s.ShowComfyUiTab)
            _transientComfyTabPolish = true;
    }

    private void RemoveTransientTechnicalTabsPolish()
    {
        if (_transientOpenCodeTabPolish &&
            _tabs.SelectedTab != tabOpenCode)
        {
            if (_tabs.TabPages.Contains(tabOpenCode))
                _tabs.TabPages.Remove(tabOpenCode);

            _transientOpenCodeTabPolish = false;
        }

        if (_transientComfyTabPolish &&
            _tabs.SelectedTab != tabComfy)
        {
            if (_tabs.TabPages.Contains(tabComfy))
                _tabs.TabPages.Remove(tabComfy);

            _transientComfyTabPolish = false;
        }
    }

    private void ConfigureComfyDarkModePolish()
    {
        _comfyWeb.DefaultBackgroundColor = AppTheme.Background;

        void Apply()
        {
            var core = _comfyWeb.CoreWebView2;
            if (core is null)
                return;

            try
            {
                core.Profile.PreferredColorScheme =
                    CoreWebView2PreferredColorScheme.Dark;

                _ = core.AddScriptToExecuteOnDocumentCreatedAsync(
                    "try{document.documentElement.style.colorScheme='dark';}catch(e){}");
            }
            catch (Exception ex)
            {
                Log(
                    "WebView ⚠",
                    "Thème sombre ComfyUI : " + ex.Message);
            }
        }

        _comfyWeb.CoreWebView2InitializationCompleted += (_, args) =>
        {
            if (args.IsSuccess)
                Apply();
        };

        Apply();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusExPolish
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GlobalMemoryStatusEx",
        CharSet = CharSet.Auto,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusExPolish(
        ref MemoryStatusExPolish buffer);

    private static string FormatBytesPolish(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        var value = (double)bytes;
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        var index = 0;

        while (value >= 1024D && index < units.Length - 1)
        {
            value /= 1024D;
            index++;
        }

        return $"{value:0.#} {units[index]}";
    }

    private static Label CreateDashboardCardPolish(string name)
        => new()
        {
            Name = name,
            BackColor = AppTheme.SurfaceAlt,
            ForeColor = AppTheme.TextMuted,
            BorderStyle = BorderStyle.FixedSingle,
            AutoSize = false,
            AutoEllipsis = true,
            Padding = new Padding(9, 0, 9, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };

    private void InitializeDashboardInfoPolish()
    {
        _dashboardRuntimeInfoPolish =
            CreateDashboardCardPolish("dashboardRuntimeInfo");
        _dashboardModelsInfoPolish =
            CreateDashboardCardPolish("dashboardModelsInfo");

        tabDashboard.Controls.AddRange(
        [
            _dashboardRuntimeInfoPolish,
            _dashboardModelsInfoPolish
        ]);

        _dashboardRuntimeInfoPolish.BringToFront();
        _dashboardModelsInfoPolish.BringToFront();
    }

    private void RefreshDashboardInfoPolish()
    {
        if (_dashboardRuntimeInfoPolish is null ||
            _dashboardModelsInfoPolish is null)
        {
            return;
        }

        var os = RuntimeInformation.OSDescription.Trim();
        var architecture = RuntimeInformation.OSArchitecture;
        var cpu =
            Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
            ?? L10n.Pick(_s.Language, "CPU non identifié", "Unknown CPU");

        var totalRam = 0UL;
        var availableRam = 0UL;
        var memory = new MemoryStatusExPolish
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusExPolish>()
        };

        if (GlobalMemoryStatusExPolish(ref memory))
        {
            totalRam = memory.TotalPhysical;
            availableRam = memory.AvailablePhysical;
        }

        var ramText = totalRam > 0
            ? L10n.Pick(
                _s.Language,
                $"RAM {FormatBytesPolish((long)availableRam)} libre / {FormatBytesPolish((long)totalRam)}",
                $"RAM {FormatBytesPolish((long)availableRam)} free / {FormatBytesPolish((long)totalRam)}")
            : L10n.Pick(
                _s.Language,
                "RAM : information indisponible",
                "RAM: information unavailable");

        _dashboardRuntimeInfoPolish.Text =
            L10n.Pick(
                _s.Language,
                $"Machine · {Environment.MachineName} · {os} · {architecture} · {Environment.ProcessorCount} processeurs logiques · {ramText}",
                $"Machine · {Environment.MachineName} · {os} · {architecture} · {Environment.ProcessorCount} logical processors · {ramText}");

        var storageText =
            L10n.Pick(
                _s.Language,
                "Stockage · information indisponible",
                "Storage · information unavailable");

        try
        {
            var root =
                Path.GetPathRoot(PortablePaths.Root)
                ?? Path.GetPathRoot(AppContext.BaseDirectory);

            if (!string.IsNullOrWhiteSpace(root))
            {
                var drive = new DriveInfo(root);
                if (drive.IsReady)
                {
                    storageText =
                        L10n.Pick(
                            _s.Language,
                            $"Stockage · {drive.Name} · {FormatBytesPolish(drive.AvailableFreeSpace)} libres / {FormatBytesPolish(drive.TotalSize)} · DreamRaster : {PortablePaths.Root}",
                            $"Storage · {drive.Name} · {FormatBytesPolish(drive.AvailableFreeSpace)} free / {FormatBytesPolish(drive.TotalSize)} · DreamRaster: {PortablePaths.Root}");
                }
            }
        }
        catch
        {
        }

        _dashboardModelsInfoPolish.Text = storageText;

        _generationTemplateTips.SetToolTip(
            _dashboardRuntimeInfoPolish,
            _dashboardRuntimeInfoPolish.Text + Environment.NewLine + cpu);
        _generationTemplateTips.SetToolTip(
            _dashboardModelsInfoPolish,
            _dashboardModelsInfoPolish.Text);
    }

    private static Label CreateAdvancedLabelPolish(
        string name,
        string text,
        bool section = false)
        => new()
        {
            Name = name,
            Text = text,
            AutoSize = false,
            BackColor = AppTheme.Background,
            ForeColor = section ? AppTheme.Text : AppTheme.TextMuted,
            Font = section
                ? new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                : new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static NumericUpDown CreateNumericMirrorPolish(
        string name,
        NumericUpDown source)
        => new()
        {
            Name = name,
            Minimum = source.Minimum,
            Maximum = source.Maximum,
            Increment = source.Increment,
            DecimalPlaces = source.DecimalPlaces,
            ThousandsSeparator = source.ThousandsSeparator,
            Value = Math.Clamp(source.Value, source.Minimum, source.Maximum),
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            TextAlign = HorizontalAlignment.Right
        };

    private static ComboBox CreateComboMirrorPolish(
        string name,
        ComboBoxStyle style = ComboBoxStyle.DropDownList)
        => new()
        {
            Name = name,
            DropDownStyle = style,
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            FlatStyle = FlatStyle.Flat
        };

    private static CheckBox CreateCheckMirrorPolish(
        string name,
        string text)
        => new()
        {
            Name = name,
            Text = text,
            AutoSize = false,
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.Text
        };

    private void InitializeAdvancedConfigurationPolish()
    {
        _configurationAdvancedPanelPolish = new Panel
        {
            Name = "configurationAdvancedPanel",
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.Text,
            BorderStyle = BorderStyle.None,
            AutoScroll = false,
            TabStop = false
        };

        _cfgShowOpenCodeTabPolish =
            CreateCheckMirrorPolish(
                "cfgShowOpenCodeTabPolish",
                "Afficher l'onglet OpenCode");
        _cfgShowComfyTabPolish =
            CreateCheckMirrorPolish(
                "cfgShowComfyTabPolish",
                "Afficher l'onglet ComfyUI");

        _configurationAdvancedPanelPolish.Controls.AddRange(
        [
            CreateAdvancedLabelPolish(
                "cfgAdvancedInterfaceTitlePolish",
                "Interfaces techniques",
                section: true),
            _cfgShowOpenCodeTabPolish,
            _cfgShowComfyTabPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedInterfaceNotePolish",
                string.Empty)
        ]);

        _configurationAdvancedPanelPolish.Paint += (_, e) =>
        {
            var rect = _configurationAdvancedPanelPolish.ClientRectangle;
            if (rect.Width <= 2 || rect.Height <= 2)
                return;

            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                rect.Width - 1,
                rect.Height - 1);
        };

        tabConfiguration.Controls.Add(
            _configurationAdvancedPanelPolish);

        lblConfigHint.Visible = false;

        _cfgShowOpenCodeTabPolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.ShowOpenCodeTab =
                _cfgShowOpenCodeTabPolish.Checked;
            SettingsStore.Save(_s);
            ApplyTechnicalTabVisibilityPolish();
        };

        _cfgShowComfyTabPolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.ShowComfyUiTab =
                _cfgShowComfyTabPolish.Checked;
            SettingsStore.Save(_s);
            ApplyTechnicalTabVisibilityPolish();
        };

        _configurationAdvancedPanelPolish.BringToFront();

        WireCompleteGenerationPersistencePolish();
    }

    private void WireCompleteGenerationPersistencePolish()
    {
        void SaveImageExtras()
        {
            if (_loadingSettingsExperience)
                return;

            _s.NegativePrompt =
                _negativePrompt.Text.Trim();
            _s.AutoImprovePrompt =
                _autoImprovePrompt.Checked;

            var promptModel =
                _promptModelCombo.Text.Trim();
            if (!string.IsNullOrWhiteSpace(promptModel))
                _s.PromptModel = promptModel;

            _s.GenerationSeed =
                Decimal.ToInt64(_seedInput.Value);
            _s.UseRandomSeed =
                _randomSeedCheck.Checked;

            SettingsStore.Save(_s);
        }

        _negativePrompt.TextChanged += (_, _) =>
            SaveImageExtras();
        _autoImprovePrompt.CheckedChanged += (_, _) =>
            SaveImageExtras();
        _promptModelCombo.TextChanged += (_, _) =>
            SaveImageExtras();
        _seedInput.ValueChanged += (_, _) =>
            SaveImageExtras();
        _randomSeedCheck.CheckedChanged += (_, _) =>
        {
            _seedInput.Enabled =
                !_randomSeedCheck.Checked;
            SaveImageExtras();
        };
    }

    private void WireNumericMirrorPolish(
        NumericUpDown mirror,
        NumericUpDown source)
    {
        mirror.ValueChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            source.Value = Math.Clamp(
                mirror.Value,
                source.Minimum,
                source.Maximum);

            RefreshDashboardInfoPolish();
        };

        source.ValueChanged += (_, _) =>
            RefreshAdvancedConfigurationPolish();
    }

    private void WireCheckMirrorPolish(
        CheckBox mirror,
        CheckBox source)
    {
        mirror.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            source.Checked = mirror.Checked;
        };

        source.CheckedChanged += (_, _) =>
            RefreshAdvancedConfigurationPolish();
    }

    private static void CopyComboItemsPolish(
        ComboBox source,
        ComboBox target)
    {
        var selectedText = source.Text;

        target.BeginUpdate();
        try
        {
            target.Items.Clear();

            foreach (var item in source.Items.Cast<object>())
            {
                var text = item?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    target.Items.Add(text);
            }
        }
        finally
        {
            target.EndUpdate();
        }

        if (target.DropDownStyle == ComboBoxStyle.DropDownList)
        {
            var match = target.Items.Cast<object>().FirstOrDefault(
                item => string.Equals(
                    item?.ToString(),
                    selectedText,
                    StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                target.SelectedItem = match;
        }
        else
        {
            target.Text = selectedText;
        }
    }

    private void RefreshAdvancedConfigurationPolish()
    {
        if (_configurationAdvancedPanelPolish is null ||
            _syncingAdvancedConfiguration)
        {
            return;
        }

        _syncingAdvancedConfiguration = true;
        try
        {
            _cfgShowOpenCodeTabPolish.Checked =
                _s.ShowOpenCodeTab;
            _cfgShowComfyTabPolish.Checked =
                _s.ShowComfyUiTab;
        }
        finally
        {
            _syncingAdvancedConfiguration = false;
        }
    }

    private Control AdvancedControlPolish(string name)
        => _configurationAdvancedPanelPolish.Controls
               .Find(name, searchAllChildren: false)
               .FirstOrDefault()
           ?? throw new InvalidOperationException(
               "Contrôle de configuration introuvable : " + name);

    private void RefreshUiPolishTranslations()
    {
        if (!_uiPolishInitialized)
            return;

        var english = L10n.IsEnglish(_s.Language);

        // Short labels line up identically on Image and Video.
        lblCfgWidth.Text = english ? "Width" : "Largeur";
        lblCfgHeight.Text = english ? "Height" : "Hauteur";
        lblCfgSteps.Text = "Steps";
        _imageCfgLabel.Text = "CFG";

        if (_configurationAdvancedPanelPolish is null)
            return;

        ((Label)AdvancedControlPolish(
            "cfgAdvancedInterfaceTitlePolish")).Text =
            english
                ? "Advanced interfaces"
                : "Interfaces techniques";

        _cfgShowOpenCodeTabPolish.Text =
            english
                ? "Show OpenCode tab"
                : "Afficher l'onglet OpenCode";
        _cfgShowComfyTabPolish.Text =
            english
                ? "Show ComfyUI tab"
                : "Afficher l'onglet ComfyUI";

        chkHardStopComfy.Text =
            english
                ? "Stop ComfyUI after generation"
                : "Arrêter ComfyUI après génération";
        chkAutoUpdates.Text =
            english
                ? "Automatic GitHub updates"
                : "Mises à jour GitHub automatiques";
        chkInstallVisionModel.Text =
            english
                ? "Install Qwen3-VL (optional)"
                : "Installer Qwen3-VL (optionnel)";
        _autoSaveConfigurationCheck.Text =
            english
                ? "Automatic save"
                : "Sauvegarde automatique";

        ((Label)AdvancedControlPolish(
            "cfgAdvancedInterfaceNotePolish")).Text =
            english
                ? "Image and Video settings are edited only in their own tabs and are saved automatically. OpenCode and ComfyUI are optional advanced/diagnostic interfaces."
                : "Les paramètres Image et Vidéo sont modifiés uniquement dans leurs onglets et enregistrés automatiquement. OpenCode et ComfyUI sont des interfaces avancées/diagnostic optionnelles.";

        tabGenerate.Invalidate();
        _tabVideo.Invalidate();
        tabConfiguration.Invalidate();
    }

    private void ConfigureV38VisualPolish()
    {
        if (_v38VisualHandlersInstalled)
            return;

        _v38VisualHandlersInstalled = true;

        tabGenerate.Paint += (_, e) =>
            DrawImageFramesV38(e.Graphics);
        _tabVideo.Paint += (_, e) =>
            DrawVideoFramesV38(e.Graphics);
        tabConfiguration.Paint += (_, e) =>
            DrawConfigurationFramesV38(e.Graphics);

        foreach (var numeric in new NumericUpDown[]
                 {
                     numDefaultWidth,
                     numDefaultHeight,
                     numDefaultSteps,
                     _imageCfg,
                     _seedInput,
                     numImg2ImgStrength,
                     _imageLoraStrength,
                     _videoWidth,
                     _videoHeight,
                     _videoDurationSeconds,
                     _videoFrames,
                     _videoFps,
                     _videoSteps,
                     _videoCfg,
                     _videoSamplingShift,
                     _videoSeed,
                     _videoLoraStrength,
                     numOpenCodePort,
                     numOllamaPort,
                     numComfyPort,
                     numProxyPort,
                     numApiPort,
                     numSafeVram,
                     numSafeRam,
                     numDownloadConnections,
                     numDownloadBuffer
                 })
        {
            numeric.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (var text in new TextBoxBase[]
                 {
                     _prompt,
                     _negativePrompt,
                     txtInputImage,
                     _videoPrompt,
                     _videoNegative,
                     _videoReferenceImage,
                     _videoOutput,
                     txtConfigRoot,
                     txtGitHubRepo,
                     _cfgVideoModel,
                     _cfgVideoTextEncoder,
                     _cfgVideoVae,
                     _cfgVideoClipVision
                 })
        {
            text.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (var combo in new ComboBox[]
                 {
                     cmbGenerationMode,
                     _promptModelCombo,
                     _imageModelRuntimeCombo,
                     _imageStyleTemplateCombo,
                     _imageNegativeTemplateCombo,
                     _imageLoraCombo,
                     _videoModelRuntimeCombo,
                     _videoQualityCombo,
                     _videoStyleTemplateCombo,
                     _videoNegativeTemplateCombo,
                     _videoLoraCombo,
                     _videoSampler,
                     _videoScheduler,
                     cmbLanguage,
                     _visionModelCombo,
                     _fluxModelCombo,
                     _textEncoderCombo,
                     _vaeCombo
                 })
        {
            combo.FlatStyle = FlatStyle.Flat;
        }

        _configurationSaveStatus.AutoEllipsis = true;
    }

    private static Rectangle BoundsOfV38(
        params Control[] controls)
    {
        var visible = controls
            .Where(control =>
                control is not null &&
                control.Visible &&
                control.Width > 0 &&
                control.Height > 0)
            .ToArray();

        if (visible.Length == 0)
            return Rectangle.Empty;

        var rect = visible[0].Bounds;
        foreach (var control in visible.Skip(1))
            rect = Rectangle.Union(rect, control.Bounds);

        return rect;
    }

    private static Rectangle InflateFrameV38(
        Rectangle bounds,
        int horizontal = 8,
        int vertical = 7)
    {
        if (bounds.IsEmpty)
            return bounds;

        bounds.Inflate(horizontal, vertical);
        return bounds;
    }

    private static void DrawFrameV38(
        Graphics graphics,
        Rectangle bounds)
    {
        if (bounds.IsEmpty ||
            bounds.Width <= 2 ||
            bounds.Height <= 2)
        {
            return;
        }

        using var pen = new Pen(AppTheme.Border);
        graphics.DrawRectangle(
            pen,
            bounds.X,
            bounds.Y,
            bounds.Width - 1,
            bounds.Height - 1);
    }

    private static void DrawTitledFrameV38(
        Graphics graphics,
        Rectangle bounds,
        string title)
    {
        DrawFrameV38(graphics, bounds);

        if (string.IsNullOrWhiteSpace(title))
            return;

        using var font = new Font(
            "Segoe UI Semibold",
            9F,
            FontStyle.Bold);
        using var brush = new SolidBrush(AppTheme.TextMuted);

        graphics.DrawString(
            title,
            font,
            brush,
            bounds.X + 10,
            bounds.Y + 6);
    }

    private void DrawImageFramesV38(Graphics graphics)
    {
        var leftFrame = InflateFrameV38(
            BoundsOfV38(
                lblPrompt,
                _improvePromptButton,
                _autoImprovePrompt,
                _promptModelLabel,
                _promptModelCombo,
                _prompt,
                _negativePromptLabel,
                _negativePrompt,
                lblGenerationMode,
                cmbGenerationMode,
                lblInputImage,
                txtInputImage,
                btnBrowseInputImage,
                btnClearInputImage,
                lblImg2ImgStrength,
                numImg2ImgStrength,
                _seedLabel,
                _seedInput,
                _randomSeedCheck,
                _imageExtractPromptButton,
                lblCfgWidth,
                numDefaultWidth,
                lblCfgHeight,
                numDefaultHeight,
                lblCfgSteps,
                numDefaultSteps,
                _imageCfgLabel,
                _imageCfg,
                btnGenerate,
                _benchmarkButton,
                _genText,
                _genProgress));

        var catalogFrame = InflateFrameV38(
            BoundsOfV38(
                _imageModelRuntimeLabel,
                _imageModelRuntimeCombo,
                _imageModelDownloadButton,
                _imageImportModelButton,
                _imageStyleTemplateLabel,
                _imageStyleTemplateCombo,
                _imageNegativeTemplateLabel,
                _imageNegativeTemplateCombo,
                _imageLoraLabel,
                _imageLoraCombo,
                _imageLoraStrength,
                _imageLoraDownloadButton,
                _imageLoraAddButton,
                _imageCatalogDownloadStatus,
                _imageCatalogDownloadProgress,
                _imageCatalogDownloadSize,
                _imageCatalogDownloadCancelButton));

        DrawFrameV38(graphics, leftFrame);
        DrawFrameV38(graphics, catalogFrame);
        DrawFrameV38(
            graphics,
            InflateFrameV38(
                _imagePreviewViewport.Bounds,
                3,
                3));
        DrawFrameV38(
            graphics,
            InflateFrameV38(
                BoundsOfV38(
                    _imageHistoryLabel,
                    _imageHistoryPanel),
                6,
                5));
    }

    private void DrawVideoFramesV38(Graphics graphics)
    {
        var leftFrame = InflateFrameV38(
            BoundsOfV38(
                _videoPromptLabel,
                _videoImprovePromptButton,
                _videoAutoImprovePrompt,
                _videoPrompt,
                _videoNegativeLabel,
                _videoNegative,
                _videoReferenceLabel,
                _videoReferenceImage,
                _videoReferenceBrowseButton,
                _videoReferenceCropButton,
                _videoExtractPromptButton,
                _videoReferenceHint,
                _videoWidthLabel,
                _videoWidth,
                _videoHeightLabel,
                _videoHeight,
                _videoDurationLabel,
                _videoDurationSeconds,
                _videoFramesLabel,
                _videoFrames,
                _videoFpsLabel,
                _videoFps,
                _videoStepsLabel,
                _videoSteps,
                _videoCfgLabel,
                _videoCfg,
                _videoShiftLabel,
                _videoSamplingShift,
                _videoSamplerLabel,
                _videoSampler,
                _videoSchedulerLabel,
                _videoScheduler,
                _videoSeedLabel,
                _videoSeed,
                _videoRandomSeed,
                _videoQualityHint,
                _videoGenerateButton,
                _videoCancelButton,
                _videoRefreshButton,
                _videoStatus,
                _videoProgress));

        var catalogFrame = InflateFrameV38(
            BoundsOfV38(
                _videoModelRuntimeLabel,
                _videoModelRuntimeCombo,
                _videoModelDownloadButton,
                _videoImportModelButton,
                _videoQualityLabel,
                _videoQualityCombo,
                _videoStyleTemplateLabel,
                _videoStyleTemplateCombo,
                _videoNegativeTemplateLabel,
                _videoNegativeTemplateCombo,
                _videoLoraLabel,
                _videoLoraCombo,
                _videoLoraStrength,
                _videoLoraDownloadButton,
                _videoLoraAddButton,
                _videoCatalogDownloadStatus,
                _videoCatalogDownloadProgress,
                _videoCatalogDownloadSize,
                _videoCatalogDownloadCancelButton));

        DrawFrameV38(graphics, leftFrame);
        DrawFrameV38(graphics, catalogFrame);
        DrawFrameV38(
            graphics,
            InflateFrameV38(
                _videoPreviewWeb.Bounds,
                3,
                3));
        DrawFrameV38(
            graphics,
            InflateFrameV38(
                BoundsOfV38(
                    _videoOutputLabel,
                    _videoOutput,
                    _videoOpenButton),
                6,
                5));
        DrawFrameV38(
            graphics,
            InflateFrameV38(
                BoundsOfV38(
                    _videoHistoryLabel,
                    _videoHistoryPanel),
                6,
                5));
    }

    private (
        Rectangle Root,
        Rectangle Services,
        Rectangle ImageModels,
        Rectangle Resources,
        Rectangle VideoModels,
        Rectangle Preferences,
        Rectangle Technical)
        GetConfigurationCardsV38()
    {
        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var contentWidth = Math.Max(500, width - 36);
        const int gap = 12;
        var columnWidth =
            Math.Max(260, (contentWidth - gap) / 2);

        const int left = 18;
        var right = left + columnWidth + gap;

        return (
            new Rectangle(
                left,
                72,
                contentWidth,
                58),
            new Rectangle(
                left,
                136,
                columnWidth,
                142),
            new Rectangle(
                right,
                136,
                columnWidth,
                142),
            new Rectangle(
                left,
                284,
                columnWidth,
                142),
            new Rectangle(
                right,
                284,
                columnWidth,
                142),
            new Rectangle(
                left,
                432,
                columnWidth,
                130),
            new Rectangle(
                right,
                432,
                columnWidth,
                130));
    }

    private void DrawConfigurationFramesV38(
        Graphics graphics)
    {
        var cards = GetConfigurationCardsV38();

        DrawTitledFrameV38(
            graphics,
            cards.Root,
            L10n.Pick(
                _s.Language,
                "Emplacement portable",
                "Portable location"));
        DrawTitledFrameV38(
            graphics,
            cards.Services,
            L10n.Pick(
                _s.Language,
                "Services locaux",
                "Local services"));
        DrawTitledFrameV38(
            graphics,
            cards.ImageModels,
            L10n.Pick(
                _s.Language,
                "Modèles Image",
                "Image models"));
        DrawTitledFrameV38(
            graphics,
            cards.Resources,
            L10n.Pick(
                _s.Language,
                "Ressources et téléchargements",
                "Resources and downloads"));
        DrawTitledFrameV38(
            graphics,
            cards.VideoModels,
            L10n.Pick(
                _s.Language,
                "Modèles Vidéo",
                "Video models"));
        DrawTitledFrameV38(
            graphics,
            cards.Preferences,
            L10n.Pick(
                _s.Language,
                "Préférences et mises à jour",
                "Preferences and updates"));
    }

    private void ApplyUiPolishLayout()
    {
        if (!_uiPolishInitialized ||
            IsDisposed ||
            tabGenerate is null ||
            _tabVideo is null)
        {
            return;
        }

        LayoutDashboardPolish();
        LayoutCatalogHeaderRowsV38();
        LayoutImageGridPolish();
        LayoutVideoGridPolish();
        LayoutAdvancedConfigurationPolish();

        tabGenerate.Invalidate();
        _tabVideo.Invalidate();
        tabConfiguration.Invalidate();
    }

    private void LayoutDashboardPolish()
    {
        if (_dashboardRuntimeInfoPolish is null ||
            _dashboardModelsInfoPolish is null)
        {
            return;
        }

        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var rightX = 260;
        var rightWidth = Math.Max(360, width - rightX - 20);

        _status.SetBounds(
            rightX,
            72,
            rightWidth,
            54);
        _gpu.SetBounds(
            rightX,
            136,
            rightWidth,
            54);

        _dashboardRuntimeInfoPolish.SetBounds(
            rightX,
            200,
            rightWidth,
            36);
        _dashboardModelsInfoPolish.SetBounds(
            rightX,
            242,
            rightWidth,
            36);

        lblLiveLog.SetBounds(
            18,
            287,
            Math.Max(300, width - 36),
            20);
        _liveLog.SetBounds(
            18,
            314,
            Math.Max(400, width - 36),
            Math.Max(120, workspace.Height - 335));

        _liveLog.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;
    }

    private void LayoutCatalogHeaderRowsV38()
    {
        const int rightX = 370;

        var workspace = GetSharedWorkspaceSizeV37();
        var rightWidth = Math.Max(
            500,
            workspace.Width - rightX - 20);

        var compact = rightWidth < 600;

        var modelLabelWidth = compact ? 44 : 48;
        var modelComboWidth = compact ? 142 : 182;
        var downloadWidth = compact ? 88 : 96;
        const int addWidth = 32;
        const int gap = 6;

        var modelComboX =
            rightX + modelLabelWidth;
        var downloadX =
            modelComboX + modelComboWidth + gap;
        var addX =
            downloadX + downloadWidth + gap;

        _imageModelRuntimeLabel.SetBounds(
            rightX,
            17,
            modelLabelWidth,
            26);
        _imageModelRuntimeCombo.SetBounds(
            modelComboX,
            18,
            modelComboWidth,
            25);
        _imageModelDownloadButton.SetBounds(
            downloadX,
            17,
            downloadWidth,
            27);
        _imageImportModelButton.SetBounds(
            addX,
            17,
            addWidth,
            27);

        _videoModelRuntimeLabel.SetBounds(
            rightX,
            17,
            modelLabelWidth,
            26);
        _videoModelRuntimeCombo.SetBounds(
            modelComboX,
            18,
            modelComboWidth,
            25);
        _videoModelDownloadButton.SetBounds(
            downloadX,
            17,
            downloadWidth,
            27);
        _videoImportModelButton.SetBounds(
            addX,
            17,
            addWidth,
            27);

        var qualityLabelX =
            addX + addWidth + 10;
        var qualityLabelWidth =
            compact ? 44 : 50;
        var qualityComboX =
            qualityLabelX + qualityLabelWidth;

        _videoQualityLabel.SetBounds(
            qualityLabelX,
            17,
            qualityLabelWidth,
            26);
        _videoQualityCombo.SetBounds(
            qualityComboX,
            18,
            Math.Max(
                86,
                rightX + rightWidth - qualityComboX),
            25);

        const int styleLabelWidth = 42;
        var styleComboWidth =
            Math.Clamp(
                rightWidth / 2 - 60,
                190,
                260);
        var negativeLabelX =
            rightX +
            styleLabelWidth +
            styleComboWidth +
            14;

        _imageStyleTemplateLabel.SetBounds(
            rightX,
            49,
            styleLabelWidth,
            26);
        _imageStyleTemplateCombo.SetBounds(
            rightX + styleLabelWidth,
            50,
            styleComboWidth,
            25);
        _imageNegativeTemplateLabel.SetBounds(
            negativeLabelX,
            49,
            62,
            26);
        _imageNegativeTemplateCombo.SetBounds(
            negativeLabelX + 62,
            50,
            Math.Max(
                92,
                rightX + rightWidth -
                (negativeLabelX + 62)),
            25);

        _videoStyleTemplateLabel.SetBounds(
            rightX,
            49,
            styleLabelWidth,
            26);
        _videoStyleTemplateCombo.SetBounds(
            rightX + styleLabelWidth,
            50,
            styleComboWidth,
            25);
        _videoNegativeTemplateLabel.SetBounds(
            negativeLabelX,
            49,
            62,
            26);
        _videoNegativeTemplateCombo.SetBounds(
            negativeLabelX + 62,
            50,
            Math.Max(
                92,
                rightX + rightWidth -
                (negativeLabelX + 62)),
            25);

        const int loraLabelWidth = 42;
        var loraComboWidth =
            compact ? 165 : 190;
        const int strengthWidth = 64;
        var loraComboX =
            rightX + loraLabelWidth;
        var strengthX =
            loraComboX + loraComboWidth + gap;
        var loraDownloadX =
            strengthX + strengthWidth + gap;
        var loraDownloadWidth =
            compact ? 96 : 108;
        var loraAddX =
            loraDownloadX +
            loraDownloadWidth +
            gap;

        _imageLoraLabel.SetBounds(
            rightX,
            80,
            loraLabelWidth,
            26);
        _imageLoraCombo.SetBounds(
            loraComboX,
            81,
            loraComboWidth,
            25);
        _imageLoraStrength.SetBounds(
            strengthX,
            81,
            strengthWidth,
            25);
        _imageLoraDownloadButton.SetBounds(
            loraDownloadX,
            80,
            loraDownloadWidth,
            27);
        _imageLoraAddButton.SetBounds(
            loraAddX,
            80,
            addWidth,
            27);

        _videoLoraLabel.SetBounds(
            rightX,
            80,
            loraLabelWidth,
            26);
        _videoLoraCombo.SetBounds(
            loraComboX,
            81,
            loraComboWidth,
            25);
        _videoLoraStrength.SetBounds(
            strengthX,
            81,
            strengthWidth,
            25);
        _videoLoraDownloadButton.SetBounds(
            loraDownloadX,
            80,
            loraDownloadWidth,
            27);
        _videoLoraAddButton.SetBounds(
            loraAddX,
            80,
            addWidth,
            27);
    }

    private void LayoutImageGridPolish()
    {
        var workspace = GetSharedWorkspaceSizeV37();
        var compact = workspace.Height < 600;
        var firstRow = compact ? 393 : 474;
        var secondRow = firstRow + (compact ? 27 : 30);

        lblCfgWidth.SetBounds(18, firstRow + 3, 64, 23);
        numDefaultWidth.SetBounds(86, firstRow, 78, 23);

        lblCfgHeight.SetBounds(174, firstRow + 3, 70, 23);
        numDefaultHeight.SetBounds(246, firstRow, 102, 23);

        lblCfgSteps.SetBounds(18, secondRow + 3, 64, 23);
        numDefaultSteps.SetBounds(86, secondRow, 78, 23);

        _imageCfgLabel.SetBounds(174, secondRow + 3, 70, 23);
        _imageCfg.SetBounds(246, secondRow, 102, 23);

        foreach (var label in new[]
                 {
                     lblCfgWidth,
                     lblCfgHeight,
                     lblCfgSteps,
                     _imageCfgLabel
                 })
        {
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
        }
    }

    private void LayoutVideoGridPolish()
    {
        var workspace = GetSharedWorkspaceSizeV37();
        var compact = workspace.Height < 600;
        var rowStart = compact ? 276 : 303;
        var rowStep = compact ? 28 : 35;

        var row0 = rowStart;
        var row1 = rowStart + rowStep;
        var row2 = rowStart + (rowStep * 2);
        var row3 = rowStart + (rowStep * 3);
        var row4 = rowStart + (rowStep * 4);

        _videoWidthLabel.SetBounds(18, row0 + 4, 64, 23);
        _videoWidth.SetBounds(86, row0, 78, 23);
        _videoHeightLabel.SetBounds(174, row0 + 4, 70, 23);
        _videoHeight.SetBounds(246, row0, 102, 23);

        _videoDurationLabel.SetBounds(18, row1 + 4, 64, 23);
        _videoDurationSeconds.SetBounds(84, row1, 54, 23);
        _videoFramesLabel.SetBounds(142, row1 + 4, 48, 23);
        _videoFrames.SetBounds(192, row1, 52, 23);
        _videoFpsLabel.SetBounds(248, row1 + 4, 30, 23);
        _videoFps.SetBounds(280, row1, 68, 23);

        _videoStepsLabel.SetBounds(18, row2 + 4, 64, 23);
        _videoSteps.SetBounds(86, row2, 78, 23);
        _videoCfgLabel.SetBounds(174, row2 + 4, 70, 23);
        _videoCfg.SetBounds(246, row2, 102, 23);

        _videoShiftLabel.SetBounds(18, row3 + 4, 64, 23);
        _videoSamplingShift.SetBounds(86, row3, 78, 23);
        _videoSamplerLabel.SetBounds(174, row3 + 4, 70, 23);
        _videoSampler.SetBounds(246, row3, 102, 23);

        _videoSchedulerLabel.SetBounds(18, row4 + 4, 64, 23);
        _videoScheduler.SetBounds(86, row4, 78, 23);
        _videoSeedLabel.SetBounds(174, row4 + 4, 70, 23);
        _videoSeed.SetBounds(246, row4, 68, 23);
        _videoRandomSeed.SetBounds(318, row4, 30, 23);
    }

    private void LayoutAdvancedConfigurationPolish()
    {
        if (_configurationAdvancedPanelPolish is null)
            return;

        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var cards = GetConfigurationCardsV38();

        tabConfiguration.AutoScroll = false;

        lblConfigTitle.SetBounds(
            18,
            14,
            390,
            34);

        var saveX =
            Math.Max(
                500,
                width - 350);
        btnSettingsSave.SetBounds(
            saveX,
            14,
            170,
            34);
        btnOpenConfigFolder.SetBounds(
            saveX + 178,
            14,
            154,
            34);

        _configurationSaveStatus.SetBounds(
            18,
            50,
            Math.Max(
                300,
                saveX - 36),
            18);

        lblConfigRootCaption.SetBounds(
            cards.Root.X + 12,
            cards.Root.Y + 28,
            112,
            23);
        txtConfigRoot.SetBounds(
            cards.Root.X + 126,
            cards.Root.Y + 28,
            Math.Max(
                180,
                cards.Root.Width - 138),
            23);

        var serviceLeftLabelX =
            cards.Services.X + 12;
        var serviceLeftInputX =
            cards.Services.X + 108;
        var serviceRightLabelX =
            cards.Services.X +
            Math.Max(
                205,
                cards.Services.Width / 2);
        var serviceRightInputX =
            serviceRightLabelX + 92;
        var serviceInputWidth =
            Math.Max(
                72,
                cards.Services.Right -
                serviceRightInputX -
                12);
        serviceInputWidth =
            Math.Min(
                90,
                serviceInputWidth);

        var serviceY =
            cards.Services.Y + 31;
        const int serviceStep = 29;

        lblCfgOpenCodePort.SetBounds(
            serviceLeftLabelX,
            serviceY + 2,
            92,
            23);
        numOpenCodePort.SetBounds(
            serviceLeftInputX,
            serviceY,
            82,
            23);
        lblCfgOllamaPort.SetBounds(
            serviceRightLabelX,
            serviceY + 2,
            88,
            23);
        numOllamaPort.SetBounds(
            serviceRightInputX,
            serviceY,
            serviceInputWidth,
            23);

        lblCfgComfyPort.SetBounds(
            serviceLeftLabelX,
            serviceY + serviceStep + 2,
            92,
            23);
        numComfyPort.SetBounds(
            serviceLeftInputX,
            serviceY + serviceStep,
            82,
            23);
        lblCfgProxyPort.SetBounds(
            serviceRightLabelX,
            serviceY + serviceStep + 2,
            88,
            23);
        numProxyPort.SetBounds(
            serviceRightInputX,
            serviceY + serviceStep,
            serviceInputWidth,
            23);

        lblCfgApiPort.SetBounds(
            serviceLeftLabelX,
            serviceY + (serviceStep * 2) + 2,
            92,
            23);
        numApiPort.SetBounds(
            serviceLeftInputX,
            serviceY + (serviceStep * 2),
            82,
            23);

        chkHardStopComfy.SetBounds(
            cards.Services.X + 12,
            serviceY + (serviceStep * 3) - 2,
            Math.Max(
                180,
                cards.Services.Width - 24),
            24);

        var imageLabelX =
            cards.ImageModels.X + 12;
        var imageInputX =
            cards.ImageModels.X + 126;
        var imageInputWidth =
            Math.Max(
                120,
                cards.ImageModels.Right -
                imageInputX -
                12);
        var imageY =
            cards.ImageModels.Y + 31;
        const int modelStep = 27;

        lblCfgVisionModel.SetBounds(
            imageLabelX,
            imageY + 2,
            108,
            23);
        _visionModelCombo.SetBounds(
            imageInputX,
            imageY,
            imageInputWidth,
            23);
        lblCfgFluxModel.SetBounds(
            imageLabelX,
            imageY + modelStep + 2,
            108,
            23);
        _fluxModelCombo.SetBounds(
            imageInputX,
            imageY + modelStep,
            imageInputWidth,
            23);
        lblCfgTextEncoder.SetBounds(
            imageLabelX,
            imageY + (modelStep * 2) + 2,
            108,
            23);
        _textEncoderCombo.SetBounds(
            imageInputX,
            imageY + (modelStep * 2),
            imageInputWidth,
            23);
        lblCfgVae.SetBounds(
            imageLabelX,
            imageY + (modelStep * 3) + 2,
            108,
            23);
        _vaeCombo.SetBounds(
            imageInputX,
            imageY + (modelStep * 3),
            imageInputWidth,
            23);

        var resourceLeftLabelX =
            cards.Resources.X + 12;
        var resourceLeftInputX =
            cards.Resources.X + 108;
        var resourceRightLabelX =
            cards.Resources.X +
            Math.Max(
                205,
                cards.Resources.Width / 2);
        var resourceRightInputX =
            resourceRightLabelX + 110;
        var resourceRightWidth =
            Math.Max(
                64,
                cards.Resources.Right -
                resourceRightInputX -
                12);
        var resourceY =
            cards.Resources.Y + 38;

        lblCfgVram.SetBounds(
            resourceLeftLabelX,
            resourceY + 2,
            92,
            23);
        numSafeVram.SetBounds(
            resourceLeftInputX,
            resourceY,
            82,
            23);
        lblCfgRam.SetBounds(
            resourceRightLabelX,
            resourceY + 2,
            106,
            23);
        numSafeRam.SetBounds(
            resourceRightInputX,
            resourceY,
            Math.Min(
                86,
                resourceRightWidth),
            23);

        lblCfgConnections.SetBounds(
            resourceLeftLabelX,
            resourceY + 34,
            122,
            23);
        numDownloadConnections.SetBounds(
            cards.Resources.X + 136,
            resourceY + 32,
            64,
            23);
        lblCfgBuffer.SetBounds(
            resourceRightLabelX,
            resourceY + 34,
            104,
            23);
        numDownloadBuffer.SetBounds(
            resourceRightInputX,
            resourceY + 32,
            Math.Min(
                86,
                resourceRightWidth),
            23);

        var videoLabelX =
            cards.VideoModels.X + 12;
        var videoInputX =
            cards.VideoModels.X + 126;
        var videoInputWidth =
            Math.Max(
                120,
                cards.VideoModels.Right -
                videoInputX -
                12);
        var videoY =
            cards.VideoModels.Y + 31;

        _cfgVideoModelLabel.SetBounds(
            videoLabelX,
            videoY + 2,
            108,
            23);
        _cfgVideoModel.SetBounds(
            videoInputX,
            videoY,
            videoInputWidth,
            23);
        _cfgVideoTextEncoderLabel.SetBounds(
            videoLabelX,
            videoY + modelStep + 2,
            108,
            23);
        _cfgVideoTextEncoder.SetBounds(
            videoInputX,
            videoY + modelStep,
            videoInputWidth,
            23);
        _cfgVideoVaeLabel.SetBounds(
            videoLabelX,
            videoY + (modelStep * 2) + 2,
            108,
            23);
        _cfgVideoVae.SetBounds(
            videoInputX,
            videoY + (modelStep * 2),
            videoInputWidth,
            23);
        _cfgVideoClipVisionLabel.SetBounds(
            videoLabelX,
            videoY + (modelStep * 3) + 2,
            108,
            23);
        _cfgVideoClipVision.SetBounds(
            videoInputX,
            videoY + (modelStep * 3),
            videoInputWidth,
            23);

        var prefY =
            cards.Preferences.Y + 32;
        var prefMid =
            cards.Preferences.X +
            cards.Preferences.Width / 2;

        lblCfgLanguage.SetBounds(
            cards.Preferences.X + 12,
            prefY + 2,
            78,
            23);
        cmbLanguage.SetBounds(
            cards.Preferences.X + 92,
            prefY,
            110,
            23);
        _autoSaveConfigurationCheck.SetBounds(
            prefMid,
            prefY,
            Math.Max(
                150,
                cards.Preferences.Right -
                prefMid -
                12),
            24);

        lblCfgGitHubRepo.SetBounds(
            cards.Preferences.X + 12,
            prefY + 32,
            88,
            23);
        txtGitHubRepo.SetBounds(
            cards.Preferences.X + 102,
            prefY + 32,
            Math.Max(
                150,
                cards.Preferences.Width - 114),
            23);

        chkInstallVisionModel.SetBounds(
            cards.Preferences.X + 12,
            prefY + 64,
            Math.Max(
                170,
                cards.Preferences.Width / 2 - 18),
            24);
        chkAutoUpdates.SetBounds(
            prefMid,
            prefY + 64,
            Math.Max(
                150,
                cards.Preferences.Right -
                prefMid -
                12),
            24);

        _configurationAdvancedPanelPolish.SetBounds(
            cards.Technical.X,
            cards.Technical.Y,
            cards.Technical.Width,
            cards.Technical.Height);

        var technicalWidth =
            cards.Technical.Width;
        AdvancedControlPolish(
                "cfgAdvancedInterfaceTitlePolish")
            .SetBounds(
                12,
                8,
                Math.Max(
                    150,
                    technicalWidth - 24),
                22);

        var techCheckWidth =
            Math.Max(
                150,
                (technicalWidth - 36) / 2);

        _cfgShowOpenCodeTabPolish.SetBounds(
            12,
            36,
            techCheckWidth,
            24);
        _cfgShowComfyTabPolish.SetBounds(
            18 + techCheckWidth,
            36,
            techCheckWidth,
            24);

        AdvancedControlPolish(
                "cfgAdvancedInterfaceNotePolish")
            .SetBounds(
                12,
                66,
                Math.Max(
                    160,
                    technicalWidth - 24),
                50);

        _configurationAdvancedPanelPolish.Invalidate();
        tabConfiguration.Invalidate();
    }
}
