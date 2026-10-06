/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Rendu des logs ANSI et choix des couleurs par source.
EN: ANSI log rendering and per-source color selection.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OpenCodeLocalAI;

public static class AnsiLogRenderer
{
    private const int MaxLogChars = 500_000;
    private static readonly Regex Ansi = new(@"\x1B\[(?<codes>[0-9;]*)m", RegexOptions.Compiled);
    private static readonly Font SourceFont = new("Consolas", 9F, FontStyle.Bold);

    public static void Append(RichTextBox box, string source, string message)
    {
        TrimIfNeeded(box);

        box.SelectionStart = box.TextLength;
        box.SelectionColor = Color.FromArgb(125, 132, 145);
        box.AppendText($"{DateTime.Now:HH:mm:ss} ");

        box.SelectionColor = SourceColor(source);
        box.SelectionFont = SourceFont;
        box.AppendText($"[{source}] ");
        box.SelectionFont = box.Font;

        AppendAnsi(box, message);
        box.SelectionColor = box.ForeColor;
        box.AppendText(Environment.NewLine);
        box.SelectionStart = box.TextLength;
        box.ScrollToCaret();
    }

    private static Color SourceColor(string source)
    {
        if (source.Contains("!", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("✗", StringComparison.Ordinal) ||
            source.StartsWith("ERREUR", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogError;

        if (source.Contains("⚠", StringComparison.Ordinal))
            return AppTheme.WarningHover;

        if (source.StartsWith("OpenCode", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("Proxy", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("API", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogOpenCode;

        if (source.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogOllama;

        if (source.StartsWith("ComfyUI", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogComfy;

        if (source.StartsWith("Install", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogInstall;

        if (source.StartsWith("GPU", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogGpu;

        if (source.StartsWith("UI", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("WebView", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogUi;

        if (source.StartsWith("FLUX", StringComparison.OrdinalIgnoreCase))
            return AppTheme.LogFlux;

        return AppTheme.Text;
    }

    private static void AppendAnsi(RichTextBox box, string text)
    {
        var pos = 0;
        var color = box.ForeColor;

        foreach (Match m in Ansi.Matches(text))
        {
            if (m.Index > pos)
            {
                box.SelectionColor = color;
                box.AppendText(text[pos..m.Index]);
            }

            var codes = m.Groups["codes"].Value
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var n) ? n : 0)
                .ToArray();

            for (var i = 0; i < codes.Length; i++)
            {
                var c = codes[i];
                if (c is 0 or 39) color = box.ForeColor;
                else if (c is >= 30 and <= 37) color = Basic(c - 30, false);
                else if (c is >= 90 and <= 97) color = Basic(c - 90, true);
                else if (c == 38 && i + 1 < codes.Length)
                {
                    if (codes[i + 1] == 5 && i + 2 < codes.Length)
                    {
                        color = Color256(codes[i + 2]); i += 2;
                    }
                    else if (codes[i + 1] == 2 && i + 4 < codes.Length)
                    {
                        color = Color.FromArgb(
                            Math.Clamp(codes[i + 2], 0, 255),
                            Math.Clamp(codes[i + 3], 0, 255),
                            Math.Clamp(codes[i + 4], 0, 255));
                        i += 4;
                    }
                }
            }

            pos = m.Index + m.Length;
        }

        if (pos < text.Length)
        {
            box.SelectionColor = color;
            box.AppendText(text[pos..]);
        }
    }

    private static Color Basic(int i, bool bright)
    {
        // Même les codes ANSI "normaux" sont volontairement clairs,
        // car toute l'application utilise un fond sombre.
        var normal = new[]
        {
            Color.FromArgb(150, 157, 171),
            Color.FromArgb(255, 128, 140),
            Color.FromArgb(137, 222, 160),
            Color.FromArgb(255, 220, 115),
            Color.FromArgb(103, 178, 255),
            Color.FromArgb(200, 146, 255),
            Color.FromArgb(107, 220, 230),
            Color.FromArgb(230, 233, 239)
        };

        var hi = new[]
        {
            Color.FromArgb(185, 191, 204),
            Color.FromArgb(255, 155, 164),
            Color.FromArgb(162, 239, 181),
            Color.FromArgb(255, 234, 151),
            Color.FromArgb(137, 197, 255),
            Color.FromArgb(218, 174, 255),
            Color.FromArgb(145, 235, 242),
            Color.White
        };

        return (bright ? hi : normal)[Math.Clamp(i, 0, 7)];
    }

    private static void TrimIfNeeded(RichTextBox box)
    {
        if (box.TextLength <= MaxLogChars)
            return;

        var remove = box.TextLength - (MaxLogChars * 3 / 4);
        if (remove <= 0)
            return;

        box.Select(0, remove);
        box.SelectedText = string.Empty;
        box.SelectionStart = box.TextLength;
    }

    private static Color Color256(int n)
    {
        n = Math.Clamp(n, 0, 255);
        if (n < 16) return Basic(n % 8, n >= 8);
        if (n >= 232)
        {
            var v = 8 + (n - 232) * 10;
            return Color.FromArgb(v, v, v);
        }
        n -= 16;
        var r = n / 36; var g = (n / 6) % 6; var b = n % 6;
        static int C(int v) => v == 0 ? 0 : 55 + 40 * v;
        return Color.FromArgb(C(r), C(g), C(b));
    }
}
