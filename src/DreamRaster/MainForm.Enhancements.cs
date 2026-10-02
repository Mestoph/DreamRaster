/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Améliorations d'interface : logs catégorisés, sélecteurs de modèles,
     négatif prompt, synchronisation des workflows et rafraîchissement des onglets.
EN: UI enhancements: categorized logs, model selectors, negative prompt,
    workflow synchronization and tab refresh.
*/

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace OpenCodeLocalAI;

public partial class MainForm
{
    private readonly Dictionary<string, RichTextBox> _logViews =
        new(StringComparer.OrdinalIgnoreCase);

    private TabControl _logTabs = null!;
    private ComboBox _visionModelCombo = null!;
    private ComboBox _fluxModelCombo = null!;
    private ComboBox _textEncoderCombo = null!;
    private ComboBox _vaeCombo = null!;
    private Label _negativePromptLabel = null!;
    private TextBox _negativePrompt = null!;
    private Button _improvePromptButton = null!;
    private CheckBox _autoImprovePrompt = null!;
    private Label _promptModelLabel = null!;
    private ComboBox _promptModelCombo = null!;
    private Label _seedLabel = null!;
    private NumericUpDown _seedInput = null!;
    private CheckBox _randomSeedCheck = null!;
    private Button _benchmarkButton = null!;
    private CancellationTokenSource? _benchmarkCts;
    private int _tabActivationBusy;
    private bool _virtualMemoryWarningLogged;

    private void InitializeEnhancedUi()
    {
        InitializeLogTabs();
        InitializeModelSelectors();
        InitializeNegativePromptEditor();
        InitializePromptEnhancerUi();
        InitializeVideoUi();
        ForceWhiteButtonText(this);
        EnableSmoothTabPainting();

        _tabs.SelectedIndexChanged += MainTabs_SelectedIndexChanged;

        AnsiLogRenderer.Append(
            _ollamaLog,
            "UI",
            "Console Ollama prête. Sélectionnez cet onglet pour démarrer/afficher Ollama.");
    }

    private void InitializeLogTabs()
    {
        tabLogs.Controls.Clear();

        _logTabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Name = "_logTabs",
            Padding = new Point(14, 4)
        };

        AddLogTab("Tous", "all", _allLog);
        AddLogTab("UI", "ui");
        AddLogTab("OpenCode", "opencode");
        AddLogTab("Ollama", "ollama");
        AddLogTab("ComfyUI", "comfyui");
        AddLogTab("FLUX.2", "flux");
        AddLogTab("Vidéo", "video");
        AddLogTab("Installation", "install");
        AddLogTab("Système", "system");

