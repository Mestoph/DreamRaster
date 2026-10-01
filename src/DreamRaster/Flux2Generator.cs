/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Orchestration de génération FLUX.2 via l'API locale ComfyUI.
EN: FLUX.2 generation orchestration through the local ComfyUI API.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenCodeLocalAI;

public sealed record ImageGenerationResult(bool Ok, string? Path, string? Url, string? Error = null);

public sealed class Flux2Generator
{
    private readonly AppSettings _s;
    private readonly Func<Task> _ensureComfy;
    private readonly Func<Task> _stopComfy;
    private readonly Func<Task> _stopVision;
    private readonly Action<string,string> _log;
    private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly SemaphoreSlim _gate = new(1,1);

    public event Action<int,string>? ProgressChanged;

    public Flux2Generator(
        AppSettings settings,
        Func<Task> ensureComfy,
        Func<Task> stopComfy,
        Func<Task> stopVision,
        Action<string,string> log)
    {
        _s = settings;
        _ensureComfy = ensureComfy;
        _stopComfy = stopComfy;
        _stopVision = stopVision;
        _log = log;
    }

    public Task<ImageGenerationResult> GenerateAsync(
        string prompt, int width, int height, CancellationToken ct)
        => GenerateAsync(
            prompt,
            width,
            height,
            inputImagePath: null,
            imgToImgStrength: 1.0,
            ct);

    /*
    FR : Génère une image FLUX.2 en mode texte→image ou image→image selon la présence
         d'une image source.
    EN: Generates a FLUX.2 image in text-to-image or image-to-image mode depending
        on whether a source image is provided.
    */
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        int width,
        int height,
        string? inputImagePath,
        double imgToImgStrength,
        CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
            return new(false, null, null, "Une génération FLUX.2 est déjà en cours.");

        var stagedInputPath = (string?)null;

