/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Finition visuelle v37 : lisibilité du thème sombre, informations du tableau
     de bord, alignement Image/Vidéo, configuration avancée et onglets techniques.
EN: v37 UI polish: dark-theme readability, dashboard information, aligned
    Image/Video controls, persistent advanced configuration and technical tabs.
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

    private Label _dashboardRuntimeInfoPolish = null!;
    private Label _dashboardModelsInfoPolish = null!;

    private Panel _configurationAdvancedPanelPolish = null!;
    private NumericUpDown _cfgImageWidthPolish = null!;
    private NumericUpDown _cfgImageHeightPolish = null!;
    private NumericUpDown _cfgImageStepsPolish = null!;
    private NumericUpDown _cfgImageCfgPolish = null!;
    private NumericUpDown _cfgImageSharpnessPolish = null!;
    private ComboBox _cfgPromptModelPolish = null!;
    private NumericUpDown _cfgImageSeedPolish = null!;
    private CheckBox _cfgImageRandomSeedPolish = null!;
    private CheckBox _cfgImageAutoImprovePolish = null!;

    private TextBox _cfgVideoI2vModelPolish = null!;
    private NumericUpDown _cfgVideoWidthPolish = null!;
    private NumericUpDown _cfgVideoHeightPolish = null!;
    private NumericUpDown _cfgVideoFramesPolish = null!;
    private NumericUpDown _cfgVideoFpsPolish = null!;
    private NumericUpDown _cfgVideoStepsPolish = null!;
    private NumericUpDown _cfgVideoCfgPolish = null!;
    private NumericUpDown _cfgVideoShiftPolish = null!;
    private NumericUpDown _cfgVideoSharpnessPolish = null!;
    private ComboBox _cfgVideoSamplerPolish = null!;
    private ComboBox _cfgVideoSchedulerPolish = null!;
    private NumericUpDown _cfgVideoSeedPolish = null!;
    private CheckBox _cfgVideoRandomSeedPolish = null!;
    private CheckBox _cfgVideoAutoImprovePolish = null!;

    private CheckBox _cfgShowOpenCodeTabPolish = null!;
    private CheckBox _cfgShowComfyTabPolish = null!;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (IsWinFormsDesigner())
            return;

        InitializeUiPolishV37();
    }

    private void InitializeUiPolishV37()
    {
        if (_uiPolishInitialized)
            return;

        _uiPolishInitialized = true;

        RestoreFeatureEditorEnabledStatesPolish();
        InitializeDashboardInfoPolish();
        InitializeAdvancedConfigurationPolish();
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
                BeginInvoke(new Action(RemoveTransientTechnicalTabsPolish));
            }

            BeginInvokePolishLayout();
        };

        Shown += (_, _) => BeginInvokePolishLayout();

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
        root.Enabled = true;

        foreach (Control child in root.Controls)
            EnableControlTreePolish(child);
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

        var architecture = RuntimeInformation.ProcessArchitecture;

        _dashboardRuntimeInfoPolish.Text =
            L10n.Pick(
                _s.Language,
                $"Runtime · DreamRaster {Application.ProductVersion} · .NET {Environment.Version} · {architecture} · " +
                $"Ports : OpenCode {_s.OpenCodePort} · Ollama {_s.OllamaPort} · ComfyUI {_s.ComfyPort} · API {_s.GenerationApiPort}",
                $"Runtime · DreamRaster {Application.ProductVersion} · .NET {Environment.Version} · {architecture} · " +
                $"Ports: OpenCode {_s.OpenCodePort} · Ollama {_s.OllamaPort} · ComfyUI {_s.ComfyPort} · API {_s.GenerationApiPort}");

        _dashboardModelsInfoPolish.Text =
            L10n.Pick(
                _s.Language,
                $"Image · {Path.GetFileName(_s.FluxModel)} · {_s.DefaultWidth}×{_s.DefaultHeight} · {_s.DefaultSteps} steps · CFG {_s.ImageCfg:0.##}   |   " +
                $"Vidéo · {Path.GetFileName(_s.VideoModel)} · {_s.VideoWidth}×{_s.VideoHeight} · {_s.VideoFrames} frames @ {_s.VideoFps} FPS",
                $"Image · {Path.GetFileName(_s.FluxModel)} · {_s.DefaultWidth}×{_s.DefaultHeight} · {_s.DefaultSteps} steps · CFG {_s.ImageCfg:0.##}   |   " +
                $"Video · {Path.GetFileName(_s.VideoModel)} · {_s.VideoWidth}×{_s.VideoHeight} · {_s.VideoFrames} frames @ {_s.VideoFps} FPS");
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
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true,
            TabStop = false
        };

        _cfgImageWidthPolish =
            CreateNumericMirrorPolish("cfgImageWidthPolish", numDefaultWidth);
        _cfgImageHeightPolish =
            CreateNumericMirrorPolish("cfgImageHeightPolish", numDefaultHeight);
        _cfgImageStepsPolish =
            CreateNumericMirrorPolish("cfgImageStepsPolish", numDefaultSteps);
        _cfgImageCfgPolish =
            CreateNumericMirrorPolish("cfgImageCfgPolish", _imageCfg);
        _cfgImageSharpnessPolish =
            CreateNumericMirrorPolish(
                "cfgImageSharpnessPolish",
                _imageMaxQualitySharpness);
        _cfgImageSeedPolish =
            CreateNumericMirrorPolish("cfgImageSeedPolish", _seedInput);
        _cfgImageRandomSeedPolish =
            CreateCheckMirrorPolish(
                "cfgImageRandomSeedPolish",
                "Seed aléatoire");
        _cfgImageAutoImprovePolish =
            CreateCheckMirrorPolish(
                "cfgImageAutoImprovePolish",
                "Amélioration auto");

        _cfgPromptModelPolish =
            CreateComboMirrorPolish(
                "cfgPromptModelPolish",
                ComboBoxStyle.DropDown);

        _cfgVideoI2vModelPolish = new TextBox
        {
            Name = "cfgVideoI2vModelPolish",
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        _cfgVideoWidthPolish =
            CreateNumericMirrorPolish("cfgVideoWidthPolish", _videoWidth);
        _cfgVideoHeightPolish =
            CreateNumericMirrorPolish("cfgVideoHeightPolish", _videoHeight);
        _cfgVideoFramesPolish =
            CreateNumericMirrorPolish("cfgVideoFramesPolish", _videoFrames);
        _cfgVideoFpsPolish =
            CreateNumericMirrorPolish("cfgVideoFpsPolish", _videoFps);
        _cfgVideoStepsPolish =
            CreateNumericMirrorPolish("cfgVideoStepsPolish", _videoSteps);
        _cfgVideoCfgPolish =
            CreateNumericMirrorPolish("cfgVideoCfgPolish", _videoCfg);
        _cfgVideoShiftPolish =
            CreateNumericMirrorPolish(
                "cfgVideoShiftPolish",
                _videoSamplingShift);
        _cfgVideoSharpnessPolish =
            CreateNumericMirrorPolish(
                "cfgVideoSharpnessPolish",
                _videoMaxQualitySharpness);
        _cfgVideoSeedPolish =
            CreateNumericMirrorPolish("cfgVideoSeedPolish", _videoSeed);
        _cfgVideoRandomSeedPolish =
            CreateCheckMirrorPolish(
                "cfgVideoRandomSeedPolish",
                "Seed aléatoire");
        _cfgVideoAutoImprovePolish =
            CreateCheckMirrorPolish(
                "cfgVideoAutoImprovePolish",
                "Amélioration auto");

        _cfgVideoSamplerPolish =
            CreateComboMirrorPolish("cfgVideoSamplerPolish");
        _cfgVideoSchedulerPolish =
            CreateComboMirrorPolish("cfgVideoSchedulerPolish");

        _cfgShowOpenCodeTabPolish =
            CreateCheckMirrorPolish(
                "cfgShowOpenCodeTabPolish",
                "Afficher l'onglet OpenCode");
        _cfgShowComfyTabPolish =
            CreateCheckMirrorPolish(
                "cfgShowComfyTabPolish",
                "Afficher l'onglet ComfyUI");

        var controls = new Control[]
        {
            CreateAdvancedLabelPolish(
                "cfgAdvancedIntroPolish",
                string.Empty),
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageTitlePolish",
                "Image · génération",
                section: true),
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageWidthLabelPolish",
                "Largeur"),
            _cfgImageWidthPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageHeightLabelPolish",
                "Hauteur"),
            _cfgImageHeightPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageStepsLabelPolish",
                "Steps"),
            _cfgImageStepsPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageCfgLabelPolish",
                "CFG"),
            _cfgImageCfgPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedPromptModelLabelPolish",
                "Modèle prompt"),
            _cfgPromptModelPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageSeedLabelPolish",
                "Seed"),
            _cfgImageSeedPolish,
            _cfgImageRandomSeedPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedImageSharpnessLabelPolish",
                "Netteté max (%)"),
            _cfgImageSharpnessPolish,
            _cfgImageAutoImprovePolish,

            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoTitlePolish",
                "Vidéo · génération",
                section: true),
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoI2vLabelPolish",
                "Modèle Wan I2V"),
            _cfgVideoI2vModelPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoWidthLabelPolish",
                "Largeur"),
            _cfgVideoWidthPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoHeightLabelPolish",
                "Hauteur"),
            _cfgVideoHeightPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoFramesLabelPolish",
                "Frames"),
            _cfgVideoFramesPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoFpsLabelPolish",
                "FPS"),
            _cfgVideoFpsPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoStepsLabelPolish",
                "Steps"),
            _cfgVideoStepsPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoCfgLabelPolish",
                "CFG"),
            _cfgVideoCfgPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoShiftLabelPolish",
                "Shift"),
            _cfgVideoShiftPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoSamplerLabelPolish",
                "Sampler"),
            _cfgVideoSamplerPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoSchedulerLabelPolish",
                "Scheduler"),
            _cfgVideoSchedulerPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoSeedLabelPolish",
                "Seed"),
            _cfgVideoSeedPolish,
            _cfgVideoRandomSeedPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedVideoSharpnessLabelPolish",
                "Netteté max (%)"),
            _cfgVideoSharpnessPolish,
            _cfgVideoAutoImprovePolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedContextNotePolish",
                string.Empty),

            CreateAdvancedLabelPolish(
                "cfgAdvancedInterfaceTitlePolish",
                "Interfaces techniques",
                section: true),
            _cfgShowOpenCodeTabPolish,
            _cfgShowComfyTabPolish,
            CreateAdvancedLabelPolish(
                "cfgAdvancedInterfaceNotePolish",
                string.Empty)
        };

        _configurationAdvancedPanelPolish.Controls.AddRange(controls);
        tabConfiguration.Controls.Add(_configurationAdvancedPanelPolish);

        // The former hint occupied the same lower area. Its content is now
        // retained in the richer introduction inside the scrollable panel.
        lblConfigHint.Visible = false;

        WireNumericMirrorPolish(
            _cfgImageWidthPolish,
            numDefaultWidth);
        WireNumericMirrorPolish(
            _cfgImageHeightPolish,
            numDefaultHeight);
        WireNumericMirrorPolish(
            _cfgImageStepsPolish,
            numDefaultSteps);
        WireNumericMirrorPolish(
            _cfgImageCfgPolish,
            _imageCfg);
        WireNumericMirrorPolish(
            _cfgImageSharpnessPolish,
            _imageMaxQualitySharpness);
        WireNumericMirrorPolish(
            _cfgImageSeedPolish,
            _seedInput);
        WireCheckMirrorPolish(
            _cfgImageRandomSeedPolish,
            _randomSeedCheck);

        WireNumericMirrorPolish(
            _cfgVideoWidthPolish,
            _videoWidth);
        WireNumericMirrorPolish(
            _cfgVideoHeightPolish,
            _videoHeight);
        WireNumericMirrorPolish(
            _cfgVideoFramesPolish,
            _videoFrames);
        WireNumericMirrorPolish(
            _cfgVideoFpsPolish,
            _videoFps);
        WireNumericMirrorPolish(
            _cfgVideoStepsPolish,
            _videoSteps);
        WireNumericMirrorPolish(
            _cfgVideoCfgPolish,
            _videoCfg);
        WireNumericMirrorPolish(
            _cfgVideoShiftPolish,
            _videoSamplingShift);
        WireNumericMirrorPolish(
            _cfgVideoSharpnessPolish,
            _videoMaxQualitySharpness);
        WireNumericMirrorPolish(
            _cfgVideoSeedPolish,
            _videoSeed);
        WireCheckMirrorPolish(
            _cfgVideoRandomSeedPolish,
            _videoRandomSeed);

        _cfgImageAutoImprovePolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _autoImprovePrompt.Checked =
                _cfgImageAutoImprovePolish.Checked;
            _s.AutoImprovePrompt =
                _cfgImageAutoImprovePolish.Checked;
            SettingsStore.Save(_s);
        };

        _autoImprovePrompt.CheckedChanged += (_, _) =>
        {
            if (!_syncingAdvancedConfiguration)
            {
                _s.AutoImprovePrompt = _autoImprovePrompt.Checked;
                SettingsStore.Save(_s);
            }

            RefreshAdvancedConfigurationPolish();
        };

        _seedInput.ValueChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.GenerationSeed = Decimal.ToInt64(_seedInput.Value);
            SettingsStore.Save(_s);
            RefreshAdvancedConfigurationPolish();
        };

        _randomSeedCheck.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.UseRandomSeed = _randomSeedCheck.Checked;
            SettingsStore.Save(_s);
            RefreshAdvancedConfigurationPolish();
        };

        _cfgVideoAutoImprovePolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _videoAutoImprovePrompt.Checked =
                _cfgVideoAutoImprovePolish.Checked;
            _s.VideoAutoImprovePrompt =
                _cfgVideoAutoImprovePolish.Checked;
            SettingsStore.Save(_s);
        };

        _videoAutoImprovePrompt.CheckedChanged += (_, _) =>
        {
            if (!_syncingAdvancedConfiguration)
            {
                _s.VideoAutoImprovePrompt = _videoAutoImprovePrompt.Checked;
                SettingsStore.Save(_s);
            }

            RefreshAdvancedConfigurationPolish();
        };

        _cfgPromptModelPolish.Validated += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            var model = _cfgPromptModelPolish.Text.Trim();
            if (string.IsNullOrWhiteSpace(model))
                return;

            _promptModelCombo.Text = model;
            _s.PromptModel = model;
            SettingsStore.Save(_s);
            RefreshDashboardInfoPolish();
        };

        _promptModelCombo.TextChanged += (_, _) =>
        {
            if (!_syncingAdvancedConfiguration &&
                !string.IsNullOrWhiteSpace(_promptModelCombo.Text))
            {
                _s.PromptModel = _promptModelCombo.Text.Trim();
                SettingsStore.Save(_s);
            }

            RefreshAdvancedConfigurationPolish();
        };

        _cfgVideoI2vModelPolish.Validated += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            try
            {
                var model = _cfgVideoI2vModelPolish.Text.Trim();
                ValidateModelFileName(model, "Modèle Wan I2V");
                _s.VideoI2vModel = model;
                SettingsStore.Save(_s);
            }
            catch (Exception ex)
            {
                _cfgVideoI2vModelPolish.Text = _s.VideoI2vModel;
                MessageBox.Show(
                    this,
                    ex.Message,
                    L10n.Pick(
                        _s.Language,
                        "Configuration vidéo",
                        "Video configuration"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };

        _cfgVideoSamplerPolish.SelectedIndexChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            SelectComboText(
                _videoSampler,
                _cfgVideoSamplerPolish.Text);
        };

        _videoSampler.SelectedIndexChanged += (_, _) =>
            RefreshAdvancedConfigurationPolish();

        _cfgVideoSchedulerPolish.SelectedIndexChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            SelectComboText(
                _videoScheduler,
                _cfgVideoSchedulerPolish.Text);
        };

        _videoScheduler.SelectedIndexChanged += (_, _) =>
            RefreshAdvancedConfigurationPolish();

        _cfgShowOpenCodeTabPolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.ShowOpenCodeTab = _cfgShowOpenCodeTabPolish.Checked;
            SettingsStore.Save(_s);
            ApplyTechnicalTabVisibilityPolish();
        };

        _cfgShowComfyTabPolish.CheckedChanged += (_, _) =>
        {
            if (_syncingAdvancedConfiguration)
                return;

            _s.ShowComfyUiTab = _cfgShowComfyTabPolish.Checked;
            SettingsStore.Save(_s);
            ApplyTechnicalTabVisibilityPolish();
        };

        _configurationAdvancedPanelPolish.BringToFront();
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
            _cfgImageWidthPolish.Value =
                Math.Clamp(
                    numDefaultWidth.Value,
                    _cfgImageWidthPolish.Minimum,
                    _cfgImageWidthPolish.Maximum);
            _cfgImageHeightPolish.Value =
                Math.Clamp(
                    numDefaultHeight.Value,
                    _cfgImageHeightPolish.Minimum,
                    _cfgImageHeightPolish.Maximum);
            _cfgImageStepsPolish.Value =
                Math.Clamp(
                    numDefaultSteps.Value,
                    _cfgImageStepsPolish.Minimum,
                    _cfgImageStepsPolish.Maximum);
            _cfgImageCfgPolish.Value =
                Math.Clamp(
                    _imageCfg.Value,
                    _cfgImageCfgPolish.Minimum,
                    _cfgImageCfgPolish.Maximum);
            _cfgImageSharpnessPolish.Value =
                Math.Clamp(
                    _imageMaxQualitySharpness.Value,
                    _cfgImageSharpnessPolish.Minimum,
                    _cfgImageSharpnessPolish.Maximum);
            _cfgImageSeedPolish.Value =
                Math.Clamp(
                    _seedInput.Value,
                    _cfgImageSeedPolish.Minimum,
                    _cfgImageSeedPolish.Maximum);
            _cfgImageRandomSeedPolish.Checked =
                _randomSeedCheck.Checked;
            _cfgImageAutoImprovePolish.Checked =
                _autoImprovePrompt.Checked;

            CopyComboItemsPolish(
                _promptModelCombo,
                _cfgPromptModelPolish);

            _cfgVideoI2vModelPolish.Text =
                _s.VideoI2vModel;

            _cfgVideoWidthPolish.Value =
                Math.Clamp(
                    _videoWidth.Value,
                    _cfgVideoWidthPolish.Minimum,
                    _cfgVideoWidthPolish.Maximum);
            _cfgVideoHeightPolish.Value =
                Math.Clamp(
                    _videoHeight.Value,
                    _cfgVideoHeightPolish.Minimum,
                    _cfgVideoHeightPolish.Maximum);
            _cfgVideoFramesPolish.Value =
                Math.Clamp(
                    _videoFrames.Value,
                    _cfgVideoFramesPolish.Minimum,
                    _cfgVideoFramesPolish.Maximum);
            _cfgVideoFpsPolish.Value =
                Math.Clamp(
                    _videoFps.Value,
                    _cfgVideoFpsPolish.Minimum,
                    _cfgVideoFpsPolish.Maximum);
            _cfgVideoStepsPolish.Value =
                Math.Clamp(
                    _videoSteps.Value,
                    _cfgVideoStepsPolish.Minimum,
                    _cfgVideoStepsPolish.Maximum);
            _cfgVideoCfgPolish.Value =
                Math.Clamp(
                    _videoCfg.Value,
                    _cfgVideoCfgPolish.Minimum,
                    _cfgVideoCfgPolish.Maximum);
            _cfgVideoShiftPolish.Value =
                Math.Clamp(
                    _videoSamplingShift.Value,
                    _cfgVideoShiftPolish.Minimum,
                    _cfgVideoShiftPolish.Maximum);
            _cfgVideoSharpnessPolish.Value =
                Math.Clamp(
                    _videoMaxQualitySharpness.Value,
                    _cfgVideoSharpnessPolish.Minimum,
                    _cfgVideoSharpnessPolish.Maximum);
            _cfgVideoSeedPolish.Value =
                Math.Clamp(
                    _videoSeed.Value,
                    _cfgVideoSeedPolish.Minimum,
                    _cfgVideoSeedPolish.Maximum);
            _cfgVideoRandomSeedPolish.Checked =
                _videoRandomSeed.Checked;
            _cfgVideoAutoImprovePolish.Checked =
                _videoAutoImprovePrompt.Checked;

            CopyComboItemsPolish(
                _videoSampler,
                _cfgVideoSamplerPolish);
            CopyComboItemsPolish(
                _videoScheduler,
                _cfgVideoSchedulerPolish);

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
            "cfgAdvancedIntroPolish")).Text =
            english
                ? "Persistent generation defaults added in v37. Styles, negative templates and LoRA choices remain directly in the Image/Video tabs."
                : "Réglages de génération persistants ajoutés en v37. Les styles, négatifs et LoRA restent directement dans les onglets Image/Vidéo.";

        ((Label)AdvancedControlPolish(
            "cfgAdvancedImageTitlePolish")).Text =
            english ? "Image · generation" : "Image · génération";
        ((Label)AdvancedControlPolish(
            "cfgAdvancedPromptModelLabelPolish")).Text =
            english ? "Prompt model" : "Modèle prompt";
        ((Label)AdvancedControlPolish(
            "cfgAdvancedImageSharpnessLabelPolish")).Text =
            english ? "Max sharpness (%)" : "Netteté max (%)";
        _cfgImageRandomSeedPolish.Text =
            english ? "Random seed" : "Seed aléatoire";
        _cfgImageAutoImprovePolish.Text =
            english ? "Auto enhancement" : "Amélioration auto";

        ((Label)AdvancedControlPolish(
            "cfgAdvancedVideoTitlePolish")).Text =
            english ? "Video · generation" : "Vidéo · génération";
        ((Label)AdvancedControlPolish(
            "cfgAdvancedVideoI2vLabelPolish")).Text =
            english ? "Wan I2V model" : "Modèle Wan I2V";
        ((Label)AdvancedControlPolish(
            "cfgAdvancedVideoSharpnessLabelPolish")).Text =
            english ? "Max sharpness (%)" : "Netteté max (%)";
        _cfgVideoRandomSeedPolish.Text =
            english ? "Random seed" : "Seed aléatoire";
        _cfgVideoAutoImprovePolish.Text =
            english ? "Auto enhancement" : "Amélioration auto";

        ((Label)AdvancedControlPolish(
            "cfgAdvancedContextNotePolish")).Text =
            english
                ? "Creative presets, negative templates, runtime model selection and LoRA choices stay contextual in Image/Video and are saved automatically."
                : "Les presets créatifs, négatifs, modèles runtime et LoRA restent contextuels dans Image/Vidéo et sont enregistrés automatiquement.";

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

        ((Label)AdvancedControlPolish(
            "cfgAdvancedInterfaceNotePolish")).Text =
            english
                ? "These tabs are not required for Image/Video generation. They stay hidden by default; Dashboard buttons can still open them temporarily for advanced use or diagnostics."
                : "Ces onglets ne sont pas requis pour générer Image/Vidéo. Ils sont masqués par défaut ; les boutons du Tableau de bord peuvent toujours les ouvrir temporairement pour un usage avancé ou le diagnostic.";
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
        LayoutImageGridPolish();
        LayoutVideoGridPolish();
        LayoutAdvancedConfigurationPolish();
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
        var panelY = 473;
        var panelWidth = Math.Max(500, width - 36);
        var panelHeight = Math.Max(92, workspace.Height - panelY - 12);

        _configurationAdvancedPanelPolish.SetBounds(
            18,
            panelY,
            panelWidth,
            panelHeight);
        _configurationAdvancedPanelPolish.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;

        var contentWidth = Math.Max(790, panelWidth - 22);
        _configurationAdvancedPanelPolish.AutoScrollMinSize =
            new Size(contentWidth, 640);

        AdvancedControlPolish("cfgAdvancedIntroPolish")
            .SetBounds(10, 8, contentWidth - 20, 42);

        AdvancedControlPolish("cfgAdvancedImageTitlePolish")
            .SetBounds(10, 58, contentWidth - 20, 24);

        AdvancedControlPolish("cfgAdvancedImageWidthLabelPolish")
            .SetBounds(10, 88, 58, 23);
        _cfgImageWidthPolish.SetBounds(72, 88, 78, 23);

        AdvancedControlPolish("cfgAdvancedImageHeightLabelPolish")
            .SetBounds(168, 88, 60, 23);
        _cfgImageHeightPolish.SetBounds(232, 88, 78, 23);

        AdvancedControlPolish("cfgAdvancedImageStepsLabelPolish")
            .SetBounds(328, 88, 44, 23);
        _cfgImageStepsPolish.SetBounds(376, 88, 72, 23);

        AdvancedControlPolish("cfgAdvancedImageCfgLabelPolish")
            .SetBounds(466, 88, 30, 23);
        _cfgImageCfgPolish.SetBounds(500, 88, 72, 23);

        AdvancedControlPolish("cfgAdvancedPromptModelLabelPolish")
            .SetBounds(10, 120, 96, 23);
        _cfgPromptModelPolish.SetBounds(
            110,
            120,
            Math.Min(380, contentWidth - 120),
            23);

        AdvancedControlPolish("cfgAdvancedImageSeedLabelPolish")
            .SetBounds(10, 152, 58, 23);
        _cfgImageSeedPolish.SetBounds(72, 152, 120, 23);
        _cfgImageRandomSeedPolish.SetBounds(202, 152, 130, 23);

        AdvancedControlPolish("cfgAdvancedImageSharpnessLabelPolish")
            .SetBounds(350, 152, 116, 23);
        _cfgImageSharpnessPolish.SetBounds(470, 152, 76, 23);
        _cfgImageAutoImprovePolish.SetBounds(560, 152, 170, 23);

        AdvancedControlPolish("cfgAdvancedVideoTitlePolish")
            .SetBounds(10, 194, contentWidth - 20, 24);

        AdvancedControlPolish("cfgAdvancedVideoI2vLabelPolish")
            .SetBounds(10, 224, 96, 23);
        _cfgVideoI2vModelPolish.SetBounds(
            110,
            224,
            Math.Max(260, contentWidth - 120),
            23);

        AdvancedControlPolish("cfgAdvancedVideoWidthLabelPolish")
            .SetBounds(10, 256, 58, 23);
        _cfgVideoWidthPolish.SetBounds(72, 256, 78, 23);

        AdvancedControlPolish("cfgAdvancedVideoHeightLabelPolish")
            .SetBounds(168, 256, 60, 23);
        _cfgVideoHeightPolish.SetBounds(232, 256, 78, 23);

        AdvancedControlPolish("cfgAdvancedVideoFramesLabelPolish")
            .SetBounds(328, 256, 48, 23);
        _cfgVideoFramesPolish.SetBounds(380, 256, 68, 23);

        AdvancedControlPolish("cfgAdvancedVideoFpsLabelPolish")
            .SetBounds(466, 256, 30, 23);
        _cfgVideoFpsPolish.SetBounds(500, 256, 72, 23);

        AdvancedControlPolish("cfgAdvancedVideoStepsLabelPolish")
            .SetBounds(10, 288, 58, 23);
        _cfgVideoStepsPolish.SetBounds(72, 288, 78, 23);

        AdvancedControlPolish("cfgAdvancedVideoCfgLabelPolish")
            .SetBounds(168, 288, 60, 23);
        _cfgVideoCfgPolish.SetBounds(232, 288, 78, 23);

        AdvancedControlPolish("cfgAdvancedVideoShiftLabelPolish")
            .SetBounds(328, 288, 48, 23);
        _cfgVideoShiftPolish.SetBounds(380, 288, 68, 23);

        AdvancedControlPolish("cfgAdvancedVideoSamplerLabelPolish")
            .SetBounds(466, 288, 60, 23);
        _cfgVideoSamplerPolish.SetBounds(530, 288, 150, 23);

        AdvancedControlPolish("cfgAdvancedVideoSchedulerLabelPolish")
            .SetBounds(10, 320, 76, 23);
        _cfgVideoSchedulerPolish.SetBounds(90, 320, 150, 23);

        AdvancedControlPolish("cfgAdvancedVideoSeedLabelPolish")
            .SetBounds(258, 320, 48, 23);
        _cfgVideoSeedPolish.SetBounds(310, 320, 118, 23);
        _cfgVideoRandomSeedPolish.SetBounds(438, 320, 130, 23);

        AdvancedControlPolish("cfgAdvancedVideoSharpnessLabelPolish")
            .SetBounds(10, 352, 116, 23);
        _cfgVideoSharpnessPolish.SetBounds(130, 352, 76, 23);
        _cfgVideoAutoImprovePolish.SetBounds(220, 352, 170, 23);

        AdvancedControlPolish("cfgAdvancedContextNotePolish")
            .SetBounds(10, 394, contentWidth - 20, 58);

        AdvancedControlPolish("cfgAdvancedInterfaceTitlePolish")
            .SetBounds(10, 464, contentWidth - 20, 24);
        _cfgShowOpenCodeTabPolish.SetBounds(
            10,
            494,
            230,
            24);
        _cfgShowComfyTabPolish.SetBounds(
            258,
            494,
            230,
            24);
        AdvancedControlPolish("cfgAdvancedInterfaceNotePolish")
            .SetBounds(10, 526, contentWidth - 20, 62);
    }
}
