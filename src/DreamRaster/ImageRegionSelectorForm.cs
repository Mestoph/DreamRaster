/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Sélecteur rectangulaire d'une zone de référence dans une image.
EN: Rectangular reference-region selector for an image.
*/

namespace OpenCodeLocalAI;

public sealed partial class ImageRegionSelectorForm : Form
{
    private Image? _image;
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;
    private string _language = "fr";

    public Rectangle SelectedRegion { get; private set; }

    // Constructeur sans paramètre requis par le WinForms Designer.
    public ImageRegionSelectorForm()
    {
        InitializeComponent();
        ApplyPresentation();
    }

    public ImageRegionSelectorForm(string path, string language)
        : this()
    {
        _language = language;
        _image = LoadUnlocked(path);
        _picture.Image = _image;
        ApplyPresentation();
    }

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

    private void Picture_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _image is null)
            return;

        var imageRect = GetDisplayedImageRectangle();
        var current = ClampToRect(e.Location, imageRect);
        _selection = NormalizeRectangle(_dragStart, current);
        _picture.Invalidate();
    }

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

    private void Picture_Paint(object? sender, PaintEventArgs e)
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
            return;

        using var shade = new SolidBrush(Color.FromArgb(45, Color.Black));
        using var pen = new Pen(AppTheme.SuccessHover, 2F);
        e.Graphics.FillRectangle(shade, _selection);
        e.Graphics.DrawRectangle(pen, _selection);
    }

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

    private static Rectangle NormalizeRectangle(Point a, Point b) =>
        Rectangle.FromLTRB(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y));

    private static Point ClampToRect(Point point, Rectangle rect) =>
        new(
            Math.Clamp(point.X, rect.Left, rect.Right),
            Math.Clamp(point.Y, rect.Top, rect.Bottom));

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