        try
        {
            try
            {
                width = Math.Clamp((int)Math.Round(width / 8d) * 8, 256, 1536);
                height = Math.Clamp((int)Math.Round(height / 8d) * 8, 256, 1536);

                var isImgToImg = !string.IsNullOrWhiteSpace(inputImagePath);

                if (isImgToImg && !File.Exists(inputImagePath))
                    throw new FileNotFoundException(
                        "Image source introuvable pour le mode image→image.",
                        inputImagePath);

                Progress(5, "Libération du modèle vision…");
                await _stopVision();
                await Task.Delay(1200, ct);
                await WaitForSafeMemoryAsync(ct);

                Progress(15, "Démarrage ComfyUI portable…");
                await _ensureComfy();

                var workflowPath = Path.Combine(
                    PortablePaths.WorkflowsDir,
                    isImgToImg
                        ? PortablePaths.ImgToImgWorkflowFile
                        : PortablePaths.TextToImageWorkflowFile);

                if (!File.Exists(workflowPath) && !isImgToImg)
                {
                    var legacy = Path.Combine(
                        PortablePaths.WorkflowsDir,
                        PortablePaths.LegacyWorkflowFile);

                    workflowPath = File.Exists(legacy)
                        ? legacy
                        : workflowPath;
                }

                if (!File.Exists(workflowPath))
                {
                    throw new FileNotFoundException(
                        isImgToImg
                            ? "Workflow FLUX.2 img-to-img absent. Placez flux2_img_to_img_api.json dans workflows."
                            : "Workflow FLUX.2 absent. Placez flux2_text_to_image_api.json ou flux2_workflow_api.json dans workflows.",
                        workflowPath);
                }

                var workflow =
                    JsonNode.Parse(await File.ReadAllTextAsync(workflowPath, ct))!
                        .AsObject();

                workflow["1"]!["inputs"]!["unet_name"] = _s.FluxModel;
                workflow["2"]!["inputs"]!["clip_name"] = _s.TextEncoderModel;
                workflow["3"]!["inputs"]!["vae_name"] = _s.VaeModel;
                workflow["4"]!["inputs"]!["text"] = prompt;

                if (workflow["6"]?["inputs"] is JsonObject w) w["value"] = width;
                if (workflow["7"]?["inputs"] is JsonObject h) h["value"] = height;
                if (workflow["11"]?["inputs"] is JsonObject st)
                {
                    st["steps"] = _s.DefaultSteps;

                    if (isImgToImg)
                    {
                        st["denoise"] =
                            Math.Clamp(imgToImgStrength, 0.05, 1.0);
                    }
                }

                if (workflow["9"]?["inputs"] is JsonObject seed)
                    seed["noise_seed"] = Random.Shared.NextInt64(1, long.MaxValue);

                if (isImgToImg)
                {
                    Progress(24, "Préparation de l'image source…");

                    var comfyInputDir =
                        Path.Combine(
                            PortablePreflight.GetComfyRoot(_s),
                            "input");

                    Directory.CreateDirectory(comfyInputDir);

                    var sourceExt =
                        Path.GetExtension(inputImagePath);

                    if (string.IsNullOrWhiteSpace(sourceExt))
                        sourceExt = ".png";

                    var inputFileName =
                        "OpenCode_FLUX2_input_" +
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ") +
                        sourceExt;

                    stagedInputPath =
                        Path.Combine(comfyInputDir, inputFileName);

                    File.Copy(inputImagePath!, stagedInputPath, true);

                    workflow["14"]!["inputs"]!["image"] = inputFileName;
                }

                var prefix =
                    (isImgToImg
                        ? "OpenCode_FLUX2_img2img_"
                        : "OpenCode_FLUX2_") +
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");

                if (workflow["15"]?["inputs"] is JsonObject save)
                    save["filename_prefix"] = prefix;

                Progress(30, "Envoi du prompt…");
                var body = JsonSerializer.Serialize(new
                {
                    prompt = workflow,
                    client_id = Guid.NewGuid().ToString()
                });

                using var response = await _http.PostAsync(
                    $"http://127.0.0.1:{_s.ComfyPort}/prompt",
                    new StringContent(body, Encoding.UTF8, "application/json"), ct);

                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"ComfyUI HTTP {response.StatusCode}: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var id = doc.RootElement.GetProperty("prompt_id").GetString()
                         ?? throw new InvalidOperationException("prompt_id absent.");

                var until = DateTime.UtcNow.AddMinutes(5);
                JsonElement job = default;
                var pct = 35;

                while (DateTime.UtcNow < until)
                {
                    ct.ThrowIfCancellationRequested();
                    using var hdoc = JsonDocument.Parse(
                        await _http.GetStringAsync(
                            $"http://127.0.0.1:{_s.ComfyPort}/history/{id}", ct));

                    if (hdoc.RootElement.TryGetProperty(id, out var j))
                    {
                        if (j.TryGetProperty("status", out var status) &&
                            status.TryGetProperty("completed", out var completed) &&
                            completed.GetBoolean())
                        {
                            job = j.Clone();
                            break;
                        }
                    }

                    pct = Math.Min(88, pct + 1);
                    Progress(
                        pct,
                        isImgToImg
                            ? "FLUX.2 transforme l'image…"
                            : "FLUX.2 calcule l'image…");

                    await Task.Delay(900, ct);
                }

                if (job.ValueKind == JsonValueKind.Undefined)
                    throw new TimeoutException("Timeout ComfyUI.");

                var source = FindOutput(job);
                if (source is null)
                    throw new FileNotFoundException("PNG final introuvable.");

                var images = PortablePaths.Resolve(_s.Images);
                Directory.CreateDirectory(images);
                var dest = Path.Combine(images, Path.GetFileName(source));
                File.Copy(source, dest, true);

                Progress(92, "Libération de la mémoire…");
                try
                {
                    await _http.PostAsync(
                        $"http://127.0.0.1:{_s.ComfyPort}/free",
                        new StringContent(
                            "{\"unload_models\":true,\"free_memory\":true}",
                            Encoding.UTF8,
                            "application/json"),
                        ct);
                }
                catch { }

                if (_s.HardStopComfyAfterGeneration)
                    await _stopComfy();

                await WaitForSafeMemoryAsync(ct);
                Progress(100, "Terminé.");
                return new(
                    true,
                    dest,
                    $"http://127.0.0.1:{_s.ImageProxyPort}/local-images/{Uri.EscapeDataString(Path.GetFileName(dest))}");
            }
            catch (Exception ex)
            {
                _log("FLUX !", ex.Message);
                try { if (_s.HardStopComfyAfterGeneration) await _stopComfy(); } catch { }
                return new(false, null, null, ex.Message);
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(stagedInputPath))
            {
                try
                {
                    if (File.Exists(stagedInputPath))
                        File.Delete(stagedInputPath);
                }
                catch { }
            }

            _gate.Release();
        }
    }

    private string? FindOutput(JsonElement job)
    {
        if (!job.TryGetProperty("outputs", out var outputs)) return null;
        foreach (var node in outputs.EnumerateObject())
        {
            if (!node.Value.TryGetProperty("images", out var imgs)) continue;
            foreach (var img in imgs.EnumerateArray())
            {
                var file = img.GetProperty("filename").GetString();
                var sub = img.TryGetProperty("subfolder", out var sf) ? sf.GetString() ?? "" : "";
                if (file is null) continue;
                var p = Path.Combine(PortablePreflight.GetComfyRoot(_s), "output", sub, file);
                if (File.Exists(p)) return p;
            }
        }
        return null;
    }

    private void Progress(int p, string t)
    {
        try { ProgressChanged?.Invoke(Math.Clamp(p, 0, 100), t); } catch { }
    }

    private async Task WaitForSafeMemoryAsync(CancellationToken ct)
    {
        var until = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();
            int? vram = null;
            try
            {
                var psi = new ProcessStartInfo(
                    "nvidia-smi",
                    "--query-gpu=memory.used --format=csv,noheader,nounits")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                if (p is not null)
                {
                    var s = await p.StandardOutput.ReadToEndAsync(ct);
                    await p.WaitForExitAsync(ct);
                    if (int.TryParse(s.Trim(), out var n)) vram = n;
                }
            }
            catch { }

            var ram = FreeRamMiB();
            if (vram is <= 1800 && ram is >= 4096) return;
            await Task.Delay(1000, ct);
        }

        throw new TimeoutException("La mémoire n'est pas revenue au seuil sûr.");
    }

    private static long? FreeRamMiB()
    {
        var m = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        return GlobalMemoryStatusEx(ref m) ? (long)(m.AvailPhys / 1048576UL) : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
