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
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;

namespace OpenCodeLocalAI;

public partial class MainForm : Form
{
    private readonly AppSettings _s;
    private readonly ManagedProcess _openCode;
    private readonly ManagedProcess _ollama;
    private readonly ManagedProcess _comfy;
    private readonly PortableInstaller _installer;
    private readonly LocalProxyServer _proxy;
    private readonly GenerationApiServer _api;
    private readonly Flux2Generator _generator;
    private readonly GitHubUpdater _updater;

    private CancellationTokenSource? _installCts;
    private Task? _installTask;
    private readonly SemaphoreSlim _logFileGate = new(1, 1);
    private readonly ConcurrentQueue<(string Source, string Message)> _pendingLogs = new();
    private readonly System.Windows.Forms.Timer _logFlushTimer = new();
    private int _logFlushBusy;
    private bool _closing;

    public MainForm()
    {
        InitializeComponent();

        AppTheme.ApplyDark(this);

        _logFlushTimer.Interval = 100;
        _logFlushTimer.Tick += (_, _) => FlushPendingLogs();
        _logFlushTimer.Start();

        PortablePaths.EnsureLayout();
        _s = SettingsStore.Load();
        _updater = new GitHubUpdater(_s);

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
            (prompt, width, height, ct) => _generator.GenerateAsync(prompt, width, height, ct),
            Log);

