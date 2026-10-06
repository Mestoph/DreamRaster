/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Palette sombre et application récursive du thème WinForms.
EN: Dark palette and recursive WinForms theme application.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Drawing;

namespace OpenCodeLocalAI;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(24, 26, 31);
    public static readonly Color Surface = Color.FromArgb(34, 37, 44);
    public static readonly Color SurfaceAlt = Color.FromArgb(29, 32, 38);
    public static readonly Color Input = Color.FromArgb(18, 20, 24);
    public static readonly Color Border = Color.FromArgb(58, 64, 76);

    public static readonly Color Text = Color.FromArgb(242, 244, 248);
    public static readonly Color TextMuted = Color.FromArgb(196, 202, 214);
    public static readonly Color TextDim = Color.FromArgb(145, 153, 169);

    public static readonly Color Primary = Color.FromArgb(52, 113, 181);
    public static readonly Color PrimaryHover = Color.FromArgb(63, 132, 208);
    public static readonly Color Success = Color.FromArgb(54, 135, 98);
    public static readonly Color SuccessHover = Color.FromArgb(65, 157, 114);
    public static readonly Color Danger = Color.FromArgb(151, 62, 74);
    public static readonly Color DangerHover = Color.FromArgb(178, 73, 87);
    public static readonly Color Warning = Color.FromArgb(171, 128, 44);
    public static readonly Color WarningHover = Color.FromArgb(196, 148, 54);

    public static readonly Color LogOpenCode = Color.FromArgb(103, 178, 255);
    public static readonly Color LogOllama = Color.FromArgb(137, 222, 160);
    public static readonly Color LogComfy = Color.FromArgb(255, 183, 93);
    public static readonly Color LogInstall = Color.FromArgb(255, 220, 115);
    public static readonly Color LogFlux = Color.FromArgb(200, 146, 255);
    public static readonly Color LogVideo = Color.FromArgb(255, 142, 196);
    public static readonly Color LogGpu = Color.FromArgb(107, 220, 230);
    public static readonly Color LogUi = Color.FromArgb(126, 190, 255);
    public static readonly Color LogError = Color.FromArgb(255, 128, 140);

    public static void ApplyDark(Control root)
    {
        ApplyRecursive(root);
    }

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

            case NumericUpDown numeric:
                numeric.BackColor = Input;
                numeric.ForeColor = Text;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = Background;
                checkBox.ForeColor = Text;
                break;

            case Button button:
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                break;

            case Label label:
                if (label.ForeColor == SystemColors.ControlText ||
                    label.ForeColor.GetBrightness() < 0.45f)
                    label.ForeColor = Text;
                break;
        }

        foreach (Control child in control.Controls)
            ApplyRecursive(child);
    }

}
