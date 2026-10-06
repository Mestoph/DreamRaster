/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Dialogue compact pour télécharger un checkpoint tiers par URL.
EN: Compact dialog for downloading a third-party checkpoint by URL.
*/

namespace OpenCodeLocalAI;

public sealed partial class ModelDownloadForm : Form
{
    private string _language = "fr";
    private bool _imageModel = true;

    public string ModelUrl => _url.Text.Trim();
    public string FileName => _fileName.Text.Trim();
    public string Sha256 => _sha256.Text.Trim();

    // Constructeur sans paramètre requis par le WinForms Designer.
    public ModelDownloadForm()
    {
        InitializeComponent();
        ApplyPresentation();
    }

    public ModelDownloadForm(bool imageModel, string language)
        : this()
    {
        _imageModel = imageModel;
        _language = language;
        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        Text = L10n.Pick(
            _language,
            _imageModel
                ? "Télécharger un modèle Image"
                : "Télécharger un modèle Vidéo",
            _imageModel
                ? "Download Image model"
                : "Download Video model");

        _intro.Text = L10n.Pick(
            _language,
            "URL directe HTTP/HTTPS vers un .safetensors compatible. Le SHA256 est optionnel mais recommandé.",
            "Direct HTTP/HTTPS URL to a compatible .safetensors file. SHA256 is optional but recommended.");
        _urlLabel.Text = L10n.Pick(_language, "URL du modèle", "Model URL");
        _fileLabel.Text = L10n.Pick(_language, "Nom du fichier", "File name");
        _hint.Text = L10n.Pick(
            _language,
            "Laissez SHA256 vide si la source ne le publie pas : DreamRaster calculera et journalisera l'empreinte obtenue.",
            "Leave SHA256 empty if the source does not publish it: DreamRaster will calculate and log the downloaded hash.");
        _thirdPartyNotice.Text = L10n.Pick(
            _language,
            "Modèles tiers : vérifiez leur licence et leur compatibilité. Pour tout contenu sexuel explicite, utilisez uniquement des sujets adultes ; ne sexualisez jamais un mineur.",
            "Third-party models: verify their license and compatibility. For any explicit sexual content, use adults only; never sexualize a minor.");
        _okButton.Text = L10n.Pick(_language, "Télécharger", "Download");
        _cancelButton.Text = L10n.Pick(_language, "Annuler", "Cancel");
    }

    private void Url_TextChanged(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_fileName.Text))
            return;

        if (!Uri.TryCreate(_url.Text.Trim(), UriKind.Absolute, out var uri))
            return;

        var candidate = Path.GetFileName(uri.AbsolutePath);
        if (candidate.EndsWith(
                ".safetensors",
                StringComparison.OrdinalIgnoreCase))
        {
            _fileName.Text = candidate;
        }
    }

    private void ModelDownloadForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
            return;

        if (!Uri.TryCreate(ModelUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            MessageBox.Show(
                this,
                L10n.Pick(
                    _language,
                    "URL HTTP/HTTPS invalide.",
                    "Invalid HTTP/HTTPS URL."),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(FileName) ||
            !FileName.EndsWith(
                ".safetensors",
                StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(FileName) != FileName)
        {
            MessageBox.Show(
                this,
                L10n.Pick(
                    _language,
                    "Le nom doit être un fichier .safetensors sans chemin.",
                    "The name must be a .safetensors file name without a path."),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        if (Sha256.Length > 0 &&
            (Sha256.Length != 64 ||
             Sha256.Any(c => !Uri.IsHexDigit(c))))
        {
            MessageBox.Show(
                this,
                L10n.Pick(
                    _language,
                    "Le SHA256 doit contenir 64 caractères hexadécimaux.",
                    "SHA256 must contain 64 hexadecimal characters."),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }
}