        tabLogs.Controls.Add(_logTabs);
        AppTheme.ApplyDark(_logTabs);
    }

    private void AddLogTab(string title, string key, RichTextBox? existing = null)
    {
        var page = new TabPage(title)
        {
            BackColor = AppTheme.Background,
            ForeColor = AppTheme.Text,
            Padding = new Padding(6)
        };

        var box = existing ?? CreateLogBox();
        box.Dock = DockStyle.Fill;
        page.Controls.Add(box);
        _logTabs.TabPages.Add(page);

        if (!key.Equals("all", StringComparison.OrdinalIgnoreCase))
            _logViews[key] = box;
    }

    private static RichTextBox CreateLogBox()
        => new()
        {
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            DetectUrls = false,
            Font = new Font("Consolas", 9F),
            ReadOnly = true,
            WordWrap = false
        };

    private void AppendCategorizedLog(string source, string message)
    {
        var key =
            source.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase)
                ? "ollama"
                : source.StartsWith("ComfyUI", StringComparison.OrdinalIgnoreCase)
                    ? "comfyui"
                    : source.StartsWith("FLUX", StringComparison.OrdinalIgnoreCase)
                        ? "flux"
                        : source.StartsWith("Video", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("Vidéo", StringComparison.OrdinalIgnoreCase)
                            ? "video"
                        : source.StartsWith("Install", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase)
                          || source.StartsWith("Ollama pull", StringComparison.OrdinalIgnoreCase)
                            ? "install"
                            : source.StartsWith("OpenCode", StringComparison.OrdinalIgnoreCase)
                              || source.StartsWith("Proxy", StringComparison.OrdinalIgnoreCase)
                              || source.StartsWith("API", StringComparison.OrdinalIgnoreCase)
                                ? "opencode"
                                : source.StartsWith("UI", StringComparison.OrdinalIgnoreCase)
                                  || source.StartsWith("WebView", StringComparison.OrdinalIgnoreCase)
                                    ? "ui"
                                    : "system";

        if (_logViews.TryGetValue(key, out var box))
            AnsiLogRenderer.Append(box, source, message);
    }

    private void InitializeModelSelectors()
    {
        _visionModelCombo = ReplaceModelTextBox(
            txtVisionModel,
            _s.VisionModel,
            new[] { "qwen3-vl:8b" });

        _fluxModelCombo = ReplaceModelTextBox(
            txtFluxModel,
            _s.FluxModel,
            new[] { "flux-2-klein-4b-fp8.safetensors" });

        _textEncoderCombo = ReplaceModelTextBox(
            txtTextEncoder,
            _s.TextEncoderModel,
            new[] { "qwen_3_4b.safetensors" });

        _vaeCombo = ReplaceModelTextBox(
            txtVae,
            _s.VaeModel,
            new[] { "flux2-vae.safetensors" });
    }

    private static ComboBox ReplaceModelTextBox(
        TextBox placeholder,
        string current,
        IEnumerable<string> defaults)
    {
        var parent = placeholder.Parent
            ?? throw new InvalidOperationException("Parent du sélecteur de modèle absent.");

        var combo = new ComboBox
        {
            Bounds = placeholder.Bounds,
            Anchor = placeholder.Anchor,
            DropDownStyle = ComboBoxStyle.DropDown,
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = placeholder.Font,
            Name = placeholder.Name + "Combo",
            DropDownWidth = 900
        };

        AddUniqueItems(combo, defaults);
        AddUniqueItems(combo, new[] { current });
        combo.Text = current;

        parent.Controls.Add(combo);
        combo.BringToFront();
        placeholder.Visible = false;

        return combo;
    }

    private static void AddUniqueItems(ComboBox combo, IEnumerable<string> values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!combo.Items.Cast<object>().Any(
                    x => string.Equals(
                        x?.ToString(),
                        value,
                        StringComparison.OrdinalIgnoreCase)))
            {
                combo.Items.Add(value);
            }
        }
    }

    private void InitializeNegativePromptEditor()
    {
        _prompt.Height = 178;

        _negativePromptLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(18, 235),
            Name = "_negativePromptLabel"
        };

        _negativePrompt = new TextBox
        {
            BackColor = AppTheme.Input,
            BorderStyle = BorderStyle.FixedSingle,
            ForeColor = AppTheme.Text,
            Location = new Point(18, 258),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Size = new Size(330, 69),
            Text = _s.NegativePrompt,
            Name = "_negativePrompt"
        };

        tabGenerate.Controls.Add(_negativePromptLabel);
        tabGenerate.Controls.Add(_negativePrompt);
        _negativePromptLabel.BringToFront();
        _negativePrompt.BringToFront();
    }

    private void InitializePromptEnhancerUi()
    {
        _improvePromptButton = new Button
        {
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(130, 12),
            Size = new Size(140, 28),
            Name = "_improvePromptButton",
            Text = "✨ Améliorer"
        };
        _improvePromptButton.FlatAppearance.BorderColor = AppTheme.Border;
        _improvePromptButton.Click += async (_, _) =>
            await SafeUiAsync(
                L10n.Pick(_s.Language, "Amélioration du prompt", "Prompt enhancement"),
                ImprovePromptFromUiAsync);

        _autoImprovePrompt = new CheckBox
        {
            AutoSize = false,
            ForeColor = Color.White,
            Location = new Point(276, 15),
            Size = new Size(72, 24),
            Name = "_autoImprovePrompt",
            Text = "Auto",
            Checked = _s.AutoImprovePrompt
        };

        _promptModelLabel = new Label
        {
            AutoSize = false,
            ForeColor = Color.White,
            Location = new Point(18, 48),
            Size = new Size(96, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Name = "_promptModelLabel",
            Text = "IA prompt"
        };

        _promptModelCombo = new ComboBox
        {
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDown,
            DropDownWidth = 620,
            Location = new Point(116, 47),
            Size = new Size(232, 23),
            Name = "_promptModelCombo",
            Text = _s.PromptModel
        };
        AddUniqueItems(
            _promptModelCombo,
            new[] { _s.PromptModel, "qwen3:1.7b", "qwen3:4b" });

        // Make room for the dedicated prompt-model selector.
        _prompt.Location = new Point(18, 78);
        _prompt.Size = new Size(330, 130);

        _negativePromptLabel.Location = new Point(18, 215);
        _negativePrompt.Location = new Point(18, 238);
        _negativePrompt.Size = new Size(330, 63);

        _seedLabel = new Label
        {
            AutoSize = false,
            ForeColor = Color.White,
            Location = new Point(203, 338),
            Size = new Size(100, 22),
            Text = "Seed",
            Name = "_seedLabel"
        };

        _seedInput = new NumericUpDown
        {
            BackColor = AppTheme.Input,
            ForeColor = AppTheme.Text,
            Location = new Point(203, 364),
            Size = new Size(98, 23),
            Minimum = 1,
            Maximum = long.MaxValue,
            Value = Math.Clamp(
                (decimal)Math.Max(1L, _s.GenerationSeed),
                1m,
                (decimal)long.MaxValue),
            Name = "_seedInput"
        };

        _randomSeedCheck = new CheckBox
        {
            AutoSize = false,
            ForeColor = Color.White,
            Location = new Point(305, 364),
            Size = new Size(43, 23),
            Text = "Rnd",
            Name = "_randomSeedCheck",
            Checked = _s.UseRandomSeed
        };
        _seedInput.Enabled = !_randomSeedCheck.Checked;
        _randomSeedCheck.CheckedChanged += (_, _) =>
            _seedInput.Enabled = !_randomSeedCheck.Checked;

        _benchmarkButton = new Button
        {
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(151, 520),
            Size = new Size(197, 34),
            Name = "_benchmarkButton",
            Text = "Comparer 1.7B / 4B"
        };
        _benchmarkButton.FlatAppearance.BorderColor = AppTheme.Border;
        _benchmarkButton.Click += async (_, _) =>
        {
            if (_benchmarkCts is not null)
            {
                _benchmarkCts.Cancel();
                return;
            }

            await SafeUiAsync(
                L10n.Pick(_s.Language, "Benchmark prompts", "Prompt benchmark"),
                RunPromptBenchmarkAsync);
        };

        tabGenerate.Controls.Add(_improvePromptButton);
        tabGenerate.Controls.Add(_autoImprovePrompt);
        tabGenerate.Controls.Add(_promptModelLabel);
        tabGenerate.Controls.Add(_promptModelCombo);
        tabGenerate.Controls.Add(_seedLabel);
        tabGenerate.Controls.Add(_seedInput);
        tabGenerate.Controls.Add(_randomSeedCheck);
        tabGenerate.Controls.Add(_benchmarkButton);

        _improvePromptButton.BringToFront();
        _autoImprovePrompt.BringToFront();
        _promptModelLabel.BringToFront();
        _promptModelCombo.BringToFront();
        _seedLabel.BringToFront();
        _seedInput.BringToFront();
        _randomSeedCheck.BringToFront();
        _benchmarkButton.BringToFront();
    }

    private async Task ImprovePromptFromUiAsync()
    {
        var source = _prompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Saisissez d'abord une idée ou un prompt.",
                    "Enter an idea or prompt first."));

        _improvePromptButton.Enabled = false;
        try
        {
            var improved = await ImprovePromptTextAsync(
                source,
                CancellationToken.None);

            _prompt.Text = improved;
            _prompt.Focus();
            _prompt.SelectionStart = _prompt.TextLength;

            Log("UI", "Prompt FLUX.2 amélioré localement via Ollama.");
        }
        finally
        {
            _improvePromptButton.Enabled = true;
        }
    }

    private async Task<string> ImprovePromptTextAsync(
        string source,
        CancellationToken ct,
        string? modelOverride = null)
    {
        await PrepareGpuForPromptEnhancementAsync(ct);
        await StartOllamaAsync();

        var model =
            string.IsNullOrWhiteSpace(modelOverride)
                ? CurrentComboModel(_promptModelCombo)
                : modelOverride.Trim();

        if (string.IsNullOrWhiteSpace(model))
            model = _s.PromptModel;

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        var installedModels = new List<string>();

        using (var tagsResponse = await http.GetAsync(
                   $"http://127.0.0.1:{_s.OllamaPort}/api/tags",
                   ct))
        {
            tagsResponse.EnsureSuccessStatusCode();
            var rawTags = await tagsResponse.Content.ReadAsStringAsync(ct);
            using var tagsDoc = JsonDocument.Parse(rawTags);

            if (tagsDoc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var item in models.EnumerateArray())
                {
                    if (!item.TryGetProperty("name", out var name))
                        continue;

                    var installedName = name.GetString();
                    if (!string.IsNullOrWhiteSpace(installedName))
                        installedModels.Add(installedName);
                }
            }
        }

        var preferredInstalled = installedModels.FirstOrDefault(name =>
            string.Equals(
                name,
                model,
                StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(preferredInstalled))
        {
            model = preferredInstalled;
        }
        else if (string.IsNullOrWhiteSpace(modelOverride))
        {
            var fallback = installedModels.FirstOrDefault(name =>
                !name.Contains(
                    "embed",
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(fallback))
            {
                throw new InvalidOperationException(
                    L10n.Pick(
                        _s.Language,
                        "Aucun modèle de chat local n'est installé dans Ollama.",
                        "No local chat model is installed in Ollama."));
            }

            Log(
                "Ollama",
                $"Amélioration du prompt : {model} indisponible, utilisation de {fallback}.");
            model = fallback;
        }
        else
        {
            throw new InvalidOperationException(
                $"Modèle Ollama requis pour le benchmark non installé : {model}");
        }

        var system =
            "You are a professional prompt engineer for FLUX.2 Klein image generation. " +
            "Return ONLY valid JSON with exactly one string property named prompt. " +
            "Rewrite the user's idea into one precise, coherent image prompt of roughly 50 to 90 useful English words. " +
            "Preserve subject, identity, actions, count, setting, era, colors and explicit constraints. " +
            "Add useful composition, framing, lighting, lens/camera language when relevant, materials, textures, atmosphere, depth and physically coherent detail. " +
            "Do not invent a different era, extra people or conflicting objects. " +
            "Avoid quality-token spam such as masterpiece, best quality, 8k, ultra HD. " +
            "Preserve quoted visible text exactly. Do not explain your reasoning.";

        var payload = JsonSerializer.Serialize(new
        {
            model,
            stream = false,
            think = false,
            format = "json",
            options = new
            {
                temperature = 0.25,
                num_predict = 240
            },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = source }
            }
        });

        using var response = await http.PostAsync(
            $"http://127.0.0.1:{_s.OllamaPort}/api/chat",
            new StringContent(
                payload,
                System.Text.Encoding.UTF8,
                "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Ollama HTTP {(int)response.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var content =
            doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var value)
                ? value.GetString()?.Trim()
                : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                L10n.Pick(
                    _s.Language,
                    "Ollama n'a retourné aucun prompt amélioré.",
                    "Ollama returned no improved prompt."));
        }

        try
        {
            using var contentDoc = JsonDocument.Parse(content);
            if (contentDoc.RootElement.TryGetProperty("prompt", out var promptNode))
            {
                var prompt = promptNode.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    Log("Ollama", $"Prompt amélioré avec {model}.");
                    return prompt;
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to the guarded plain-text fallback below.
        }

        var looksLikeReasoning =
            content.Contains("We are given", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Steps:", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Let's tackle", StringComparison.OrdinalIgnoreCase) ||
            content.Length > 1600;

        if (looksLikeReasoning)
        {
            throw new InvalidOperationException(
                $"Le modèle {model} n'a pas respecté le format JSON du prompt.");
        }

        Log("Ollama", $"Prompt amélioré avec {model} (fallback texte).");
        return content.Trim().Trim('"');
    }

    private async Task PrepareGpuForPromptEnhancementAsync(
        CancellationToken ct)
    {
        if (_comfy.Running)
        {
            Log(
                "ComfyUI",
                "Arrêt temporaire avant amélioration du prompt pour libérer la VRAM.");
            await _comfy.StopAsync();
        }
        else
        {
            await EnsurePortableServicePortFreeAsync(
                _s.ComfyPort,
                "ComfyUI",
                _s.ComfyPython);
        }

        await Task.Delay(1000, ct);
    }

    private async Task<HashSet<string>> GetInstalledOllamaModelsAsync(
        CancellationToken ct)
    {
        await StartOllamaAsync();

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var raw = await http.GetStringAsync(
            $"http://127.0.0.1:{_s.OllamaPort}/api/tags",
            ct);

        using var doc = JsonDocument.Parse(raw);
        var result = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (doc.RootElement.TryGetProperty("models", out var models))
        {
            foreach (var item in models.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var name))
                    continue;

                var value = name.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }
        }

        return result;
    }

    private sealed record PromptBenchmarkRow(
        string Category,
        string Model,
        long Seed,
        string SourcePrompt,
        string ImprovedPrompt,
        double PromptSeconds,
        double FluxSeconds,
        double TotalSeconds,
        string ImagePath);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(
        [In, Out] MemoryStatusEx buffer);

    private static long GetAvailableCommitMiB()
    {
        var status = new MemoryStatusEx();
        return GlobalMemoryStatusEx(status)
            ? (long)(status.AvailPageFile / (1024UL * 1024UL))
            : long.MaxValue;
    }

    private static long GetConfiguredPageFileMiB()
    {
        try
        {
            using var key =
                Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");

            var raw = key?.GetValue("PagingFiles");
            var entries = raw switch
            {
                string one => new[] { one },
                string[] many => many,
                _ => Array.Empty<string>()
            };

            long totalMaximumMiB = 0;
            var foundExplicitMaximum = false;

            foreach (var entry in entries)
            {
                var parts = entry.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

                if (parts.Length < 3 ||
                    !long.TryParse(parts[^1], out var maximumMiB))
                {
                    continue;
                }

                foundExplicitMaximum = true;

                // "0 0" means Windows/system-managed sizing.
                if (maximumMiB == 0)
                    return 0;

                totalMaximumMiB += maximumMiB;
            }

            if (foundExplicitMaximum)
                return totalMaximumMiB;
        }
        catch
        {
            // Fall back to the currently allocated commit extension.
        }

        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
            return 0;

        var totalCommit =
            (long)(status.TotalPageFile / (1024UL * 1024UL));
        var physical =
            (long)(status.TotalPhys / (1024UL * 1024UL));

        return Math.Max(0, totalCommit - physical);
    }

    private string BuildVirtualMemoryGuidance(
        string component,
        long availableCommitMiB)
    {
        var pageFileMiB = GetConfiguredPageFileMiB();

        var pageFileText =
            pageFileMiB > 0
                ? $"{pageFileMiB / 1024.0:0.0} Go maximum"
                : L10n.Pick(
                    _s.Language,
                    "géré par Windows / taille dynamique",
                    "Windows-managed / dynamic size");

        return L10n.Pick(
            _s.Language,
            $"{component} ne peut pas être démarré avec une marge de mémoire engagée suffisante. " +
            $"Commit disponible : {availableCommitMiB / 1024.0:0.0} Go. " +
            $"Fichier de pagination : {pageFileText}. " +
            "DreamRaster évite maintenant de charger Ollama et ComfyUI en même temps. " +
            "Pour les charges FLUX/Wan/Qwen, utilisez de préférence un fichier de pagination géré par Windows " +
            "ou un maximum d'au moins 16 Go.",
            $"{component} cannot start with enough committed-memory headroom. " +
            $"Available commit: {availableCommitMiB / 1024.0:0.0} GB. " +
            $"Page file: {pageFileText}. " +
            "DreamRaster now avoids loading Ollama and ComfyUI at the same time. " +
            "For FLUX/Wan/Qwen workloads, prefer a Windows-managed page file " +
            "or a maximum of at least 16 GB.");
    }

    private async Task WaitForCommitRecoveryAsync(
        long minimumMiB,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        var lastAvailable = GetAvailableCommitMiB();

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            lastAvailable = GetAvailableCommitMiB();
            if (lastAvailable >= minimumMiB)
                return;

            await Task.Delay(500, ct);
        }

        throw new InvalidOperationException(
            BuildVirtualMemoryGuidance(
                "DreamRaster",
                lastAvailable));
    }

    private void LogVirtualMemoryWarningIfNeeded()
    {
        if (_virtualMemoryWarningLogged)
            return;

        _virtualMemoryWarningLogged = true;

        var pageFileMiB = GetConfiguredPageFileMiB();
        if (pageFileMiB <= 0 || pageFileMiB >= 8192)
            return;

        Log(
            "Système !",
            L10n.Pick(
                _s.Language,
                $"Fichier de pagination Windows limité à {pageFileMiB / 1024.0:0.0} Go max. " +
                "FLUX.2, Wan et Qwen peuvent atteindre la limite de mémoire engagée. " +
                "DreamRaster empêchera les chevauchements Ollama/ComfyUI ; " +
                "un fichier de pagination géré par Windows ou >= 16 Go est recommandé.",
                $"Windows page file is limited to {pageFileMiB / 1024.0:0.0} GB max. " +
                "FLUX.2, Wan and Qwen can hit the commit limit. " +
                "DreamRaster will prevent Ollama/ComfyUI overlap; " +
                "a Windows-managed page file or >= 16 GB is recommended."));
    }

    private async Task CleanupBenchmarkComfyAsync(
        CancellationToken ct)
    {
        try
        {
            await _comfy.StopAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            // A previous DreamRaster process can leave its portable ComfyUI alive.
            // Ensure the tracked port is really released before the next case.
            await EnsurePortableServicePortFreeAsync(
                _s.ComfyPort,
                "ComfyUI",
                _s.ComfyPython);
        }

        var deadline = DateTime.UtcNow.AddSeconds(30);
        const long desiredFreeCommitMiB = 6144;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var available = GetAvailableCommitMiB();
            if (available >= desiredFreeCommitMiB)
            {
                Log(
                    "Système",
                    $"Benchmark : ComfyUI arrêté · commit disponible {available:N0} MiB.");
                return;
            }

            await Task.Delay(500, ct);
        }

        Log(
            "Système !",
            $"Benchmark : commit disponible seulement {GetAvailableCommitMiB():N0} MiB après nettoyage.");
    }

    private async Task RunPromptBenchmarkAsync()
    {
        var answer = MessageBox.Show(
            L10n.Pick(
                _s.Language,
                "Ce test va générer 8 images FLUX.2 (4 catégories × 2 modèles Ollama) avec des seeds fixes. " +
                "Il peut prendre plusieurs minutes et solliciter fortement le GPU.\n\nContinuer ?",
                "This test will generate 8 FLUX.2 images (4 categories × 2 Ollama models) with fixed seeds. " +
                "It may take several minutes and heavily use the GPU.\n\nContinue?"),
            L10n.Pick(_s.Language, "Comparatif IA prompt", "Prompt AI comparison"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        var pageFileMiB = GetConfiguredPageFileMiB();
        if (pageFileMiB > 0 && pageFileMiB < 8192)
        {
            var memoryAnswer = MessageBox.Show(
                L10n.Pick(
                    _s.Language,
                    $"La mémoire virtuelle Windows semble limitée à environ {pageFileMiB / 1024.0:0.0} Go. " +
                    "Le benchmark FLUX.2 peut atteindre la limite de mémoire engagée même si de la RAM physique reste libre. " +
                    "DreamRaster arrêtera ComfyUI entre chaque image pour réduire ce risque.\n\nContinuer ?",
                    $"Windows virtual memory appears limited to about {pageFileMiB / 1024.0:0.0} GB. " +
                    "The FLUX.2 benchmark can hit the commit limit even while physical RAM is still available. " +
                    "DreamRaster will fully stop ComfyUI between images to reduce this risk.\n\nContinue?"),
                L10n.Pick(
                    _s.Language,
                    "Mémoire virtuelle limitée",
                    "Limited virtual memory"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (memoryAnswer != DialogResult.Yes)
                return;
        }

        _benchmarkCts = new CancellationTokenSource();
        var ct = _benchmarkCts.Token;

        _benchmarkButton.Text =
            L10n.Pick(_s.Language, "Annuler benchmark", "Cancel benchmark");
        btnGenerate.Enabled = false;
        _improvePromptButton.Enabled = false;

        var originalPrompt = _prompt.Text;
        var originalModel = CurrentComboModel(_promptModelCombo);

        try
        {
            var installed = await GetInstalledOllamaModelsAsync(ct);
            var models = new[] { "qwen3:1.7b", "qwen3:4b" };

            var missing = models
                .Where(model => !installed.Contains(model))
                .ToArray();

            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Modèles Ollama requis absents : " +
                    string.Join(", ", missing));
            }

            var cases = new[]
            {
                (
                    Category: "Portrait",
                    Seed: 1001L,
                    Prompt: "portrait serré d'une femme rousse de 35 ans, lumière de fenêtre, fond sombre"),
                (
                    Category: "Paysage",
                    Seed: 1002L,
                    Prompt: "vallée alpine au lever du soleil après une nuit de neige, rivière au premier plan"),
                (
                    Category: "Architecture",
                    Seed: 1003L,
                    Prompt: "maison brutaliste en béton au bord d'un lac, grandes baies vitrées, temps couvert"),
                (
                    Category: "Cinematique",
                    Seed: 1004L,
                    Prompt: "détective seul traverse une rue mouillée la nuit, néons rouges et bleus, voiture ancienne au fond")
            };

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folder = Path.Combine(
                PortablePaths.Resolve(_s.Images),
                "benchmark_" + stamp);
            Directory.CreateDirectory(folder);

            var rows = new List<PromptBenchmarkRow>();
            var totalJobs = models.Length * cases.Length;
            var job = 0;

            foreach (var model in models)
            {
                foreach (var test in cases)
                {
                    ct.ThrowIfCancellationRequested();
                    job++;

                    await CleanupBenchmarkComfyAsync(ct);

                    _genText.Text =
                        $"Benchmark {job}/{totalJobs} · {test.Category} · {model}";
                    _prompt.Text = test.Prompt;
                    _seedInput.Value = test.Seed;
                    _randomSeedCheck.Checked = false;

                    var totalWatch = Stopwatch.StartNew();

                    var promptWatch = Stopwatch.StartNew();
                    var improved = await ImprovePromptTextAsync(
                        test.Prompt,
                        ct,
                        model);
                    promptWatch.Stop();

                    _prompt.Text = improved;

                    var fluxWatch = Stopwatch.StartNew();
                    ImageGenerationResult result;
                    try
                    {
                        result = await _generator.GenerateAsync(
                            improved,
                            _negativePrompt.Text.Trim(),
                            _s.DefaultWidth,
                            _s.DefaultHeight,
                            inputImagePath: null,
                            imgToImgStrength: 1.0,
                            ct,
                            test.Seed);
                    }
                    finally
                    {
                        fluxWatch.Stop();
                        await CleanupBenchmarkComfyAsync(ct);
                    }

                    totalWatch.Stop();

                    if (!result.Ok ||
                        string.IsNullOrWhiteSpace(result.Path) ||
                        !File.Exists(result.Path))
                    {
                        throw new InvalidOperationException(
                            result.Error ??
                            $"Image benchmark absente : {test.Category} / {model}");
                    }

                    var safeModel = model
                        .Replace(":", "_")
                        .Replace("/", "_");
                    var destination = Path.Combine(
                        folder,
                        $"{test.Seed}_{test.Category}_{safeModel}.png");
                    File.Copy(result.Path, destination, true);
                    ShowPreviewImage(destination);

                    rows.Add(new PromptBenchmarkRow(
                        test.Category,
                        model,
                        test.Seed,
                        test.Prompt,
                        improved,
                        Math.Round(promptWatch.Elapsed.TotalSeconds, 3),
                        Math.Round(fluxWatch.Elapsed.TotalSeconds, 3),
                        Math.Round(totalWatch.Elapsed.TotalSeconds, 3),
                        destination));

                    Log(
                        "UI",
                        $"Benchmark {test.Category}/{model} : " +
                        $"prompt {promptWatch.Elapsed.TotalSeconds:0.00}s · " +
                        $"FLUX {fluxWatch.Elapsed.TotalSeconds:0.00}s · " +
                        $"total {totalWatch.Elapsed.TotalSeconds:0.00}s.");
                }
            }

            var jsonPath = Path.Combine(folder, "benchmark.json");
            await File.WriteAllTextAsync(
                jsonPath,
                JsonSerializer.Serialize(
                    rows,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                ct);

            var csvPath = Path.Combine(folder, "benchmark.csv");
            var csvLines = new List<string>
            {
                "category;model;seed;prompt_seconds;flux_seconds;total_seconds;image"
            };
            csvLines.AddRange(rows.Select(row =>
                string.Join(
                    ";",
                    row.Category,
                    row.Model,
                    row.Seed,
                    row.PromptSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.FluxSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.TotalSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    row.ImagePath.Replace(";", "_"))));
            await File.WriteAllLinesAsync(csvPath, csvLines, ct);

            var summaries = models.Select(model =>
            {
                var modelRows = rows
                    .Where(row => row.Model.Equals(
                        model,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                return
                    $"{model} : prompt {modelRows.Average(x => x.PromptSeconds):0.00}s · " +
                    $"FLUX {modelRows.Average(x => x.FluxSeconds):0.00}s · " +
                    $"total {modelRows.Average(x => x.TotalSeconds):0.00}s";
            });

            _genText.Text = L10n.Pick(
                _s.Language,
                "Benchmark terminé.",
                "Benchmark complete.");

            MessageBox.Show(
                string.Join(Environment.NewLine, summaries) +
                Environment.NewLine + Environment.NewLine +
                folder,
                L10n.Pick(_s.Language, "Benchmark terminé", "Benchmark complete"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        finally
        {
            try
            {
                await CleanupBenchmarkComfyAsync(
                    CancellationToken.None);
            }
            catch
            {
            }

            _prompt.Text = originalPrompt;

            if (!string.IsNullOrWhiteSpace(originalModel))
                _promptModelCombo.Text = originalModel;

            _benchmarkCts?.Dispose();
            _benchmarkCts = null;
            _benchmarkButton.Text = L10n.Pick(
                _s.Language,
                "Comparer 1.7B / 4B",
                "Compare 1.7B / 4B");
            btnGenerate.Enabled = true;
            _improvePromptButton.Enabled = true;
        }
    }

    private void ApplyEnhancedTranslations()
    {
        if (_negativePromptLabel is null)
            return;

        _negativePromptLabel.Text = L10n.Pick(
            _s.Language,
            "Négatif / éléments à éviter",
            "Negative / things to avoid");

        _improvePromptButton.Text = L10n.Pick(
            _s.Language,
            "✨ Améliorer",
            "✨ Improve");
        _autoImprovePrompt.Text = "Auto";
        _autoImprovePrompt.AccessibleDescription = L10n.Pick(
            _s.Language,
            "Améliorer automatiquement le prompt avec le modèle Ollama sélectionné avant chaque génération.",
            "Automatically improve the prompt with the selected Ollama model before each generation.");
        _promptModelLabel.Text = L10n.Pick(
            _s.Language,
            "IA prompt",
            "Prompt AI");
        _seedLabel.Text = "Seed";
        _randomSeedCheck.Text = L10n.Pick(
            _s.Language,
            "Rnd",
            "Rnd");
        if (_benchmarkCts is null)
        {
            _benchmarkButton.Text = L10n.Pick(
                _s.Language,
                "Comparer 1.7B / 4B",
                "Compare 1.7B / 4B");
        }

        if (_logTabs is not null && _logTabs.TabPages.Count >= 9)
        {
            _logTabs.TabPages[0].Text = L10n.Pick(_s.Language, "Tous", "All");
            _logTabs.TabPages[1].Text = "UI";
            _logTabs.TabPages[2].Text = "OpenCode";
            _logTabs.TabPages[3].Text = "Ollama";
            _logTabs.TabPages[4].Text = "ComfyUI";
            _logTabs.TabPages[5].Text = "FLUX.2";
            _logTabs.TabPages[6].Text = L10n.Pick(_s.Language, "Vidéo", "Video");
            _logTabs.TabPages[7].Text = L10n.Pick(_s.Language, "Installation", "Installation");
            _logTabs.TabPages[8].Text = L10n.Pick(_s.Language, "Système", "System");
        }

        ApplyVideoTranslations();

        lblConfigHint.Text =
            L10n.T(_s.Language, "config.hint") + Environment.NewLine +
            L10n.Pick(
                _s.Language,
                "Les listes de modèles sont éditables : vous pouvez saisir le nom exact d'un modèle local personnalisé ou non filtré, à condition qu'il soit compatible avec le moteur sélectionné.",
                "Model lists are editable: you may enter the exact name of a custom or unfiltered local model, provided it is compatible with the selected engine.") +
            Environment.NewLine +
            L10n.Pick(
                _s.Language,
                "Validation : [OK] compatible/installé · [?] détecté mais non vérifié · [X] incompatible, manquant ou non installé.",
                "Validation: [OK] compatible/installed · [?] detected but unverified · [X] incompatible, missing or not installed.");
    }

    private async void MainTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshSelectedTab();

        if (Interlocked.Exchange(ref _tabActivationBusy, 1) != 0)
            return;

        try
        {
            if (_tabs.SelectedTab == tabComfy && !_closing)
            {
                await SafeUiAsync("ComfyUI", OpenComfyEmbeddedAsync);
            }
            else if (_tabs.SelectedTab == tabOpenCode && !_closing)
            {
                await SafeUiAsync("OpenCode", OpenEmbeddedAsync);
            }
            else if (_tabs.SelectedTab == tabOllama && !_closing)
            {
                if (_comfy.Running &&
                    (_generator.IsBusy || _videoCts is not null))
                {
                    var message =
                        L10n.Pick(
                            _s.Language,
                            "Ollama n'est pas démarré pendant une génération FLUX/Wan active afin d'éviter une saturation de la mémoire virtuelle.",
                            "Ollama is not started during an active FLUX/Wan generation to avoid exhausting virtual memory.");

                    Log("Ollama", message);
                    AnsiLogRenderer.Append(
                        _ollamaLog,
                        "Ollama",
                        message);
                }
                else
                {
                    await SafeUiAsync("Ollama", StartOllamaAsync);
                    await RefreshModelChoicesAsync();
                }
            }
            else if (_tabs.SelectedTab == tabConfiguration && !_closing)
            {
                await RefreshModelChoicesAsync();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _tabActivationBusy, 0);
            RefreshSelectedTab();
        }
    }

    private void RefreshSelectedTab()
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(new Action(() =>
        {
            var page = _tabs.SelectedTab;
            _tabs.Invalidate(true);
            page?.Invalidate(true);
            _web.Invalidate();
            _comfyWeb.Invalidate();
            page?.Refresh();
            _tabs.Refresh();
        }));
    }

    private void EnableSmoothTabPainting()
    {
        try
        {
            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_tabs, true);
        }
        catch
        {
            // Cosmetic optimization only.
        }
    }

    private static void ForceWhiteButtonText(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Button button)
            {
                button.ForeColor = Color.White;
                button.UseVisualStyleBackColor = false;
            }

            if (control.HasChildren)
                ForceWhiteButtonText(control);
        }
    }

    private async Task RefreshModelChoicesAsync()
    {
        var comfyRoot = PortablePreflight.GetComfyRoot(_s);

        RefreshValidatedModelChoices(
            _fluxModelCombo,
            Path.Combine(comfyRoot, "models", "diffusion_models"),
            Flux2ModelRole.Diffusion,
            _s.FluxModel);

        RefreshValidatedModelChoices(
            _textEncoderCombo,
            Path.Combine(comfyRoot, "models", "text_encoders"),
            Flux2ModelRole.TextEncoder,
            _s.TextEncoderModel);

        RefreshValidatedModelChoices(
            _vaeCombo,
            Path.Combine(comfyRoot, "models", "vae"),
            Flux2ModelRole.Vae,
            _s.VaeModel);

        await RefreshVisionModelChoicesAsync();
    }

    private async Task RefreshVisionModelChoicesAsync()
    {
        var wanted = CurrentComboModel(_visionModelCombo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = _s.VisionModel;

        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var raw = await http.GetStringAsync(
                $"http://127.0.0.1:{_s.OllamaPort}/api/tags");

            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var value = name.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            installed.Add(value);
                    }
                }
            }
        }
        catch
        {
            // The portable Ollama service may legitimately be stopped.
            // The manifest check below still gives a reliable local state
            // for explicitly configured/default models.
        }

        var candidates = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase)
        {
            "qwen3-vl:8b"
        };

        if (!string.IsNullOrWhiteSpace(_s.VisionModel))
            candidates.Add(_s.VisionModel);

        var choices = candidates
            .Select(name =>
            {
                var isInstalled =
                    installed.Contains(name) ||
                    IsPortableOllamaModelInstalled(name);

                var state = isInstalled
                    ? ModelCompatibilityState.Compatible
                    : ModelCompatibilityState.Missing;

                var status = isInstalled
                    ? L10n.Pick(
                        _s.Language,
                        "installé dans Ollama portable",
                        "installed in portable Ollama")
                    : L10n.Pick(
                        _s.Language,
                        "non installé",
                        "not installed");

                var marker = isInstalled ? "[OK]" : "[X]";

                return new ModelChoice(
                    name,
                    state,
                    $"{marker} {name} — {status}");
            })
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _visionModelCombo.BeginUpdate();
        try
        {
            _visionModelCombo.Items.Clear();

            foreach (var choice in choices)
                _visionModelCombo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                _visionModelCombo.SelectedItem = selected;
            else
                _visionModelCombo.Text = wanted;
        }
        finally
        {
            _visionModelCombo.EndUpdate();
        }

        RefreshPromptModelChoices(installed);
    }

    private void RefreshPromptModelChoices(
        HashSet<string> installed)
    {
        var wanted = CurrentComboModel(_promptModelCombo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = _s.PromptModel;

        var candidates =
            new HashSet<string>(
                installed,
                StringComparer.OrdinalIgnoreCase)
            {
                "qwen3:1.7b",
                "qwen3:4b"
            };

        if (!string.IsNullOrWhiteSpace(_s.PromptModel))
            candidates.Add(_s.PromptModel);

        var choices = candidates
            .Select(name =>
            {
                var isInstalled =
                    installed.Contains(name) ||
                    IsPortableOllamaModelInstalled(name);

                return new ModelChoice(
                    name,
                    isInstalled
                        ? ModelCompatibilityState.Compatible
                        : ModelCompatibilityState.Missing,
                    (isInstalled ? "[OK] " : "[X] ") +
                    name +
                    " — " +
                    (isInstalled
                        ? L10n.Pick(
                            _s.Language,
                            "installé",
                            "installed")
                        : L10n.Pick(
                            _s.Language,
                            "non installé",
                            "not installed")));
            })
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _promptModelCombo.BeginUpdate();
        try
        {
            _promptModelCombo.Items.Clear();
            foreach (var choice in choices)
                _promptModelCombo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                _promptModelCombo.SelectedItem = selected;
            else
                _promptModelCombo.Text = wanted;
        }
        finally
        {
            _promptModelCombo.EndUpdate();
        }
    }

    private bool IsPortableOllamaModelInstalled(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return false;

        var colon = modelName.LastIndexOf(':');
        var repository = colon > 0
            ? modelName[..colon]
            : modelName;
        var tag = colon > 0 && colon < modelName.Length - 1
            ? modelName[(colon + 1)..]
            : "latest";

        var repositoryParts = repository
            .Split(
                new[] { '/', '\\' },
                StringSplitOptions.RemoveEmptyEntries);

        var manifest = Path.Combine(
            new[]
            {
                PortablePaths.Resolve(_s.OllamaModels),
                "manifests",
                "registry.ollama.ai",
                "library"
            }
            .Concat(repositoryParts)
            .Append(tag)
            .ToArray());

        return File.Exists(manifest);
    }

    private void RefreshValidatedModelChoices(
        ComboBox combo,
        string folder,
        Flux2ModelRole role,
        string configured)
    {
        var wanted = CurrentComboModel(combo);
        if (string.IsNullOrWhiteSpace(wanted))
            wanted = configured;

        var files = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder)
                .Where(IsSupportedModelFile)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var choices = files
            .Select(path => CreateModelChoice(
                Path.GetFileName(path),
                Flux2ModelCompatibility.Validate(path, role)))
            .OrderBy(choice => CompatibilityOrder(choice.State))
            .ThenBy(choice => choice.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!choices.Any(choice =>
                string.Equals(
                    choice.FileName,
                    configured,
                    StringComparison.OrdinalIgnoreCase)))
        {
            choices.Add(CreateModelChoice(
                configured,
                new ModelCompatibilityResult(
                    ModelCompatibilityState.Missing,
                    "fichier configuré absent du dossier de modèles")));
        }

        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            foreach (var choice in choices)
                combo.Items.Add(choice);

            var selected = choices.FirstOrDefault(choice =>
                string.Equals(
                    choice.FileName,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                combo.SelectedItem = selected;
            else
                combo.Text = wanted;
        }
        finally
        {
            combo.EndUpdate();
        }
    }

    private ModelChoice CreateModelChoice(
        string fileName,
        ModelCompatibilityResult result)
    {
        var status = result.State switch
        {
            ModelCompatibilityState.Compatible =>
                L10n.Pick(_s.Language, "compatible FLUX.2 Klein", "FLUX.2 Klein compatible"),
            ModelCompatibilityState.Unverified =>
                L10n.Pick(_s.Language, "détecté — non vérifié", "detected — unverified"),
            ModelCompatibilityState.Incompatible =>
                L10n.Pick(_s.Language, "incompatible FLUX.2 Klein", "FLUX.2 Klein incompatible"),
            ModelCompatibilityState.Missing =>
                L10n.Pick(_s.Language, "configuré — fichier manquant", "configured — file missing"),
            _ => "?"
        };

        var marker = result.State switch
        {
            ModelCompatibilityState.Compatible => "[OK]",
            ModelCompatibilityState.Unverified => "[?]",
            _ => "[X]"
        };

        return new(
            fileName,
            result.State,
            $"{marker} {fileName} — {status} ({result.Reason})");
    }

    private static int CompatibilityOrder(ModelCompatibilityState state)
        => state switch
        {
            ModelCompatibilityState.Compatible => 0,
            ModelCompatibilityState.Unverified => 1,
            ModelCompatibilityState.Incompatible => 2,
            ModelCompatibilityState.Missing => 3,
            _ => 4
        };

    private static bool IsSupportedModelFile(string path)
        => path.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".ckpt", StringComparison.OrdinalIgnoreCase);

    private static string CurrentComboModel(ComboBox combo)
    {
        if (combo.SelectedItem is ModelChoice choice)
            return choice.FileName;

        return combo.Text.Trim();
    }

    private static string SelectedModel(ComboBox combo, TextBox fallback)
    {
        var value = CurrentComboModel(combo);
        return string.IsNullOrWhiteSpace(value)
            ? fallback.Text.Trim()
            : value;
    }

    private void ValidateSelectedFlux2Models(
        string fluxModel,
        string textEncoderModel,
        string vaeModel)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var checks = new[]
        {
            (L10n.Pick(_s.Language, "Modèle FLUX", "FLUX model"),
                Path.Combine(root, "models", "diffusion_models", fluxModel),
                Flux2ModelRole.Diffusion),
            ("Text encoder",
                Path.Combine(root, "models", "text_encoders", textEncoderModel),
                Flux2ModelRole.TextEncoder),
            ("VAE",
                Path.Combine(root, "models", "vae", vaeModel),
                Flux2ModelRole.Vae)
        };

        foreach (var (label, path, role) in checks)
        {
            var result = Flux2ModelCompatibility.Validate(path, role);
            if (result.State == ModelCompatibilityState.Incompatible)
            {
                throw new InvalidOperationException(
                    $"{label} : {Path.GetFileName(path)} n'est pas compatible avec " +
                    $"le workflow FLUX.2 Klein.\n\n{result.Reason}");
            }
        }
    }

    private void SynchronizeWorkflowDefaults()
    {
        var width = _s.DefaultWidth;
        var height = _s.DefaultHeight;
        var steps = _s.DefaultSteps;

        SynchronizeWorkflow(
            Path.Combine(PortablePaths.WorkflowsDir, PortablePaths.TextToImageWorkflowFile),
            width,
            height,
            steps,
            imgToImg: false);

        SynchronizeWorkflow(
            Path.Combine(PortablePaths.WorkflowsDir, PortablePaths.ImgToImgWorkflowFile),
            width,
            height,
            steps,
            imgToImg: true);

        Log(
            "UI",
            $"Workflows synchronisés : {width}x{height}, {steps} steps.");
    }

    private static void SynchronizeWorkflow(
        string path,
        int width,
        int height,
        int steps,
        bool imgToImg)
    {
        if (!File.Exists(path))
            return;

        var workflow =
            JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidDataException("Workflow JSON invalide : " + path);

        SetWorkflowInputs(workflow, "Flux2Scheduler", inputs =>
        {
            inputs["steps"] = steps;

            if (!imgToImg)
            {
                inputs["width"] = width;
                inputs["height"] = height;
            }
        });

        if (!imgToImg)
        {
            SetWorkflowInputs(workflow, "EmptyFlux2LatentImage", inputs =>
            {
                inputs["width"] = width;
                inputs["height"] = height;
                inputs["batch_size"] = 1;
            });
        }
        else
        {
            SetWorkflowInputs(workflow, "ImageScale", inputs =>
            {
                inputs["upscale_method"] = "lanczos";
                inputs["width"] = width;
                inputs["height"] = height;
                inputs["crop"] = "center";
            });
        }

        File.WriteAllText(
            path,
            workflow.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void SetWorkflowInputs(
        JsonObject workflow,
        string classType,
        Action<JsonObject> update)
    {
        foreach (var entry in workflow)
        {
            if (entry.Value is not JsonObject node)
                continue;

            if (!string.Equals(
                    node["class_type"]?.GetValue<string>(),
                    classType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (node["inputs"] is JsonObject inputs)
                update(inputs);
        }
    }
}
