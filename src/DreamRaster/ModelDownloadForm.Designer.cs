/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Déclaration Designer WinForms générée.
EN: Generated WinForms Designer declaration.
*/

#nullable enable

namespace OpenCodeLocalAI;

partial class ModelDownloadForm
{
    private System.ComponentModel.IContainer? components = null;

    private Label _intro = null!;
    private Label _urlLabel = null!;
    private Label _fileLabel = null!;
    private Label _shaLabel = null!;
    private Label _hint = null!;
    private Label _thirdPartyNotice = null!;
    private TextBox _url = null!;
    private TextBox _fileName = null!;
    private TextBox _sha256 = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _intro = new Label();
        _urlLabel = new Label();
        _fileLabel = new Label();
        _shaLabel = new Label();
        _hint = new Label();
        _thirdPartyNotice = new Label();
        _url = new TextBox();
        _fileName = new TextBox();
        _sha256 = new TextBox();
        _okButton = new Button();
        _cancelButton = new Button();
        SuspendLayout();
        // 
        // _intro
        // 
        _intro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _intro.ForeColor = AppTheme.TextMuted;
        _intro.Location = new Point(18, 14);
        _intro.Name = "_intro";
        _intro.Size = new Size(640, 34);
        _intro.TabIndex = 0;
        _intro.Text = "URL directe HTTP/HTTPS vers un .safetensors compatible. Le SHA256 est optionnel mais recommandé.";
        // 
        // _urlLabel
        // 
        _urlLabel.ForeColor = AppTheme.TextMuted;
        _urlLabel.Location = new Point(18, 58);
        _urlLabel.Name = "_urlLabel";
        _urlLabel.Size = new Size(120, 24);
        _urlLabel.TabIndex = 1;
        _urlLabel.Text = "URL du modèle";
        _urlLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _url
        // 
        _url.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _url.BackColor = AppTheme.Input;
        _url.BorderStyle = BorderStyle.FixedSingle;
        _url.ForeColor = AppTheme.Text;
        _url.Location = new Point(145, 55);
        _url.Name = "_url";
        _url.Size = new Size(515, 23);
        _url.TabIndex = 2;
        _url.TextChanged += Url_TextChanged;
        // 
        // _fileLabel
        // 
        _fileLabel.ForeColor = AppTheme.TextMuted;
        _fileLabel.Location = new Point(18, 94);
        _fileLabel.Name = "_fileLabel";
        _fileLabel.Size = new Size(120, 24);
        _fileLabel.TabIndex = 3;
        _fileLabel.Text = "Nom du fichier";
        _fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _fileName
        // 
        _fileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _fileName.BackColor = AppTheme.Input;
        _fileName.BorderStyle = BorderStyle.FixedSingle;
        _fileName.ForeColor = AppTheme.Text;
        _fileName.Location = new Point(145, 91);
        _fileName.Name = "_fileName";
        _fileName.Size = new Size(515, 23);
        _fileName.TabIndex = 4;
        // 
        // _shaLabel
        // 
        _shaLabel.ForeColor = AppTheme.TextMuted;
        _shaLabel.Location = new Point(18, 130);
        _shaLabel.Name = "_shaLabel";
        _shaLabel.Size = new Size(120, 24);
        _shaLabel.TabIndex = 5;
        _shaLabel.Text = "SHA256";
        _shaLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _sha256
        // 
        _sha256.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _sha256.BackColor = AppTheme.Input;
        _sha256.BorderStyle = BorderStyle.FixedSingle;
        _sha256.ForeColor = AppTheme.Text;
        _sha256.Location = new Point(145, 127);
        _sha256.Name = "_sha256";
        _sha256.Size = new Size(515, 23);
        _sha256.TabIndex = 6;
        // 
        // _hint
        // 
        _hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _hint.Font = new Font("Segoe UI", 8F);
        _hint.ForeColor = AppTheme.TextDim;
        _hint.Location = new Point(145, 155);
        _hint.Name = "_hint";
        _hint.Size = new Size(515, 35);
        _hint.TabIndex = 7;
        _hint.Text = "Laissez SHA256 vide si la source ne le publie pas : DreamRaster calculera et journalisera l'empreinte obtenue.";
        // 
        // _thirdPartyNotice
        // 
        _thirdPartyNotice.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _thirdPartyNotice.Font = new Font("Segoe UI", 8F);
        _thirdPartyNotice.ForeColor = AppTheme.Warning;
        _thirdPartyNotice.Location = new Point(145, 190);
        _thirdPartyNotice.Name = "_thirdPartyNotice";
        _thirdPartyNotice.Size = new Size(515, 48);
        _thirdPartyNotice.TabIndex = 8;
        _thirdPartyNotice.Text = "Modèles tiers : vérifiez leur licence et leur compatibilité. Pour tout contenu sexuel explicite, utilisez uniquement des sujets adultes ; ne sexualisez jamais un mineur.";
        // 
        // _okButton
        // 
        _okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _okButton.BackColor = AppTheme.Primary;
        _okButton.DialogResult = DialogResult.OK;
        _okButton.FlatAppearance.BorderSize = 0;
        _okButton.FlatStyle = FlatStyle.Flat;
        _okButton.ForeColor = Color.White;
        _okButton.Location = new Point(454, 258);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(98, 30);
        _okButton.TabIndex = 9;
        _okButton.Text = "Télécharger";
        _okButton.UseVisualStyleBackColor = false;
        // 
        // _cancelButton
        // 
        _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _cancelButton.BackColor = AppTheme.Surface;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.FlatAppearance.BorderColor = AppTheme.Border;
        _cancelButton.FlatStyle = FlatStyle.Flat;
        _cancelButton.ForeColor = AppTheme.Text;
        _cancelButton.Location = new Point(562, 258);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(98, 30);
        _cancelButton.TabIndex = 10;
        _cancelButton.Text = "Annuler";
        _cancelButton.UseVisualStyleBackColor = false;
        // 
        // ModelDownloadForm
        // 
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = AppTheme.Background;
        CancelButton = _cancelButton;
        ClientSize = new Size(680, 306);
        Controls.Add(_cancelButton);
        Controls.Add(_okButton);
        Controls.Add(_thirdPartyNotice);
        Controls.Add(_hint);
        Controls.Add(_sha256);
        Controls.Add(_shaLabel);
        Controls.Add(_fileName);
        Controls.Add(_fileLabel);
        Controls.Add(_url);
        Controls.Add(_urlLabel);
        Controls.Add(_intro);
        Font = new Font("Segoe UI", 9F);
        ForeColor = AppTheme.Text;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MaximizeBox = false;
        MaximumSize = new Size(900, 380);
        MinimizeBox = false;
        MinimumSize = new Size(680, 306);
        Name = "ModelDownloadForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Télécharger un modèle Image";
        FormClosing += ModelDownloadForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }
}
