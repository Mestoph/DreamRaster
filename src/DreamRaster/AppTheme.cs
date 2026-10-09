/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Palette sombre et application récursive du thème WinForms.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

using System.Drawing;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « AppTheme », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
internal static class AppTheme
{
    /// <summary>
    /// Stocke « Background », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Background = Color.FromArgb(24, 26, 31);
    /// <summary>
    /// Stocke « Surface », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Surface = Color.FromArgb(34, 37, 44);
    /// <summary>
    /// Stocke « SurfaceAlt », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color SurfaceAlt = Color.FromArgb(29, 32, 38);
    /// <summary>
    /// Stocke « Input », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Input = Color.FromArgb(18, 20, 24);
    /// <summary>
    /// Stocke « Border », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Border = Color.FromArgb(58, 64, 76);

    /// <summary>

    /// Stocke « Text », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.

    /// </summary>
    public static readonly Color Text = Color.FromArgb(242, 244, 248);
    /// <summary>
    /// Stocke « TextMuted », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color TextMuted = Color.FromArgb(196, 202, 214);
    /// <summary>
    /// Stocke « TextDim », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color TextDim = Color.FromArgb(145, 153, 169);

    /// <summary>

    /// Stocke « Primary », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.

    /// </summary>
    public static readonly Color Primary = Color.FromArgb(52, 113, 181);
    /// <summary>
    /// Stocke « PrimaryHover », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color PrimaryHover = Color.FromArgb(63, 132, 208);
    /// <summary>
    /// Stocke « Success », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Success = Color.FromArgb(54, 135, 98);
    /// <summary>
    /// Stocke « SuccessHover », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color SuccessHover = Color.FromArgb(65, 157, 114);
    /// <summary>
    /// Stocke « Danger », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Danger = Color.FromArgb(151, 62, 74);
    /// <summary>
    /// Stocke « DangerHover », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color DangerHover = Color.FromArgb(178, 73, 87);
    /// <summary>
    /// Stocke « Warning », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color Warning = Color.FromArgb(171, 128, 44);
    /// <summary>
    /// Stocke « WarningHover », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color WarningHover = Color.FromArgb(196, 148, 54);

    /// <summary>

    /// Stocke « LogOpenCode », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.

    /// </summary>
    public static readonly Color LogOpenCode = Color.FromArgb(103, 178, 255);
    /// <summary>
    /// Stocke « LogOllama », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogOllama = Color.FromArgb(137, 222, 160);
    /// <summary>
    /// Stocke « LogComfy », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogComfy = Color.FromArgb(255, 183, 93);
    /// <summary>
    /// Stocke « LogInstall », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogInstall = Color.FromArgb(255, 220, 115);
    /// <summary>
    /// Stocke « LogFlux », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogFlux = Color.FromArgb(200, 146, 255);
    /// <summary>
    /// Stocke « LogVideo », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogVideo = Color.FromArgb(255, 142, 196);
    /// <summary>
    /// Stocke « LogGpu », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogGpu = Color.FromArgb(107, 220, 230);
    /// <summary>
    /// Stocke « LogUi », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogUi = Color.FromArgb(126, 190, 255);
    /// <summary>
    /// Stocke « LogError », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    public static readonly Color LogError = Color.FromArgb(255, 128, 140);

    /// <summary>

    /// Applique ApplyDark aux réglages ou contrôles concernés en respectant les contraintes de DreamRaster.

    /// </summary>
    public static void ApplyDark(Control root)
    {
        ApplyRecursive(root);
    }

    /// <summary>

    /// Applique ApplyRecursive aux réglages ou contrôles concernés en respectant les contraintes de DreamRaster.

    /// </summary>
    private static void ApplyRecursive(Control control)
    {
        switch (control)
        {
            case Form form:
                form.BackColor = Background;
                form.ForeColor = Text;
                break;

            case TabPage page:
                page.BackColor = Background;
                page.ForeColor = Text;
                page.UseVisualStyleBackColor = false;
                break;

            case TabControl tabs:
                tabs.BackColor = Background;
                tabs.ForeColor = Text;
                break;

            case FlowLayoutPanel:
            case TableLayoutPanel:
            case Panel:
                control.BackColor = Background;
                control.ForeColor = Text;
                break;

            case RichTextBox rich:
                rich.BackColor = Input;
                rich.ForeColor = Text;
                rich.BorderStyle = BorderStyle.FixedSingle;
                break;

            case TextBox text:
                text.BackColor = Input;
                text.ForeColor = Text;
                text.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ComboBox combo:
                combo.BackColor = Input;
                combo.ForeColor = Text;
                combo.FlatStyle = FlatStyle.Flat;
                break;

            case NumericUpDown numeric:
                numeric.BackColor = Input;
                numeric.ForeColor = Text;
                break;

            case CheckedListBox checkedList:
                checkedList.BackColor = Input;
                checkedList.ForeColor = Text;
                break;

            case ListBox list:
                list.BackColor = Input;
                list.ForeColor = Text;
                break;

            case TreeView tree:
                tree.BackColor = Input;
                tree.ForeColor = Text;
                tree.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ListView listView:
                listView.BackColor = Input;
                listView.ForeColor = Text;
                listView.BorderStyle = BorderStyle.FixedSingle;
                break;

            case GroupBox group:
                group.BackColor = Background;
                group.ForeColor = Text;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = Background;
                checkBox.ForeColor = Text;
                break;

            case RadioButton radio:
                radio.BackColor = Background;
                radio.ForeColor = Text;
                break;

            case TrackBar track:
                track.BackColor = Background;
                track.ForeColor = Text;
                break;

            case LinkLabel link:
                link.BackColor = Background;
                link.ForeColor = Text;
                link.LinkColor = PrimaryHover;
                link.ActiveLinkColor = Text;
                link.VisitedLinkColor = Primary;
                break;

            case Button button:
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                break;

            case Label label:
                // Do not replace semantic colors (warning/success/error).
                // Corrige uniquement la couleur sombre par défaut de WinForms lorsque le thème sombre est actif.
                if (label.ForeColor == SystemColors.ControlText ||
                    label.ForeColor == Color.Black ||
                    label.ForeColor == Color.Empty)
                {
                    label.ForeColor = Text;
                }
                break;
        }

        foreach (Control child in control.Controls)
            ApplyRecursive(child);
    }

}
