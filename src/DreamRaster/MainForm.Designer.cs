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
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
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
        btnBrowseInputImage.Location = new Point(218, 420);
        btnBrowseInputImage.Name = "btnBrowseInputImage";
        btnBrowseInputImage.Size = new Size(82, 25);
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
        btnClearInputImage.Location = new Point(306, 420);
        btnClearInputImage.Name = "btnClearInputImage";
        btnClearInputImage.Size = new Size(42, 25);
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
