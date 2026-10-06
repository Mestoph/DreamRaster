/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Déclaration Designer WinForms de tous les contrôles.
EN: WinForms Designer declaration for all controls.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

#nullable enable

namespace OpenCodeLocalAI;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private TabControl _tabs = null!;
    private TabPage tabDashboard = null!;
    private TabPage tabOpenCode = null!;
    private TabPage tabComfy = null!;
    private TabPage tabOllama = null!;
    private TabPage tabGenerate = null!;
    private TabPage tabInstallation = null!;
    private TabPage tabConfiguration = null!;
    private TabPage tabLogs = null!;
    private TabPage tabAbout = null!;

    private Label lblTitle = null!;
    private Button btnStart = null!;
    private Button btnStop = null!;
    private Button btnOpenCode = null!;
    private Button btnComfy = null!;
    private Button btnDiagnostic = null!;
    private Label _status = null!;
    private Label _gpu = null!;
    private Label lblLiveLog = null!;
    private RichTextBox _liveLog = null!;

    private Microsoft.Web.WebView2.WinForms.WebView2 _web = null!;
    private Microsoft.Web.WebView2.WinForms.WebView2 _comfyWeb = null!;
    private RichTextBox _ollamaLog = null!;

    private Label lblPrompt = null!;
    private TextBox _prompt = null!;
    private Label lblGenerationMode = null!;
    private ComboBox cmbGenerationMode = null!;
    private Label lblInputImage = null!;
    private TextBox txtInputImage = null!;
    private Button btnBrowseInputImage = null!;
    private Button btnClearInputImage = null!;
    private Label lblImg2ImgStrength = null!;
    private NumericUpDown numImg2ImgStrength = null!;
    private Button btnGenerate = null!;
    private Label _genText = null!;
    private ProgressBar _genProgress = null!;
    private PictureBox _preview = null!;

    private Label lblInstallTitle = null!;
    private Label lblInstallInfo = null!;
    private Button btnInstallAll = null!;
    private Button btnInstallCancel = null!;
    private Label _installText = null!;
    private ProgressBar _installProgress = null!;
    private RichTextBox _installLog = null!;

    private Label lblConfigTitle = null!;
    private Label lblConfigRootCaption = null!;
    private TextBox txtConfigRoot = null!;
    private Label lblCfgOpenCodePort = null!;
    private Label lblCfgOllamaPort = null!;
    private Label lblCfgComfyPort = null!;
    private Label lblCfgProxyPort = null!;
    private Label lblCfgApiPort = null!;
    private NumericUpDown numOpenCodePort = null!;
    private NumericUpDown numOllamaPort = null!;
    private NumericUpDown numComfyPort = null!;
    private NumericUpDown numProxyPort = null!;
    private NumericUpDown numApiPort = null!;
    private Label lblCfgVisionModel = null!;
    private Label lblCfgFluxModel = null!;
    private Label lblCfgTextEncoder = null!;
    private Label lblCfgVae = null!;
    private TextBox txtVisionModel = null!;
    private TextBox txtFluxModel = null!;
    private TextBox txtTextEncoder = null!;
    private TextBox txtVae = null!;
    private Label lblCfgWidth = null!;
    private Label lblCfgHeight = null!;
    private Label lblCfgSteps = null!;
    private NumericUpDown numDefaultWidth = null!;
    private NumericUpDown numDefaultHeight = null!;
    private NumericUpDown numDefaultSteps = null!;
    private Label lblCfgVram = null!;
    private Label lblCfgRam = null!;
    private NumericUpDown numSafeVram = null!;
    private NumericUpDown numSafeRam = null!;
    private Label lblCfgConnections = null!;
    private Label lblCfgBuffer = null!;
    private NumericUpDown numDownloadConnections = null!;
    private NumericUpDown numDownloadBuffer = null!;
    private CheckBox chkHardStopComfy = null!;
    private Button btnSettingsSave = null!;
    private Button btnOpenConfigFolder = null!;
    private Label lblConfigHint = null!;
    private Label lblCfgLanguage = null!;
    private ComboBox cmbLanguage = null!;
    private CheckBox chkAutoUpdates = null!;
    private Label lblCfgGitHubRepo = null!;
    private TextBox txtGitHubRepo = null!;
    private CheckBox chkInstallVisionModel = null!;

    private RichTextBox _allLog = null!;

    private PictureBox picAboutIcon = null!;
    private Label lblAboutTitle = null!;
    private Label lblAboutDescription = null!;
    private Label lblAboutVersion = null!;
    private Label lblAboutAuthor = null!;
    private Label lblAboutRepository = null!;
    private Label lblAboutLicense = null!;
    private Label _aboutUpdateStatus = null!;
    private ProgressBar _aboutUpdateProgress = null!;
    private Button btnCheckUpdates = null!;
    private Button btnOpenGitHub = null!;

    private Button _imageModelDownloadButton = null!;
    private Button _benchmarkButton = null!;
    private CheckBox _randomSeedCheck = null!;
    private NumericUpDown _seedInput = null!;
    private Label _seedLabel = null!;
    private ComboBox _promptModelCombo = null!;
    private Label _promptModelLabel = null!;
    private CheckBox _autoImprovePrompt = null!;
    private Button _improvePromptButton = null!;
    private TextBox _negativePrompt = null!;
    private Label _negativePromptLabel = null!;
    private Label _imageStyleTemplateLabel = null!;
    private ComboBox _imageStyleTemplateCombo = null!;
    private Label _imageNegativeTemplateLabel = null!;
    private ComboBox _imageNegativeTemplateCombo = null!;
    private Label _imageHistoryLabel = null!;
    private FlowLayoutPanel _imageHistoryPanel = null!;
    private Label _imageCfgLabel = null!;
    private NumericUpDown _imageCfg = null!;
    private Label _imageMaxQualitySharpnessLabel = null!;
    private NumericUpDown _imageMaxQualitySharpness = null!;
    private Button _imageSharpnessPreviewButton = null!;
    private Label _imageSharpnessBeforeLabel = null!;
    private Label _imageSharpnessAfterLabel = null!;
    private PictureBox _imageSharpnessBeforePreview = null!;
    private PictureBox _imageSharpnessAfterPreview = null!;
    private Panel _imageSharpnessComparisonPanel = null!;
    private TrackBar _imageSharpnessComparisonSlider = null!;
    private FocusPanel _imagePreviewViewport = null!;
    private Label _imageModelRuntimeLabel = null!;
    private ComboBox _imageModelRuntimeCombo = null!;
    private Button _imageImportModelButton = null!;
    private Button _imageExtractPromptButton = null!;
    private Label _imageLoraLabel = null!;
    private ComboBox _imageLoraCombo = null!;
    private NumericUpDown _imageLoraStrength = null!;
    private Button _imageLoraAddButton = null!;
    private Button _imageLoraDownloadButton = null!;
    private ProgressBar _imageCatalogDownloadProgress = null!;
    private Label _imageCatalogDownloadStatus = null!;
    private Label _imageCatalogDownloadSize = null!;
    private Button _imageCatalogDownloadCancelButton = null!;
    private TabPage _tabVideo = null!;
    private Button _videoModelDownloadButton = null!;
    private Microsoft.Web.WebView2.WinForms.WebView2 _videoPreviewWeb = null!;
    private Label _videoPromptLabel = null!;
    private TextBox _videoPrompt = null!;
    private Label _videoNegativeLabel = null!;
    private TextBox _videoNegative = null!;
    private NumericUpDown _videoWidth = null!;
    private NumericUpDown _videoHeight = null!;
    private NumericUpDown _videoFrames = null!;
    private NumericUpDown _videoFps = null!;
    private Label _videoDurationLabel = null!;
    private NumericUpDown _videoDurationSeconds = null!;
    private NumericUpDown _videoSteps = null!;
    private Button _videoGenerateButton = null!;
    private Button _videoCancelButton = null!;
    private Button _videoRefreshButton = null!;
    private ProgressBar _videoProgress = null!;
    private Label _videoStatus = null!;
    private Label _videoModelStatus = null!;
    private Label _videoOutputLabel = null!;
    private TextBox _videoOutput = null!;
    private Button _videoOpenButton = null!;
    private Label _videoStyleTemplateLabel = null!;
    private ComboBox _videoStyleTemplateCombo = null!;
    private Label _videoNegativeTemplateLabel = null!;
    private ComboBox _videoNegativeTemplateCombo = null!;
    private Label _videoHistoryLabel = null!;
    private FlowLayoutPanel _videoHistoryPanel = null!;
    private NumericUpDown _videoCfg = null!;
    private Label _videoMaxQualitySharpnessLabel = null!;
    private NumericUpDown _videoMaxQualitySharpness = null!;
    private Button _videoSharpnessPreviewButton = null!;
    private Panel _videoSharpnessPreviewPanel = null!;
    private Label _videoSharpnessBeforeLabel = null!;
    private Label _videoSharpnessAfterLabel = null!;
    private PictureBox _videoSharpnessBeforePreview = null!;
    private PictureBox _videoSharpnessAfterPreview = null!;
    private NumericUpDown _videoSamplingShift = null!;
    private ComboBox _videoSampler = null!;
    private ComboBox _videoScheduler = null!;
    private NumericUpDown _videoSeed = null!;
    private CheckBox _videoRandomSeed = null!;
    private Label _videoQualityHint = null!;
    private Label _videoModelRuntimeLabel = null!;
    private ComboBox _videoModelRuntimeCombo = null!;
    private Button _videoImportModelButton = null!;
    private Label _videoReferenceLabel = null!;
    private TextBox _videoReferenceImage = null!;
    private Button _videoReferenceBrowseButton = null!;
    private Button _videoReferenceCropButton = null!;
    private Button _videoExtractPromptButton = null!;
    private Label _videoReferenceHint = null!;
    private Label _videoQualityLabel = null!;
    private ComboBox _videoQualityCombo = null!;
    private Label _videoLoraLabel = null!;
    private ComboBox _videoLoraCombo = null!;
    private NumericUpDown _videoLoraStrength = null!;
    private Button _videoLoraAddButton = null!;
    private Button _videoLoraDownloadButton = null!;
    private ProgressBar _videoCatalogDownloadProgress = null!;
    private Label _videoCatalogDownloadStatus = null!;
    private Label _videoCatalogDownloadSize = null!;
    private Button _videoCatalogDownloadCancelButton = null!;
    private Button _videoImprovePromptButton = null!;
    private CheckBox _videoAutoImprovePrompt = null!;
    private ComboBox _vaeCombo = null!;
    private ComboBox _textEncoderCombo = null!;
    private ComboBox _fluxModelCombo = null!;
    private ComboBox _visionModelCombo = null!;
    private Label _cfgVideoModelLabel = null!;
    private Label _cfgVideoTextEncoderLabel = null!;
    private Label _cfgVideoVaeLabel = null!;
    private TextBox _cfgVideoModel = null!;
    private TextBox _cfgVideoTextEncoder = null!;
    private TextBox _cfgVideoVae = null!;
    private Label _cfgVideoClipVisionLabel = null!;
    private TextBox _cfgVideoClipVision = null!;
    private CheckBox _autoSaveConfigurationCheck = null!;
    private Label _configurationSaveStatus = null!;
    private Button _installImageModelsButton = null!;
    private Button _installVideoModelsButton = null!;
    private Label _installVideoModelsStatus = null!;
    private TextBox _installComponentsStatus = null!;
    private Panel _comfyStatusPanel = null!;
    private Panel _comfyStatusCard = null!;
    private Label _comfyStatusTitle = null!;
    private Label _comfyStatusMessage = null!;
    private Button _comfyStatusRetryButton = null!;
    private Label _comfyStatusPortLabel = null!;
    private TabControl _logTabs = null!;
    private TabPage _logPageAll = null!;
    private TabPage _logPageUi = null!;
    private RichTextBox _logUi = null!;
    private TabPage _logPageOpenCode = null!;
    private RichTextBox _logOpenCode = null!;
    private TabPage _logPageOllama = null!;
    private RichTextBox _logOllama = null!;
    private TabPage _logPageComfy = null!;
    private RichTextBox _logComfy = null!;
    private TabPage _logPageFlux = null!;
    private RichTextBox _logFlux = null!;
    private TabPage _logPageVideo = null!;
    private RichTextBox _logVideo = null!;
    private TabPage _logPageInstall = null!;
    private RichTextBox _logInstall = null!;
    private TabPage _logPageSystem = null!;
    private RichTextBox _logSystem = null!;

    private Label _videoWidthLabel = null!;
    private Label _videoHeightLabel = null!;
    private Label _videoFramesLabel = null!;
    private Label _videoFpsLabel = null!;
    private Label _videoStepsLabel = null!;
    private Label _videoModelTitleLabel = null!;
    private Label _videoNoteLabel = null!;
    private Label _videoCfgLabel = null!;
    private Label _videoShiftLabel = null!;
    private Label _videoSamplerLabel = null!;
    private Label _videoSchedulerLabel = null!;
    private Label _videoSeedLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _tabs = new TabControl();
        tabDashboard = new TabPage();
        tabOpenCode = new TabPage();
        tabComfy = new TabPage();
        tabOllama = new TabPage();
        tabGenerate = new TabPage();
        tabInstallation = new TabPage();
        tabConfiguration = new TabPage();
        tabLogs = new TabPage();
        tabAbout = new TabPage();

        lblTitle = new Label();
        btnStart = new Button();
        btnStop = new Button();
        btnOpenCode = new Button();
        btnComfy = new Button();
        btnDiagnostic = new Button();
        _status = new Label();
        _gpu = new Label();
        lblLiveLog = new Label();
        _liveLog = new RichTextBox();

        _web = new Microsoft.Web.WebView2.WinForms.WebView2();
        _comfyWeb = new Microsoft.Web.WebView2.WinForms.WebView2();
        _ollamaLog = new RichTextBox();

        lblPrompt = new Label();
        _prompt = new TextBox();
        lblGenerationMode = new Label();
        cmbGenerationMode = new ComboBox();
        lblInputImage = new Label();
        txtInputImage = new TextBox();
        btnBrowseInputImage = new Button();
        btnClearInputImage = new Button();
        lblImg2ImgStrength = new Label();
        numImg2ImgStrength = new NumericUpDown();
        btnGenerate = new Button();
        _genText = new Label();
        _genProgress = new ProgressBar();
        _preview = new PictureBox();

        lblInstallTitle = new Label();
        lblInstallInfo = new Label();
        btnInstallAll = new Button();
        btnInstallCancel = new Button();
        _installText = new Label();
        _installProgress = new ProgressBar();
        _installLog = new RichTextBox();

        lblConfigTitle = new Label();
        lblConfigRootCaption = new Label();
        txtConfigRoot = new TextBox();
        lblCfgOpenCodePort = new Label();
        lblCfgOllamaPort = new Label();
        lblCfgComfyPort = new Label();
        lblCfgProxyPort = new Label();
        lblCfgApiPort = new Label();
        numOpenCodePort = new NumericUpDown();
        numOllamaPort = new NumericUpDown();
        numComfyPort = new NumericUpDown();
        numProxyPort = new NumericUpDown();
        numApiPort = new NumericUpDown();
        lblCfgVisionModel = new Label();
        lblCfgFluxModel = new Label();
        lblCfgTextEncoder = new Label();
        lblCfgVae = new Label();
        txtVisionModel = new TextBox();
        txtFluxModel = new TextBox();
        txtTextEncoder = new TextBox();
        txtVae = new TextBox();
        lblCfgWidth = new Label();
        lblCfgHeight = new Label();
        lblCfgSteps = new Label();
        numDefaultWidth = new NumericUpDown();
        numDefaultHeight = new NumericUpDown();
        numDefaultSteps = new NumericUpDown();
        lblCfgVram = new Label();
        lblCfgRam = new Label();
        numSafeVram = new NumericUpDown();
        numSafeRam = new NumericUpDown();
        lblCfgConnections = new Label();
        lblCfgBuffer = new Label();
        numDownloadConnections = new NumericUpDown();
        numDownloadBuffer = new NumericUpDown();
        chkHardStopComfy = new CheckBox();
        btnSettingsSave = new Button();
        btnOpenConfigFolder = new Button();
        lblConfigHint = new Label();
        lblCfgLanguage = new Label();
        cmbLanguage = new ComboBox();
        chkAutoUpdates = new CheckBox();
        lblCfgGitHubRepo = new Label();
        txtGitHubRepo = new TextBox();
        chkInstallVisionModel = new CheckBox();

        _allLog = new RichTextBox();

        picAboutIcon = new PictureBox();
        lblAboutTitle = new Label();
        lblAboutDescription = new Label();
        lblAboutVersion = new Label();
        lblAboutAuthor = new Label();
        lblAboutRepository = new Label();
        lblAboutLicense = new Label();
        _aboutUpdateStatus = new Label();
        _aboutUpdateProgress = new ProgressBar();
        btnCheckUpdates = new Button();
        btnOpenGitHub = new Button();

        ((System.ComponentModel.ISupportInitialize)_web).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_comfyWeb).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numImg2ImgStrength).BeginInit();
        ((System.ComponentModel.ISupportInitialize)picAboutIcon).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numOpenCodePort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numOllamaPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numComfyPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numProxyPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numApiPort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultWidth).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultHeight).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultSteps).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numSafeVram).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numSafeRam).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDownloadConnections).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDownloadBuffer).BeginInit();

        _tabs.SuspendLayout();
        tabDashboard.SuspendLayout();
        tabOpenCode.SuspendLayout();
        tabComfy.SuspendLayout();
        tabOllama.SuspendLayout();
        tabGenerate.SuspendLayout();
        tabInstallation.SuspendLayout();
        tabConfiguration.SuspendLayout();
        tabLogs.SuspendLayout();
        tabAbout.SuspendLayout();
        SuspendLayout();

        // 
        // _tabs
        // 
        _tabs.Controls.Add(tabDashboard);
        _tabs.Controls.Add(tabOpenCode);
        _tabs.Controls.Add(tabComfy);
        _tabs.Controls.Add(tabOllama);
        _tabs.Controls.Add(tabGenerate);
        _tabs.Controls.Add(tabInstallation);
        _tabs.Controls.Add(tabConfiguration);
        _tabs.Controls.Add(tabLogs);
        _tabs.Controls.Add(tabAbout);
        _tabs.Dock = DockStyle.Fill;
        _tabs.DrawMode = TabDrawMode.Normal;
        _tabs.ItemSize = new Size(112, 28);
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.DrawItem += Tabs_DrawItem;
        _tabs.Location = new Point(0, 0);
        _tabs.Name = "_tabs";
        _tabs.Padding = new Point(14, 5);
        _tabs.SelectedIndex = 0;
        _tabs.Size = new Size(1064, 681);
        _tabs.TabIndex = 0;

        // 
        // tabDashboard
        // 
        tabDashboard.BackColor = Color.FromArgb(24, 26, 31);
        tabDashboard.Controls.Add(_liveLog);
        tabDashboard.Controls.Add(lblLiveLog);
        tabDashboard.Controls.Add(_gpu);
        tabDashboard.Controls.Add(_status);
        tabDashboard.Controls.Add(btnDiagnostic);
        tabDashboard.Controls.Add(btnComfy);
        tabDashboard.Controls.Add(btnOpenCode);
        tabDashboard.Controls.Add(btnStop);
        tabDashboard.Controls.Add(btnStart);
        tabDashboard.Controls.Add(lblTitle);
        tabDashboard.Location = new Point(4, 28);
        tabDashboard.Name = "tabDashboard";
        tabDashboard.Padding = new Padding(12);
        tabDashboard.Size = new Size(1056, 649);
        tabDashboard.TabIndex = 0;
        tabDashboard.Text = "Tableau de bord";

        // 
        // lblTitle
        // 
        lblTitle.AutoSize = false;
        lblTitle.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(18, 14);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(400, 42);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "DreamRaster";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // btnStart
        // 
        btnStart.BackColor = Color.FromArgb(54, 135, 98);
        btnStart.Cursor = Cursors.Hand;
        btnStart.FlatAppearance.BorderSize = 0;
        btnStart.FlatStyle = FlatStyle.Flat;
        btnStart.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnStart.ForeColor = Color.White;
        btnStart.Location = new Point(18, 72);
        btnStart.Name = "btnStart";
        btnStart.Size = new Size(215, 34);
        btnStart.TabIndex = 1;
        btnStart.Text = "Démarrer";
        btnStart.UseVisualStyleBackColor = false;
        btnStart.Click += btnStart_Click;

        // 
        // btnStop
        // 
        btnStop.BackColor = Color.FromArgb(151, 62, 74);
        btnStop.Cursor = Cursors.Hand;
        btnStop.FlatAppearance.BorderSize = 0;
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnStop.ForeColor = Color.White;
        btnStop.Location = new Point(18, 112);
        btnStop.Name = "btnStop";
        btnStop.Size = new Size(215, 34);
        btnStop.TabIndex = 2;
        btnStop.Text = "Tout arrêter";
        btnStop.UseVisualStyleBackColor = false;
        btnStop.Click += btnStop_Click;

        // 
        // btnOpenCode
        // 
        btnOpenCode.BackColor = Color.FromArgb(52, 113, 181);
        btnOpenCode.Cursor = Cursors.Hand;
        btnOpenCode.FlatAppearance.BorderSize = 0;
        btnOpenCode.FlatStyle = FlatStyle.Flat;
        btnOpenCode.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnOpenCode.ForeColor = Color.White;
        btnOpenCode.Location = new Point(18, 152);
        btnOpenCode.Name = "btnOpenCode";
        btnOpenCode.Size = new Size(215, 34);
        btnOpenCode.TabIndex = 3;
        btnOpenCode.Text = "Ouvrir OpenCode intégré";
        btnOpenCode.UseVisualStyleBackColor = false;
        btnOpenCode.Click += btnOpenCode_Click;

        // 
        // btnComfy
        // 
        btnComfy.BackColor = Color.FromArgb(52, 113, 181);
        btnComfy.Cursor = Cursors.Hand;
        btnComfy.FlatAppearance.BorderSize = 0;
        btnComfy.FlatStyle = FlatStyle.Flat;
        btnComfy.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnComfy.ForeColor = Color.White;
        btnComfy.Location = new Point(18, 192);
        btnComfy.Name = "btnComfy";
        btnComfy.Size = new Size(215, 34);
        btnComfy.TabIndex = 4;
        btnComfy.Text = "Ouvrir ComfyUI intégré";
        btnComfy.UseVisualStyleBackColor = false;
        btnComfy.Click += btnComfy_Click;

        // 
        // btnDiagnostic
        // 
        btnDiagnostic.BackColor = Color.FromArgb(171, 128, 44);
        btnDiagnostic.Cursor = Cursors.Hand;
        btnDiagnostic.FlatAppearance.BorderSize = 0;
        btnDiagnostic.FlatStyle = FlatStyle.Flat;
        btnDiagnostic.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnDiagnostic.ForeColor = Color.White;
        btnDiagnostic.Location = new Point(18, 232);
        btnDiagnostic.Name = "btnDiagnostic";
        btnDiagnostic.Size = new Size(215, 34);
        btnDiagnostic.TabIndex = 5;
        btnDiagnostic.Text = "Exporter diagnostic";
        btnDiagnostic.UseVisualStyleBackColor = false;
        btnDiagnostic.Click += btnDiagnostic_Click;

        // 
        // _status
        // 
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _status.BackColor = Color.FromArgb(34, 37, 44);
        _status.ForeColor = Color.FromArgb(220, 224, 232);
        _status.Location = new Point(260, 72);
        _status.Name = "_status";
        _status.Padding = new Padding(8, 0, 8, 0);
        _status.Size = new Size(776, 54);
        _status.TabIndex = 5;
        _status.Text = "Prêt.";
        _status.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // _gpu
        // 
        _gpu.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _gpu.BackColor = Color.FromArgb(34, 37, 44);
        _gpu.ForeColor = Color.FromArgb(220, 224, 232);
        _gpu.Location = new Point(260, 136);
        _gpu.Name = "_gpu";
        _gpu.Padding = new Padding(8, 0, 8, 0);
        _gpu.Size = new Size(776, 54);
        _gpu.TabIndex = 6;
        _gpu.Text = "GPU : …";
        _gpu.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // lblLiveLog
        // 
        lblLiveLog.AutoSize = true;
        lblLiveLog.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        lblLiveLog.ForeColor = Color.FromArgb(220, 224, 232);
        lblLiveLog.Location = new Point(18, 287);
        lblLiveLog.Name = "lblLiveLog";
        lblLiveLog.Size = new Size(122, 19);
        lblLiveLog.TabIndex = 7;
        lblLiveLog.Text = "Console en direct";

        // 
        // _liveLog
        // 
        _liveLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _liveLog.BackColor = Color.FromArgb(18, 20, 24);
        _liveLog.BorderStyle = BorderStyle.FixedSingle;
        _liveLog.DetectUrls = false;
        _liveLog.Font = new Font("Consolas", 9F);
        _liveLog.ForeColor = Color.FromArgb(220, 224, 232);
        _liveLog.Location = new Point(18, 314);
        _liveLog.Name = "_liveLog";
        _liveLog.ReadOnly = true;
        _liveLog.Size = new Size(1018, 314);
        _liveLog.TabIndex = 8;
        _liveLog.Text = "";

        // 
        // tabOpenCode
        // 
        tabOpenCode.BackColor = Color.FromArgb(24, 26, 31);
        tabOpenCode.Controls.Add(_web);
        tabOpenCode.Location = new Point(4, 28);
        tabOpenCode.Name = "tabOpenCode";
        tabOpenCode.Padding = new Padding(3);
        tabOpenCode.Size = new Size(1056, 649);
        tabOpenCode.TabIndex = 1;
        tabOpenCode.Text = "OpenCode";

        // 
        // _web
        // 
        _web.AllowExternalDrop = false;
        _web.CreationProperties = null;
        _web.DefaultBackgroundColor = Color.FromArgb(24, 26, 31);
        _web.Dock = DockStyle.Fill;
        _web.Location = new Point(3, 3);
        _web.Name = "_web";
        _web.Size = new Size(1050, 643);
        _web.TabIndex = 0;
        _web.ZoomFactor = 1D;

        // 
        // tabComfy
        // 
        tabComfy.BackColor = Color.FromArgb(24, 26, 31);
        tabComfy.Controls.Add(_comfyWeb);
        tabComfy.Location = new Point(4, 28);
        tabComfy.Name = "tabComfy";
        tabComfy.Padding = new Padding(3);
        tabComfy.Size = new Size(1056, 649);
        tabComfy.TabIndex = 2;
        tabComfy.Text = "ComfyUI";

        // 
        // _comfyWeb
        // 
        _comfyWeb.AllowExternalDrop = false;
        _comfyWeb.CreationProperties = null;
        _comfyWeb.DefaultBackgroundColor = Color.FromArgb(24, 26, 31);
        _comfyWeb.Dock = DockStyle.Fill;
        _comfyWeb.Location = new Point(3, 3);
        _comfyWeb.Name = "_comfyWeb";
        _comfyWeb.Size = new Size(1050, 643);
        _comfyWeb.TabIndex = 0;
        _comfyWeb.ZoomFactor = 1D;

        // 
        // tabOllama
        // 
        tabOllama.BackColor = Color.FromArgb(24, 26, 31);
        tabOllama.Controls.Add(_ollamaLog);
        tabOllama.Location = new Point(4, 28);
        tabOllama.Name = "tabOllama";
        tabOllama.Padding = new Padding(8);
        tabOllama.Size = new Size(1056, 649);
        tabOllama.TabIndex = 3;
        tabOllama.Text = "Console Ollama";

        // 
        // _ollamaLog
        // 
        _ollamaLog.BackColor = Color.FromArgb(18, 20, 24);
        _ollamaLog.BorderStyle = BorderStyle.FixedSingle;
        _ollamaLog.DetectUrls = false;
        _ollamaLog.Dock = DockStyle.Fill;
        _ollamaLog.Font = new Font("Consolas", 9F);
        _ollamaLog.ForeColor = Color.FromArgb(220, 224, 232);
        _ollamaLog.Location = new Point(8, 8);
        _ollamaLog.Name = "_ollamaLog";
        _ollamaLog.ReadOnly = true;
        _ollamaLog.Size = new Size(1040, 633);
        _ollamaLog.TabIndex = 0;
        _ollamaLog.Text = "";

        // 
        // tabGenerate
        // 
        tabGenerate.BackColor = Color.FromArgb(24, 26, 31);
        tabGenerate.Controls.Add(_preview);
        tabGenerate.Controls.Add(_genProgress);
        tabGenerate.Controls.Add(_genText);
        tabGenerate.Controls.Add(btnGenerate);
        tabGenerate.Controls.Add(numImg2ImgStrength);
        tabGenerate.Controls.Add(lblImg2ImgStrength);
        tabGenerate.Controls.Add(btnClearInputImage);
        tabGenerate.Controls.Add(btnBrowseInputImage);
        tabGenerate.Controls.Add(txtInputImage);
        tabGenerate.Controls.Add(lblInputImage);
        tabGenerate.Controls.Add(cmbGenerationMode);
        tabGenerate.Controls.Add(lblGenerationMode);
        tabGenerate.Controls.Add(_prompt);
        tabGenerate.Controls.Add(lblPrompt);
        tabGenerate.Location = new Point(4, 28);
        tabGenerate.Name = "tabGenerate";
        tabGenerate.Padding = new Padding(12);
        tabGenerate.Size = new Size(1056, 649);
        tabGenerate.TabIndex = 4;
        tabGenerate.Text = "Générer";

        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        lblPrompt.ForeColor = Color.White;
        lblPrompt.Location = new Point(18, 18);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(96, 19);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Prompt FLUX.2";

        // 
        // _prompt
        // 
        _prompt.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _prompt.BackColor = Color.FromArgb(18, 20, 24);
        _prompt.BorderStyle = BorderStyle.FixedSingle;
        _prompt.ForeColor = Color.FromArgb(242, 244, 248);
        _prompt.Location = new Point(18, 45);
        _prompt.Multiline = true;
        _prompt.Name = "_prompt";
        _prompt.ScrollBars = ScrollBars.Vertical;
        _prompt.Size = new Size(330, 282);
        _prompt.TabIndex = 1;

        // 
        // lblGenerationMode
        // 
        lblGenerationMode.AutoSize = true;
        lblGenerationMode.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblGenerationMode.ForeColor = Color.White;
        lblGenerationMode.Location = new Point(18, 342);
        lblGenerationMode.Name = "lblGenerationMode";
        lblGenerationMode.Size = new Size(39, 15);
        lblGenerationMode.TabIndex = 2;
        lblGenerationMode.Text = "Mode";

        // 
        // cmbGenerationMode
        // 
        cmbGenerationMode.BackColor = Color.FromArgb(18, 20, 24);
        cmbGenerationMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbGenerationMode.ForeColor = Color.FromArgb(242, 244, 248);
        cmbGenerationMode.FormattingEnabled = true;
        cmbGenerationMode.Items.AddRange(new object[] { "Texte → image", "Image → image" });
        cmbGenerationMode.Location = new Point(18, 364);
        cmbGenerationMode.Name = "cmbGenerationMode";
        cmbGenerationMode.Size = new Size(170, 23);
        cmbGenerationMode.TabIndex = 3;
        cmbGenerationMode.SelectedIndexChanged += cmbGenerationMode_SelectedIndexChanged;

        // 
        // lblInputImage
        // 
        lblInputImage.AutoSize = true;
        lblInputImage.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblInputImage.ForeColor = Color.White;
        lblInputImage.Location = new Point(18, 399);
        lblInputImage.Name = "lblInputImage";
        lblInputImage.Size = new Size(77, 15);
        lblInputImage.TabIndex = 4;
        lblInputImage.Text = "Image source";

        // 
        // txtInputImage
        // 
        txtInputImage.BackColor = Color.FromArgb(18, 20, 24);
        txtInputImage.BorderStyle = BorderStyle.FixedSingle;
        txtInputImage.ForeColor = Color.FromArgb(242, 244, 248);
        txtInputImage.Location = new Point(18, 421);
        txtInputImage.Name = "txtInputImage";
        txtInputImage.ReadOnly = true;
        txtInputImage.Size = new Size(194, 23);
        txtInputImage.TabIndex = 5;

        // 
        // btnBrowseInputImage
        // 
        btnBrowseInputImage.BackColor = Color.FromArgb(52, 113, 181);
        btnBrowseInputImage.Cursor = Cursors.Hand;
        btnBrowseInputImage.FlatAppearance.BorderSize = 0;
        btnBrowseInputImage.FlatStyle = FlatStyle.Flat;
        btnBrowseInputImage.ForeColor = Color.White;
        btnBrowseInputImage.Location = new Point(202, 420);
        btnBrowseInputImage.Name = "btnBrowseInputImage";
        btnBrowseInputImage.Size = new Size(80, 25);
        btnBrowseInputImage.TabIndex = 6;
        btnBrowseInputImage.Text = "Parcourir…";
        btnBrowseInputImage.UseVisualStyleBackColor = false;
        btnBrowseInputImage.Click += btnBrowseInputImage_Click;

        // 
        // btnClearInputImage
        // 
        btnClearInputImage.BackColor = Color.FromArgb(151, 62, 74);
        btnClearInputImage.Cursor = Cursors.Hand;
        btnClearInputImage.FlatAppearance.BorderSize = 0;
        btnClearInputImage.FlatStyle = FlatStyle.Flat;
        btnClearInputImage.ForeColor = Color.White;
        btnClearInputImage.Location = new Point(288, 420);
        btnClearInputImage.Name = "btnClearInputImage";
        btnClearInputImage.Size = new Size(60, 25);
        btnClearInputImage.TabIndex = 7;
        btnClearInputImage.Text = "X";
        btnClearInputImage.UseVisualStyleBackColor = false;
        btnClearInputImage.Click += btnClearInputImage_Click;

        // 
        // lblImg2ImgStrength
        // 
        lblImg2ImgStrength.AutoSize = true;
        lblImg2ImgStrength.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblImg2ImgStrength.ForeColor = Color.White;
        lblImg2ImgStrength.Location = new Point(18, 456);
        lblImg2ImgStrength.Name = "lblImg2ImgStrength";
        lblImg2ImgStrength.Size = new Size(84, 15);
        lblImg2ImgStrength.TabIndex = 8;
        lblImg2ImgStrength.Text = "Force img2img";

        // 
        // numImg2ImgStrength
        // 
        numImg2ImgStrength.BackColor = Color.FromArgb(18, 20, 24);
        numImg2ImgStrength.DecimalPlaces = 2;
        numImg2ImgStrength.ForeColor = Color.FromArgb(242, 244, 248);
        numImg2ImgStrength.Increment = 0.05M;
        numImg2ImgStrength.Location = new Point(18, 478);
        numImg2ImgStrength.Maximum = 1;
        numImg2ImgStrength.Minimum = 0.05M;
        numImg2ImgStrength.Name = "numImg2ImgStrength";
        numImg2ImgStrength.Size = new Size(100, 23);
        numImg2ImgStrength.TabIndex = 9;
        numImg2ImgStrength.Value = 0.55M;

        // 
        // btnGenerate
        // 
        btnGenerate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnGenerate.BackColor = Color.FromArgb(54, 135, 98);
        btnGenerate.Cursor = Cursors.Hand;
        btnGenerate.FlatAppearance.BorderSize = 0;
        btnGenerate.FlatStyle = FlatStyle.Flat;
        btnGenerate.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnGenerate.ForeColor = Color.White;
        btnGenerate.Location = new Point(18, 520);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(125, 34);
        btnGenerate.TabIndex = 10;
        btnGenerate.Text = "Générer";
        btnGenerate.UseVisualStyleBackColor = false;
        btnGenerate.Click += btnGenerate_Click;

        // 
        // _genText
        // 
        _genText.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _genText.BackColor = Color.FromArgb(34, 37, 44);
        _genText.ForeColor = Color.FromArgb(220, 224, 232);
        _genText.Location = new Point(18, 566);
        _genText.Name = "_genText";
        _genText.Padding = new Padding(8, 0, 8, 0);
        _genText.Size = new Size(330, 30);
        _genText.TabIndex = 11;
        _genText.Text = "Prêt.";
        _genText.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // _genProgress
        // 
        _genProgress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _genProgress.Location = new Point(18, 603);
        _genProgress.Name = "_genProgress";
        _genProgress.Size = new Size(330, 18);
        _genProgress.TabIndex = 12;

        // 
        // _preview
        // 
        _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _preview.BackColor = Color.FromArgb(18, 20, 24);
        _preview.BorderStyle = BorderStyle.FixedSingle;
        _preview.Location = new Point(370, 18);
        _preview.Name = "_preview";
        _preview.Size = new Size(666, 603);
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.TabIndex = 13;
        _preview.TabStop = false;

        // 
        // tabInstallation
        // 
        tabInstallation.BackColor = Color.FromArgb(24, 26, 31);
        tabInstallation.Controls.Add(_installLog);
        tabInstallation.Controls.Add(_installProgress);
        tabInstallation.Controls.Add(_installText);
        tabInstallation.Controls.Add(btnInstallCancel);
        tabInstallation.Controls.Add(btnInstallAll);
        tabInstallation.Controls.Add(lblInstallInfo);
        tabInstallation.Controls.Add(lblInstallTitle);
        tabInstallation.Location = new Point(4, 28);
        tabInstallation.Name = "tabInstallation";
        tabInstallation.Padding = new Padding(12);
        tabInstallation.Size = new Size(1056, 649);
        tabInstallation.TabIndex = 5;
        tabInstallation.Text = "Installation";

        // 
        // lblInstallTitle
        // 
        lblInstallTitle.AutoSize = false;
        lblInstallTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        lblInstallTitle.ForeColor = Color.White;
        lblInstallTitle.Location = new Point(18, 14);
        lblInstallTitle.Name = "lblInstallTitle";
        lblInstallTitle.Size = new Size(400, 38);
        lblInstallTitle.TabIndex = 0;
        lblInstallTitle.Text = "Installation portable";
        lblInstallTitle.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // lblInstallInfo
        // 
        lblInstallInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblInstallInfo.Font = new Font("Segoe UI", 9.5F);
        lblInstallInfo.ForeColor = Color.FromArgb(242, 244, 248);
        lblInstallInfo.Location = new Point(18, 60);
        lblInstallInfo.Name = "lblInstallInfo";
        lblInstallInfo.Size = new Size(1018, 60);
        lblInstallInfo.TabIndex = 1;
        lblInstallInfo.Text = "Les composants sont installés uniquement dans le dossier portable. Les versions Ollama, OpenCode ou ComfyUI présentes ailleurs dans Windows ne sont jamais utilisées.";
        lblInstallInfo.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // btnInstallAll
        // 
        btnInstallAll.BackColor = Color.FromArgb(52, 113, 181);
        btnInstallAll.Cursor = Cursors.Hand;
        btnInstallAll.FlatAppearance.BorderSize = 0;
        btnInstallAll.FlatStyle = FlatStyle.Flat;
        btnInstallAll.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnInstallAll.ForeColor = Color.White;
        btnInstallAll.Location = new Point(18, 134);
        btnInstallAll.Name = "btnInstallAll";
        btnInstallAll.Size = new Size(235, 34);
        btnInstallAll.TabIndex = 2;
        btnInstallAll.Text = "Installer / réparer le pack portable";
        btnInstallAll.UseVisualStyleBackColor = false;
        btnInstallAll.Click += btnInstallAll_Click;

        // 
        // btnInstallCancel
        // 
        btnInstallCancel.BackColor = Color.FromArgb(151, 62, 74);
        btnInstallCancel.Cursor = Cursors.Hand;
        btnInstallCancel.FlatAppearance.BorderSize = 0;
        btnInstallCancel.FlatStyle = FlatStyle.Flat;
        btnInstallCancel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        btnInstallCancel.ForeColor = Color.White;
        btnInstallCancel.Location = new Point(265, 134);
        btnInstallCancel.Name = "btnInstallCancel";
        btnInstallCancel.Enabled = false;
        btnInstallCancel.Size = new Size(105, 34);
        btnInstallCancel.TabIndex = 3;
        btnInstallCancel.Text = "Annuler";
        btnInstallCancel.UseVisualStyleBackColor = false;
        btnInstallCancel.Click += btnInstallCancel_Click;

        // 
        // _installText
        // 
        _installText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _installText.BackColor = Color.FromArgb(34, 37, 44);
        _installText.ForeColor = Color.FromArgb(242, 244, 248);
        _installText.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        _installText.Location = new Point(18, 183);
        _installText.Name = "_installText";
        _installText.Padding = new Padding(8, 0, 8, 0);
        _installText.Size = new Size(1018, 34);
        _installText.TabIndex = 4;
        _installText.Text = "Aucune installation en cours.";
        _installText.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // _installProgress
        // 
        _installProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _installProgress.Location = new Point(18, 226);
        _installProgress.Name = "_installProgress";
        _installProgress.Size = new Size(1018, 18);
        _installProgress.TabIndex = 5;

        // 
        // _installLog
        // 
        _installLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _installLog.BackColor = Color.FromArgb(18, 20, 24);
        _installLog.BorderStyle = BorderStyle.FixedSingle;
        _installLog.DetectUrls = false;
        _installLog.Font = new Font("Consolas", 9F);
        _installLog.ForeColor = Color.FromArgb(220, 224, 232);
        _installLog.Location = new Point(18, 258);
        _installLog.Name = "_installLog";
        _installLog.ReadOnly = true;
        _installLog.Size = new Size(1018, 370);
        _installLog.TabIndex = 6;
        _installLog.Text = "";

        // 
        // tabConfiguration
        // 
        tabConfiguration.AutoScroll = true;
        tabConfiguration.BackColor = Color.FromArgb(24, 26, 31);
        tabConfiguration.Controls.Add(lblConfigHint);
        tabConfiguration.Controls.Add(chkInstallVisionModel);
        tabConfiguration.Controls.Add(txtGitHubRepo);
        tabConfiguration.Controls.Add(lblCfgGitHubRepo);
        tabConfiguration.Controls.Add(chkAutoUpdates);
        tabConfiguration.Controls.Add(cmbLanguage);
        tabConfiguration.Controls.Add(lblCfgLanguage);
        tabConfiguration.Controls.Add(btnOpenConfigFolder);
        tabConfiguration.Controls.Add(btnSettingsSave);
        tabConfiguration.Controls.Add(chkHardStopComfy);
        tabConfiguration.Controls.Add(numDownloadBuffer);
        tabConfiguration.Controls.Add(numDownloadConnections);
        tabConfiguration.Controls.Add(lblCfgBuffer);
        tabConfiguration.Controls.Add(lblCfgConnections);
        tabConfiguration.Controls.Add(numSafeRam);
        tabConfiguration.Controls.Add(numSafeVram);
        tabConfiguration.Controls.Add(lblCfgRam);
        tabConfiguration.Controls.Add(lblCfgVram);
        tabConfiguration.Controls.Add(numDefaultSteps);
        tabConfiguration.Controls.Add(numDefaultHeight);
        tabConfiguration.Controls.Add(numDefaultWidth);
        tabConfiguration.Controls.Add(lblCfgSteps);
        tabConfiguration.Controls.Add(lblCfgHeight);
        tabConfiguration.Controls.Add(lblCfgWidth);
        tabConfiguration.Controls.Add(txtVae);
        tabConfiguration.Controls.Add(txtTextEncoder);
        tabConfiguration.Controls.Add(txtFluxModel);
        tabConfiguration.Controls.Add(txtVisionModel);
        tabConfiguration.Controls.Add(lblCfgVae);
        tabConfiguration.Controls.Add(lblCfgTextEncoder);
        tabConfiguration.Controls.Add(lblCfgFluxModel);
        tabConfiguration.Controls.Add(lblCfgVisionModel);
        tabConfiguration.Controls.Add(numApiPort);
        tabConfiguration.Controls.Add(numProxyPort);
        tabConfiguration.Controls.Add(numComfyPort);
        tabConfiguration.Controls.Add(numOllamaPort);
        tabConfiguration.Controls.Add(numOpenCodePort);
        tabConfiguration.Controls.Add(lblCfgApiPort);
        tabConfiguration.Controls.Add(lblCfgProxyPort);
        tabConfiguration.Controls.Add(lblCfgComfyPort);
        tabConfiguration.Controls.Add(lblCfgOllamaPort);
        tabConfiguration.Controls.Add(lblCfgOpenCodePort);
        tabConfiguration.Controls.Add(txtConfigRoot);
        tabConfiguration.Controls.Add(lblConfigRootCaption);
        tabConfiguration.Controls.Add(lblConfigTitle);
        tabConfiguration.Location = new Point(4, 28);
        tabConfiguration.Name = "tabConfiguration";
        tabConfiguration.Padding = new Padding(12);
        tabConfiguration.Size = new Size(1056, 649);
        tabConfiguration.TabIndex = 6;
        tabConfiguration.Text = "Configuration";

        // 
        // lblConfigTitle
        // 
        lblConfigTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        lblConfigTitle.ForeColor = Color.White;
        lblConfigTitle.Location = new Point(18, 14);
        lblConfigTitle.Name = "lblConfigTitle";
        lblConfigTitle.Size = new Size(400, 36);
        lblConfigTitle.Text = "Configuration portable";

        // 
        // lblConfigRootCaption
        // 
        lblConfigRootCaption.ForeColor = Color.FromArgb(196, 202, 214);
        lblConfigRootCaption.Location = new Point(18, 58);
        lblConfigRootCaption.Name = "lblConfigRootCaption";
        lblConfigRootCaption.Size = new Size(130, 24);
        lblConfigRootCaption.Text = "Racine portable :";
        lblConfigRootCaption.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // txtConfigRoot
        // 
        txtConfigRoot.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtConfigRoot.BackColor = Color.FromArgb(18, 20, 24);
        txtConfigRoot.BorderStyle = BorderStyle.FixedSingle;
        txtConfigRoot.ForeColor = Color.FromArgb(242, 244, 248);
        txtConfigRoot.Location = new Point(150, 59);
        txtConfigRoot.Name = "txtConfigRoot";
        txtConfigRoot.ReadOnly = true;
        txtConfigRoot.Size = new Size(882, 23);

        // ports labels + controls
        lblCfgOpenCodePort.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgOpenCodePort.Location = new Point(18, 108);
        lblCfgOpenCodePort.Size = new Size(120, 23);
        lblCfgOpenCodePort.Text = "Port OpenCode";
        numOpenCodePort.Location = new Point(150, 108);
        numOpenCodePort.Maximum = 65535;
        numOpenCodePort.Minimum = 1024;
        numOpenCodePort.Size = new Size(120, 23);

        lblCfgOllamaPort.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgOllamaPort.Location = new Point(18, 141);
        lblCfgOllamaPort.Size = new Size(120, 23);
        lblCfgOllamaPort.Text = "Port Ollama";
        numOllamaPort.Location = new Point(150, 141);
        numOllamaPort.Maximum = 65535;
        numOllamaPort.Minimum = 1024;
        numOllamaPort.Size = new Size(120, 23);

        lblCfgComfyPort.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgComfyPort.Location = new Point(18, 174);
        lblCfgComfyPort.Size = new Size(120, 23);
        lblCfgComfyPort.Text = "Port ComfyUI";
        numComfyPort.Location = new Point(150, 174);
        numComfyPort.Maximum = 65535;
        numComfyPort.Minimum = 1024;
        numComfyPort.Size = new Size(120, 23);

        lblCfgProxyPort.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgProxyPort.Location = new Point(18, 207);
        lblCfgProxyPort.Size = new Size(120, 23);
        lblCfgProxyPort.Text = "Port Proxy";
        numProxyPort.Location = new Point(150, 207);
        numProxyPort.Maximum = 65535;
        numProxyPort.Minimum = 1024;
        numProxyPort.Size = new Size(120, 23);

        lblCfgApiPort.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgApiPort.Location = new Point(18, 240);
        lblCfgApiPort.Size = new Size(120, 23);
        lblCfgApiPort.Text = "Port API génération";
        numApiPort.Location = new Point(150, 240);
        numApiPort.Maximum = 65535;
        numApiPort.Minimum = 1024;
        numApiPort.Size = new Size(120, 23);

        // models
        lblCfgVisionModel.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgVisionModel.Location = new Point(330, 108);
        lblCfgVisionModel.Size = new Size(130, 23);
        lblCfgVisionModel.Text = "Modèle vision";
        txtVisionModel.Location = new Point(470, 108);
        txtVisionModel.Size = new Size(562, 23);

        lblCfgFluxModel.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgFluxModel.Location = new Point(330, 141);
        lblCfgFluxModel.Size = new Size(130, 23);
        lblCfgFluxModel.Text = "Modèle FLUX";
        txtFluxModel.Location = new Point(470, 141);
        txtFluxModel.Size = new Size(562, 23);

        lblCfgTextEncoder.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgTextEncoder.Location = new Point(330, 174);
        lblCfgTextEncoder.Size = new Size(130, 23);
        lblCfgTextEncoder.Text = "Text encoder";
        txtTextEncoder.Location = new Point(470, 174);
        txtTextEncoder.Size = new Size(562, 23);

        lblCfgVae.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgVae.Location = new Point(330, 207);
        lblCfgVae.Size = new Size(130, 23);
        lblCfgVae.Text = "VAE";
        txtVae.Location = new Point(470, 207);
        txtVae.Size = new Size(562, 23);

        // generation
        lblCfgWidth.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgWidth.Location = new Point(18, 302);
        lblCfgWidth.Size = new Size(120, 23);
        lblCfgWidth.Text = "Largeur défaut";
        numDefaultWidth.Location = new Point(150, 302);
        numDefaultWidth.Maximum = 4096;
        numDefaultWidth.Minimum = 256;
        numDefaultWidth.Increment = 64;
        numDefaultWidth.Size = new Size(120, 23);

        lblCfgHeight.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgHeight.Location = new Point(18, 335);
        lblCfgHeight.Size = new Size(120, 23);
        lblCfgHeight.Text = "Hauteur défaut";
        numDefaultHeight.Location = new Point(150, 335);
        numDefaultHeight.Maximum = 4096;
        numDefaultHeight.Minimum = 256;
        numDefaultHeight.Increment = 64;
        numDefaultHeight.Size = new Size(120, 23);

        lblCfgSteps.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgSteps.Location = new Point(18, 368);
        lblCfgSteps.Size = new Size(120, 23);
        lblCfgSteps.Text = "Steps défaut";
        numDefaultSteps.Location = new Point(150, 368);
        numDefaultSteps.Maximum = 100;
        numDefaultSteps.Minimum = 1;
        numDefaultSteps.Size = new Size(120, 23);

        lblCfgVram.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgVram.Location = new Point(330, 302);
        lblCfgVram.Size = new Size(130, 23);
        lblCfgVram.Text = "VRAM libre min MiB";
        numSafeVram.Location = new Point(470, 302);
        numSafeVram.Maximum = 65536;
        numSafeVram.Size = new Size(120, 23);

        lblCfgRam.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgRam.Location = new Point(330, 335);
        lblCfgRam.Size = new Size(130, 23);
        lblCfgRam.Text = "RAM libre min MiB";
        numSafeRam.Location = new Point(470, 335);
        numSafeRam.Maximum = 262144;
        numSafeRam.Size = new Size(120, 23);

        lblCfgConnections.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgConnections.Location = new Point(650, 302);
        lblCfgConnections.Size = new Size(150, 23);
        lblCfgConnections.Text = "Connexions téléchargement";
        numDownloadConnections.Location = new Point(820, 302);
        numDownloadConnections.Maximum = 16;
        numDownloadConnections.Minimum = 1;
        numDownloadConnections.Size = new Size(90, 23);

        lblCfgBuffer.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgBuffer.Location = new Point(650, 335);
        lblCfgBuffer.Size = new Size(150, 23);
        lblCfgBuffer.Text = "Buffer téléchargement MiB";
        numDownloadBuffer.Location = new Point(820, 335);
        numDownloadBuffer.Maximum = 32;
        numDownloadBuffer.Minimum = 1;
        numDownloadBuffer.Size = new Size(90, 23);

        chkHardStopComfy.ForeColor = Color.FromArgb(242, 244, 248);
        chkHardStopComfy.Location = new Point(330, 368);
        chkHardStopComfy.Name = "chkHardStopComfy";
        chkHardStopComfy.Size = new Size(330, 24);
        chkHardStopComfy.Text = "Arrêter ComfyUI après une génération";
        chkHardStopComfy.UseVisualStyleBackColor = true;

        // 
        // lblCfgLanguage
        // 
        lblCfgLanguage.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgLanguage.Location = new Point(18, 414);
        lblCfgLanguage.Name = "lblCfgLanguage";
        lblCfgLanguage.Size = new Size(120, 23);
        lblCfgLanguage.Text = "Langue";

        // 
        // cmbLanguage
        // 
        cmbLanguage.BackColor = Color.FromArgb(18, 20, 24);
        cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLanguage.ForeColor = Color.FromArgb(242, 244, 248);
        cmbLanguage.Items.AddRange(new object[] { "Français", "English" });
        cmbLanguage.Location = new Point(150, 414);
        cmbLanguage.Name = "cmbLanguage";
        cmbLanguage.Size = new Size(160, 23);
        cmbLanguage.SelectedIndexChanged += cmbLanguage_SelectedIndexChanged;

        // 
        // chkAutoUpdates
        // 
        chkAutoUpdates.ForeColor = Color.FromArgb(242, 244, 248);
        chkAutoUpdates.Location = new Point(330, 414);
        chkAutoUpdates.Name = "chkAutoUpdates";
        chkAutoUpdates.Size = new Size(380, 24);
        chkAutoUpdates.Text = "Rechercher automatiquement les mises à jour GitHub";
        chkAutoUpdates.UseVisualStyleBackColor = true;

        // 
        // lblCfgGitHubRepo
        // 
        lblCfgGitHubRepo.ForeColor = Color.FromArgb(196, 202, 214);
        lblCfgGitHubRepo.Location = new Point(18, 452);
        lblCfgGitHubRepo.Name = "lblCfgGitHubRepo";
        lblCfgGitHubRepo.Size = new Size(120, 23);
        lblCfgGitHubRepo.Text = "Dépôt GitHub";

        // 
        // txtGitHubRepo
        // 
        txtGitHubRepo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtGitHubRepo.Location = new Point(150, 452);
        txtGitHubRepo.Name = "txtGitHubRepo";
        txtGitHubRepo.Size = new Size(882, 23);

        // 
        // chkInstallVisionModel
        // 
        chkInstallVisionModel.ForeColor = Color.FromArgb(242, 244, 248);
        chkInstallVisionModel.Location = new Point(18, 490);
        chkInstallVisionModel.Name = "chkInstallVisionModel";
        chkInstallVisionModel.Size = new Size(430, 24);
        chkInstallVisionModel.Text = "Installer Qwen3-VL (vision, optionnel)";
        chkInstallVisionModel.UseVisualStyleBackColor = true;

        btnSettingsSave.BackColor = Color.FromArgb(54, 135, 98);
        btnSettingsSave.FlatAppearance.BorderSize = 0;
        btnSettingsSave.FlatStyle = FlatStyle.Flat;
        btnSettingsSave.ForeColor = Color.White;
        btnSettingsSave.Location = new Point(18, 560);
        btnSettingsSave.Name = "btnSettingsSave";
        btnSettingsSave.Size = new Size(190, 36);
        btnSettingsSave.Text = "Enregistrer la configuration";
        btnSettingsSave.UseVisualStyleBackColor = false;
        btnSettingsSave.Click += btnSettingsSave_Click;

        btnOpenConfigFolder.BackColor = Color.FromArgb(52, 113, 181);
        btnOpenConfigFolder.FlatAppearance.BorderSize = 0;
        btnOpenConfigFolder.FlatStyle = FlatStyle.Flat;
        btnOpenConfigFolder.ForeColor = Color.White;
        btnOpenConfigFolder.Location = new Point(220, 560);
        btnOpenConfigFolder.Name = "btnOpenConfigFolder";
        btnOpenConfigFolder.Size = new Size(170, 36);
        btnOpenConfigFolder.Text = "Ouvrir dossier config";
        btnOpenConfigFolder.UseVisualStyleBackColor = false;
        btnOpenConfigFolder.Click += btnOpenConfigFolder_Click;

        lblConfigHint.ForeColor = Color.FromArgb(196, 202, 214);
        lblConfigHint.Location = new Point(18, 608);
        lblConfigHint.Name = "lblConfigHint";
        lblConfigHint.Size = new Size(1014, 60);
        lblConfigHint.Text = "Le fichier config\\settings.json est créé dans la racine portable. Les changements de ports nécessitent un redémarrage de l'application.";
        lblConfigHint.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // tabLogs
        // 
        tabLogs.BackColor = Color.FromArgb(24, 26, 31);
        tabLogs.Controls.Add(_allLog);
        tabLogs.Location = new Point(4, 28);
        tabLogs.Name = "tabLogs";
        tabLogs.Padding = new Padding(8);
        tabLogs.Size = new Size(1056, 649);
        tabLogs.TabIndex = 7;
        tabLogs.Text = "Logs";

        // 
        // _allLog
        // 
        _allLog.BackColor = Color.FromArgb(18, 20, 24);
        _allLog.BorderStyle = BorderStyle.FixedSingle;
        _allLog.DetectUrls = false;
        _allLog.Dock = DockStyle.Fill;
        _allLog.Font = new Font("Consolas", 9F);
        _allLog.ForeColor = Color.FromArgb(220, 224, 232);
        _allLog.Location = new Point(8, 8);
        _allLog.Name = "_allLog";
        _allLog.ReadOnly = true;
        _allLog.Size = new Size(1040, 633);
        _allLog.TabIndex = 0;
        _allLog.Text = "";

        // 
        // tabAbout
        // 
        tabAbout.BackColor = Color.FromArgb(24, 26, 31);
        tabAbout.Controls.Add(btnOpenGitHub);
        tabAbout.Controls.Add(btnCheckUpdates);
        tabAbout.Controls.Add(_aboutUpdateProgress);
        tabAbout.Controls.Add(_aboutUpdateStatus);
        tabAbout.Controls.Add(lblAboutLicense);
        tabAbout.Controls.Add(lblAboutRepository);
        tabAbout.Controls.Add(lblAboutAuthor);
        tabAbout.Controls.Add(lblAboutVersion);
        tabAbout.Controls.Add(lblAboutDescription);
        tabAbout.Controls.Add(lblAboutTitle);
        tabAbout.Controls.Add(picAboutIcon);
        tabAbout.Location = new Point(4, 28);
        tabAbout.Name = "tabAbout";
        tabAbout.Padding = new Padding(12);
        tabAbout.Size = new Size(1056, 649);
        tabAbout.TabIndex = 8;
        tabAbout.Text = "À propos";

        // 
        // picAboutIcon
        // 
        picAboutIcon.Location = new Point(38, 44);
        picAboutIcon.Name = "picAboutIcon";
        picAboutIcon.Size = new Size(190, 190);
        picAboutIcon.SizeMode = PictureBoxSizeMode.Zoom;
        picAboutIcon.TabStop = false;

        // 
        // lblAboutTitle
        // 
        lblAboutTitle.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        lblAboutTitle.ForeColor = Color.White;
        lblAboutTitle.Location = new Point(265, 44);
        lblAboutTitle.Name = "lblAboutTitle";
        lblAboutTitle.Size = new Size(740, 48);
        lblAboutTitle.Text = "À propos de DreamRaster";

        // 
        // lblAboutDescription
        // 
        lblAboutDescription.ForeColor = Color.FromArgb(220, 224, 232);
        lblAboutDescription.Font = new Font("Segoe UI", 10F);
        lblAboutDescription.Location = new Point(267, 102);
        lblAboutDescription.Name = "lblAboutDescription";
        lblAboutDescription.Size = new Size(720, 60);
        lblAboutDescription.Text = "Studio Windows portable pour IA locale.";

        // 
        // lblAboutVersion
        // 
        lblAboutVersion.ForeColor = Color.FromArgb(196, 202, 214);
        lblAboutVersion.Location = new Point(267, 176);
        lblAboutVersion.Name = "lblAboutVersion";
        lblAboutVersion.Size = new Size(720, 24);
        lblAboutVersion.Text = "Version";

        // 
        // lblAboutAuthor
        // 
        lblAboutAuthor.ForeColor = Color.FromArgb(196, 202, 214);
        lblAboutAuthor.Location = new Point(267, 206);
        lblAboutAuthor.Name = "lblAboutAuthor";
        lblAboutAuthor.Size = new Size(720, 24);
        lblAboutAuthor.Text = "Auteur / propriétaire : Mestoph";

        // 
        // lblAboutRepository
        // 
        lblAboutRepository.ForeColor = Color.FromArgb(196, 202, 214);
        lblAboutRepository.Location = new Point(267, 236);
        lblAboutRepository.Name = "lblAboutRepository";
        lblAboutRepository.Size = new Size(720, 24);
        lblAboutRepository.Text = "Dépôt";

        // 
        // lblAboutLicense
        // 
        lblAboutLicense.ForeColor = Color.FromArgb(196, 202, 214);
        lblAboutLicense.Location = new Point(267, 266);
        lblAboutLicense.Name = "lblAboutLicense";
        lblAboutLicense.Size = new Size(720, 42);
        lblAboutLicense.Text = "Licence";

        // 
        // _aboutUpdateStatus
        // 
        _aboutUpdateStatus.BackColor = Color.FromArgb(34, 37, 44);
        _aboutUpdateStatus.ForeColor = Color.FromArgb(242, 244, 248);
        _aboutUpdateStatus.Location = new Point(38, 310);
        _aboutUpdateStatus.Name = "_aboutUpdateStatus";
        _aboutUpdateStatus.Padding = new Padding(10, 0, 10, 0);
        _aboutUpdateStatus.Size = new Size(949, 48);
        _aboutUpdateStatus.Text = "Prêt à vérifier les mises à jour.";
        _aboutUpdateStatus.TextAlign = ContentAlignment.MiddleLeft;

        // 
        // _aboutUpdateProgress
        // 
        _aboutUpdateProgress.Location = new Point(38, 372);
        _aboutUpdateProgress.Name = "_aboutUpdateProgress";
        _aboutUpdateProgress.Size = new Size(949, 18);

        // 
        // btnCheckUpdates
        // 
        btnCheckUpdates.BackColor = Color.FromArgb(52, 113, 181);
        btnCheckUpdates.FlatAppearance.BorderSize = 0;
        btnCheckUpdates.FlatStyle = FlatStyle.Flat;
        btnCheckUpdates.ForeColor = Color.White;
        btnCheckUpdates.Location = new Point(38, 414);
        btnCheckUpdates.Name = "btnCheckUpdates";
        btnCheckUpdates.Size = new Size(220, 36);
        btnCheckUpdates.Text = "Rechercher une mise à jour";
        btnCheckUpdates.UseVisualStyleBackColor = false;
        btnCheckUpdates.Click += btnCheckUpdates_Click;

        // 
        // btnOpenGitHub
        // 
        btnOpenGitHub.BackColor = Color.FromArgb(54, 135, 98);
        btnOpenGitHub.FlatAppearance.BorderSize = 0;
        btnOpenGitHub.FlatStyle = FlatStyle.Flat;
        btnOpenGitHub.ForeColor = Color.White;
        btnOpenGitHub.Location = new Point(270, 414);
        btnOpenGitHub.Name = "btnOpenGitHub";
        btnOpenGitHub.Size = new Size(180, 36);
        btnOpenGitHub.Text = "Ouvrir GitHub";
        btnOpenGitHub.UseVisualStyleBackColor = false;
        btnOpenGitHub.Click += btnOpenGitHub_Click;

        // ------------------------------------------------------------
        // Designer-owned runtime experience controls
        // ------------------------------------------------------------
        _imageModelDownloadButton = new Button();
        _benchmarkButton = new Button();
        _randomSeedCheck = new CheckBox();
        _seedInput = new NumericUpDown();
        _seedLabel = new Label();
        _promptModelCombo = new ComboBox();
        _promptModelLabel = new Label();
        _autoImprovePrompt = new CheckBox();
        _improvePromptButton = new Button();
        _negativePrompt = new TextBox();
        _negativePromptLabel = new Label();
        _imageStyleTemplateLabel = new Label();
        _imageStyleTemplateCombo = new ComboBox();
        _imageNegativeTemplateLabel = new Label();
        _imageNegativeTemplateCombo = new ComboBox();
        _imageHistoryLabel = new Label();
        _imageHistoryPanel = new FlowLayoutPanel();
        _imageCfgLabel = new Label();
        _imageCfg = new NumericUpDown();
        _imageMaxQualitySharpnessLabel = new Label();
        _imageMaxQualitySharpness = new NumericUpDown();
        _imageSharpnessPreviewButton = new Button();
        _imageSharpnessBeforeLabel = new Label();
        _imageSharpnessAfterLabel = new Label();
        _imageSharpnessBeforePreview = new PictureBox();
        _imageSharpnessAfterPreview = new PictureBox();
        _imageSharpnessComparisonPanel = new Panel();
        _imageSharpnessComparisonSlider = new TrackBar();
        _imagePreviewViewport = new FocusPanel();
        _imageModelRuntimeLabel = new Label();
        _imageModelRuntimeCombo = new ComboBox();
        _imageImportModelButton = new Button();
        _imageExtractPromptButton = new Button();
        _imageLoraLabel = new Label();
        _imageLoraCombo = new ComboBox();
        _imageLoraStrength = new NumericUpDown();
        _imageLoraAddButton = new Button();
        _imageLoraDownloadButton = new Button();
        _imageCatalogDownloadProgress = new ProgressBar();
        _imageCatalogDownloadStatus = new Label();
        _imageCatalogDownloadSize = new Label();
        _imageCatalogDownloadCancelButton = new Button();
        _tabVideo = new TabPage();
        _videoModelDownloadButton = new Button();
        _videoPreviewWeb = new Microsoft.Web.WebView2.WinForms.WebView2();
        _videoPromptLabel = new Label();
        _videoPrompt = new TextBox();
        _videoNegativeLabel = new Label();
        _videoNegative = new TextBox();
        _videoWidth = new NumericUpDown();
        _videoHeight = new NumericUpDown();
        _videoFrames = new NumericUpDown();
        _videoFps = new NumericUpDown();
        _videoDurationLabel = new Label();
        _videoDurationSeconds = new NumericUpDown();
        _videoSteps = new NumericUpDown();
        _videoGenerateButton = new Button();
        _videoCancelButton = new Button();
        _videoRefreshButton = new Button();
        _videoProgress = new ProgressBar();
        _videoStatus = new Label();
        _videoModelStatus = new Label();
        _videoOutputLabel = new Label();
        _videoOutput = new TextBox();
        _videoOpenButton = new Button();
        _videoStyleTemplateLabel = new Label();
        _videoStyleTemplateCombo = new ComboBox();
        _videoNegativeTemplateLabel = new Label();
        _videoNegativeTemplateCombo = new ComboBox();
        _videoHistoryLabel = new Label();
        _videoHistoryPanel = new FlowLayoutPanel();
        _videoCfg = new NumericUpDown();
        _videoMaxQualitySharpnessLabel = new Label();
        _videoMaxQualitySharpness = new NumericUpDown();
        _videoSharpnessPreviewButton = new Button();
        _videoSharpnessPreviewPanel = new Panel();
        _videoSharpnessBeforeLabel = new Label();
        _videoSharpnessAfterLabel = new Label();
        _videoSharpnessBeforePreview = new PictureBox();
        _videoSharpnessAfterPreview = new PictureBox();
        _videoSamplingShift = new NumericUpDown();
        _videoSampler = new ComboBox();
        _videoScheduler = new ComboBox();
        _videoSeed = new NumericUpDown();
        _videoRandomSeed = new CheckBox();
        _videoQualityHint = new Label();
        _videoModelRuntimeLabel = new Label();
        _videoModelRuntimeCombo = new ComboBox();
        _videoImportModelButton = new Button();
        _videoReferenceLabel = new Label();
        _videoReferenceImage = new TextBox();
        _videoReferenceBrowseButton = new Button();
        _videoReferenceCropButton = new Button();
        _videoExtractPromptButton = new Button();
        _videoReferenceHint = new Label();
        _videoQualityLabel = new Label();
        _videoQualityCombo = new ComboBox();
        _videoLoraLabel = new Label();
        _videoLoraCombo = new ComboBox();
        _videoLoraStrength = new NumericUpDown();
        _videoLoraAddButton = new Button();
        _videoLoraDownloadButton = new Button();
        _videoCatalogDownloadProgress = new ProgressBar();
        _videoCatalogDownloadStatus = new Label();
        _videoCatalogDownloadSize = new Label();
        _videoCatalogDownloadCancelButton = new Button();
        _videoImprovePromptButton = new Button();
        _videoAutoImprovePrompt = new CheckBox();
        _vaeCombo = new ComboBox();
        _textEncoderCombo = new ComboBox();
        _fluxModelCombo = new ComboBox();
        _visionModelCombo = new ComboBox();
        _cfgVideoModelLabel = new Label();
        _cfgVideoTextEncoderLabel = new Label();
        _cfgVideoVaeLabel = new Label();
        _cfgVideoModel = new TextBox();
        _cfgVideoTextEncoder = new TextBox();
        _cfgVideoVae = new TextBox();
        _cfgVideoClipVisionLabel = new Label();
        _cfgVideoClipVision = new TextBox();
        _autoSaveConfigurationCheck = new CheckBox();
        _configurationSaveStatus = new Label();
        _installImageModelsButton = new Button();
        _installVideoModelsButton = new Button();
        _installVideoModelsStatus = new Label();
        _installComponentsStatus = new TextBox();
        _comfyStatusPanel = new Panel();
        _comfyStatusCard = new Panel();
        _comfyStatusTitle = new Label();
        _comfyStatusMessage = new Label();
        _comfyStatusRetryButton = new Button();
        _comfyStatusPortLabel = new Label();
        _logTabs = new TabControl();
        _logPageAll = new TabPage();
        _logPageUi = new TabPage();
        _logUi = new RichTextBox();
        _logPageOpenCode = new TabPage();
        _logOpenCode = new RichTextBox();
        _logPageOllama = new TabPage();
        _logOllama = new RichTextBox();
        _logPageComfy = new TabPage();
        _logComfy = new RichTextBox();
        _logPageFlux = new TabPage();
        _logFlux = new RichTextBox();
        _logPageVideo = new TabPage();
        _logVideo = new RichTextBox();
        _logPageInstall = new TabPage();
        _logInstall = new RichTextBox();
        _logPageSystem = new TabPage();
        _logSystem = new RichTextBox();

        _videoWidthLabel = new Label();
        _videoHeightLabel = new Label();
        _videoFramesLabel = new Label();
        _videoFpsLabel = new Label();
        _videoStepsLabel = new Label();
        _videoModelTitleLabel = new Label();
        _videoNoteLabel = new Label();
        _videoCfgLabel = new Label();
        _videoShiftLabel = new Label();
        _videoSamplerLabel = new Label();
        _videoSchedulerLabel = new Label();
        _videoSeedLabel = new Label();

        // Canonical properties captured from the validated runtime layout
        // _imageModelDownloadButton
        _imageModelDownloadButton.Name = "_imageModelDownloadButton";
        _imageModelDownloadButton.Location = new Point(578, 17);
        _imageModelDownloadButton.Size = new Size(112, 27);
        _imageModelDownloadButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageModelDownloadButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageModelDownloadButton.Text = "Télécharger";
        _imageModelDownloadButton.TabIndex = 41;
        _imageModelDownloadButton.Enabled = false;
        _imageModelDownloadButton.Visible = true;
        _imageModelDownloadButton.FlatStyle = FlatStyle.Flat;

        // _benchmarkButton
        _benchmarkButton.Name = "_benchmarkButton";
        _benchmarkButton.Location = new Point(151, 545);
        _benchmarkButton.Size = new Size(197, 34);
        _benchmarkButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _benchmarkButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _benchmarkButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _benchmarkButton.Text = "Comparer 1.7B / 4B";
        _benchmarkButton.TabIndex = 23;
        _benchmarkButton.Enabled = false;
        _benchmarkButton.Visible = true;
        _benchmarkButton.FlatStyle = FlatStyle.Flat;

        // _randomSeedCheck
        _randomSeedCheck.Name = "_randomSeedCheck";
        _randomSeedCheck.Location = new Point(248, 406);
        _randomSeedCheck.Size = new Size(50, 23);
        _randomSeedCheck.BackColor = Color.FromArgb(255, 24, 26, 31);
        _randomSeedCheck.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _randomSeedCheck.Text = "Rnd";
        _randomSeedCheck.TabIndex = 22;
        _randomSeedCheck.Enabled = false;
        _randomSeedCheck.Visible = true;
        _randomSeedCheck.AutoSize = false;

        // _seedInput
        _seedInput.Name = "_seedInput";
        _seedInput.Location = new Point(132, 406);
        _seedInput.Size = new Size(110, 23);
        _seedInput.BackColor = Color.FromArgb(255, 18, 20, 24);
        _seedInput.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _seedInput.TabIndex = 21;
        _seedInput.Enabled = false;
        _seedInput.Visible = true;
        _seedInput.Minimum = 1M;
        _seedInput.Maximum = 9223372036854775807M;
        _seedInput.Increment = 1M;

        // _seedLabel
        _seedLabel.Name = "_seedLabel";
        _seedLabel.Location = new Point(132, 386);
        _seedLabel.Size = new Size(90, 18);
        _seedLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _seedLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _seedLabel.Text = "Seed";
        _seedLabel.TabIndex = 20;
        _seedLabel.Enabled = false;
        _seedLabel.Visible = true;
        _seedLabel.AutoSize = false;

        // _promptModelCombo
        _promptModelCombo.Name = "_promptModelCombo";
        _promptModelCombo.Location = new Point(116, 47);
        _promptModelCombo.Size = new Size(232, 23);
        _promptModelCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _promptModelCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _promptModelCombo.TabIndex = 19;
        _promptModelCombo.Enabled = false;
        _promptModelCombo.Visible = true;
        _promptModelCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _promptModelCombo.FlatStyle = FlatStyle.Flat;
        _promptModelCombo.DropDownWidth = 620;

        // _promptModelLabel
        _promptModelLabel.Name = "_promptModelLabel";
        _promptModelLabel.Location = new Point(18, 48);
        _promptModelLabel.Size = new Size(96, 23);
        _promptModelLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _promptModelLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _promptModelLabel.Text = "IA prompt";
        _promptModelLabel.TabIndex = 18;
        _promptModelLabel.Enabled = false;
        _promptModelLabel.Visible = true;
        _promptModelLabel.AutoSize = false;
        _promptModelLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _autoImprovePrompt
        _autoImprovePrompt.Name = "_autoImprovePrompt";
        _autoImprovePrompt.Location = new Point(276, 15);
        _autoImprovePrompt.Size = new Size(72, 24);
        _autoImprovePrompt.BackColor = Color.FromArgb(255, 24, 26, 31);
        _autoImprovePrompt.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _autoImprovePrompt.Text = "Auto";
        _autoImprovePrompt.TabIndex = 17;
        _autoImprovePrompt.Enabled = false;
        _autoImprovePrompt.Visible = true;
        _autoImprovePrompt.AutoSize = false;

        // _improvePromptButton
        _improvePromptButton.Name = "_improvePromptButton";
        _improvePromptButton.Location = new Point(130, 12);
        _improvePromptButton.Size = new Size(140, 28);
        _improvePromptButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _improvePromptButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _improvePromptButton.Text = "✨ Améliorer";
        _improvePromptButton.TabIndex = 16;
        _improvePromptButton.Enabled = false;
        _improvePromptButton.Visible = true;
        _improvePromptButton.FlatStyle = FlatStyle.Flat;

        // _negativePrompt
        _negativePrompt.Name = "_negativePrompt";
        _negativePrompt.Location = new Point(18, 213);
        _negativePrompt.Size = new Size(330, 52);
        _negativePrompt.BackColor = Color.FromArgb(255, 18, 20, 24);
        _negativePrompt.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _negativePrompt.TabIndex = 15;
        _negativePrompt.Enabled = false;
        _negativePrompt.Visible = true;
        _negativePrompt.Multiline = true;
        _negativePrompt.ScrollBars = ScrollBars.Vertical;
        _negativePrompt.BorderStyle = BorderStyle.FixedSingle;

        // _negativePromptLabel
        _negativePromptLabel.Name = "_negativePromptLabel";
        _negativePromptLabel.Location = new Point(18, 190);
        _negativePromptLabel.Size = new Size(154, 21);
        _negativePromptLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _negativePromptLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _negativePromptLabel.Text = "Négatif / éléments à éviter";
        _negativePromptLabel.TabIndex = 14;
        _negativePromptLabel.Enabled = false;
        _negativePromptLabel.Visible = true;
        _negativePromptLabel.AutoSize = true;

        // _imageStyleTemplateLabel
        _imageStyleTemplateLabel.Name = "imageStyleTemplateLabel";
        _imageStyleTemplateLabel.Location = new Point(370, 49);
        _imageStyleTemplateLabel.Size = new Size(45, 26);
        _imageStyleTemplateLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageStyleTemplateLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageStyleTemplateLabel.Text = "Style";
        _imageStyleTemplateLabel.TabIndex = 24;
        _imageStyleTemplateLabel.Enabled = false;
        _imageStyleTemplateLabel.Visible = true;
        _imageStyleTemplateLabel.AutoSize = false;
        _imageStyleTemplateLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageStyleTemplateCombo
        _imageStyleTemplateCombo.Name = "imageStyleTemplateCombo";
        _imageStyleTemplateCombo.Location = new Point(415, 50);
        _imageStyleTemplateCombo.Size = new Size(265, 23);
        _imageStyleTemplateCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageStyleTemplateCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageStyleTemplateCombo.TabIndex = 25;
        _imageStyleTemplateCombo.Enabled = false;
        _imageStyleTemplateCombo.Visible = true;
        _imageStyleTemplateCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _imageStyleTemplateCombo.FlatStyle = FlatStyle.Flat;
        _imageStyleTemplateCombo.DropDownWidth = 265;

        // _imageNegativeTemplateLabel
        _imageNegativeTemplateLabel.Name = "imageNegativeTemplateLabel";
        _imageNegativeTemplateLabel.Location = new Point(700, 49);
        _imageNegativeTemplateLabel.Size = new Size(62, 26);
        _imageNegativeTemplateLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageNegativeTemplateLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageNegativeTemplateLabel.Text = "Négatif";
        _imageNegativeTemplateLabel.TabIndex = 26;
        _imageNegativeTemplateLabel.Enabled = false;
        _imageNegativeTemplateLabel.Visible = true;
        _imageNegativeTemplateLabel.AutoSize = false;
        _imageNegativeTemplateLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageNegativeTemplateCombo
        _imageNegativeTemplateCombo.Name = "imageNegativeTemplateCombo";
        _imageNegativeTemplateCombo.Location = new Point(762, 50);
        _imageNegativeTemplateCombo.Size = new Size(274, 23);
        _imageNegativeTemplateCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageNegativeTemplateCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageNegativeTemplateCombo.TabIndex = 27;
        _imageNegativeTemplateCombo.Enabled = false;
        _imageNegativeTemplateCombo.Visible = true;
        _imageNegativeTemplateCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _imageNegativeTemplateCombo.FlatStyle = FlatStyle.Flat;
        _imageNegativeTemplateCombo.DropDownWidth = 274;

        // _imageHistoryLabel
        _imageHistoryLabel.Name = "imageHistoryLabel";
        _imageHistoryLabel.Location = new Point(370, 518);
        _imageHistoryLabel.Size = new Size(250, 20);
        _imageHistoryLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _imageHistoryLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageHistoryLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageHistoryLabel.Text = "Historique des images";
        _imageHistoryLabel.TabIndex = 28;
        _imageHistoryLabel.Enabled = false;
        _imageHistoryLabel.Visible = true;
        _imageHistoryLabel.AutoSize = false;
        _imageHistoryLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageHistoryPanel
        _imageHistoryPanel.Name = "imageHistoryPanel";
        _imageHistoryPanel.Location = new Point(370, 540);
        _imageHistoryPanel.Size = new Size(666, 88);
        _imageHistoryPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _imageHistoryPanel.BackColor = Color.FromArgb(255, 29, 32, 38);
        _imageHistoryPanel.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageHistoryPanel.TabIndex = 29;
        _imageHistoryPanel.Padding = new Padding(5, 5, 5, 5);
        _imageHistoryPanel.Enabled = false;
        _imageHistoryPanel.Visible = true;
        _imageHistoryPanel.AutoScroll = true;
        _imageHistoryPanel.WrapContents = false;

        // lblCfgWidth
        lblCfgWidth.Name = "lblCfgWidth";
        lblCfgWidth.Location = new Point(18, 474);
        lblCfgWidth.Size = new Size(90, 23);
        lblCfgWidth.BackColor = Color.FromArgb(255, 24, 26, 31);
        lblCfgWidth.ForeColor = Color.FromArgb(255, 196, 202, 214);
        lblCfgWidth.Text = "Largeur image";
        lblCfgWidth.TabIndex = 23;
        lblCfgWidth.Enabled = false;
        lblCfgWidth.Visible = true;
        lblCfgWidth.AutoSize = false;

        // numDefaultWidth
        numDefaultWidth.Name = "numDefaultWidth";
        numDefaultWidth.Location = new Point(108, 474);
        numDefaultWidth.Size = new Size(74, 23);
        numDefaultWidth.BackColor = Color.FromArgb(255, 18, 20, 24);
        numDefaultWidth.ForeColor = Color.FromArgb(255, 242, 244, 248);
        numDefaultWidth.TabIndex = 20;
        numDefaultWidth.Enabled = false;
        numDefaultWidth.Visible = true;
        numDefaultWidth.Minimum = 256M;
        numDefaultWidth.Maximum = 4096M;
        numDefaultWidth.Increment = 64M;

        // lblCfgHeight
        lblCfgHeight.Name = "lblCfgHeight";
        lblCfgHeight.Location = new Point(190, 474);
        lblCfgHeight.Size = new Size(86, 23);
        lblCfgHeight.BackColor = Color.FromArgb(255, 24, 26, 31);
        lblCfgHeight.ForeColor = Color.FromArgb(255, 196, 202, 214);
        lblCfgHeight.Text = "Hauteur image";
        lblCfgHeight.TabIndex = 22;
        lblCfgHeight.Enabled = false;
        lblCfgHeight.Visible = true;
        lblCfgHeight.AutoSize = false;

        // numDefaultHeight
        numDefaultHeight.Name = "numDefaultHeight";
        numDefaultHeight.Location = new Point(278, 474);
        numDefaultHeight.Size = new Size(70, 23);
        numDefaultHeight.BackColor = Color.FromArgb(255, 18, 20, 24);
        numDefaultHeight.ForeColor = Color.FromArgb(255, 242, 244, 248);
        numDefaultHeight.TabIndex = 19;
        numDefaultHeight.Enabled = false;
        numDefaultHeight.Visible = true;
        numDefaultHeight.Minimum = 256M;
        numDefaultHeight.Maximum = 4096M;
        numDefaultHeight.Increment = 64M;

        // lblCfgSteps
        lblCfgSteps.Name = "lblCfgSteps";
        lblCfgSteps.Location = new Point(18, 504);
        lblCfgSteps.Size = new Size(90, 23);
        lblCfgSteps.BackColor = Color.FromArgb(255, 24, 26, 31);
        lblCfgSteps.ForeColor = Color.FromArgb(255, 196, 202, 214);
        lblCfgSteps.Text = "Steps image";
        lblCfgSteps.TabIndex = 21;
        lblCfgSteps.Enabled = false;
        lblCfgSteps.Visible = true;
        lblCfgSteps.AutoSize = false;

        // numDefaultSteps
        numDefaultSteps.Name = "numDefaultSteps";
        numDefaultSteps.Location = new Point(108, 504);
        numDefaultSteps.Size = new Size(74, 23);
        numDefaultSteps.BackColor = Color.FromArgb(255, 18, 20, 24);
        numDefaultSteps.ForeColor = Color.FromArgb(255, 242, 244, 248);
        numDefaultSteps.TabIndex = 18;
        numDefaultSteps.Enabled = false;
        numDefaultSteps.Visible = true;
        numDefaultSteps.Minimum = 1M;
        numDefaultSteps.Maximum = 100M;
        numDefaultSteps.Increment = 1M;

        // _imageCfgLabel
        _imageCfgLabel.Name = "imageCfgLabel";
        _imageCfgLabel.Location = new Point(190, 504);
        _imageCfgLabel.Size = new Size(70, 23);
        _imageCfgLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageCfgLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageCfgLabel.Text = "CFG image";
        _imageCfgLabel.TabIndex = 30;
        _imageCfgLabel.Enabled = false;
        _imageCfgLabel.Visible = true;
        _imageCfgLabel.AutoSize = false;
        _imageCfgLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageCfg
        _imageCfg.Name = "_imageCfg";
        _imageCfg.Location = new Point(278, 504);
        _imageCfg.Size = new Size(70, 23);
        _imageCfg.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageCfg.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageCfg.TabIndex = 31;
        _imageCfg.Enabled = false;
        _imageCfg.Visible = true;
        _imageCfg.Minimum = 0.1M;
        _imageCfg.Maximum = 20M;
        _imageCfg.Increment = 0.1M;
        _imageCfg.DecimalPlaces = 1;

        // _imageMaxQualitySharpnessLabel
        _imageMaxQualitySharpnessLabel.Name = "_imageMaxQualitySharpnessLabel";
        _imageMaxQualitySharpnessLabel.Location = new Point(700, 82);
        _imageMaxQualitySharpnessLabel.Size = new Size(174, 23);
        _imageMaxQualitySharpnessLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageMaxQualitySharpnessLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageMaxQualitySharpnessLabel.Text = "Netteté passe 2 (%)";
        _imageMaxQualitySharpnessLabel.TabIndex = 90;
        _imageMaxQualitySharpnessLabel.Enabled = false;
        _imageMaxQualitySharpnessLabel.Visible = true;
        _imageMaxQualitySharpnessLabel.AutoSize = false;
        _imageMaxQualitySharpnessLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageMaxQualitySharpness
        _imageMaxQualitySharpness.Name = "_imageMaxQualitySharpness";
        _imageMaxQualitySharpness.Location = new Point(876, 82);
        _imageMaxQualitySharpness.Size = new Size(80, 23);
        _imageMaxQualitySharpness.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageMaxQualitySharpness.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageMaxQualitySharpness.TabIndex = 91;
        _imageMaxQualitySharpness.Enabled = false;
        _imageMaxQualitySharpness.Visible = true;
        _imageMaxQualitySharpness.Minimum = 0M;
        _imageMaxQualitySharpness.Maximum = 200M;
        _imageMaxQualitySharpness.Increment = 5M;
        _imageMaxQualitySharpness.Value = 75M;

        // _imageSharpnessPreviewButton
        _imageSharpnessPreviewButton.Name = "_imageSharpnessPreviewButton";
        _imageSharpnessPreviewButton.Location = new Point(876, 110);
        _imageSharpnessPreviewButton.Size = new Size(160, 26);
        _imageSharpnessPreviewButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageSharpnessPreviewButton.ForeColor = Color.White;
        _imageSharpnessPreviewButton.Text = "Aperçu avant/après";
        _imageSharpnessPreviewButton.TabIndex = 94;
        _imageSharpnessPreviewButton.FlatStyle = FlatStyle.Flat;

        // _imageSharpnessBeforeLabel
        _imageSharpnessBeforeLabel.Name = "_imageSharpnessBeforeLabel";
        _imageSharpnessBeforeLabel.Location = new Point(8, 5);
        _imageSharpnessBeforeLabel.Size = new Size(320, 22);
        _imageSharpnessBeforeLabel.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageSharpnessBeforeLabel.ForeColor = Color.White;
        _imageSharpnessBeforeLabel.Text = "Avant · upscale seul";
        _imageSharpnessBeforeLabel.TextAlign = ContentAlignment.MiddleCenter;

        // _imageSharpnessAfterLabel
        _imageSharpnessAfterLabel.Name = "_imageSharpnessAfterLabel";
        _imageSharpnessAfterLabel.Location = new Point(338, 5);
        _imageSharpnessAfterLabel.Size = new Size(320, 22);
        _imageSharpnessAfterLabel.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageSharpnessAfterLabel.ForeColor = Color.White;
        _imageSharpnessAfterLabel.Text = "Après · netteté";
        _imageSharpnessAfterLabel.TextAlign = ContentAlignment.MiddleCenter;

        // _imageSharpnessBeforePreview
        _imageSharpnessBeforePreview.Name = "_imageSharpnessBeforePreview";
        _imageSharpnessBeforePreview.Location = new Point(8, 30);
        _imageSharpnessBeforePreview.Size = new Size(320, 276);
        _imageSharpnessBeforePreview.BackColor = Color.FromArgb(255, 10, 11, 14);
        _imageSharpnessBeforePreview.SizeMode = PictureBoxSizeMode.Zoom;

        // _imageSharpnessAfterPreview
        _imageSharpnessAfterPreview.Name = "_imageSharpnessAfterPreview";
        _imageSharpnessAfterPreview.Location = new Point(338, 30);
        _imageSharpnessAfterPreview.Size = new Size(320, 276);
        _imageSharpnessAfterPreview.BackColor = Color.FromArgb(255, 10, 11, 14);
        _imageSharpnessAfterPreview.SizeMode = PictureBoxSizeMode.Zoom;
        _imageSharpnessBeforePreview.Visible = false;
        _imageSharpnessAfterPreview.Visible = false;

        // _imageSharpnessComparisonPanel
        _imageSharpnessComparisonPanel.Name = "_imageSharpnessComparisonPanel";
        _imageSharpnessComparisonPanel.Location = new Point(8, 30);
        _imageSharpnessComparisonPanel.Size = new Size(650, 242);
        _imageSharpnessComparisonPanel.BackColor = Color.FromArgb(255, 10, 11, 14);
        _imageSharpnessComparisonPanel.TabIndex = 96;
        _imageSharpnessComparisonPanel.Visible = false;

        // _imageSharpnessComparisonSlider
        _imageSharpnessComparisonSlider.Name = "_imageSharpnessComparisonSlider";
        _imageSharpnessComparisonSlider.Location = new Point(8, 276);
        _imageSharpnessComparisonSlider.Size = new Size(650, 30);
        _imageSharpnessComparisonSlider.Minimum = 0;
        _imageSharpnessComparisonSlider.Maximum = 100;
        _imageSharpnessComparisonSlider.Value = 50;
        _imageSharpnessComparisonSlider.TickStyle = TickStyle.None;
        _imageSharpnessComparisonSlider.SmallChange = 1;
        _imageSharpnessComparisonSlider.LargeChange = 10;
        _imageSharpnessComparisonSlider.TabIndex = 97;
        _imageSharpnessComparisonSlider.Visible = false;

        // _imagePreviewViewport
        _imagePreviewViewport.Name = "_imagePreviewViewport";
        _imagePreviewViewport.Location = new Point(370, 142);
        _imagePreviewViewport.Size = new Size(666, 314);
        _imagePreviewViewport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _imagePreviewViewport.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imagePreviewViewport.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imagePreviewViewport.TabIndex = 32;
        _imagePreviewViewport.Enabled = false;
        _imagePreviewViewport.Visible = true;

        // _preview
        _preview.Name = "_preview";
        _preview.Location = new Point(0, 0);
        _preview.Size = new Size(664, 312);
        _preview.Anchor = AnchorStyles.None;
        _preview.BackColor = Color.FromArgb(255, 18, 20, 24);
        _preview.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _preview.TabIndex = 13;
        _preview.Enabled = false;
        _preview.Visible = true;

        // _imageModelRuntimeLabel
        _imageModelRuntimeLabel.Name = "imageModelRuntimeLabel";
        _imageModelRuntimeLabel.Location = new Point(370, 17);
        _imageModelRuntimeLabel.Size = new Size(52, 26);
        _imageModelRuntimeLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageModelRuntimeLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageModelRuntimeLabel.Text = "Modèle";
        _imageModelRuntimeLabel.TabIndex = 33;
        _imageModelRuntimeLabel.Enabled = false;
        _imageModelRuntimeLabel.Visible = true;
        _imageModelRuntimeLabel.AutoSize = false;
        _imageModelRuntimeLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageModelRuntimeCombo
        _imageModelRuntimeCombo.Name = "_imageModelRuntimeCombo";
        _imageModelRuntimeCombo.Location = new Point(422, 18);
        _imageModelRuntimeCombo.Size = new Size(150, 23);
        _imageModelRuntimeCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageModelRuntimeCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageModelRuntimeCombo.TabIndex = 34;
        _imageModelRuntimeCombo.Enabled = false;
        _imageModelRuntimeCombo.Visible = true;
        _imageModelRuntimeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _imageModelRuntimeCombo.FlatStyle = FlatStyle.Flat;
        _imageModelRuntimeCombo.DropDownWidth = 620;

        // _imageImportModelButton
        _imageImportModelButton.Name = "_imageImportModelButton";
        _imageImportModelButton.Location = new Point(696, 17);
        _imageImportModelButton.Size = new Size(32, 27);
        _imageImportModelButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageImportModelButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageImportModelButton.Text = "+";
        _imageImportModelButton.TabIndex = 35;
        _imageImportModelButton.Enabled = false;
        _imageImportModelButton.Visible = true;
        _imageImportModelButton.FlatStyle = FlatStyle.Flat;

        // _imageExtractPromptButton
        _imageExtractPromptButton.Name = "_imageExtractPromptButton";
        _imageExtractPromptButton.Location = new Point(172, 438);
        _imageExtractPromptButton.Size = new Size(176, 30);
        _imageExtractPromptButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageExtractPromptButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageExtractPromptButton.Text = "Extraire prompt (VLM)";
        _imageExtractPromptButton.TabIndex = 36;
        _imageExtractPromptButton.Enabled = false;
        _imageExtractPromptButton.Visible = true;
        _imageExtractPromptButton.FlatStyle = FlatStyle.Flat;

        // _imageLoraLabel
        _imageLoraLabel.Name = "imageLoraLabel";
        _imageLoraLabel.Location = new Point(370, 80);
        _imageLoraLabel.Size = new Size(52, 26);
        _imageLoraLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageLoraLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _imageLoraLabel.Text = "LoRA";
        _imageLoraLabel.TabIndex = 37;
        _imageLoraLabel.Enabled = false;
        _imageLoraLabel.Visible = true;
        _imageLoraLabel.AutoSize = false;
        _imageLoraLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _imageLoraCombo
        _imageLoraCombo.Name = "_imageLoraCombo";
        _imageLoraCombo.Location = new Point(422, 81);
        _imageLoraCombo.Size = new Size(180, 23);
        _imageLoraCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageLoraCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageLoraCombo.TabIndex = 38;
        _imageLoraCombo.Enabled = false;
        _imageLoraCombo.Visible = true;
        _imageLoraCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _imageLoraCombo.FlatStyle = FlatStyle.Flat;
        _imageLoraCombo.DropDownWidth = 620;

        // _imageLoraStrength
        _imageLoraStrength.Name = "_imageLoraStrength";
        _imageLoraStrength.Location = new Point(608, 81);
        _imageLoraStrength.Size = new Size(62, 23);
        _imageLoraStrength.BackColor = Color.FromArgb(255, 18, 20, 24);
        _imageLoraStrength.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _imageLoraStrength.TabIndex = 39;
        _imageLoraStrength.Enabled = false;
        _imageLoraStrength.Visible = true;
        _imageLoraStrength.Minimum = -2M;
        _imageLoraStrength.Maximum = 2M;
        _imageLoraStrength.Increment = 0.05M;
        _imageLoraStrength.DecimalPlaces = 2;

        // _imageLoraAddButton
        _imageLoraAddButton.Name = "_imageLoraAddButton";
        _imageLoraAddButton.Location = new Point(798, 80);
        _imageLoraAddButton.Size = new Size(36, 27);
        _imageLoraAddButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageLoraAddButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageLoraAddButton.Text = "+";
        _imageLoraAddButton.TabIndex = 40;
        _imageLoraAddButton.Enabled = false;
        _imageLoraAddButton.Visible = true;
        _imageLoraAddButton.FlatStyle = FlatStyle.Flat;

        // _imageLoraDownloadButton
        _imageLoraDownloadButton.Name = "_imageLoraDownloadButton";
        _imageLoraDownloadButton.Location = new Point(676, 80);
        _imageLoraDownloadButton.Size = new Size(116, 27);
        _imageLoraDownloadButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _imageLoraDownloadButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageLoraDownloadButton.Text = "Télécharger";
        _imageLoraDownloadButton.TabIndex = 42;
        _imageLoraDownloadButton.Enabled = false;
        _imageLoraDownloadButton.Visible = true;
        _imageLoraDownloadButton.FlatStyle = FlatStyle.Flat;

        // _imageCatalogDownloadProgress
        _imageCatalogDownloadProgress.Name = "_imageCatalogDownloadProgress";
        _imageCatalogDownloadProgress.Location = new Point(520, 116);
        _imageCatalogDownloadProgress.Size = new Size(275, 16);
        _imageCatalogDownloadProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _imageCatalogDownloadProgress.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageCatalogDownloadProgress.ForeColor = Color.FromArgb(255, 0, 120, 215);
        _imageCatalogDownloadProgress.TabIndex = 43;
        _imageCatalogDownloadProgress.Enabled = false;
        _imageCatalogDownloadProgress.Visible = true;
        _imageCatalogDownloadProgress.Minimum = 0;
        _imageCatalogDownloadProgress.Maximum = 100;
        _imageCatalogDownloadProgress.Style = ProgressBarStyle.Continuous;

        // _imageCatalogDownloadStatus
        _imageCatalogDownloadStatus.Name = "_imageCatalogDownloadStatus";
        _imageCatalogDownloadStatus.Location = new Point(370, 112);
        _imageCatalogDownloadStatus.Size = new Size(145, 24);
        _imageCatalogDownloadStatus.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageCatalogDownloadStatus.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _imageCatalogDownloadStatus.TabIndex = 44;
        _imageCatalogDownloadStatus.Enabled = false;
        _imageCatalogDownloadStatus.Visible = true;
        _imageCatalogDownloadStatus.AutoSize = false;
        _imageCatalogDownloadStatus.AutoEllipsis = true;
        _imageCatalogDownloadStatus.TextAlign = ContentAlignment.MiddleLeft;

        // _imageCatalogDownloadSize
        _imageCatalogDownloadSize.Name = "_imageCatalogDownloadSize";
        _imageCatalogDownloadSize.Location = new Point(800, 112);
        _imageCatalogDownloadSize.Size = new Size(155, 24);
        _imageCatalogDownloadSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _imageCatalogDownloadSize.BackColor = Color.FromArgb(255, 24, 26, 31);
        _imageCatalogDownloadSize.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _imageCatalogDownloadSize.Text = "Taille : M 4,07 Go";
        _imageCatalogDownloadSize.TabIndex = 45;
        _imageCatalogDownloadSize.Enabled = false;
        _imageCatalogDownloadSize.Visible = true;
        _imageCatalogDownloadSize.AutoSize = false;
        _imageCatalogDownloadSize.AutoEllipsis = true;
        _imageCatalogDownloadSize.TextAlign = ContentAlignment.MiddleLeft;

        // _imageCatalogDownloadCancelButton
        _imageCatalogDownloadCancelButton.Name = "_imageCatalogDownloadCancelButton";
        _imageCatalogDownloadCancelButton.Location = new Point(960, 112);
        _imageCatalogDownloadCancelButton.Size = new Size(76, 24);
        _imageCatalogDownloadCancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _imageCatalogDownloadCancelButton.BackColor = Color.FromArgb(255, 151, 62, 74);
        _imageCatalogDownloadCancelButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _imageCatalogDownloadCancelButton.Text = "Annuler";
        _imageCatalogDownloadCancelButton.TabIndex = 46;
        _imageCatalogDownloadCancelButton.Enabled = false;
        _imageCatalogDownloadCancelButton.Visible = true;
        _imageCatalogDownloadCancelButton.FlatStyle = FlatStyle.Flat;

        // _tabVideo
        _tabVideo.Name = "tabVideo";
        _tabVideo.Location = new Point(4, 32);
        _tabVideo.Size = new Size(1056, 645);
        _tabVideo.BackColor = Color.FromArgb(255, 24, 26, 31);
        _tabVideo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _tabVideo.Text = "Vidéo";
        _tabVideo.TabIndex = 9;
        _tabVideo.Enabled = false;
        _tabVideo.Visible = true;

        // _videoModelDownloadButton
        _videoModelDownloadButton.Name = "_videoModelDownloadButton";
        _videoModelDownloadButton.Location = new Point(578, 17);
        _videoModelDownloadButton.Size = new Size(112, 27);
        _videoModelDownloadButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoModelDownloadButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoModelDownloadButton.Text = "Télécharger";
        _videoModelDownloadButton.TabIndex = 62;
        _videoModelDownloadButton.Enabled = false;
        _videoModelDownloadButton.Visible = true;
        _videoModelDownloadButton.FlatStyle = FlatStyle.Flat;

        // _videoPreviewWeb
        _videoPreviewWeb.Name = "_videoPreviewWeb";
        _videoPreviewWeb.Location = new Point(370, 142);
        _videoPreviewWeb.Size = new Size(666, 314);
        _videoPreviewWeb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _videoPreviewWeb.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoPreviewWeb.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoPreviewWeb.TabIndex = 55;
        _videoPreviewWeb.Enabled = false;
        _videoPreviewWeb.Visible = true;

        // _videoPromptLabel
        _videoPromptLabel.Name = "videoPromptLabel";
        _videoPromptLabel.Location = new Point(18, 18);
        _videoPromptLabel.Size = new Size(106, 23);
        _videoPromptLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoPromptLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoPromptLabel.Text = "Prompt vidéo";
        _videoPromptLabel.TabIndex = 0;
        _videoPromptLabel.Enabled = false;
        _videoPromptLabel.Visible = true;
        _videoPromptLabel.AutoSize = false;

        // _videoPrompt
        _videoPrompt.Name = "videoPrompt";
        _videoPrompt.Location = new Point(18, 45);
        _videoPrompt.Size = new Size(330, 88);
        _videoPrompt.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoPrompt.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoPrompt.TabIndex = 1;
        _videoPrompt.Enabled = false;
        _videoPrompt.Visible = true;
        _videoPrompt.Multiline = true;
        _videoPrompt.ScrollBars = ScrollBars.Vertical;
        _videoPrompt.BorderStyle = BorderStyle.FixedSingle;

        // _videoNegativeLabel
        _videoNegativeLabel.Name = "videoNegativeLabel";
        _videoNegativeLabel.Location = new Point(18, 138);
        _videoNegativeLabel.Size = new Size(330, 23);
        _videoNegativeLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoNegativeLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoNegativeLabel.Text = "Négatif / éléments à éviter";
        _videoNegativeLabel.TabIndex = 2;
        _videoNegativeLabel.Enabled = false;
        _videoNegativeLabel.Visible = true;
        _videoNegativeLabel.AutoSize = false;

        // _videoNegative
        _videoNegative.Name = "videoNegative";
        _videoNegative.Location = new Point(18, 163);
        _videoNegative.Size = new Size(330, 48);
        _videoNegative.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoNegative.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoNegative.TabIndex = 3;
        _videoNegative.Enabled = false;
        _videoNegative.Visible = true;
        _videoNegative.Multiline = true;
        _videoNegative.ScrollBars = ScrollBars.Vertical;
        _videoNegative.BorderStyle = BorderStyle.FixedSingle;

        // _videoWidth
        _videoWidth.Name = "videoWidth";
        _videoWidth.Location = new Point(86, 303);
        _videoWidth.Size = new Size(78, 23);
        _videoWidth.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoWidth.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoWidth.TabIndex = 5;
        _videoWidth.Enabled = false;
        _videoWidth.Visible = true;
        _videoWidth.Minimum = 256M;
        _videoWidth.Maximum = 1280M;
        _videoWidth.Increment = 16M;

        // _videoHeight
        _videoHeight.Name = "videoHeight";
        _videoHeight.Location = new Point(246, 303);
        _videoHeight.Size = new Size(102, 23);
        _videoHeight.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoHeight.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoHeight.TabIndex = 7;
        _videoHeight.Enabled = false;
        _videoHeight.Visible = true;
        _videoHeight.Minimum = 256M;
        _videoHeight.Maximum = 1280M;
        _videoHeight.Increment = 16M;

        // _videoFrames
        _videoFrames.Name = "videoFrames";
        _videoFrames.Location = new Point(192, 338);
        _videoFrames.Size = new Size(52, 23);
        _videoFrames.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoFrames.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoFrames.TabIndex = 9;
        _videoFrames.Enabled = false;
        _videoFrames.Visible = true;
        _videoFrames.Minimum = 1M;
        _videoFrames.Maximum = 161M;
        _videoFrames.Increment = 4M;

        // _videoFps
        _videoFps.Name = "videoFps";
        _videoFps.Location = new Point(280, 338);
        _videoFps.Size = new Size(68, 23);
        _videoFps.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoFps.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoFps.TabIndex = 11;
        _videoFps.Enabled = false;
        _videoFps.Visible = true;
        _videoFps.Minimum = 1M;
        _videoFps.Maximum = 60M;
        _videoFps.Increment = 1M;

        // _videoDurationLabel
        _videoDurationLabel.Name = "_videoDurationLabel";
        _videoDurationLabel.Location = new Point(18, 342);
        _videoDurationLabel.Size = new Size(64, 23);
        _videoDurationLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoDurationLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoDurationLabel.Text = "Durée (s)";
        _videoDurationLabel.TabIndex = 12;
        _videoDurationLabel.Enabled = false;
        _videoDurationLabel.Visible = true;
        _videoDurationLabel.AutoSize = false;

        // _videoDurationSeconds
        _videoDurationSeconds.Name = "_videoDurationSeconds";
        _videoDurationSeconds.Location = new Point(84, 338);
        _videoDurationSeconds.Size = new Size(54, 23);
        _videoDurationSeconds.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoDurationSeconds.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoDurationSeconds.TabIndex = 13;
        _videoDurationSeconds.Enabled = false;
        _videoDurationSeconds.Visible = true;
        _videoDurationSeconds.Minimum = 0.05M;
        _videoDurationSeconds.Maximum = 10.06M;
        _videoDurationSeconds.Increment = 0.25M;
        _videoDurationSeconds.DecimalPlaces = 2;

        // _videoSteps
        _videoSteps.Name = "videoSteps";
        _videoSteps.Location = new Point(86, 373);
        _videoSteps.Size = new Size(78, 23);
        _videoSteps.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoSteps.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoSteps.TabIndex = 15;
        _videoSteps.Enabled = false;
        _videoSteps.Visible = true;
        _videoSteps.Minimum = 1M;
        _videoSteps.Maximum = 100M;
        _videoSteps.Increment = 1M;

        // _videoGenerateButton
        _videoGenerateButton.Name = "videoGenerateButton";
        _videoGenerateButton.Location = new Point(18, 545);
        _videoGenerateButton.Size = new Size(125, 34);
        _videoGenerateButton.BackColor = Color.FromArgb(255, 54, 135, 98);
        _videoGenerateButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoGenerateButton.Text = "Générer la vidéo";
        _videoGenerateButton.TabIndex = 16;
        _videoGenerateButton.Enabled = false;
        _videoGenerateButton.Visible = true;
        _videoGenerateButton.FlatStyle = FlatStyle.Flat;

        // _videoCancelButton
        _videoCancelButton.Name = "videoCancelButton";
        _videoCancelButton.Location = new Point(151, 545);
        _videoCancelButton.Size = new Size(90, 34);
        _videoCancelButton.BackColor = Color.FromArgb(255, 151, 62, 74);
        _videoCancelButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoCancelButton.Text = "Annuler";
        _videoCancelButton.TabIndex = 17;
        _videoCancelButton.Enabled = false;
        _videoCancelButton.Visible = true;
        _videoCancelButton.FlatStyle = FlatStyle.Flat;

        // _videoRefreshButton
        _videoRefreshButton.Name = "videoRefreshButton";
        _videoRefreshButton.Location = new Point(249, 545);
        _videoRefreshButton.Size = new Size(99, 34);
        _videoRefreshButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoRefreshButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoRefreshButton.Text = "Actualiser";
        _videoRefreshButton.TabIndex = 18;
        _videoRefreshButton.Enabled = false;
        _videoRefreshButton.Visible = true;
        _videoRefreshButton.FlatStyle = FlatStyle.Flat;

        // _videoProgress
        _videoProgress.Name = "videoProgress";
        _videoProgress.Location = new Point(18, 617);
        _videoProgress.Size = new Size(330, 18);
        _videoProgress.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoProgress.ForeColor = Color.FromArgb(255, 0, 120, 215);
        _videoProgress.TabIndex = 19;
        _videoProgress.Enabled = false;
        _videoProgress.Visible = true;
        _videoProgress.Minimum = 0;
        _videoProgress.Maximum = 100;

        // _videoStatus
        _videoStatus.Name = "videoStatus";
        _videoStatus.Location = new Point(18, 583);
        _videoStatus.Size = new Size(330, 30);
        _videoStatus.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoStatus.ForeColor = Color.FromArgb(255, 171, 128, 44);
        _videoStatus.Text = "Dépendances manquantes : Wan T2V, UMT5 XXL, Wan VAE";
        _videoStatus.TabIndex = 20;
        _videoStatus.Enabled = false;
        _videoStatus.Visible = true;
        _videoStatus.AutoSize = false;
        _videoStatus.AutoEllipsis = true;

        // _videoModelStatus
        _videoModelStatus.Name = "videoModelStatus";
        _videoModelStatus.Location = new Point(448, 52);
        _videoModelStatus.Size = new Size(570, 210);
        _videoModelStatus.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoModelStatus.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoModelStatus.Text = "[X] Wan T2V — non installé\r\n    wan2.1_t2v_1.3B_fp16.safetensors\r\n\r\n[X] UMT5 XXL — non installé\r\n    umt5_xxl_fp8_e4m3fn_scaled.safetensors\r\n\r\n[X] Wan VAE — non installé\r\n    wan_2.1_vae.safetensors";
        _videoModelStatus.TabIndex = 22;
        _videoModelStatus.Enabled = false;
        _videoModelStatus.Visible = true;
        _videoModelStatus.AutoSize = false;

        // _videoOutputLabel
        _videoOutputLabel.Name = "videoOutputLabel";
        _videoOutputLabel.Location = new Point(370, 462);
        _videoOutputLabel.Size = new Size(110, 22);
        _videoOutputLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoOutputLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoOutputLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoOutputLabel.Text = "Fichier vidéo";
        _videoOutputLabel.TabIndex = 23;
        _videoOutputLabel.Enabled = false;
        _videoOutputLabel.Visible = true;
        _videoOutputLabel.AutoSize = false;

        // _videoOutput
        _videoOutput.Name = "videoOutput";
        _videoOutput.Location = new Point(370, 484);
        _videoOutput.Size = new Size(496, 23);
        _videoOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _videoOutput.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoOutput.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoOutput.TabIndex = 24;
        _videoOutput.Enabled = false;
        _videoOutput.Visible = true;
        _videoOutput.ReadOnly = true;
        _videoOutput.BorderStyle = BorderStyle.FixedSingle;

        // _videoOpenButton
        _videoOpenButton.Name = "videoOpenButton";
        _videoOpenButton.Location = new Point(876, 481);
        _videoOpenButton.Size = new Size(160, 34);
        _videoOpenButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _videoOpenButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoOpenButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoOpenButton.Text = "Ouvrir la vidéo";
        _videoOpenButton.TabIndex = 25;
        _videoOpenButton.Enabled = false;
        _videoOpenButton.Visible = true;
        _videoOpenButton.FlatStyle = FlatStyle.Flat;

        // _videoStyleTemplateLabel
        _videoStyleTemplateLabel.Name = "videoStyleTemplateLabel";
        _videoStyleTemplateLabel.Location = new Point(370, 49);
        _videoStyleTemplateLabel.Size = new Size(45, 26);
        _videoStyleTemplateLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoStyleTemplateLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoStyleTemplateLabel.Text = "Style";
        _videoStyleTemplateLabel.TabIndex = 28;
        _videoStyleTemplateLabel.Enabled = false;
        _videoStyleTemplateLabel.Visible = true;
        _videoStyleTemplateLabel.AutoSize = false;
        _videoStyleTemplateLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoStyleTemplateCombo
        _videoStyleTemplateCombo.Name = "videoStyleTemplateCombo";
        _videoStyleTemplateCombo.Location = new Point(415, 50);
        _videoStyleTemplateCombo.Size = new Size(265, 23);
        _videoStyleTemplateCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoStyleTemplateCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoStyleTemplateCombo.TabIndex = 29;
        _videoStyleTemplateCombo.Enabled = false;
        _videoStyleTemplateCombo.Visible = true;
        _videoStyleTemplateCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoStyleTemplateCombo.FlatStyle = FlatStyle.Flat;
        _videoStyleTemplateCombo.DropDownWidth = 265;

        // _videoNegativeTemplateLabel
        _videoNegativeTemplateLabel.Name = "videoNegativeTemplateLabel";
        _videoNegativeTemplateLabel.Location = new Point(700, 49);
        _videoNegativeTemplateLabel.Size = new Size(62, 26);
        _videoNegativeTemplateLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoNegativeTemplateLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoNegativeTemplateLabel.Text = "Négatif";
        _videoNegativeTemplateLabel.TabIndex = 30;
        _videoNegativeTemplateLabel.Enabled = false;
        _videoNegativeTemplateLabel.Visible = true;
        _videoNegativeTemplateLabel.AutoSize = false;
        _videoNegativeTemplateLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoNegativeTemplateCombo
        _videoNegativeTemplateCombo.Name = "videoNegativeTemplateCombo";
        _videoNegativeTemplateCombo.Location = new Point(762, 50);
        _videoNegativeTemplateCombo.Size = new Size(274, 23);
        _videoNegativeTemplateCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoNegativeTemplateCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoNegativeTemplateCombo.TabIndex = 31;
        _videoNegativeTemplateCombo.Enabled = false;
        _videoNegativeTemplateCombo.Visible = true;
        _videoNegativeTemplateCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoNegativeTemplateCombo.FlatStyle = FlatStyle.Flat;
        _videoNegativeTemplateCombo.DropDownWidth = 274;

        // _videoHistoryLabel
        _videoHistoryLabel.Name = "videoHistoryLabel";
        _videoHistoryLabel.Location = new Point(370, 518);
        _videoHistoryLabel.Size = new Size(220, 20);
        _videoHistoryLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _videoHistoryLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoHistoryLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoHistoryLabel.Text = "Historique vidéo";
        _videoHistoryLabel.TabIndex = 32;
        _videoHistoryLabel.Enabled = false;
        _videoHistoryLabel.Visible = true;
        _videoHistoryLabel.AutoSize = false;
        _videoHistoryLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoHistoryPanel
        _videoHistoryPanel.Name = "videoHistoryPanel";
        _videoHistoryPanel.Location = new Point(370, 540);
        _videoHistoryPanel.Size = new Size(666, 88);
        _videoHistoryPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _videoHistoryPanel.BackColor = Color.FromArgb(255, 29, 32, 38);
        _videoHistoryPanel.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoHistoryPanel.TabIndex = 33;
        _videoHistoryPanel.Padding = new Padding(5, 5, 5, 5);
        _videoHistoryPanel.Enabled = false;
        _videoHistoryPanel.Visible = true;
        _videoHistoryPanel.AutoScroll = true;
        _videoHistoryPanel.WrapContents = false;

        // _videoCfg
        _videoCfg.Name = "_videoCfg";
        _videoCfg.Location = new Point(246, 373);
        _videoCfg.Size = new Size(102, 23);
        _videoCfg.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoCfg.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoCfg.TabIndex = 35;
        _videoCfg.Enabled = false;
        _videoCfg.Visible = true;
        _videoCfg.Minimum = 1M;
        _videoCfg.Maximum = 20M;
        _videoCfg.Increment = 0.5M;
        _videoCfg.DecimalPlaces = 1;

        // _videoMaxQualitySharpnessLabel
        _videoMaxQualitySharpnessLabel.Name = "_videoMaxQualitySharpnessLabel";
        _videoMaxQualitySharpnessLabel.Location = new Point(700, 82);
        _videoMaxQualitySharpnessLabel.Size = new Size(174, 23);
        _videoMaxQualitySharpnessLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoMaxQualitySharpnessLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoMaxQualitySharpnessLabel.Text = "Netteté passe 2 (%)";
        _videoMaxQualitySharpnessLabel.TabIndex = 92;
        _videoMaxQualitySharpnessLabel.Enabled = false;
        _videoMaxQualitySharpnessLabel.Visible = true;
        _videoMaxQualitySharpnessLabel.AutoSize = false;
        _videoMaxQualitySharpnessLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoMaxQualitySharpness
        _videoMaxQualitySharpness.Name = "_videoMaxQualitySharpness";
        _videoMaxQualitySharpness.Location = new Point(876, 82);
        _videoMaxQualitySharpness.Size = new Size(80, 23);
        _videoMaxQualitySharpness.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoMaxQualitySharpness.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoMaxQualitySharpness.TabIndex = 93;
        _videoMaxQualitySharpness.Enabled = false;
        _videoMaxQualitySharpness.Visible = true;
        _videoMaxQualitySharpness.Minimum = 0M;
        _videoMaxQualitySharpness.Maximum = 200M;
        _videoMaxQualitySharpness.Increment = 5M;
        _videoMaxQualitySharpness.Value = 65M;

        // _videoSharpnessPreviewButton
        _videoSharpnessPreviewButton.Name = "_videoSharpnessPreviewButton";
        _videoSharpnessPreviewButton.Location = new Point(876, 110);
        _videoSharpnessPreviewButton.Size = new Size(160, 26);
        _videoSharpnessPreviewButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoSharpnessPreviewButton.ForeColor = Color.White;
        _videoSharpnessPreviewButton.Text = "Aperçu avant/après";
        _videoSharpnessPreviewButton.TabIndex = 95;
        _videoSharpnessPreviewButton.FlatStyle = FlatStyle.Flat;

        // _videoSharpnessPreviewPanel
        _videoSharpnessPreviewPanel.Name = "_videoSharpnessPreviewPanel";
        _videoSharpnessPreviewPanel.Location = new Point(370, 142);
        _videoSharpnessPreviewPanel.Size = new Size(666, 314);
        _videoSharpnessPreviewPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _videoSharpnessPreviewPanel.BackColor = Color.FromArgb(255, 18, 20, 24);

        // _videoSharpnessBeforeLabel
        _videoSharpnessBeforeLabel.Name = "_videoSharpnessBeforeLabel";
        _videoSharpnessBeforeLabel.Location = new Point(8, 5);
        _videoSharpnessBeforeLabel.Size = new Size(320, 22);
        _videoSharpnessBeforeLabel.ForeColor = Color.White;
        _videoSharpnessBeforeLabel.Text = "Avant · upscale seul";
        _videoSharpnessBeforeLabel.TextAlign = ContentAlignment.MiddleCenter;

        // _videoSharpnessAfterLabel
        _videoSharpnessAfterLabel.Name = "_videoSharpnessAfterLabel";
        _videoSharpnessAfterLabel.Location = new Point(338, 5);
        _videoSharpnessAfterLabel.Size = new Size(320, 22);
        _videoSharpnessAfterLabel.ForeColor = Color.White;
        _videoSharpnessAfterLabel.Text = "Après · netteté";
        _videoSharpnessAfterLabel.TextAlign = ContentAlignment.MiddleCenter;

        // _videoSharpnessBeforePreview
        _videoSharpnessBeforePreview.Name = "_videoSharpnessBeforePreview";
        _videoSharpnessBeforePreview.Location = new Point(8, 30);
        _videoSharpnessBeforePreview.Size = new Size(320, 276);
        _videoSharpnessBeforePreview.BackColor = Color.FromArgb(255, 10, 11, 14);
        _videoSharpnessBeforePreview.SizeMode = PictureBoxSizeMode.Zoom;

        // _videoSharpnessAfterPreview
        _videoSharpnessAfterPreview.Name = "_videoSharpnessAfterPreview";
        _videoSharpnessAfterPreview.Location = new Point(338, 30);
        _videoSharpnessAfterPreview.Size = new Size(320, 276);
        _videoSharpnessAfterPreview.BackColor = Color.FromArgb(255, 10, 11, 14);
        _videoSharpnessAfterPreview.SizeMode = PictureBoxSizeMode.Zoom;

        // _videoSamplingShift
        _videoSamplingShift.Name = "_videoSamplingShift";
        _videoSamplingShift.Location = new Point(86, 408);
        _videoSamplingShift.Size = new Size(78, 23);
        _videoSamplingShift.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoSamplingShift.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoSamplingShift.TabIndex = 37;
        _videoSamplingShift.Enabled = false;
        _videoSamplingShift.Visible = true;
        _videoSamplingShift.Minimum = 0M;
        _videoSamplingShift.Maximum = 20M;
        _videoSamplingShift.Increment = 0.5M;
        _videoSamplingShift.DecimalPlaces = 1;

        // _videoSampler
        _videoSampler.Name = "_videoSampler";
        _videoSampler.Location = new Point(246, 408);
        _videoSampler.Size = new Size(102, 23);
        _videoSampler.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoSampler.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoSampler.TabIndex = 39;
        _videoSampler.Enabled = false;
        _videoSampler.Visible = true;
        _videoSampler.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoSampler.FlatStyle = FlatStyle.Flat;
        _videoSampler.DropDownWidth = 102;
        _videoSampler.Items.AddRange(new object[] { "uni_pc", "euler", "euler_ancestral", "dpmpp_2m" });

        // _videoScheduler
        _videoScheduler.Name = "_videoScheduler";
        _videoScheduler.Location = new Point(86, 443);
        _videoScheduler.Size = new Size(78, 23);
        _videoScheduler.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoScheduler.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoScheduler.TabIndex = 41;
        _videoScheduler.Enabled = false;
        _videoScheduler.Visible = true;
        _videoScheduler.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoScheduler.FlatStyle = FlatStyle.Flat;
        _videoScheduler.DropDownWidth = 78;
        _videoScheduler.Items.AddRange(new object[] { "simple", "normal", "karras", "sgm_uniform" });

        // _videoSeed
        _videoSeed.Name = "_videoSeed";
        _videoSeed.Location = new Point(246, 443);
        _videoSeed.Size = new Size(68, 23);
        _videoSeed.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoSeed.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoSeed.TabIndex = 43;
        _videoSeed.Enabled = false;
        _videoSeed.Visible = true;
        _videoSeed.Minimum = 1M;
        _videoSeed.Maximum = 9223372036854775807M;
        _videoSeed.Increment = 1M;

        // _videoRandomSeed
        _videoRandomSeed.Name = "_videoRandomSeed";
        _videoRandomSeed.Location = new Point(318, 443);
        _videoRandomSeed.Size = new Size(30, 23);
        _videoRandomSeed.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoRandomSeed.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoRandomSeed.Text = "R";
        _videoRandomSeed.TabIndex = 44;
        _videoRandomSeed.Enabled = false;
        _videoRandomSeed.Visible = true;
        _videoRandomSeed.AutoSize = false;

        // _videoQualityHint
        _videoQualityHint.Name = "videoQualityHint";
        _videoQualityHint.Location = new Point(18, 507);
        _videoQualityHint.Size = new Size(330, 34);
        _videoQualityHint.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoQualityHint.ForeColor = Color.FromArgb(255, 65, 157, 114);
        _videoQualityHint.Text = "✓ Meilleure : 50 steps · CFG 6 · shift 8 · uni_pc · simple.";
        _videoQualityHint.TabIndex = 45;
        _videoQualityHint.Enabled = false;
        _videoQualityHint.Visible = true;
        _videoQualityHint.AutoSize = false;

        // _videoModelRuntimeLabel
        _videoModelRuntimeLabel.Name = "videoModelRuntimeLabel";
        _videoModelRuntimeLabel.Location = new Point(370, 17);
        _videoModelRuntimeLabel.Size = new Size(52, 26);
        _videoModelRuntimeLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoModelRuntimeLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoModelRuntimeLabel.Text = "Modèle";
        _videoModelRuntimeLabel.TabIndex = 46;
        _videoModelRuntimeLabel.Enabled = false;
        _videoModelRuntimeLabel.Visible = true;
        _videoModelRuntimeLabel.AutoSize = false;
        _videoModelRuntimeLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoModelRuntimeCombo
        _videoModelRuntimeCombo.Name = "_videoModelRuntimeCombo";
        _videoModelRuntimeCombo.Location = new Point(422, 18);
        _videoModelRuntimeCombo.Size = new Size(150, 23);
        _videoModelRuntimeCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoModelRuntimeCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoModelRuntimeCombo.TabIndex = 47;
        _videoModelRuntimeCombo.Enabled = false;
        _videoModelRuntimeCombo.Visible = true;
        _videoModelRuntimeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoModelRuntimeCombo.FlatStyle = FlatStyle.Flat;
        _videoModelRuntimeCombo.DropDownWidth = 620;

        // _videoImportModelButton
        _videoImportModelButton.Name = "_videoImportModelButton";
        _videoImportModelButton.Location = new Point(696, 17);
        _videoImportModelButton.Size = new Size(32, 27);
        _videoImportModelButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoImportModelButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoImportModelButton.Text = "+";
        _videoImportModelButton.TabIndex = 48;
        _videoImportModelButton.Enabled = false;
        _videoImportModelButton.Visible = true;
        _videoImportModelButton.FlatStyle = FlatStyle.Flat;

        // _videoReferenceLabel
        _videoReferenceLabel.Name = "videoReferenceLabel";
        _videoReferenceLabel.Location = new Point(18, 218);
        _videoReferenceLabel.Size = new Size(180, 23);
        _videoReferenceLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoReferenceLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoReferenceLabel.Text = "Image de référence";
        _videoReferenceLabel.TabIndex = 49;
        _videoReferenceLabel.Enabled = false;
        _videoReferenceLabel.Visible = true;
        _videoReferenceLabel.AutoSize = false;

        // _videoReferenceImage
        _videoReferenceImage.Name = "_videoReferenceImage";
        _videoReferenceImage.Location = new Point(18, 241);
        _videoReferenceImage.Size = new Size(190, 23);
        _videoReferenceImage.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoReferenceImage.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoReferenceImage.TabIndex = 50;
        _videoReferenceImage.Enabled = false;
        _videoReferenceImage.Visible = true;
        _videoReferenceImage.ReadOnly = true;
        _videoReferenceImage.BorderStyle = BorderStyle.FixedSingle;

        // _videoReferenceBrowseButton
        _videoReferenceBrowseButton.Name = "_videoReferenceBrowseButton";
        _videoReferenceBrowseButton.Location = new Point(214, 240);
        _videoReferenceBrowseButton.Size = new Size(58, 25);
        _videoReferenceBrowseButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoReferenceBrowseButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoReferenceBrowseButton.Text = "…";
        _videoReferenceBrowseButton.TabIndex = 51;
        _videoReferenceBrowseButton.Enabled = false;
        _videoReferenceBrowseButton.Visible = true;
        _videoReferenceBrowseButton.FlatStyle = FlatStyle.Flat;

        // _videoReferenceCropButton
        _videoReferenceCropButton.Name = "_videoReferenceCropButton";
        _videoReferenceCropButton.Location = new Point(278, 240);
        _videoReferenceCropButton.Size = new Size(70, 25);
        _videoReferenceCropButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoReferenceCropButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoReferenceCropButton.Text = "Zone";
        _videoReferenceCropButton.TabIndex = 52;
        _videoReferenceCropButton.Enabled = false;
        _videoReferenceCropButton.Visible = true;
        _videoReferenceCropButton.FlatStyle = FlatStyle.Flat;

        // _videoExtractPromptButton
        _videoExtractPromptButton.Name = "_videoExtractPromptButton";
        _videoExtractPromptButton.Location = new Point(18, 270);
        _videoExtractPromptButton.Size = new Size(150, 29);
        _videoExtractPromptButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoExtractPromptButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoExtractPromptButton.Text = "Extraire prompt (VLM)";
        _videoExtractPromptButton.TabIndex = 53;
        _videoExtractPromptButton.Enabled = false;
        _videoExtractPromptButton.Visible = true;
        _videoExtractPromptButton.FlatStyle = FlatStyle.Flat;

        // _videoReferenceHint
        _videoReferenceHint.Name = "videoReferenceHint";
        _videoReferenceHint.Location = new Point(174, 269);
        _videoReferenceHint.Size = new Size(174, 30);
        _videoReferenceHint.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoReferenceHint.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _videoReferenceHint.Text = "La référence/zone est utilisée avec un modèle I2V. Un modèle T2V l'ignore.";
        _videoReferenceHint.TabIndex = 54;
        _videoReferenceHint.Enabled = false;
        _videoReferenceHint.Visible = true;
        _videoReferenceHint.AutoSize = false;

        // _videoQualityLabel
        _videoQualityLabel.Name = "videoQualityLabel";
        _videoQualityLabel.Location = new Point(730, 17);
        _videoQualityLabel.Size = new Size(46, 26);
        _videoQualityLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoQualityLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoQualityLabel.Text = "Qualité";
        _videoQualityLabel.TabIndex = 56;
        _videoQualityLabel.Enabled = false;
        _videoQualityLabel.Visible = true;
        _videoQualityLabel.AutoSize = false;
        _videoQualityLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoQualityCombo
        _videoQualityCombo.Name = "_videoQualityCombo";
        _videoQualityCombo.Location = new Point(776, 18);
        _videoQualityCombo.Size = new Size(260, 23);
        _videoQualityCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoQualityCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoQualityCombo.TabIndex = 57;
        _videoQualityCombo.Enabled = false;
        _videoQualityCombo.Visible = true;
        _videoQualityCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoQualityCombo.FlatStyle = FlatStyle.Flat;
        _videoQualityCombo.DropDownWidth = 260;

        // _videoLoraLabel
        _videoLoraLabel.Name = "videoLoraLabel";
        _videoLoraLabel.Location = new Point(370, 80);
        _videoLoraLabel.Size = new Size(52, 26);
        _videoLoraLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoLoraLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _videoLoraLabel.Text = "LoRA";
        _videoLoraLabel.TabIndex = 58;
        _videoLoraLabel.Enabled = false;
        _videoLoraLabel.Visible = true;
        _videoLoraLabel.AutoSize = false;
        _videoLoraLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _videoLoraCombo
        _videoLoraCombo.Name = "_videoLoraCombo";
        _videoLoraCombo.Location = new Point(422, 81);
        _videoLoraCombo.Size = new Size(180, 23);
        _videoLoraCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoLoraCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoLoraCombo.TabIndex = 59;
        _videoLoraCombo.Enabled = false;
        _videoLoraCombo.Visible = true;
        _videoLoraCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _videoLoraCombo.FlatStyle = FlatStyle.Flat;
        _videoLoraCombo.DropDownWidth = 620;

        // _videoLoraStrength
        _videoLoraStrength.Name = "_videoLoraStrength";
        _videoLoraStrength.Location = new Point(608, 81);
        _videoLoraStrength.Size = new Size(62, 23);
        _videoLoraStrength.BackColor = Color.FromArgb(255, 18, 20, 24);
        _videoLoraStrength.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _videoLoraStrength.TabIndex = 60;
        _videoLoraStrength.Enabled = false;
        _videoLoraStrength.Visible = true;
        _videoLoraStrength.Minimum = -2M;
        _videoLoraStrength.Maximum = 2M;
        _videoLoraStrength.Increment = 0.05M;
        _videoLoraStrength.DecimalPlaces = 2;

        // _videoLoraAddButton
        _videoLoraAddButton.Name = "_videoLoraAddButton";
        _videoLoraAddButton.Location = new Point(798, 80);
        _videoLoraAddButton.Size = new Size(36, 27);
        _videoLoraAddButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoLoraAddButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoLoraAddButton.Text = "+";
        _videoLoraAddButton.TabIndex = 61;
        _videoLoraAddButton.Enabled = false;
        _videoLoraAddButton.Visible = true;
        _videoLoraAddButton.FlatStyle = FlatStyle.Flat;

        // _videoLoraDownloadButton
        _videoLoraDownloadButton.Name = "_videoLoraDownloadButton";
        _videoLoraDownloadButton.Location = new Point(676, 80);
        _videoLoraDownloadButton.Size = new Size(116, 27);
        _videoLoraDownloadButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoLoraDownloadButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoLoraDownloadButton.Text = "Télécharger";
        _videoLoraDownloadButton.TabIndex = 63;
        _videoLoraDownloadButton.Enabled = false;
        _videoLoraDownloadButton.Visible = true;
        _videoLoraDownloadButton.FlatStyle = FlatStyle.Flat;

        // _videoCatalogDownloadProgress
        _videoCatalogDownloadProgress.Name = "_videoCatalogDownloadProgress";
        _videoCatalogDownloadProgress.Location = new Point(520, 116);
        _videoCatalogDownloadProgress.Size = new Size(275, 16);
        _videoCatalogDownloadProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _videoCatalogDownloadProgress.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoCatalogDownloadProgress.ForeColor = Color.FromArgb(255, 0, 120, 215);
        _videoCatalogDownloadProgress.TabIndex = 64;
        _videoCatalogDownloadProgress.Enabled = false;
        _videoCatalogDownloadProgress.Visible = true;
        _videoCatalogDownloadProgress.Minimum = 0;
        _videoCatalogDownloadProgress.Maximum = 100;
        _videoCatalogDownloadProgress.Style = ProgressBarStyle.Continuous;

        // _videoCatalogDownloadStatus
        _videoCatalogDownloadStatus.Name = "_videoCatalogDownloadStatus";
        _videoCatalogDownloadStatus.Location = new Point(370, 112);
        _videoCatalogDownloadStatus.Size = new Size(145, 24);
        _videoCatalogDownloadStatus.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoCatalogDownloadStatus.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _videoCatalogDownloadStatus.TabIndex = 65;
        _videoCatalogDownloadStatus.Enabled = false;
        _videoCatalogDownloadStatus.Visible = true;
        _videoCatalogDownloadStatus.AutoSize = false;
        _videoCatalogDownloadStatus.AutoEllipsis = true;
        _videoCatalogDownloadStatus.TextAlign = ContentAlignment.MiddleLeft;

        // _videoCatalogDownloadSize
        _videoCatalogDownloadSize.Name = "_videoCatalogDownloadSize";
        _videoCatalogDownloadSize.Location = new Point(800, 112);
        _videoCatalogDownloadSize.Size = new Size(155, 24);
        _videoCatalogDownloadSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _videoCatalogDownloadSize.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoCatalogDownloadSize.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _videoCatalogDownloadSize.Text = "Taille : M 2,84 Go";
        _videoCatalogDownloadSize.TabIndex = 66;
        _videoCatalogDownloadSize.Enabled = false;
        _videoCatalogDownloadSize.Visible = true;
        _videoCatalogDownloadSize.AutoSize = false;
        _videoCatalogDownloadSize.AutoEllipsis = true;
        _videoCatalogDownloadSize.TextAlign = ContentAlignment.MiddleLeft;

        // _videoCatalogDownloadCancelButton
        _videoCatalogDownloadCancelButton.Name = "_videoCatalogDownloadCancelButton";
        _videoCatalogDownloadCancelButton.Location = new Point(960, 112);
        _videoCatalogDownloadCancelButton.Size = new Size(76, 24);
        _videoCatalogDownloadCancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _videoCatalogDownloadCancelButton.BackColor = Color.FromArgb(255, 151, 62, 74);
        _videoCatalogDownloadCancelButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoCatalogDownloadCancelButton.Text = "Annuler";
        _videoCatalogDownloadCancelButton.TabIndex = 67;
        _videoCatalogDownloadCancelButton.Enabled = false;
        _videoCatalogDownloadCancelButton.Visible = true;
        _videoCatalogDownloadCancelButton.FlatStyle = FlatStyle.Flat;

        // _videoImprovePromptButton
        _videoImprovePromptButton.Name = "_videoImprovePromptButton";
        _videoImprovePromptButton.Location = new Point(130, 12);
        _videoImprovePromptButton.Size = new Size(140, 28);
        _videoImprovePromptButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _videoImprovePromptButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoImprovePromptButton.Text = "✨ Améliorer";
        _videoImprovePromptButton.TabIndex = 68;
        _videoImprovePromptButton.Enabled = false;
        _videoImprovePromptButton.Visible = true;
        _videoImprovePromptButton.FlatStyle = FlatStyle.Flat;

        // _videoAutoImprovePrompt
        _videoAutoImprovePrompt.Name = "_videoAutoImprovePrompt";
        _videoAutoImprovePrompt.Location = new Point(276, 15);
        _videoAutoImprovePrompt.Size = new Size(72, 24);
        _videoAutoImprovePrompt.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoAutoImprovePrompt.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoAutoImprovePrompt.Text = "Auto";
        _videoAutoImprovePrompt.TabIndex = 69;
        _videoAutoImprovePrompt.Enabled = false;
        _videoAutoImprovePrompt.Visible = true;
        _videoAutoImprovePrompt.AutoSize = false;

        // _vaeCombo
        _vaeCombo.Name = "Combo";
        _vaeCombo.Location = new Point(470, 207);
        _vaeCombo.Size = new Size(568, 23);
        _vaeCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _vaeCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _vaeCombo.TabIndex = 48;
        _vaeCombo.Visible = true;
        _vaeCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _vaeCombo.FlatStyle = FlatStyle.Flat;
        _vaeCombo.DropDownWidth = 900;

        // _textEncoderCombo
        _textEncoderCombo.Name = "Combo";
        _textEncoderCombo.Location = new Point(470, 174);
        _textEncoderCombo.Size = new Size(568, 23);
        _textEncoderCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _textEncoderCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _textEncoderCombo.TabIndex = 47;
        _textEncoderCombo.Visible = true;
        _textEncoderCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _textEncoderCombo.FlatStyle = FlatStyle.Flat;
        _textEncoderCombo.DropDownWidth = 900;

        // _fluxModelCombo
        _fluxModelCombo.Name = "Combo";
        _fluxModelCombo.Location = new Point(470, 141);
        _fluxModelCombo.Size = new Size(568, 23);
        _fluxModelCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _fluxModelCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _fluxModelCombo.TabIndex = 46;
        _fluxModelCombo.Visible = true;
        _fluxModelCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _fluxModelCombo.FlatStyle = FlatStyle.Flat;
        _fluxModelCombo.DropDownWidth = 900;

        // _visionModelCombo
        _visionModelCombo.Name = "Combo";
        _visionModelCombo.Location = new Point(470, 108);
        _visionModelCombo.Size = new Size(568, 23);
        _visionModelCombo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _visionModelCombo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _visionModelCombo.TabIndex = 45;
        _visionModelCombo.Visible = true;
        _visionModelCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _visionModelCombo.FlatStyle = FlatStyle.Flat;
        _visionModelCombo.DropDownWidth = 900;

        // _cfgVideoModelLabel
        _cfgVideoModelLabel.Name = "cfgVideoModelLabel";
        _cfgVideoModelLabel.Location = new Point(330, 240);
        _cfgVideoModelLabel.Size = new Size(130, 23);
        _cfgVideoModelLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _cfgVideoModelLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _cfgVideoModelLabel.Text = "Modèle Wan";
        _cfgVideoModelLabel.TabIndex = 49;
        _cfgVideoModelLabel.Visible = true;
        _cfgVideoModelLabel.AutoSize = false;
        _cfgVideoModelLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _cfgVideoTextEncoderLabel
        _cfgVideoTextEncoderLabel.Name = "cfgVideoTextEncoderLabel";
        _cfgVideoTextEncoderLabel.Location = new Point(330, 273);
        _cfgVideoTextEncoderLabel.Size = new Size(130, 23);
        _cfgVideoTextEncoderLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _cfgVideoTextEncoderLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _cfgVideoTextEncoderLabel.Text = "Encodeur Wan";
        _cfgVideoTextEncoderLabel.TabIndex = 50;
        _cfgVideoTextEncoderLabel.Visible = true;
        _cfgVideoTextEncoderLabel.AutoSize = false;
        _cfgVideoTextEncoderLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _cfgVideoVaeLabel
        _cfgVideoVaeLabel.Name = "cfgVideoVaeLabel";
        _cfgVideoVaeLabel.Location = new Point(330, 306);
        _cfgVideoVaeLabel.Size = new Size(130, 23);
        _cfgVideoVaeLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _cfgVideoVaeLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _cfgVideoVaeLabel.Text = "Wan VAE";
        _cfgVideoVaeLabel.TabIndex = 51;
        _cfgVideoVaeLabel.Visible = true;
        _cfgVideoVaeLabel.AutoSize = false;
        _cfgVideoVaeLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _cfgVideoModel
        _cfgVideoModel.Name = "_cfgVideoModel";
        _cfgVideoModel.Location = new Point(470, 240);
        _cfgVideoModel.Size = new Size(568, 23);
        _cfgVideoModel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _cfgVideoModel.BackColor = Color.FromArgb(255, 18, 20, 24);
        _cfgVideoModel.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _cfgVideoModel.TabIndex = 52;
        _cfgVideoModel.Visible = true;
        _cfgVideoModel.BorderStyle = BorderStyle.FixedSingle;

        // _cfgVideoTextEncoder
        _cfgVideoTextEncoder.Name = "_cfgVideoTextEncoder";
        _cfgVideoTextEncoder.Location = new Point(470, 273);
        _cfgVideoTextEncoder.Size = new Size(568, 23);
        _cfgVideoTextEncoder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _cfgVideoTextEncoder.BackColor = Color.FromArgb(255, 18, 20, 24);
        _cfgVideoTextEncoder.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _cfgVideoTextEncoder.TabIndex = 53;
        _cfgVideoTextEncoder.Visible = true;
        _cfgVideoTextEncoder.BorderStyle = BorderStyle.FixedSingle;

        // _cfgVideoVae
        _cfgVideoVae.Name = "_cfgVideoVae";
        _cfgVideoVae.Location = new Point(470, 306);
        _cfgVideoVae.Size = new Size(568, 23);
        _cfgVideoVae.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _cfgVideoVae.BackColor = Color.FromArgb(255, 18, 20, 24);
        _cfgVideoVae.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _cfgVideoVae.TabIndex = 54;
        _cfgVideoVae.Visible = true;
        _cfgVideoVae.BorderStyle = BorderStyle.FixedSingle;

        // _cfgVideoClipVisionLabel
        _cfgVideoClipVisionLabel.Name = "cfgVideoClipVisionLabel";
        _cfgVideoClipVisionLabel.Location = new Point(18, 273);
        _cfgVideoClipVisionLabel.Size = new Size(120, 23);
        _cfgVideoClipVisionLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _cfgVideoClipVisionLabel.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _cfgVideoClipVisionLabel.Text = "CLIP Vision Wan";
        _cfgVideoClipVisionLabel.TabIndex = 55;
        _cfgVideoClipVisionLabel.Visible = true;
        _cfgVideoClipVisionLabel.AutoSize = false;
        _cfgVideoClipVisionLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _cfgVideoClipVision
        _cfgVideoClipVision.Name = "_cfgVideoClipVision";
        _cfgVideoClipVision.Location = new Point(150, 273);
        _cfgVideoClipVision.Size = new Size(160, 23);
        _cfgVideoClipVision.BackColor = Color.FromArgb(255, 18, 20, 24);
        _cfgVideoClipVision.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _cfgVideoClipVision.TabIndex = 56;
        _cfgVideoClipVision.Visible = true;
        _cfgVideoClipVision.BorderStyle = BorderStyle.FixedSingle;

        // _autoSaveConfigurationCheck
        _autoSaveConfigurationCheck.Name = "_autoSaveConfigurationCheck";
        _autoSaveConfigurationCheck.Location = new Point(470, 446);
        _autoSaveConfigurationCheck.Size = new Size(568, 24);
        _autoSaveConfigurationCheck.BackColor = Color.FromArgb(255, 24, 26, 31);
        _autoSaveConfigurationCheck.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _autoSaveConfigurationCheck.Text = "Enregistrer automatiquement après validation";
        _autoSaveConfigurationCheck.TabIndex = 57;
        _autoSaveConfigurationCheck.Visible = true;
        _autoSaveConfigurationCheck.AutoSize = false;

        // _configurationSaveStatus
        _configurationSaveStatus.Name = "_configurationSaveStatus";
        _configurationSaveStatus.Location = new Point(330, 84);
        _configurationSaveStatus.Size = new Size(708, 20);
        _configurationSaveStatus.BackColor = Color.FromArgb(255, 24, 26, 31);
        _configurationSaveStatus.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _configurationSaveStatus.TabIndex = 58;
        _configurationSaveStatus.Visible = true;
        _configurationSaveStatus.AutoSize = false;
        _configurationSaveStatus.AutoEllipsis = true;
        _configurationSaveStatus.TextAlign = ContentAlignment.MiddleLeft;

        // _installImageModelsButton
        _installImageModelsButton.Name = "installImageModelsButton";
        _installImageModelsButton.Location = new Point(382, 118);
        _installImageModelsButton.Size = new Size(184, 34);
        _installImageModelsButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _installImageModelsButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _installImageModelsButton.Text = "Installer / réparer Image";
        _installImageModelsButton.TabIndex = 7;
        _installImageModelsButton.Visible = true;
        _installImageModelsButton.FlatStyle = FlatStyle.Flat;

        // _installVideoModelsButton
        _installVideoModelsButton.Name = "installVideoModelsButton";
        _installVideoModelsButton.Location = new Point(578, 118);
        _installVideoModelsButton.Size = new Size(184, 34);
        _installVideoModelsButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _installVideoModelsButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _installVideoModelsButton.Text = "Installer / réparer Vidéo";
        _installVideoModelsButton.TabIndex = 8;
        _installVideoModelsButton.Visible = true;
        _installVideoModelsButton.FlatStyle = FlatStyle.Flat;

        // _installVideoModelsStatus
        _installVideoModelsStatus.Name = "_installVideoModelsStatus";
        _installVideoModelsStatus.Location = new Point(18, 160);
        _installVideoModelsStatus.Size = new Size(1020, 30);
        _installVideoModelsStatus.BackColor = Color.FromArgb(255, 34, 37, 44);
        _installVideoModelsStatus.ForeColor = Color.FromArgb(255, 171, 128, 44);
        _installVideoModelsStatus.Text = "Wan vidéo : 3 modèle(s) manquant(s)";
        _installVideoModelsStatus.TabIndex = 9;
        _installVideoModelsStatus.Padding = new Padding(8, 0, 8, 0);
        _installVideoModelsStatus.Visible = true;
        _installVideoModelsStatus.AutoSize = false;
        _installVideoModelsStatus.AutoEllipsis = true;
        _installVideoModelsStatus.TextAlign = ContentAlignment.MiddleLeft;

        // _installComponentsStatus
        _installComponentsStatus.Name = "installComponentsStatus";
        _installComponentsStatus.Location = new Point(18, 264);
        _installComponentsStatus.Size = new Size(1020, 118);
        _installComponentsStatus.BackColor = Color.FromArgb(255, 34, 37, 44);
        _installComponentsStatus.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _installComponentsStatus.TabIndex = 10;
        _installComponentsStatus.Visible = true;
        _installComponentsStatus.Multiline = true;
        _installComponentsStatus.ReadOnly = true;
        _installComponentsStatus.ScrollBars = ScrollBars.Vertical;
        _installComponentsStatus.BorderStyle = BorderStyle.FixedSingle;

        // _comfyStatusPanel
        _comfyStatusPanel.Name = "_comfyStatusPanel";
        _comfyStatusPanel.Location = new Point(3, 3);
        _comfyStatusPanel.Size = new Size(1050, 639);
        _comfyStatusPanel.Dock = DockStyle.Fill;
        _comfyStatusPanel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _comfyStatusPanel.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _comfyStatusPanel.TabIndex = 1;
        _comfyStatusPanel.Enabled = false;
        _comfyStatusPanel.Visible = true;

        // _comfyStatusCard
        _comfyStatusCard.Name = "comfyStatusCard";
        _comfyStatusCard.Location = new Point(215, 209);
        _comfyStatusCard.Size = new Size(620, 220);
        _comfyStatusCard.Anchor = AnchorStyles.None;
        _comfyStatusCard.BackColor = Color.FromArgb(255, 34, 37, 44);
        _comfyStatusCard.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _comfyStatusCard.TabIndex = 0;
        _comfyStatusCard.Enabled = false;
        _comfyStatusCard.Visible = true;

        // _comfyStatusTitle
        _comfyStatusTitle.Name = "comfyStatusTitle";
        _comfyStatusTitle.Location = new Point(28, 24);
        _comfyStatusTitle.Size = new Size(560, 34);
        _comfyStatusTitle.BackColor = Color.FromArgb(255, 34, 37, 44);
        _comfyStatusTitle.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _comfyStatusTitle.Text = "ComfyUI";
        _comfyStatusTitle.TabIndex = 0;
        _comfyStatusTitle.Enabled = false;
        _comfyStatusTitle.Visible = true;
        _comfyStatusTitle.AutoSize = false;

        // _comfyStatusMessage
        _comfyStatusMessage.Name = "comfyStatusMessage";
        _comfyStatusMessage.Location = new Point(28, 70);
        _comfyStatusMessage.Size = new Size(560, 74);
        _comfyStatusMessage.BackColor = Color.FromArgb(255, 34, 37, 44);
        _comfyStatusMessage.ForeColor = Color.FromArgb(255, 196, 202, 214);
        _comfyStatusMessage.Text = "Vérification du serveur local ComfyUI…";
        _comfyStatusMessage.TabIndex = 1;
        _comfyStatusMessage.Enabled = false;
        _comfyStatusMessage.Visible = true;
        _comfyStatusMessage.AutoSize = false;

        // _comfyStatusRetryButton
        _comfyStatusRetryButton.Name = "comfyStatusRetryButton";
        _comfyStatusRetryButton.Location = new Point(28, 156);
        _comfyStatusRetryButton.Size = new Size(138, 36);
        _comfyStatusRetryButton.BackColor = Color.FromArgb(255, 52, 113, 181);
        _comfyStatusRetryButton.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _comfyStatusRetryButton.Text = "Réessayer";
        _comfyStatusRetryButton.TabIndex = 2;
        _comfyStatusRetryButton.Enabled = false;
        _comfyStatusRetryButton.Visible = true;
        _comfyStatusRetryButton.FlatStyle = FlatStyle.Flat;

        // _comfyStatusPortLabel
        _comfyStatusPortLabel.Name = "comfyStatusPortLabel";
        _comfyStatusPortLabel.Location = new Point(188, 163);
        _comfyStatusPortLabel.Size = new Size(370, 24);
        _comfyStatusPortLabel.BackColor = Color.FromArgb(255, 34, 37, 44);
        _comfyStatusPortLabel.ForeColor = Color.FromArgb(255, 145, 153, 169);
        _comfyStatusPortLabel.TabIndex = 3;
        _comfyStatusPortLabel.Enabled = false;
        _comfyStatusPortLabel.Visible = true;
        _comfyStatusPortLabel.AutoSize = false;

        // _logTabs
        _logTabs.Name = "_logTabs";
        _logTabs.Location = new Point(8, 8);
        _logTabs.Size = new Size(1040, 629);
        _logTabs.Dock = DockStyle.Fill;
        _logTabs.SizeMode = TabSizeMode.Fixed;
        _logTabs.ItemSize = new Size(92, 24);
        _logTabs.BackColor = Color.FromArgb(255, 240, 240, 240);
        _logTabs.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logTabs.TabIndex = 0;
        _logTabs.Visible = true;

        // _logPageAll
        _logPageAll.Name = "logPageAll";
        _logPageAll.Location = new Point(0, 0);
        _logPageAll.Size = new Size(200, 100);
        _logPageAll.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageAll.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageAll.Text = "Tous";
        _logPageAll.TabIndex = 0;
        _logPageAll.Padding = new Padding(6, 6, 6, 6);
        _logPageAll.Visible = true;

        // _allLog
        _allLog.Name = "_allLog";
        _allLog.Location = new Point(6, 6);
        _allLog.Size = new Size(188, 88);
        _allLog.Dock = DockStyle.Fill;
        _allLog.BackColor = Color.FromArgb(255, 18, 20, 24);
        _allLog.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _allLog.TabIndex = 0;
        _allLog.Visible = true;
        _allLog.ReadOnly = true;
        _allLog.DetectUrls = false;
        _allLog.BorderStyle = BorderStyle.FixedSingle;

        // _logPageUi
        _logPageUi.Name = "logPageUi";
        _logPageUi.Location = new Point(0, 0);
        _logPageUi.Size = new Size(200, 100);
        _logPageUi.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageUi.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageUi.Text = "UI";
        _logPageUi.TabIndex = 1;
        _logPageUi.Padding = new Padding(6, 6, 6, 6);
        _logPageUi.Visible = true;

        // _logUi
        _logUi.Name = "logUi";
        _logUi.Location = new Point(6, 6);
        _logUi.Size = new Size(188, 88);
        _logUi.Dock = DockStyle.Fill;
        _logUi.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logUi.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logUi.TabIndex = 0;
        _logUi.Visible = true;
        _logUi.ReadOnly = true;
        _logUi.WordWrap = false;
        _logUi.DetectUrls = false;
        _logUi.BorderStyle = BorderStyle.FixedSingle;

        // _logPageOpenCode
        _logPageOpenCode.Name = "logPageOpenCode";
        _logPageOpenCode.Location = new Point(0, 0);
        _logPageOpenCode.Size = new Size(200, 100);
        _logPageOpenCode.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageOpenCode.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageOpenCode.Text = "OpenCode";
        _logPageOpenCode.TabIndex = 2;
        _logPageOpenCode.Padding = new Padding(6, 6, 6, 6);
        _logPageOpenCode.Visible = true;

        // _logOpenCode
        _logOpenCode.Name = "logOpenCode";
        _logOpenCode.Location = new Point(6, 6);
        _logOpenCode.Size = new Size(188, 88);
        _logOpenCode.Dock = DockStyle.Fill;
        _logOpenCode.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logOpenCode.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logOpenCode.TabIndex = 0;
        _logOpenCode.Visible = true;
        _logOpenCode.ReadOnly = true;
        _logOpenCode.WordWrap = false;
        _logOpenCode.DetectUrls = false;
        _logOpenCode.BorderStyle = BorderStyle.FixedSingle;

        // _logPageOllama
        _logPageOllama.Name = "logPageOllama";
        _logPageOllama.Location = new Point(0, 0);
        _logPageOllama.Size = new Size(200, 100);
        _logPageOllama.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageOllama.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageOllama.Text = "Ollama";
        _logPageOllama.TabIndex = 3;
        _logPageOllama.Padding = new Padding(6, 6, 6, 6);
        _logPageOllama.Visible = true;

        // _logOllama
        _logOllama.Name = "logOllama";
        _logOllama.Location = new Point(6, 6);
        _logOllama.Size = new Size(188, 88);
        _logOllama.Dock = DockStyle.Fill;
        _logOllama.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logOllama.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logOllama.TabIndex = 0;
        _logOllama.Visible = true;
        _logOllama.ReadOnly = true;
        _logOllama.WordWrap = false;
        _logOllama.DetectUrls = false;
        _logOllama.BorderStyle = BorderStyle.FixedSingle;

        // _logPageComfy
        _logPageComfy.Name = "logPageComfy";
        _logPageComfy.Location = new Point(0, 0);
        _logPageComfy.Size = new Size(200, 100);
        _logPageComfy.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageComfy.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageComfy.Text = "ComfyUI";
        _logPageComfy.TabIndex = 4;
        _logPageComfy.Padding = new Padding(6, 6, 6, 6);
        _logPageComfy.Visible = true;

        // _logComfy
        _logComfy.Name = "logComfy";
        _logComfy.Location = new Point(6, 6);
        _logComfy.Size = new Size(188, 88);
        _logComfy.Dock = DockStyle.Fill;
        _logComfy.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logComfy.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logComfy.TabIndex = 0;
        _logComfy.Visible = true;
        _logComfy.ReadOnly = true;
        _logComfy.WordWrap = false;
        _logComfy.DetectUrls = false;
        _logComfy.BorderStyle = BorderStyle.FixedSingle;

        // _logPageFlux
        _logPageFlux.Name = "logPageFlux";
        _logPageFlux.Location = new Point(0, 0);
        _logPageFlux.Size = new Size(200, 100);
        _logPageFlux.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageFlux.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageFlux.Text = "FLUX.2";
        _logPageFlux.TabIndex = 5;
        _logPageFlux.Padding = new Padding(6, 6, 6, 6);
        _logPageFlux.Visible = true;

        // _logFlux
        _logFlux.Name = "logFlux";
        _logFlux.Location = new Point(6, 6);
        _logFlux.Size = new Size(188, 88);
        _logFlux.Dock = DockStyle.Fill;
        _logFlux.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logFlux.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logFlux.TabIndex = 0;
        _logFlux.Visible = true;
        _logFlux.ReadOnly = true;
        _logFlux.WordWrap = false;
        _logFlux.DetectUrls = false;
        _logFlux.BorderStyle = BorderStyle.FixedSingle;

        // _logPageVideo
        _logPageVideo.Name = "logPageVideo";
        _logPageVideo.Location = new Point(0, 0);
        _logPageVideo.Size = new Size(200, 100);
        _logPageVideo.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageVideo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageVideo.Text = "Vidéo";
        _logPageVideo.TabIndex = 6;
        _logPageVideo.Padding = new Padding(6, 6, 6, 6);
        _logPageVideo.Visible = true;

        // _logVideo
        _logVideo.Name = "logVideo";
        _logVideo.Location = new Point(6, 6);
        _logVideo.Size = new Size(188, 88);
        _logVideo.Dock = DockStyle.Fill;
        _logVideo.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logVideo.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logVideo.TabIndex = 0;
        _logVideo.Visible = true;
        _logVideo.ReadOnly = true;
        _logVideo.WordWrap = false;
        _logVideo.DetectUrls = false;
        _logVideo.BorderStyle = BorderStyle.FixedSingle;

        // _logPageInstall
        _logPageInstall.Name = "logPageInstall";
        _logPageInstall.Location = new Point(0, 0);
        _logPageInstall.Size = new Size(200, 100);
        _logPageInstall.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageInstall.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageInstall.Text = "Installation";
        _logPageInstall.TabIndex = 7;
        _logPageInstall.Padding = new Padding(6, 6, 6, 6);
        _logPageInstall.Visible = true;

        // _logInstall
        _logInstall.Name = "logInstall";
        _logInstall.Location = new Point(6, 6);
        _logInstall.Size = new Size(188, 88);
        _logInstall.Dock = DockStyle.Fill;
        _logInstall.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logInstall.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logInstall.TabIndex = 0;
        _logInstall.Visible = true;
        _logInstall.ReadOnly = true;
        _logInstall.WordWrap = false;
        _logInstall.DetectUrls = false;
        _logInstall.BorderStyle = BorderStyle.FixedSingle;

        // _logPageSystem
        _logPageSystem.Name = "logPageSystem";
        _logPageSystem.Location = new Point(0, 0);
        _logPageSystem.Size = new Size(200, 100);
        _logPageSystem.BackColor = Color.FromArgb(255, 24, 26, 31);
        _logPageSystem.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logPageSystem.Text = "Système";
        _logPageSystem.TabIndex = 8;
        _logPageSystem.Padding = new Padding(6, 6, 6, 6);
        _logPageSystem.Visible = true;

        // _logSystem
        _logSystem.Name = "logSystem";
        _logSystem.Location = new Point(6, 6);
        _logSystem.Size = new Size(188, 88);
        _logSystem.Dock = DockStyle.Fill;
        _logSystem.BackColor = Color.FromArgb(255, 18, 20, 24);
        _logSystem.ForeColor = Color.FromArgb(255, 242, 244, 248);
        _logSystem.TabIndex = 0;
        _logSystem.Visible = true;
        _logSystem.ReadOnly = true;
        _logSystem.WordWrap = false;
        _logSystem.DetectUrls = false;
        _logSystem.BorderStyle = BorderStyle.FixedSingle;

        // _videoWidthLabel (designer-owned caption)
        _videoWidthLabel.Name = "_videoWidthLabel";
        _videoWidthLabel.Location = new Point(18, 307);
        _videoWidthLabel.Size = new Size(64, 23);
        _videoWidthLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoWidthLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoWidthLabel.Text = "Largeur";
        _videoWidthLabel.AutoSize = false;

        // _videoHeightLabel (designer-owned caption)
        _videoHeightLabel.Name = "_videoHeightLabel";
        _videoHeightLabel.Location = new Point(174, 307);
        _videoHeightLabel.Size = new Size(70, 23);
        _videoHeightLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoHeightLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoHeightLabel.Text = "Hauteur";
        _videoHeightLabel.AutoSize = false;

        // _videoFramesLabel (designer-owned caption)
        _videoFramesLabel.Name = "_videoFramesLabel";
        _videoFramesLabel.Location = new Point(142, 342);
        _videoFramesLabel.Size = new Size(48, 23);
        _videoFramesLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoFramesLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoFramesLabel.Text = "Frames";
        _videoFramesLabel.AutoSize = false;

        // _videoFpsLabel (designer-owned caption)
        _videoFpsLabel.Name = "_videoFpsLabel";
        _videoFpsLabel.Location = new Point(248, 342);
        _videoFpsLabel.Size = new Size(30, 23);
        _videoFpsLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoFpsLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoFpsLabel.Text = "FPS";
        _videoFpsLabel.AutoSize = false;

        // _videoStepsLabel (designer-owned caption)
        _videoStepsLabel.Name = "_videoStepsLabel";
        _videoStepsLabel.Location = new Point(18, 377);
        _videoStepsLabel.Size = new Size(64, 23);
        _videoStepsLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoStepsLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoStepsLabel.Text = "Steps";
        _videoStepsLabel.AutoSize = false;

        // _videoModelTitleLabel (designer-owned caption)
        _videoModelTitleLabel.Name = "_videoModelTitleLabel";
        _videoModelTitleLabel.Location = new Point(448, 18);
        _videoModelTitleLabel.Size = new Size(570, 23);
        _videoModelTitleLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoModelTitleLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoModelTitleLabel.Text = "Modèles vidéo Wan 2.1";
        _videoModelTitleLabel.AutoSize = false;

        // _videoNoteLabel (designer-owned caption)
        _videoNoteLabel.Name = "_videoNoteLabel";
        _videoNoteLabel.Location = new Point(448, 410);
        _videoNoteLabel.Size = new Size(570, 70);
        _videoNoteLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoNoteLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoNoteLabel.Text = "Moteur local prévu : Wan 2.1 T2V 1.3B via ComfyUI. Aucun téléchargement de plusieurs Go n'est lancé automatiquement.";
        _videoNoteLabel.AutoSize = false;

        // _videoCfgLabel (designer-owned caption)
        _videoCfgLabel.Name = "_videoCfgLabel";
        _videoCfgLabel.Location = new Point(174, 377);
        _videoCfgLabel.Size = new Size(70, 23);
        _videoCfgLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoCfgLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoCfgLabel.Text = "CFG vidéo";
        _videoCfgLabel.AutoSize = false;

        // _videoShiftLabel (designer-owned caption)
        _videoShiftLabel.Name = "_videoShiftLabel";
        _videoShiftLabel.Location = new Point(18, 412);
        _videoShiftLabel.Size = new Size(64, 23);
        _videoShiftLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoShiftLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoShiftLabel.Text = "Shift";
        _videoShiftLabel.AutoSize = false;

        // _videoSamplerLabel (designer-owned caption)
        _videoSamplerLabel.Name = "_videoSamplerLabel";
        _videoSamplerLabel.Location = new Point(174, 412);
        _videoSamplerLabel.Size = new Size(70, 23);
        _videoSamplerLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoSamplerLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoSamplerLabel.Text = "Sampler";
        _videoSamplerLabel.AutoSize = false;

        // _videoSchedulerLabel (designer-owned caption)
        _videoSchedulerLabel.Name = "_videoSchedulerLabel";
        _videoSchedulerLabel.Location = new Point(18, 447);
        _videoSchedulerLabel.Size = new Size(64, 23);
        _videoSchedulerLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoSchedulerLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoSchedulerLabel.Text = "Scheduler";
        _videoSchedulerLabel.AutoSize = false;

        // _videoSeedLabel (designer-owned caption)
        _videoSeedLabel.Name = "_videoSeedLabel";
        _videoSeedLabel.Location = new Point(174, 447);
        _videoSeedLabel.Size = new Size(70, 23);
        _videoSeedLabel.BackColor = Color.FromArgb(255, 24, 26, 31);
        _videoSeedLabel.ForeColor = Color.FromArgb(255, 255, 255, 255);
        _videoSeedLabel.Text = "Seed vidéo";
        _videoSeedLabel.AutoSize = false;

        // Canonical parent hierarchy
        _tabVideo.Controls.Add(_videoWidthLabel);
        _tabVideo.Controls.Add(_videoHeightLabel);
        _tabVideo.Controls.Add(_videoFramesLabel);
        _tabVideo.Controls.Add(_videoFpsLabel);
        _tabVideo.Controls.Add(_videoStepsLabel);
        _tabVideo.Controls.Add(_videoModelTitleLabel);
        _tabVideo.Controls.Add(_videoNoteLabel);
        _tabVideo.Controls.Add(_videoCfgLabel);
        _tabVideo.Controls.Add(_videoShiftLabel);
        _tabVideo.Controls.Add(_videoSamplerLabel);
        _tabVideo.Controls.Add(_videoSchedulerLabel);
        _tabVideo.Controls.Add(_videoSeedLabel);

        tabGenerate.Controls.Add(_imageModelDownloadButton);
        tabGenerate.Controls.Add(_benchmarkButton);
        tabGenerate.Controls.Add(_randomSeedCheck);
        tabGenerate.Controls.Add(_seedInput);
        tabGenerate.Controls.Add(_seedLabel);
        tabGenerate.Controls.Add(_promptModelCombo);
        tabGenerate.Controls.Add(_promptModelLabel);
        tabGenerate.Controls.Add(_autoImprovePrompt);
        tabGenerate.Controls.Add(_improvePromptButton);
        tabGenerate.Controls.Add(_negativePrompt);
        tabGenerate.Controls.Add(_negativePromptLabel);
        tabGenerate.Controls.Add(_imageStyleTemplateLabel);
        tabGenerate.Controls.Add(_imageStyleTemplateCombo);
        tabGenerate.Controls.Add(_imageNegativeTemplateLabel);
        tabGenerate.Controls.Add(_imageNegativeTemplateCombo);
        tabGenerate.Controls.Add(_imageHistoryLabel);
        tabGenerate.Controls.Add(_imageHistoryPanel);
        tabGenerate.Controls.Add(lblCfgWidth);
        tabGenerate.Controls.Add(numDefaultWidth);
        tabGenerate.Controls.Add(lblCfgHeight);
        tabGenerate.Controls.Add(numDefaultHeight);
        tabGenerate.Controls.Add(lblCfgSteps);
        tabGenerate.Controls.Add(numDefaultSteps);
        tabGenerate.Controls.Add(_imageCfgLabel);
        tabGenerate.Controls.Add(_imageCfg);
        tabGenerate.Controls.Add(_imageMaxQualitySharpnessLabel);
        tabGenerate.Controls.Add(_imageMaxQualitySharpness);
        tabGenerate.Controls.Add(_imageSharpnessPreviewButton);
        tabGenerate.Controls.Add(_imagePreviewViewport);
        _imagePreviewViewport.Controls.Add(_preview);
        _imagePreviewViewport.Controls.Add(_imageSharpnessBeforeLabel);
        _imagePreviewViewport.Controls.Add(_imageSharpnessAfterLabel);
        _imagePreviewViewport.Controls.Add(_imageSharpnessBeforePreview);
        _imagePreviewViewport.Controls.Add(_imageSharpnessAfterPreview);
        _imagePreviewViewport.Controls.Add(_imageSharpnessComparisonPanel);
        _imagePreviewViewport.Controls.Add(_imageSharpnessComparisonSlider);
        tabGenerate.Controls.Add(_imageModelRuntimeLabel);
        tabGenerate.Controls.Add(_imageModelRuntimeCombo);
        tabGenerate.Controls.Add(_imageImportModelButton);
        tabGenerate.Controls.Add(_imageExtractPromptButton);
        tabGenerate.Controls.Add(_imageLoraLabel);
        tabGenerate.Controls.Add(_imageLoraCombo);
        tabGenerate.Controls.Add(_imageLoraStrength);
        tabGenerate.Controls.Add(_imageLoraAddButton);
        tabGenerate.Controls.Add(_imageLoraDownloadButton);
        tabGenerate.Controls.Add(_imageCatalogDownloadProgress);
        tabGenerate.Controls.Add(_imageCatalogDownloadStatus);
        tabGenerate.Controls.Add(_imageCatalogDownloadSize);
        tabGenerate.Controls.Add(_imageCatalogDownloadCancelButton);
        _tabVideo.Controls.Add(_videoModelDownloadButton);
        _tabVideo.Controls.Add(_videoPreviewWeb);
        _tabVideo.Controls.Add(_videoPromptLabel);
        _tabVideo.Controls.Add(_videoPrompt);
        _tabVideo.Controls.Add(_videoNegativeLabel);
        _tabVideo.Controls.Add(_videoNegative);
        _tabVideo.Controls.Add(_videoWidth);
        _tabVideo.Controls.Add(_videoHeight);
        _tabVideo.Controls.Add(_videoFrames);
        _tabVideo.Controls.Add(_videoFps);
        _tabVideo.Controls.Add(_videoDurationLabel);
        _tabVideo.Controls.Add(_videoDurationSeconds);
        _tabVideo.Controls.Add(_videoSteps);
        _tabVideo.Controls.Add(_videoGenerateButton);
        _tabVideo.Controls.Add(_videoCancelButton);
        _tabVideo.Controls.Add(_videoRefreshButton);
        _tabVideo.Controls.Add(_videoProgress);
        _tabVideo.Controls.Add(_videoStatus);
        _tabVideo.Controls.Add(_videoModelStatus);
        _tabVideo.Controls.Add(_videoOutputLabel);
        _tabVideo.Controls.Add(_videoOutput);
        _tabVideo.Controls.Add(_videoOpenButton);
        _tabVideo.Controls.Add(_videoStyleTemplateLabel);
        _tabVideo.Controls.Add(_videoStyleTemplateCombo);
        _tabVideo.Controls.Add(_videoNegativeTemplateLabel);
        _tabVideo.Controls.Add(_videoNegativeTemplateCombo);
        _tabVideo.Controls.Add(_videoHistoryLabel);
        _tabVideo.Controls.Add(_videoHistoryPanel);
        _tabVideo.Controls.Add(_videoCfg);
        _tabVideo.Controls.Add(_videoMaxQualitySharpnessLabel);
        _tabVideo.Controls.Add(_videoMaxQualitySharpness);
        _tabVideo.Controls.Add(_videoSharpnessPreviewButton);
        _tabVideo.Controls.Add(_videoSharpnessPreviewPanel);
        _videoSharpnessPreviewPanel.Controls.Add(_videoSharpnessBeforeLabel);
        _videoSharpnessPreviewPanel.Controls.Add(_videoSharpnessAfterLabel);
        _videoSharpnessPreviewPanel.Controls.Add(_videoSharpnessBeforePreview);
        _videoSharpnessPreviewPanel.Controls.Add(_videoSharpnessAfterPreview);
        _tabVideo.Controls.Add(_videoSamplingShift);
        _tabVideo.Controls.Add(_videoSampler);
        _tabVideo.Controls.Add(_videoScheduler);
        _tabVideo.Controls.Add(_videoSeed);
        _tabVideo.Controls.Add(_videoRandomSeed);
        _tabVideo.Controls.Add(_videoQualityHint);
        _tabVideo.Controls.Add(_videoModelRuntimeLabel);
        _tabVideo.Controls.Add(_videoModelRuntimeCombo);
        _tabVideo.Controls.Add(_videoImportModelButton);
        _tabVideo.Controls.Add(_videoReferenceLabel);
        _tabVideo.Controls.Add(_videoReferenceImage);
        _tabVideo.Controls.Add(_videoReferenceBrowseButton);
        _tabVideo.Controls.Add(_videoReferenceCropButton);
        _tabVideo.Controls.Add(_videoExtractPromptButton);
        _tabVideo.Controls.Add(_videoReferenceHint);
        _tabVideo.Controls.Add(_videoQualityLabel);
        _tabVideo.Controls.Add(_videoQualityCombo);
        _tabVideo.Controls.Add(_videoLoraLabel);
        _tabVideo.Controls.Add(_videoLoraCombo);
        _tabVideo.Controls.Add(_videoLoraStrength);
        _tabVideo.Controls.Add(_videoLoraAddButton);
        _tabVideo.Controls.Add(_videoLoraDownloadButton);
        _tabVideo.Controls.Add(_videoCatalogDownloadProgress);
        _tabVideo.Controls.Add(_videoCatalogDownloadStatus);
        _tabVideo.Controls.Add(_videoCatalogDownloadSize);
        _tabVideo.Controls.Add(_videoCatalogDownloadCancelButton);
        _tabVideo.Controls.Add(_videoImprovePromptButton);
        _tabVideo.Controls.Add(_videoAutoImprovePrompt);
        tabConfiguration.Controls.Add(_vaeCombo);
        tabConfiguration.Controls.Add(_textEncoderCombo);
        tabConfiguration.Controls.Add(_fluxModelCombo);
        tabConfiguration.Controls.Add(_visionModelCombo);
        tabConfiguration.Controls.Add(_cfgVideoModelLabel);
        tabConfiguration.Controls.Add(_cfgVideoTextEncoderLabel);
        tabConfiguration.Controls.Add(_cfgVideoVaeLabel);
        tabConfiguration.Controls.Add(_cfgVideoModel);
        tabConfiguration.Controls.Add(_cfgVideoTextEncoder);
        tabConfiguration.Controls.Add(_cfgVideoVae);
        tabConfiguration.Controls.Add(_cfgVideoClipVisionLabel);
        tabConfiguration.Controls.Add(_cfgVideoClipVision);
        tabConfiguration.Controls.Add(_autoSaveConfigurationCheck);
        tabConfiguration.Controls.Add(_configurationSaveStatus);
        tabInstallation.Controls.Add(_installImageModelsButton);
        tabInstallation.Controls.Add(_installVideoModelsButton);
        tabInstallation.Controls.Add(_installVideoModelsStatus);
        tabInstallation.Controls.Add(_installComponentsStatus);
        tabComfy.Controls.Add(_comfyStatusPanel);
        _comfyStatusPanel.Controls.Add(_comfyStatusCard);
        _comfyStatusCard.Controls.Add(_comfyStatusTitle);
        _comfyStatusCard.Controls.Add(_comfyStatusMessage);
        _comfyStatusCard.Controls.Add(_comfyStatusRetryButton);
        _comfyStatusCard.Controls.Add(_comfyStatusPortLabel);
        _logTabs.TabPages.Add(_logPageAll);
        _logPageAll.Controls.Add(_allLog);
        _logTabs.TabPages.Add(_logPageUi);
        _logPageUi.Controls.Add(_logUi);
        _logTabs.TabPages.Add(_logPageOpenCode);
        _logPageOpenCode.Controls.Add(_logOpenCode);
        _logTabs.TabPages.Add(_logPageOllama);
        _logPageOllama.Controls.Add(_logOllama);
        _logTabs.TabPages.Add(_logPageComfy);
        _logPageComfy.Controls.Add(_logComfy);
        _logTabs.TabPages.Add(_logPageFlux);
        _logPageFlux.Controls.Add(_logFlux);
        _logTabs.TabPages.Add(_logPageVideo);
        _logPageVideo.Controls.Add(_logVideo);
        _logTabs.TabPages.Add(_logPageInstall);
        _logPageInstall.Controls.Add(_logInstall);
        _logTabs.TabPages.Add(_logPageSystem);
        _logPageSystem.Controls.Add(_logSystem);
        tabLogs.Controls.Add(_logTabs);
        _tabs.TabPages.Clear();
        _tabs.TabPages.AddRange(new TabPage[] { tabDashboard, tabGenerate, _tabVideo, tabConfiguration, tabInstallation, tabOpenCode, tabComfy, tabOllama, tabLogs, tabAbout });
        _tabs.ItemSize = new Size(103, 28);
        txtVisionModel.Visible = false;
        txtFluxModel.Visible = false;
        txtTextEncoder.Visible = false;
        txtVae.Visible = false;


        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(24, 26, 31);
        DoubleBuffered = true;
        ClientSize = new Size(1064, 681);
        Controls.Add(_tabs);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ForeColor = Color.FromArgb(242, 244, 248);
        MinimumSize = new Size(900, 620);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "DreamRaster";
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;

        ((System.ComponentModel.ISupportInitialize)_web).EndInit();
        ((System.ComponentModel.ISupportInitialize)_comfyWeb).EndInit();
        ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
        ((System.ComponentModel.ISupportInitialize)numImg2ImgStrength).EndInit();
        ((System.ComponentModel.ISupportInitialize)picAboutIcon).EndInit();
        ((System.ComponentModel.ISupportInitialize)numOpenCodePort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numOllamaPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numComfyPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numProxyPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numApiPort).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultWidth).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultHeight).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDefaultSteps).EndInit();
        ((System.ComponentModel.ISupportInitialize)numSafeVram).EndInit();
        ((System.ComponentModel.ISupportInitialize)numSafeRam).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDownloadConnections).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDownloadBuffer).EndInit();

        _tabs.ResumeLayout(false);
        tabDashboard.ResumeLayout(false);
        tabDashboard.PerformLayout();
        tabOpenCode.ResumeLayout(false);
        tabComfy.ResumeLayout(false);
        tabOllama.ResumeLayout(false);
        tabGenerate.ResumeLayout(false);
        tabGenerate.PerformLayout();
        tabInstallation.ResumeLayout(false);
        tabConfiguration.ResumeLayout(false);
        tabConfiguration.PerformLayout();
        tabLogs.ResumeLayout(false);
        tabAbout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
