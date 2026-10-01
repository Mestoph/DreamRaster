/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Export des diagnostics TXT/JSON sans casser l'application.
EN: TXT/JSON diagnostic export without crashing the application.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Diagnostics;
using System.Text.Json;

namespace OpenCodeLocalAI;

public static class DiagnosticExporter
{
    public static async Task<(string TextPath, string JsonPath)> ExportAsync(AppSettings s)
    {
        PortablePaths.EnsureLayout();
        var now = DateTime.Now;
        var stamp = now.ToString("yyyyMMdd-HHmmss");

        var missing = PortablePreflight.GetMissing(s);
        var gpu = await CaptureAsync(
            "nvidia-smi",
            "--query-gpu=name,driver_version,memory.total,memory.used --format=csv,noheader,nounits");
        var cuda = await CaptureAsync("nvidia-smi", "");

        var ports = new[]
        {
            ("OpenCode", s.OpenCodePort),
            ("Ollama", s.OllamaPort),
            ("ComfyUI", s.ComfyPort),
            ("Proxy", s.ImageProxyPort),
            ("API", s.GenerationApiPort)
        };

        var portData = new List<object>();
        var portStatus = "OK";
        foreach (var item in ports)
        {
            var p = await PortablePreflight.InspectPortAsync(item.Item2);
            var external = p.Open && !PortablePaths.IsInsidePack(p.Path);
            if (external) portStatus = "ERREUR";
            portData.Add(new
            {
                label = item.Item1,
                port = item.Item2,
                listening = p.Open,
                pid = p.Pid,
                path = p.Path,
                portable = p.Open && PortablePaths.IsInsidePack(p.Path)
            });
        }

        var nvidiaStatus = gpu.Ok ? "OK" : "ERREUR";
        var cudaStatus = gpu.Ok && cuda.Text.Contains("CUDA", StringComparison.OrdinalIgnoreCase)
            ? "OK" : "AVERTISSEMENT";
        var pathStatus = missing.Any(m => m.Key is "opencode" or "ollama" or "comfy-python" or "comfy-main")
            ? "ERREUR" : "OK";
        var modelStatus = missing.Any(m => m.Key is "flux" or "text-encoder" or "vae" or "qwen")
            ? "ERREUR" : "OK";

        var severities = new[] { nvidiaStatus, cudaStatus, portStatus, pathStatus, modelStatus };
        var global = severities.Contains("ERREUR") ? "ERREUR"
            : severities.Contains("AVERTISSEMENT") ? "AVERTISSEMENT" : "OK";

        var report = new
        {
            schemaVersion = 2,
            generatedAt = now.ToString("o"),
            summary = new
            {
                status = global,
                nvidia = new { status = nvidiaStatus },
                cuda = new { status = cudaStatus },
                ports = new { status = portStatus },
                paths = new { status = pathStatus, missing = missing.Count },
                models = new { status = modelStatus }
            },
            system = new
            {
                machine = Environment.MachineName,
                windows = Environment.OSVersion.VersionString,
                packRoot = PortablePaths.Root,
                application = Environment.ProcessPath
            },
            nvidia = new { ok = gpu.Ok, raw = gpu.Text },
            cuda = new { raw = cuda.Text },
            ports = portData,
            missing = missing,
            paths = new
            {
                opencode = SafePath(s.OpenCodeExe),
                ollama = SafePath(s.OllamaExe),
                comfyPython = SafePath(s.ComfyPython),
                comfyMain = SafePath(s.ComfyMain),
                workspace = SafePath(s.Workspace),
                images = SafePath(s.Images),
                ollamaModels = SafePath(s.OllamaModels)
            }
        };

        var jsonPath = Path.Combine(PortablePaths.Root, $"diagnostic-{stamp}.json");
        var textPath = Path.Combine(PortablePaths.Root, $"diagnostic-{stamp}.txt");

        await File.WriteAllTextAsync(jsonPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        var txt = new System.Text.StringBuilder();
        txt.AppendLine("DreamRaster - Diagnostic");
        txt.AppendLine($"Date : {now:yyyy-MM-dd HH:mm:ss}");
        txt.AppendLine($"Pack : {PortablePaths.Root}");
        txt.AppendLine($"Global : {global}");
        txt.AppendLine($"NVIDIA : {nvidiaStatus}");
        txt.AppendLine($"CUDA : {cudaStatus}");
        txt.AppendLine($"Ports : {portStatus}");
        txt.AppendLine($"Chemins : {pathStatus}");
        txt.AppendLine($"Modèles : {modelStatus}");
        txt.AppendLine();
        txt.AppendLine("Composants manquants :");
        foreach (var item in missing)
            txt.AppendLine($"- {item.Label} : {item.Path}");
        txt.AppendLine();
        txt.AppendLine("NVIDIA :");
        txt.AppendLine(gpu.Text);
        txt.AppendLine();
        txt.AppendLine("CUDA :");
        txt.AppendLine(cuda.Text);

        await File.WriteAllTextAsync(textPath, txt.ToString());
        return (textPath, jsonPath);
    }

    private static object SafePath(string configured)
    {
        try
        {
            var p = PortablePaths.Resolve(configured);
            return new { configured, path = p, exists = File.Exists(p) || Directory.Exists(p), portable = true };
        }
        catch (Exception ex)
        {
            return new { configured, path = (string?)null, exists = false, portable = false, error = ex.Message };
        }
    }

    private static async Task<(bool Ok,string Text)> CaptureAsync(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p is null) return (false, "Impossible de lancer " + exe);
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            var text = (await stdout).Trim() + Environment.NewLine + (await stderr).Trim();
            return (p.ExitCode == 0, text.Trim());
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
