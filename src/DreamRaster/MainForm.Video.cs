/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Onglet de génération vidéo locale Wan 2.1.
EN: Local Wan 2.1 video-generation tab.
*/

using System.Diagnostics;

namespace OpenCodeLocalAI;

public partial class MainForm
{
    private TabPage _tabVideo = null!;
    private TextBox _videoPrompt = null!;
    private TextBox _videoNegative = null!;
    private NumericUpDown _videoWidth = null!;
    private NumericUpDown _videoHeight = null!;
    private NumericUpDown _videoFrames = null!;
    private NumericUpDown _videoFps = null!;
    private NumericUpDown _videoSteps = null!;
    private Label _videoModelStatus = null!;
    private Label _videoStatus = null!;
    private TextBox _videoOutput = null!;
    private ProgressBar _videoProgress = null!;
    private Button _videoGenerateButton = null!;
    private Button _videoCancelButton = null!;
    private Button _videoRefreshButton = null!;
    private Button _videoOpenButton = null!;
    private Button _videoInstallButton = null!;
    private VideoGenerator _videoGenerator = null!;
    private CancellationTokenSource? _videoCts;

    private void InitializeVideoUi()
    {
        _videoGenerator = new VideoGenerator(
            _s,
            StartComfyAsync,
            Log);

        _videoGenerator.ProgressChanged += (value, text) =>
            Ui(() =>
            {
                _videoProgress.Value = Math.Clamp(value, 0, 100);
                _videoStatus.Text = text;
            });
        _tabVideo = new TabPage
        {
            Name = "tabVideo",
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.Text,
            Padding = new Padding(0)
        };

        var insertionIndex =
            Math.Max(0, _tabs.TabPages.IndexOf(tabInstallation));

        _tabs.TabPages.Insert(
            insertionIndex,
            _tabVideo);

        // FR : 10 onglets doivent rester visibles sans flèches de défilement.
        // EN: Keep all 10 main tabs visible without scroll arrows.
        _tabs.ItemSize = new Size(103, 28);

        var promptLabel = VideoLabel(
            "Prompt vidéo",
            18,
            18,
            390);

        _videoPrompt = VideoTextBox(
            18,
            45,
            390,
            145,
            multiline: true);

        var negativeLabel = VideoLabel(
            "Négatif / éléments à éviter",
            18,
            205,
            390);

        _videoNegative = VideoTextBox(
            18,
            232,
            390,
            76,
            multiline: true);

        _videoNegative.Text =
            "low quality, blurry, out of focus, flicker, jitter, " +
            "camera shake, warped anatomy, deformed motion, " +
            "text, subtitles, watermark, logo, artifacts";

        var widthLabel = VideoLabel(
            "Largeur",
            18,
            330,
            85);
        _videoWidth = VideoNumber(
            105,
            326,
            95,
            256,
            1280,
            16,
            _s.VideoWidth);

        var heightLabel = VideoLabel(
            "Hauteur",
            220,
            330,
            85);
        _videoHeight = VideoNumber(
            307,
            326,
            101,
            256,
            1280,
            16,
            _s.VideoHeight);

        var framesLabel = VideoLabel(
            "Frames",
            18,
            370,
            85);
        _videoFrames = VideoNumber(
            105,
            366,
            95,
            1,
            161,
            4,
            _s.VideoFrames);

        var fpsLabel = VideoLabel(
            "FPS",
            220,
            370,
            85);
        _videoFps = VideoNumber(
            307,
            366,
            101,
            1,
            60,
            1,
            _s.VideoFps);

        var stepsLabel = VideoLabel(
            "Steps",
            18,
            410,
            85);
        _videoSteps = VideoNumber(
            105,
            406,
            95,
            1,
            100,
            1,
            _s.VideoSteps);

        _videoGenerateButton = new Button
        {
            Location = new Point(18, 455),
            Size = new Size(145, 34),
            BackColor = AppTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Générer la vidéo"
        };

        _videoCancelButton = new Button
        {
            Location = new Point(174, 455),
            Size = new Size(105, 34),
            BackColor = AppTheme.Danger,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Annuler",
            Enabled = false
        };
        _videoRefreshButton = new Button
        {
            Location = new Point(290, 455),
            Size = new Size(118, 34),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Actualiser"
        };

        _videoProgress = new ProgressBar
        {
            Location = new Point(18, 512),
            Size = new Size(390, 18),
            Minimum = 0,
            Maximum = 100
        };

        _videoStatus = VideoLabel(
            "Prêt.",
            18,
            540,
            390);
        var modelTitle = VideoLabel(
            "Modèles vidéo Wan 2.1",
            448,
            18,
            570);
        modelTitle.Font =
            new Font(
                "Segoe UI Semibold",
                10F,
                FontStyle.Bold);

        _videoModelStatus = VideoLabel(
            string.Empty,
            448,
            52,
            570);
        _videoModelStatus.AutoSize = false;
        _videoModelStatus.Size = new Size(570, 210);

        var outputLabel = VideoLabel(
            "Fichier vidéo",
            448,
            285,
            570);

        _videoOutput = VideoTextBox(
            448,
            312,
            570,
            28,
            multiline: false);
        _videoOutput.ReadOnly = true;

        _videoOpenButton = new Button
        {
            Location = new Point(448, 354),
            Size = new Size(165, 34),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Ouvrir la vidéo",
            Enabled = false
        };

        _videoInstallButton = new Button
        {
            Location = new Point(625, 354),
            Size = new Size(230, 34),
            BackColor = AppTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Text = "Installer modèles vidéo"
        };

        var note = VideoLabel(
            "Moteur local prévu : Wan 2.1 T2V 1.3B via ComfyUI. " +
            "Aucun téléchargement de plusieurs Go n'est lancé automatiquement.",
            448,
            410,
            570);
        note.AutoSize = false;
        note.Size = new Size(570, 70);
        _tabVideo.Controls.AddRange(
        [
            promptLabel,
            _videoPrompt,
            negativeLabel,
            _videoNegative,
            widthLabel,
            _videoWidth,
            heightLabel,
            _videoHeight,
            framesLabel,
            _videoFrames,
            fpsLabel,
            _videoFps,
            stepsLabel,
            _videoSteps,
            _videoGenerateButton,
            _videoCancelButton,
            _videoRefreshButton,
            _videoProgress,
            _videoStatus,
            modelTitle,
            _videoModelStatus,
            outputLabel,
            _videoOutput,
            _videoOpenButton,
            _videoInstallButton,
            note
        ]);

        _videoGenerateButton.Click += async (_, _) =>
            await SafeUiAsync(
                "Vidéo Wan",
                GenerateVideoFromUiAsync);

        _videoCancelButton.Click += (_, _) =>
            _videoCts?.Cancel();

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
        await Task.CompletedTask;
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

        _s.VideoWidth = Decimal.ToInt32(
            _videoWidth.Value);
        _s.VideoHeight = Decimal.ToInt32(
            _videoHeight.Value);
        _s.VideoFrames = Decimal.ToInt32(
            _videoFrames.Value);
        _s.VideoFps = Decimal.ToInt32(
            _videoFps.Value);
        _s.VideoSteps = Decimal.ToInt32(
            _videoSteps.Value);
        SettingsStore.Save(_s);
        _videoCts?.Dispose();
        _videoCts = new CancellationTokenSource();

        _videoGenerateButton.Enabled = false;
        _videoCancelButton.Enabled = true;
        _videoOpenButton.Enabled = false;
        _videoOutput.Clear();
        _videoProgress.Value = 0;

        try
        {
            var result =
                await _videoGenerator.GenerateAsync(
                    prompt,
                    _videoNegative.Text.Trim(),
                    _s.VideoWidth,
                    _s.VideoHeight,
                    _s.VideoFrames,
                    _s.VideoFps,
                    _s.VideoSteps,
                    _videoCts.Token);

            if (!result.Ok)
                throw new InvalidOperationException(
                    result.Error ??
                    "Échec de la génération vidéo.");
            _videoOutput.Text =
                result.Path ?? string.Empty;
            _videoOpenButton.Enabled =
                !string.IsNullOrWhiteSpace(result.Path);
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
        var models = new[]
        {
            (
                "Wan 2.1 T2V 1.3B",
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

        var lines = models.Select(model =>
            (File.Exists(model.Item2)
                ? "[OK] "
                : "[X] ") +
            model.Item1 +
            " — " +
            (File.Exists(model.Item2)
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
            Path.GetFileName(model.Item2));

        _videoModelStatus.Text =
            string.Join(
                Environment.NewLine +
                Environment.NewLine,
                lines);

        _videoGenerateButton.Enabled =
            models.All(model =>
                File.Exists(model.Item2)) &&
            _videoCts is null;

        _videoInstallButton.Enabled =
            models.Any(model =>
                !File.Exists(model.Item2)) &&
            _videoCts is null;
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

    private void ApplyVideoTranslations()
    {
        if (_tabVideo is null)
            return;

        _tabVideo.Text =
            L10n.Pick(
                _s.Language,
                "Vidéo",
                "Video");

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

    private static Label VideoLabel(
        string text,
        int x,
        int y,
        int width)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.White,
            BackColor = AppTheme.Background,
            Location = new Point(x, y),
            Size = new Size(width, 23),
            AutoSize = false
        };
    }

    private static TextBox VideoTextBox(
        int x,
        int y,
        int width,
        int height,
        bool multiline)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            Multiline = multiline,
            ScrollBars =
                multiline
                    ? ScrollBars.Vertical
                    : ScrollBars.None,
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static NumericUpDown VideoNumber(
        int x,
        int y,
        int width,
        int min,
        int max,
        int increment,
        int value)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Size = new Size(width, 27),
            Minimum = min,
            Maximum = max,
            Increment = increment,
            Value = Math.Clamp(value, min, max),
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text
        };
    }
}
