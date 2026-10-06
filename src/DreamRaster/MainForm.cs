/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Logique principale de l'interface, services, traduction et mise à jour.
EN: Main UI logic, services, localization and updating.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;

using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
namespace OpenCodeLocalAI;

public partial class MainForm : Form
{
    private readonly AppSettings _s = null!;
    private readonly ManagedProcess _openCode = null!;
    private readonly ManagedProcess _ollama = null!;
    private readonly ManagedProcess _comfy = null!;
    private readonly PortableInstaller _installer = null!;
    private readonly LocalProxyServer _proxy = null!;
    private readonly GenerationApiServer _api = null!;
    private readonly Flux2Generator _generator = null!;
    private readonly QualityPostProcessor _qualityPostProcessor = null!;
    private string? _lastMaximumQualityImageFirstPassPath;
    private string? _lastMaximumQualityVideoFirstPassPath;
    private bool _draggingImageSharpnessDivider;
    private readonly GitHubUpdater _updater = null!;

    private CancellationTokenSource? _installCts;
    private Task? _installTask;
    private readonly SemaphoreSlim _logFileGate = new(1, 1);
    private readonly ConcurrentQueue<(string Source, string Message)> _pendingLogs = new();
    private readonly System.Windows.Forms.Timer _logFlushTimer = new();
    private readonly SemaphoreSlim _gpuWorkflowGate = new(1, 1);
    private int _logFlushBusy;
    private int _serviceLifecycleEpoch;
    private bool _closing;

    public MainForm()
    {
        InitializeComponent();

        if (IsWinFormsDesigner())
        {
            _s = new AppSettings();
            return;
        }

        // The Designer uses standard tabs so all pages remain readable/selectable.
        // Runtime restores DreamRaster's custom owner-drawn tab appearance.
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;

        AppTheme.ApplyDark(this);

        _logFlushTimer.Interval = 100;
        _logFlushTimer.Tick += (_, _) => FlushPendingLogs();
        _logFlushTimer.Start();

        PortablePaths.EnsureLayout();
        _s = SettingsStore.Load();
        _updater = new GitHubUpdater(_s);

        InitializeEnhancedUi();
        LoadSettingsToUi();
        ApplyTranslations();
        InitializeAbout();

        cmbGenerationMode.SelectedIndex = 0;
        UpdateGenerationModeUi();

        _openCode = new ManagedProcess("OpenCode", Log);
        _ollama = new ManagedProcess("Ollama", Log);
        _comfy = new ManagedProcess("ComfyUI", Log);

        _installer = new PortableInstaller(_s, Log);
        _installer.ProgressChanged += (p, t) => Ui(() =>
        {
            _installProgress.Value = Math.Clamp(p, 0, 100);
            _installText.Text = t;
        });

        _generator = new Flux2Generator(
            _s,
            StartComfyAsync,
            () => _comfy.StopAsync(),
            StopVisionModelAsync,
            Log);

        _qualityPostProcessor =
            new QualityPostProcessor(
                _s,
                Log);

        _generator.ProgressChanged += (p, t) => Ui(() =>
        {
            _genProgress.Value = Math.Clamp(p, 0, 100);
            _genText.Text = t;
        });

        _proxy = new LocalProxyServer(
            _s.ImageProxyPort,
            _s.OpenCodePort,
            PortablePaths.Resolve(_s.Images),
            Log);

        _api = new GenerationApiServer(
            _s.GenerationApiPort,
            (prompt, width, height, ct) =>
                RunGpuExclusiveAsync(
                    "API Image",
                    () => _generator.GenerateAsync(prompt, width, height, ct)),
            Log);

        Log("UI", $"{BrandInfo.ProductName} v{Application.ProductVersion}");
        Log("UI", "Racine portable : " + PortablePaths.Root);
        Log("UI", "Configuration : " + Path.Combine(PortablePaths.ConfigDir, "settings.json"));
    }

    private static bool IsWinFormsDesigner()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return true;

        var processName = Process.GetCurrentProcess().ProcessName;
        return processName.Equals(
                   "devenv",
                   StringComparison.OrdinalIgnoreCase) ||
               processName.Contains(
                   "DesignToolsServer",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        if (IsWinFormsDesigner())
            return;

        await SafeUiAsync(
            L10n.Pick(_s.Language, "Vérification portable", "Portable check"),
            StartupPreflightAsync);

        FixV36Layout();
        ApplyV37SharedLayout();
        RefreshFeatureAvailability();
        RefreshImageHistory();
        RefreshVideoHistory();
        LogVirtualMemoryWarningIfNeeded();

        await RefreshModelChoicesAsync();

        if (_s.AutoCheckUpdates && !_closing)
        {
            await SafeUiAsync(
                L10n.T(_s.Language, "msg.update"),
                () => CheckForUpdatesAsync(interactive: false));
        }
    }

    private async void btnStart_Click(object? sender, EventArgs e)
        => await SafeUiAsync("Démarrage", StartCoreAsync);

    private async void btnStop_Click(object? sender, EventArgs e)
        => await SafeUiAsync("Arrêt", StopAllAsync);

    private async void btnOpenCode_Click(object? sender, EventArgs e)
        => await SafeUiAsync("OpenCode", OpenEmbeddedAsync);

    private async void btnComfy_Click(object? sender, EventArgs e)
        => await SafeUiAsync("ComfyUI", OpenComfyEmbeddedAsync);

    private async void btnDiagnostic_Click(object? sender, EventArgs e)
        => await SafeUiAsync("Diagnostic", ExportDiagnosticAsync);

    private async void btnGenerate_Click(object? sender, EventArgs e)
        => await SafeUiAsync(
            "Génération FLUX.2",
            () => RunGpuExclusiveAsync("Image", GenerateFromUiAsync));

    private void cmbGenerationMode_SelectedIndexChanged(object? sender, EventArgs e)
        => UpdateGenerationModeUi();

    private void btnBrowseInputImage_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = L10n.IsEnglish(_s.Language)
                ? "Select a source image"
                : "Sélectionner une image source",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        txtInputImage.Text = ofd.FileName;
        ShowPreviewImage(ofd.FileName);
    }

    private void btnClearInputImage_Click(object? sender, EventArgs e)
        => txtInputImage.Clear();

    private async void btnInstallAll_Click(object? sender, EventArgs e)
        => await SafeUiAsync("Installation", InstallAllAsync);

    private async void btnCheckUpdates_Click(object? sender, EventArgs e)
        => await SafeUiAsync(
            L10n.T(_s.Language, "msg.update"),
            () => CheckForUpdatesAsync(interactive: true));

    private void btnOpenGitHub_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updater.RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                L10n.T(_s.Language, "msg.update"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void cmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _s.Language = cmbLanguage.SelectedIndex == 1 ? "en" : "fr";
        ApplyTranslations();
        _ = RefreshModelChoicesAsync();
    }

    private void btnSettingsSave_Click(object? sender, EventArgs e)
    {
        try
        {
            ValidateConfigurationUi();
            SaveSettingsFromUi();
            SettingsStore.Save(_s);
            SynchronizeWorkflowDefaults();

            Log("UI", "Configuration enregistrée : " +
                Path.Combine(PortablePaths.ConfigDir, "settings.json"));

            MessageBox.Show(
                L10n.T(_s.Language, "msg.settings_saved"),
                L10n.T(_s.Language, "msg.config"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            CrashLog.Write("Settings.Save", ex);
            MessageBox.Show(
                ex.Message,
                "Configuration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void btnOpenConfigFolder_Click(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(PortablePaths.ConfigDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{PortablePaths.ConfigDir}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Configuration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void LoadSettingsToUi()
    {
        _loadingSettingsExperience = true;
        txtConfigRoot.Text = PortablePaths.Root;

        numOpenCodePort.Value = ClampNumeric(numOpenCodePort, _s.OpenCodePort);
        numOllamaPort.Value = ClampNumeric(numOllamaPort, _s.OllamaPort);
        numComfyPort.Value = ClampNumeric(numComfyPort, _s.ComfyPort);
        numProxyPort.Value = ClampNumeric(numProxyPort, _s.ImageProxyPort);
        numApiPort.Value = ClampNumeric(numApiPort, _s.GenerationApiPort);

        txtVisionModel.Text = _s.VisionModel;
        txtFluxModel.Text = _s.FluxModel;
        txtTextEncoder.Text = _s.TextEncoderModel;
        txtVae.Text = _s.VaeModel;

        numDefaultWidth.Value = ClampNumeric(numDefaultWidth, _s.DefaultWidth);
        numDefaultHeight.Value = ClampNumeric(numDefaultHeight, _s.DefaultHeight);
        numDefaultSteps.Value = ClampNumeric(numDefaultSteps, _s.DefaultSteps);
        numSafeVram.Value = ClampNumeric(numSafeVram, _s.SafeVramMiB);
        numSafeRam.Value = ClampNumeric(numSafeRam, _s.SafeFreeRamMiB);
        numDownloadConnections.Value = ClampNumeric(
            numDownloadConnections,
            _s.DownloadConnections);
        numDownloadBuffer.Value = ClampNumeric(
            numDownloadBuffer,
            _s.DownloadBufferMiB);

        chkHardStopComfy.Checked = _s.HardStopComfyAfterGeneration;

        cmbLanguage.SelectedIndex =
            L10n.IsEnglish(_s.Language) ? 1 : 0;

        chkAutoUpdates.Checked = _s.AutoCheckUpdates;
        chkInstallVisionModel.Checked = _s.InstallVisionModel;
        txtGitHubRepo.Text =
            $"{_s.GitHubOwner}/{_s.GitHubRepository}";

        LoadSettingsExperienceToUi();
        _loadingSettingsExperience = false;
    }

    private void SaveSettingsFromUi()
    {
        _s.OpenCodePort = Decimal.ToInt32(numOpenCodePort.Value);
        _s.OllamaPort = Decimal.ToInt32(numOllamaPort.Value);
        _s.ComfyPort = Decimal.ToInt32(numComfyPort.Value);
        _s.ImageProxyPort = Decimal.ToInt32(numProxyPort.Value);
        _s.GenerationApiPort = Decimal.ToInt32(numApiPort.Value);

        var visionModel = SelectedModel(_visionModelCombo, txtVisionModel);
        var fluxModel = SelectedModel(_fluxModelCombo, txtFluxModel);
        var textEncoderModel = SelectedModel(_textEncoderCombo, txtTextEncoder);
        var vaeModel = SelectedModel(_vaeCombo, txtVae);

        ValidateSelectedFlux2Models(
            fluxModel,
            textEncoderModel,
            vaeModel);

        _s.VisionModel = visionModel;
        _s.FluxModel = fluxModel;
        _s.TextEncoderModel = textEncoderModel;
        _s.VaeModel = vaeModel;
        _s.VideoModel = _cfgVideoModel.Text.Trim();
        _s.VideoTextEncoderModel = _cfgVideoTextEncoder.Text.Trim();
        _s.VideoVaeModel = _cfgVideoVae.Text.Trim();
        _s.VideoClipVisionModel = _cfgVideoClipVision.Text.Trim();
        _s.NegativePrompt = _negativePrompt.Text.Trim();
        _s.AutoImprovePrompt = _autoImprovePrompt.Checked;
        _s.PromptModel = CurrentComboModel(_promptModelCombo);
        _s.GenerationSeed = Decimal.ToInt64(_seedInput.Value);
        _s.UseRandomSeed = _randomSeedCheck.Checked;

        _s.DefaultWidth = Decimal.ToInt32(numDefaultWidth.Value);
        _s.DefaultHeight = Decimal.ToInt32(numDefaultHeight.Value);
        _s.DefaultSteps = Decimal.ToInt32(numDefaultSteps.Value);
        _s.ImageCfg = Decimal.ToDouble(_imageCfg.Value);
        _s.MaximumQualityImageSharpnessPercent =
            Decimal.ToInt32(_imageMaxQualitySharpness.Value);

        _s.VideoWidth = Decimal.ToInt32(_videoWidth.Value);
        _s.VideoHeight = Decimal.ToInt32(_videoHeight.Value);
        _s.VideoFrames = Decimal.ToInt32(_videoFrames.Value);
        _s.VideoFps = Decimal.ToInt32(_videoFps.Value);
        _s.VideoDurationSeconds =
            Decimal.ToDouble(_videoDurationSeconds.Value);
        _s.VideoSteps = Decimal.ToInt32(_videoSteps.Value);
        _s.VideoCfg = Decimal.ToDouble(_videoCfg.Value);
        _s.MaximumQualityVideoSharpnessPercent =
            Decimal.ToInt32(_videoMaxQualitySharpness.Value);
        _s.VideoSamplingShift = Decimal.ToDouble(_videoSamplingShift.Value);
        _s.VideoSampler = _videoSampler.SelectedItem?.ToString() ?? "uni_pc";
        _s.VideoScheduler = _videoScheduler.SelectedItem?.ToString() ?? "simple";
        _s.VideoSeed = Decimal.ToInt64(_videoSeed.Value);
        _s.UseRandomVideoSeed = _videoRandomSeed.Checked;

        _s.SafeVramMiB = Decimal.ToInt32(numSafeVram.Value);
        _s.SafeFreeRamMiB = Decimal.ToInt32(numSafeRam.Value);
        _s.DownloadConnections = Decimal.ToInt32(numDownloadConnections.Value);
        _s.DownloadBufferMiB = Decimal.ToInt32(numDownloadBuffer.Value);
        _s.HardStopComfyAfterGeneration = chkHardStopComfy.Checked;
        _s.AutoSaveConfiguration = _autoSaveConfigurationCheck.Checked;

        _s.Language =
            cmbLanguage.SelectedIndex == 1 ? "en" : "fr";

        _s.AutoCheckUpdates = chkAutoUpdates.Checked;
        _s.InstallVisionModel = chkInstallVisionModel.Checked;

        var repoParts =
            txtGitHubRepo.Text
                .Trim()
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

        if (repoParts.Length != 2)
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Le dépôt GitHub doit être au format propriétaire/dépôt.",
                    "The GitHub repository must use the owner/repository format."));

        _s.GitHubOwner = repoParts[0];
        _s.GitHubRepository = repoParts[1];

        ApplyTranslations();
    }

    /*
    FR : Applique les libellés statiques de la langue sélectionnée.
    EN: Applies all static labels for the selected UI language.
    */
    private void ApplyTranslations()
    {
        var lang = _s.Language;

        tabDashboard.Text = L10n.T(lang, "tab.dashboard");
        tabOpenCode.Text = L10n.T(lang, "tab.opencode");
        tabComfy.Text = L10n.T(lang, "tab.comfy");
        tabOllama.Text = L10n.T(lang, "tab.ollama");
        tabGenerate.Text = L10n.T(lang, "tab.generate");
        tabInstallation.Text = L10n.T(lang, "tab.install");
        tabConfiguration.Text = L10n.T(lang, "tab.config");
        tabLogs.Text = L10n.T(lang, "tab.logs");
        tabAbout.Text = L10n.T(lang, "tab.about");

        Text = BrandInfo.ProductName;
        lblTitle.Text = BrandInfo.ProductName;

        btnStart.Text = L10n.T(lang, "button.start");
        btnStop.Text = L10n.T(lang, "button.stop");
        btnOpenCode.Text = L10n.T(lang, "button.open_opencode");
        btnComfy.Text = L10n.T(lang, "button.open_comfy");
        btnDiagnostic.Text = L10n.T(lang, "button.diagnostic");
        btnGenerate.Text = L10n.T(lang, "button.generate");
        btnBrowseInputImage.Text = L10n.T(lang, "button.browse");
        btnClearInputImage.Text = L10n.T(lang, "button.clear");
        btnInstallAll.Text = L10n.T(lang, "button.install");
        btnInstallCancel.Text = L10n.T(lang, "button.cancel");
        btnSettingsSave.Text = L10n.T(lang, "button.save");
        btnOpenConfigFolder.Text = L10n.T(lang, "button.open_config");
        btnCheckUpdates.Text = L10n.T(lang, "button.check_updates");
        btnOpenGitHub.Text = L10n.T(lang, "button.github");

        lblLiveLog.Text = L10n.T(lang, "label.live_console");
        lblPrompt.Text = L10n.T(lang, "label.prompt");
        lblGenerationMode.Text = L10n.T(lang, "label.mode");
        lblInputImage.Text = L10n.T(lang, "label.input_image");
        lblImg2ImgStrength.Text = L10n.T(lang, "label.img2img_strength");
        lblInstallTitle.Text = L10n.T(lang, "install.title");
        lblInstallInfo.Text = L10n.T(lang, "install.info");

        lblConfigTitle.Text = L10n.T(lang, "config.title");
        lblConfigRootCaption.Text = L10n.T(lang, "config.root");
        lblCfgLanguage.Text = L10n.T(lang, "config.language");
        chkAutoUpdates.Text = L10n.T(lang, "config.auto_update");
        lblCfgGitHubRepo.Text = L10n.T(lang, "config.github_repo");
        chkInstallVisionModel.Text = L10n.T(lang, "config.vision_optional");
        lblConfigHint.Text = L10n.T(lang, "config.hint");

        lblCfgOpenCodePort.Text = "Port OpenCode";
        lblCfgOllamaPort.Text = "Port Ollama";
        lblCfgComfyPort.Text = "Port ComfyUI";
        lblCfgProxyPort.Text = "Port Proxy";
        lblCfgApiPort.Text = L10n.Pick(lang, "Port API génération", "Generation API port");
        lblCfgVisionModel.Text = L10n.Pick(lang, "Modèle vision", "Vision model");
        lblCfgFluxModel.Text = L10n.Pick(lang, "Modèle FLUX", "FLUX model");
        lblCfgTextEncoder.Text = "Text encoder";
        lblCfgVae.Text = "VAE";
        lblCfgWidth.Text = L10n.Pick(lang, "Largeur défaut", "Default width");
        lblCfgHeight.Text = L10n.Pick(lang, "Hauteur défaut", "Default height");
        lblCfgSteps.Text = L10n.Pick(lang, "Steps défaut", "Default steps");
        lblCfgVram.Text = L10n.Pick(lang, "VRAM libre min MiB", "Min free VRAM MiB");
        lblCfgRam.Text = L10n.Pick(lang, "RAM libre min MiB", "Min free RAM MiB");
        lblCfgConnections.Text = L10n.Pick(lang, "Connexions téléchargement", "Download connections");
        lblCfgBuffer.Text = L10n.Pick(lang, "Buffer téléchargement MiB", "Download buffer MiB");
        chkHardStopComfy.Text = L10n.Pick(
            lang,
            "Arrêter ComfyUI après une génération",
            "Stop ComfyUI after generation");

        lblAboutTitle.Text =
            $"{L10n.T(lang, "about.title")} {BrandInfo.ProductName}";

        lblAboutDescription.Text =
            L10n.IsEnglish(lang)
                ? BrandInfo.DescriptionEn
                : BrandInfo.DescriptionFr;

        lblAboutVersion.Text =
            $"{L10n.T(lang, "about.version")} : {Application.ProductVersion}";

        lblAboutAuthor.Text =
            $"{L10n.T(lang, "about.author")} : {BrandInfo.AuthorName}";

        lblAboutRepository.Text =
            $"{L10n.T(lang, "about.repository")} : {_s.GitHubOwner}/{_s.GitHubRepository}";

        lblAboutLicense.Text =
            L10n.T(lang, "about.license");

        if (string.IsNullOrWhiteSpace(_aboutUpdateStatus.Text))
            _aboutUpdateStatus.Text = L10n.T(lang, "about.update_ready");

        var selectedMode = cmbGenerationMode.SelectedIndex;
        cmbGenerationMode.Items.Clear();
        cmbGenerationMode.Items.Add(L10n.T(lang, "generation.mode.txt2img"));
        cmbGenerationMode.Items.Add(L10n.T(lang, "generation.mode.img2img"));
        cmbGenerationMode.SelectedIndex =
            selectedMode is 1 ? 1 : 0;

        ApplyEnhancedTranslations();
        ApplySettingsExperienceTranslations();
        _tabs.Invalidate(true);
        _tabs.Refresh();
    }

    /*
    FR : Active ou désactive les contrôles propres au mode image→image.
    EN: Enables or disables controls that are specific to image-to-image mode.
    */
    private void UpdateGenerationModeUi()
    {
        var isImgToImg = cmbGenerationMode.SelectedIndex == 1;

        txtInputImage.Enabled = isImgToImg;
        btnBrowseInputImage.Enabled = isImgToImg;
        btnClearInputImage.Enabled = isImgToImg;
        lblInputImage.Enabled = true;
        lblInputImage.ForeColor = Color.White;
        // Le workflow officiel FLUX.2 Klein Image Edit n'expose pas de
        // paramètre denoise/strength. Le champ reste visible pour compatibilité
        // avec les anciennes configurations, mais il n'est plus modifiable.
        numImg2ImgStrength.Enabled = false;
        lblImg2ImgStrength.Enabled = true;
        lblImg2ImgStrength.ForeColor = Color.White;
    }

    /*
    FR : Affiche une image dans le panneau de prévisualisation.
    EN: Displays an image in the preview area.
    */
    private void ShowPreviewImage(string path)
    {
        if (!File.Exists(path))
            return;

        using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using var temp = Image.FromStream(fs);
        var old = _preview.Image;
        _preview.Image = new Bitmap(temp);
        old?.Dispose();
        ResetImagePreviewView();
    }

    /*
    FR : Charge l'icône de l'exécutable et initialise l'onglet À propos.
    EN: Loads the executable icon and initializes the About tab.
    */
    private void InitializeAbout()
    {
        try
        {
            var exe = Environment.ProcessPath;

            if (!string.IsNullOrWhiteSpace(exe))
            {
                using var icon = Icon.ExtractAssociatedIcon(exe);
                if (icon is not null)
                    Icon = (Icon)icon.Clone();
            }

            using var stream =
                typeof(MainForm)
                    .Assembly
                    .GetManifestResourceStream(
                        "OpenCodeLocalAI.Resources.AppIcon.png");

            if (stream is not null)
            {
                using var source = Image.FromStream(stream);
                var old = picAboutIcon.Image;
                picAboutIcon.Image = new Bitmap(source);
                old?.Dispose();

                Log(
                    "UI",
                    $"Image À propos HD chargée : {source.Width}x{source.Height}.");
            }
            else
            {
                Log(
                    "UI !",
                    "Ressource HD AppIcon.png introuvable pour l'onglet À propos.");
            }
        }
        catch (Exception ex)
        {
            Log("UI", "Icône About : " + ex.Message);
        }

        _aboutUpdateProgress.Value = 0;
        _aboutUpdateStatus.Text =
            L10n.T(_s.Language, "about.update_ready");
    }

    /*
    FR : Vérifie la dernière release GitHub et propose une mise à jour sûre du seul EXE.
    EN: Checks the latest GitHub release and offers a safe single-EXE update.
    */
    private async Task CheckForUpdatesAsync(bool interactive)
    {
        btnCheckUpdates.Enabled = false;
        _aboutUpdateProgress.Value = 0;
        _aboutUpdateStatus.Text = L10n.T(_s.Language, "about.checking");

        try
        {
            var update =
                await _updater.CheckAsync(CancellationToken.None);

            if (update is null)
            {
                _aboutUpdateStatus.Text =
                    L10n.T(_s.Language, "about.repo_missing");

                if (interactive)
                {
                    MessageBox.Show(
                        _aboutUpdateStatus.Text,
                        L10n.T(_s.Language, "msg.update"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            if (!update.Available)
            {
                _aboutUpdateStatus.Text =
                    $"{L10n.T(_s.Language, "about.latest")} ({update.CurrentVersion})";

                return;
            }

            _aboutUpdateStatus.Text =
                $"{L10n.T(_s.Language, "about.update_available")} {update.LatestVersion}";

            var answer =
                MessageBox.Show(
                    _aboutUpdateStatus.Text + "\n\n" +
                    L10n.T(_s.Language, "about.install_update"),
                    L10n.T(_s.Language, "msg.update"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            _aboutUpdateStatus.Text =
                L10n.T(_s.Language, "about.downloading");

            var progress =
                new Progress<int>(value =>
                {
                    _aboutUpdateProgress.Value =
                        Math.Clamp(value, 0, 100);
                });

            await _updater.StageAndLaunchAsync(
                update,
                progress,
                CancellationToken.None);

            _aboutUpdateStatus.Text =
                L10n.T(_s.Language, "about.update_staged");

            _closing = true;
            _logFlushTimer.Stop();

            try { await StopAllAsync(); }
            catch (Exception ex) { CrashLog.Write("Update shutdown", ex); }

            Application.Exit();
        }
        catch (HttpRequestException ex)
        {
            _aboutUpdateStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "Impossible de joindre GitHub : ",
                    "Unable to reach GitHub: ") + ex.Message;

            if (interactive)
                throw;
        }
        finally
        {
            if (!IsDisposed)
                btnCheckUpdates.Enabled = true;
        }
    }

    private static decimal ClampNumeric(NumericUpDown control, int value)
        => Math.Clamp((decimal)value, control.Minimum, control.Maximum);

    private void btnInstallCancel_Click(object? sender, EventArgs e)
    {
        var cts = _installCts;
        if (cts is null || cts.IsCancellationRequested)
            return;

        _installText.Text = "Annulation demandée… arrêt propre en cours.";
        btnInstallCancel.Enabled = false;
        Log("Install", "Annulation demandée par l'utilisateur.");
        cts.Cancel();
    }

    private async Task StartupPreflightAsync()
    {
        UpdateGpu();

        var missing = PortablePreflight.GetMissing(_s);
        if (missing.Count == 0)
        {
            _status.Text = "Pack portable prêt.";
            return;
        }

        _status.Text = $"{missing.Count} composant(s) portable(s) manquant(s).";
        var details = string.Join("\n", missing.Select(m => "• " + m.Label));

        var result = MessageBox.Show(
            "Le pack portable n'est pas complet :\n\n" + details +
            "\n\nVoulez-vous lancer l'installation portable maintenant ?",
            "Installation portable requise",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result == DialogResult.Yes)
        {
            _tabs.SelectedTab = tabInstallation;
            await InstallAllAsync();
        }
    }

    private async Task InstallAllAsync()
    {
        if (_installTask is not null)
        {
            MessageBox.Show(
                "Une installation est déjà en cours.",
                "Installation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _installTask = InstallAllCoreAsync();

        try
        {
            await _installTask;
        }
        finally
        {
            _installTask = null;
        }
    }

    private async Task InstallAllCoreAsync()
    {
        _installCts = new CancellationTokenSource();
        btnInstallAll.Enabled = false;
        btnInstallCancel.Enabled = true;
        btnStart.Enabled = false;
        btnOpenCode.Enabled = false;
        btnComfy.Enabled = false;
        btnGenerate.Enabled = false;
        _installProgress.Value = 0;
        _installText.Text = "Préparation de l'installation portable…";

        try
        {
            await StopAllAsync();
            _installCts.Token.ThrowIfCancellationRequested();

            await _installer.InstallAllAsync(_installCts.Token);
            _installCts.Token.ThrowIfCancellationRequested();

            var missing = PortablePreflight.GetMissing(_s);
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "Installation terminée mais certains composants sont encore absents :\n" +
                    string.Join("\n", missing.Select(x => "• " + x.Label + " : " + x.Path)));
            }

            _installText.Text = "Installation portable terminée.";
            RefreshFeatureAvailability();
            RefreshInstallationVideoStatus();
            MessageBox.Show(
                "Installation portable terminée avec succès.",
                BrandInfo.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            _installText.Text = "Installation annulée. Les fichiers temporaires incomplets ont été nettoyés.";
            Log("Install", "Installation annulée proprement.");
            throw;
        }
        finally
        {
            _installCts.Dispose();
            _installCts = null;

            btnInstallAll.Enabled = true;
            btnInstallCancel.Enabled = false;
            btnStart.Enabled = true;
            btnOpenCode.Enabled = true;
            btnComfy.Enabled = true;
            btnGenerate.Enabled = true;
            RefreshFeatureAvailability();
        }
    }

    private int CaptureServiceLifecycleEpoch() =>
        Volatile.Read(ref _serviceLifecycleEpoch);

    private bool IsServiceLifecycleCurrent(int epoch) =>
        epoch == Volatile.Read(ref _serviceLifecycleEpoch);

    private async Task StartCoreAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();
        Log("Système", $"Démarrage global demandé · cycle {epoch}.");
        var missing = PortablePreflight.GetMissing(_s);

        if (missing.Any(x => x.Key is "opencode" or "ollama"))
        {
            var r = MessageBox.Show(
                "OpenCode/Ollama portable n'est pas installé.\n\n" +
                "L'application n'utilisera pas une installation présente ailleurs dans Windows.\n\n" +
                "Ouvrir l'installeur portable maintenant ?",
                "Composants portables absents",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (r == DialogResult.Yes)
            {
                _tabs.SelectedTab = tabInstallation;
                await InstallAllAsync();
            }
            return;
        }

        await StartOllamaAsync();
        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await StartOpenCodeAsync();
        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await _proxy.StartAsync();
        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _proxy.StopAsync();
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await _api.StartAsync();
        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _api.StopAsync();
            await _proxy.StopAsync();
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await OpenEmbeddedAsync();

        if (IsServiceLifecycleCurrent(epoch))
            _status.Text = "OpenCode + Ollama portables démarrés.";
    }

    private async Task EnsurePortableServicePortFreeAsync(
        int port,
        string label,
        string expectedRelativeExe)
    {
        var existing = await PortablePreflight.InspectPortAsync(port);
        if (!existing.Open)
            return;

        var expected = Path.GetFullPath(
            PortablePaths.Resolve(expectedRelativeExe));

        var actual =
            string.IsNullOrWhiteSpace(existing.Path)
                ? null
                : Path.GetFullPath(existing.Path);

        var isOurPortableProcess =
            existing.Pid is int &&
            actual is not null &&
            PortablePaths.IsInsidePack(actual) &&
            string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase);

        if (!isOurPortableProcess)
        {
            await PortablePreflight.RejectOccupiedExternalPortAsync(
                port,
                label);
            return;
        }

        var pid = existing.Pid!.Value;
        Log(
            label,
            $"Ancien service portable détecté sur le port {port} · PID {pid}. " +
            "Redémarrage propre pour restaurer les flux console.");

        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var check = await PortablePreflight.InspectPortAsync(port);
            if (!check.Open)
                return;

            await Task.Delay(200);
        }

        throw new InvalidOperationException(
            $"{label} : le port {port} n'a pas été libéré après l'arrêt de l'ancien service portable.");
    }

    private async Task<bool> IsPortableServiceListeningAsync(
        int port,
        string expectedRelativeExe)
    {
        var existing =
            await PortablePreflight.InspectPortAsync(port);

        if (!existing.Open ||
            existing.Pid is not int ||
            string.IsNullOrWhiteSpace(existing.Path))
        {
            return false;
        }

        try
        {
            var expected =
                Path.GetFullPath(
                    PortablePaths.Resolve(
                        expectedRelativeExe));

            var actual =
                Path.GetFullPath(
                    existing.Path);

            return PortablePaths.IsInsidePack(actual) &&
                   string.Equals(
                       actual,
                       expected,
                       StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task StartOllamaAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();

        if (_ollama.Running)
            return;

        var portableComfyActive =
            _comfy.Running ||
            await IsPortableServiceListeningAsync(
                _s.ComfyPort,
                _s.ComfyPython);

        if (portableComfyActive)
        {
            if (_generator.IsBusy || _videoCts is not null)
            {
                throw new InvalidOperationException(
                    L10n.Pick(
                        _s.Language,
                        "Ollama ne sera pas démarré pendant une génération FLUX/Wan active. " +
                        "Attendez la fin du calcul avant d'ouvrir la console Ollama.",
                        "Ollama will not be started while an active FLUX/Wan generation is using ComfyUI. " +
                        "Wait for the generation to finish before opening the Ollama console."));
            }

            Log(
                "Système",
                "ComfyUI portable arrêté avant Ollama pour éviter le chevauchement RAM/VRAM.");

            await _comfy.StopAsync(TimeSpan.FromSeconds(1));

            // Also catches a portable ComfyUI left alive by a previous
            // DreamRaster process and releases its console/memory cleanly.
            await EnsurePortableServicePortFreeAsync(
                _s.ComfyPort,
                "ComfyUI",
                _s.ComfyPython);

            await WaitForCommitRecoveryAsync(
                4096,
                TimeSpan.FromSeconds(20),
                CancellationToken.None);

            if (!IsServiceLifecycleCurrent(epoch))
                return;
        }

        var availableCommit = GetAvailableCommitMiB();
        if (availableCommit < 3072)
        {
            throw new InvalidOperationException(
                BuildVirtualMemoryGuidance(
                    "Ollama",
                    availableCommit));
        }

        await EnsurePortableServicePortFreeAsync(
            _s.OllamaPort,
            "Ollama",
            _s.OllamaExe);

        if (!IsServiceLifecycleCurrent(epoch))
            return;

        var env = PortablePreflight.PortableEnvironment("ollama");
        env["OLLAMA_MODELS"] = PortablePaths.Resolve(_s.OllamaModels);
        env["OLLAMA_HOST"] = $"127.0.0.1:{_s.OllamaPort}";
        env["OLLAMA_NO_CLOUD"] = "true";
        env["OLLAMA_NOHISTORY"] = "true";

        // One local model/request at a time is intentional on the
        // 16-GB GPU target: it avoids a second runner consuming commit
        // while ComfyUI is about to claim the GPU.
        env["OLLAMA_MAX_LOADED_MODELS"] = "1";
        env["OLLAMA_NUM_PARALLEL"] = "1";

        var exe = PortablePaths.Resolve(_s.OllamaExe);

        try
        {
            await _ollama.StartAsync(
                _s.OllamaExe,
                "serve",
                Path.GetDirectoryName(exe)!,
                env);
        }
        catch (Win32Exception ex)
            when (ex.NativeErrorCode == 1455)
        {
            throw new InvalidOperationException(
                BuildVirtualMemoryGuidance(
                    "Ollama",
                    GetAvailableCommitMiB()),
                ex);
        }

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await WaitForPortAsync(
            _s.OllamaPort,
            TimeSpan.FromSeconds(20));

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await WaitForOllamaReadyAsync(
            TimeSpan.FromSeconds(30));

        if (!IsServiceLifecycleCurrent(epoch))
            await _ollama.StopAsync(TimeSpan.FromSeconds(1));
    }

    private async Task WaitForOllamaReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync(
                    $"http://127.0.0.1:{_s.OllamaPort}/api/tags");

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            "Ollama a ouvert son port mais son API n'est pas devenue prête à temps." +
            (lastError is null ? string.Empty : " " + lastError.Message));
    }

    private async Task StartOpenCodeAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();

        if (_openCode.Running)
            return;

        await EnsurePortableServicePortFreeAsync(
            _s.OpenCodePort,
            "OpenCode",
            _s.OpenCodeExe);

        if (!IsServiceLifecycleCurrent(epoch))
            return;

        var env = PortablePreflight.PortableEnvironment("opencode");
        env["XDG_DATA_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "opencode", "data");
        env["XDG_CONFIG_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "opencode", "config");
        env["XDG_STATE_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "opencode", "state");
        env["XDG_CACHE_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "opencode", "cache");

        foreach (var entry in env.Where(kv => kv.Key.StartsWith("XDG_", StringComparison.OrdinalIgnoreCase)))
            Directory.CreateDirectory(entry.Value);

        var workspace = PortablePaths.Resolve(_s.Workspace);

        await _openCode.StartAsync(
            _s.OpenCodeExe,
            $"serve --hostname 127.0.0.1 --port {_s.OpenCodePort}",
            workspace,
            env);

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await WaitForPortAsync(_s.OpenCodePort, TimeSpan.FromSeconds(20));

        if (!IsServiceLifecycleCurrent(epoch))
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
    }

    private async Task StartComfyAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();

        if (_comfy.Running)
        {
            // Le processus Python peut déjà exister alors que ComfyUI est encore
            // en phase d'import / migration et que 8188 n'écoute pas. Attendre
            // réellement le port évite les POST /prompt trop précoces.
            var runningPort =
                await PortablePreflight.InspectPortAsync(
                    _s.ComfyPort);

            if (!runningPort.Open)
            {
                await WaitForPortAsync(
                    _s.ComfyPort,
                    TimeSpan.FromSeconds(120),
                    () => _comfy.Running,
                    "ComfyUI");
            }

            return;
        }

        var missing = PortablePreflight.GetMissing(_s);
        if (missing.Any(x => x.Key is "comfy-python" or "comfy-main"))
        {
            MessageBox.Show(
                "ComfyUI portable n'est pas installé.\n\nUtilisez l'onglet Installation.",
                "ComfyUI",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _tabs.SelectedTab = tabInstallation;
            return;
        }

        await EnsurePortableServicePortFreeAsync(
            _s.ComfyPort,
            "ComfyUI",
            _s.ComfyPython);

        if (!IsServiceLifecycleCurrent(epoch))
            return;

        var env = PortablePreflight.PortableEnvironment("comfyui");
        env["PYTHONNOUSERSITE"] = "1";
        env["HF_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "huggingface");
        env["TORCH_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "torch");
        env["XDG_CACHE_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "cache");

        var py = PortablePaths.Resolve(_s.ComfyPython);
        var main = PortablePaths.Resolve(_s.ComfyMain);
        var relPy = Path.GetRelativePath(PortablePaths.Root, py);

        ComfyWindowsCompatibility.CheckAimdoCompatibility(
            _s,
            Log);

        await _comfy.StartAsync(
            relPy,
            $"\"{main}\" --listen 127.0.0.1 --port {_s.ComfyPort} --disable-auto-launch --disable-pinned-memory --disable-async-offload --disable-fast-disk",
            Path.GetDirectoryName(main)!,
            env);

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await WaitForPortAsync(
            _s.ComfyPort,
            TimeSpan.FromSeconds(120),
            () => _comfy.Running,
            "ComfyUI");

        if (!IsServiceLifecycleCurrent(epoch))
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
    }

    private async Task OpenEmbeddedAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();

        if (!_openCode.Running)
            await StartOpenCodeAsync();

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        var fixedRuntime = PortablePaths.GetFixedWebView2RuntimePath();
        if (fixedRuntime is null)
        {
            MessageBox.Show(
                "Le runtime WebView2 Fixed portable est absent de bin\\webview2-fixed.\n\n" +
                "Aucun runtime WebView2 installé dans Windows ne sera utilisé automatiquement.",
                "WebView2 portable absent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_web.CoreWebView2 is null)
        {
            Log(
                "WebView2",
                "Fixed Runtime OpenCode : " + fixedRuntime);

            var profile = Path.Combine(PortablePaths.RuntimeDir, "webview2-opencode");
            var webEnv = await CoreWebView2Environment.CreateAsync(fixedRuntime, profile);
            await _web.EnsureCoreWebView2Async(webEnv);

            if (!IsServiceLifecycleCurrent(epoch))
            {
                await _openCode.StopAsync(TimeSpan.FromSeconds(1));
                return;
            }

            var core =
                _web.CoreWebView2
                ?? throw new InvalidOperationException(
                    "Initialisation WebView2 OpenCode incomplète.");

            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;

                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && IsLocalUri(uri))
                    core.Navigate(e.Uri);
                else
                    Log("WebView", "Ouverture externe bloquée : " + e.Uri);
            };

            core.NavigationStarting += (_, e) =>
            {
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) || IsLocalUri(uri))
                    return;

                e.Cancel = true;
                Log("WebView", "Navigation externe bloquée : " + e.Uri);
            };
        }

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        await _proxy.StartAsync();

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _proxy.StopAsync();
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        var openCodeCore =
            _web.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 OpenCode n'est pas initialisé.");

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _proxy.StopAsync();
            await _openCode.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        openCodeCore.Navigate($"http://127.0.0.1:{_s.ImageProxyPort}/");
        EnsureTechnicalTabVisibleForDirectOpenPolish(tabOpenCode);
        _tabs.SelectedTab = tabOpenCode;
    }

    private async Task OpenComfyEmbeddedAsync()
    {
        var epoch = CaptureServiceLifecycleEpoch();
        ShowComfyStatusPageV36(
            L10n.Pick(_s.Language, "Démarrage de ComfyUI…", "Starting ComfyUI…"),
            L10n.Pick(
                _s.Language,
                "DreamRaster attend que le serveur local soit réellement prêt. Cette première ouverture peut prendre plus d'une minute.",
                "DreamRaster is waiting for the local server to become fully ready. The first start can take more than a minute."),
            retry: false);

        try
        {
            // Gère aussi Python déjà lancé mais 8188 encore en initialisation.
            await StartComfyAsync();
        }
        catch (TimeoutException ex)
        {
            Log("ComfyUI ⚠", ex.Message);
            ShowComfyStatusPageV36(
                L10n.Pick(_s.Language, "ComfyUI ne répond pas", "ComfyUI is not responding"),
                L10n.Pick(
                    _s.Language,
                    "Le processus a été lancé mais le port local ne s'est pas ouvert à temps. Consultez les logs ComfyUI puis utilisez Réessayer.",
                    "The process was started but the local port did not open in time. Check the ComfyUI logs, then use Retry."),
                retry: true);
            return;
        }
        catch (Exception ex)
        {
            Log("ComfyUI ✗", ex.Message);
            ShowComfyStatusPageV36(
                L10n.Pick(_s.Language, "Échec du démarrage ComfyUI", "ComfyUI startup failed"),
                ex.Message,
                retry: true);
            return;
        }

        if (!IsServiceLifecycleCurrent(epoch))
        {
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
            return;
        }

        var port = await PortablePreflight.InspectPortAsync(_s.ComfyPort);
        if (!_comfy.Running || !port.Open)
        {
            ShowComfyStatusPageV36(
                L10n.Pick(_s.Language, "ComfyUI indisponible", "ComfyUI unavailable"),
                L10n.Pick(
                    _s.Language,
                    "Le serveur local n'écoute pas encore sur le port configuré.",
                    "The local server is not listening on the configured port yet."),
                retry: true);
            return;
        }

        await EnsureComfyWebViewReadyV36();
        ShowComfyWebV36();

        var core = _comfyWeb.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 ComfyUI n'est pas initialisé.");

        core.Navigate(
            $"http://127.0.0.1:{_s.ComfyPort}/");

        EnsureTechnicalTabVisibleForDirectOpenPolish(tabComfy);
        _tabs.SelectedTab = tabComfy;
    }

    private static bool IsLocalUri(Uri uri)
        => uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase)
           // NavigateToString utilise une URL data: interne. Elle doit rester
           // autorisée, sinon la page d'état ComfyUI s'auto-bloque en boucle.
           || uri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("images.local", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("videos.local", StringComparison.OrdinalIgnoreCase);

    private async Task ExportDiagnosticAsync()
    {
        var paths = await DiagnosticExporter.ExportAsync(_s);

        MessageBox.Show(
            $"Diagnostic enregistré :\n{paths.TextPath}\n{paths.JsonPath}",
            "Diagnostic",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async Task StopVisionModelAsync()
    {
        try
        {
            var portableListening =
                await IsPortableServiceListeningAsync(
                    _s.OllamaPort,
                    _s.OllamaExe);

            if (!_ollama.Running && !portableListening)
            {
                Log("Ollama", "Ollama déjà arrêté avant l'opération GPU.");
                await StopPortableOllamaForFluxAsync();
                return;
            }

            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var models = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var runningModelsKnown = false;

            // The benchmark can temporarily use a model different from
            // PromptModel (for example qwen3:4b). Ask Ollama what is
            // actually resident so every loaded model is released before
            // ComfyUI/FLUX claims the GPU. Do not send keep_alive=0 to a
            // non-resident configured model because Ollama can load it
            // first, which defeats the purpose of freeing VRAM.
            try
            {
                using var psResponse = await http.GetAsync(
                    $"http://127.0.0.1:{_s.OllamaPort}/api/ps");

                if (psResponse.IsSuccessStatusCode)
                {
                    runningModelsKnown = true;
                    var raw = await psResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(raw);

                    if (doc.RootElement.TryGetProperty(
                            "models",
                            out var runningModels))
                    {
                        foreach (var item in runningModels.EnumerateArray())
                        {
                            if (!item.TryGetProperty(
                                    "name",
                                    out var nameNode))
                                continue;

                            var name = nameNode.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                models.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(
                    "Ollama ⚠",
                    "Impossible de lire les modèles chargés ; arrêt direct du serveur portable : " +
                    ex.Message);
            }

            // Never guess a configured model when /api/ps failed: sending
            // keep_alive=0 to a non-resident model can make Ollama load it,
            // exactly when ComfyUI needs the GPU. Stop the server instead.
            if (!runningModelsKnown)
                models.Clear();

            foreach (var model in models)
            {
                var body = JsonSerializer.Serialize(new
                {
                    model,
                    prompt = "",
                    keep_alive = 0,
                    stream = false
                });

                try
                {
                    using var response = await http.PostAsync(
                        $"http://127.0.0.1:{_s.OllamaPort}/api/generate",
                        new StringContent(
                            body,
                            Encoding.UTF8,
                            "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        Log(
                            "Ollama",
                            $"Modèle libéré avant FLUX : {model}");
                    }
                    else
                    {
                        Log(
                            "Ollama !",
                            $"Libération {model} : HTTP {(int)response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Log(
                        "Ollama !",
                        $"Libération {model} : {ex.Message}");
                }
            }

            await StopPortableOllamaForFluxAsync();
        }
        catch (Exception ex)
        {
            Log(
                "Ollama !",
                "Libération modèles prompt/vision : " + ex.Message);
        }
    }

    private async Task StopPortableOllamaForFluxAsync()
    {
        try
        {
            await _ollama.StopAsync(
                TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            Log(
                "Ollama !",
                "Arrêt serveur portable avant FLUX : " +
                ex.Message);
        }

        try
        {
            var port =
                await PortablePreflight.InspectPortAsync(
                    _s.OllamaPort);

            if (port.Open &&
                port.Pid is int pid &&
                !string.IsNullOrWhiteSpace(port.Path))
            {
                var expected =
                    Path.GetFullPath(
                        PortablePaths.Resolve(
                            _s.OllamaExe));

                var actual =
                    Path.GetFullPath(
                        port.Path);

                if (PortablePaths.IsInsidePack(actual) &&
                    string.Equals(
                        actual,
                        expected,
                        StringComparison.OrdinalIgnoreCase))
                {
                    using var process =
                        Process.GetProcessById(pid);

                    process.Kill(
                        entireProcessTree: true);

                    await process.WaitForExitAsync();

                    Log(
                        "Ollama",
                        $"Serveur portable arrêté avant FLUX · PID {pid}.");
                }
            }
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }
        catch (Exception ex)
        {
            Log(
                "Ollama !",
                "Arrêt du service portable avant FLUX : " +
                ex.Message);
        }

        var ollamaDir =
            Path.GetFullPath(
                Path.GetDirectoryName(
                    PortablePaths.Resolve(
                        _s.OllamaExe))!);

        foreach (var runner in
                 Process.GetProcessesByName(
                     "llama-server"))
        {
            try
            {
                var path =
                    runner.MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var full =
                    Path.GetFullPath(path);

                if (!full.StartsWith(
                        ollamaDir +
                        Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pid =
                    runner.Id;

                if (!runner.HasExited)
                {
                    runner.Kill(
                        entireProcessTree: true);

                    await runner.WaitForExitAsync();
                }

                Log(
                    "Ollama",
                    $"Runner portable libéré avant FLUX · PID {pid}.");
            }
            catch (Win32Exception ex)
                when (ex.NativeErrorCode == 299)
            {
                // ERROR_PARTIAL_COPY: the runner is already disappearing.
            }
            catch (InvalidOperationException)
            {
                // The process exited between enumeration and inspection.
            }
            catch (Exception ex)
            {
                Log(
                    "Ollama !",
                    "Libération runner portable : " +
                    ex.Message);
            }
            finally
            {
                runner.Dispose();
            }
        }

        await Task.Delay(500);
    }

    private async Task GenerateFromUiAsync()
    {
        var text = _prompt.Text.Trim();

        if (text.Length == 0)
        {
            MessageBox.Show(
                L10n.IsEnglish(_s.Language)
                    ? "Please enter a prompt."
                    : "Saisissez un prompt.",
                "FLUX.2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var isImgToImg = cmbGenerationMode.SelectedIndex == 1;
        var inputImagePath = txtInputImage.Text.Trim();

        if (isImgToImg)
        {
            if (inputImagePath.Length == 0 || !File.Exists(inputImagePath))
            {
                MessageBox.Show(
                    L10n.IsEnglish(_s.Language)
                        ? "Select a valid source image for image-to-image mode."
                        : "Sélectionnez une image source valide pour le mode image→image.",
                    "FLUX.2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ShowPreviewImage(inputImagePath);
        }

        var totalWatch = Stopwatch.StartNew();
        var promptElapsed = TimeSpan.Zero;

        if (_autoImprovePrompt.Checked)
        {
            if (IsPromptAlreadyImproved(text))
            {
                Log("UI", "Amélioration automatique ignorée : ce prompt a déjà été amélioré.");
            }
            else
            {
                _genText.Text = L10n.Pick(
                    _s.Language,
                    "Amélioration locale du prompt…",
                    "Improving prompt locally…");

                var promptWatch = Stopwatch.StartNew();
                text = await ImprovePromptTextAsync(text, CancellationToken.None);
                promptWatch.Stop();
                promptElapsed = promptWatch.Elapsed;
                _prompt.Text = text;
                _s.LastImprovedPromptHash = PromptFingerprint(text);
                SettingsStore.Save(_s);
                UpdatePromptEnhancementState();
            }
        }

        var seed =
            _randomSeedCheck.Checked
                ? (long?)null
                : Decimal.ToInt64(_seedInput.Value);

        var imageStyleId = SelectedTemplateId(
            _imageStyleTemplateCombo,
            _s.ImageStyleTemplate);
        var imageNegativeId = SelectedTemplateId(
            _imageNegativeTemplateCombo,
            _s.ImageNegativeTemplate);

        var generationPrompt = ApplyImageStyleTemplate(text);
        var generationNegative = ApplyImageNegativeTemplate(_negativePrompt.Text.Trim());

        Log(
            "FLUX",
            $"Templates · style={imageStyleId} · négatif={imageNegativeId}.");

        var fluxWatch = Stopwatch.StartNew();
        var result = await _generator.GenerateAsync(
            generationPrompt,
            generationNegative,
            _s.DefaultWidth,
            _s.DefaultHeight,
            isImgToImg ? inputImagePath : null,
            Decimal.ToDouble(numImg2ImgStrength.Value),
            CancellationToken.None,
            seed);
        fluxWatch.Stop();

        var secondPassElapsed = TimeSpan.Zero;
        var secondPassApplied = false;

        if (result.Ok &&
            GenerationTemplates.UsesTwoPassMaximumQuality(imageStyleId) &&
            !string.IsNullOrWhiteSpace(result.Path) &&
            File.Exists(result.Path))
        {
            _lastMaximumQualityImageFirstPassPath = result.Path;
            UpdateMaximumQualitySharpnessUi();
            await RefreshImageSharpnessPreviewAsync(CancellationToken.None);

            try
            {
                _genProgress.Value = 95;
                _genText.Text =
                    L10n.Pick(
                        _s.Language,
                        "Passe 2/2 · upscale et amélioration…",
                        "Pass 2/2 · upscaling and enhancement…");

                var secondPassWatch = Stopwatch.StartNew();
                var enhancedPath =
                    await _qualityPostProcessor.EnhanceImageAsync(
                        result.Path,
                        CancellationToken.None);
                secondPassWatch.Stop();

                secondPassElapsed = secondPassWatch.Elapsed;
                secondPassApplied = true;

                result = result with
                {
                    Path = enhancedPath,
                    Url =
                        $"http://127.0.0.1:{_s.ImageProxyPort}/local-images/" +
                        Uri.EscapeDataString(Path.GetFileName(enhancedPath))
                };

                _genProgress.Value = 100;
            }
            catch (Exception ex)
            {
                Log(
                    "Qualité max !",
                    "Passe 2 image échouée, passe 1 conservée : " +
                    ex.Message);
            }
        }

        totalWatch.Stop();

        _genText.Text =
            L10n.Pick(_s.Language, "Terminé", "Done") +
            $" · Prompt {promptElapsed.TotalSeconds:0.00}s" +
            $" · FLUX {fluxWatch.Elapsed.TotalSeconds:0.00}s" +
            (secondPassApplied
                ? $" · Passe 2 {secondPassElapsed.TotalSeconds:0.00}s"
                : string.Empty) +
            $" · Total {totalWatch.Elapsed.TotalSeconds:0.00}s" +
            $" · Seed {(seed?.ToString() ?? "auto")}";

        if (!result.Ok)
        {
            MessageBox.Show(
                result.Error ?? "Génération échouée.",
                "FLUX.2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (result.Path is not null && File.Exists(result.Path))
        {
            ShowPreviewImage(result.Path);
            if (secondPassApplied &&
                _imageSharpnessBeforePreview.Image is not null)
            {
                SetImageSharpnessComparisonVisible(true);
            }
            RefreshImageHistory();
        }
    }

    private async Task StopAllAsync()
    {
        // Invalide toute ouverture/démarrage asynchrone lancé avant cet arrêt.
        var epoch = Interlocked.Increment(ref _serviceLifecycleEpoch);
        Log("Système", $"Arrêt global demandé · cycle {epoch}.");

        if (_tabs.SelectedTab == tabComfy ||
            _tabs.SelectedTab == tabOpenCode ||
            _tabs.SelectedTab == tabOllama)
        {
            _tabs.SelectedTab = tabDashboard;
        }

        await _api.StopAsync();
        await _proxy.StopAsync();
        await _comfy.StopAsync();
        await _openCode.StopAsync();
        await _ollama.StopAsync();

        _status.Text = "Tous les services portables sont arrêtés.";
    }

    private async Task RunGpuExclusiveAsync(
        string operation,
        Func<Task> action)
    {
        if (!await _gpuWorkflowGate.WaitAsync(0))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    $"Une autre opération GPU est déjà en cours. {operation} ne peut pas démarrer en parallèle.",
                    $"Another GPU operation is already running. {operation} cannot start in parallel."));
        }

        SetGpuWorkflowUiLocked(true, operation);
        try
        {
            await action();
        }
        finally
        {
            SetGpuWorkflowUiLocked(false, operation);
            _gpuWorkflowGate.Release();
        }
    }

    private async Task<T> RunGpuExclusiveAsync<T>(
        string operation,
        Func<Task<T>> action)
    {
        if (!await _gpuWorkflowGate.WaitAsync(0))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    $"Une autre opération GPU est déjà en cours. {operation} ne peut pas démarrer en parallèle.",
                    $"Another GPU operation is already running. {operation} cannot start in parallel."));
        }

        SetGpuWorkflowUiLocked(true, operation);
        try
        {
            return await action();
        }
        finally
        {
            SetGpuWorkflowUiLocked(false, operation);
            _gpuWorkflowGate.Release();
        }
    }

    private async Task SafeUiAsync(string operation, Func<Task> action)
    {
        try
        {
            // Ne jamais désactiver le formulaire entier : le bouton Annuler,
            // les onglets et la fermeture de fenêtre doivent rester réactifs.
            await action();
        }
        catch (OperationCanceledException)
        {
            Log("UI", operation + " annulée.");
        }
        catch (PortableComponentMissingException ex)
        {
            Log("ERREUR", ex.Message);

            var r = MessageBox.Show(
                ex.Message + "\n\nOuvrir l'installeur portable ?",
                operation,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (r == DialogResult.Yes)
                _tabs.SelectedTab = tabInstallation;
        }
        catch (Exception ex)
        {
            CrashLog.Write(operation, ex);
            Log("ERREUR", ex.Message);

            MessageBox.Show(
                ex.Message,
                operation,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void Log(string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _pendingLogs.Enqueue((source, message));
    }

    private void FlushPendingLogs()
    {
        if (Interlocked.Exchange(ref _logFlushBusy, 1) != 0)
            return;

        try
        {
            var diskLines = new List<string>();
            var processed = 0;

            while (processed < 60 &&
                   _pendingLogs.TryDequeue(out var item))
            {
                AnsiLogRenderer.Append(_liveLog, item.Source, item.Message);
                AnsiLogRenderer.Append(_allLog, item.Source, item.Message);
                AppendCategorizedLog(item.Source, item.Message);

                if (item.Source.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiLogRenderer.Append(_ollamaLog, item.Source, item.Message);
                }

                if (item.Source.StartsWith("Install", StringComparison.OrdinalIgnoreCase)
                    || item.Source.StartsWith("Ollama pull", StringComparison.OrdinalIgnoreCase)
                    || item.Source.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiLogRenderer.Append(_installLog, item.Source, item.Message);
                }

                var plain = Regex.Replace(item.Message, @"\x1B\[[0-9;]*m", "");
                diskLines.Add(
                    $"{DateTime.Now:HH:mm:ss} [{item.Source}] {plain}");

                processed++;
            }

            if (diskLines.Count > 0)
                _ = AppendLogFileBatchAsync(diskLines);
        }
        finally
        {
            Interlocked.Exchange(ref _logFlushBusy, 0);
        }
    }

    private async Task AppendLogFileBatchAsync(IReadOnlyCollection<string> lines)
    {
        if (lines.Count == 0)
            return;

        try
        {
            await _logFileGate.WaitAsync();

            try
            {
                await File.AppendAllLinesAsync(
                    Path.Combine(
                        PortablePaths.LogsDir,
                        $"{DateTime.Now:yyyy-MM-dd}.log"),
                    lines);
            }
            finally
            {
                _logFileGate.Release();
            }
        }
        catch
        {
            // Le journal disque ne doit jamais ralentir l'interface.
        }
    }

    private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tabs.TabCount)
            return;

        var page = _tabs.TabPages[e.Index];
        var enabled = page.Enabled;
        var selected = enabled && e.Index == _tabs.SelectedIndex;
        var bounds = e.Bounds;

        var back = !enabled
            ? Color.FromArgb(27, 29, 34)
            : selected
                ? AppTheme.Primary
                : AppTheme.SurfaceAlt;

        var fore = !enabled
            ? AppTheme.TextDim
            : selected
                ? Color.White
                : AppTheme.TextMuted;

        using var backBrush = new SolidBrush(back);
        using var textBrush = new SolidBrush(fore);

        e.Graphics.FillRectangle(backBrush, bounds);

        var text = page.Text;
        var compactTabs = bounds.Width < 96;
        var font = compactTabs
            ? new Font(
                Font.FontFamily,
                8F,
                selected ? FontStyle.Bold : Font.Style,
                GraphicsUnit.Point)
            : selected
                ? new Font(Font, FontStyle.Bold)
                : Font;

        try
        {
            var size = e.Graphics.MeasureString(text, font);
            var x = bounds.Left + (bounds.Width - size.Width) / 2f;
            var y = bounds.Top + (bounds.Height - size.Height) / 2f;
            e.Graphics.DrawString(text, font, textBrush, x, y);
        }
        finally
        {
            if (!ReferenceEquals(font, Font))
                font.Dispose();
        }
    }

    private void UpdateGpu()
    {
        try
        {
            var psi = new ProcessStartInfo(
                "nvidia-smi",
                "--query-gpu=name,driver_version,memory.used,memory.total --format=csv,noheader,nounits")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var p = Process.Start(psi);
            var text = p?.StandardOutput.ReadToEnd().Trim();
            p?.WaitForExit(800);

            _gpu.Text = string.IsNullOrWhiteSpace(text)
                ? "GPU NVIDIA : non détecté"
                : "GPU : " + text;
        }
        catch
        {
            _gpu.Text = "GPU NVIDIA : non détecté";
        }
    }

    private static async Task WaitForPortAsync(
        int port,
        TimeSpan timeout,
        Func<bool>? processRunning = null,
        string? serviceName = null)
    {
        var until = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < until)
        {
            var p = await PortablePreflight.InspectPortAsync(port);
            if (p.Open)
                return;

            if (processRunning is not null && !processRunning())
            {
                throw new InvalidOperationException(
                    $"{serviceName ?? "Le service"} s'est arrêté avant l'ouverture du port {port}.");
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"Le port {port} ne s'est pas ouvert après {timeout.TotalSeconds:0} secondes.");
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closing) return;

        Log(
            "UI",
            $"Fermeture demandée · raison={e.CloseReason}.");

        _closing = true;
        e.Cancel = true;

        try
        {
            _logFlushTimer.Stop();
            FlushPendingLogs();

            _installCts?.Cancel();

            if (_installTask is not null)
            {
                try { await _installTask; }
                catch (OperationCanceledException) { }
                catch (Exception ex) { CrashLog.Write("Install shutdown", ex); }
            }

            await StopAllAsync();
        }
        catch (Exception ex)
        {
            CrashLog.Write("Shutdown", ex);
        }
        finally
        {
            try { _updater.Dispose(); } catch { }

            e.Cancel = false;
            Close();
        }
    }

    private void Ui(Action action)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
            BeginInvoke(action);
        else
            action();
    }

    // =====================================================================
    // Shared designer control
    // =====================================================================
    internal sealed class FocusPanel : Panel
    {
        public FocusPanel()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }
    }

    // =====================================================================
    // MainForm.Enhancements
    // =====================================================================
    private readonly Dictionary<string, RichTextBox> _logViews =
        new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _benchmarkCts;
    private int _tabActivationBusy;
    private bool _virtualMemoryWarningLogged;

    private void InitializeEnhancedUi()
    {
        InitializeLogTabs();
        InitializeModelSelectors();
        InitializeNegativePromptEditor();
        InitializePromptEnhancerUi();
        InitializeVideoUi();
        InitializeGenerationExperienceUi();
        InitializeSettingsExperienceUi();
        InitializeV36Ui();
        InitializeV37Ui();
        ForceWhiteButtonText(this);
        EnableSmoothTabPainting();

        _tabs.SelectedIndexChanged += MainTabs_SelectedIndexChanged;

        AnsiLogRenderer.Append(
            _ollamaLog,
            "UI",
            "Console Ollama prête. Sélectionnez cet onglet pour démarrer/afficher Ollama.");
    }

    private void InitializeLogTabs()
    {
        _logViews.Clear();
        _logViews["ui"] = _logUi;
        _logViews["opencode"] = _logOpenCode;
        _logViews["ollama"] = _logOllama;
        _logViews["comfyui"] = _logComfy;
        _logViews["flux"] = _logFlux;
        _logViews["video"] = _logVideo;
        _logViews["install"] = _logInstall;
        _logViews["system"] = _logSystem;

        AppTheme.ApplyDark(_logTabs);
    }



    private void AppendCategorizedLog(string source, string message)
    {
        var key =
            source.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase)
                ? "ollama"
                : source.StartsWith("ComfyUI", StringComparison.OrdinalIgnoreCase)
                    ? "comfyui"
                    : source.StartsWith("FLUX", StringComparison.OrdinalIgnoreCase)
                        ? "flux"
                        : source.StartsWith("Video", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("Vidéo", StringComparison.OrdinalIgnoreCase)
                            ? "video"
                        : source.StartsWith("Install", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("Ollama pull", StringComparison.OrdinalIgnoreCase)
                            ? "install"
                            : source.StartsWith("OpenCode", StringComparison.OrdinalIgnoreCase)
                              || source.StartsWith("Proxy", StringComparison.OrdinalIgnoreCase)
                              || source.StartsWith("API", StringComparison.OrdinalIgnoreCase)
                                ? "opencode"
                                : source.StartsWith("UI", StringComparison.OrdinalIgnoreCase)
                                  || source.StartsWith("WebView", StringComparison.OrdinalIgnoreCase)
                                    ? "ui"
                                    : "system";

        if (_logViews.TryGetValue(key, out var box))
            AnsiLogRenderer.Append(box, source, message);
    }

    private void InitializeModelSelectors()
    {
        ConfigureDesignerModelCombo(
            _visionModelCombo,
            txtVisionModel,
            _s.VisionModel,
            new[] { "qwen3-vl:8b" });

        ConfigureDesignerModelCombo(
            _fluxModelCombo,
            txtFluxModel,
            _s.FluxModel,
            new[] { "flux-2-klein-4b-fp8.safetensors" });

        ConfigureDesignerModelCombo(
            _textEncoderCombo,
            txtTextEncoder,
            _s.TextEncoderModel,
            new[] { "qwen_3_4b.safetensors" });

        ConfigureDesignerModelCombo(
            _vaeCombo,
            txtVae,
            _s.VaeModel,
            new[] { "flux2-vae.safetensors" });
    }

    private static void ConfigureDesignerModelCombo(
        ComboBox combo,
        TextBox placeholder,
        string current,
        IEnumerable<string> defaults)
    {
        combo.Bounds = placeholder.Bounds;
        combo.Anchor = placeholder.Anchor;
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        combo.BackColor = AppTheme.Input;
        combo.ForeColor = AppTheme.Text;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = placeholder.Font;
        combo.DropDownWidth = 900;

        AddUniqueItems(combo, defaults);
        AddUniqueItems(combo, new[] { current });
        combo.Text = current;

        placeholder.Visible = false;
        combo.Visible = true;
        combo.BringToFront();
    }


    private static void AddUniqueItems(ComboBox combo, IEnumerable<string> values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!combo.Items.Cast<object>().Any(
                    x => string.Equals(
                        x?.ToString(),
                        value,
                        StringComparison.OrdinalIgnoreCase)))
            {
                combo.Items.Add(value);
            }
        }
    }

    private void InitializeNegativePromptEditor()
    {
        _negativePrompt.Text = _s.NegativePrompt;

        _prompt.Height = 178;



        tabGenerate.Controls.Add(_negativePromptLabel);
        tabGenerate.Controls.Add(_negativePrompt);
        _negativePromptLabel.BringToFront();
        _negativePrompt.BringToFront();
    }

    private void InitializePromptEnhancerUi()
    {
        _autoImprovePrompt.Checked = _s.AutoImprovePrompt;
        _promptModelCombo.Text = _s.PromptModel;
        _seedInput.Value = Math.Clamp(
            (decimal)Math.Max(1L, _s.GenerationSeed),
            _seedInput.Minimum,
            _seedInput.Maximum);
        _randomSeedCheck.Checked = _s.UseRandomSeed;

        _improvePromptButton.FlatAppearance.BorderColor = AppTheme.Border;
        _improvePromptButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(_s.Language, "Amélioration du prompt", "Prompt enhancement"),
                () => RunGpuExclusiveAsync("Amélioration du prompt", ImprovePromptFromUiAsync));



        AddUniqueItems(
            _promptModelCombo,
            new[] { _s.PromptModel, "qwen3:1.7b", "qwen3:4b" });

        // Make room for the dedicated prompt-model selector.
        _prompt.Location = new Point(18, 78);
        _prompt.Size = new Size(330, 130);

        _negativePromptLabel.Location = new Point(18, 215);
        _negativePrompt.Location = new Point(18, 238);
        _negativePrompt.Size = new Size(330, 63);



        _seedInput.Enabled = !_randomSeedCheck.Checked;
        _randomSeedCheck.CheckedChanged += (_, _) =>
            _seedInput.Enabled = !_randomSeedCheck.Checked;

        _benchmarkButton.FlatAppearance.BorderColor = AppTheme.Border;
        _benchmarkButton.Click += async (_, _) =>
        {
            if (_benchmarkCts is not null)
            {
                _benchmarkCts.Cancel();
                return;
            }

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Benchmark prompts", "Prompt benchmark"),
                () => RunGpuExclusiveAsync("Benchmark prompts", RunPromptBenchmarkAsync));
        };

        tabGenerate.Controls.Add(_improvePromptButton);
        tabGenerate.Controls.Add(_autoImprovePrompt);
        tabGenerate.Controls.Add(_promptModelLabel);
        tabGenerate.Controls.Add(_promptModelCombo);
        tabGenerate.Controls.Add(_seedLabel);
        tabGenerate.Controls.Add(_seedInput);
        tabGenerate.Controls.Add(_randomSeedCheck);
        tabGenerate.Controls.Add(_benchmarkButton);

        _improvePromptButton.BringToFront();
        _autoImprovePrompt.BringToFront();
        _promptModelLabel.BringToFront();
        _promptModelCombo.BringToFront();
        _seedLabel.BringToFront();
        _seedInput.BringToFront();
        _randomSeedCheck.BringToFront();
        _benchmarkButton.BringToFront();

        _prompt.TextChanged += (_, _) => UpdatePromptEnhancementState();
        UpdatePromptEnhancementState();
    }

    private async Task ImprovePromptFromUiAsync()
    {
        var source = _prompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Saisissez d'abord une idée ou un prompt.",
                    "Enter an idea or prompt first."));

        if (IsPromptAlreadyImproved(source))
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Ce prompt a déjà été amélioré. Modifiez-le avant de relancer l'amélioration.",
                    "This prompt has already been enhanced. Edit it before enhancing again."));

        _improvePromptButton.Enabled = false;
        try
        {
            var improved = await ImprovePromptTextAsync(
                source,
                CancellationToken.None);

            _prompt.Text = improved;
            _s.LastImprovedPromptHash = PromptFingerprint(improved);
            SettingsStore.Save(_s);
            _prompt.Focus();
            _prompt.SelectionStart = _prompt.TextLength;

            Log("UI", "Prompt FLUX.2 amélioré localement via Ollama.");
        }
        finally
        {
            UpdatePromptEnhancementState();
        }
    }

    private bool IsPromptAlreadyImproved(string text) =>
        !string.IsNullOrWhiteSpace(_s.LastImprovedPromptHash) &&
        string.Equals(
            _s.LastImprovedPromptHash,
            PromptFingerprint(text),
            StringComparison.OrdinalIgnoreCase);

    private void UpdatePromptEnhancementState()
    {
        if (_improvePromptButton is null || _prompt is null)
            return;

        var text = _prompt.Text.Trim();
        var alreadyImproved =
            !string.IsNullOrWhiteSpace(text) &&
            IsPromptAlreadyImproved(text);

        _improvePromptButton.Enabled =
            !string.IsNullOrWhiteSpace(text) &&
            !alreadyImproved;

        _improvePromptButton.Text = alreadyImproved
            ? L10n.Pick(_s.Language, "✓ Déjà amélioré", "✓ Already enhanced")
            : L10n.Pick(_s.Language, "✨ Améliorer", "✨ Enhance");
    }

    private static string PromptFingerprint(string text)
    {
        var normalized = string.Join(
            " ",
            text.Trim()
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private async Task<string> ImprovePromptTextAsync(
        string source,
        CancellationToken ct,
        string? modelOverride = null,
        bool videoPrompt = false)
    {
        await PrepareGpuForPromptEnhancementAsync(ct);
        await StartOllamaAsync();

        var model =
            string.IsNullOrWhiteSpace(modelOverride)
                ? CurrentComboModel(_promptModelCombo)
                : modelOverride.Trim();

        if (string.IsNullOrWhiteSpace(model))
            model = _s.PromptModel;

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        var installedModels = new List<string>();

        using (var tagsResponse = await http.GetAsync(
                   $"http://127.0.0.1:{_s.OllamaPort}/api/tags",
                   ct))
        {
            tagsResponse.EnsureSuccessStatusCode();
            var rawTags = await tagsResponse.Content.ReadAsStringAsync(ct);
            using var tagsDoc = JsonDocument.Parse(rawTags);

            if (tagsDoc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var item in models.EnumerateArray())
                {
                    if (!item.TryGetProperty("name", out var name))
                        continue;

                    var installedName = name.GetString();
                    if (!string.IsNullOrWhiteSpace(installedName))
                        installedModels.Add(installedName);
                }
            }
        }

        var preferredInstalled = installedModels.FirstOrDefault(name =>
            string.Equals(
                name,
                model,
                StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(preferredInstalled))
        {
            model = preferredInstalled;
        }
        else if (string.IsNullOrWhiteSpace(modelOverride))
        {
            var fallback = installedModels.FirstOrDefault(name =>
                !name.Contains(
                    "embed",
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(fallback))
            {
                throw new InvalidOperationException(
                    L10n.Pick(
                        _s.Language,
                        "Aucun modèle de chat local n'est installé dans Ollama.",
                        "No local chat model is installed in Ollama."));
            }

            Log(
                "Ollama",
                $"Amélioration du prompt : {model} indisponible, utilisation de {fallback}.");
            model = fallback;
        }
        else
        {
            throw new InvalidOperationException(
                $"Modèle Ollama requis pour le benchmark non installé : {model}");
        }

        var system = videoPrompt
            ? "You are a professional prompt engineer for Wan 2.1 video generation. " +
              "Return ONLY valid JSON with exactly one string property named prompt. " +
              "Rewrite the user's idea into one precise, coherent English video prompt of roughly 60 to 110 useful words. " +
              "Preserve subject, identity, action, count, setting, era, colors and explicit constraints. " +
              "Describe subject motion, camera motion, framing, lens perspective, lighting, atmosphere, temporal continuity and a clear beginning-to-end shot progression when relevant. " +
              "Prefer one coherent shot unless the user explicitly asks for cuts. Avoid contradictory motions, impossible camera moves and quality-token spam. " +
              "Do not invent extra people or conflicting objects. Preserve quoted visible text exactly. Do not explain your reasoning."
            : "You are a professional prompt engineer for FLUX.2 Klein image generation. " +
              "Return ONLY valid JSON with exactly one string property named prompt. " +
              "Rewrite the user's idea into one precise, coherent image prompt of roughly 50 to 90 useful English words. " +
              "Preserve subject, identity, actions, count, setting, era, colors and explicit constraints. " +
              "Add useful composition, framing, lighting, lens/camera language when relevant, materials, textures, atmosphere, depth and physically coherent detail. " +
              "Do not invent a different era, extra people or conflicting objects. " +
              "Avoid quality-token spam such as masterpiece, best quality, 8k, ultra HD. " +
              "Preserve quoted visible text exactly. Do not explain your reasoning.";

        var payload = JsonSerializer.Serialize(new
        {
            model,
            stream = false,
            think = false,
            format = "json",
            options = new
            {
                temperature = 0.25,
                num_predict = 240
            },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = source }
            }
        });

        using var response = await http.PostAsync(
            $"http://127.0.0.1:{_s.OllamaPort}/api/chat",
            new StringContent(
                payload,
                System.Text.Encoding.UTF8,
                "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Ollama HTTP {(int)response.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var content =
            doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var value)
                ? value.GetString()?.Trim()
                : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Ollama n'a retourné aucun prompt amélioré.",
                    "Ollama returned no improved prompt."));
        }

        try
        {
            using var contentDoc = JsonDocument.Parse(content);
            if (contentDoc.RootElement.TryGetProperty("prompt", out var promptNode))
            {
                var prompt = promptNode.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    Log("Ollama", $"Prompt amélioré avec {model}.");
                    return prompt;
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to the guarded plain-text fallback below.
        }

        var looksLikeReasoning =
            content.Contains("We are given", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Steps:", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Let's tackle", StringComparison.OrdinalIgnoreCase) ||
            content.Length > 1600;

        if (looksLikeReasoning)
        {
            throw new InvalidOperationException(
                $"Le modèle {model} n'a pas respecté le format JSON du prompt.");
        }

        Log("Ollama", $"Prompt amélioré avec {model} (fallback texte).");
        return content.Trim().Trim('"');
    }

    private async Task PrepareGpuForPromptEnhancementAsync(
        CancellationToken ct)
    {
        if (_comfy.Running)
        {
            Log(
                "ComfyUI",
                "Arrêt temporaire avant amélioration du prompt pour libérer la VRAM.");
            await _comfy.StopAsync();
        }
        else
        {
            await EnsurePortableServicePortFreeAsync(
                _s.ComfyPort,
                "ComfyUI",
                _s.ComfyPython);
        }

        await Task.Delay(1000, ct);
    }

    private async Task<HashSet<string>> GetInstalledOllamaModelsAsync(
        CancellationToken ct)
    {
        await StartOllamaAsync();

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var raw = await http.GetStringAsync(
            $"http://127.0.0.1:{_s.OllamaPort}/api/tags",
            ct);

        using var doc = JsonDocument.Parse(raw);
        var result = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (doc.RootElement.TryGetProperty("models", out var models))
        {
            foreach (var item in models.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var name))
                    continue;

                var value = name.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }
        }

        return result;
    }

    private sealed record PromptBenchmarkRow(
        string Category,
        string Model,
        long Seed,
        string SourcePrompt,
        string ImprovedPrompt,
        double PromptSeconds,
        double FluxSeconds,
        double TotalSeconds,
        string ImagePath);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(
        [In, Out] MemoryStatusEx buffer);

    private static long GetAvailableCommitMiB()
    {
        var status = new MemoryStatusEx();
        return GlobalMemoryStatusEx(status)
            ? (long)(status.AvailPageFile / (1024UL * 1024UL))
            : long.MaxValue;
    }

    private static long GetConfiguredPageFileMiB()
    {
        try
        {
            using var key =
                Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");

            var raw = key?.GetValue("PagingFiles");
            var entries = raw switch
            {
                string one => new[] { one },
                string[] many => many,
                _ => Array.Empty<string>()
            };

            long totalMaximumMiB = 0;
            var foundExplicitMaximum = false;

            foreach (var entry in entries)
            {
                var parts = entry.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

                if (parts.Length < 3 ||
                    !long.TryParse(parts[^1], out var maximumMiB))
                {
                    continue;
                }

                foundExplicitMaximum = true;

                // "0 0" means Windows/system-managed sizing.
                if (maximumMiB == 0)
                    return 0;

                totalMaximumMiB += maximumMiB;
            }

            if (foundExplicitMaximum)
                return totalMaximumMiB;
        }
        catch
        {
            // Fall back to the currently allocated commit extension.
        }

        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
            return 0;

        var totalCommit =
            (long)(status.TotalPageFile / (1024UL * 1024UL));
        var physical =
            (long)(status.TotalPhys / (1024UL * 1024UL));

        return Math.Max(0, totalCommit - physical);
    }

    private string BuildVirtualMemoryGuidance(
        string component,
        long availableCommitMiB)
    {
        var pageFileMiB = GetConfiguredPageFileMiB();

        var pageFileText =
            pageFileMiB > 0
                ? $"{pageFileMiB / 1024.0:0.0} Go maximum"
                : L10n.Pick(
                    _s.Language,
                    "géré par Windows / taille dynamique",
                    "Windows-managed / dynamic size");

        return L10n.Pick(
            _s.Language,
            $"{component} ne peut pas être démarré avec une marge de mémoire engagée suffisante. " +
            $"Commit disponible : {availableCommitMiB / 1024.0:0.0} Go. " +
            $"Fichier de pagination : {pageFileText}. " +
            "DreamRaster évite maintenant de charger Ollama et ComfyUI en même temps. " +
            "Pour les charges FLUX/Wan/Qwen, utilisez de préférence un fichier de pagination géré par Windows " +
            "ou un maximum d'au moins 16 Go.",
            $"{component} cannot start with enough committed-memory headroom. " +
            $"Available commit: {availableCommitMiB / 1024.0:0.0} GB. " +
            $"Page file: {pageFileText}. " +
            "DreamRaster now avoids loading Ollama and ComfyUI at the same time. " +
            "For FLUX/Wan/Qwen workloads, prefer a Windows-managed page file " +
            "or a maximum of at least 16 GB.");
    }

    private async Task WaitForCommitRecoveryAsync(
        long minimumMiB,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        var lastAvailable = GetAvailableCommitMiB();

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            lastAvailable = GetAvailableCommitMiB();
            if (lastAvailable >= minimumMiB)
                return;

            await Task.Delay(500, ct);
        }

        throw new InvalidOperationException(
            BuildVirtualMemoryGuidance(
                "DreamRaster",
                lastAvailable));
    }

    private void LogVirtualMemoryWarningIfNeeded()
    {
        if (_virtualMemoryWarningLogged)
            return;

        _virtualMemoryWarningLogged = true;

        var pageFileMiB = GetConfiguredPageFileMiB();
        if (pageFileMiB <= 0 || pageFileMiB >= 8192)
            return;

        Log(
            "Système !",
            L10n.Pick(
                _s.Language,
                $"Fichier de pagination Windows limité à {pageFileMiB / 1024.0:0.0} Go max. " +
                "FLUX.2, Wan et Qwen peuvent atteindre la limite de mémoire engagée. " +
                "DreamRaster empêchera les chevauchements Ollama/ComfyUI ; " +
                "un fichier de pagination géré par Windows ou >= 16 Go est recommandé.",
                $"Windows page file is limited to {pageFileMiB / 1024.0:0.0} GB max. " +
                "FLUX.2, Wan and Qwen can hit the commit limit. " +
                "DreamRaster will prevent Ollama/ComfyUI overlap; " +
                "a Windows-managed page file or >= 16 GB is recommended."));
    }

    private async Task CleanupBenchmarkComfyAsync(
        CancellationToken ct)
    {
        try
        {
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            // A previous DreamRaster process can leave its portable ComfyUI alive.
            // Ensure the tracked port is really released before the next case.
            await EnsurePortableServicePortFreeAsync(
                _s.ComfyPort,
                "ComfyUI",
                _s.ComfyPython);
        }

        var deadline = DateTime.UtcNow.AddSeconds(30);
        const long desiredFreeCommitMiB = 6144;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var available = GetAvailableCommitMiB();
            if (available >= desiredFreeCommitMiB)
            {
                Log(
                    "Système",
                    $"Benchmark : ComfyUI arrêté · commit disponible {available:N0} MiB.");
                return;
            }

            await Task.Delay(500, ct);
        }

        Log(
            "Système !",
            $"Benchmark : commit disponible seulement {GetAvailableCommitMiB():N0} MiB après nettoyage.");
    }

    private async Task RunPromptBenchmarkAsync()
    {
        var answer = MessageBox.Show(
            L10n.Pick(
                _s.Language,
                "Ce test va générer 8 images FLUX.2 (4 catégories × 2 modèles Ollama) avec des seeds fixes. " +
                "Il peut prendre plusieurs minutes et solliciter fortement le GPU.\n\nContinuer ?",
                "This test will generate 8 FLUX.2 images (4 categories × 2 Ollama models) with fixed seeds. " +
                "It may take several minutes and heavily use the GPU.\n\nContinue?"),
            L10n.Pick(_s.Language, "Comparatif IA prompt", "Prompt AI comparison"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        var pageFileMiB = GetConfiguredPageFileMiB();
        if (pageFileMiB > 0 && pageFileMiB < 8192)
        {
            var memoryAnswer = MessageBox.Show(
                L10n.Pick(
                    _s.Language,
                    $"La mémoire virtuelle Windows semble limitée à environ {pageFileMiB / 1024.0:0.0} Go. " +
                    "Le benchmark FLUX.2 peut atteindre la limite de mémoire engagée même si de la RAM physique reste libre. " +
                    "DreamRaster arrêtera ComfyUI entre chaque image pour réduire ce risque.\n\nContinuer ?",
                    $"Windows virtual memory appears limited to about {pageFileMiB / 1024.0:0.0} GB. " +
                    "The FLUX.2 benchmark can hit the commit limit even while physical RAM is still available. " +
                    "DreamRaster will fully stop ComfyUI between images to reduce this risk.\n\nContinue?"),
                L10n.Pick(
                    _s.Language,
                    "Mémoire virtuelle limitée",
                    "Limited virtual memory"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (memoryAnswer != DialogResult.Yes)
                return;
        }

        _benchmarkCts = new CancellationTokenSource();
        var ct = _benchmarkCts.Token;

        _benchmarkButton.Text =
            L10n.Pick(_s.Language, "Annuler benchmark", "Cancel benchmark");
        btnGenerate.Enabled = false;
        _improvePromptButton.Enabled = false;

        var originalPrompt = _prompt.Text;
        var originalModel = CurrentComboModel(_promptModelCombo);

        try
        {
            var installed = await GetInstalledOllamaModelsAsync(ct);
            var models = new[] { "qwen3:1.7b", "qwen3:4b" };

            var missing = models
                .Where(model => !installed.Contains(model))
                .ToArray();

            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Modèles Ollama requis absents : " +
                    string.Join(", ", missing));
            }

            var cases = new[]
            {
                (
                    Category: "Portrait",
                    Seed: 1001L,
                    Prompt: "portrait serré d'une femme rousse de 35 ans, lumière de fenêtre, fond sombre"),
                (
                    Category: "Paysage",
                    Seed: 1002L,
                    Prompt: "vallée alpine au lever du soleil après une nuit de neige, rivière au premier plan"),
                (
                    Category: "Architecture",
                    Seed: 1003L,
                    Prompt: "maison brutaliste en béton au bord d'un lac, grandes baies vitrées, temps couvert"),
                (
                    Category: "Cinematique",
                    Seed: 1004L,
                    Prompt: "détective seul traverse une rue mouillée la nuit, néons rouges et bleus, voiture ancienne au fond")
            };

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folder = Path.Combine(
                PortablePaths.Resolve(_s.Images),
                "benchmark_" + stamp);
            Directory.CreateDirectory(folder);

            var rows = new List<PromptBenchmarkRow>();
            var totalJobs = models.Length * cases.Length;
            var job = 0;

            foreach (var model in models)
            {
                foreach (var test in cases)
                {
                    ct.ThrowIfCancellationRequested();
                    job++;

                    await CleanupBenchmarkComfyAsync(ct);

                    _genText.Text =
                        $"Benchmark {job}/{totalJobs} · {test.Category} · {model}";
                    _prompt.Text = test.Prompt;
                    _seedInput.Value = test.Seed;
                    _randomSeedCheck.Checked = false;

                    var totalWatch = Stopwatch.StartNew();

                    var promptWatch = Stopwatch.StartNew();
                    var improved = await ImprovePromptTextAsync(
                        test.Prompt,
                        ct,
                        model);
                    promptWatch.Stop();

                    _prompt.Text = improved;

                    var fluxWatch = Stopwatch.StartNew();
                    ImageGenerationResult result;
                    try
                    {
                        result = await _generator.GenerateAsync(
                            improved,
                            _negativePrompt.Text.Trim(),
                            _s.DefaultWidth,
                            _s.DefaultHeight,
                            inputImagePath: null,
                            imgToImgStrength: 1.0,
                            ct,
                            test.Seed);
                    }
                    finally
                    {
                        fluxWatch.Stop();
                        await CleanupBenchmarkComfyAsync(ct);
                    }

                    totalWatch.Stop();

                    if (!result.Ok ||
                        string.IsNullOrWhiteSpace(result.Path) ||
                        !File.Exists(result.Path))
                    {
                        throw new InvalidOperationException(
                            result.Error ??
                            $"Image benchmark absente : {test.Category} / {model}");
                    }

                    var safeModel = model
                        .Replace(":", "_")
                        .Replace("/", "_");
                    var destination = Path.Combine(
                        folder,
                        $"{test.Seed}_{test.Category}_{safeModel}.png");
                    File.Copy(result.Path, destination, true);
                    ShowPreviewImage(destination);

                    rows.Add(new PromptBenchmarkRow(
                        test.Category,
                        model,
                        test.Seed,
                        test.Prompt,
                        improved,
                        Math.Round(promptWatch.Elapsed.TotalSeconds, 3),
                        Math.Round(fluxWatch.Elapsed.TotalSeconds, 3),
                        Math.Round(totalWatch.Elapsed.TotalSeconds, 3),
                        destination));

                    Log(
                        "UI",
                        $"Benchmark {test.Category}/{model} : " +
                        $"prompt {promptWatch.Elapsed.TotalSeconds:0.00}s · " +
                        $"FLUX {fluxWatch.Elapsed.TotalSeconds:0.00}s · " +
                        $"total {totalWatch.Elapsed.TotalSeconds:0.00}s.");
                }
            }

            var jsonPath = Path.Combine(folder, "benchmark.json");
            await File.WriteAllTextAsync(
                jsonPath,
                JsonSerializer.Serialize(
                    rows,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                ct);

            var csvPath = Path.Combine(folder, "benchmark.csv");
            var csvLines = new List<string>
            {
                "category;model;seed;prompt_seconds;flux_seconds;total_seconds;image"
            };
            csvLines.AddRange(rows.Select(row =>
                string.Join(
                    ";",
                    row.Category,
                    row.Model,
                    row.Seed,
                    row.PromptSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.FluxSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.TotalSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.ImagePath.Replace(";", "_"))));
            await File.WriteAllLinesAsync(csvPath, csvLines, ct);

            var summaries = models.Select(model =>
            {
                var modelRows = rows
                    .Where(row => row.Model.Equals(
                        model,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                return
                    $"{model} : prompt {modelRows.Average(x => x.PromptSeconds):0.00}s · " +
                    $"FLUX {modelRows.Average(x => x.FluxSeconds):0.00}s · " +
                    $"total {modelRows.Average(x => x.TotalSeconds):0.00}s";
            });

            _genText.Text = L10n.Pick(
                _s.Language,
                "Benchmark terminé.",
                "Benchmark complete.");

            MessageBox.Show(
                string.Join(Environment.NewLine, summaries) +
                Environment.NewLine + Environment.NewLine +
                folder,
                L10n.Pick(_s.Language, "Benchmark terminé", "Benchmark complete"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        finally
        {
            try
            {
                await CleanupBenchmarkComfyAsync(
                    CancellationToken.None);
            }
            catch
            {
            }

            _prompt.Text = originalPrompt;

            if (!string.IsNullOrWhiteSpace(originalModel))
                _promptModelCombo.Text = originalModel;

            _benchmarkCts?.Dispose();
            _benchmarkCts = null;
            _benchmarkButton.Text = L10n.Pick(
                _s.Language,
                "Comparer 1.7B / 4B",
                "Compare 1.7B / 4B");
            btnGenerate.Enabled = true;
            UpdatePromptEnhancementState();
        }
    }

    private void ApplyEnhancedTranslations()
    {
        if (_negativePromptLabel is null)
            return;

        _negativePromptLabel.Text = L10n.Pick(
            _s.Language,
            "Négatif / éléments à éviter",
            "Negative / things to avoid");

        _improvePromptButton.Text = L10n.Pick(
            _s.Language,
            "✨ Améliorer",
            "✨ Improve");
        _autoImprovePrompt.Text = "Auto";
        _autoImprovePrompt.AccessibleDescription = L10n.Pick(
            _s.Language,
            "Améliorer automatiquement le prompt avec le modèle Ollama sélectionné avant chaque génération.",
            "Automatically improve the prompt with the selected Ollama model before each generation.");
        _promptModelLabel.Text = L10n.Pick(
            _s.Language,
            "IA prompt",
            "Prompt AI");
        _seedLabel.Text = "Seed";
        _randomSeedCheck.Text = L10n.Pick(
            _s.Language,
            "Rnd",
            "Rnd");
        if (_benchmarkCts is null)
        {
            _benchmarkButton.Text = L10n.Pick(
                _s.Language,
                "Comparer 1.7B / 4B",
                "Compare 1.7B / 4B");
        }

        if (_logTabs is not null && _logTabs.TabPages.Count >= 9)
        {
            _logTabs.TabPages[0].Text = L10n.Pick(_s.Language, "Tous", "All");
            _logTabs.TabPages[1].Text = "UI";
            _logTabs.TabPages[2].Text = "OpenCode";
            _logTabs.TabPages[3].Text = "Ollama";
            _logTabs.TabPages[4].Text = "ComfyUI";
            _logTabs.TabPages[5].Text = "FLUX.2";
            _logTabs.TabPages[6].Text = L10n.Pick(_s.Language, "Vidéo", "Video");
            _logTabs.TabPages[7].Text = L10n.Pick(_s.Language, "Installation", "Installation");
            _logTabs.TabPages[8].Text = L10n.Pick(_s.Language, "Système", "System");
        }

        ApplyVideoTranslations();
        ApplyGenerationExperienceTranslations();

        lblConfigHint.Text =
            L10n.T(_s.Language, "config.hint") + Environment.NewLine +
            L10n.Pick(
                _s.Language,
                "Les listes de modèles sont éditables : vous pouvez saisir le nom exact d'un modèle local personnalisé ou non filtré, à condition qu'il soit compatible avec le moteur sélectionné.",
                "Model lists are editable: you may enter the exact name of a custom or unfiltered local model, provided it is compatible with the selected engine.") +
            Environment.NewLine +
            L10n.Pick(
                _s.Language,
                "Validation : [OK] compatible/installé · [?] détecté mais non vérifié · [X] incompatible, manquant ou non installé.",
                "Validation: [OK] compatible/installed · [?] detected but unverified · [X] incompatible, missing or not installed.");
    }

    private async void MainTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshSelectedTab();

        if (Interlocked.Exchange(ref _tabActivationBusy, 1) != 0)
            return;

        try
        {
            if (_tabs.SelectedTab == tabComfy && !_closing)
            {
                await SafeUiAsync("ComfyUI", OpenComfyEmbeddedAsync);
            }
            else if (_tabs.SelectedTab == tabOpenCode && !_closing)
            {
                await SafeUiAsync("OpenCode", OpenEmbeddedAsync);
            }
            else if (_tabs.SelectedTab == tabOllama && !_closing)
            {
                if (_comfy.Running &&
                    (_generator.IsBusy || _videoCts is not null))
                {
                    var message =
                        L10n.Pick(
                            _s.Language,
                            "Ollama n'est pas démarré pendant une génération FLUX/Wan active afin d'éviter une saturation de la mémoire virtuelle.",
                            "Ollama is not started during an active FLUX/Wan generation to avoid exhausting virtual memory.");

                    Log("Ollama", message);
                    AnsiLogRenderer.Append(
                        _ollamaLog,
                        "Ollama",
                        message);
                }
                else
                {
                    await SafeUiAsync("Ollama", StartOllamaAsync);
                    await RefreshModelChoicesAsync();
                }
            }
            else if (_tabs.SelectedTab == tabConfiguration && !_closing)
            {
                await RefreshModelChoicesAsync();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _tabActivationBusy, 0);
            RefreshSelectedTab();
        }
    }

    private void RefreshSelectedTab()
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(new Action(() =>
        {
            var page = _tabs.SelectedTab;
            _tabs.Invalidate(true);
            page?.Invalidate(true);
            _web.Invalidate();
            _comfyWeb.Invalidate();
            page?.Refresh();
            _tabs.Refresh();
        }));
    }

    private void EnableSmoothTabPainting()
    {
        try
        {
            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_tabs, true);
        }
        catch
        {
            // Cosmetic optimization only.
        }
    }

    private static void ForceWhiteButtonText(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Button button)
            {
                button.ForeColor = Color.White;
                button.UseVisualStyleBackColor = false;
            }

            if (control.HasChildren)
                ForceWhiteButtonText(control);
        }
    }

    private async Task RefreshModelChoicesAsync()
    {
        var comfyRoot = PortablePreflight.GetComfyRoot(_s);

        RefreshValidatedModelChoices(
            _fluxModelCombo,
            Path.Combine(comfyRoot, "models", "diffusion_models"),
            Flux2ModelRole.Diffusion,
            _s.FluxModel);

        RefreshValidatedModelChoices(
            _textEncoderCombo,
            Path.Combine(comfyRoot, "models", "text_encoders"),
            Flux2ModelRole.TextEncoder,
            _s.TextEncoderModel);

        RefreshValidatedModelChoices(
            _vaeCombo,
            Path.Combine(comfyRoot, "models", "vae"),
            Flux2ModelRole.Vae,
            _s.VaeModel);

        await RefreshVisionModelChoicesAsync();
    }

    private async Task RefreshVisionModelChoicesAsync()
    {
        var wanted = CurrentComboModel(_visionModelCombo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = _s.VisionModel;

        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var raw = await http.GetStringAsync(
                $"http://127.0.0.1:{_s.OllamaPort}/api/tags");

            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var value = name.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            installed.Add(value);
                    }
                }
            }
        }
        catch
        {
            // The portable Ollama service may legitimately be stopped.
            // The manifest check below still gives a reliable local state
            // for explicitly configured/default models.
        }

        var candidates = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase)
        {
            "qwen3-vl:8b"
        };

        if (!string.IsNullOrWhiteSpace(_s.VisionModel))
            candidates.Add(_s.VisionModel);

        var choices = candidates
            .Select(name =>
            {
                var isInstalled =
                    installed.Contains(name) ||
                    IsPortableOllamaModelInstalled(name);

                var state = isInstalled
                    ? ModelCompatibilityState.Compatible
                    : ModelCompatibilityState.Missing;

                var status = isInstalled
                    ? L10n.Pick(
                        _s.Language,
                        "installé dans Ollama portable",
                        "installed in portable Ollama")
                    : L10n.Pick(
                        _s.Language,
                        "non installé",
                        "not installed");

                var marker = isInstalled ? "[OK]" : "[X]";

                return new ModelChoice(
                    name,
                    state,
                    $"{marker} {name} — {status}");
            })
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _visionModelCombo.BeginUpdate();
        try
        {
            _visionModelCombo.Items.Clear();

            foreach (var choice in choices)
                _visionModelCombo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                _visionModelCombo.SelectedItem = selected;
            else
                _visionModelCombo.Text = wanted;
        }
        finally
        {
            _visionModelCombo.EndUpdate();
        }

        RefreshPromptModelChoices(installed);
    }

    private void RefreshPromptModelChoices(
        HashSet<string> installed)
    {
        var wanted = CurrentComboModel(_promptModelCombo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = _s.PromptModel;

        var candidates =
            new HashSet<string>(
                installed,
                StringComparer.OrdinalIgnoreCase)
            {
                "qwen3:1.7b",
                "qwen3:4b"
            };

        if (!string.IsNullOrWhiteSpace(_s.PromptModel))
            candidates.Add(_s.PromptModel);

        var choices = candidates
            .Select(name =>
            {
                var isInstalled =
                    installed.Contains(name) ||
                    IsPortableOllamaModelInstalled(name);

                return new ModelChoice(
                    name,
                    isInstalled
                        ? ModelCompatibilityState.Compatible
                        : ModelCompatibilityState.Missing,
                    (isInstalled ? "[OK] " : "[X] ") +
                    name +
                    " — " +
                    (isInstalled
                        ? L10n.Pick(
                            _s.Language,
                            "installé",
                            "installed")
                        : L10n.Pick(
                            _s.Language,
                            "non installé",
                            "not installed")));
            })
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _promptModelCombo.BeginUpdate();
        try
        {
            _promptModelCombo.Items.Clear();
            foreach (var choice in choices)
                _promptModelCombo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                _promptModelCombo.SelectedItem = selected;
            else
                _promptModelCombo.Text = wanted;
        }
        finally
        {
            _promptModelCombo.EndUpdate();
        }
    }

    private bool IsPortableOllamaModelInstalled(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return false;

        var colon = modelName.LastIndexOf(':');
        var repository = colon > 0
            ? modelName[..colon]
            : modelName;
        var tag = colon > 0 && colon < modelName.Length - 1
            ? modelName[(colon + 1)..]
            : "latest";

        var repositoryParts = repository
            .Split(
                new[] { '/', '\\' },
                StringSplitOptions.RemoveEmptyEntries);

        var manifest = Path.Combine(
            new[]
            {
                PortablePaths.Resolve(_s.OllamaModels),
                "manifests",
                "registry.ollama.ai",
                "library"
            }
            .Concat(repositoryParts)
            .Append(tag)
            .ToArray());

        return File.Exists(manifest);
    }

    private void RefreshValidatedModelChoices(
        ComboBox combo,
        string folder,
        Flux2ModelRole role,
        string configured)
    {
        var wanted = CurrentComboModel(combo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = configured;

        var files = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder)
                .Where(IsSupportedModelFile)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var choices = files
            .Select(path => CreateModelChoice(
                Path.GetFileName(path),
                Flux2ModelCompatibility.Validate(path, role)))
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!choices.Any(choice =>
                string.Equals(
                    choice.FileName,
                    configured,
                    StringComparison.OrdinalIgnoreCase)))
        {
            choices.Add(CreateModelChoice(
                configured,
                new ModelCompatibilityResult(
                    ModelCompatibilityState.Missing,
                    "fichier configuré absent du dossier de modèles")));
        }

        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            foreach (var choice in choices)
                combo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                combo.SelectedItem = selected;
            else
                combo.Text = wanted;
        }
        finally
        {
            combo.EndUpdate();
        }
    }

    private ModelChoice CreateModelChoice(
        string fileName,
        ModelCompatibilityResult result)
    {
        var status = result.State switch
        {
            ModelCompatibilityState.Compatible =>
                L10n.Pick(_s.Language, "compatible FLUX.2 Klein", "FLUX.2 Klein compatible"),
            ModelCompatibilityState.Unverified =>
                L10n.Pick(_s.Language, "détecté — non vérifié", "detected — unverified"),
            ModelCompatibilityState.Incompatible =>
                L10n.Pick(_s.Language, "incompatible FLUX.2 Klein", "FLUX.2 Klein incompatible"),
            ModelCompatibilityState.Missing =>
                L10n.Pick(_s.Language, "configuré — fichier manquant", "configured — file missing"),
            _ => "?"
        };

        var marker = result.State switch
        {
            ModelCompatibilityState.Compatible => "[OK]",
            ModelCompatibilityState.Unverified => "[?]",
            _ => "[X]"
        };

        return new(
            fileName,
            result.State,
            $"{marker} {fileName} — {status} ({result.Reason})");
    }

    private static int CompatibilityOrder(ModelCompatibilityState state)
        => state switch
        {
            ModelCompatibilityState.Compatible => 0,
            ModelCompatibilityState.Unverified => 1,
            ModelCompatibilityState.Incompatible => 2,
            ModelCompatibilityState.Missing => 3,
            _ => 4
        };

    private static bool IsSupportedModelFile(string path)
        => path.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".ckpt", StringComparison.OrdinalIgnoreCase);

    private static string CurrentComboModel(ComboBox combo)
    {
        if (combo.SelectedItem is ModelChoice choice)
            return choice.FileName;

        return combo.Text.Trim();
    }

    private static string SelectedModel(ComboBox combo, TextBox fallback)
    {
        var value = CurrentComboModel(combo);
        return string.IsNullOrWhiteSpace(value)
            ? fallback.Text.Trim()
            : value;
    }

    private void ValidateSelectedFlux2Models(
        string fluxModel,
        string textEncoderModel,
        string vaeModel)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var checks = new[]
        {
            (L10n.Pick(_s.Language, "Modèle FLUX", "FLUX model"),
                Path.Combine(root, "models", "diffusion_models", fluxModel),
                Flux2ModelRole.Diffusion),
            ("Text encoder",
                Path.Combine(root, "models", "text_encoders", textEncoderModel),
                Flux2ModelRole.TextEncoder),
            ("VAE",
                Path.Combine(root, "models", "vae", vaeModel),
                Flux2ModelRole.Vae)
        };

        foreach (var (label, path, role) in checks)
        {
            var result = Flux2ModelCompatibility.Validate(path, role);
            if (result.State == ModelCompatibilityState.Incompatible)
            {
                throw new InvalidOperationException(
                    $"{label} : {Path.GetFileName(path)} n'est pas compatible avec " +
                    $"le workflow FLUX.2 Klein.\n\n{result.Reason}");
            }
        }
    }

    private void SynchronizeWorkflowDefaults()
    {
        var width = _s.DefaultWidth;
        var height = _s.DefaultHeight;
        var steps = _s.DefaultSteps;
        var cfg = _s.ImageCfg;

        SynchronizeWorkflow(
            Path.Combine(PortablePaths.WorkflowsDir, PortablePaths.TextToImageWorkflowFile),
            width,
            height,
            steps,
            cfg,
            imgToImg: false);

        SynchronizeWorkflow(
            Path.Combine(PortablePaths.WorkflowsDir, PortablePaths.ImgToImgWorkflowFile),
            width,
            height,
            steps,
            cfg,
            imgToImg: true);

        Log(
            "UI",
            $"Workflows synchronisés : {width}x{height}, {steps} steps.");
    }

    private static void SynchronizeWorkflow(
        string path,
        int width,
        int height,
        int steps,
        double cfg,
        bool imgToImg)
    {
        if (!File.Exists(path))
            return;

        var workflow =
            JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidDataException("Workflow JSON invalide : " + path);

        SetWorkflowInputs(workflow, "CFGGuider", inputs =>
        {
            inputs["cfg"] = Math.Clamp(cfg, 0.1, 20.0);
        });

        SetWorkflowInputs(workflow, "Flux2Scheduler", inputs =>
        {
            inputs["steps"] = steps;

            if (!imgToImg)
            {
                inputs["width"] = width;
                inputs["height"] = height;
            }
        });

        if (!imgToImg)
        {
            SetWorkflowInputs(workflow, "EmptyFlux2LatentImage", inputs =>
            {
                inputs["width"] = width;
                inputs["height"] = height;
                inputs["batch_size"] = 1;
            });
        }
        else
        {
            SetWorkflowInputs(workflow, "ImageScale", inputs =>
            {
                inputs["upscale_method"] = "lanczos";
                inputs["width"] = width;
                inputs["height"] = height;
                inputs["crop"] = "center";
            });
        }

        File.WriteAllText(
            path,
            workflow.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void SetWorkflowInputs(
        JsonObject workflow,
        string classType,
        Action<JsonObject> update)
    {
        foreach (var entry in workflow)
        {
            if (entry.Value is not JsonObject node)
                continue;

            if (!string.Equals(
                    node["class_type"]?.GetValue<string>(),
                    classType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (node["inputs"] is JsonObject inputs)
                update(inputs);
        }
    }

    // =====================================================================
    // MainForm.GenerationExperience
    // =====================================================================
    private sealed record TemplateComboItem(string Id, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly ToolTip _generationTemplateTips = new();
    private bool _updatingGenerationTemplateUi;
    private void InitializeGenerationExperienceUi()
    {
        InitializeImageTemplateUi();
        InitializeVideoTemplateUi();
        InitializeInstallationVideoUi();

        _tabs.ShowToolTips = true;
        _tabs.Selecting += MainTabs_Selecting;

        _genText.AutoEllipsis = true;
        _videoStatus.AutoEllipsis = true;
        _generationTemplateTips.SetToolTip(_genText, _genText.Text);
        _generationTemplateTips.SetToolTip(_videoStatus, _videoStatus.Text);
        _genText.TextChanged += (_, _) =>
            _generationTemplateTips.SetToolTip(_genText, _genText.Text);
        _videoStatus.TextChanged += (_, _) =>
            _generationTemplateTips.SetToolTip(_videoStatus, _videoStatus.Text);

        RefreshImageHistory();
        RefreshVideoHistory();
        RefreshFeatureAvailability();
    }


    private void InitializeImageTemplateUi()
    {
        _preview.Location = new Point(370, 52);
        _preview.Size = new Size(666, 430);
        _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;



        FillImageTemplateCombos();
        UpdateMaximumQualitySharpnessUi();


        _imageHistoryLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _imageHistoryPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        tabGenerate.Controls.AddRange(
        [
            _imageStyleTemplateLabel,
            _imageStyleTemplateCombo,
            _imageNegativeTemplateLabel,
            _imageNegativeTemplateCombo,
            _imageHistoryLabel,
            _imageHistoryPanel
        ]);

        _imageSharpnessPreviewButton.Click += async (_, _) =>
        {
            if (_imageSharpnessComparisonPanel.Visible)
            {
                SetImageSharpnessComparisonVisible(false);
                return;
            }

            await RefreshImageSharpnessPreviewAsync(CancellationToken.None);
        };

        _imageSharpnessComparisonSlider.ValueChanged += (_, _) =>
            _imageSharpnessComparisonPanel.Invalidate();

        _imageSharpnessComparisonPanel.Paint +=
            DrawImageSharpnessComparison;
        _imageSharpnessComparisonPanel.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            _draggingImageSharpnessDivider = true;
            MoveImageSharpnessDivider(e.X);
        };
        _imageSharpnessComparisonPanel.MouseMove += (_, e) =>
        {
            if (_draggingImageSharpnessDivider)
                MoveImageSharpnessDivider(e.X);
        };
        _imageSharpnessComparisonPanel.MouseUp += (_, _) =>
            _draggingImageSharpnessDivider = false;
        _imageSharpnessComparisonPanel.MouseLeave += (_, _) =>
        {
            if (Control.MouseButtons == MouseButtons.None)
                _draggingImageSharpnessDivider = false;
        };

        _imageStyleTemplateCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingGenerationTemplateUi)
                return;

            _s.ImageStyleTemplate = SelectedTemplateId(_imageStyleTemplateCombo, "photo4k");
            ApplyImageObjectiveSettings(_s.ImageStyleTemplate);
            UpdateMaximumQualitySharpnessUi();
            UpdateTemplateToolTips();
            SettingsStore.Save(_s);
        };
        _imageNegativeTemplateCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingGenerationTemplateUi)
                return;

            _s.ImageNegativeTemplate = SelectedTemplateId(_imageNegativeTemplateCombo, "style");
            SettingsStore.Save(_s);
        };
    }

    private void InitializeVideoTemplateUi()
    {


        FillVideoTemplateCombos();
        UpdateMaximumQualitySharpnessUi();


        _videoHistoryLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoStyleTemplateLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoNegativeTemplateLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoStyleTemplateCombo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoNegativeTemplateCombo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoHistoryPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _tabVideo.Controls.AddRange(
        [
            _videoStyleTemplateLabel,
            _videoStyleTemplateCombo,
            _videoNegativeTemplateLabel,
            _videoNegativeTemplateCombo,
            _videoHistoryLabel,
            _videoHistoryPanel
        ]);
        _videoSharpnessPreviewButton.Click += async (_, _) =>
        {
            if (_videoSharpnessPreviewPanel.Visible)
            {
                SetVideoSharpnessComparisonVisible(false);
                return;
            }

            await RefreshVideoSharpnessPreviewAsync(CancellationToken.None);
        };

        _videoStyleTemplateCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingGenerationTemplateUi)
                return;

            _s.VideoStyleTemplate = SelectedTemplateId(_videoStyleTemplateCombo, "cinematic");
            ApplyVideoObjectiveSettings(_s.VideoStyleTemplate);
            UpdateMaximumQualitySharpnessUi();
            UpdateTemplateToolTips();
            SettingsStore.Save(_s);
        };

        _videoNegativeTemplateCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingGenerationTemplateUi)
                return;

            _s.VideoNegativeTemplate = SelectedTemplateId(_videoNegativeTemplateCombo, "style");
            SettingsStore.Save(_s);
        };
    }

    private void ApplyImageObjectiveSettings(string id)
    {
        var preset = GenerationTemplates.FindImageSettings(id);
        if (preset is null)
            return;

        var wasLoading = _loadingSettingsExperience;
        _loadingSettingsExperience = true;
        try
        {
            numDefaultWidth.Value = Math.Clamp(
                (decimal)preset.Width,
                numDefaultWidth.Minimum,
                numDefaultWidth.Maximum);
            numDefaultHeight.Value = Math.Clamp(
                (decimal)preset.Height,
                numDefaultHeight.Minimum,
                numDefaultHeight.Maximum);
            numDefaultSteps.Value = Math.Clamp(
                (decimal)preset.Steps,
                numDefaultSteps.Minimum,
                numDefaultSteps.Maximum);
            _imageCfg.Value = Math.Clamp(
                (decimal)preset.Cfg,
                _imageCfg.Minimum,
                _imageCfg.Maximum);

            _s.DefaultWidth = Decimal.ToInt32(numDefaultWidth.Value);
            _s.DefaultHeight = Decimal.ToInt32(numDefaultHeight.Value);
            _s.DefaultSteps = Decimal.ToInt32(numDefaultSteps.Value);
            _s.ImageCfg = Decimal.ToDouble(_imageCfg.Value);
        }
        finally
        {
            _loadingSettingsExperience = wasLoading;
        }
    }

    private void ApplyVideoObjectiveSettings(string id)
    {
        var preset = GenerationTemplates.FindVideoSettings(id);
        if (preset is null)
            return;

        var wasLoading = _loadingSettingsExperience;
        var wasApplyingQuality = _applyingVideoQualityPreset;
        _loadingSettingsExperience = true;
        _applyingVideoQualityPreset = true;
        try
        {
            _videoWidth.Value = Math.Clamp(
                (decimal)preset.Width,
                _videoWidth.Minimum,
                _videoWidth.Maximum);
            _videoHeight.Value = Math.Clamp(
                (decimal)preset.Height,
                _videoHeight.Minimum,
                _videoHeight.Maximum);
            _videoFrames.Value = Math.Clamp(
                (decimal)preset.Frames,
                _videoFrames.Minimum,
                _videoFrames.Maximum);
            _videoFps.Value = Math.Clamp(
                (decimal)preset.Fps,
                _videoFps.Minimum,
                _videoFps.Maximum);
            _videoSteps.Value = Math.Clamp(
                (decimal)preset.Steps,
                _videoSteps.Minimum,
                _videoSteps.Maximum);
            _videoCfg.Value = Math.Clamp(
                (decimal)preset.Cfg,
                _videoCfg.Minimum,
                _videoCfg.Maximum);
            _videoSamplingShift.Value = Math.Clamp(
                (decimal)preset.SamplingShift,
                _videoSamplingShift.Minimum,
                _videoSamplingShift.Maximum);

            SelectComboText(_videoSampler, preset.Sampler);
            SelectComboText(_videoScheduler, preset.Scheduler);

            var qualityItem = _videoQualityCombo.Items
                .OfType<TemplateComboItem>()
                .FirstOrDefault(x =>
                    x.Id.Equals(
                        preset.QualityPreset,
                        StringComparison.OrdinalIgnoreCase));

            if (qualityItem is not null)
                _videoQualityCombo.SelectedItem = qualityItem;

            _s.VideoWidth = Decimal.ToInt32(_videoWidth.Value);
            _s.VideoHeight = Decimal.ToInt32(_videoHeight.Value);
            _s.VideoFrames = Decimal.ToInt32(_videoFrames.Value);
            _s.VideoFps = Decimal.ToInt32(_videoFps.Value);
            _s.VideoDurationSeconds =
                _s.VideoFrames / Math.Max(1d, _s.VideoFps);
            _s.VideoSteps = Decimal.ToInt32(_videoSteps.Value);
            _s.VideoCfg = Decimal.ToDouble(_videoCfg.Value);
            _s.VideoSamplingShift =
                Decimal.ToDouble(_videoSamplingShift.Value);
            _s.VideoSampler =
                _videoSampler.SelectedItem?.ToString() ?? preset.Sampler;
            _s.VideoScheduler =
                _videoScheduler.SelectedItem?.ToString() ?? preset.Scheduler;
            _s.VideoQualityPreset = preset.QualityPreset;

            _videoDurationSeconds.Value = Math.Clamp(
                (decimal)_s.VideoDurationSeconds,
                _videoDurationSeconds.Minimum,
                _videoDurationSeconds.Maximum);
        }
        finally
        {
            _applyingVideoQualityPreset = wasApplyingQuality;
            _loadingSettingsExperience = wasLoading;
        }

        UpdateVideoQualityHint();
    }

    private void InitializeInstallationVideoUi()
    {


        _generationTemplateTips.SetToolTip(
            _installVideoModelsStatus,
            _installVideoModelsStatus.Text);
        _installVideoModelsStatus.TextChanged += (_, _) =>
            _generationTemplateTips.SetToolTip(
                _installVideoModelsStatus,
                _installVideoModelsStatus.Text);


        _installLog.Location = new Point(18, 412);
        _installLog.Size = new Size(1018, 216);

        tabInstallation.Controls.Add(_installImageModelsButton);
        tabInstallation.Controls.Add(_installVideoModelsButton);
        tabInstallation.Controls.Add(_installVideoModelsStatus);
        tabInstallation.Controls.Add(_installComponentsStatus);

        _installImageModelsButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(_s.Language, "Installation Image", "Image setup"),
                () => InstallModelGroupAsync(
                    L10n.Pick(_s.Language, "Image", "Image"),
                    _installer.InstallFluxModelsAsync));

        _installVideoModelsButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(_s.Language, "Installation vidéo", "Video setup"),
                () => InstallModelGroupAsync(
                    L10n.Pick(_s.Language, "Vidéo", "Video"),
                    _installer.InstallVideoModelsAsync));

        RefreshInstallationVideoStatus();
        RefreshInstallationComponentsStatus();
    }


    private async Task InstallModelGroupAsync(
        string label,
        Func<CancellationToken, Task> install)
    {
        if (_installTask is not null)
            throw new InvalidOperationException(
                L10n.Pick(_s.Language, "Une installation est déjà en cours.", "An installation is already running."));

        _installTask = InstallModelGroupCoreAsync(label, install);
        try
        {
            await _installTask;
        }
        finally
        {
            _installTask = null;
        }
    }

    private async Task InstallModelGroupCoreAsync(
        string label,
        Func<CancellationToken, Task> install)
    {
        _installCts = new CancellationTokenSource();
        btnInstallAll.Enabled = false;
        btnInstallCancel.Enabled = true;
        _installImageModelsButton.Enabled = false;
        _installVideoModelsButton.Enabled = false;
        _installProgress.Value = 0;
        _installText.Text = $"{label} · vérification des fichiers et SHA256…";

        try
        {
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
            await install(_installCts.Token);
            _installText.Text = $"{label} · installation / réparation terminée.";
        }
        catch (OperationCanceledException)
        {
            _installText.Text =
                L10n.Pick(
                    _s.Language,
                    $"{label} · installation annulée.",
                    $"{label} · installation cancelled.");
            throw;
        }
        finally
        {
            _installCts.Dispose();
            _installCts = null;
            btnInstallAll.Enabled = true;
            btnInstallCancel.Enabled = false;
            _installImageModelsButton.Enabled = true;
            _installVideoModelsButton.Enabled = true;
            RefreshInstallationComponentsStatus();
            RefreshInstallationVideoStatus();
            RefreshFeatureAvailability();
        }
    }


    private void ApplyGenerationExperienceTranslations()
    {
        if (_imageNegativeTemplateLabel is null)
            return;

        _imageNegativeTemplateLabel.Text =
            L10n.Pick(_s.Language, "Négatif", "Negative");
        _imageHistoryLabel.Text =
            L10n.Pick(_s.Language, "Historique des images", "Image history");
        _videoNegativeTemplateLabel.Text =
            L10n.Pick(_s.Language, "Négatif", "Negative");
        _videoHistoryLabel.Text =
            L10n.Pick(_s.Language, "Historique vidéo", "Video history");

        _installImageModelsButton.Text =
            L10n.Pick(_s.Language, "Installer / réparer Image", "Install / repair Image");
        _installVideoModelsButton.Text =
            L10n.Pick(_s.Language, "Installer / réparer Vidéo", "Install / repair Video");

        _updatingGenerationTemplateUi = true;
        try
        {
            FillImageTemplateCombos();
            FillVideoTemplateCombos();
        }
        finally
        {
            _updatingGenerationTemplateUi = false;
        }

        SetImageSharpnessComparisonVisible(false);
        SetVideoSharpnessComparisonVisible(false);
        UpdateMaximumQualitySharpnessUi();
        UpdateTemplateToolTips();
        RefreshInstallationVideoStatus();
        RefreshInstallationComponentsStatus();
    }

    private void FillImageTemplateCombos()
    {
        FillTemplateCombo(
            _imageStyleTemplateCombo,
            GenerationTemplates.ImageStyles.Select(x =>
                new TemplateComboItem(x.Id, GenerationTemplates.Display(x, _s.Language))),
            _s.ImageStyleTemplate);

        FillTemplateCombo(
            _imageNegativeTemplateCombo,
            GenerationTemplates.ImageNegatives.Select(x =>
                new TemplateComboItem(x.Id, GenerationTemplates.Display(x, _s.Language))),
            _s.ImageNegativeTemplate);
    }
    private void FillVideoTemplateCombos()
    {
        FillTemplateCombo(
            _videoStyleTemplateCombo,
            GenerationTemplates.VideoStyles.Select(x =>
                new TemplateComboItem(x.Id, GenerationTemplates.Display(x, _s.Language))),
            _s.VideoStyleTemplate);

        FillTemplateCombo(
            _videoNegativeTemplateCombo,
            GenerationTemplates.VideoNegatives.Select(x =>
                new TemplateComboItem(x.Id, GenerationTemplates.Display(x, _s.Language))),
            _s.VideoNegativeTemplate);

        UpdateTemplateToolTips();
    }

    private void UpdateMaximumQualitySharpnessUi()
    {
        var imageEnabled =
            GenerationTemplates.UsesTwoPassMaximumQuality(
                SelectedTemplateId(
                    _imageStyleTemplateCombo,
                    _s.ImageStyleTemplate));
        var videoEnabled =
            GenerationTemplates.UsesTwoPassMaximumQuality(
                SelectedTemplateId(
                    _videoStyleTemplateCombo,
                    _s.VideoStyleTemplate));

        _imageMaxQualitySharpnessLabel.Visible = imageEnabled;
        _imageMaxQualitySharpness.Visible = imageEnabled;
        _imageMaxQualitySharpnessLabel.Enabled = imageEnabled;
        _imageMaxQualitySharpness.Enabled = imageEnabled;
        _imageSharpnessPreviewButton.Visible = imageEnabled;
        _imageSharpnessPreviewButton.Enabled =
            imageEnabled &&
            !string.IsNullOrWhiteSpace(_lastMaximumQualityImageFirstPassPath) &&
            File.Exists(_lastMaximumQualityImageFirstPassPath);

        if (!imageEnabled)
            SetImageSharpnessComparisonVisible(false);

        _videoMaxQualitySharpnessLabel.Visible = videoEnabled;
        _videoMaxQualitySharpness.Visible = videoEnabled;
        _videoMaxQualitySharpnessLabel.Enabled = videoEnabled;
        _videoMaxQualitySharpness.Enabled = videoEnabled;
        _videoSharpnessPreviewButton.Visible = videoEnabled;
        _videoSharpnessPreviewButton.Enabled =
            videoEnabled &&
            !string.IsNullOrWhiteSpace(_lastMaximumQualityVideoFirstPassPath) &&
            File.Exists(_lastMaximumQualityVideoFirstPassPath);

        if (!videoEnabled)
            SetVideoSharpnessComparisonVisible(false);

        _generationTemplateTips.SetToolTip(
            _imageMaxQualitySharpness,
            L10n.Pick(
                _s.Language,
                "Netteté appliquée uniquement à la passe 2 de Qualité maximale. 0 = aucune accentuation.",
                "Sharpness applied only to Maximum quality pass 2. 0 = no sharpening."));
        _generationTemplateTips.SetToolTip(
            _imageSharpnessComparisonSlider,
            L10n.Pick(
                _s.Language,
                "Déplacez le curseur horizontalement : Avant à gauche, Après à droite.",
                "Move the slider horizontally: Before on the left, After on the right."));
        _generationTemplateTips.SetToolTip(
            _videoMaxQualitySharpness,
            L10n.Pick(
                _s.Language,
                "Netteté appliquée uniquement à la passe 2 vidéo de Qualité maximale. 0 = aucune accentuation.",
                "Sharpness applied only to Maximum quality video pass 2. 0 = no sharpening."));
    }

    private async Task RefreshImageSharpnessPreviewAsync(
        CancellationToken cancellationToken)
    {
        var sourcePath = _lastMaximumQualityImageFirstPassPath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return;

        var oldText = _imageSharpnessPreviewButton.Text;
        _imageSharpnessPreviewButton.Enabled = false;
        _imageSharpnessPreviewButton.Text =
            L10n.Pick(_s.Language, "Calcul…", "Rendering…");

        try
        {
            var preview =
                await _qualityPostProcessor.CreateImageSharpnessPreviewAsync(
                    sourcePath,
                    Decimal.ToInt32(_imageMaxQualitySharpness.Value),
                    cancellationToken);

            LoadPreviewPicture(_imageSharpnessBeforePreview, preview.BeforePath);
            LoadPreviewPicture(_imageSharpnessAfterPreview, preview.AfterPath);
            TryDeletePreviewFile(preview.BeforePath);
            TryDeletePreviewFile(preview.AfterPath);
            SetImageSharpnessComparisonVisible(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log("Aperçu netteté !", ex.Message);
        }
        finally
        {
            _imageSharpnessPreviewButton.Enabled = true;
            if (!_imageSharpnessComparisonPanel.Visible)
                _imageSharpnessPreviewButton.Text = oldText;
        }
    }

    private async Task RefreshVideoSharpnessPreviewAsync(
        CancellationToken cancellationToken)
    {
        var sourcePath = _lastMaximumQualityVideoFirstPassPath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return;

        var oldText = _videoSharpnessPreviewButton.Text;
        _videoSharpnessPreviewButton.Enabled = false;
        _videoSharpnessPreviewButton.Text =
            L10n.Pick(_s.Language, "Calcul…", "Rendering…");

        try
        {
            var preview =
                await _qualityPostProcessor.CreateVideoSharpnessPreviewAsync(
                    sourcePath,
                    Decimal.ToInt32(_videoMaxQualitySharpness.Value),
                    cancellationToken);

            LoadPreviewPicture(_videoSharpnessBeforePreview, preview.BeforePath);
            LoadPreviewPicture(_videoSharpnessAfterPreview, preview.AfterPath);
            TryDeletePreviewFile(preview.BeforePath);
            TryDeletePreviewFile(preview.AfterPath);
            SetVideoSharpnessComparisonVisible(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log("Aperçu netteté !", ex.Message);
        }
        finally
        {
            _videoSharpnessPreviewButton.Enabled = true;
            if (!_videoSharpnessPreviewPanel.Visible)
                _videoSharpnessPreviewButton.Text = oldText;
        }
    }

    private void DrawImageSharpnessComparison(
        object? sender,
        PaintEventArgs e)
    {
        var before = _imageSharpnessBeforePreview.Image;
        var after = _imageSharpnessAfterPreview.Image;

        e.Graphics.Clear(_imageSharpnessComparisonPanel.BackColor);

        if (before is null || after is null)
            return;

        e.Graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode =
            System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        e.Graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var bounds =
            _imageSharpnessComparisonPanel.ClientRectangle;
        var destination =
            CalculateZoomRectangle(before.Size, bounds);

        e.Graphics.DrawImage(before, destination);

        var dividerX = (int)Math.Round(
            bounds.Width *
            (_imageSharpnessComparisonSlider.Value / 100d));
        dividerX = Math.Clamp(
            dividerX,
            bounds.Left,
            Math.Max(bounds.Left, bounds.Right - 1));

        var state = e.Graphics.Save();
        e.Graphics.SetClip(
            new Rectangle(
                dividerX,
                bounds.Top,
                Math.Max(0, bounds.Right - dividerX),
                bounds.Height));
        e.Graphics.DrawImage(after, destination);
        e.Graphics.Restore(state);

        using var dividerPen =
            new Pen(Color.White, 2f);
        e.Graphics.DrawLine(
            dividerPen,
            dividerX,
            bounds.Top,
            dividerX,
            bounds.Bottom);

        const int handleRadius = 9;
        var handleY = bounds.Top + bounds.Height / 2;
        var handleRect =
            new Rectangle(
                dividerX - handleRadius,
                handleY - handleRadius,
                handleRadius * 2,
                handleRadius * 2);

        using var handleBrush =
            new SolidBrush(Color.FromArgb(230, 24, 26, 31));
        e.Graphics.FillEllipse(handleBrush, handleRect);
        e.Graphics.DrawEllipse(dividerPen, handleRect);
    }

    private static Rectangle CalculateZoomRectangle(
        Size imageSize,
        Rectangle bounds)
    {
        if (imageSize.Width <= 0 ||
            imageSize.Height <= 0 ||
            bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var scale = Math.Min(
            bounds.Width / (double)imageSize.Width,
            bounds.Height / (double)imageSize.Height);

        var width =
            Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        var height =
            Math.Max(1, (int)Math.Round(imageSize.Height * scale));

        return new Rectangle(
            bounds.Left + (bounds.Width - width) / 2,
            bounds.Top + (bounds.Height - height) / 2,
            width,
            height);
    }

    private void MoveImageSharpnessDivider(int mouseX)
    {
        var width =
            Math.Max(1, _imageSharpnessComparisonPanel.ClientSize.Width);
        var value =
            (int)Math.Round(
                Math.Clamp(mouseX, 0, width) *
                100d /
                width);

        _imageSharpnessComparisonSlider.Value =
            Math.Clamp(
                value,
                _imageSharpnessComparisonSlider.Minimum,
                _imageSharpnessComparisonSlider.Maximum);
    }

    private void SetImageSharpnessComparisonVisible(bool visible)
    {
        _imagePreviewViewport.Enabled = visible;
        _imageSharpnessComparisonSlider.Enabled = visible;
        _imageSharpnessBeforeLabel.Visible = visible;
        _imageSharpnessAfterLabel.Visible = visible;
        _imageSharpnessBeforePreview.Visible = false;
        _imageSharpnessAfterPreview.Visible = false;
        _imageSharpnessComparisonPanel.Visible = visible;
        _imageSharpnessComparisonSlider.Visible = visible;
        _preview.Visible = !visible;

        if (visible)
        {
            _imageSharpnessComparisonPanel.BringToFront();
            _imageSharpnessBeforeLabel.BringToFront();
            _imageSharpnessAfterLabel.BringToFront();
            _imageSharpnessComparisonSlider.BringToFront();
            _imageSharpnessComparisonPanel.Invalidate();
        }

        _imageSharpnessPreviewButton.Text =
            visible
                ? L10n.Pick(_s.Language, "Voir résultat", "Show result")
                : L10n.Pick(_s.Language, "Aperçu avant/après", "Before/after preview");
    }

    private void SetVideoSharpnessComparisonVisible(bool visible)
    {
        _videoSharpnessPreviewPanel.Visible = visible;
        _videoPreviewWeb.Visible = !visible;

        _videoSharpnessPreviewButton.Text =
            visible
                ? L10n.Pick(_s.Language, "Voir vidéo", "Show video")
                : L10n.Pick(_s.Language, "Aperçu avant/après", "Before/after preview");
    }

    private static void LoadPreviewPicture(PictureBox pictureBox, string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var source = Image.FromStream(stream);
        var old = pictureBox.Image;
        pictureBox.Image = new Bitmap(source);
        old?.Dispose();
    }

    private static void TryDeletePreviewFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private void UpdateTemplateToolTips()
    {
        if (_imageStyleTemplateCombo is not null)
        {
            var style = GenerationTemplates.FindStyle(
                GenerationTemplates.ImageStyles,
                SelectedTemplateId(_imageStyleTemplateCombo, _s.ImageStyleTemplate));
            if (style is not null)
                _generationTemplateTips.SetToolTip(
                    _imageStyleTemplateCombo,
                    GenerationTemplates.Description(style, _s.Language) +
                    (string.IsNullOrWhiteSpace(style.RecommendedSettings)
                        ? string.Empty
                        : Environment.NewLine + style.RecommendedSettings));
        }

        if (_videoStyleTemplateCombo is not null)
        {
            var style = GenerationTemplates.FindStyle(
                GenerationTemplates.VideoStyles,
                SelectedTemplateId(_videoStyleTemplateCombo, _s.VideoStyleTemplate));
            if (style is not null)
                _generationTemplateTips.SetToolTip(
                    _videoStyleTemplateCombo,
                    GenerationTemplates.Description(style, _s.Language) +
                    (string.IsNullOrWhiteSpace(style.RecommendedSettings)
                        ? string.Empty
                        : Environment.NewLine + style.RecommendedSettings));
        }
    }

    private static void FillTemplateCombo(
        ComboBox combo,
        IEnumerable<TemplateComboItem> items,
        string wanted)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (var item in items)
            combo.Items.Add(item);

        var selected = combo.Items.Cast<TemplateComboItem>()
            .FirstOrDefault(x => x.Id.Equals(wanted, StringComparison.OrdinalIgnoreCase));

        combo.SelectedItem = selected ?? combo.Items.Cast<object>().FirstOrDefault();
        combo.EndUpdate();
    }

    private static string SelectedTemplateId(ComboBox combo, string fallback) =>
        combo.SelectedItem is TemplateComboItem item ? item.Id : fallback;
    private string ApplyImageStyleTemplate(string prompt)
    {
        var style = GenerationTemplates.FindStyle(
            GenerationTemplates.ImageStyles,
            SelectedTemplateId(_imageStyleTemplateCombo, _s.ImageStyleTemplate));

        return GenerationTemplates.ApplyPrompt(prompt, style);
    }

    private string ApplyImageNegativeTemplate(string negative)
    {
        var preset = GenerationTemplates.FindNegative(
            GenerationTemplates.ImageNegatives,
            SelectedTemplateId(_imageNegativeTemplateCombo, _s.ImageNegativeTemplate));
        var style = GenerationTemplates.FindStyle(
            GenerationTemplates.ImageStyles,
            SelectedTemplateId(_imageStyleTemplateCombo, _s.ImageStyleTemplate));

        return GenerationTemplates.MergeNegative(negative, preset, style);
    }

    private string ApplyVideoStyleTemplate(string prompt)
    {
        var style = GenerationTemplates.FindStyle(
            GenerationTemplates.VideoStyles,
            SelectedTemplateId(_videoStyleTemplateCombo, _s.VideoStyleTemplate));

        return GenerationTemplates.ApplyPrompt(prompt, style);
    }

    private string ApplyVideoNegativeTemplate(string negative)
    {
        var preset = GenerationTemplates.FindNegative(
            GenerationTemplates.VideoNegatives,
            SelectedTemplateId(_videoNegativeTemplateCombo, _s.VideoNegativeTemplate));
        var style = GenerationTemplates.FindStyle(
            GenerationTemplates.VideoStyles,
            SelectedTemplateId(_videoStyleTemplateCombo, _s.VideoStyleTemplate));

        return GenerationTemplates.MergeNegative(negative, preset, style);
    }
    private void RefreshImageHistory()
    {
        if (_imageHistoryPanel is null)
            return;

        ClearHistoryPanel(_imageHistoryPanel);

        var dir = PortablePaths.Resolve(_s.Images);
        if (!Directory.Exists(dir))
            return;

        var files = Directory.EnumerateFiles(dir)
            .Where(path =>
                new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" }
                    .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(16);

        foreach (var path in files)
            _imageHistoryPanel.Controls.Add(CreateImageHistoryCard(path));
    }

    private Control CreateImageHistoryCard(string path)
    {
        var card = CreateHistoryCard();
        var picture = new PictureBox
        {
            Location = new Point(3, 3),
            Size = new Size(84, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = AppTheme.Input,
            Image = LoadImageThumbnail(path, new Size(84, 48)),
            Cursor = Cursors.Hand,
            Tag = path
        };

        var label = HistoryLabel(path, 53, 29);
        picture.Click += (_, _) => ShowPreviewImage(path);
        label.Click += (_, _) => ShowPreviewImage(path);
        picture.DoubleClick += (_, _) => OpenHistoryFile(path);
        label.DoubleClick += (_, _) => OpenHistoryFile(path);
        card.Controls.Add(picture);
        card.Controls.Add(label);
        return card;
    }
    private void RefreshVideoHistory()
    {
        if (_videoHistoryPanel is null)
            return;

        ClearHistoryPanel(_videoHistoryPanel);

        var dir = PortablePaths.Resolve(_s.Videos);
        if (!Directory.Exists(dir))
            return;

        foreach (var path in Directory.EnumerateFiles(dir, "*.mp4")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Take(16))
        {
            _videoHistoryPanel.Controls.Add(CreateVideoHistoryCard(path));
        }
    }

    private Control CreateVideoHistoryCard(string path)
    {
        var card = CreateHistoryCard();
        var picture = new PictureBox
        {
            Location = new Point(3, 3),
            Size = new Size(84, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = AppTheme.Input,
            Image = TryLoadShellThumbnail(path, new Size(84, 48)),
            Cursor = Cursors.Hand,
            Tag = path
        };

        if (picture.Image is null)
        {
            var fallback = new Bitmap(84, 48);
            using var g = Graphics.FromImage(fallback);
            g.Clear(AppTheme.Input);
            using var font = new Font("Segoe UI Symbol", 20F, FontStyle.Bold);
            TextRenderer.DrawText(g, "▶", font, new Rectangle(0, 0, 84, 48),
                AppTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            picture.Image = fallback;
        }
        var label = HistoryLabel(path, 53, 29);
        async void Preview()
        {
            await PreviewVideoAsync(path);
        }

        card.Cursor = Cursors.Hand;
        card.Tag = path;
        card.Click += (_, _) => Preview();
        picture.Click += (_, _) => Preview();
        label.Click += (_, _) => Preview();
        card.DoubleClick += (_, _) => OpenHistoryFile(path);
        picture.DoubleClick += (_, _) => OpenHistoryFile(path);
        label.DoubleClick += (_, _) => OpenHistoryFile(path);
        card.Controls.Add(picture);
        card.Controls.Add(label);
        return card;
    }

    private static Panel CreateHistoryCard() =>
        new()
        {
            Size = new Size(90, 84),
            Margin = new Padding(3),
            BackColor = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };

    private static Label HistoryLabel(string path, int y, int height)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.Length > 14)
            name = name[..13] + "…";

        return new Label
        {
            Location = new Point(3, y),
            Size = new Size(84, height),
            ForeColor = AppTheme.TextDim,
            Font = new Font("Segoe UI", 6.5F),
            Text = name + Environment.NewLine +
                   File.GetLastWriteTime(path).ToString("dd/MM HH:mm"),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Tag = path
        };
    }

    private static void OpenHistoryFile(string path)
    {
        if (!File.Exists(path))
            return;

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    private static Image? LoadImageThumbnail(string path, Size size)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var source = Image.FromStream(fs);
            return new Bitmap(source, size);
        }
        catch
        {
            return null;
        }
    }

    private static void ClearHistoryPanel(FlowLayoutPanel panel)
    {
        foreach (Control control in panel.Controls)
        {
            foreach (PictureBox picture in control.Controls.OfType<PictureBox>())
            {
                picture.Image?.Dispose();
                picture.Image = null;
            }
            control.Dispose();
        }
        panel.Controls.Clear();
    }

    private void RefreshInstallationComponentsStatus()
    {
        if (_installComponentsStatus is null)
            return;

        var comfyRoot = PortablePreflight.GetComfyRoot(_s);
        var webViewOk = PortablePaths.GetFixedWebView2RuntimePath() is not null;

        var lines = new[]
        {
            L10n.Pick(_s.Language, "Composants de base", "Base components"),
            StatusLine("OpenCode", File.Exists(PortablePaths.Resolve(_s.OpenCodeExe))),
            StatusLine("Ollama", File.Exists(PortablePaths.Resolve(_s.OllamaExe))),
            StatusLine("ComfyUI", File.Exists(PortablePaths.Resolve(_s.ComfyPython)) &&
                                   File.Exists(PortablePaths.Resolve(_s.ComfyMain))),
            StatusLine("WebView2 Fixed", webViewOk),
            string.Empty,
            L10n.Pick(_s.Language, "Image", "Image"),
            StatusLine("FLUX.2", File.Exists(Path.Combine(
                comfyRoot, "models", "diffusion_models", _s.FluxModel))),
            StatusLine("Qwen text encoder", File.Exists(Path.Combine(
                comfyRoot, "models", "text_encoders", _s.TextEncoderModel))),
            StatusLine("FLUX VAE", File.Exists(Path.Combine(
                comfyRoot, "models", "vae", _s.VaeModel))),
            string.Empty,
            L10n.Pick(_s.Language, "Vidéo", "Video"),
            StatusLine("Wan 2.1", File.Exists(Path.Combine(
                comfyRoot, "models", "diffusion_models", _s.VideoModel))),
            StatusLine("UMT5", File.Exists(Path.Combine(
                comfyRoot, "models", "text_encoders", _s.VideoTextEncoderModel))),
            StatusLine("Wan VAE", File.Exists(Path.Combine(
                comfyRoot, "models", "vae", _s.VideoVaeModel)))
        };

        _installComponentsStatus.Text = string.Join(Environment.NewLine, lines);
    }

    private string StatusLine(string label, bool installed) =>
        installed
            ? $"  ✓ {label} — {L10n.Pick(_s.Language, "Installé", "Installed")}"
            : $"  ⚠ {label} — {L10n.Pick(_s.Language, "Manquant", "Missing")}";

    private void RefreshInstallationVideoStatus()
    {
        if (_installVideoModelsStatus is null || _videoGenerator is null)
            return;

        var missing = _videoGenerator.GetMissingModels();
        _installVideoModelsStatus.Text = missing.Count == 0
            ? L10n.Pick(_s.Language, "✓ Wan vidéo prêt", "✓ Wan video ready")
            : L10n.Pick(_s.Language, $"Wan vidéo : {missing.Count} modèle(s) manquant(s)", $"Wan video: {missing.Count} model(s) missing");
        _installVideoModelsStatus.ForeColor = missing.Count == 0 ? AppTheme.SuccessHover : AppTheme.Warning;
    }
    private void RefreshFeatureAvailability()
    {
        if (_tabVideo is null)
            return;

        var openCodeOk = File.Exists(PortablePaths.Resolve(_s.OpenCodeExe)) &&
                         PortablePaths.GetFixedWebView2RuntimePath() is not null;
        var ollamaOk = File.Exists(PortablePaths.Resolve(_s.OllamaExe));
        var comfyOk = File.Exists(PortablePaths.Resolve(_s.ComfyPython)) &&
                      File.Exists(PortablePaths.Resolve(_s.ComfyMain));
        var fluxOk = comfyOk && HasFluxModels();
        var missingVideo = _videoGenerator.GetMissingModels();
        var videoOk = comfyOk && missingVideo.Count == 0;

        Log(
            "UI",
            $"Capacités · OpenCode={(openCodeOk ? "OK" : "manquant")} · " +
            $"Ollama={(ollamaOk ? "OK" : "manquant")} · " +
            $"ComfyUI={(comfyOk ? "OK" : "manquant")} · " +
            $"Image={(fluxOk ? "OK" : "manquante")} · " +
            $"Vidéo={(videoOk ? "OK" : "manquante")}" +
            (missingVideo.Count == 0
                ? string.Empty
                : " · " + string.Join(", ", missingVideo.Select(x => x.Label))));

        SetFeatureTab(tabOpenCode, openCodeOk,
            L10n.Pick(_s.Language, "OpenCode/WebView2 portable manque.", "Portable OpenCode/WebView2 is missing."));
        SetFeatureTab(tabComfy, comfyOk && PortablePaths.GetFixedWebView2RuntimePath() is not null,
            L10n.Pick(_s.Language, "ComfyUI/WebView2 portable manque.", "Portable ComfyUI/WebView2 is missing."));
        SetFeatureTab(tabOllama, ollamaOk,
            L10n.Pick(_s.Language, "Ollama portable manque.", "Portable Ollama is missing."));
        SetFeatureTab(tabGenerate, fluxOk,
            L10n.Pick(_s.Language, "ComfyUI ou modèles FLUX.2 manquants.", "ComfyUI or FLUX.2 models are missing."));
        SetFeatureTab(_tabVideo, videoOk,
            L10n.Pick(_s.Language, "ComfyUI ou modèles Wan vidéo manquants.", "ComfyUI or Wan video models are missing."));

        RefreshInstallationVideoStatus();
        RefreshInstallationComponentsStatus();
    }

    private bool HasFluxModels()
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        return File.Exists(Path.Combine(root, "models", "diffusion_models", _s.FluxModel)) &&
               File.Exists(Path.Combine(root, "models", "text_encoders", _s.TextEncoderModel)) &&
               File.Exists(Path.Combine(root, "models", "vae", _s.VaeModel));
    }
    private void SetFeatureTab(TabPage page, bool enabled, string reason)
    {
        page.ToolTipText = enabled ? string.Empty : reason;

        // Image and Video are editors as well as launch surfaces. Missing
        // models/backends must block generation, not lock the entire page.
        // Users still need access to prompts, dimensions, presets, model
        // selectors and installation/configuration helpers.
        if (page == tabGenerate || page == _tabVideo)
        {
            page.Enabled = true;

            if (page == tabGenerate)
            {
                btnGenerate.Enabled =
                    enabled && !_gpuUiLocked;
                if (!enabled)
                    _genText.Text = reason;
            }
            else
            {
                _videoGenerateButton.Enabled =
                    enabled &&
                    !_gpuUiLocked &&
                    _videoCts is null;
                if (!enabled)
                    _videoStatus.Text = reason;
            }

            _tabs.Invalidate();
            return;
        }

        page.Enabled = enabled;
        _tabs.Invalidate();
    }

    private void MainTabs_Selecting(object? sender, TabControlCancelEventArgs e)
    {
        if (e.TabPage is null || e.TabPage.Enabled || e.TabPage == tabInstallation)
            return;

        var reason = string.IsNullOrWhiteSpace(e.TabPage.ToolTipText)
            ? L10n.Pick(_s.Language, "Composant requis manquant.", "Required component is missing.")
            : e.TabPage.ToolTipText;

        e.Cancel = true;
        _installText.Text = reason;
        Log("UI", reason);

        BeginInvoke(() => _tabs.SelectedTab = tabInstallation);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShellSize
    {
        public int Width;
        public int Height;
    }

    [Flags]
    private enum ShellImageFlags
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        MemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10
    }
    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(
            ShellSize size,
            ShellImageFlags flags,
            out IntPtr bitmapHandle);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        IntPtr bindContext,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory factory);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);

    private static Image? TryLoadShellThumbnail(string path, Size size)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            var id = typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref id, out var factory);
            var hr = factory.GetImage(
                new ShellSize { Width = size.Width, Height = size.Height },
                ShellImageFlags.ResizeToFit | ShellImageFlags.BiggerSizeOk,
                out handle);
            if (hr != 0 || handle == IntPtr.Zero)
                return null;

            using var source = Image.FromHbitmap(handle);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (handle != IntPtr.Zero)
                DeleteObject(handle);
        }
    }

    // =====================================================================
    // MainForm.SettingsExperience
    // =====================================================================
    private bool _loadingSettingsExperience;
    private bool _configurationAutoSaveBusy;

    private void InitializeSettingsExperienceUi()
    {
        ReorderMainTabs();
        MoveImageGenerationSettingsToImageTab();
        ExtendVideoGenerationSettings();
        AddVideoModelConfiguration();
        InitializeConfigurationAutoSave();
        WireGenerationSettingsPersistence();
        WireConfigurationAutoSave();
        UpdateVideoQualityHint();
    }

    private void ReorderMainTabs()
    {
        var selected = _tabs.SelectedTab;
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

        _tabs.SuspendLayout();
        try
        {
            _tabs.TabPages.Clear();
            _tabs.TabPages.AddRange(order);
            _tabs.ItemSize = new Size(103, 28);
            _tabs.SelectedTab = selected is not null && order.Contains(selected)
                ? selected
                : tabDashboard;
        }
        finally
        {
            _tabs.ResumeLayout();
        }
    }

    private void MoveImageGenerationSettingsToImageTab()
    {
        // These controls keep their historical persisted names (DefaultWidth/Height/Steps)
        // for settings.json compatibility, but belong visually to the Image feature.
        tabGenerate.Controls.Add(lblCfgWidth);
        tabGenerate.Controls.Add(numDefaultWidth);
        tabGenerate.Controls.Add(lblCfgHeight);
        tabGenerate.Controls.Add(numDefaultHeight);
        tabGenerate.Controls.Add(lblCfgSteps);
        tabGenerate.Controls.Add(numDefaultSteps);

        lblCfgWidth.Text = "Largeur image";
        lblCfgHeight.Text = "Hauteur image";
        lblCfgSteps.Text = "Steps image";

        lblCfgWidth.Location = new Point(18, 474);
        lblCfgWidth.Size = new Size(90, 23);
        numDefaultWidth.Location = new Point(108, 474);
        numDefaultWidth.Size = new Size(74, 23);
        numDefaultWidth.Increment = 64;

        lblCfgHeight.Location = new Point(190, 474);
        lblCfgHeight.Size = new Size(86, 23);
        numDefaultHeight.Location = new Point(278, 474);
        numDefaultHeight.Size = new Size(70, 23);
        numDefaultHeight.Increment = 64;

        lblCfgSteps.Location = new Point(18, 504);
        lblCfgSteps.Size = new Size(90, 23);
        numDefaultSteps.Location = new Point(108, 504);
        numDefaultSteps.Size = new Size(74, 23);

        tabGenerate.Controls.Add(_imageCfgLabel);
        tabGenerate.Controls.Add(_imageCfg);

        // Compact the left column so every Image setting remains visible.
        _prompt.Size = new Size(330, 105);
        _negativePromptLabel.Location = new Point(18, 190);
        _negativePrompt.Location = new Point(18, 213);
        _negativePrompt.Size = new Size(330, 52);

        lblGenerationMode.Location = new Point(18, 274);
        cmbGenerationMode.Location = new Point(18, 296);
        lblInputImage.Location = new Point(18, 329);
        txtInputImage.Location = new Point(18, 351);
        txtInputImage.Size = new Size(178, 23);
        btnBrowseInputImage.Location = new Point(202, 350);
        btnBrowseInputImage.Size = new Size(80, 25);
        btnClearInputImage.Location = new Point(288, 350);
        btnClearInputImage.Size = new Size(60, 25);
        lblImg2ImgStrength.Location = new Point(18, 384);
        numImg2ImgStrength.Location = new Point(18, 406);

        _seedLabel.Location = new Point(132, 386);
        _seedLabel.Size = new Size(90, 18);
        _seedInput.Location = new Point(132, 406);
        _seedInput.Size = new Size(110, 23);
        _randomSeedCheck.Location = new Point(248, 406);
        _randomSeedCheck.Size = new Size(50, 23);

        btnGenerate.Location = new Point(18, 545);
        _benchmarkButton.Location = new Point(151, 545);
        _genText.Location = new Point(18, 587);
        _genProgress.Location = new Point(18, 621);
    }

    private void ExtendVideoGenerationSettings()
    {
        // Make room for the advanced sampling controls.
        _videoPrompt.Size = new Size(390, 112);
        _videoNegative.Location = new Point(18, 188);
        _videoNegative.Size = new Size(390, 52);

        RepositionVideoBaseControls();

        _videoCfg.Name = "_videoCfg";

        _videoSamplingShift.Name = "_videoSamplingShift";

        _videoSampler.Name = "_videoSampler";

        _videoScheduler.Name = "_videoScheduler";

        _videoSeed.Enabled = !_videoRandomSeed.Checked;

        _videoQualityHint.AutoSize = false;
        _videoQualityHint.Size = new Size(390, 38);

        _tabVideo.Controls.AddRange(
        [
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
            _videoQualityHint
        ]);

        _videoRandomSeed.CheckedChanged += (_, _) =>
        {
            _videoSeed.Enabled = !_videoRandomSeed.Checked;
            SaveVideoGenerationSettings();
        };

        _videoSteps.ValueChanged += (_, _) => UpdateVideoQualityHint();
    }

    private void RepositionVideoBaseControls()
    {
        // Labels are discovered by their current text because they are local variables
        // in InitializeVideoUi. Their controls themselves already have stable fields.
        foreach (var label in _tabVideo.Controls.OfType<Label>())
        {
            switch (label.Text)
            {
                case "Négatif / éléments à éviter":
                    label.Location = new Point(18, 163);
                    break;
                case "Largeur":
                    label.Location = new Point(18, 265);
                    break;
                case "Hauteur":
                    label.Location = new Point(210, 265);
                    break;
                case "Frames":
                    label.Location = new Point(18, 300);
                    break;
                case "FPS":
                    label.Location = new Point(210, 300);
                    break;
                case "Steps":
                    label.Text = "Steps vidéo";
                    label.Location = new Point(18, 335);
                    break;
            }
        }

        _videoWidth.Location = new Point(90, 261);
        _videoWidth.Size = new Size(100, 23);
        _videoHeight.Location = new Point(292, 261);
        _videoHeight.Size = new Size(116, 23);
        _videoFrames.Location = new Point(90, 296);
        _videoFrames.Size = new Size(100, 23);
        _videoFps.Location = new Point(292, 296);
        _videoFps.Size = new Size(116, 23);
        _videoSteps.Location = new Point(90, 331);
        _videoSteps.Size = new Size(100, 23);

        _videoGenerateButton.Location = new Point(18, 478);
        _videoCancelButton.Location = new Point(174, 478);
        _videoRefreshButton.Location = new Point(290, 478);
        _videoProgress.Location = new Point(18, 522);
        _videoStatus.Location = new Point(18, 548);
    }



    private void AddVideoModelConfiguration()
    {

        _cfgVideoModel.Name = "_cfgVideoModel";
        _cfgVideoTextEncoder.Name = "_cfgVideoTextEncoder";
        _cfgVideoVae.Name = "_cfgVideoVae";


        tabConfiguration.Controls.AddRange(
        [
            _cfgVideoModelLabel,
            _cfgVideoTextEncoderLabel,
            _cfgVideoVaeLabel,
            _cfgVideoModel,
            _cfgVideoTextEncoder,
            _cfgVideoVae,
            _cfgVideoClipVisionLabel,
            _cfgVideoClipVision
        ]);

        // Reclaim the space previously used by Image generation defaults.
        lblCfgVram.Location = new Point(18, 340);
        numSafeVram.Location = new Point(150, 340);
        lblCfgRam.Location = new Point(18, 373);
        numSafeRam.Location = new Point(150, 373);

        lblCfgConnections.Location = new Point(330, 340);
        numDownloadConnections.Location = new Point(500, 340);
        lblCfgBuffer.Location = new Point(620, 340);
        numDownloadBuffer.Location = new Point(790, 340);
        chkHardStopComfy.Location = new Point(330, 373);

        lblCfgLanguage.Location = new Point(18, 414);
        cmbLanguage.Location = new Point(150, 414);
        chkAutoUpdates.Location = new Point(330, 414);
        lblCfgGitHubRepo.Location = new Point(18, 452);
        txtGitHubRepo.Location = new Point(150, 452);
        chkInstallVisionModel.Location = new Point(18, 490);
    }



    private void InitializeConfigurationAutoSave()
    {

        _generationTemplateTips.SetToolTip(
            _configurationSaveStatus,
            _configurationSaveStatus.Text);
        _configurationSaveStatus.TextChanged += (_, _) =>
            _generationTemplateTips.SetToolTip(
                _configurationSaveStatus,
                _configurationSaveStatus.Text);

        tabConfiguration.Controls.Add(_autoSaveConfigurationCheck);
        tabConfiguration.Controls.Add(_configurationSaveStatus);

        _autoSaveConfigurationCheck.CheckedChanged += (_, _) =>
        {
            if (_loadingSettingsExperience)
                return;

            _s.AutoSaveConfiguration = _autoSaveConfigurationCheck.Checked;
            SettingsStore.Save(_s);
            if (_autoSaveConfigurationCheck.Checked)
                TryAutoSaveConfiguration("activation");
            else
                _configurationSaveStatus.Text = "Sauvegarde automatique désactivée.";
        };
    }

    private void WireGenerationSettingsPersistence()
    {
        numDefaultWidth.ValueChanged += (_, _) => SaveImageGenerationSettings();
        numDefaultHeight.ValueChanged += (_, _) => SaveImageGenerationSettings();
        numDefaultSteps.ValueChanged += (_, _) => SaveImageGenerationSettings();
        _imageCfg.ValueChanged += (_, _) => SaveImageGenerationSettings();
        _imageMaxQualitySharpness.ValueChanged += (_, _) =>
        {
            SaveImageGenerationSettings();
            if (_imageSharpnessComparisonPanel.Visible)
                SetImageSharpnessComparisonVisible(false);
        };

        foreach (var control in new NumericUpDown[]
                 {
                     _videoWidth, _videoHeight, _videoFrames, _videoFps, _videoSteps,
                     _videoCfg, _videoMaxQualitySharpness, _videoSamplingShift, _videoSeed
                 })
        {
            control.ValueChanged += (_, _) => SaveVideoGenerationSettings();
        }

        _videoSampler.SelectedIndexChanged += (_, _) => SaveVideoGenerationSettings();
        _videoScheduler.SelectedIndexChanged += (_, _) => SaveVideoGenerationSettings();
        _videoMaxQualitySharpness.ValueChanged += (_, _) =>
        {
            if (_videoSharpnessPreviewPanel.Visible)
                SetVideoSharpnessComparisonVisible(false);
        };
    }

    private void SaveImageGenerationSettings()
    {
        if (_loadingSettingsExperience)
            return;

        _s.DefaultWidth = Decimal.ToInt32(numDefaultWidth.Value);
        _s.DefaultHeight = Decimal.ToInt32(numDefaultHeight.Value);
        _s.DefaultSteps = Decimal.ToInt32(numDefaultSteps.Value);
        _s.ImageCfg = Decimal.ToDouble(_imageCfg.Value);
        _s.MaximumQualityImageSharpnessPercent =
            Decimal.ToInt32(_imageMaxQualitySharpness.Value);
        SettingsStore.Save(_s);
    }

    private void SaveVideoGenerationSettings()
    {
        if (_loadingSettingsExperience)
            return;

        _s.VideoWidth = Decimal.ToInt32(_videoWidth.Value);
        _s.VideoHeight = Decimal.ToInt32(_videoHeight.Value);
        _s.VideoFrames = Decimal.ToInt32(_videoFrames.Value);
        _s.VideoFps = Decimal.ToInt32(_videoFps.Value);
        _s.VideoDurationSeconds =
            Decimal.ToDouble(_videoDurationSeconds.Value);
        _s.VideoSteps = Decimal.ToInt32(_videoSteps.Value);
        _s.VideoCfg = Decimal.ToDouble(_videoCfg.Value);
        _s.MaximumQualityVideoSharpnessPercent =
            Decimal.ToInt32(_videoMaxQualitySharpness.Value);
        _s.VideoSamplingShift = Decimal.ToDouble(_videoSamplingShift.Value);
        _s.VideoSampler = _videoSampler.SelectedItem?.ToString() ?? "uni_pc";
        _s.VideoScheduler = _videoScheduler.SelectedItem?.ToString() ?? "simple";
        _s.VideoSeed = Decimal.ToInt64(_videoSeed.Value);
        _s.UseRandomVideoSeed = _videoRandomSeed.Checked;
        SettingsStore.Save(_s);
        UpdateVideoQualityHint();
    }

    private void UpdateVideoQualityHint()
    {
        if (_videoQualityHint is null || _videoSteps is null)
            return;

        var steps = Decimal.ToInt32(_videoSteps.Value);
        var cfg = Decimal.ToDouble(_videoCfg.Value);
        var shift = Decimal.ToDouble(_videoSamplingShift.Value);
        var sampler = _videoSampler.SelectedItem?.ToString() ?? string.Empty;
        var scheduler = _videoScheduler.SelectedItem?.ToString() ?? string.Empty;

        var referenceSampling =
            Math.Abs(cfg - 6.0) < 0.01 &&
            Math.Abs(shift - 8.0) < 0.01 &&
            sampler.Equals("uni_pc", StringComparison.OrdinalIgnoreCase) &&
            scheduler.Equals("simple", StringComparison.OrdinalIgnoreCase);

        if (steps < 12)
        {
            _videoQualityHint.Text =
                "⚠ Risque élevé de bruit : Wan 2.1 1.3B n'est pas distillé. Utilisez 20–30 steps (30 recommandé).";
            _videoQualityHint.ForeColor = AppTheme.Warning;
        }
        else if (steps < 20 || !referenceSampling)
        {
            _videoQualityHint.Text =
                "⚠ Réglages personnalisés. Référence stable : 30 steps · CFG 6 · UniPC · simple · shift 8.";
            _videoQualityHint.ForeColor = AppTheme.Warning;
        }
        else
        {
            var qualityName = steps switch
            {
                20 => "Rapide",
                30 => "Standard",
                40 => "Qualité",
                50 => "Meilleure",
                _ => "Personnalisé"
            };

            _videoQualityHint.Text =
                $"✓ {qualityName} : {steps} steps · CFG {cfg:0.##} · shift {shift:0.##} · {sampler} · {scheduler}.";
            _videoQualityHint.ForeColor = AppTheme.SuccessHover;
        }
    }

    private void WireConfigurationAutoSave()
    {
        foreach (var numeric in new[]
                 {
                     numOpenCodePort, numOllamaPort, numComfyPort, numProxyPort, numApiPort,
                     numSafeVram, numSafeRam, numDownloadConnections, numDownloadBuffer
                 })
        {
            numeric.ValueChanged += (_, _) => TryAutoSaveConfiguration("valeur");
        }

        foreach (var check in new[] { chkHardStopComfy, chkAutoUpdates, chkInstallVisionModel })
            check.CheckedChanged += (_, _) => TryAutoSaveConfiguration("option");

        foreach (var combo in new[]
                 {
                     cmbLanguage, _visionModelCombo, _fluxModelCombo, _textEncoderCombo, _vaeCombo
                 })
        {
            combo.Validated += (_, _) => TryAutoSaveConfiguration("sélection");
        }

        foreach (var text in new[]
                 {
                     txtGitHubRepo,
                     _cfgVideoModel,
                     _cfgVideoTextEncoder,
                     _cfgVideoVae,
                     _cfgVideoClipVision
                 })
            text.Validated += (_, _) => TryAutoSaveConfiguration("texte");
    }

    private void TryAutoSaveConfiguration(string source)
    {
        if (_loadingSettingsExperience ||
            _configurationAutoSaveBusy ||
            _autoSaveConfigurationCheck is null ||
            !_autoSaveConfigurationCheck.Checked)
        {
            return;
        }

        _configurationAutoSaveBusy = true;
        try
        {
            ValidateConfigurationUi();
            SaveSettingsFromUi();
            SettingsStore.Save(_s);
            SynchronizeWorkflowDefaults();
            _configurationSaveStatus.ForeColor = AppTheme.SuccessHover;
            _configurationSaveStatus.Text =
                $"✓ Configuration validée et enregistrée automatiquement ({DateTime.Now:HH:mm:ss}).";
            Log("UI", $"Configuration auto-enregistrée après validation ({source}).");
        }
        catch (Exception ex)
        {
            _configurationSaveStatus.ForeColor = AppTheme.Warning;
            _configurationSaveStatus.Text = "⚠ Non enregistré : " + ex.Message;
            Log("UI ⚠", "Configuration non enregistrée : " + ex.Message);
        }
        finally
        {
            _configurationAutoSaveBusy = false;
        }
    }

    private void ValidateConfigurationUi()
    {
        var ports = new[]
        {
            Decimal.ToInt32(numOpenCodePort.Value),
            Decimal.ToInt32(numOllamaPort.Value),
            Decimal.ToInt32(numComfyPort.Value),
            Decimal.ToInt32(numProxyPort.Value),
            Decimal.ToInt32(numApiPort.Value)
        };

        if (ports.Distinct().Count() != ports.Length)
            throw new InvalidOperationException("Chaque service doit utiliser un port différent.");

        ValidateModelFileName(SelectedModel(_fluxModelCombo, txtFluxModel), "Modèle FLUX");
        ValidateModelFileName(SelectedModel(_textEncoderCombo, txtTextEncoder), "Encodeur FLUX");
        ValidateModelFileName(SelectedModel(_vaeCombo, txtVae), "VAE FLUX");
        ValidateModelFileName(_cfgVideoModel.Text, "Modèle Wan");
        ValidateModelFileName(_cfgVideoTextEncoder.Text, "Encodeur Wan");
        ValidateModelFileName(_cfgVideoVae.Text, "VAE Wan");
        ValidateModelFileName(_cfgVideoClipVision.Text, "CLIP Vision Wan");

        var repoParts = txtGitHubRepo.Text.Trim()
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (repoParts.Length != 2)
            throw new InvalidOperationException("Le dépôt GitHub doit utiliser le format propriétaire/dépôt.");
    }

    private static void ValidateModelFileName(string value, string label)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(label + " ne peut pas être vide.");

        if (value.IndexOfAny(new[] { '\\', '/' }) >= 0 ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException(label + " doit être un nom de fichier, sans chemin.");
        }
    }

    private void LoadSettingsExperienceToUi()
    {
        if (_imageCfg is not null)
            _imageCfg.Value = ClampDecimal(_s.ImageCfg, _imageCfg.Minimum, _imageCfg.Maximum);
        if (_imageMaxQualitySharpness is not null)
            _imageMaxQualitySharpness.Value = Math.Clamp(
                _s.MaximumQualityImageSharpnessPercent,
                (int)_imageMaxQualitySharpness.Minimum,
                (int)_imageMaxQualitySharpness.Maximum);

        if (_videoCfg is not null)
        {
            _videoCfg.Value = ClampDecimal(_s.VideoCfg, _videoCfg.Minimum, _videoCfg.Maximum);
            _videoMaxQualitySharpness.Value = Math.Clamp(
                _s.MaximumQualityVideoSharpnessPercent,
                (int)_videoMaxQualitySharpness.Minimum,
                (int)_videoMaxQualitySharpness.Maximum);
            _videoSamplingShift.Value = ClampDecimal(
                _s.VideoSamplingShift, _videoSamplingShift.Minimum, _videoSamplingShift.Maximum);
            SelectComboText(_videoSampler, _s.VideoSampler);
            SelectComboText(_videoScheduler, _s.VideoScheduler);
            _videoSeed.Value = Math.Clamp(
                (decimal)Math.Max(1L, _s.VideoSeed), _videoSeed.Minimum, _videoSeed.Maximum);
            _videoRandomSeed.Checked = _s.UseRandomVideoSeed;
            _videoSeed.Enabled = !_videoRandomSeed.Checked;
        }

        if (_cfgVideoModel is not null)
        {
            _cfgVideoModel.Text = _s.VideoModel;
            _cfgVideoTextEncoder.Text = _s.VideoTextEncoderModel;
            _cfgVideoVae.Text = _s.VideoVaeModel;
            _cfgVideoClipVision.Text = _s.VideoClipVisionModel;
        }

        if (_autoSaveConfigurationCheck is not null)
            _autoSaveConfigurationCheck.Checked = _s.AutoSaveConfiguration;

        UpdateVideoQualityHint();
    }

    private static void SelectComboText(ComboBox combo, string value)
    {
        var item = combo.Items.Cast<object>()
            .FirstOrDefault(x => string.Equals(x?.ToString(), value, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
            combo.SelectedItem = item;
    }

    private void ApplySettingsExperienceTranslations()
    {
        tabGenerate.Text = "Image";
        _tabVideo.Text = L10n.Pick(_s.Language, "Vidéo", "Video");
        tabConfiguration.Text = L10n.Pick(_s.Language, "Configuration", "Configuration");
        tabInstallation.Text = L10n.Pick(_s.Language, "Installation", "Installation");
        tabAbout.Text = L10n.Pick(_s.Language, "À Propos", "About");

        lblCfgWidth.Text = L10n.Pick(_s.Language, "Largeur image", "Image width");
        lblCfgHeight.Text = L10n.Pick(_s.Language, "Hauteur image", "Image height");
        lblCfgSteps.Text = L10n.Pick(_s.Language, "Steps image", "Image steps");

        if (_imageCfgLabel is not null)
            _imageCfgLabel.Text = "CFG image";
        if (_imageMaxQualitySharpnessLabel is not null)
            _imageMaxQualitySharpnessLabel.Text =
                L10n.Pick(_s.Language, "Netteté passe 2 (%)", "Pass 2 sharpness (%)");
        if (_videoMaxQualitySharpnessLabel is not null)
            _videoMaxQualitySharpnessLabel.Text =
                L10n.Pick(_s.Language, "Netteté passe 2 (%)", "Pass 2 sharpness (%)");

        _imageSharpnessBeforeLabel.Text =
            L10n.Pick(_s.Language, "Avant · upscale seul", "Before · upscale only");
        _imageSharpnessAfterLabel.Text =
            L10n.Pick(_s.Language, "Après · netteté", "After · sharpness");
        _videoSharpnessBeforeLabel.Text =
            L10n.Pick(_s.Language, "Avant · upscale seul", "Before · upscale only");
        _videoSharpnessAfterLabel.Text =
            L10n.Pick(_s.Language, "Après · netteté", "After · sharpness");

        foreach (var label in _tabVideo.Controls.OfType<Label>())
        {
            if (label.Text is "Largeur" or "Largeur vidéo" or "Video width" or "Width")
                label.Text = L10n.Pick(_s.Language, "Largeur", "Width");
            else if (label.Text is "Hauteur" or "Hauteur vidéo" or "Video height" or "Height")
                label.Text = L10n.Pick(_s.Language, "Hauteur", "Height");
            else if (label.Text is "Durée (s)" or "Duration (s)")
                label.Text = L10n.Pick(_s.Language, "Durée (s)", "Duration (s)");
            else if (label.Text is "Frames" or "Frames vidéo" or "Video frames")
                label.Text = "Frames";
            else if (label.Text is "FPS" or "FPS vidéo" or "Video FPS")
                label.Text = "FPS";
            else if (label.Text is "Steps" or "Steps vidéo" or "Video steps")
                label.Text = "Steps";
        }

        if (_cfgVideoModelLabel is not null)
        {
            _cfgVideoModelLabel.Text = L10n.Pick(_s.Language, "Modèle Wan", "Wan model");
            _cfgVideoTextEncoderLabel.Text = L10n.Pick(_s.Language, "Encodeur Wan", "Wan encoder");
            _cfgVideoVaeLabel.Text = "Wan VAE";
        }

        if (_autoSaveConfigurationCheck is not null)
        {
            _autoSaveConfigurationCheck.Text = L10n.Pick(
                _s.Language,
                "Enregistrer automatiquement après validation",
                "Automatically save after validation");
        }

        ApplyMainTabStripLayoutV37();
        UpdateMaximumQualitySharpnessUi();
        UpdatePromptEnhancementState();
    }

    private static decimal ClampDecimal(double value, decimal min, decimal max) =>
        Math.Clamp((decimal)value, min, max);

    // =====================================================================
    // MainForm.Video
    // =====================================================================
    private bool _syncingVideoDuration;
    private Button _videoInstallButton = null!;
    private VideoGenerator _videoGenerator = null!;
    private CancellationTokenSource? _videoCts;

    private void InitializeVideoUi()
    {
        _videoGenerator = new VideoGenerator(
            _s,
            StartComfyAsync,
            () => _comfy.StopAsync(
                TimeSpan.FromSeconds(1)),
            Log);

        _videoGenerator.ProgressChanged += (value, text) =>
            Ui(() =>
            {
                _videoProgress.Value = Math.Clamp(value, 0, 100);
                _videoStatus.Text = text;
            });

        _videoWidth.Value = ClampNumeric(_videoWidth, _s.VideoWidth);
        _videoHeight.Value = ClampNumeric(_videoHeight, _s.VideoHeight);
        _videoFrames.Value = ClampNumeric(_videoFrames, _s.VideoFrames);
        _videoFps.Value = ClampNumeric(_videoFps, _s.VideoFps);
        _videoSteps.Value = ClampNumeric(_videoSteps, _s.VideoSteps);

        var insertionIndex =
            Math.Max(0, _tabs.TabPages.IndexOf(tabInstallation));

        _tabs.TabPages.Insert(
            insertionIndex,
            _tabVideo);

        // FR : 10 onglets doivent rester visibles sans flèches de défilement.
        // EN: Keep all 10 main tabs visible without scroll arrows.
        _tabs.ItemSize = new Size(103, 28);





        _videoNegative.Text =
            "low quality, blurry, out of focus, flicker, jitter, " +
            "camera shake, warped anatomy, deformed motion, " +
            "text, subtitles, watermark, logo, artifacts";





        _videoDurationLabel.Name = "_videoDurationLabel";

        UpdateVideoDurationFromFramesV37();





        _videoModelTitleLabel.Font =
            new Font(
                "Segoe UI Semibold",
                10F,
                FontStyle.Bold);

        _videoModelStatus.AutoSize = false;
        _videoModelStatus.Size = new Size(570, 210);


        _videoOutput.ReadOnly = true;


        _videoInstallButton = new Button
        {
            Location = new Point(625, 354),
            Size = new Size(230, 34),
            BackColor = AppTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Installer modèles vidéo"
        };

        _videoNoteLabel.AutoSize = false;
        _videoNoteLabel.Size = new Size(570, 70);
        _tabVideo.Controls.AddRange(
        [
            _videoPromptLabel,
            _videoPrompt,
            _videoNegativeLabel,
            _videoNegative,
            _videoWidthLabel,
            _videoWidth,
            _videoHeightLabel,
            _videoHeight,
            _videoFramesLabel,
            _videoFrames,
            _videoFpsLabel,
            _videoFps,
            _videoDurationLabel,
            _videoDurationSeconds,
            _videoStepsLabel,
            _videoSteps,
            _videoGenerateButton,
            _videoCancelButton,
            _videoRefreshButton,
            _videoProgress,
            _videoStatus,
            _videoModelTitleLabel,
            _videoModelStatus,
            _videoOutputLabel,
            _videoOutput,
            _videoOpenButton,
            _videoInstallButton,
            _videoNoteLabel
        ]);

        _videoDurationSeconds.ValueChanged += (_, _) =>
            UpdateVideoFramesFromDurationV37();
        _videoFrames.ValueChanged += (_, _) =>
            UpdateVideoDurationFromFramesV37();
        _videoFps.ValueChanged += (_, _) =>
            UpdateVideoDurationFromFramesV37();

        _videoGenerateButton.Click += async (_, _) =>
            await SafeUiAsync(
                "Vidéo Wan",
                () => RunGpuExclusiveAsync("Vidéo Wan", GenerateVideoFromUiAsync));

        _videoCancelButton.Click += async (_, _) =>
        {
            var cts = _videoCts;
            if (cts is null || cts.IsCancellationRequested)
                return;

            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "Annulation de la génération Wan…",
                    "Cancelling Wan generation…");

            // L'UI et les attentes locales s'arrêtent immédiatement, même si
            // ComfyUI ne répond plus. L'interruption serveur est best-effort.
            cts.Cancel();
            await _videoGenerator.CancelActivePromptAsync();
        };

        _videoRefreshButton.Click += (_, _) =>
            RefreshVideoModelStatus();

        _videoOpenButton.Click += (_, _) =>
            OpenGeneratedVideo();

        _videoInstallButton.Click += async (_, _) =>
            await SafeUiAsync(
                "Installation vidéo",
                InstallVideoModelsFromUiAsync);

        _tabs.SelectedIndexChanged +=
            VideoTab_SelectedIndexChanged;

        ApplyVideoTranslations();
        RefreshVideoModelStatus();
    }
    private async void VideoTab_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (_tabs.SelectedTab != _tabVideo)
            return;

        RefreshVideoModelStatus();

        if (_videoPreviewWeb is not null &&
            !_videoPreviewReady)
        {
            try
            {
                await EnsureVideoPreviewWebReadyV36();

                if (!string.IsNullOrWhiteSpace(_videoPreviewPath) &&
                    File.Exists(_videoPreviewPath))
                {
                    await PreviewVideoAsync(_videoPreviewPath);
                }
                else
                {
                    ShowVideoPreviewPlaceholderV36(
                        L10n.Pick(
                            _s.Language,
                            "Cliquez sur une vidéo de l'historique pour la prévisualiser.",
                            "Click a video in the history to preview it."));
                }
            }
            catch (Exception ex)
            {
                Log("WebView ⚠", "Initialisation aperçu vidéo : " + ex.Message);
            }
        }
    }

    private async Task GenerateVideoFromUiAsync()
    {
        var prompt = _videoPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Saisissez un prompt vidéo.",
                    "Enter a video prompt."));
        }

        if (_videoAutoImprovePrompt is not null &&
            _videoAutoImprovePrompt.Checked &&
            !IsVideoPromptAlreadyImprovedV37(prompt))
        {
            _videoStatus.Text = L10n.Pick(
                _s.Language,
                "Amélioration locale du prompt vidéo…",
                "Improving video prompt locally…");

            prompt = await ImprovePromptTextAsync(
                prompt,
                CancellationToken.None,
                modelOverride: null,
                videoPrompt: true);
            _videoPrompt.Text = prompt;
            _s.LastImprovedVideoPromptHash = PromptFingerprint(prompt);
            SettingsStore.Save(_s);
            UpdateVideoPromptEnhancementStateV37();
        }

        var missing = _videoGenerator.GetMissingModels();
        if (missing.Count > 0)
        {
            RefreshVideoModelStatus();
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Les modèles vidéo Wan sont absents. " +
                    "La liste exacte est affichée dans l'onglet Vidéo.",
                    "Wan video models are missing. " +
                    "The exact list is shown in the Video tab."));
        }

        var isImageToVideo =
            VideoGenerator.IsImageToVideoModel(_s.VideoModel);
        var referenceImage =
            _videoReferenceImage?.Text.Trim() ?? string.Empty;

        if (isImageToVideo &&
            (string.IsNullOrWhiteSpace(referenceImage) ||
             !File.Exists(referenceImage)))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Le modèle Wan I2V sélectionné exige une image de référence. Utilisez le champ Image de référence ou sélectionnez une zone.",
                    "The selected Wan I2V model requires a reference image. Use Reference image or select a region."));
        }

        var requestedSteps = Decimal.ToInt32(_videoSteps.Value);
        if (requestedSteps < 12)
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Wan 2.1 T2V 1.3B n'est pas un modèle distillé pour 4 steps. " +
                    "Sous 12 steps, le résultat est souvent du bruit. Utilisez 20 à 30 steps (30 recommandé).",
                    "Wan 2.1 T2V 1.3B is not a 4-step distilled model. " +
                    "Below 12 steps the result is often noise. Use 20 to 30 steps (30 recommended)."));
        }

        SaveVideoGenerationSettings();
        _videoCts?.Dispose();
        _videoCts = new CancellationTokenSource();

        _videoGenerateButton.Enabled = false;
        _videoCancelButton.Enabled = true;
        _videoOpenButton.Enabled = false;
        _videoOutput.Clear();
        _videoProgress.Value = 0;

        try
        {
            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "Libération d'Ollama avant Wan…",
                    "Releasing Ollama before Wan…");

            await StopVisionModelAsync();

            await WaitForCommitRecoveryAsync(
                Math.Max(4096, _s.SafeFreeRamMiB),
                TimeSpan.FromSeconds(30),
                _videoCts.Token);

            var videoStyleId = SelectedTemplateId(
                _videoStyleTemplateCombo,
                _s.VideoStyleTemplate);
            var videoNegativeId = SelectedTemplateId(
                _videoNegativeTemplateCombo,
                _s.VideoNegativeTemplate);

            var generationPrompt = ApplyVideoStyleTemplate(prompt);
            var generationNegative = ApplyVideoNegativeTemplate(_videoNegative.Text.Trim());

            Log(
                "Video",
                $"Templates · style={videoStyleId} · négatif={videoNegativeId}.");

            var result =
                await _videoGenerator.GenerateAsync(
                    generationPrompt,
                    generationNegative,
                    _s.VideoWidth,
                    _s.VideoHeight,
                    _s.VideoFrames,
                    _s.VideoFps,
                    _s.VideoSteps,
                    _videoCts.Token,
                    isImageToVideo
                        ? referenceImage
                        : null);

            if (!result.Ok)
            {
                _videoStatus.ForeColor = AppTheme.Danger;
                _videoStatus.Text =
                    result.Error ??
                    L10n.Pick(
                        _s.Language,
                        "Échec de la génération vidéo.",
                        "Video generation failed.");
                return;
            }

            var finalVideoPath =
                result.Path ?? string.Empty;
            var secondPassApplied = false;

            if (GenerationTemplates.UsesTwoPassMaximumQuality(videoStyleId) &&
                !string.IsNullOrWhiteSpace(finalVideoPath) &&
                File.Exists(finalVideoPath))
            {
                _lastMaximumQualityVideoFirstPassPath = finalVideoPath;
                UpdateMaximumQualitySharpnessUi();
                await RefreshVideoSharpnessPreviewAsync(_videoCts.Token);

                try
                {
                    _videoProgress.Value = 95;
                    _videoStatus.ForeColor = AppTheme.Text;
                    _videoStatus.Text =
                        L10n.Pick(
                            _s.Language,
                            "Passe 2/2 · upscale et amélioration vidéo…",
                            "Pass 2/2 · video upscaling and enhancement…");

                    finalVideoPath =
                        await _qualityPostProcessor.EnhanceVideoAsync(
                            finalVideoPath,
                            _videoCts.Token);

                    secondPassApplied = true;
                    _videoProgress.Value = 100;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log(
                        "Qualité max !",
                        "Passe 2 vidéo échouée, passe 1 conservée : " +
                        ex.Message);
                }
            }

            _videoStatus.ForeColor = AppTheme.Text;
            _videoStatus.Text =
                secondPassApplied
                    ? L10n.Pick(
                        _s.Language,
                        "Terminé · Qualité maximale · 2 passes",
                        "Done · Maximum quality · 2 passes")
                    : L10n.Pick(
                        _s.Language,
                        "Terminé",
                        "Done");

            _videoOutput.Text = finalVideoPath;
            _videoOpenButton.Enabled =
                !string.IsNullOrWhiteSpace(finalVideoPath);

            if (!string.IsNullOrWhiteSpace(finalVideoPath) &&
                File.Exists(finalVideoPath))
            {
                await PreviewVideoAsync(finalVideoPath);
                if (secondPassApplied &&
                    _videoSharpnessBeforePreview.Image is not null)
                {
                    SetVideoSharpnessComparisonVisible(true);
                }
            }

            RefreshVideoHistory();
        }
        finally
        {
            _videoCancelButton.Enabled = false;
            _videoGenerateButton.Enabled =
                _videoGenerator
                    .GetMissingModels()
                    .Count == 0;

            _videoCts.Dispose();
            _videoCts = null;
        }
    }

    private async Task InstallVideoModelsFromUiAsync()
    {
        var missing = _videoGenerator.GetMissingModels();
        if (missing.Count == 0)
        {
            RefreshVideoModelStatus();
            _videoStatus.Text = L10n.Pick(
                _s.Language,
                "Tous les modèles vidéo Wan sont déjà installés.",
                "All Wan video models are already installed.");
            return;
        }

        if (_videoCts is not null)
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Une opération vidéo est déjà en cours.",
                    "A video operation is already running."));

        var confirmation = MessageBox.Show(
            L10n.Pick(
                _s.Language,
                "L'installation vidéo va télécharger les modèles officiels Wan 2.1 nécessaires.\n\n" +
                "Téléchargement total maximal : environ 9,2 GiB.\n" +
                "Les fichiers seront vérifiés par SHA256 avant utilisation.\n\n" +
                "Continuer ?",
                "Video setup will download the required official Wan 2.1 models.\n\n" +
                "Maximum total download: about 9.2 GiB.\n" +
                "Files will be verified with SHA256 before use.\n\n" +
                "Continue?"),
            L10n.Pick(
                _s.Language,
                "Installer les modèles vidéo",
                "Install video models"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (confirmation != DialogResult.Yes)
            return;

        _videoCts = new CancellationTokenSource();
        _videoInstallButton.Enabled = false;
        _videoGenerateButton.Enabled = false;
        _videoCancelButton.Enabled = true;
        _videoProgress.Value = 0;

        Action<int, string> progress = (value, text) =>
            Ui(() =>
            {
                _videoProgress.Value = Math.Clamp(value, 0, 100);
                _videoStatus.Text = text;
            });

        _installer.ProgressChanged += progress;

        try
        {
            Log(
                "Video",
                "Installation explicite des modèles vidéo Wan 2.1 démarrée.");

            await _installer.InstallVideoModelsAsync(
                _videoCts.Token);

            RefreshVideoModelStatus();
            RefreshInstallationVideoStatus();
            RefreshFeatureAvailability();
            _videoStatus.Text = L10n.Pick(
                _s.Language,
                "Modèles vidéo Wan installés et vérifiés.",
                "Wan video models installed and verified.");
        }
        finally
        {
            _installer.ProgressChanged -= progress;
            _videoCancelButton.Enabled = false;

            _videoCts.Dispose();
            _videoCts = null;

            RefreshVideoModelStatus();
        }
    }

    private void RefreshVideoModelStatus()
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var isI2v = VideoGenerator.IsImageToVideoModel(_s.VideoModel);

        var models = new List<(string Label, string Path)>
        {
            (
                isI2v ? "Wan I2V" : "Wan T2V",
                Path.Combine(
                    root,
                    "models",
                    "diffusion_models",
                    _s.VideoModel)),
            (
                "UMT5 XXL",
                Path.Combine(
                    root,
                    "models",
                    "text_encoders",
                    _s.VideoTextEncoderModel)),
            (
                "Wan VAE",
                Path.Combine(
                    root,
                    "models",
                    "vae",
                    _s.VideoVaeModel))
        };

        if (isI2v)
        {
            models.Add(
                (
                    "CLIP Vision",
                    Path.Combine(
                        root,
                        "models",
                        "clip_vision",
                        _s.VideoClipVisionModel)));
        }

        var lines = models.Select(model =>
            (File.Exists(model.Path)
                ? "[OK] "
                : "[X] ") +
            model.Label +
            " — " +
            (File.Exists(model.Path)
                ? L10n.Pick(
                    _s.Language,
                    "installé",
                    "installed")
                : L10n.Pick(
                    _s.Language,
                    "non installé",
                    "not installed")) +
            Environment.NewLine +
            "    " +
            Path.GetFileName(model.Path));

        _videoModelStatus.Text =
            string.Join(
                Environment.NewLine +
                Environment.NewLine,
                lines);

        var missingLabels = models
            .Where(model => !File.Exists(model.Path))
            .Select(model => model.Label)
            .ToArray();

        _videoGenerateButton.Enabled =
            missingLabels.Length == 0 &&
            _videoCts is null &&
            !_gpuUiLocked;

        if (_videoCts is null && missingLabels.Length > 0)
        {
            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "Dépendances manquantes : ",
                    "Missing dependencies: ") +
                string.Join(", ", missingLabels);
            _videoStatus.ForeColor = AppTheme.Warning;
        }
        else if (_videoCts is null)
        {
            _videoStatus.ForeColor = AppTheme.Text;
        }

        // L'installation des modèles est centralisée dans l'onglet Installation.
        _videoInstallButton.Visible = false;
        _videoInstallButton.Enabled = false;

        RefreshRuntimeModelChoicesV36();
    }

    private void OpenGeneratedVideo()
    {
        var path = _videoOutput.Text.Trim();
        if (!File.Exists(path))
            return;

        Process.Start(
            new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
    }

    private void UpdateVideoFramesFromDurationV37()
    {
        if (_syncingVideoDuration ||
            _videoDurationSeconds is null ||
            _videoFrames is null ||
            _videoFps is null)
        {
            return;
        }

        _syncingVideoDuration = true;
        try
        {
            var fps = Math.Max(1, Decimal.ToInt32(_videoFps.Value));
            var requestedSeconds =
                Decimal.ToDouble(_videoDurationSeconds.Value);
            var requestedFrames =
                Math.Max(1, (int)Math.Round(
                    requestedSeconds * fps,
                    MidpointRounding.AwayFromZero));

            var wanFrames =
                1 +
                (4 * (int)Math.Round(
                    (requestedFrames - 1) / 4.0,
                    MidpointRounding.AwayFromZero));
            wanFrames = Math.Clamp(
                wanFrames,
                Decimal.ToInt32(_videoFrames.Minimum),
                Decimal.ToInt32(_videoFrames.Maximum));

            _videoFrames.Value = wanFrames;
            UpdateVideoDurationValueV37(wanFrames, fps);
        }
        finally
        {
            _syncingVideoDuration = false;
        }
    }

    private void UpdateVideoDurationFromFramesV37()
    {
        if (_syncingVideoDuration ||
            _videoDurationSeconds is null ||
            _videoFrames is null ||
            _videoFps is null)
        {
            return;
        }

        _syncingVideoDuration = true;
        try
        {
            var fps = Math.Max(1, Decimal.ToInt32(_videoFps.Value));
            var frames = Decimal.ToInt32(_videoFrames.Value);
            UpdateVideoDurationValueV37(frames, fps);
        }
        finally
        {
            _syncingVideoDuration = false;
        }
    }

    private void UpdateVideoDurationValueV37(int frames, int fps)
    {
        var maxSeconds = Math.Max(
            _videoDurationSeconds.Minimum,
            Math.Round(
                _videoFrames.Maximum / fps,
                2,
                MidpointRounding.AwayFromZero));
        _videoDurationSeconds.Maximum = maxSeconds;

        var seconds = Math.Round(
            (decimal)frames / fps,
            2,
            MidpointRounding.AwayFromZero);
        _videoDurationSeconds.Value = Math.Clamp(
            seconds,
            _videoDurationSeconds.Minimum,
            _videoDurationSeconds.Maximum);

        _videoDurationSeconds.AccessibleDescription =
            L10n.Pick(
                _s.Language,
                $"Durée réelle Wan : {_videoDurationSeconds.Value:0.00} s · {frames} frames à {fps} FPS.",
                $"Actual Wan duration: {_videoDurationSeconds.Value:0.00} s · {frames} frames at {fps} FPS.");
    }

    private void ApplyVideoTranslations()
    {
        if (_tabVideo is null)
            return;

        _tabVideo.Text =
            L10n.Pick(
                _s.Language,
                "Vidéo",
                "Video");

        _videoDurationLabel.Text =
            L10n.Pick(
                _s.Language,
                "Durée (s)",
                "Duration (s)");
        UpdateVideoDurationFromFramesV37();

        _videoGenerateButton.Text =
            L10n.Pick(
                _s.Language,
                "Générer la vidéo",
                "Generate video");
        _videoCancelButton.Text =
            L10n.Pick(
                _s.Language,
                "Annuler",
                "Cancel");

        _videoRefreshButton.Text =
            L10n.Pick(
                _s.Language,
                "Actualiser",
                "Refresh");

        _videoOpenButton.Text =
            L10n.Pick(
                _s.Language,
                "Ouvrir la vidéo",
                "Open video");

        _videoInstallButton.Text =
            L10n.Pick(
                _s.Language,
                "Installer modèles vidéo",
                "Install video models");
    }

    // =====================================================================
    // MainForm.V36
    // =====================================================================
    private double _imagePreviewZoom = 1d;
    private Point _imagePreviewPan;
    private Point _imagePreviewDragOrigin;
    private Point _imagePreviewPanOrigin;
    private bool _imagePreviewDragging;
    private Control? _imagePreviewDragCapture;

    private bool _videoPreviewReady;
    private string? _videoPreviewPath;

    private bool _gpuUiLocked;
    private readonly Dictionary<Control, bool> _gpuUiEnabledSnapshot = new();
    private bool _comfyDirectTabBusy;
    private bool _applyingVideoQualityPreset;
    private sealed record VideoQualityPreset(
        string Id,
        string Fr,
        string En,
        int Steps)
    {
        public override string ToString() => Fr;
    }

    private static readonly VideoQualityPreset[] VideoQualityPresets =
    [
        new("fast", "Rapide · 20 steps", "Fast · 20 steps", 20),
        new("standard", "Standard · 30 steps", "Standard · 30 steps", 30),
        new("quality", "Qualité · 40 steps", "Quality · 40 steps", 40),
        new("best", "Meilleure · 50 steps", "Best · 50 steps", 50),
        new("custom", "Personnalisé", "Custom", 0)
    ];

    private void InitializeV36Ui()
    {
        InitializeImageZoomPanV36();
        InitializeRuntimeModelSelectorsV36();
        InitializeVisionPromptExtractionV36();
        InitializeVideoReferenceUiV36();
        InitializeVideoPreviewV36();
        InitializeVideoQualityUiV36();
        InitializeLogTabColorsV36();
        ConfigureComfyWebViewEnvironmentV36();
        InitializeComfyStatusPanelV36();
        InitializeComfyDirectTabV36();
        FixV36Layout();
    }

    private void InitializeImageZoomPanV36()
    {
        var oldBounds = _preview.Bounds;
        var oldAnchor = _preview.Anchor;

        tabGenerate.Controls.Remove(_preview);


        _preview.Parent = _imagePreviewViewport;
        _preview.BorderStyle = BorderStyle.None;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.Anchor = AnchorStyles.None;
        _preview.Cursor = Cursors.Default;

        tabGenerate.Controls.Add(_imagePreviewViewport);
        _imagePreviewViewport.Controls.Add(_preview);

        _imagePreviewViewport.MouseEnter += (_, _) => _imagePreviewViewport.Focus();
        _preview.MouseEnter += (_, _) => _imagePreviewViewport.Focus();

        _imagePreviewViewport.MouseWheel += ImagePreview_MouseWheel;
        _preview.MouseWheel += ImagePreview_MouseWheel;
        _imagePreviewViewport.MouseDown += ImagePreview_MouseDown;
        _imagePreviewViewport.MouseMove += ImagePreview_MouseMove;
        _imagePreviewViewport.MouseUp += ImagePreview_MouseUp;
        _preview.MouseDown += ImagePreview_MouseDown;
        _preview.MouseMove += ImagePreview_MouseMove;
        _preview.MouseUp += ImagePreview_MouseUp;
        _preview.DoubleClick += (_, _) => ResetImagePreviewView();
        _imagePreviewViewport.Resize += (_, _) => UpdateImagePreviewLayout();
    }

    private void ImagePreview_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_preview.Image is null)
            return;

        var factor = e.Delta > 0 ? 1.18d : 1d / 1.18d;
        _imagePreviewZoom = Math.Clamp(_imagePreviewZoom * factor, 1d, 12d);

        if (_imagePreviewZoom <= 1.001d)
            _imagePreviewPan = Point.Empty;

        UpdateImagePreviewLayout();
    }

    private void ImagePreview_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _preview.Image is null)
            return;

        _imagePreviewDragging = true;
        _imagePreviewDragOrigin = Cursor.Position;
        _imagePreviewPanOrigin = _imagePreviewPan;
        _imagePreviewDragCapture = sender as Control ?? _preview;
        _imagePreviewDragCapture.Capture = true;
        _preview.Cursor = Cursors.SizeAll;
        _imagePreviewViewport.Cursor = Cursors.SizeAll;
    }

    private void ImagePreview_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_imagePreviewDragging)
            return;

        var now = Cursor.Position;
        _imagePreviewPan = new Point(
            _imagePreviewPanOrigin.X + now.X - _imagePreviewDragOrigin.X,
            _imagePreviewPanOrigin.Y + now.Y - _imagePreviewDragOrigin.Y);
        UpdateImagePreviewLayout();
    }

    private void ImagePreview_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        _imagePreviewDragging = false;
        if (_imagePreviewDragCapture is not null)
            _imagePreviewDragCapture.Capture = false;
        _imagePreviewDragCapture = null;
        var cursor = _imagePreviewZoom > 1.001d
            ? Cursors.Hand
            : Cursors.Default;
        _preview.Cursor = cursor;
        _imagePreviewViewport.Cursor = cursor;
    }

    private void ResetImagePreviewView()
    {
        _imagePreviewZoom = 1d;
        _imagePreviewPan = Point.Empty;
        UpdateImagePreviewLayout();
    }

    private void UpdateImagePreviewLayout()
    {
        if (_imagePreviewViewport is null || _preview.Image is null)
        {
            if (_imagePreviewViewport is not null)
            {
                _preview.Location = Point.Empty;
                _preview.Size = _imagePreviewViewport.ClientSize;
            }
            return;
        }

        var image = _preview.Image;
        var viewport = _imagePreviewViewport.ClientSize;

        if (viewport.Width <= 1 || viewport.Height <= 1 ||
            image.Width <= 0 || image.Height <= 0)
            return;

        var fit = Math.Min(
            viewport.Width / (double)image.Width,
            viewport.Height / (double)image.Height);

        var scale = fit * _imagePreviewZoom;
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));

        var baseX = (viewport.Width - width) / 2;
        var baseY = (viewport.Height - height) / 2;

        var maxPanX = Math.Max(0, (width - viewport.Width) / 2 + 40);
        var maxPanY = Math.Max(0, (height - viewport.Height) / 2 + 40);
        _imagePreviewPan = new Point(
            Math.Clamp(_imagePreviewPan.X, -maxPanX, maxPanX),
            Math.Clamp(_imagePreviewPan.Y, -maxPanY, maxPanY));

        _preview.Bounds = new Rectangle(
            baseX + _imagePreviewPan.X,
            baseY + _imagePreviewPan.Y,
            width,
            height);
        _preview.Cursor = _imagePreviewZoom > 1.001d
            ? Cursors.Hand
            : Cursors.Default;
    }

    private void InitializeVisionPromptExtractionV36()
    {
        _imageExtractPromptButton.FlatAppearance.BorderSize = 0;
        tabGenerate.Controls.Add(_imageExtractPromptButton);

        _imageExtractPromptButton.Click += async (_, _) =>
            await SafeUiAsync(
                "Extraction prompt image",
                () => RunGpuExclusiveAsync(
                    "Vision image",
                    ExtractImagePromptFromUiAsync));
    }

    private async Task ExtractImagePromptFromUiAsync()
    {
        var path = txtInputImage.Text.Trim();
        if (!File.Exists(path))
        {
            using var ofd = CreateImageOpenDialog(
                L10n.Pick(
                    _s.Language,
                    "Choisir l'image à analyser",
                    "Choose the image to analyze"));

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            path = ofd.FileName;
            txtInputImage.Text = path;
            ShowPreviewImage(path);
        }

        _imageExtractPromptButton.Enabled = false;
        try
        {
            _genText.Text = L10n.Pick(
                _s.Language,
                "Analyse de l'image avec le modèle vision…",
                "Analyzing image with the vision model…");

            var prompt = await ExtractPromptWithVisionAsync(
                path,
                CancellationToken.None);

            _prompt.Text = prompt;
            _genText.Text = L10n.Pick(
                _s.Language,
                "Prompt extrait de l'image.",
                "Prompt extracted from image.");
        }
        finally
        {
            _imageExtractPromptButton.Enabled = !_gpuUiLocked;
        }
    }

    private static OpenFileDialog CreateImageOpenDialog(string title) =>
        new()
        {
            Title = title,
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tous les fichiers|*.*",
            Multiselect = false
        };

    private async Task<string> ExtractPromptWithVisionAsync(
        string path,
        CancellationToken ct)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Image source introuvable.", path);

        if (!IsPortableOllamaModelInstalled(_s.VisionModel))
        {
            throw new InvalidOperationException(
                $"Le modèle vision {_s.VisionModel} n'est pas installé dans Ollama portable. " +
                "Activez l'installation du modèle Vision dans Configuration puis utilisez Installation.");
        }

        await _comfy.StopAsync(TimeSpan.FromSeconds(1));
        await StartOllamaAsync();

        var bytes = await File.ReadAllBytesAsync(path, ct);
        var body = JsonSerializer.Serialize(
            new
            {
                model = _s.VisionModel,
                prompt =
                    "Analyze this image for an image/video generation workflow. " +
                    "Return only one concise but detailed English prompt describing the visible subject, " +
                    "appearance, environment, composition, lighting, camera/viewpoint and relevant action. " +
                    "Do not add explanations, headings or quotation marks.",
                images = new[]
                {
                    Convert.ToBase64String(bytes)
                },
                stream = false,
                keep_alive = 0
            });

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };

        using var response = await http.PostAsync(
            $"http://127.0.0.1:{_s.OllamaPort}/api/generate",
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama vision HTTP {(int)response.StatusCode}: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var result = doc.RootElement.TryGetProperty("response", out var value)
            ? value.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Le modèle vision n'a renvoyé aucun prompt.");

        return result.Trim().Trim('"');
    }


    private sealed record RuntimeModelChoice(
        string FileName,
        string Display,
        string Kind,
        string Url = "",
        string Sha256 = "",
        string TargetFolder = "diffusion_models",
        bool AdultOnly = false,
        bool Installed = true,
        bool Catalog = false,
        bool ActivatesTextEncoder = false,
        bool RequiresHuggingFaceAuth = false,
        string SourceUrl = "",
        string LicenseId = "",
        string LicenseNote = "",
        long FileSizeBytes = 0)
    {
        public override string ToString() => Display;
    }

    private void InitializeRuntimeModelSelectorsV36()
    {


        _imageImportModelButton.FlatAppearance.BorderSize = 0;

        tabGenerate.Controls.AddRange(
        [
            _imageModelRuntimeLabel,
            _imageModelRuntimeCombo,
            _imageImportModelButton
        ]);



        _videoImportModelButton.FlatAppearance.BorderSize = 0;
        _generationTemplateTips.SetToolTip(
            _videoImportModelButton,
            L10n.Pick(
                _s.Language,
                "Importer un checkpoint Wan T2V local compatible.",
                "Import a compatible local Wan T2V checkpoint."));

        _tabVideo.Controls.AddRange(
        [
            _videoModelRuntimeLabel,
            _videoModelRuntimeCombo,
            _videoImportModelButton
        ]);

        RefreshRuntimeModelChoicesV36();

        _imageModelRuntimeCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateRuntimeCatalogTooltipV37(_imageModelRuntimeCombo);

            RefreshCatalogDownloadUiV37();

            if (_refreshingRuntimeCatalogChoicesV37 ||
                _imageModelRuntimeCombo.SelectedItem is not RuntimeModelChoice choice)
            {
                return;
            }

            if (choice.Catalog && !choice.Installed)
                return;

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Sélection du modèle Image", "Image model selection"),
                () => HandleRuntimeModelChoiceV37Async(choice, imageModel: true));
        };

        _videoModelRuntimeCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateRuntimeCatalogTooltipV37(_videoModelRuntimeCombo);

            RefreshCatalogDownloadUiV37();

            if (_refreshingRuntimeCatalogChoicesV37 ||
                _videoModelRuntimeCombo.SelectedItem is not RuntimeModelChoice choice)
            {
                return;
            }

            if (choice.Catalog && !choice.Installed)
                return;

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Sélection du modèle Vidéo", "Video model selection"),
                () => HandleRuntimeModelChoiceV37Async(choice, imageModel: false));
        };

        _imageImportModelButton.Text =
            L10n.Pick(_s.Language, "Ajouter…", "Add…");
        _imageImportModelButton.Click += (_, _) =>
            ShowRuntimeModelAddMenuV36(
                _imageImportModelButton,
                imageModel: true);

        _videoImportModelButton.Click += (_, _) =>
            ShowRuntimeModelAddMenuV36(
                _videoImportModelButton,
                imageModel: false);

        UpdateRuntimeCatalogTooltipV37(_imageModelRuntimeCombo);
        UpdateRuntimeCatalogTooltipV37(_videoModelRuntimeCombo);
    }

    private void ShowRuntimeModelAddMenuV36(
        Control anchor,
        bool imageModel)
    {
        var menu = new ContextMenuStrip
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.Text,
            ShowImageMargin = false
        };

        var import = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Importer un fichier local…",
                "Import local file…"));

        var download = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Télécharger depuis une URL…",
                "Download from URL…"));

        import.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    imageModel ? "Import modèle Image" : "Import modèle Vidéo",
                    imageModel ? "Import Image model" : "Import Video model"),
                () => ImportRuntimeModelAsync(imageModel));

        download.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    imageModel ? "Téléchargement modèle Image" : "Téléchargement modèle Vidéo",
                    imageModel ? "Download Image model" : "Download Video model"),
                () => DownloadRuntimeModelFromUrlAsync(imageModel));

        menu.Items.Add(import);
        menu.Items.Add(download);

        if (imageModel)
        {
            var textEncoder =
                new ToolStripMenuItem(
                    L10n.Pick(
                        _s.Language,
                        "Télécharger un encodeur texte FLUX.2 tiers…",
                        "Download a third-party FLUX.2 text encoder…"));

            textEncoder.Click += async (_, _) =>
                await SafeUiAsync(
                    L10n.Pick(
                        _s.Language,
                        "Encodeur texte FLUX.2 tiers",
                        "Third-party FLUX.2 text encoder"),
                    DownloadExternalFluxTextEncoderV36Async);

            menu.Items.Add(textEncoder);
        }

        if (!imageModel)
        {
            menu.Items.Add(new ToolStripSeparator());

            var officialI2v =
                new ToolStripMenuItem(
                    L10n.Pick(
                        _s.Language,
                        "Installer Wan 2.1 I2V 480p 14B FP8 officiel…",
                        "Install official Wan 2.1 I2V 480p 14B FP8…"));

            officialI2v.Click += async (_, _) =>
                await SafeUiAsync(
                    "Wan I2V",
                    DownloadOfficialVideoI2vV36Async);

            var clipVision =
                new ToolStripMenuItem(
                    L10n.Pick(
                        _s.Language,
                        "Installer seulement CLIP Vision pour I2V…",
                        "Install only CLIP Vision for I2V…"));

            clipVision.Click += async (_, _) =>
                await SafeUiAsync(
                    "CLIP Vision I2V",
                    DownloadVideoClipVisionV36Async);

            menu.Items.Add(officialI2v);
            menu.Items.Add(clipVision);
        }

        AppendRuntimeCatalogMenuV37(
            menu,
            imageModel);

        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private async Task DownloadExternalFluxTextEncoderV36Async()
    {
        using var dialog =
            new ModelDownloadForm(
                imageModel: true,
                _s.Language)
            {
                Text = L10n.Pick(
                    _s.Language,
                    "Télécharger un encodeur texte FLUX.2 tiers",
                    "Download a third-party FLUX.2 text encoder")
            };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var comfyRoot =
            PortablePreflight.GetComfyRoot(_s);
        var folder =
            Path.Combine(
                comfyRoot,
                "models",
                "text_encoders");
        Directory.CreateDirectory(folder);

        var destination =
            Path.Combine(
                folder,
                dialog.FileName);

        if (File.Exists(destination))
        {
            var replace =
                MessageBox.Show(
                    this,
                    L10n.Pick(
                        _s.Language,
                        $"Le fichier {dialog.FileName} existe déjà. Le remplacer ?",
                        $"{dialog.FileName} already exists. Replace it?"),
                    L10n.Pick(
                        _s.Language,
                        "Encodeur existant",
                        "Existing encoder"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (replace != DialogResult.Yes)
                return;
        }

        var tempDir =
            Path.Combine(
                PortablePaths.RuntimeDir,
                "external-model-downloads");
        Directory.CreateDirectory(tempDir);

        var temp =
            Path.Combine(
                tempDir,
                Guid.NewGuid().ToString("N") +
                ".safetensors");

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    _genProgress.Value =
                        Math.Clamp(
                            value,
                            _genProgress.Minimum,
                            _genProgress.Maximum);
                    _genText.Text = message;
                });

        _installer.ProgressChanged += progress;

        try
        {
            await _installer.DownloadExternalModelAsync(
                dialog.ModelUrl,
                temp,
                dialog.Sha256,
                CancellationToken.None);

            var compatibility =
                Flux2ModelCompatibility.Validate(
                    temp,
                    Flux2ModelRole.TextEncoder);

            if (compatibility.State ==
                ModelCompatibilityState.Incompatible)
            {
                throw new InvalidOperationException(
                    "L'encodeur téléchargé n'est pas compatible avec le chemin FLUX.2 Klein actuel : " +
                    compatibility.Reason);
            }

            File.Move(
                temp,
                destination,
                overwrite: true);

            _s.TextEncoderModel =
                Path.GetFileName(destination);
            txtTextEncoder.Text =
                _s.TextEncoderModel;

            SettingsStore.Save(_s);
            await RefreshModelChoicesAsync();
            RefreshFeatureAvailability();

            Log(
                "UI",
                $"Encodeur texte FLUX.2 tiers activé · {Path.GetFileName(destination)} · {compatibility.Reason}.");
        }
        finally
        {
            _installer.ProgressChanged -= progress;

            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
            }
        }
    }

    private async Task DownloadOfficialVideoI2vV36Async()
    {
        var confirm = MessageBox.Show(
            this,
            L10n.Pick(
                _s.Language,
                "Le checkpoint Wan 2.1 I2V 480p 14B FP8 fait environ 16,4 Go, auxquels s'ajoute CLIP Vision (~1,26 Go).\n\n" +
                "Le téléchargement est optionnel et peut être long. Le modèle est exigeant en VRAM/RAM et DreamRaster conservera le modèle T2V actuel sur disque.\n\n" +
                "Télécharger et activer ce modèle I2V ?",
                "The Wan 2.1 I2V 480p 14B FP8 checkpoint is about 16.4 GB, plus CLIP Vision (~1.26 GB).\n\n" +
                "This optional download can take a long time. The model is demanding on VRAM/RAM and DreamRaster will keep the current T2V model on disk.\n\n" +
                "Download and activate this I2V model?"),
            "Wan 2.1 I2V",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (confirm != DialogResult.Yes)
            return;

        var root = PortablePreflight.GetComfyRoot(_s);
        var diffusion = Path.Combine(root, "models", "diffusion_models");
        var clipVisionDir = Path.Combine(root, "models", "clip_vision");
        Directory.CreateDirectory(diffusion);
        Directory.CreateDirectory(clipVisionDir);

        var modelPath = Path.Combine(diffusion, _s.VideoI2vModel);
        var clipPath = Path.Combine(clipVisionDir, _s.VideoClipVisionModel);

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    _videoProgress.Value =
                        Math.Clamp(
                            value,
                            _videoProgress.Minimum,
                            _videoProgress.Maximum);
                    _videoStatus.Text = message;
                });

        _installer.ProgressChanged += progress;
        try
        {
            await _installer.EnsureExternalModelAsync(
                _s.VideoI2vModelUrl,
                modelPath,
                _s.VideoI2vModelSha256,
                CancellationToken.None);

            await _installer.EnsureExternalModelAsync(
                _s.VideoClipVisionUrl,
                clipPath,
                _s.VideoClipVisionSha256,
                CancellationToken.None);

            _s.VideoModel = _s.VideoI2vModel;
            if (_cfgVideoModel is not null)
                _cfgVideoModel.Text = _s.VideoModel;

            SettingsStore.Save(_s);
            RefreshRuntimeModelChoicesV36();
            RefreshVideoModelStatus();
            RefreshFeatureAvailability();

            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "Wan I2V officiel installé et activé.",
                    "Official Wan I2V installed and activated.");
        }
        finally
        {
            _installer.ProgressChanged -= progress;
        }
    }

    private async Task DownloadVideoClipVisionV36Async()
    {
        var root =
            PortablePreflight.GetComfyRoot(_s);
        var folder =
            Path.Combine(
                root,
                "models",
                "clip_vision");
        Directory.CreateDirectory(folder);

        var destination =
            Path.Combine(
                folder,
                _s.VideoClipVisionModel);

        if (File.Exists(destination))
        {
            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "CLIP Vision I2V est déjà installé.",
                    "I2V CLIP Vision is already installed.");
            return;
        }

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    _videoProgress.Value =
                        Math.Clamp(
                            value,
                            _videoProgress.Minimum,
                            _videoProgress.Maximum);
                    _videoStatus.Text = message;
                });

        _installer.ProgressChanged += progress;
        try
        {
            await _installer.EnsureExternalModelAsync(
                _s.VideoClipVisionUrl,
                destination,
                _s.VideoClipVisionSha256,
                CancellationToken.None);

            RefreshVideoModelStatus();
            RefreshFeatureAvailability();

            _videoStatus.Text =
                L10n.Pick(
                    _s.Language,
                    "CLIP Vision I2V installé.",
                    "I2V CLIP Vision installed.");
        }
        finally
        {
            _installer.ProgressChanged -= progress;
        }
    }

    private async Task DownloadRuntimeModelFromUrlAsync(bool imageModel)
    {
        using var dialog =
            new ModelDownloadForm(
                imageModel,
                _s.Language);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var comfyRoot =
            PortablePreflight.GetComfyRoot(_s);
        var modelDir =
            Path.Combine(
                comfyRoot,
                "models",
                "diffusion_models");
        Directory.CreateDirectory(modelDir);

        var destination =
            Path.Combine(
                modelDir,
                dialog.FileName);

        if (File.Exists(destination))
        {
            var replace =
                MessageBox.Show(
                    this,
                    L10n.Pick(
                        _s.Language,
                        $"Le fichier {dialog.FileName} existe déjà. Le remplacer ?",
                        $"{dialog.FileName} already exists. Replace it?"),
                    L10n.Pick(
                        _s.Language,
                        "Modèle existant",
                        "Existing model"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (replace != DialogResult.Yes)
                return;
        }

        var tempDir =
            Path.Combine(
                PortablePaths.RuntimeDir,
                "external-model-downloads");
        Directory.CreateDirectory(tempDir);

        var temp =
            Path.Combine(
                tempDir,
                Guid.NewGuid().ToString("N") +
                ".safetensors");

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    if (imageModel)
                    {
                        _genProgress.Value =
                            Math.Clamp(
                                value,
                                _genProgress.Minimum,
                                _genProgress.Maximum);
                        _genText.Text = message;
                    }
                    else
                    {
                        _videoProgress.Value =
                            Math.Clamp(
                                value,
                                _videoProgress.Minimum,
                                _videoProgress.Maximum);
                        _videoStatus.Text = message;
                    }
                });

        _installer.ProgressChanged += progress;

        try
        {
            await _installer.DownloadExternalModelAsync(
                dialog.ModelUrl,
                temp,
                dialog.Sha256,
                CancellationToken.None);

            if (imageModel)
            {
                var compatibility =
                    Flux2ModelCompatibility.Validate(
                        temp,
                        Flux2ModelRole.Diffusion);

                if (compatibility.State ==
                    ModelCompatibilityState.Incompatible)
                {
                    throw new InvalidOperationException(
                        "Le checkpoint téléchargé n'est pas compatible avec le workflow FLUX.2 actuel : " +
                        compatibility.Reason);
                }
            }
            else
            {
                var kind =
                    VideoModelKindFromName(
                        dialog.FileName);

                if (kind == "vace")
                {
                    throw new InvalidOperationException(
                        "Ce checkpoint VACE requiert un workflow de contrôle/masque différent. " +
                        "La v37 le détecte mais ne l'active pas comme T2V/I2V.");
                }
            }

            File.Move(
                temp,
                destination,
                overwrite: true);

            if (imageModel)
            {
                _s.FluxModel =
                    Path.GetFileName(destination);
            }
            else
            {
                _s.VideoModel =
                    Path.GetFileName(destination);

                if (_cfgVideoModel is not null)
                    _cfgVideoModel.Text =
                        _s.VideoModel;
            }

            SettingsStore.Save(_s);
            RefreshRuntimeModelChoicesV36();
            RefreshFeatureAvailability();
            RefreshVideoModelStatus();

            Log(
                "UI",
                $"Modèle tiers activé · {Path.GetFileName(destination)}.");
        }
        finally
        {
            _installer.ProgressChanged -= progress;

            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
            }
        }
    }

    private void RefreshRuntimeModelChoicesV36()
    {
        if (_imageModelRuntimeCombo is null || _videoModelRuntimeCombo is null)
            return;

        var comfyRoot = PortablePreflight.GetComfyRoot(_s);
        var folder = Path.Combine(comfyRoot, "models", "diffusion_models");
        Directory.CreateDirectory(folder);

        var files = Directory.EnumerateFiles(folder, "*.safetensors")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var imageChoices = new List<RuntimeModelChoice>();
        foreach (var path in files)
        {
            var compatibility = Flux2ModelCompatibility.Validate(
                path,
                Flux2ModelRole.Diffusion);

            if (compatibility.State is not
                (ModelCompatibilityState.Compatible or ModelCompatibilityState.Unverified))
            {
                continue;
            }

            var name = Path.GetFileName(path);
            imageChoices.Add(
                new RuntimeModelChoice(
                    name,
                    (compatibility.State == ModelCompatibilityState.Compatible
                        ? "[OK] "
                        : "[?] ") +
                    name,
                    "image"));
        }

        if (!imageChoices.Any(x =>
                string.Equals(x.FileName, _s.FluxModel, StringComparison.OrdinalIgnoreCase)))
        {
            imageChoices.Add(
                new RuntimeModelChoice(
                    _s.FluxModel,
                    "[X] " + _s.FluxModel,
                    "image"));
        }

        AddRuntimeModelCatalogChoicesV37(
            imageChoices,
            imageModel: true);

        FillRuntimeModelCombo(
            _imageModelRuntimeCombo,
            imageChoices,
            _s.FluxModel);

        var videoChoices = files
            .Select(path => Path.GetFileName(path))
            .Where(name =>
                name.StartsWith("wan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, _s.VideoModel, StringComparison.OrdinalIgnoreCase))
            .Select(name =>
            {
                var kind = VideoModelKindFromName(name);
                var marker = kind switch
                {
                    "t2v" => "[T2V]",
                    "i2v" => "[I2V]",
                    "vace" => "[VACE]",
                    _ => "[?]"
                };

                return new RuntimeModelChoice(
                    name,
                    marker + " " + name,
                    kind);
            })
            .ToList();

        if (!videoChoices.Any(x =>
                string.Equals(x.FileName, _s.VideoModel, StringComparison.OrdinalIgnoreCase)))
        {
            videoChoices.Add(
                new RuntimeModelChoice(
                    _s.VideoModel,
                    "[X] " + _s.VideoModel,
                    VideoModelKindFromName(_s.VideoModel)));
        }

        AddRuntimeModelCatalogChoicesV37(
            videoChoices,
            imageModel: false);

        FillRuntimeModelCombo(
            _videoModelRuntimeCombo,
            videoChoices,
            _s.VideoModel);
    }

    private static string VideoModelKindFromName(string name)
    {
        if (name.Contains("i2v", StringComparison.OrdinalIgnoreCase))
            return "i2v";
        if (name.Contains("vace", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("fun_inp", StringComparison.OrdinalIgnoreCase))
            return "vace";
        if (name.Contains("t2v", StringComparison.OrdinalIgnoreCase))
            return "t2v";
        return "custom";
    }

    private void FillRuntimeModelCombo(
        ComboBox combo,
        IEnumerable<RuntimeModelChoice> choices,
        string selectedFile)
    {
        var list = choices
            .GroupBy(
                x => x.TargetFolder + "|" + x.FileName,
                StringComparer.OrdinalIgnoreCase)
            .Select(g =>
                g.OrderByDescending(x => x.Catalog)
                    .ThenByDescending(x => x.Installed)
                    .First())
            .OrderByDescending(x => x.Installed)
            .ThenBy(x => x.AdultOnly)
            .ThenBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _refreshingRuntimeCatalogChoicesV37 = true;
        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            foreach (var item in list)
                combo.Items.Add(item);

            combo.SelectedItem = list.FirstOrDefault(x =>
                !x.ActivatesTextEncoder &&
                string.Equals(
                    x.FileName,
                    selectedFile,
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            combo.EndUpdate();
            _refreshingRuntimeCatalogChoicesV37 = false;
        }
    }

    private async Task ImportRuntimeModelAsync(bool imageModel)
    {
        using var ofd = new OpenFileDialog
        {
            Title = imageModel
                ? "Importer un modèle FLUX.2 compatible"
                : "Importer un modèle Wan compatible",
            Filter = "Safetensors|*.safetensors|Tous les fichiers|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        if (imageModel)
        {
            var compatibility = Flux2ModelCompatibility.Validate(
                ofd.FileName,
                Flux2ModelRole.Diffusion);

            if (compatibility.State == ModelCompatibilityState.Incompatible)
            {
                throw new InvalidOperationException(
                    "Ce checkpoint n'est pas compatible avec le workflow FLUX.2 actuel : " +
                    compatibility.Reason);
            }
        }
        else
        {
            var kind = VideoModelKindFromName(Path.GetFileName(ofd.FileName));
            if (kind == "vace")
            {
                throw new InvalidOperationException(
                    "Ce checkpoint VACE utilise un workflow de contrôle/masque différent. " +
                    "La v37 le détecte mais ne l'emploie pas encore comme modèle T2V/I2V.");
            }
        }

        var comfyRoot = PortablePreflight.GetComfyRoot(_s);
        var destination = Path.Combine(
            comfyRoot,
            "models",
            "diffusion_models",
            Path.GetFileName(ofd.FileName));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        if (!string.Equals(
                Path.GetFullPath(ofd.FileName),
                Path.GetFullPath(destination),
                StringComparison.OrdinalIgnoreCase))
        {
            var sizeGiB = new FileInfo(ofd.FileName).Length / 1024d / 1024d / 1024d;
            Log(
                "UI",
                $"Import modèle local · {Path.GetFileName(ofd.FileName)} · {sizeGiB:0.00} GiB.");

            await Task.Run(() => File.Copy(ofd.FileName, destination, overwrite: true));
        }

        if (imageModel)
            _s.FluxModel = Path.GetFileName(destination);
        else
        {
            _s.VideoModel = Path.GetFileName(destination);
            if (_cfgVideoModel is not null)
                _cfgVideoModel.Text = _s.VideoModel;
        }

        SettingsStore.Save(_s);
        RefreshRuntimeModelChoicesV36();
        RefreshFeatureAvailability();
        RefreshVideoModelStatus();
    }

    private void InitializeVideoQualityUiV36()
    {


        foreach (var preset in VideoQualityPresets)
        {
            _videoQualityCombo.Items.Add(
                new TemplateComboItem(
                    preset.Id,
                    L10n.IsEnglish(_s.Language) ? preset.En : preset.Fr));
        }

        _tabVideo.Controls.Add(_videoQualityLabel);
        _tabVideo.Controls.Add(_videoQualityCombo);

        SelectVideoQualityPresetForCurrentSettings();

        _videoQualityCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_applyingVideoQualityPreset ||
                _videoQualityCombo.SelectedItem is not TemplateComboItem item)
            {
                return;
            }

            var preset = VideoQualityPresets.FirstOrDefault(x => x.Id == item.Id);
            if (preset is null)
                return;

            _s.VideoQualityPreset = item.Id;
            if (preset.Steps <= 0)
            {
                SettingsStore.Save(_s);
                return;
            }

            _applyingVideoQualityPreset = true;
            try
            {
                _videoSteps.Value = Math.Clamp(
                    preset.Steps,
                    Decimal.ToInt32(_videoSteps.Minimum),
                    Decimal.ToInt32(_videoSteps.Maximum));
                _videoCfg.Value = 6m;
                _videoSamplingShift.Value = 8m;
                _videoSampler.SelectedItem = "uni_pc";
                _videoScheduler.SelectedItem = "simple";
                _videoNegative.Text = VideoNegativeForQualityV37(item.Id);
                SaveVideoGenerationSettings();
                UpdateVideoQualityHint();
            }
            finally
            {
                _applyingVideoQualityPreset = false;
            }
        };

        EventHandler refreshQuality = (_, _) =>
        {
            if (!_applyingVideoQualityPreset)
                SelectVideoQualityPresetForCurrentSettings();
        };

        _videoSteps.ValueChanged += refreshQuality;
        _videoCfg.ValueChanged += refreshQuality;
        _videoSamplingShift.ValueChanged += refreshQuality;
        _videoSampler.SelectedIndexChanged += refreshQuality;
        _videoScheduler.SelectedIndexChanged += refreshQuality;
    }

    private void SelectVideoQualityPresetForCurrentSettings()
    {
        if (_videoQualityCombo is null)
            return;

        var steps = Decimal.ToInt32(_videoSteps.Value);
        var referenceSampling =
            Math.Abs(Decimal.ToDouble(_videoCfg.Value) - 6.0) < 0.01 &&
            Math.Abs(Decimal.ToDouble(_videoSamplingShift.Value) - 8.0) < 0.01 &&
            string.Equals(
                _videoSampler.SelectedItem?.ToString(),
                "uni_pc",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                _videoScheduler.SelectedItem?.ToString(),
                "simple",
                StringComparison.OrdinalIgnoreCase);

        var id = referenceSampling
            ? steps switch
            {
                20 => "fast",
                30 => "standard",
                40 => "quality",
                50 => "best",
                _ => "custom"
            }
            : "custom";

        var selected = _videoQualityCombo.Items
            .OfType<TemplateComboItem>()
            .FirstOrDefault(x => x.Id == id);

        if (selected is not null &&
            !ReferenceEquals(_videoQualityCombo.SelectedItem, selected))
        {
            _videoQualityCombo.SelectedItem = selected;
        }
    }


    private void InitializeVideoReferenceUiV36()
    {

        _videoReferenceImage.Name = "_videoReferenceImage";
        _videoReferenceImage.ReadOnly = true;

        _videoReferenceBrowseButton.FlatAppearance.BorderSize = 0;

        _videoReferenceCropButton.FlatAppearance.BorderSize = 0;

        _videoExtractPromptButton.FlatAppearance.BorderSize = 0;

        _videoReferenceHint.AutoSize = false;
        _videoReferenceHint.Size = new Size(203, 30);
        _videoReferenceHint.ForeColor = AppTheme.TextDim;
        _videoReferenceHint.Font = new Font("Segoe UI", 7.5F);

        _tabVideo.Controls.AddRange(
        [
            _videoReferenceLabel,
            _videoReferenceImage,
            _videoReferenceBrowseButton,
            _videoReferenceCropButton,
            _videoExtractPromptButton,
            _videoReferenceHint
        ]);

        _videoReferenceBrowseButton.Click += (_, _) =>
        {
            using var ofd = CreateImageOpenDialog(
                L10n.Pick(
                    _s.Language,
                    "Choisir l'image de référence vidéo",
                    "Choose the video reference image"));

            if (ofd.ShowDialog(this) == DialogResult.OK)
                _videoReferenceImage.Text = ofd.FileName;
        };

        _videoReferenceCropButton.Click += (_, _) =>
            SelectVideoReferenceRegionV36();

        _videoExtractPromptButton.Click += async (_, _) =>
            await SafeUiAsync(
                "Extraction prompt vidéo",
                () => RunGpuExclusiveAsync(
                    "Vision vidéo",
                    ExtractVideoPromptFromUiAsync));
    }

    private void SelectVideoReferenceRegionV36()
    {
        var path = _videoReferenceImage.Text.Trim();

        if (!File.Exists(path))
        {
            using var ofd = CreateImageOpenDialog(
                L10n.Pick(
                    _s.Language,
                    "Choisir l'image de référence vidéo",
                    "Choose the video reference image"));

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            path = ofd.FileName;
            _videoReferenceImage.Text = path;
        }

        using var selector = new ImageRegionSelectorForm(
            path,
            _s.Language);

        if (selector.ShowDialog(this) != DialogResult.OK ||
            selector.SelectedRegion.IsEmpty)
        {
            return;
        }

        var outputDir = Path.Combine(
            PortablePaths.RuntimeDir,
            "reference-selections");
        Directory.CreateDirectory(outputDir);

        var output = Path.Combine(
            outputDir,
            "video_reference_" +
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") +
            ".png");

        using var source = new Bitmap(path);
        var region = Rectangle.Intersect(
            selector.SelectedRegion,
            new Rectangle(0, 0, source.Width, source.Height));

        if (region.Width < 8 || region.Height < 8)
            return;

        using var crop = source.Clone(
            region,
            source.PixelFormat);
        crop.Save(
            output,
            System.Drawing.Imaging.ImageFormat.Png);

        _videoReferenceImage.Text = output;
        Log(
            "Video",
            $"Zone de référence sélectionnée · {region.Width}x{region.Height} · {output}");
    }

    private async Task ExtractVideoPromptFromUiAsync()
    {
        var path = _videoReferenceImage.Text.Trim();

        if (!File.Exists(path))
        {
            using var ofd = CreateImageOpenDialog(
                L10n.Pick(
                    _s.Language,
                    "Choisir l'image de référence à analyser",
                    "Choose the reference image to analyze"));

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            path = ofd.FileName;
            _videoReferenceImage.Text = path;
        }

        _videoExtractPromptButton.Enabled = false;
        try
        {
            _videoStatus.Text = L10n.Pick(
                _s.Language,
                "Analyse de l'image avec le modèle vision…",
                "Analyzing reference image with the vision model…");

            var prompt = await ExtractPromptWithVisionAsync(
                path,
                CancellationToken.None);

            _videoPrompt.Text = prompt;
            _videoStatus.Text = L10n.Pick(
                _s.Language,
                "Prompt vidéo extrait de l'image.",
                "Video prompt extracted from image.");
        }
        finally
        {
            _videoExtractPromptButton.Enabled = !_gpuUiLocked;
        }
    }

    private void InitializeVideoPreviewV36()
    {

        _tabVideo.Controls.Add(_videoPreviewWeb);
        _videoPreviewWeb.BringToFront();

        ShowVideoPreviewPlaceholderV36(
            L10n.Pick(
                _s.Language,
                "Cliquez sur une vidéo de l'historique pour la prévisualiser.",
                "Click a video in the history to preview it."));
    }

    private async Task EnsureVideoPreviewWebReadyV36()
    {
        if (_videoPreviewReady)
            return;

        var runtime = PortablePaths.GetFixedWebView2RuntimePath()
            ?? throw new InvalidOperationException(
                "WebView2 Fixed Runtime portable absent.");

        var profile = Path.Combine(
            PortablePaths.RuntimeDir,
            "webview2-video-preview");

        var env = await CoreWebView2Environment.CreateAsync(
            runtime,
            profile);

        await _videoPreviewWeb.EnsureCoreWebView2Async(env);

        var core = _videoPreviewWeb.CoreWebView2
            ?? throw new InvalidOperationException(
                "Initialisation WebView2 vidéo incomplète.");

        var videos = PortablePaths.Resolve(_s.Videos);
        Directory.CreateDirectory(videos);

        core.SetVirtualHostNameToFolderMapping(
            "videos.local",
            videos,
            CoreWebView2HostResourceAccessKind.Allow);

        _videoPreviewReady = true;
    }

    private async Task PreviewVideoAsync(string path)
    {
        if (!File.Exists(path))
            return;

        _videoPreviewPath = path;
        _videoOutput.Text = path;
        _videoOpenButton.Enabled = true;

        try
        {
            await EnsureVideoPreviewWebReadyV36();

            var videosRoot = Path.GetFullPath(
                PortablePaths.Resolve(_s.Videos));
            var full = Path.GetFullPath(path);

            if (!full.StartsWith(
                    videosRoot.TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "La prévisualisation intégrée accepte uniquement les vidéos du dossier DreamRaster.");
            }

            var name = Uri.EscapeDataString(
                Path.GetFileName(full));

            var html =
                "<!doctype html><html><head><meta charset='utf-8'>" +
                "<style>html,body{margin:0;width:100%;height:100%;background:#121418;overflow:hidden}" +
                "video{width:100%;height:100%;object-fit:contain;background:#121418}</style></head>" +
                "<body><video controls autoplay playsinline src='https://videos.local/" +
                name +
                "'></video></body></html>";

            _videoPreviewWeb.NavigateToString(html);
            Log("Video", "Aperçu vidéo intégré : " + Path.GetFileName(full));
        }
        catch (Exception ex)
        {
            ShowVideoPreviewPlaceholderV36(
                L10n.Pick(
                    _s.Language,
                    "Prévisualisation intégrée indisponible. Double-cliquez pour ouvrir la vidéo.",
                    "Integrated preview unavailable. Double-click to open the video."));
            Log("WebView ⚠", "Aperçu vidéo : " + ex.Message);
        }
    }

    private void ShowVideoPreviewPlaceholderV36(string message)
    {
        var safe = System.Net.WebUtility.HtmlEncode(message);
        var html =
            "<!doctype html><html><head><meta charset='utf-8'>" +
            "<style>html,body{margin:0;width:100%;height:100%;background:#121418;color:#c4cad6;" +
            "font:14px 'Segoe UI',sans-serif;display:flex;align-items:center;justify-content:center}" +
            "div{max-width:80%;text-align:center;line-height:1.5}</style></head>" +
            "<body><div>▶<br>" + safe + "</div></body></html>";

        try
        {
            if (_videoPreviewWeb.CoreWebView2 is not null)
                _videoPreviewWeb.NavigateToString(html);
        }
        catch
        {
        }
    }

    private void InitializeLogTabColorsV36()
    {
        if (_logTabs is null)
            return;

        _logTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _logTabs.DrawItem += (_, e) =>
        {
            var page = _logTabs.TabPages[e.Index];
            var selected = e.Index == _logTabs.SelectedIndex;
            var back = selected
                ? AppTheme.Surface
                : AppTheme.Background;
            var fore = LogTabColorV36(e.Index);

            using var brush = new SolidBrush(back);
            e.Graphics.FillRectangle(
                brush,
                e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                page.Text,
                _logTabs.Font,
                e.Bounds,
                fore,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        };
    }

    private static Color LogTabColorV36(int index) =>
        index switch
        {
            1 => AppTheme.LogUi,
            2 => AppTheme.LogOpenCode,
            3 => AppTheme.LogOllama,
            4 => AppTheme.LogComfy,
            5 => AppTheme.LogFlux,
            6 => AppTheme.LogVideo,
            7 => AppTheme.LogInstall,
            8 => AppTheme.TextMuted,
            _ => AppTheme.Text
        };

    private void FixV36Layout()
    {
        // Image : deuxième ligne réservée au modèle, puis aperçu.
        if (_imagePreviewViewport is not null)
        {
            _imagePreviewViewport.Location = new Point(370, 82);
            _imagePreviewViewport.Size = new Size(666, 400);
        }

        // Garder les libellés Image compacts : l'ancien texte très long
        // "Force (non utilisée...)" recouvrait le libellé Seed.
        lblImg2ImgStrength.AutoSize = false;
        lblImg2ImgStrength.Location = new Point(18, 384);
        lblImg2ImgStrength.Size = new Size(100, 22);
        lblImg2ImgStrength.Text =
            L10n.Pick(
                _s.Language,
                "Force I2I",
                "I2I strength");
        _generationTemplateTips.SetToolTip(
            lblImg2ImgStrength,
            L10n.Pick(
                _s.Language,
                "Paramètre legacy conservé pour compatibilité ; le workflow FLUX.2 officiel actuel ne l'utilise pas.",
                "Legacy compatibility setting; the current official FLUX.2 workflow does not use it."));

        // Aligner les deux boutons d'action principaux de l'onglet Image.
        btnGenerate.Location = new Point(18, 545);
        btnGenerate.Size = new Size(125, 34);
        _benchmarkButton.Location = new Point(151, 545);
        _benchmarkButton.Size = new Size(197, 34);

        // Vidéo : l'aperçu occupe uniquement la zone supérieure droite.
        // Ne pas l'ancrer au bas : sinon WebView2 peut recouvrir fichier/historique
        // lorsque l'onglet reçoit sa taille définitive après construction.
        if (_videoPreviewWeb is not null)
        {
            _videoPreviewWeb.Location = new Point(448, 82);
            _videoPreviewWeb.Size = new Size(570, 374);
            _videoPreviewWeb.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left |
                AnchorStyles.Right;
        }

        // Vidéo : l'installation appartient à l'onglet Installation.
        _videoInstallButton.Visible = false;
        _videoInstallButton.Enabled = false;
        if (_videoInstallButton.Parent == _tabVideo)
            _tabVideo.Controls.Remove(_videoInstallButton);

        foreach (var label in _tabVideo.Controls.OfType<Label>())
        {
            if (label.Text.StartsWith(
                    "Modèles vidéo",
                    StringComparison.OrdinalIgnoreCase) ||
                label.Text.StartsWith(
                    "Moteur local prévu",
                    StringComparison.OrdinalIgnoreCase))
            {
                label.Visible = false;
            }

            if (label.Text == "Style" && label.Left >= 440)
            {
                label.Anchor =
                    AnchorStyles.Top | AnchorStyles.Left;
                label.Location = new Point(448, 49);
                label.Size = new Size(45, 26);
            }

            if (label.Text.StartsWith(
                    "Fichier vidéo",
                    StringComparison.OrdinalIgnoreCase))
            {
                label.Anchor =
                    AnchorStyles.Bottom | AnchorStyles.Left;
                label.Location = new Point(448, 462);
                label.Size = new Size(110, 22);
            }
        }

        _videoModelStatus.Visible = false;

        _videoStyleTemplateCombo.Anchor =
            AnchorStyles.Top | AnchorStyles.Left;
        _videoStyleTemplateCombo.Location = new Point(493, 50);
        _videoStyleTemplateCombo.Size = new Size(222, 25);

        _videoNegativeTemplateLabel.Anchor =
            AnchorStyles.Top | AnchorStyles.Left;
        _videoNegativeTemplateLabel.Location = new Point(730, 49);
        _videoNegativeTemplateLabel.Size = new Size(62, 26);

        _videoNegativeTemplateCombo.Anchor =
            AnchorStyles.Top | AnchorStyles.Left;
        _videoNegativeTemplateCombo.Location = new Point(792, 50);
        _videoNegativeTemplateCombo.Size = new Size(226, 25);

        _videoOutput.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _videoOutput.Location = new Point(448, 484);
        _videoOutput.Size = new Size(400, 28);

        _videoOpenButton.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Right;
        _videoOpenButton.Location = new Point(858, 481);
        _videoOpenButton.Size = new Size(160, 34);

        _videoHistoryLabel.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left;
        _videoHistoryLabel.Location = new Point(448, 518);

        _videoHistoryPanel.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _videoHistoryPanel.Location = new Point(448, 540);
        _videoHistoryPanel.Size = new Size(570, 88);

        LayoutVideoLeftColumnV36();
    }

    private void LayoutVideoLeftColumnV36()
    {
        _videoPrompt.Location = new Point(18, 45);
        _videoPrompt.Size = new Size(390, 88);

        _videoNegative.Location = new Point(18, 163);
        _videoNegative.Size = new Size(390, 48);

        foreach (var label in _tabVideo.Controls.OfType<Label>())
        {
            switch (label.Text)
            {
                case "Négatif / éléments à éviter":
                    label.Location = new Point(18, 138);
                    break;
                case "Largeur vidéo":
                case "Video width":
                    label.Location = new Point(18, 307);
                    label.Size = new Size(68, 23);
                    break;
                case "Hauteur vidéo":
                case "Video height":
                    label.Location = new Point(210, 307);
                    label.Size = new Size(75, 23);
                    break;
                case "Frames vidéo":
                case "Video frames":
                    label.Location = new Point(18, 342);
                    label.Size = new Size(68, 23);
                    break;
                case "FPS vidéo":
                case "Video FPS":
                    label.Location = new Point(210, 342);
                    label.Size = new Size(75, 23);
                    break;
                case "Steps vidéo":
                case "Video steps":
                    label.Location = new Point(18, 377);
                    label.Size = new Size(68, 23);
                    break;
                case "CFG vidéo":
                case "Video CFG":
                    label.Location = new Point(210, 377);
                    label.Size = new Size(75, 23);
                    break;
                case "Shift":
                    label.Location = new Point(18, 412);
                    label.Size = new Size(68, 23);
                    break;
                case "Sampler":
                    label.Location = new Point(210, 412);
                    label.Size = new Size(75, 23);
                    break;
                case "Scheduler":
                    label.Location = new Point(18, 447);
                    label.Size = new Size(68, 23);
                    break;
                case "Seed vidéo":
                case "Video seed":
                    label.Location = new Point(210, 447);
                    label.Size = new Size(75, 23);
                    break;
            }
        }

        _videoWidth.Location = new Point(90, 303);
        _videoHeight.Location = new Point(292, 303);
        _videoFrames.Location = new Point(90, 338);
        _videoFps.Location = new Point(292, 338);
        _videoSteps.Location = new Point(90, 373);
        _videoCfg.Location = new Point(292, 373);
        _videoSamplingShift.Location = new Point(90, 408);
        _videoSampler.Location = new Point(292, 408);
        _videoScheduler.Location = new Point(90, 443);
        _videoSeed.Location = new Point(292, 443);
        _videoRandomSeed.Location = new Point(379, 443);

        _videoQualityHint.Location = new Point(18, 470);
        _videoQualityHint.Size = new Size(390, 34);

        _videoGenerateButton.Location = new Point(18, 508);
        _videoCancelButton.Location = new Point(174, 508);
        _videoRefreshButton.Location = new Point(290, 508);
        _videoProgress.Location = new Point(18, 552);
        _videoStatus.Location = new Point(18, 575);
    }

    private void SetGpuWorkflowUiLocked(
        bool locked,
        string operation)
    {
        _gpuUiLocked = locked;

        void Apply()
        {
            if (locked)
            {
                _gpuUiEnabledSnapshot.Clear();

                foreach (var control in EnumerateGpuWorkspaceInteractiveControls())
                {
                    _gpuUiEnabledSnapshot[control] = control.Enabled;
                    control.Enabled = false;
                }

                Log(
                    "GPU",
                    $"Verrouillage des outils Image/Vidéo · {operation}.");
                return;
            }

            foreach (var pair in _gpuUiEnabledSnapshot)
            {
                if (!pair.Key.IsDisposed)
                    pair.Key.Enabled = pair.Value;
            }

            _gpuUiEnabledSnapshot.Clear();

            // Restaurer aussi les dépendances fonctionnelles qui ne sont pas de
            // simples états UI persistants.
            UpdateGenerationModeUi();
            UpdateMaximumQualitySharpnessUi();

            // Le snapshot GPU est pris avant la création de la première passe.
            // Recalculer ensuite les états du comparateur évite de restaurer
            // son ancien état désactivé après une génération Qualité maximale.
            if (_imageSharpnessComparisonPanel.Visible)
                SetImageSharpnessComparisonVisible(true);

            if (_videoSharpnessPreviewPanel.Visible)
                SetVideoSharpnessComparisonVisible(true);

            if (_seedInput is not null)
                _seedInput.Enabled = !_randomSeedCheck.Checked;

            if (_videoSeed is not null)
                _videoSeed.Enabled = !_videoRandomSeed.Checked;

            if (_improvePromptButton is not null)
                UpdatePromptEnhancementState();
            if (_videoImprovePromptButton is not null)
                UpdateVideoPromptEnhancementStateV37();
        }

        if (InvokeRequired && IsHandleCreated)
            Invoke(Apply);
        else
            Apply();
    }

    private IEnumerable<Control> EnumerateGpuWorkspaceInteractiveControls()
    {
        foreach (var root in new Control?[]
                 {
                     tabGenerate,
                     _tabVideo
                 })
        {
            if (root is null)
                continue;

            foreach (var control in EnumerateDescendantControls(root))
            {
                if (IsGpuWorkspaceInteractiveControl(control))
                    yield return control;
            }
        }
    }

    private static IEnumerable<Control> EnumerateDescendantControls(
        Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            // Un NumericUpDown et d'autres contrôles WinForms possèdent
            // des enfants internes (par exemple UpDownEdit). Leur état
            // doit être géré uniquement par le contrôle parent.
            if (IsGpuWorkspaceInteractiveControl(child))
                continue;

            foreach (var descendant in
                     EnumerateDescendantControls(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsGpuWorkspaceInteractiveControl(
        Control control)
    {
        // Les boutons d'annulation d'un téléchargement doivent rester
        // disponibles même si un workflow GPU démarre entre-temps.
        if (control.Name is
            "_imageCatalogDownloadCancelButton" or
            "_videoCatalogDownloadCancelButton")
        {
            return false;
        }

        return control is ButtonBase or
            TextBoxBase or
            ComboBox or
            NumericUpDown or
            ListBox or
            CheckedListBox or
            TrackBar or
            DateTimePicker;
    }

    private void ConfigureComfyWebViewEnvironmentV36()
    {
        var fixedRuntime = PortablePaths.GetFixedWebView2RuntimePath();
        if (fixedRuntime is null)
            return;

        // Important : CreationProperties doit être défini avant que le contrôle
        // devienne visible. Sinon WebView2 peut s'auto-initialiser avec le runtime
        // système, puis refuser l'environnement portable fourni plus tard.
        _comfyWeb.CreationProperties =
            new CoreWebView2CreationProperties
            {
                BrowserExecutableFolder = fixedRuntime,
                UserDataFolder = Path.Combine(
                    PortablePaths.RuntimeDir,
                    "webview2-comfy")
            };
    }

    private void InitializeComfyStatusPanelV36()
    {




        _comfyStatusRetryButton.FlatAppearance.BorderSize = 0;
        _comfyStatusMessage.Text = L10n.Pick(
            _s.Language,
            "Vérification du serveur local ComfyUI…",
            "Checking the local ComfyUI server…");
        _comfyStatusRetryButton.Text =
            L10n.Pick(_s.Language, "Réessayer", "Retry");
        _comfyStatusPortLabel.Text = $"127.0.0.1:{_s.ComfyPort}";
        _comfyStatusRetryButton.Click += async (_, _) =>
            await SafeUiAsync("ComfyUI", OpenComfyEmbeddedAsync);


        _comfyStatusCard.Controls.AddRange(
        [
            _comfyStatusTitle,
            _comfyStatusMessage,
            _comfyStatusRetryButton,
            _comfyStatusPortLabel
        ]);

        _comfyStatusPanel.Controls.Add(_comfyStatusCard);
        _comfyStatusPanel.Resize += (_, _) =>
        {
            _comfyStatusCard.Left = Math.Max(0, (_comfyStatusPanel.ClientSize.Width - _comfyStatusCard.Width) / 2);
            _comfyStatusCard.Top = Math.Max(0, (_comfyStatusPanel.ClientSize.Height - _comfyStatusCard.Height) / 2);
        };

        tabComfy.Controls.Add(_comfyStatusPanel);
        _comfyStatusPanel.BringToFront();
    }

    private void SetComfyStatusPanelV36(
        string title,
        string message,
        bool retry)
    {
        if (_comfyStatusPanel is null)
            return;

        _comfyStatusTitle.Text = title;
        _comfyStatusMessage.Text = message;
        _comfyStatusRetryButton.Visible = retry;
        _comfyWeb.Visible = false;
        _comfyStatusPanel.Visible = true;
        _comfyStatusPanel.BringToFront();
    }

    private void ShowComfyWebV36()
    {
        if (_comfyStatusPanel is not null)
            _comfyStatusPanel.Visible = false;

        _comfyWeb.Visible = true;
        _comfyWeb.BringToFront();
    }

    private void InitializeComfyDirectTabV36()
    {
        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (_tabs.SelectedTab != tabComfy ||
                _closing ||
                _comfyDirectTabBusy)
            {
                return;
            }

            _comfyDirectTabBusy = true;
            try
            {
                var port =
                    await PortablePreflight.InspectPortAsync(
                        _s.ComfyPort);

                if (!port.Open)
                {
                    SetComfyStatusPanelV36(
                        L10n.Pick(
                            _s.Language,
                            "ComfyUI n'est pas démarré",
                            "ComfyUI is not running"),
                        L10n.Pick(
                            _s.Language,
                            "Le serveur local ne répond pas. Utilisez Réessayer pour démarrer ComfyUI sans afficher une page d'erreur du navigateur.",
                            "The local server is not responding. Use Retry to start ComfyUI without showing a browser error page."),
                        retry: true);
                    return;
                }

                await EnsureComfyWebViewReadyV36();
                ShowComfyWebV36();
                _comfyWeb.CoreWebView2?.Navigate(
                    $"http://127.0.0.1:{_s.ComfyPort}/");
            }
            catch (Exception ex)
            {
                SetComfyStatusPanelV36(
                    L10n.Pick(_s.Language, "ComfyUI indisponible", "ComfyUI unavailable"),
                    ex.Message,
                    retry: true);
                Log(
                    "WebView ⚠",
                    "Page ComfyUI locale : " +
                    ex.Message);
            }
            finally
            {
                _comfyDirectTabBusy = false;
            }
        };
    }

    private bool _comfyWebHandlersInstalled;

    private async Task EnsureComfyWebViewReadyV36()
    {
        var fixedRuntime = PortablePaths.GetFixedWebView2RuntimePath();
        if (fixedRuntime is null)
        {
            throw new InvalidOperationException(
                "Le runtime WebView2 Fixed portable est absent de bin\\webview2-fixed.");
        }

        if (_comfyWeb.CoreWebView2 is null)
        {
            // CreationProperties contient déjà le runtime et le profil portables.
            // Ne pas fournir un second CoreWebView2Environment ici : si le
            // contrôle a commencé son initialisation lors de l'affichage de
            // l'onglet, WebView2 rejetterait un environnement différent.
            await _comfyWeb.EnsureCoreWebView2Async();
        }

        if (_comfyWebHandlersInstalled)
            return;

        var core = _comfyWeb.CoreWebView2
            ?? throw new InvalidOperationException(
                "Initialisation WebView2 ComfyUI incomplète.");

        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;

            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
                IsLocalUri(uri))
            {
                core.Navigate(e.Uri);
            }
            else
            {
                Log("WebView", "Ouverture externe bloquée : " + e.Uri);
            }
        };

        core.NavigationStarting += (_, e) =>
        {
            if (e.Uri.StartsWith(
                    "dreamraster://retry-comfy",
                    StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                _ = SafeUiAsync(
                    "ComfyUI",
                    OpenComfyEmbeddedAsync);
                return;
            }

            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) ||
                IsLocalUri(uri))
            {
                return;
            }

            e.Cancel = true;
            Log("WebView", "Navigation externe bloquée : " + e.Uri);
        };

        core.NavigationCompleted += (_, e) =>
        {
            if (e.IsSuccess)
                return;

            Ui(() =>
            {
                ShowComfyStatusPageV36(
                    L10n.Pick(
                        _s.Language,
                        "ComfyUI indisponible",
                        "ComfyUI unavailable"),
                    L10n.Pick(
                        _s.Language,
                        "Le serveur local ne répond pas encore. Vous pouvez réessayer sans quitter DreamRaster.",
                        "The local server is not responding yet. You can retry without leaving DreamRaster."),
                    retry: true);
            });
        };

        _comfyWebHandlersInstalled = true;
    }

    private void ShowComfyStatusPageV36(
        string title,
        string message,
        bool retry)
    {
        // Une navigation WebView2 peut échouer après que l'utilisateur a déjà
        // quitté l'onglet ComfyUI. Mettre à jour son panneau d'état ne doit
        // jamais voler la sélection courante ni provoquer une boucle d'onglets.
        SetComfyStatusPanelV36(
            title,
            message,
            retry);
    }

    // =====================================================================
    // MainForm.V37
    // =====================================================================
    private CancellationTokenSource? _catalogDownloadCts;
    private bool _catalogDownloadBusy;
    private bool? _catalogDownloadVideoSide;
    private bool _applyingMainTabStripLayoutV37;
    private bool _applyingV37SharedLayout;

    private bool _refreshingRuntimeCatalogChoicesV37;
    private bool _refreshingLoraCatalogChoicesV37;

    private sealed record RuntimeModelCatalogEntry(
        string Label,
        string FileName,
        string Url,
        string Sha256,
        bool Video,
        string Kind,
        string TargetFolder = "diffusion_models",
        bool AdultOnly = false,
        bool ActivatesTextEncoder = false,
        bool RequiresHuggingFaceAuth = false,
        string SourceUrl = "",
        string LicenseId = "",
        string LicenseNote = "",
        long FileSizeBytes = 0);

    private sealed record LoraChoice(
        string FileName,
        string Display,
        bool Installed,
        LoraCatalogEntry? Catalog)
    {
        public override string ToString() => Display;
    }

    private static readonly RuntimeModelCatalogEntry[] RuntimeModelCatalogV37 =
    [
        new(
            "FLUX.2 Klein 4B FP8 · officiel",
            "flux-2-klein-4b-fp8.safetensors",
            "https://huggingface.co/black-forest-labs/FLUX.2-klein-4b-fp8/resolve/main/flux-2-klein-4b-fp8.safetensors",
            "97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6",
            false,
            "image",
            FileSizeBytes: 4070624520L),
        new(
            "FLUX.2 Klein 4B BF16 · officiel",
            "flux-2-klein-4b.safetensors",
            "https://huggingface.co/black-forest-labs/FLUX.2-klein-4B/resolve/main/flux-2-klein-4b.safetensors",
            "",
            false,
            "image",
            FileSizeBytes: 7751105712L),
        new(
            "FLUX.2 Klein Base 4B BF16 · officiel",
            "flux-2-klein-base-4b.safetensors",
            "https://huggingface.co/black-forest-labs/FLUX.2-klein-base-4B/resolve/main/flux-2-klein-base-4b.safetensors",
            "9c5fed22b76baea749d88fc2abe3ad53245e7b21a0d353a762665eea00043b92",
            false,
            "image",
            FileSizeBytes: 7751105712L),
        new(
            "Uncensored · FLUX.2 Klein 4B · encodeur abliterated · 18+",
            "flux2-klein-4b-uncensored-text-encoder.safetensors",
            "https://huggingface.co/ponpoke/flux2-klein-4b-uncensored-text-encoder/resolve/main/flux2-klein-4b-uncensored-text-encoder/model.safetensors?download=true",
            "",
            false,
            "text-encoder",
            "text_encoders",
            true,
            true,
            true,
            "https://huggingface.co/ponpoke/flux2-klein-4b-uncensored-text-encoder",
            "other",
            "Hugging Face déclare license=other : licence non standard, consulter la page source et ses conditions.",
            8044981680L),
        new(
            "Wan 2.1 T2V 1.3B FP16 · officiel",
            "wan2.1_t2v_1.3B_fp16.safetensors",
            "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_t2v_1.3B_fp16.safetensors?download=true",
            "be531024cd9018cb5b48c40cfbb6a6191645b1c792eb8bf4f8c1c6e10f924dc5",
            true,
            "t2v",
            FileSizeBytes: 2838303560L),
        new(
            "Wan 2.1 I2V 14B FP8 · officiel",
            "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors",
            "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors?download=true",
            "0ca75338e7a47ca7cacddb7e626647e65829c497387f718ecb6ea0bae456944a",
            true,
            "i2v",
            FileSizeBytes: 16397245448L),
        new(
            "NSFW / uncensored · Wan 1.3B T2V e11 alpha · 18+",
            "wan_1.3B_e11_alpha.safetensors",
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b/resolve/main/wan_1.3B_e11_alpha.safetensors?download=true",
            "12e67995a03e03361b6680d05a17b98c650d52dea310dc0922814f11491b4e52",
            true,
            "t2v",
            "diffusion_models",
            true,
            false,
            false,
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b",
            "creativeml-openrail-m",
            "Licence déclarée dans les métadonnées Hugging Face du dépôt.",
            2838077328L),
        new(
            "NSFW · Wan 1.3B I2V · 18+ · expérimental",
            "wan13bNSFWFull_v20I2v.safetensors",
            "https://huggingface.co/sl-riveria/wan-i2v-nsfw-1.3b/resolve/main/wan13bNSFWFull_v20I2v.safetensors?download=true",
            "",
            true,
            "i2v",
            "diffusion_models",
            true,
            false,
            false,
            "https://huggingface.co/sl-riveria/wan-i2v-nsfw-1.3b",
            "apache-2.0",
            "Licence déclarée dans les métadonnées Hugging Face du dépôt.",
            3129424040L)
    ];

    private sealed record LoraCatalogEntry(
        string Label,
        string FileName,
        string Url,
        bool Video,
        double Strength = 1.0,
        bool AdultOnly = false,
        string SourceUrl = "",
        string LicenseId = "",
        string LicenseNote = "",
        long FileSizeBytes = 0);

    private static readonly LoraCatalogEntry[] LoraCatalogV37 =
    [
        new(
            "Anime · Koni style · FLUX.2 Klein 4B",
            "Flux_klein_4b_anime_Koni.safetensors",
            "https://huggingface.co/Sawata97/flux2_4b_koni_animestyle/resolve/main/Flux_klein_4b_anime_Koni.safetensors?download=true",
            false,
            0.85,
            FileSizeBytes: 184832664L),
        new(
            "Réalisme / détails · FLUX.2 Klein 4B",
            "f2k_4B_consist_20260314.safetensors",
            "https://huggingface.co/xocialize/consistence-edit-FLUX.2-klein-4B-lora/resolve/main/f2k_4B_consist_20260314.safetensors?download=true",
            false,
            0.65,
            FileSizeBytes: 385379872L),
        new(
            "Sci-fi / Old Gods · FLUX.2 Klein 4B",
            "flux2-klein-4b-lora-old-gods.safetensors",
            "https://huggingface.co/Norod78/flux2-klein-4b-lora-old-gods/resolve/main/flux2-klein-4b-lora-old-gods.safetensors?download=true",
            false,
            0.80,
            FileSizeBytes: 92426800L),
        new(
            "Adulte / nudité anime · FLUX.2 Klein 4B · 18+",
            "anime-stripper-klein4b-final.safetensors",
            "https://huggingface.co/Bakanayatsu/klein-4b-anime-clothes-stripper-nl/resolve/main/anime-stripper-klein4b-final.safetensors?download=true",
            false,
            0.80,
            true,
            "https://huggingface.co/Bakanayatsu/klein-4b-anime-clothes-stripper-nl",
            "non-specifiee",
            "Aucune licence n'est déclarée dans cardData/tags Hugging Face ; vérifier la page source avant usage.",
            739277216L),
        new(
            "NSFW général · Wan 1.3B · 18+",
            "nsfw_lora_wan_1.3b.safetensors",
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b/resolve/main/nsfw_lora_wan_1.3b.safetensors?download=true",
            true,
            0.80,
            true,
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b",
            "creativeml-openrail-m",
            "Licence déclarée dans les métadonnées Hugging Face du dépôt.",
            350065648L),
        new(
            "NSFW motion helper · Wan 1.3B · 18+",
            "nsfw_wan_1.3b_motion_helper.safetensors",
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b_motion_helper/resolve/main/nsfw_wan_1.3b_motion_helper.safetensors?download=true",
            true,
            0.70,
            true,
            "https://huggingface.co/NSFW-API/NSFW_Wan_1.3b_motion_helper",
            "non-specifiee",
            "Aucune licence n'est déclarée dans cardData/tags Hugging Face ; vérifier la page source avant usage.",
            175057880L),
        new(
            "Adulte / sex helper · Wan 1.3B · 18+",
            "sex_helper_nsfw_wan_1.3b.safetensors",
            "https://huggingface.co/NSFW-API/Sex_Helper_For_NSFW_Wan_1.3B/resolve/main/sex_helper_nsfw_wan_1.3b.safetensors?download=true",
            true,
            0.75,
            true,
            "https://huggingface.co/NSFW-API/Sex_Helper_For_NSFW_Wan_1.3B",
            "non-specifiee",
            "Aucune licence n'est déclarée dans cardData/tags Hugging Face ; vérifier la page source avant usage.",
            175086848L),
        new(
            "Adulte / nudité poitrine · Wan 1.3B · 18+",
            "RevealingBoobs.safetensors",
            "https://huggingface.co/NSFW-API/Revealing_Boobs_Wan_1.3B/resolve/main/RevealingBoobs.safetensors?download=true",
            true,
            0.75,
            true,
            "https://huggingface.co/NSFW-API/Revealing_Boobs_Wan_1.3B",
            "non-specifiee",
            "Aucune licence n'est déclarée dans cardData/tags Hugging Face ; vérifier la page source avant usage.",
            43849920L)
    ];

    private void InitializeV37Ui()
    {
        InitializeLoraUiV37();
        InitializeCatalogDownloadUiV37();
        InitializeVideoPromptEnhancementV37();
        ApplyPersistedVideoQualityV37();
        ApplyV37SharedLayout();

        tabGenerate.Resize += (_, _) => ApplyV37SharedLayout();
        _tabVideo.Resize += (_, _) => ApplyV37SharedLayout();
        tabConfiguration.Resize += (_, _) => ApplyV37SharedLayout();
        tabInstallation.Resize += (_, _) => ApplyV37SharedLayout();
        tabAbout.Resize += (_, _) => ApplyV37SharedLayout();
        tabComfy.Resize += (_, _) => ApplyEmbeddedWebZoomV37();
        tabOpenCode.Resize += (_, _) => ApplyEmbeddedWebZoomV37();
        _tabs.Resize += (_, _) => ApplyMainTabStripLayoutV37();

        ApplyEmbeddedWebZoomV37();
        ApplyMainTabStripLayoutV37();
    }


    private void InitializeCatalogDownloadUiV37()
    {


        tabGenerate.Controls.AddRange(
        [
            _imageModelDownloadButton,
            _imageLoraDownloadButton,
            _imageCatalogDownloadProgress,
            _imageCatalogDownloadStatus,
            _imageCatalogDownloadSize,
            _imageCatalogDownloadCancelButton
        ]);

        _tabVideo.Controls.AddRange(
        [
            _videoModelDownloadButton,
            _videoLoraDownloadButton,
            _videoCatalogDownloadProgress,
            _videoCatalogDownloadStatus,
            _videoCatalogDownloadSize,
            _videoCatalogDownloadCancelButton
        ]);

        _imageModelDownloadButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    "Téléchargement modèle Image",
                    "Image model download"),
                () => StartSelectedRuntimeCatalogDownloadV37Async(
                    imageModel: true));

        _videoModelDownloadButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    "Téléchargement modèle Vidéo",
                    "Video model download"),
                () => StartSelectedRuntimeCatalogDownloadV37Async(
                    imageModel: false));

        _imageLoraDownloadButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    "Téléchargement LoRA Image",
                    "Image LoRA download"),
                () => StartSelectedLoraCatalogDownloadV37Async(
                    video: false));

        _videoLoraDownloadButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(
                    _s.Language,
                    "Téléchargement LoRA Vidéo",
                    "Video LoRA download"),
                () => StartSelectedLoraCatalogDownloadV37Async(
                    video: true));

        _imageCatalogDownloadCancelButton.Click += (_, _) =>
            _catalogDownloadCts?.Cancel();
        _videoCatalogDownloadCancelButton.Click += (_, _) =>
            _catalogDownloadCts?.Cancel();

        RefreshCatalogDownloadUiV37();
    }





    private void RefreshCatalogDownloadUiV37()
    {
        if (_imageModelDownloadButton is null)
            return;

        ConfigureRuntimeDownloadButtonV37(
            _imageModelDownloadButton,
            _imageModelRuntimeCombo);
        ConfigureRuntimeDownloadButtonV37(
            _videoModelDownloadButton,
            _videoModelRuntimeCombo);
        ConfigureLoraDownloadButtonV37(
            _imageLoraDownloadButton,
            _imageLoraCombo);
        ConfigureLoraDownloadButtonV37(
            _videoLoraDownloadButton,
            _videoLoraCombo);

        RefreshCatalogSizeLabelV37(
            videoSide: false,
            _imageCatalogDownloadSize,
            _imageModelRuntimeCombo,
            _imageLoraCombo);
        RefreshCatalogSizeLabelV37(
            videoSide: true,
            _videoCatalogDownloadSize,
            _videoModelRuntimeCombo,
            _videoLoraCombo);

        _imageCatalogDownloadCancelButton.Visible =
            _catalogDownloadBusy &&
            _catalogDownloadVideoSide == false;
        _videoCatalogDownloadCancelButton.Visible =
            _catalogDownloadBusy &&
            _catalogDownloadVideoSide == true;
    }

    private void ConfigureRuntimeDownloadButtonV37(
        Button button,
        ComboBox combo)
    {
        if (_catalogDownloadBusy)
        {
            button.Visible = false;
            button.Enabled = false;
            return;
        }

        var choice = combo.SelectedItem as RuntimeModelChoice;
        var show = choice is
        {
            Catalog: true,
            Installed: false
        };

        button.Enabled = show && !_catalogDownloadBusy;
        if (show)
        {
            button.Show();
            button.BringToFront();
        }
        else
        {
            button.Hide();
        }
        button.Text = choice?.RequiresHuggingFaceAuth == true
            ? L10n.Pick(_s.Language, "HF / Importer", "HF / Import")
            : L10n.Pick(_s.Language, "Télécharger", "Download");

        if (choice is not null && show)
        {
            _generationTemplateTips.SetToolTip(
                button,
                L10n.Pick(_s.Language, "Taille : ", "Size: ") +
                FormatFileSizeV37(choice.FileSizeBytes));
        }
    }

    private void ConfigureLoraDownloadButtonV37(
        Button button,
        ComboBox combo)
    {
        if (_catalogDownloadBusy)
        {
            button.Visible = false;
            button.Enabled = false;
            return;
        }

        var choice = combo.SelectedItem as LoraChoice;
        var show = choice is
        {
            Installed: false,
            Catalog: not null
        };

        button.Enabled = show && !_catalogDownloadBusy;
        if (show)
        {
            button.Show();
            button.BringToFront();
        }
        else
        {
            button.Hide();
        }
        button.Text = L10n.Pick(
            _s.Language,
            "Télécharger",
            "Download");

        if (choice?.Catalog is not null && show)
        {
            _generationTemplateTips.SetToolTip(
                button,
                L10n.Pick(_s.Language, "Taille : ", "Size: ") +
                FormatFileSizeV37(choice.Catalog.FileSizeBytes));
        }
    }

    private void RefreshCatalogSizeLabelV37(
        bool videoSide,
        Label label,
        ComboBox modelCombo,
        ComboBox loraCombo)
    {
        var isActiveDownload =
            _catalogDownloadBusy &&
            _catalogDownloadVideoSide == videoSide;

        if (isActiveDownload)
        {
            label.Visible = true;

            var activeProgress = videoSide
                ? _videoCatalogDownloadProgress
                : _imageCatalogDownloadProgress;
            activeProgress.Visible = true;
            return;
        }

        var parts = new List<string>();

        if (modelCombo.SelectedItem is RuntimeModelChoice model &&
            model.Catalog &&
            !model.Installed)
        {
            parts.Add(
                "M " +
                FormatFileSizeV37(model.FileSizeBytes));
        }

        if (loraCombo.SelectedItem is LoraChoice lora &&
            !lora.Installed &&
            lora.Catalog is not null)
        {
            parts.Add(
                "L " +
                FormatFileSizeV37(lora.Catalog.FileSizeBytes));
        }

        label.Text = parts.Count == 0
            ? string.Empty
            : L10n.Pick(_s.Language, "Taille : ", "Size: ") +
              string.Join(" · ", parts);
        label.Visible = parts.Count > 0 || _catalogDownloadBusy;

        var progress = videoSide
            ? _videoCatalogDownloadProgress
            : _imageCatalogDownloadProgress;
        var status = videoSide
            ? _videoCatalogDownloadStatus
            : _imageCatalogDownloadStatus;

        if (!_catalogDownloadBusy)
        {
            progress.Visible = parts.Count > 0;
            progress.Value = 0;
            status.Visible = false;
            status.Text = string.Empty;
        }
    }

    private static string FormatFileSizeV37(long bytes)
    {
        if (bytes <= 0)
            return "inconnue";

        string[] units = ["o", "Ko", "Mo", "Go", "To"];
        var value = (double)bytes;
        var unit = 0;

        while (value >= 1000.0 && unit < units.Length - 1)
        {
            value /= 1000.0;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {units[unit]}"
            : $"{value:0.##} {units[unit]}";
    }

    private async Task StartSelectedRuntimeCatalogDownloadV37Async(
        bool imageModel)
    {
        var combo = imageModel
            ? _imageModelRuntimeCombo
            : _videoModelRuntimeCombo;

        if (combo.SelectedItem is not RuntimeModelChoice choice ||
            !choice.Catalog ||
            choice.Installed)
        {
            RefreshCatalogDownloadUiV37();
            return;
        }

        if (choice.AdultOnly &&
            !ConfirmAdultCatalogV37(choice.Display))
        {
            return;
        }

        if (choice.RequiresHuggingFaceAuth)
        {
            var open = MessageBox.Show(
                this,
                L10n.Pick(
                    _s.Language,
                    "Ce dépôt Hugging Face exige une authentification ou l'acceptation de conditions. " +
                    "DreamRaster ne peut pas récupérer ce fichier anonymement. " +
                    "Ouvrir la page source pour le télécharger puis l'importer avec + ?",
                    "This Hugging Face repository requires authentication or acceptance of terms. " +
                    "DreamRaster cannot retrieve this file anonymously. " +
                    "Open the source page, download it, then import it with +?"),
                "DreamRaster · Hugging Face",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (open == DialogResult.Yes)
                OpenCatalogUrlV37(choice.SourceUrl);

            return;
        }

        await RunCatalogDownloadV37Async(
            videoSide: !imageModel,
            loraDownload: false,
            choice.Display,
            choice.FileSizeBytes,
            ct => DownloadRuntimeCatalogChoiceV37Async(
                choice,
                imageModel,
                ct));
    }

    private async Task StartSelectedLoraCatalogDownloadV37Async(
        bool video)
    {
        var combo = video
            ? _videoLoraCombo
            : _imageLoraCombo;

        if (combo.SelectedItem is not LoraChoice choice ||
            choice.Installed ||
            choice.Catalog is null)
        {
            RefreshCatalogDownloadUiV37();
            return;
        }

        if (choice.Catalog.AdultOnly &&
            !ConfirmAdultCatalogV37(choice.Catalog.Label))
        {
            return;
        }

        await RunCatalogDownloadV37Async(
            videoSide: video,
            loraDownload: true,
            choice.Catalog.Label,
            choice.Catalog.FileSizeBytes,
            ct => DownloadLoraV37Async(
                video,
                choice.Catalog.FileName,
                choice.Catalog.Url,
                string.Empty,
                choice.Catalog.Strength,
                ct));
    }

    private async Task RunCatalogDownloadV37Async(
        bool videoSide,
        bool loraDownload,
        string label,
        long fileSizeBytes,
        Func<CancellationToken, Task> action)
    {
        if (_catalogDownloadBusy)
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Un autre téléchargement de catalogue est déjà en cours.",
                    "Another catalog download is already running."));
        }

        using var cts = new CancellationTokenSource();
        _catalogDownloadCts = cts;
        _catalogDownloadBusy = true;
        _catalogDownloadVideoSide = videoSide;

        var progressBar = videoSide
            ? _videoCatalogDownloadProgress
            : _imageCatalogDownloadProgress;
        var statusLabel = videoSide
            ? _videoCatalogDownloadStatus
            : _imageCatalogDownloadStatus;
        var sizeLabel = videoSide
            ? _videoCatalogDownloadSize
            : _imageCatalogDownloadSize;
        var cancelButton = videoSide
            ? _videoCatalogDownloadCancelButton
            : _imageCatalogDownloadCancelButton;

        progressBar.Visible = true;
        progressBar.Value = 0;
        statusLabel.Visible = true;
        statusLabel.Text = L10n.Pick(
            _s.Language,
            "Préparation…",
            "Preparing…");
        var sizeText =
            L10n.Pick(_s.Language, "Taille : ", "Size: ") +
            FormatFileSizeV37(fileSizeBytes);
        sizeLabel.Visible = true;
        sizeLabel.Text = sizeText;
        cancelButton.Visible = true;
        RefreshCatalogDownloadUiV37();

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    progressBar.Value = Math.Clamp(
                        value,
                        progressBar.Minimum,
                        progressBar.Maximum);
                    progressBar.Visible = true;
                    statusLabel.Text = message;
                    statusLabel.Visible = true;

                    if (fileSizeBytes > 0)
                    {
                        if (value <= 94)
                        {
                            var downloaded = (long)Math.Round(
                                fileSizeBytes *
                                (Math.Clamp(value, 0, 94) / 94.0));
                            sizeLabel.Text =
                                FormatFileSizeV37(downloaded) +
                                " / " +
                                FormatFileSizeV37(fileSizeBytes);
                        }
                        else
                        {
                            sizeLabel.Text =
                                FormatFileSizeV37(fileSizeBytes) +
                                " · SHA256";
                        }

                        sizeLabel.Visible = true;
                    }
                });

        var finalStatus = string.Empty;
        var finalProgress = 0;

        _installer.ProgressChanged += progress;
        try
        {
            await action(cts.Token);
            progressBar.Value = 100;
            finalProgress = 100;
            finalStatus = L10n.Pick(
                _s.Language,
                "Téléchargement terminé.",
                "Download complete.");
            statusLabel.Text = finalStatus;
            Log("Catalogue", "Téléchargement terminé · " + label);
        }
        catch (OperationCanceledException)
        {
            finalProgress = progressBar.Value;
            finalStatus = L10n.Pick(
                _s.Language,
                "Téléchargement annulé.",
                "Download canceled.");
            statusLabel.Text = finalStatus;
            Log("Catalogue", "Téléchargement annulé · " + label);
            throw;
        }
        catch (Exception ex)
        {
            finalProgress = progressBar.Value;
            finalStatus = L10n.Pick(
                _s.Language,
                "Échec du téléchargement.",
                "Download failed.");
            var clear = FormatCatalogDownloadErrorV37(
                label,
                ex);
            statusLabel.Text = finalStatus;
            Log("ERREUR", clear);
            throw new InvalidOperationException(clear, ex);
        }
        finally
        {
            _installer.ProgressChanged -= progress;
            _catalogDownloadBusy = false;
            _catalogDownloadCts = null;
            _catalogDownloadVideoSide = null;
            cancelButton.Visible = false;

            if (loraDownload)
                RefreshLoraChoicesV37();
            else
                RefreshRuntimeModelChoicesV36();

            RefreshCatalogDownloadUiV37();

            if (!string.IsNullOrWhiteSpace(finalStatus))
            {
                progressBar.Visible = true;
                progressBar.Value = Math.Clamp(
                    finalProgress,
                    progressBar.Minimum,
                    progressBar.Maximum);
                statusLabel.Visible = true;
                statusLabel.Text = finalStatus;
                sizeLabel.Visible = true;
                sizeLabel.Text = sizeText;
            }
        }
    }

    private string FormatCatalogDownloadErrorV37(
        string label,
        Exception ex)
    {
        var message = ex.Message ?? string.Empty;
        var reason =
            message.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("403", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                ? L10n.Pick(
                    _s.Language,
                    "Le serveur refuse l'accès. Le dépôt peut nécessiter une connexion Hugging Face ou l'acceptation de ses conditions.",
                    "The server denied access. The repository may require a Hugging Face login or acceptance of its terms.")
                : message.Contains("SHA256", StringComparison.OrdinalIgnoreCase)
                    ? L10n.Pick(
                        _s.Language,
                        "Le fichier reçu ne correspond pas au SHA256 attendu. Il a été refusé pour éviter d'utiliser un fichier corrompu ou différent.",
                        "The received file does not match the expected SHA256. It was rejected to avoid using a corrupted or different file.")
                    : message.Contains("space", StringComparison.OrdinalIgnoreCase) ||
                      message.Contains("disque", StringComparison.OrdinalIgnoreCase)
                        ? L10n.Pick(
                            _s.Language,
                            "Espace disque insuffisant ou erreur d'écriture sur le disque.",
                            "Not enough disk space or disk write error.")
                        : L10n.Pick(
                            _s.Language,
                            "Erreur réseau ou téléchargement interrompu. Vérifiez la connexion, l'URL et l'espace disque.",
                            "Network error or interrupted download. Check the connection, URL, and disk space.");

        return L10n.Pick(
            _s.Language,
            $"Impossible de télécharger « {label} ».\n\n{reason}\n\nDétail technique : {message}",
            $"Unable to download “{label}”.\n\n{reason}\n\nTechnical detail: {message}");
    }

    private void AddRuntimeModelCatalogChoicesV37(
        List<RuntimeModelChoice> choices,
        bool imageModel)
    {
        foreach (var entry in RuntimeModelCatalogV37.Where(
                     x => imageModel ? !x.Video : x.Video))
        {
            choices.Add(CreateRuntimeModelChoiceV37(entry));
        }
    }

    private RuntimeModelChoice CreateRuntimeModelChoiceV37(
        RuntimeModelCatalogEntry entry)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var path = Path.Combine(
            root,
            "models",
            entry.TargetFolder,
            entry.FileName);
        var installed = File.Exists(path);
        var marker = installed
            ? "[✓]"
            : entry.RequiresHuggingFaceAuth
                ? "[HF]"
                : "[↓]";
        var role = entry.ActivatesTextEncoder
            ? "ENCODER"
            : entry.Kind.ToUpperInvariant();

        return new RuntimeModelChoice(
            entry.FileName,
            $"{marker} [{role}] {entry.Label}",
            entry.Kind,
            entry.Url,
            entry.Sha256,
            entry.TargetFolder,
            entry.AdultOnly,
            installed,
            Catalog: true,
            entry.ActivatesTextEncoder,
            entry.RequiresHuggingFaceAuth,
            entry.SourceUrl,
            entry.LicenseId,
            entry.LicenseNote,
            entry.FileSizeBytes);
    }

    private string CatalogLicenseDisplayV37(string licenseId)
    {
        if (string.IsNullOrWhiteSpace(licenseId))
        {
            return L10n.Pick(
                _s.Language,
                "non spécifiée",
                "not specified");
        }

        return string.Equals(
                licenseId,
                "non-specifiee",
                StringComparison.OrdinalIgnoreCase)
            ? L10n.Pick(
                _s.Language,
                "non spécifiée — vérifier la source",
                "not specified — check source")
            : licenseId;
    }

    private string CatalogMetadataTextV37(
        string directUrl,
        string sourceUrl,
        string licenseId,
        string licenseNote)
    {
        var lines = new List<string>
        {
            L10n.Pick(
                _s.Language,
                "Licence : ",
                "License: ") +
            CatalogLicenseDisplayV37(licenseId)
        };

        if (!string.IsNullOrWhiteSpace(licenseNote))
            lines.Add(licenseNote);

        if (!string.IsNullOrWhiteSpace(directUrl))
        {
            lines.Add(
                L10n.Pick(
                    _s.Language,
                    "Lien direct : ",
                    "Direct link: ") +
                directUrl);
        }

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            lines.Add(
                L10n.Pick(
                    _s.Language,
                    "Source : ",
                    "Source: ") +
                sourceUrl);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void OpenCatalogUrlV37(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
    }

    private void AddCatalogReferenceMenuItemsV37(
        ToolStripMenuItem parent,
        string directUrl,
        string sourceUrl,
        string licenseId,
        string licenseNote)
    {
        var license = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Licence : ",
                "License: ") +
            CatalogLicenseDisplayV37(licenseId))
        {
            Enabled = false,
            ToolTipText = licenseNote
        };

        var copyDirect = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Copier le lien direct",
                "Copy direct link"))
        {
            Enabled = !string.IsNullOrWhiteSpace(directUrl)
        };
        copyDirect.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(directUrl))
                Clipboard.SetText(directUrl);
        };

        var openSource = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Ouvrir la page source",
                "Open source page"))
        {
            Enabled = !string.IsNullOrWhiteSpace(sourceUrl)
        };
        openSource.Click += (_, _) =>
            OpenCatalogUrlV37(sourceUrl);

        parent.DropDownItems.Add(new ToolStripSeparator());
        parent.DropDownItems.Add(license);
        parent.DropDownItems.Add(copyDirect);
        parent.DropDownItems.Add(openSource);
    }

    private void AppendRuntimeCatalogMenuV37(
        ContextMenuStrip menu,
        bool imageModel)
    {
        var adultEntries = RuntimeModelCatalogV37
            .Where(x =>
                x.AdultOnly &&
                (imageModel ? !x.Video : x.Video))
            .ToArray();

        if (adultEntries.Length == 0)
            return;

        menu.Items.Add(new ToolStripSeparator());

        var root = new ToolStripMenuItem(
            L10n.Pick(
                _s.Language,
                "Catalogue uncensored / 18+",
                "Uncensored / 18+ catalog"));

        foreach (var entry in adultEntries)
        {
            var choice = CreateRuntimeModelChoiceV37(entry);
            var item = new ToolStripMenuItem(choice.Display)
            {
                ToolTipText = CatalogMetadataTextV37(
                    entry.Url,
                    entry.SourceUrl,
                    entry.LicenseId,
                    entry.LicenseNote)
            };

            var actionText = choice.Installed
                ? L10n.Pick(_s.Language, "Activer", "Activate")
                : entry.RequiresHuggingFaceAuth
                    ? L10n.Pick(
                        _s.Language,
                        "Instructions Hugging Face / importer…",
                        "Hugging Face instructions / import…")
                    : L10n.Pick(
                        _s.Language,
                        "Télécharger et activer",
                        "Download and activate");

            var action = new ToolStripMenuItem(actionText);
            action.Click += async (_, _) =>
                await SafeUiAsync(
                    L10n.Pick(
                        _s.Language,
                        "Catalogue modèle 18+",
                        "18+ model catalog"),
                    () => HandleRuntimeModelChoiceV37Async(
                        CreateRuntimeModelChoiceV37(entry),
                        imageModel));

            item.DropDownItems.Add(action);
            AddCatalogReferenceMenuItemsV37(
                item,
                entry.Url,
                entry.SourceUrl,
                entry.LicenseId,
                entry.LicenseNote);

            root.DropDownItems.Add(item);
        }

        menu.Items.Add(root);
    }

    private void UpdateRuntimeCatalogTooltipV37(ComboBox combo)
    {
        if (combo.SelectedItem is not RuntimeModelChoice choice ||
            !choice.Catalog ||
            (!choice.AdultOnly &&
             string.IsNullOrWhiteSpace(choice.LicenseId)))
        {
            _generationTemplateTips.SetToolTip(combo, string.Empty);
            return;
        }

        _generationTemplateTips.SetToolTip(
            combo,
            CatalogMetadataTextV37(
                choice.Url,
                choice.SourceUrl,
                choice.LicenseId,
                choice.LicenseNote));
    }

    private void UpdateLoraCatalogTooltipV37(ComboBox combo)
    {
        if (combo.SelectedItem is not LoraChoice choice ||
            choice.Catalog is null)
        {
            _generationTemplateTips.SetToolTip(combo, string.Empty);
            return;
        }

        _generationTemplateTips.SetToolTip(
            combo,
            CatalogMetadataTextV37(
                choice.Catalog.Url,
                choice.Catalog.SourceUrl,
                choice.Catalog.LicenseId,
                choice.Catalog.LicenseNote));
    }

    private async Task HandleRuntimeModelChoiceV37Async(
        RuntimeModelChoice choice,
        bool imageModel)
    {
        if (choice.AdultOnly &&
            !ConfirmAdultCatalogV37(choice.Display))
        {
            RefreshRuntimeModelChoicesV36();
            return;
        }

        if (choice.Catalog &&
            !choice.Installed &&
            choice.RequiresHuggingFaceAuth)
        {
            var open = MessageBox.Show(
                this,
                L10n.Pick(
                    _s.Language,
                    "Ce modèle est visible dans le catalogue mais son dépôt Hugging Face est gated. " +
                    "Acceptez les conditions du dépôt et téléchargez le fichier avec votre compte, " +
                    "puis utilisez le bouton + pour l'importer. Ouvrir la page Hugging Face ?",
                    "This model is visible in the catalog but its Hugging Face repository is gated. " +
                    "Accept the repository terms and download the file with your account, " +
                    "then use the + button to import it. Open the Hugging Face page?"),
                "DreamRaster · Hugging Face",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (open == DialogResult.Yes)
            {
                var resolveAt = choice.Url.IndexOf(
                    "/resolve/",
                    StringComparison.OrdinalIgnoreCase);
                var pageUrl = resolveAt > 0
                    ? choice.Url[..resolveAt]
                    : choice.Url;

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pageUrl,
                        UseShellExecute = true
                    });
            }

            RefreshRuntimeModelChoicesV36();
            return;
        }

        if (choice.Catalog && !choice.Installed)
        {
            var confirmation = MessageBox.Show(
                this,
                L10n.Pick(
                    _s.Language,
                    $"« {choice.Display} » n'est pas encore installé. Le télécharger maintenant ?",
                    $"“{choice.Display}” is not installed yet. Download it now?"),
                "DreamRaster · catalogue",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                RefreshRuntimeModelChoicesV36();
                return;
            }

            await DownloadRuntimeCatalogChoiceV37Async(
                choice,
                imageModel);
            return;
        }

        if (choice.ActivatesTextEncoder)
        {
            var root = PortablePreflight.GetComfyRoot(_s);
            var path = Path.Combine(
                root,
                "models",
                choice.TargetFolder,
                choice.FileName);

            var compatibility = Flux2ModelCompatibility.Validate(
                path,
                Flux2ModelRole.TextEncoder);

            if (compatibility.State == ModelCompatibilityState.Incompatible)
            {
                throw new InvalidOperationException(
                    "Cet encodeur uncensored est présent mais n'est pas compatible " +
                    "avec le CLIPLoader FLUX.2 actuel : " +
                    compatibility.Reason);
            }

            _s.TextEncoderModel = choice.FileName;
            txtTextEncoder.Text = choice.FileName;
            SettingsStore.Save(_s);
            RefreshRuntimeModelChoicesV36();
            RefreshFeatureAvailability();
            _status.Text =
                "Encodeur FLUX.2 uncensored activé · " +
                choice.FileName;
            return;
        }

        if (imageModel)
        {
            if (string.Equals(
                    _s.FluxModel,
                    choice.FileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _s.FluxModel = choice.FileName;
        }
        else
        {
            if (string.Equals(
                    _s.VideoModel,
                    choice.FileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _s.VideoModel = choice.FileName;
            if (_cfgVideoModel is not null)
                _cfgVideoModel.Text = choice.FileName;
        }

        SettingsStore.Save(_s);
        RefreshRuntimeModelChoicesV36();
        RefreshFeatureAvailability();
        RefreshVideoModelStatus();
    }

    private async Task DownloadRuntimeCatalogChoiceV37Async(
        RuntimeModelChoice choice,
        bool imageModel,
        CancellationToken ct = default)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var folder = Path.Combine(
            root,
            "models",
            choice.TargetFolder);
        Directory.CreateDirectory(folder);

        var destination = Path.Combine(
            folder,
            choice.FileName);
        var existedBefore = File.Exists(destination);

        Action<int, string> progress =
            (value, message) =>
                Ui(() =>
                {
                    if (imageModel)
                    {
                        _genProgress.Value = Math.Clamp(
                            value,
                            _genProgress.Minimum,
                            _genProgress.Maximum);
                        _genText.Text = message;
                    }
                    else
                    {
                        _videoProgress.Value = Math.Clamp(
                            value,
                            _videoProgress.Minimum,
                            _videoProgress.Maximum);
                        _videoStatus.Text = message;
                    }
                });

        _installer.ProgressChanged += progress;
        try
        {
            await _installer.DownloadExternalModelAsync(
                choice.Url,
                destination,
                choice.Sha256,
                ct);

            if (choice.ActivatesTextEncoder)
            {
                var compatibility = Flux2ModelCompatibility.Validate(
                    destination,
                    Flux2ModelRole.TextEncoder);

                if (compatibility.State == ModelCompatibilityState.Incompatible)
                {
                    throw new InvalidOperationException(
                        "Le fichier a été téléchargé mais sa structure n'est pas " +
                        "compatible avec le CLIPLoader FLUX.2 actuel : " +
                        compatibility.Reason);
                }

                _s.TextEncoderModel = choice.FileName;
                txtTextEncoder.Text = choice.FileName;
            }
            else if (imageModel)
            {
                var compatibility = Flux2ModelCompatibility.Validate(
                    destination,
                    Flux2ModelRole.Diffusion);

                if (compatibility.State == ModelCompatibilityState.Incompatible)
                {
                    throw new InvalidOperationException(
                        "Le modèle téléchargé n'est pas compatible avec le workflow FLUX.2 : " +
                        compatibility.Reason);
                }

                _s.FluxModel = choice.FileName;
            }
            else
            {
                _s.VideoModel = choice.FileName;
                if (_cfgVideoModel is not null)
                    _cfgVideoModel.Text = choice.FileName;
            }

            SettingsStore.Save(_s);
            RefreshRuntimeModelChoicesV36();
            RefreshFeatureAvailability();
            RefreshVideoModelStatus();

            Log(
                "UI",
                "Catalogue installé · " +
                choice.FileName);
        }
        catch
        {
            TryDeleteCatalogDownloadArtifactsV37(
                destination,
                deleteDestination: !existedBefore);
            throw;
        }
        finally
        {
            _installer.ProgressChanged -= progress;
        }
    }

    private static void TryDeleteCatalogDownloadArtifactsV37(
        string destination,
        bool deleteDestination)
    {
        if (deleteDestination)
        {
            try
            {
                if (File.Exists(destination))
                    File.Delete(destination);
            }
            catch { }
        }

        try
        {
            var parts = destination + ".curlparts";
            if (Directory.Exists(parts))
                Directory.Delete(parts, recursive: true);
        }
        catch { }
    }

    private bool ConfirmAdultCatalogV37(string label)
    {
        var confirmation = MessageBox.Show(
            this,
            L10n.Pick(
                _s.Language,
                "Cette entrée est destinée à du contenu adulte/NSFW. " +
                "Elle doit être utilisée uniquement avec des sujets adultes et jamais pour sexualiser un mineur. " +
                "Continuer ?",
                "This entry is intended for adult/NSFW content. " +
                "Use adult subjects only and never sexualize a minor. Continue?"),
            "DreamRaster · 18+ · " + label,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        return confirmation == DialogResult.Yes;
    }

    private void InitializeLoraUiV37()
    {
        _imageLoraStrength.Value = Math.Clamp(
            (decimal)_s.ImageLoraStrength,
            _imageLoraStrength.Minimum,
            _imageLoraStrength.Maximum);
        _videoLoraStrength.Value = Math.Clamp(
            (decimal)_s.VideoLoraStrength,
            _videoLoraStrength.Minimum,
            _videoLoraStrength.Maximum);


        _imageModelRuntimeCombo.Location = new Point(430, 49);
        _imageModelRuntimeCombo.Size = new Size(214, 25);
        _imageImportModelButton.Location = new Point(650, 48);
        _imageImportModelButton.Size = new Size(84, 27);


        tabGenerate.Controls.AddRange(
        [
            _imageLoraLabel,
            _imageLoraCombo,
            _imageLoraStrength,
            _imageLoraAddButton
        ]);



        _tabVideo.Controls.AddRange(
        [
            _videoLoraLabel,
            _videoLoraCombo,
            _videoLoraStrength,
            _videoLoraAddButton
        ]);

        RefreshLoraChoicesV37();

        _imageLoraCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateLoraCatalogTooltipV37(_imageLoraCombo);
            RefreshCatalogDownloadUiV37();

            if (_refreshingLoraCatalogChoicesV37 ||
                _imageLoraCombo.SelectedItem is not LoraChoice choice)
            {
                return;
            }

            if (!choice.Installed && choice.Catalog is not null)
                return;

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Sélection LoRA Image", "Image LoRA selection"),
                () => HandleLoraChoiceV37Async(choice, video: false));
        };
        _imageLoraStrength.ValueChanged += (_, _) =>
        {
            _s.ImageLoraStrength = Decimal.ToDouble(_imageLoraStrength.Value);
            SettingsStore.Save(_s);
        };
        _imageLoraAddButton.Click += (_, _) =>
            ShowLoraAddMenuV37(_imageLoraAddButton, video: false);

        _videoLoraCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateLoraCatalogTooltipV37(_videoLoraCombo);
            RefreshCatalogDownloadUiV37();

            if (_refreshingLoraCatalogChoicesV37 ||
                _videoLoraCombo.SelectedItem is not LoraChoice choice)
            {
                return;
            }

            if (!choice.Installed && choice.Catalog is not null)
                return;

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Sélection LoRA Vidéo", "Video LoRA selection"),
                () => HandleLoraChoiceV37Async(choice, video: true));
        };
        _videoLoraStrength.ValueChanged += (_, _) =>
        {
            _s.VideoLoraStrength = Decimal.ToDouble(_videoLoraStrength.Value);
            SettingsStore.Save(_s);
        };
        _videoLoraAddButton.Click += (_, _) =>
            ShowLoraAddMenuV37(_videoLoraAddButton, video: true);

        UpdateLoraCatalogTooltipV37(_imageLoraCombo);
        UpdateLoraCatalogTooltipV37(_videoLoraCombo);

        // La ligne supplémentaire rend la partie droite Vidéo symétrique :
        // modèle/qualité, style/négatif, LoRA, puis aperçu.
        _videoPreviewWeb.Location = new Point(448, 113);
        _videoPreviewWeb.Size = new Size(570, 343);
    }




    private void RefreshLoraChoicesV37()
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var dir = Path.Combine(root, "models", "loras");
        Directory.CreateDirectory(dir);

        var files = Directory
            .EnumerateFiles(dir, "*.safetensors")
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        FillLoraComboV37(
            _imageLoraCombo,
            files,
            _s.ImageLora,
            video: false);
        FillLoraComboV37(
            _videoLoraCombo,
            files,
            _s.VideoLora,
            video: true);
    }

    private void FillLoraComboV37(
        ComboBox combo,
        IEnumerable<string> files,
        string selected,
        bool video)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var dir = Path.Combine(root, "models", "loras");
        var installed = files.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var choices = new List<LoraChoice>
        {
            new(
                string.Empty,
                L10n.Pick(_s.Language, "(aucun)", "(none)"),
                Installed: true,
                Catalog: null)
        };

        foreach (var entry in LoraCatalogV37.Where(x => x.Video == video))
        {
            var isInstalled = installed.Contains(entry.FileName) ||
                              File.Exists(Path.Combine(dir, entry.FileName));

            choices.Add(
                new LoraChoice(
                    entry.FileName,
                    $"{(isInstalled ? "[✓]" : "[↓]")} " +
                    entry.Label +
                    (isInstalled
                        ? string.Empty
                        : L10n.Pick(_s.Language, " · non installé", " · not installed")),
                    isInstalled,
                    entry));
        }

        var catalogNames = LoraCatalogV37
            .Where(x => x.Video == video)
            .Select(x => x.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in installed
                     .Where(x => !catalogNames.Contains(x))
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            choices.Add(
                new LoraChoice(
                    file,
                    "[✓] " + file,
                    Installed: true,
                    Catalog: null));
        }

        _refreshingLoraCatalogChoicesV37 = true;
        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            foreach (var item in choices)
                combo.Items.Add(item);

            combo.SelectedItem = choices.FirstOrDefault(x =>
                    string.Equals(
                        x.FileName,
                        selected,
                        StringComparison.OrdinalIgnoreCase))
                ?? choices[0];
        }
        finally
        {
            combo.EndUpdate();
            _refreshingLoraCatalogChoicesV37 = false;
        }
    }

    private static string SelectedLoraFileV37(ComboBox combo) =>
        combo.SelectedItem is LoraChoice choice
            ? choice.FileName
            : string.Empty;

    private async Task HandleLoraChoiceV37Async(
        LoraChoice choice,
        bool video)
    {
        if (string.IsNullOrWhiteSpace(choice.FileName))
        {
            if (video)
                _s.VideoLora = string.Empty;
            else
                _s.ImageLora = string.Empty;

            SettingsStore.Save(_s);
            return;
        }

        if (!choice.Installed && choice.Catalog is not null)
        {
            var confirmation = MessageBox.Show(
                this,
                L10n.Pick(
                    _s.Language,
                    $"« {choice.Catalog.Label} » n'est pas encore installé. Le télécharger maintenant ?",
                    $"“{choice.Catalog.Label}” is not installed yet. Download it now?"),
                "DreamRaster · LoRA",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation == DialogResult.Yes)
                await DownloadCatalogLoraV37Async(choice.Catalog);
            else
                RefreshLoraChoicesV37();

            return;
        }

        if (choice.Catalog?.AdultOnly == true &&
            !ConfirmAdultCatalogV37(choice.Catalog.Label))
        {
            RefreshLoraChoicesV37();
            return;
        }

        if (video)
        {
            _s.VideoLora = choice.FileName;
            if (choice.Catalog is not null)
            {
                _videoLoraStrength.Value = Math.Clamp(
                    (decimal)choice.Catalog.Strength,
                    _videoLoraStrength.Minimum,
                    _videoLoraStrength.Maximum);
                _s.VideoLoraStrength = choice.Catalog.Strength;
            }
        }
        else
        {
            _s.ImageLora = choice.FileName;
            if (choice.Catalog is not null)
            {
                _imageLoraStrength.Value = Math.Clamp(
                    (decimal)choice.Catalog.Strength,
                    _imageLoraStrength.Minimum,
                    _imageLoraStrength.Maximum);
                _s.ImageLoraStrength = choice.Catalog.Strength;
            }
        }

        SettingsStore.Save(_s);
        Log(
            "UI",
            $"LoRA {(video ? "Vidéo" : "Image")} sélectionné · {choice.FileName}.");
    }

    private void ShowLoraAddMenuV37(Control owner, bool video)
    {
        var menu = new ContextMenuStrip();

        var import = menu.Items.Add(
            L10n.Pick(_s.Language, "Importer un LoRA local…", "Import local LoRA…"));
        import.Click += async (_, _) => await ImportLoraV37Async(video);

        var url = menu.Items.Add(
            L10n.Pick(_s.Language, "Télécharger depuis une URL…", "Download from URL…"));
        url.Click += async (_, _) => await DownloadLoraFromDialogV37Async(video);

        menu.Items.Add(new ToolStripSeparator());
        foreach (var entry in LoraCatalogV37.Where(x => x.Video == video))
        {
            var root = PortablePreflight.GetComfyRoot(_s);
            var path = Path.Combine(
                root,
                "models",
                "loras",
                entry.FileName);
            var installed = File.Exists(path);

            if (!entry.AdultOnly)
            {
                var normalItem = menu.Items.Add(
                    (installed ? "[✓] " : "[↓] ") +
                    entry.Label);
                normalItem.ToolTipText = L10n.Pick(
                    _s.Language,
                    "Téléchargement optionnel depuis Hugging Face.",
                    "Optional download from Hugging Face.");
                normalItem.Click += async (_, _) =>
                    await DownloadCatalogLoraV37Async(entry);
                continue;
            }

            var adultItem = new ToolStripMenuItem(
                (installed ? "[✓] " : "[↓] ") +
                entry.Label +
                " · " +
                L10n.Pick(_s.Language, "licence ", "license ") +
                CatalogLicenseDisplayV37(entry.LicenseId))
            {
                ToolTipText = CatalogMetadataTextV37(
                    entry.Url,
                    entry.SourceUrl,
                    entry.LicenseId,
                    entry.LicenseNote)
            };

            var action = new ToolStripMenuItem(
                installed
                    ? L10n.Pick(_s.Language, "Activer", "Activate")
                    : L10n.Pick(
                        _s.Language,
                        "Télécharger et activer",
                        "Download and activate"));

            action.Click += async (_, _) =>
            {
                if (installed)
                {
                    await SafeUiAsync(
                        L10n.Pick(
                            _s.Language,
                            "Activation LoRA 18+",
                            "18+ LoRA activation"),
                        () => HandleLoraChoiceV37Async(
                            new LoraChoice(
                                entry.FileName,
                                entry.Label,
                                Installed: true,
                                Catalog: entry),
                            video));
                }
                else
                {
                    await SafeUiAsync(
                        L10n.Pick(
                            _s.Language,
                            "Téléchargement LoRA 18+",
                            "18+ LoRA download"),
                        () => DownloadCatalogLoraV37Async(entry));
                }
            };

            adultItem.DropDownItems.Add(action);
            AddCatalogReferenceMenuItemsV37(
                adultItem,
                entry.Url,
                entry.SourceUrl,
                entry.LicenseId,
                entry.LicenseNote);

            menu.Items.Add(adultItem);
        }

        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(owner, new Point(0, owner.Height));
    }

    private async Task ImportLoraV37Async(bool video)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Importer un LoRA ComfyUI",
            Filter = "Safetensors|*.safetensors|Tous les fichiers|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var root = PortablePreflight.GetComfyRoot(_s);
        var dir = Path.Combine(root, "models", "loras");
        Directory.CreateDirectory(dir);
        var destination = Path.Combine(dir, Path.GetFileName(dialog.FileName));

        if (!string.Equals(
                Path.GetFullPath(dialog.FileName),
                Path.GetFullPath(destination),
                StringComparison.OrdinalIgnoreCase))
        {
            await Task.Run(() =>
                File.Copy(dialog.FileName, destination, overwrite: true));
        }

        SelectDownloadedLoraV37(video, Path.GetFileName(destination), 1.0);
        Log("UI", "LoRA importé : " + Path.GetFileName(destination));
    }

    private async Task DownloadLoraFromDialogV37Async(bool video)
    {
        using var dialog = new ModelDownloadForm(
            imageModel: !video,
            _s.Language)
        {
            Text = L10n.Pick(
                _s.Language,
                "Télécharger un LoRA",
                "Download LoRA")
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await DownloadLoraV37Async(
            video,
            dialog.FileName,
            dialog.ModelUrl,
            dialog.Sha256,
            1.0);
    }

    private async Task DownloadCatalogLoraV37Async(
        LoraCatalogEntry entry,
        CancellationToken ct = default)
    {
        if (entry.AdultOnly &&
            !ConfirmAdultCatalogV37(entry.Label))
        {
            RefreshLoraChoicesV37();
            return;
        }

        await DownloadLoraV37Async(
            entry.Video,
            entry.FileName,
            entry.Url,
            string.Empty,
            entry.Strength,
            ct);
    }

    private async Task DownloadLoraV37Async(
        bool video,
        string fileName,
        string url,
        string sha256,
        double strength,
        CancellationToken ct = default)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var dir = Path.Combine(root, "models", "loras");
        Directory.CreateDirectory(dir);
        var destination = Path.Combine(dir, Path.GetFileName(fileName));
        var existedBefore = File.Exists(destination);

        try
        {
            if (!existedBefore)
            {
                _status.Text = "Téléchargement LoRA · " + fileName;
                await _installer.DownloadExternalModelAsync(
                    url,
                    destination,
                    sha256,
                    ct);
            }

            SelectDownloadedLoraV37(
                video,
                Path.GetFileName(destination),
                strength);
            Log("UI", "LoRA activé : " + Path.GetFileName(destination));
        }
        catch
        {
            TryDeleteCatalogDownloadArtifactsV37(
                destination,
                deleteDestination: !existedBefore);
            throw;
        }
    }

    private void SelectDownloadedLoraV37(
        bool video,
        string fileName,
        double strength)
    {
        RefreshLoraChoicesV37();

        var combo = video ? _videoLoraCombo : _imageLoraCombo;
        var numeric = video ? _videoLoraStrength : _imageLoraStrength;

        _refreshingLoraCatalogChoicesV37 = true;
        try
        {
            combo.SelectedItem = combo.Items
                .OfType<LoraChoice>()
                .FirstOrDefault(x => string.Equals(
                    x.FileName,
                    fileName,
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _refreshingLoraCatalogChoicesV37 = false;
        }
        numeric.Value = Math.Clamp((decimal)strength, numeric.Minimum, numeric.Maximum);

        if (video)
        {
            _s.VideoLora = fileName;
            _s.VideoLoraStrength = strength;
        }
        else
        {
            _s.ImageLora = fileName;
            _s.ImageLoraStrength = strength;
        }

        SettingsStore.Save(_s);
    }

    private void InitializeVideoPromptEnhancementV37()
    {
        _videoAutoImprovePrompt.Checked = _s.VideoAutoImprovePrompt;
        _videoImprovePromptButton.Text =
            L10n.Pick(_s.Language, "✨ Améliorer", "✨ Enhance");

        _videoImprovePromptButton.FlatAppearance.BorderSize = 0;


        _tabVideo.Controls.AddRange(
        [
            _videoImprovePromptButton,
            _videoAutoImprovePrompt
        ]);

        _videoImprovePromptButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(_s.Language, "Amélioration du prompt vidéo", "Video prompt enhancement"),
                () => RunGpuExclusiveAsync(
                    "Amélioration du prompt vidéo",
                    ImproveVideoPromptFromUiV37Async));

        _videoAutoImprovePrompt.CheckedChanged += (_, _) =>
        {
            _s.VideoAutoImprovePrompt = _videoAutoImprovePrompt.Checked;
            SettingsStore.Save(_s);
        };

        _videoPrompt.TextChanged += (_, _) => UpdateVideoPromptEnhancementStateV37();
        UpdateVideoPromptEnhancementStateV37();
    }

    private async Task ImproveVideoPromptFromUiV37Async()
    {
        var source = _videoPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("Saisissez d'abord un prompt vidéo.");

        _videoImprovePromptButton.Enabled = false;
        try
        {
            var improved = await ImprovePromptTextAsync(
                source,
                CancellationToken.None,
                modelOverride: null,
                videoPrompt: true);

            _videoPrompt.Text = improved;
            _s.LastImprovedVideoPromptHash = PromptFingerprint(improved);
            SettingsStore.Save(_s);
            Log("Video", "Prompt vidéo amélioré localement via Ollama.");
        }
        finally
        {
            UpdateVideoPromptEnhancementStateV37();
        }
    }

    private bool IsVideoPromptAlreadyImprovedV37(string text) =>
        !string.IsNullOrWhiteSpace(_s.LastImprovedVideoPromptHash) &&
        string.Equals(
            PromptFingerprint(text),
            _s.LastImprovedVideoPromptHash,
            StringComparison.OrdinalIgnoreCase);

    private void UpdateVideoPromptEnhancementStateV37()
    {
        if (_videoImprovePromptButton is null || _videoPrompt is null)
            return;

        var text = _videoPrompt.Text.Trim();
        var improved =
            !string.IsNullOrWhiteSpace(text) &&
            IsVideoPromptAlreadyImprovedV37(text);

        _videoImprovePromptButton.Enabled =
            !_gpuUiLocked &&
            !string.IsNullOrWhiteSpace(text) &&
            !improved;
        _videoImprovePromptButton.Text = improved
            ? L10n.Pick(_s.Language, "✓ Déjà amélioré", "✓ Already enhanced")
            : L10n.Pick(_s.Language, "✨ Améliorer", "✨ Enhance");
    }

    private void ApplyPersistedVideoQualityV37()
    {
        if (_videoQualityCombo is null)
            return;

        var id = string.IsNullOrWhiteSpace(_s.VideoQualityPreset)
            ? "best"
            : _s.VideoQualityPreset;

        var item = _videoQualityCombo.Items
            .OfType<TemplateComboItem>()
            .FirstOrDefault(x => x.Id == id)
            ?? _videoQualityCombo.Items
                .OfType<TemplateComboItem>()
                .FirstOrDefault(x => x.Id == "best");

        if (item is not null)
            _videoQualityCombo.SelectedItem = item;
    }


    private void ApplyMainTabStripLayoutV37()
    {
        if (_applyingMainTabStripLayoutV37 ||
            _tabs is null ||
            _tabs.TabCount <= 0)
        {
            return;
        }

        _applyingMainTabStripLayoutV37 = true;
        try
        {
            var available = Math.Max(1, _tabs.ClientSize.Width - 8);
            var compact = available < 1000;
            var width = Math.Clamp(
                available / _tabs.TabCount,
                72,
                103);
            var targetSize = new Size(width, 28);

            if (_tabs.ItemSize != targetSize)
                _tabs.ItemSize = targetSize;

            static void SetText(TabPage page, string text)
            {
                if (!string.Equals(
                        page.Text,
                        text,
                        StringComparison.Ordinal))
                {
                    page.Text = text;
                }
            }

            SetText(
                tabDashboard,
                compact
                    ? L10n.Pick(_s.Language, "Accueil", "Dashboard")
                    : L10n.T(_s.Language, "tab.dashboard"));
            SetText(tabOpenCode, "OpenCode");
            SetText(tabComfy, "ComfyUI");
            SetText(
                tabOllama,
                compact
                    ? "Ollama"
                    : L10n.T(_s.Language, "tab.ollama"));
            SetText(tabGenerate, "Image");
            SetText(
                _tabVideo,
                L10n.Pick(_s.Language, "Vidéo", "Video"));
            SetText(
                tabInstallation,
                L10n.Pick(
                    _s.Language,
                    "Installation",
                    "Installation"));
            SetText(
                tabConfiguration,
                L10n.Pick(
                    _s.Language,
                    "Configuration",
                    "Settings"));
            SetText(tabLogs, "Logs");
            SetText(
                tabAbout,
                L10n.Pick(_s.Language, "À propos", "About"));

            _tabs.Invalidate();
        }
        finally
        {
            _applyingMainTabStripLayoutV37 = false;
        }
    }

    private void ApplyEmbeddedWebZoomV37()
    {
        if (_comfyWeb is not null && tabComfy is not null)
        {
            var height = tabComfy.ClientSize.Height;
            _comfyWeb.ZoomFactor = height < 600
                ? 0.80D
                : height < 680
                    ? 0.90D
                    : 1.00D;
        }

        if (_web is not null && tabOpenCode is not null)
        {
            var height = tabOpenCode.ClientSize.Height;
            _web.ZoomFactor = height < 600
                ? 0.85D
                : height < 680
                    ? 0.92D
                    : 1.00D;
        }
    }

    private void ApplyV37SharedLayout()
    {
        if (_applyingV37SharedLayout ||
            tabGenerate is null ||
            _tabVideo is null ||
            _imagePreviewViewport is null ||
            _videoPreviewWeb is null)
        {
            return;
        }

        _applyingV37SharedLayout = true;
        try
        {
            ApplyMainTabStripLayoutV37();
            LayoutImageV37();
            LayoutVideoV37();
            LayoutConfigurationV37();
            LayoutInstallationV37();
            LayoutAboutV37();
        }
        finally
        {
            _applyingV37SharedLayout = false;
        }
    }

    private void LayoutConfigurationV37()
    {
        if (_visionModelCombo is null ||
            _cfgVideoModel is null ||
            _autoSaveConfigurationCheck is null)
        {
            return;
        }

        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var contentWidth = Math.Max(500, width - 36);
        var rootWidth = Math.Max(300, width - 168);
        var rightFieldWidth = Math.Max(220, width - 488);

        tabConfiguration.AutoScroll = false;

        lblConfigTitle.SetBounds(18, 14, 390, 36);
        var saveX = Math.Max(420, width - 390);
        btnSettingsSave.SetBounds(saveX, 14, 190, 34);
        btnOpenConfigFolder.SetBounds(saveX + 202, 14, 170, 34);

        txtConfigRoot.SetBounds(150, 59, rootWidth, 23);

        _visionModelCombo.SetBounds(470, 108, rightFieldWidth, 23);
        _fluxModelCombo.SetBounds(470, 141, rightFieldWidth, 23);
        _textEncoderCombo.SetBounds(470, 174, rightFieldWidth, 23);
        _vaeCombo.SetBounds(470, 207, rightFieldWidth, 23);

        _cfgVideoModel.SetBounds(470, 240, rightFieldWidth, 23);
        _cfgVideoTextEncoder.SetBounds(470, 273, rightFieldWidth, 23);
        _cfgVideoVae.SetBounds(470, 306, rightFieldWidth, 23);

        _cfgVideoClipVisionLabel.SetBounds(18, 273, 120, 23);
        _cfgVideoClipVision.SetBounds(150, 273, 160, 23);

        lblCfgVram.SetBounds(18, 326, 126, 23);
        numSafeVram.SetBounds(150, 326, 120, 23);
        lblCfgRam.SetBounds(18, 355, 126, 23);
        numSafeRam.SetBounds(150, 355, 120, 23);

        lblCfgConnections.SetBounds(330, 326, 154, 23);
        numDownloadConnections.SetBounds(490, 326, 82, 23);
        lblCfgBuffer.SetBounds(590, 326, 154, 23);
        numDownloadBuffer.SetBounds(
            Math.Min(750, width - 108),
            326,
            90,
            23);
        chkHardStopComfy.SetBounds(330, 355, Math.Max(300, width - 348), 24);

        lblCfgLanguage.SetBounds(18, 386, 120, 23);
        cmbLanguage.SetBounds(150, 386, 160, 23);
        chkAutoUpdates.SetBounds(330, 386, Math.Max(300, width - 348), 24);

        lblCfgGitHubRepo.SetBounds(18, 416, 120, 23);
        txtGitHubRepo.SetBounds(150, 416, rootWidth, 23);

        chkInstallVisionModel.SetBounds(18, 446, 430, 24);
        _autoSaveConfigurationCheck.SetBounds(
            470,
            446,
            Math.Max(300, width - 488),
            24);

        _configurationSaveStatus.SetBounds(
            330,
            84,
            Math.Max(300, width - 348),
            20);

        var hintY = 473;
        var hintHeight = Math.Max(44, workspace.Height - hintY - 12);
        lblConfigHint.SetBounds(
            18,
            hintY,
            contentWidth,
            hintHeight);
    }

    private void LayoutInstallationV37()
    {
        if (_installImageModelsButton is null ||
            _installVideoModelsButton is null ||
            _installVideoModelsStatus is null ||
            _installComponentsStatus is null)
        {
            return;
        }

        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var contentWidth = Math.Max(500, width - 36);

        lblInstallTitle.SetBounds(18, 14, 400, 38);
        lblInstallInfo.SetBounds(18, 54, contentWidth, 58);

        btnInstallAll.SetBounds(18, 118, 235, 34);
        btnInstallCancel.SetBounds(265, 118, 105, 34);
        _installImageModelsButton.SetBounds(382, 118, 184, 34);
        _installVideoModelsButton.SetBounds(578, 118, 184, 34);

        _installVideoModelsStatus.SetBounds(
            18,
            160,
            contentWidth,
            30);

        _installText.SetBounds(18, 198, contentWidth, 32);
        _installProgress.SetBounds(18, 238, contentWidth, 16);

        var componentsHeight = workspace.Height < 620 ? 98 : 118;
        _installComponentsStatus.SetBounds(
            18,
            264,
            contentWidth,
            componentsHeight);

        var logY = 274 + componentsHeight;
        var logHeight = Math.Max(
            110,
            workspace.Height - logY - 14);
        _installLog.SetBounds(
            18,
            logY,
            contentWidth,
            logHeight);
    }

    private void LayoutAboutV37()
    {
        var workspace = GetSharedWorkspaceSizeV37();
        var width = Math.Max(876, workspace.Width);
        var compact = width < 1000;
        var iconSize = compact ? 150 : 190;
        var textX = compact ? 215 : 267;
        var rightWidth = Math.Max(420, width - textX - 38);
        var fullWidth = Math.Max(500, width - 76);

        picAboutIcon.SetBounds(38, 44, iconSize, iconSize);

        lblAboutTitle.SetBounds(textX, 44, rightWidth, 48);
        lblAboutDescription.SetBounds(textX, 102, rightWidth, 60);
        lblAboutVersion.SetBounds(textX, 176, rightWidth, 24);
        lblAboutAuthor.SetBounds(textX, 206, rightWidth, 24);
        lblAboutRepository.SetBounds(textX, 236, rightWidth, 24);
        lblAboutLicense.SetBounds(textX, 266, rightWidth, 42);

        _aboutUpdateStatus.SetBounds(38, 310, fullWidth, 48);
        _aboutUpdateProgress.SetBounds(38, 372, fullWidth, 18);

        btnCheckUpdates.SetBounds(38, 414, 220, 36);
        btnOpenGitHub.SetBounds(270, 414, 180, 36);
    }

    private Size GetSharedWorkspaceSizeV37()
    {
        var display = _tabs.DisplayRectangle;
        var width = display.Width > 0
            ? display.Width
            : Math.Max(tabGenerate.ClientSize.Width, _tabVideo.ClientSize.Width);
        var height = display.Height > 0
            ? display.Height
            : Math.Max(tabGenerate.ClientSize.Height, _tabVideo.ClientSize.Height);

        return new Size(width, height);
    }

    private static void LayoutCatalogDownloadStatusV37(
        int rightX,
        int rightWidth,
        Label status,
        ProgressBar progress,
        Label size,
        Button cancel)
    {
        const int rowY = 112;
        const int statusWidth = 145;
        const int sizeWidth = 155;
        const int cancelWidth = 76;
        const int gap = 5;

        status.SetBounds(
            rightX,
            rowY,
            statusWidth,
            24);

        var cancelX =
            rightX + rightWidth - cancelWidth;
        var sizeX =
            cancelX - gap - sizeWidth;
        var progressX =
            rightX + statusWidth + gap;
        var progressWidth = Math.Max(
            90,
            sizeX - gap - progressX);

        progress.SetBounds(
            progressX,
            rowY + 4,
            progressWidth,
            16);
        size.SetBounds(
            sizeX,
            rowY,
            sizeWidth,
            24);
        cancel.SetBounds(
            cancelX,
            rowY,
            cancelWidth,
            24);

        status.Anchor =
            AnchorStyles.Top | AnchorStyles.Left;
        progress.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;
        size.Anchor =
            AnchorStyles.Top | AnchorStyles.Right;
        cancel.Anchor =
            AnchorStyles.Top | AnchorStyles.Right;
    }

    private void LayoutImageV37()
    {
        const int rightX = 370;
        const int previewY = 142;

        var workspace = GetSharedWorkspaceSizeV37();
        var rightWidth = Math.Max(
            500,
            workspace.Width - rightX - 20);
        var historyY = Math.Max(
            440,
            workspace.Height - 105);
        var historyLabelY = historyY - 22;
        var reservedOutputY = historyLabelY - 56;
        var previewHeight = Math.Max(
            180,
            reservedOutputY - previewY - 6);

        _imageModelRuntimeLabel.SetBounds(rightX, 17, 52, 26);
        _imageModelRuntimeCombo.SetBounds(rightX + 52, 18, 150, 25);
        _imageModelDownloadButton.SetBounds(rightX + 208, 17, 112, 27);
        _imageImportModelButton.SetBounds(rightX + 326, 17, 32, 27);
        _imageImportModelButton.Text = "+";

        _imageStyleTemplateLabel.SetBounds(rightX, 49, 45, 26);
        _imageStyleTemplateCombo.SetBounds(rightX + 45, 50, 265, 25);
        _imageNegativeTemplateLabel.SetBounds(rightX + 330, 49, 62, 26);
        _imageNegativeTemplateCombo.SetBounds(
            rightX + 392,
            50,
            Math.Max(96, rightWidth - 392),
            25);

        _imageLoraLabel.SetBounds(rightX, 80, 52, 26);
        _imageLoraCombo.SetBounds(rightX + 52, 81, 180, 25);
        _imageLoraStrength.SetBounds(rightX + 238, 81, 62, 25);
        _imageLoraDownloadButton.SetBounds(rightX + 306, 80, 116, 27);
        _imageLoraAddButton.SetBounds(rightX + 428, 80, 36, 27);

        LayoutCatalogDownloadStatusV37(
            rightX,
            rightWidth,
            _imageCatalogDownloadStatus,
            _imageCatalogDownloadProgress,
            _imageCatalogDownloadSize,
            _imageCatalogDownloadCancelButton);

        _imagePreviewViewport.SetBounds(
            rightX,
            previewY,
            rightWidth,
            previewHeight);
        _imagePreviewViewport.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        _imageHistoryLabel.SetBounds(
            rightX,
            historyLabelY,
            250,
            20);
        _imageHistoryPanel.SetBounds(
            rightX,
            historyY,
            rightWidth,
            88);

        _imageHistoryLabel.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left;
        _imageHistoryPanel.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;


        var compact = workspace.Height < 600;
        if (compact)
        {
            lblPrompt.SetBounds(18, 18, 106, 23);
            _improvePromptButton.SetBounds(130, 12, 140, 28);
            _autoImprovePrompt.SetBounds(276, 15, 72, 24);

            _promptModelLabel.SetBounds(18, 48, 88, 23);
            _promptModelCombo.SetBounds(108, 47, 240, 23);
            _prompt.SetBounds(18, 76, 330, 72);

            _negativePromptLabel.SetBounds(18, 153, 180, 20);
            _negativePrompt.SetBounds(18, 175, 330, 42);

            lblGenerationMode.SetBounds(18, 223, 58, 23);
            cmbGenerationMode.SetBounds(82, 222, 266, 23);

            lblInputImage.SetBounds(18, 253, 120, 20);
            txtInputImage.SetBounds(18, 275, 178, 23);
            btnBrowseInputImage.SetBounds(202, 274, 80, 25);
            btnClearInputImage.SetBounds(288, 274, 60, 25);

            lblImg2ImgStrength.SetBounds(18, 306, 108, 20);
            numImg2ImgStrength.SetBounds(18, 327, 100, 23);

            _seedLabel.SetBounds(132, 306, 90, 20);
            _seedInput.SetBounds(132, 327, 110, 23);
            _randomSeedCheck.SetBounds(248, 327, 50, 23);

            _imageExtractPromptButton.SetBounds(172, 357, 176, 30);

            lblCfgWidth.SetBounds(18, 393, 90, 23);
            numDefaultWidth.SetBounds(108, 393, 74, 23);
            lblCfgHeight.SetBounds(190, 393, 86, 23);
            numDefaultHeight.SetBounds(278, 393, 70, 23);

            lblCfgSteps.SetBounds(18, 420, 90, 23);
            numDefaultSteps.SetBounds(108, 420, 74, 23);
            _imageCfgLabel.SetBounds(190, 420, 70, 23);
            _imageCfg.SetBounds(278, 420, 70, 23);
        }

        // Les actions Image suivent le bas de la zone commune, y compris
        // à la taille minimale. Le benchmark restait auparavant à Y=545
        // et disparaissait en mode compact.
        var imageActionY = Math.Max(445, workspace.Height - 100);
        btnGenerate.SetBounds(18, imageActionY, 125, 34);
        _benchmarkButton.SetBounds(151, imageActionY, 197, 34);
        _genText.SetBounds(18, imageActionY + 38, 330, 30);
        _genProgress.SetBounds(18, imageActionY + 72, 330, 18);
        _benchmarkButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        UpdateImagePreviewLayout();
    }

    private void LayoutVideoV37()
    {
        const int rightX = 370;
        const int previewY = 142;

        var workspace = GetSharedWorkspaceSizeV37();
        var rightWidth = Math.Max(
            500,
            workspace.Width - rightX - 20);
        var historyY = Math.Max(
            440,
            workspace.Height - 105);
        var historyLabelY = historyY - 22;
        var outputLabelY = historyLabelY - 56;
        var previewHeight = Math.Max(
            180,
            outputLabelY - previewY - 6);

        // Colonne gauche : même largeur utile que l'onglet Image.
        // Sous 600 px de hauteur utile, on compacte verticalement sans
        // supprimer de fonction : tout reste visible à la taille minimale.
        var compact = workspace.Height < 600;

        _videoPromptLabel.SetBounds(18, 18, 106, 23);
        _videoPrompt.SetBounds(
            18,
            45,
            330,
            compact ? 72 : 88);
        _videoImprovePromptButton.SetBounds(130, 12, 140, 28);
        _videoAutoImprovePrompt.SetBounds(276, 15, 72, 24);

        var negativeLabelY = compact ? 122 : 138;
        var negativeY = compact ? 145 : 163;
        _videoNegativeLabel.SetBounds(18, negativeLabelY, 330, 23);
        _videoNegative.SetBounds(
            18,
            negativeY,
            330,
            compact ? 42 : 48);

        var referenceLabelY = compact ? 194 : 218;
        var referenceY = compact ? 217 : 241;
        var extractY = compact ? 246 : 270;
        _videoReferenceLabel.SetBounds(18, referenceLabelY, 180, 23);
        _videoReferenceImage.SetBounds(18, referenceY, 190, 23);
        _videoReferenceBrowseButton.SetBounds(
            214,
            referenceY - 1,
            58,
            25);
        _videoReferenceCropButton.SetBounds(
            278,
            referenceY - 1,
            70,
            25);
        _videoExtractPromptButton.SetBounds(
            18,
            extractY,
            150,
            29);
        _videoReferenceHint.SetBounds(
            174,
            extractY - 1,
            174,
            30);

        var rowStart = compact ? 276 : 303;
        var rowStep = compact ? 28 : 35;
        var row0 = rowStart;
        var row1 = rowStart + rowStep;
        var row2 = rowStart + (rowStep * 2);
        var row3 = rowStart + (rowStep * 3);
        var row4 = rowStart + (rowStep * 4);

        foreach (var label in _tabVideo.Controls.OfType<Label>())
        {
            switch (label.Text)
            {
                case "Largeur":
                case "Largeur vidéo":
                case "Video width":
                case "Width":
                    label.SetBounds(18, row0 + 4, 64, 23);
                    break;
                case "Hauteur":
                case "Hauteur vidéo":
                case "Video height":
                case "Height":
                    label.SetBounds(174, row0 + 4, 70, 23);
                    break;
                case "Frames":
                case "Frames vidéo":
                case "Video frames":
                    label.SetBounds(142, row1 + 4, 48, 23);
                    break;
                case "FPS":
                case "FPS vidéo":
                case "Video FPS":
                    label.SetBounds(248, row1 + 4, 30, 23);
                    break;
                case "Steps":
                case "Steps vidéo":
                case "Video steps":
                    label.SetBounds(18, row2 + 4, 64, 23);
                    break;
                case "CFG vidéo":
                case "Video CFG":
                    label.SetBounds(174, row2 + 4, 70, 23);
                    break;
                case "Shift":
                    label.SetBounds(18, row3 + 4, 64, 23);
                    break;
                case "Sampler":
                    label.SetBounds(174, row3 + 4, 70, 23);
                    break;
                case "Scheduler":
                    label.SetBounds(18, row4 + 4, 64, 23);
                    break;
                case "Seed vidéo":
                case "Video seed":
                    label.SetBounds(174, row4 + 4, 70, 23);
                    break;
            }
        }

        _videoWidth.SetBounds(86, row0, 78, 23);
        _videoHeight.SetBounds(246, row0, 102, 23);

        _videoDurationLabel.SetBounds(18, row1 + 4, 64, 23);
        _videoDurationSeconds.SetBounds(84, row1, 54, 23);
        _videoFrames.SetBounds(192, row1, 52, 23);
        _videoFps.SetBounds(280, row1, 68, 23);

        _videoSteps.SetBounds(86, row2, 78, 23);
        _videoCfg.SetBounds(246, row2, 102, 23);
        _videoSamplingShift.SetBounds(86, row3, 78, 23);
        _videoSampler.SetBounds(246, row3, 102, 23);
        _videoScheduler.SetBounds(86, row4, 78, 23);
        _videoSeed.SetBounds(246, row4, 68, 23);
        _videoRandomSeed.SetBounds(318, row4, 30, 23);

        // Même ancrage vertical que les actions Image :
        // boutons, texte d'état puis barre de progression.
        var videoActionY = Math.Max(445, workspace.Height - 100);
        _videoQualityHint.SetBounds(
            18,
            compact ? videoActionY - 30 : videoActionY - 38,
            330,
            compact ? 24 : 34);
        _videoGenerateButton.SetBounds(18, videoActionY, 125, 34);
        _videoCancelButton.SetBounds(151, videoActionY, 90, 34);
        _videoRefreshButton.SetBounds(249, videoActionY, 99, 34);
        _videoStatus.SetBounds(18, videoActionY + 38, 330, 30);
        _videoProgress.SetBounds(18, videoActionY + 72, 330, 18);

        // Partie droite : trois rangées identiques à l'onglet Image.
        _videoModelRuntimeLabel.SetBounds(rightX, 17, 52, 26);
        _videoModelRuntimeCombo.SetBounds(rightX + 52, 18, 150, 25);
        _videoModelDownloadButton.SetBounds(rightX + 208, 17, 112, 27);
        _videoImportModelButton.SetBounds(rightX + 326, 17, 32, 27);
        _videoQualityLabel.SetBounds(rightX + 360, 17, 46, 26);
        _videoQualityCombo.SetBounds(
            rightX + 406,
            18,
            Math.Max(94, rightWidth - 406),
            25);

        _videoStyleTemplateLabel.SetBounds(rightX, 49, 45, 26);
        _videoStyleTemplateCombo.SetBounds(rightX + 45, 50, 265, 25);
        _videoNegativeTemplateLabel.SetBounds(rightX + 330, 49, 62, 26);
        _videoNegativeTemplateCombo.SetBounds(
            rightX + 392,
            50,
            Math.Max(96, rightWidth - 392),
            25);

        _videoLoraLabel.SetBounds(rightX, 80, 52, 26);
        _videoLoraCombo.SetBounds(rightX + 52, 81, 180, 25);
        _videoLoraStrength.SetBounds(rightX + 238, 81, 62, 25);
        _videoLoraDownloadButton.SetBounds(rightX + 306, 80, 116, 27);
        _videoLoraAddButton.SetBounds(rightX + 428, 80, 36, 27);

        LayoutCatalogDownloadStatusV37(
            rightX,
            rightWidth,
            _videoCatalogDownloadStatus,
            _videoCatalogDownloadProgress,
            _videoCatalogDownloadSize,
            _videoCatalogDownloadCancelButton);

        _videoPreviewWeb.SetBounds(
            rightX,
            previewY,
            rightWidth,
            previewHeight);
        _videoPreviewWeb.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        _videoOutputLabel.SetBounds(
            rightX,
            outputLabelY,
            110,
            22);
        _videoOutput.SetBounds(
            rightX,
            outputLabelY + 22,
            Math.Max(250, rightWidth - 170),
            28);
        _videoOpenButton.SetBounds(
            rightX + rightWidth - 160,
            outputLabelY + 19,
            160,
            34);

        _videoHistoryLabel.SetBounds(
            rightX,
            historyLabelY,
            220,
            20);
        _videoHistoryPanel.SetBounds(
            rightX,
            historyY,
            rightWidth,
            88);

        _videoOutputLabel.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left;
        _videoOutput.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;
        _videoOpenButton.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Right;
        _videoHistoryLabel.Anchor =
            AnchorStyles.Bottom | AnchorStyles.Left;
        _videoHistoryPanel.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;
    }

    private static string VideoNegativeForQualityV37(string id) =>
        id switch
        {
            "fast" =>
                "low quality, blurry, out of focus, flicker, jitter, camera shake, warped anatomy, deformed motion, text, subtitles, watermark, logo, artifacts",
            "standard" =>
                "low quality, blurry, out of focus, flicker, jitter, temporal inconsistency, camera shake, warped anatomy, deformed motion, duplicate limbs, text, subtitles, watermark, logo, compression artifacts",
            "quality" =>
                "low quality, blur, flicker, jitter, temporal inconsistency, unstable identity, warped anatomy, deformed motion, duplicate objects, ghosting, frame tearing, text, subtitles, watermark, logo, compression artifacts",
            "best" =>
                "low quality, blur, soft focus, flicker, jitter, temporal inconsistency, unstable identity, inconsistent clothing, warped anatomy, deformed motion, duplicate limbs, duplicate objects, ghosting, frame tearing, abrupt camera jumps, text, subtitles, watermark, logo, compression artifacts, banding",
            _ =>
                "low quality, blurry, flicker, jitter, warped anatomy, text, watermark"
        };
}
