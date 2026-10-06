/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Déclaration Designer WinForms générée.
EN: Generated WinForms Designer declaration.
*/

#nullable enable

namespace OpenCodeLocalAI;

partial class ImageRegionSelectorForm
{
    private System.ComponentModel.IContainer? components = null;

    private Label _info = null!;
    private Panel _footer = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;
    private PictureBox _picture = null!;

    private void InitializeComponent()
    {
        _info = new Label();
        _footer = new Panel();
        _okButton = new Button();
        _cancelButton = new Button();
        _picture = new PictureBox();
        _footer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_picture).BeginInit();
        SuspendLayout();
        // 
        // _info
        // 
        _info.Dock = DockStyle.Top;
        _info.ForeColor = AppTheme.TextMuted;
        _info.Location = new Point(0, 0);
        _info.Name = "_info";
        _info.Padding = new Padding(12, 8, 12, 4);
        _info.Size = new Size(964, 42);
        _info.TabIndex = 0;
        _info.Text = "Maintenez le bouton gauche et tracez un rectangle autour de la zone à conserver.";
        // 
        // _footer
        // 
        _footer.BackColor = AppTheme.Surface;
        _footer.Controls.Add(_okButton);
        _footer.Controls.Add(_cancelButton);
        _footer.Dock = DockStyle.Bottom;
        _footer.Location = new Point(0, 667);
        _footer.Name = "_footer";
        _footer.Size = new Size(964, 54);
        _footer.TabIndex = 2;
        // 
        // _okButton
        // 
        _okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _okButton.BackColor = AppTheme.Success;
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Enabled = false;
        _okButton.FlatAppearance.BorderSize = 0;
        _okButton.FlatStyle = FlatStyle.Flat;
        _okButton.ForeColor = Color.White;
        _okButton.Location = new Point(730, 10);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(110, 32);
        _okButton.TabIndex = 0;
        _okButton.Text = "OK";
        _okButton.UseVisualStyleBackColor = false;
        // 
        // _cancelButton
        // 
        _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _cancelButton.BackColor = AppTheme.Danger;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.FlatAppearance.BorderSize = 0;
        _cancelButton.FlatStyle = FlatStyle.Flat;
        _cancelButton.ForeColor = Color.White;
        _cancelButton.Location = new Point(850, 10);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(110, 32);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Annuler";
        _cancelButton.UseVisualStyleBackColor = false;
        // 
        // _picture
        // 
        _picture.BackColor = AppTheme.Input;
        _picture.Cursor = Cursors.Cross;
        _picture.Dock = DockStyle.Fill;
        _picture.Location = new Point(0, 42);
        _picture.Name = "_picture";
        _picture.Size = new Size(964, 625);
        _picture.SizeMode = PictureBoxSizeMode.Zoom;
        _picture.TabIndex = 1;
        _picture.TabStop = false;
        _picture.Paint += Picture_Paint;
        _picture.MouseDown += Picture_MouseDown;
        _picture.MouseMove += Picture_MouseMove;
        _picture.MouseUp += Picture_MouseUp;
        // 
        // ImageRegionSelectorForm
        // 
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = AppTheme.Background;
        CancelButton = _cancelButton;
        ClientSize = new Size(964, 721);
        Controls.Add(_picture);
        Controls.Add(_footer);
        Controls.Add(_info);
        ForeColor = AppTheme.Text;
        MinimumSize = new Size(720, 520);
        Name = "ImageRegionSelectorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Sélectionner la zone à conserver";
        _footer.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_picture).EndInit();
        ResumeLayout(false);
    }
}
