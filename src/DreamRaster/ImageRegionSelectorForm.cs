/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

Sélecteur rectangulaire d'une zone de référence dans une image.
*/

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « ImageRegionSelectorForm », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed partial class ImageRegionSelectorForm : Form
{
    /// <summary>
    /// Stocke « _image », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private Image? _image;
    /// <summary>
    /// Stocke « _dragStart », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private Point _dragStart;
    /// <summary>
    /// Stocke « _selection », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private Rectangle _selection;
    /// <summary>
    /// Stocke « _dragging », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private bool _dragging;
    /// <summary>
    /// Stocke « _language », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private string _language = "fr";

        /// <summary>
    /// Retourne le rectangle s?lectionn? dans les coordonn?es de l?image source apr?s validation de la zone par l?utilisateur.
    /// </summary>
    public Rectangle SelectedRegion { get; private set; }

    // Constructeur sans paramètre requis par le WinForms Designer.
/// <summary>
/// Constructeur sans param?tre r?serv? au Designer WinForms ; il cr?e la fen?tre sans charger d?image utilisateur.
/// </summary>
    public ImageRegionSelectorForm()
    {
        InitializeComponent();
        ApplyPresentation();
    }

/// <summary>
/// Cr?e le s?lecteur de r?gion pour l?image fournie, applique la langue demand?e et pr?pare la s?lection rectangulaire retourn?e ? l?appelant.
/// </summary>
    public ImageRegionSelectorForm(string path, string language)
        : this()
    {
        _language = language;
        _image = LoadUnlocked(path);
        _picture.Image = _image;
        ApplyPresentation();
    }

    /// <summary>

    /// Applique ApplyPresentation aux réglages ou contrôles concernés en respectant les contraintes de DreamRaster.

    /// </summary>
    private void ApplyPresentation()
    {
        Text = L10n.Pick(
            _language,
            "Sélectionner la zone à conserver",
            "Select the region to keep");
        _info.Text = L10n.Pick(
            _language,
            "Maintenez le bouton gauche et tracez un rectangle autour de la zone à conserver.",
            "Hold the left mouse button and draw a rectangle around the region to keep.");
        _cancelButton.Text = L10n.Pick(_language, "Annuler", "Cancel");
    }

    /// <summary>

    /// Démarre la sélection d’une zone lorsque l’utilisateur appuie dans l’aperçu Image.

    /// </summary>
    private void Picture_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _image is null)
            return;

        var imageRect = GetDisplayedImageRectangle();
        if (!imageRect.Contains(e.Location))
            return;

        _dragging = true;
        _dragStart = ClampToRect(e.Location, imageRect);
        _selection = new Rectangle(_dragStart, Size.Empty);
        _picture.Invalidate();
    }

    /// <summary>

    /// Met à jour la zone de sélection pendant le déplacement de la souris dans l’aperçu Image.

    /// </summary>
    private void Picture_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _image is null)
            return;

        var imageRect = GetDisplayedImageRectangle();
        var current = ClampToRect(e.Location, imageRect);
        _selection = NormalizeRectangle(_dragStart, current);
        _picture.Invalidate();
    }

    /// <summary>

    /// Termine la sélection de zone lorsque l’utilisateur relâche le bouton de la souris.

    /// </summary>
    private void Picture_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_dragging || _image is null)
            return;

        _dragging = false;
        SelectedRegion = ControlToImageRectangle(_selection);
        _okButton.Enabled =
            SelectedRegion.Width >= 8 &&
            SelectedRegion.Height >= 8;
        _picture.Invalidate();
    }

    /// <summary>

    /// Dessine la zone de sélection et ses repères dans l’aperçu Image.

    /// </summary>
    private void Picture_Paint(object? sender, PaintEventArgs e)
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
            return;

        using var shade = new SolidBrush(Color.FromArgb(45, Color.Black));
        using var pen = new Pen(AppTheme.SuccessHover, 2F);
        e.Graphics.FillRectangle(shade, _selection);
        e.Graphics.DrawRectangle(pen, _selection);
    }

    /// <summary>

    /// Libère les ressources détenues par cette instance et termine proprement les objets associés.

    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _picture.Image = null;
            _image?.Dispose();
            _image = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>

    /// Retourne GetDisplayedImageRectangle calculé à partir de l’état courant de l’application.

    /// </summary>
    private Rectangle GetDisplayedImageRectangle()
    {
        var client = _picture.ClientRectangle;
        if (_image is null || _image.Width <= 0 || _image.Height <= 0)
            return client;

        var scale = Math.Min(
            client.Width / (double)_image.Width,
            client.Height / (double)_image.Height);

        var width = Math.Max(1, (int)Math.Round(_image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(_image.Height * scale));

        return new Rectangle(
            (client.Width - width) / 2,
            (client.Height - height) / 2,
            width,
            height);
    }

    /// <summary>

    /// Exécute le traitement <c>ControlToImageRectangle</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    private Rectangle ControlToImageRectangle(Rectangle controlRect)
    {
        if (_image is null)
            return Rectangle.Empty;

        var displayed = GetDisplayedImageRectangle();
        var clipped = Rectangle.Intersect(controlRect, displayed);
        if (clipped.IsEmpty)
            return Rectangle.Empty;

        var scaleX = _image.Width / (double)displayed.Width;
        var scaleY = _image.Height / (double)displayed.Height;

        var x = (int)Math.Round((clipped.Left - displayed.Left) * scaleX);
        var y = (int)Math.Round((clipped.Top - displayed.Top) * scaleY);
        var width = (int)Math.Round(clipped.Width * scaleX);
        var height = (int)Math.Round(clipped.Height * scaleY);

        return Rectangle.Intersect(
            new Rectangle(x, y, width, height),
            new Rectangle(0, 0, _image.Width, _image.Height));
    }

    /// <summary>

    /// Exécute le traitement <c>NormalizeRectangle</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    private static Rectangle NormalizeRectangle(Point a, Point b) =>
        Rectangle.FromLTRB(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y));

    /// <summary>

    /// Exécute le traitement <c>ClampToRect</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    private static Point ClampToRect(Point point, Rectangle rect) =>
        new(
            Math.Clamp(point.X, rect.Left, rect.Right),
            Math.Clamp(point.Y, rect.Top, rect.Bottom));

    /// <summary>

    /// Charge une copie autonome de l’image afin que le fichier source puisse être libé immédiatement après lecture.

    /// </summary>
    private static Image LoadUnlocked(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var temp = Image.FromStream(stream);
        return new Bitmap(temp);
    }
}