        Log("UI", $"{BrandInfo.ProductName} v{Application.ProductVersion}");
        Log("UI", "Racine portable : " + PortablePaths.Root);
        Log("UI", "Configuration : " + Path.Combine(PortablePaths.ConfigDir, "settings.json"));
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        await SafeUiAsync(
            L10n.Pick(_s.Language, "Vérification portable", "Portable check"),
            StartupPreflightAsync);

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
        => await SafeUiAsync("Génération FLUX.2", GenerateFromUiAsync);

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
    }

    private void btnSettingsSave_Click(object? sender, EventArgs e)
    {
        try
        {
            SaveSettingsFromUi();
            SettingsStore.Save(_s);

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
    }

    private void SaveSettingsFromUi()
    {
        _s.OpenCodePort = Decimal.ToInt32(numOpenCodePort.Value);
        _s.OllamaPort = Decimal.ToInt32(numOllamaPort.Value);
        _s.ComfyPort = Decimal.ToInt32(numComfyPort.Value);
        _s.ImageProxyPort = Decimal.ToInt32(numProxyPort.Value);
        _s.GenerationApiPort = Decimal.ToInt32(numApiPort.Value);

        _s.VisionModel = txtVisionModel.Text.Trim();
        _s.FluxModel = txtFluxModel.Text.Trim();
        _s.TextEncoderModel = txtTextEncoder.Text.Trim();
        _s.VaeModel = txtVae.Text.Trim();

        _s.DefaultWidth = Decimal.ToInt32(numDefaultWidth.Value);
        _s.DefaultHeight = Decimal.ToInt32(numDefaultHeight.Value);
        _s.DefaultSteps = Decimal.ToInt32(numDefaultSteps.Value);
        _s.SafeVramMiB = Decimal.ToInt32(numSafeVram.Value);
        _s.SafeFreeRamMiB = Decimal.ToInt32(numSafeRam.Value);
        _s.DownloadConnections = Decimal.ToInt32(numDownloadConnections.Value);
        _s.DownloadBufferMiB = Decimal.ToInt32(numDownloadBuffer.Value);
        _s.HardStopComfyAfterGeneration = chkHardStopComfy.Checked;

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

        _tabs.Invalidate();
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
        lblInputImage.Enabled = isImgToImg;
        // Le workflow officiel FLUX.2 Klein Image Edit n'expose pas de
        // paramètre denoise/strength. Le champ reste visible pour compatibilité
        // avec les anciennes configurations, mais il n'est plus modifiable.
        numImg2ImgStrength.Enabled = false;
        lblImg2ImgStrength.Enabled = isImgToImg;
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
                {
                    Icon = (Icon)icon.Clone();
                    picAboutIcon.Image = icon.ToBitmap();
                }
            }
        }
        catch (Exception ex)
        {
            Log("UI", "Icône About : " + ex.Message);
        }

        _aboutUpdateProgress.Value = 0;
        _aboutUpdateStatus.Text = L10n.T(_s.Language, "about.update_ready");
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
        }
    }

    private async Task StartCoreAsync()
    {
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
        await StartOpenCodeAsync();
        await _proxy.StartAsync();
        await _api.StartAsync();
        await OpenEmbeddedAsync();

        _status.Text = "OpenCode + Ollama portables démarrés.";
    }

    private async Task StartOllamaAsync()
    {
        if (_ollama.Running) return;

        await PortablePreflight.RejectOccupiedExternalPortAsync(_s.OllamaPort, "Ollama");

        var env = PortablePreflight.PortableEnvironment("ollama");
        env["OLLAMA_MODELS"] = PortablePaths.Resolve(_s.OllamaModels);
        env["OLLAMA_HOST"] = $"127.0.0.1:{_s.OllamaPort}";
        env["OLLAMA_NO_CLOUD"] = "true";
        env["OLLAMA_NOHISTORY"] = "true";

        var exe = PortablePaths.Resolve(_s.OllamaExe);

        await _ollama.StartAsync(
            _s.OllamaExe,
            "serve",
            Path.GetDirectoryName(exe)!,
            env);
    }

    private async Task StartOpenCodeAsync()
    {
        if (_openCode.Running) return;

        await PortablePreflight.RejectOccupiedExternalPortAsync(_s.OpenCodePort, "OpenCode");

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

        await WaitForPortAsync(_s.OpenCodePort, TimeSpan.FromSeconds(20));
    }

    private async Task StartComfyAsync()
    {
        if (_comfy.Running) return;

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

        await PortablePreflight.RejectOccupiedExternalPortAsync(_s.ComfyPort, "ComfyUI");

        var env = PortablePreflight.PortableEnvironment("comfyui");
        env["PYTHONNOUSERSITE"] = "1";
        env["HF_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "huggingface");
        env["TORCH_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "torch");
        env["XDG_CACHE_HOME"] = Path.Combine(PortablePaths.RuntimeDir, "comfyui", "cache");

        var py = PortablePaths.Resolve(_s.ComfyPython);
        var main = PortablePaths.Resolve(_s.ComfyMain);
        var relPy = Path.GetRelativePath(PortablePaths.Root, py);

        await _comfy.StartAsync(
            relPy,
            $"\"{main}\" --listen 127.0.0.1 --port {_s.ComfyPort} --disable-auto-launch --disable-pinned-memory",
            Path.GetDirectoryName(main)!,
            env);

        await WaitForPortAsync(_s.ComfyPort, TimeSpan.FromSeconds(60));
    }

    private async Task OpenEmbeddedAsync()
    {
        if (!_openCode.Running)
            await StartOpenCodeAsync();

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

        Log("WebView2", "Fixed Runtime OpenCode : " + fixedRuntime);

        if (_web.CoreWebView2 is null)
        {
            var profile = Path.Combine(PortablePaths.RuntimeDir, "webview2-opencode");
            var webEnv = await CoreWebView2Environment.CreateAsync(fixedRuntime, profile);
            await _web.EnsureCoreWebView2Async(webEnv);

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

        await _proxy.StartAsync();

        var openCodeCore =
            _web.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 OpenCode n'est pas initialisé.");

        openCodeCore.Navigate($"http://127.0.0.1:{_s.ImageProxyPort}/");
        _tabs.SelectedTab = tabOpenCode;
    }

    private async Task OpenComfyEmbeddedAsync()
    {
        if (!_comfy.Running)
            await StartComfyAsync();

        if (!_comfy.Running)
            return;

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

        Log("WebView2", "Fixed Runtime ComfyUI : " + fixedRuntime);

        if (_comfyWeb.CoreWebView2 is null)
        {
            var profile = Path.Combine(
                PortablePaths.RuntimeDir,
                "webview2-comfy");

            var webEnv = await CoreWebView2Environment.CreateAsync(
                fixedRuntime,
                profile);

            await _comfyWeb.EnsureCoreWebView2Async(webEnv);

            var core =
                _comfyWeb.CoreWebView2
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
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) ||
                    IsLocalUri(uri))
                    return;

                e.Cancel = true;
                Log("WebView", "Navigation externe bloquée : " + e.Uri);
            };
        }

        var comfyCore =
            _comfyWeb.CoreWebView2
            ?? throw new InvalidOperationException(
                "WebView2 ComfyUI n'est pas initialisé.");

        comfyCore.Navigate(
            $"http://127.0.0.1:{_s.ComfyPort}/");

        _tabs.SelectedTab = tabComfy;
    }

    private static bool IsLocalUri(Uri uri)
        => uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
           || uri.Host.Equals("images.local", StringComparison.OrdinalIgnoreCase);

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
        // FR : Aucun appel Ollama si le modèle vision optionnel est désactivé.
        // EN: Do not call Ollama when the optional vision model is disabled.
        if (!_s.InstallVisionModel)
            return;

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var body = JsonSerializer.Serialize(new
            {
                model = _s.VisionModel,
                prompt = "",
                keep_alive = 0,
                stream = false
            });

            using var _ = await http.PostAsync(
                $"http://127.0.0.1:{_s.OllamaPort}/api/generate",
                new StringContent(
                    body,
                    System.Text.Encoding.UTF8,
                    "application/json"));
        }
        catch (Exception ex)
        {
            Log("Ollama", "Déchargement vision : " + ex.Message);
        }
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

        var result = await _generator.GenerateAsync(
            text,
            _s.DefaultWidth,
            _s.DefaultHeight,
            isImgToImg ? inputImagePath : null,
            Decimal.ToDouble(numImg2ImgStrength.Value),
            CancellationToken.None);

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
            ShowPreviewImage(result.Path);
    }

    private async Task StopAllAsync()
    {
        await _api.StopAsync();
        await _proxy.StopAsync();
        await _comfy.StopAsync();
        await _openCode.StopAsync();
        await _ollama.StopAsync();

        _status.Text = "Tous les services portables sont arrêtés.";
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

        var selected = e.Index == _tabs.SelectedIndex;
        var bounds = e.Bounds;
        var back = selected ? AppTheme.Primary : AppTheme.SurfaceAlt;
        var fore = selected ? Color.White : AppTheme.TextMuted;

        using var backBrush = new SolidBrush(back);
        using var textBrush = new SolidBrush(fore);

        e.Graphics.FillRectangle(backBrush, bounds);

        var text = _tabs.TabPages[e.Index].Text;
        var font = selected
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

    private static async Task WaitForPortAsync(int port, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < until)
        {
            var p = await PortablePreflight.InspectPortAsync(port);
            if (p.Open) return;

            await Task.Delay(250);
        }

        throw new TimeoutException($"Le port {port} ne s'est pas ouvert.");
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closing) return;

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
}
