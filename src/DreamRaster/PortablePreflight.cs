/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Contrôles des composants, ports et environnement portable.
EN: Component, port and portable-environment checks.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Diagnostics;
using System.Net.Sockets;

namespace OpenCodeLocalAI;

public sealed record MissingComponent(string Key, string Label, string Path);

public static class PortablePreflight
{
    public static IReadOnlyList<MissingComponent> GetMissing(AppSettings s)
    {
        var result = new List<MissingComponent>();

        CheckFile(result, "opencode", "OpenCode", s.OpenCodeExe);
        CheckFile(result, "ollama", "Ollama", s.OllamaExe);
        CheckFile(result, "comfy-python", "Python ComfyUI", s.ComfyPython);
        CheckFile(result, "comfy-main", "ComfyUI", s.ComfyMain);

        var webViewRuntime = PortablePaths.GetFixedWebView2RuntimePath();
        if (webViewRuntime is null)
        {
            result.Add(new(
                "webview2-fixed",
                "WebView2 Fixed Version Runtime x64",
                Path.Combine(PortablePaths.Root, "bin", "webview2-fixed")));
        }

        var comfyRoot = GetComfyRoot(s);
        CheckAbsoluteFile(result, "flux", "FLUX.2",
            Path.Combine(comfyRoot, "models", "diffusion_models", s.FluxModel));
        CheckAbsoluteFile(result, "text-encoder", "Text encoder FLUX.2",
            Path.Combine(comfyRoot, "models", "text_encoders", s.TextEncoderModel));
        CheckAbsoluteFile(result, "vae", "VAE FLUX.2",
            Path.Combine(comfyRoot, "models", "vae", s.VaeModel));

        // FR : Le modèle Vision est optionnel et son manifeste est dérivé du nom configuré.
        // EN: The Vision model is optional and its manifest is derived from the configured model name.
        if (s.InstallVisionModel)
        {
            var manifest = GetPortableOllamaManifestPath(
                s.OllamaModels,
                s.VisionModel);

            if (!File.Exists(manifest))
                result.Add(new("qwen", s.VisionModel, manifest));
        }

        return result;
    }

    internal static string GetPortableOllamaManifestPath(
        string modelsDirectory,
        string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException(
                "Le nom du modèle Ollama ne peut pas être vide.",
                nameof(modelName));

        var trimmed = modelName.Trim();
        var colon = trimmed.LastIndexOf(':');
        var repository = colon > 0
            ? trimmed[..colon]
            : trimmed;
        var tag = colon > 0 && colon < trimmed.Length - 1
            ? trimmed[(colon + 1)..]
            : "latest";

        var repositoryParts = repository.Split(
            new[] { '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries);

        if (repositoryParts.Length == 0)
            throw new ArgumentException(
                "Nom de modèle Ollama invalide.",
                nameof(modelName));

        return Path.Combine(
            new[]
            {
                PortablePaths.Resolve(modelsDirectory),
                "manifests",
                "registry.ollama.ai",
                "library"
            }
            .Concat(repositoryParts)
            .Append(tag)
            .ToArray());
    }

    private static void CheckFile(List<MissingComponent> list, string key, string label, string configured)
    {
        string p;
        try { p = PortablePaths.Resolve(configured); }
        catch (Exception ex)
        {
            list.Add(new(key, label, ex.Message));
            return;
        }
        if (!File.Exists(p)) list.Add(new(key, label, p));
    }

    private static void CheckAbsoluteFile(List<MissingComponent> list, string key, string label, string path)
    {
        if (!PortablePaths.IsInsidePack(path) || !File.Exists(path))
            list.Add(new(key, label, path));
    }

    public static string GetComfyRoot(AppSettings s)
    {
        var main = PortablePaths.Resolve(s.ComfyMain);
        return Path.GetDirectoryName(main)
               ?? throw new InvalidOperationException("Chemin ComfyUI invalide.");
    }

    public static Dictionary<string,string> PortableEnvironment(string runtimeName)
    {
        var home = Path.Combine(PortablePaths.RuntimeDir, runtimeName);
        var appdata = Path.Combine(home, "appdata");
        var local = Path.Combine(home, "localappdata");
        var temp = Path.Combine(home, "temp");
        foreach (var d in new[] { home, appdata, local, temp, Path.Combine(home, "cuda-cache") })
            Directory.CreateDirectory(d);

        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["HOME"] = home,
            ["USERPROFILE"] = home,
            ["APPDATA"] = appdata,
            ["LOCALAPPDATA"] = local,
            ["TEMP"] = temp,
            ["TMP"] = temp,
            ["CUDA_CACHE_PATH"] = Path.Combine(home, "cuda-cache"),
            ["BROWSER"] = "none",
            ["NO_BROWSER"] = "1"
        };
    }

    public static async Task<(bool Open, int? Pid, string? Path)> InspectPortAsync(int port)
    {
        try
        {
            using var c = new TcpClient();
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(250));

            await c.ConnectAsync(
                "127.0.0.1",
                port,
                timeout.Token);
        }
        catch (OperationCanceledException)
        {
            return (false, null, null);
        }
        catch (SocketException)
        {
            return (false, null, null);
        }

        try
        {
            var psi = new ProcessStartInfo("netstat", "-ano -p tcp")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi);
            if (p is null) return (true, null, null);
            var text = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();

            foreach (var line in text.Split('\n'))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5 || !parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!parts[1].EndsWith(":" + port, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!parts[3].Contains("LISTEN", StringComparison.OrdinalIgnoreCase) &&
                    !parts[3].Contains("ÉCOUTE", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!int.TryParse(parts[^1], out var pid)) continue;
                try
                {
                    using var proc = Process.GetProcessById(pid);
                    return (true, pid, proc.MainModule?.FileName);
                }
                catch { return (true, pid, null); }
            }
        }
        catch { }

        return (true, null, null);
    }

    public static async Task RejectOccupiedExternalPortAsync(int port, string label)
    {
        var info = await InspectPortAsync(port);
        if (!info.Open) return;

        var owner = info.Path ?? "processus non identifié";
        var where = PortablePaths.IsInsidePack(info.Path) ? "un ancien processus du pack" : "un processus externe";
        throw new InvalidOperationException(
            $"{label} ne sera pas démarré : le port {port} est déjà utilisé par {where}.\n\n" +
            $"PID : {info.Pid?.ToString() ?? "?"}\nChemin : {owner}\n\n" +
            "DreamRaster n'utilise volontairement aucune installation système.\n" +
            "Fermez le processus externe, ou choisissez un autre port dans l'onglet Configuration puis enregistrez.");
    }
}
