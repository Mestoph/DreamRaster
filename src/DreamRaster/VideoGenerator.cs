/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Génération vidéo locale Wan 2.1 via le ComfyUI portable.
EN: Local Wan 2.1 video generation through portable ComfyUI.
*/

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenCodeLocalAI;

public sealed record VideoGenerationResult(
    bool Ok,
    string? Path,
    string? Error = null);

public sealed class VideoGenerator
{
    private readonly AppSettings _s;
    private readonly Func<Task> _ensureComfy;
    private readonly Func<Task> _stopComfy;
    private readonly Action<string, string> _log;
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public event Action<int, string>? ProgressChanged;

    public VideoGenerator(
        AppSettings settings,
        Func<Task> ensureComfy,
        Func<Task> stopComfy,
        Action<string, string> log)
    {
        _s = settings;
        _ensureComfy = ensureComfy;
        _stopComfy = stopComfy;
        _log = log;
    }

    public IReadOnlyList<(string Label, string Path)> GetMissingModels()
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var checks = new[]
        {
            (
                "Wan 2.1 T2V 1.3B",
                Path.Combine(
                    root,
                    "models",
                    "diffusion_models",
                    _s.VideoModel)),
            (
                "UMT5 XXL",
                Path.Combine(
                    root,
                    "models",
                    "text_encoders",
                    _s.VideoTextEncoderModel)),
            (
                "Wan VAE",
                Path.Combine(
                    root,
                    "models",
                    "vae",
                    _s.VideoVaeModel))
        };

        return checks
            .Where(x => !File.Exists(x.Item2))
            .Select(x => (x.Item1, x.Item2))
            .ToArray();
    }

    public async Task<VideoGenerationResult> GenerateAsync(
        string prompt,
        string negativePrompt,
        int width,
        int height,
        int frames,
        double fps,
        int steps,
        CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
        {
            return new(
                false,
                null,
                "Une génération vidéo est déjà en cours.");
        }

        try
        {
            var missing = GetMissingModels();
            if (missing.Count > 0)
            {
                return new(
                    false,
                    null,
                    "Modèles vidéo manquants :\n" +
                    string.Join(
                        "\n",
                        missing.Select(x =>
                            $"• {x.Label} : {x.Path}")));
            }

            width = Math.Clamp(
                (int)Math.Round(width / 16d) * 16,
                256,
                1280);
            height = Math.Clamp(
                (int)Math.Round(height / 16d) * 16,
                256,
                1280);
            frames = Math.Clamp(
                1 + (int)Math.Round((frames - 1) / 4d) * 4,
                1,
                161);
            fps = Math.Clamp(fps, 1d, 60d);
            steps = Math.Clamp(steps, 1, 100);

            Progress(4, "Vérification de ComfyUI…");
            await _ensureComfy();

            var workflowPath = Path.Combine(
                PortablePaths.WorkflowsDir,
                PortablePaths.WanTextToVideoWorkflowFile);

            if (!File.Exists(workflowPath))
            {
                return new(
                    false,
                    null,
                    "Workflow vidéo Wan absent : " + workflowPath);
            }

            var workflow =
                JsonNode.Parse(
                    await File.ReadAllTextAsync(workflowPath, ct))
                ?.AsObject()
                ?? throw new InvalidDataException(
                    "Workflow vidéo Wan JSON invalide.");

            SetInput(
                workflow,
                "UNETLoader",
                "unet_name",
                JsonValue.Create(_s.VideoModel));
            SetInput(
                workflow,
                "UNETLoader",
                "weight_dtype",
                JsonValue.Create("default"));

            SetInput(
                workflow,
                "CLIPLoader",
                "clip_name",
                JsonValue.Create(_s.VideoTextEncoderModel));
            SetInput(
                workflow,
                "CLIPLoader",
                "type",
                JsonValue.Create("wan"));
            SetInput(
                workflow,
                "CLIPLoader",
                "device",
                JsonValue.Create("default"));

            SetInput(
                workflow,
                "VAELoader",
                "vae_name",
                JsonValue.Create(_s.VideoVaeModel));

            SetInputById(
                workflow,
                "4",
                "text",
                JsonValue.Create(prompt));
            SetInputById(
                workflow,
                "5",
                "text",
                JsonValue.Create(negativePrompt));

            SetInput(
                workflow,
                "EmptyHunyuanLatentVideo",
                "width",
                JsonValue.Create(width));
            SetInput(
                workflow,
                "EmptyHunyuanLatentVideo",
                "height",
                JsonValue.Create(height));
            SetInput(
                workflow,
                "EmptyHunyuanLatentVideo",
                "length",
                JsonValue.Create(frames));
            SetInput(
                workflow,
                "EmptyHunyuanLatentVideo",
                "batch_size",
                JsonValue.Create(1));

            SetInput(
                workflow,
                "ModelSamplingSD3",
                "shift",
                JsonValue.Create(8.0));

            SetInput(
                workflow,
                "KSampler",
                "seed",
                JsonValue.Create(
                    Random.Shared.NextInt64(1, long.MaxValue)));
            SetInput(
                workflow,
                "KSampler",
                "steps",
                JsonValue.Create(steps));
            SetInput(
                workflow,
                "KSampler",
                "cfg",
                JsonValue.Create(6.0));
            SetInput(
                workflow,
                "KSampler",
                "sampler_name",
                JsonValue.Create("uni_pc"));
            SetInput(
                workflow,
                "KSampler",
                "scheduler",
                JsonValue.Create("simple"));
            SetInput(
                workflow,
                "KSampler",
                "denoise",
                JsonValue.Create(1.0));

            SetInput(
                workflow,
                "CreateVideo",
                "fps",
                JsonValue.Create(fps));

            var prefix =
                "video/DreamRaster_WAN_" +
                DateTime.UtcNow.ToString(
                    "yyyy-MM-ddTHH-mm-ss-fffZ");

            SetInput(
                workflow,
                "SaveVideo",
                "filename_prefix",
                JsonValue.Create(prefix));
            SetInput(
                workflow,
                "SaveVideo",
                "format",
                JsonValue.Create("auto"));

            Progress(
                12,
                "Wan calcule le latent vidéo…");

            var source =
                await RunSplitDecodeWorkflowAsync(
                    workflow,
                    prefix,
                    fps,
                    ct);

            var videos = Path.Combine(
                PortablePaths.Root,
                _s.Videos);
            Directory.CreateDirectory(videos);

            var destination = Path.Combine(
                videos,
                Path.GetFileName(source));

            File.Copy(
                source,
                destination,
                true);

            await FreeComfyModelsAsync(ct);

            Progress(
                100,
                "Vidéo terminée.");

            _log(
                "Video",
                "Vidéo Wan enregistrée : " +
                destination);

            return new(
                true,
                destination);
        }
        catch (Exception ex)
        {
            _log("Video !", ex.Message);
            return new(false, null, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> RunSplitDecodeWorkflowAsync(
        JsonObject configuredWorkflow,
        string videoPrefix,
        double fps,
        CancellationToken ct)
    {
        var comfyRoot =
            PortablePreflight.GetComfyRoot(_s);
        var outputRoot =
            Path.Combine(
                comfyRoot,
                "output");
        var inputRoot =
            Path.Combine(
                comfyRoot,
                "input");

        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(inputRoot);

        string? latentOutputPath = null;
        string? latentInputPath = null;

        try
        {
            var phase1 =
                configuredWorkflow
                    .DeepClone()
                    .AsObject();

            RemoveNodesByClassType(
                phase1,
                "VAELoader",
                "VAEDecode",
                "CreateVideo",
                "SaveVideo");

            var samplerId =
                RequireNodeId(
                    phase1,
                    "KSampler");

            var saveLatentId =
                NextNumericNodeId(
                    phase1);

            phase1[saveLatentId] =
                new JsonObject
                {
                    ["class_type"] =
                        "SaveLatent",
                    ["inputs"] =
                        new JsonObject
                        {
                            ["samples"] =
                                new JsonArray(
                                    JsonValue.Create(
                                        samplerId),
                                    JsonValue.Create(0)),
                            ["filename_prefix"] =
                                JsonValue.Create(
                                    "latents/DreamRaster_WAN_intermediate_" +
                                    Guid.NewGuid()
                                        .ToString("N"))
                        }
                };

            var phase1Job =
                await QueueWorkflowAndWaitAsync(
                    phase1,
                    TimeSpan.FromMinutes(20),
                    16,
                    68,
                    "Wan génère le latent vidéo…",
                    ct);

            latentOutputPath =
                FindComfyOutput(
                    phase1Job,
                    "latents",
                    ".latent");

            if (latentOutputPath is null)
            {
                throw new FileNotFoundException(
                    "Latent intermédiaire Wan introuvable.");
            }

            var latentInputName =
                "DreamRaster_WAN_intermediate_" +
                Guid.NewGuid().ToString("N") +
                ".latent";

            latentInputPath =
                Path.Combine(
                    inputRoot,
                    latentInputName);

            File.Copy(
                latentOutputPath,
                latentInputPath,
                overwrite: true);

            Progress(
                70,
                "Libération de Wan et UMT5 avant le VAE…");

            await FreeComfyModelsAsync(ct);

            // /free releases model objects, but on Windows with a very
            // small page file the process can keep enough committed
            // address space to make the following VAE allocation fail.
            // A clean process restart gives the decode phase a fresh
            // commit budget.
            await _stopComfy();

            await Task.Delay(
                800,
                ct);

            Progress(
                74,
                "Redémarrage ComfyUI pour le décodage VAE…");

            await _ensureComfy();

            var phase2 =
                new JsonObject
                {
                    ["1"] =
                        new JsonObject
                        {
                            ["class_type"] =
                                "LoadLatent",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["latent"] =
                                        JsonValue.Create(
                                            latentInputName)
                                }
                        },
                    ["2"] =
                        new JsonObject
                        {
                            ["class_type"] =
                                "VAELoader",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["vae_name"] =
                                        JsonValue.Create(
                                            _s.VideoVaeModel)
                                }
                        },
                    ["3"] =
                        new JsonObject
                        {
                            ["class_type"] =
                                "VAEDecode",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["samples"] =
                                        new JsonArray(
                                            JsonValue.Create("1"),
                                            JsonValue.Create(0)),
                                    ["vae"] =
                                        new JsonArray(
                                            JsonValue.Create("2"),
                                            JsonValue.Create(0))
                                }
                        },
                    ["4"] =
                        new JsonObject
                        {
                            ["class_type"] =
                                "CreateVideo",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["images"] =
                                        new JsonArray(
                                            JsonValue.Create("3"),
                                            JsonValue.Create(0)),
                                    ["fps"] =
                                        JsonValue.Create(fps)
                                }
                        },
                    ["5"] =
                        new JsonObject
                        {
                            ["class_type"] =
                                "SaveVideo",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["video"] =
                                        new JsonArray(
                                            JsonValue.Create("4"),
                                            JsonValue.Create(0)),
                                    ["filename_prefix"] =
                                        JsonValue.Create(
                                            videoPrefix),
                                    ["format"] =
                                        JsonValue.Create("auto")
                                }
                        }
                };

            Progress(
                80,
                "Décodage VAE et encodage vidéo…");

            var phase2Job =
                await QueueWorkflowAndWaitAsync(
                    phase2,
                    TimeSpan.FromMinutes(8),
                    82,
                    96,
                    "Décodage et encodage MP4…",
                    ct);

            return
                FindVideoOutput(
                    phase2Job)
                ?? throw new FileNotFoundException(
                    "Vidéo finale introuvable dans l'historique ComfyUI.");
        }
        finally
        {
            foreach (var path in new[]
                     {
                         latentInputPath,
                         latentOutputPath
                     })
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                }
            }
        }
    }

    private async Task<JsonElement> QueueWorkflowAndWaitAsync(
        JsonObject workflow,
        TimeSpan timeout,
        int progressStart,
        int progressEnd,
        string progressText,
        CancellationToken ct)
    {
        var body =
            JsonSerializer.Serialize(
                new
                {
                    prompt = workflow,
                    client_id =
                        Guid.NewGuid()
                            .ToString()
                });

        using var response =
            await _http.PostAsync(
                $"http://127.0.0.1:{_s.ComfyPort}/prompt",
                new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json"),
                ct);

        var raw =
            await response.Content
                .ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"ComfyUI HTTP {response.StatusCode}: {raw}");
        }

        using var promptDoc =
            JsonDocument.Parse(raw);

        var promptId =
            promptDoc.RootElement
                .GetProperty("prompt_id")
                .GetString()
            ?? throw new InvalidOperationException(
                "prompt_id vidéo absent.");

        var deadline =
            DateTime.UtcNow + timeout;
        var progress =
            progressStart;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            using var historyDoc =
                JsonDocument.Parse(
                    await _http.GetStringAsync(
                        $"http://127.0.0.1:{_s.ComfyPort}/history/{promptId}",
                        ct));

            if (historyDoc.RootElement.TryGetProperty(
                    promptId,
                    out var job))
            {
                if (TryGetFailure(
                        job,
                        out var error))
                {
                    throw new InvalidOperationException(
                        error);
                }

                if (job.TryGetProperty(
                        "status",
                        out var status) &&
                    status.TryGetProperty(
                        "completed",
                        out var completed) &&
                    completed.ValueKind ==
                        JsonValueKind.True)
                {
                    return job.Clone();
                }
            }

            progress =
                Math.Min(
                    progressEnd,
                    progress + 1);

            Progress(
                progress,
                progressText);

            await Task.Delay(
                1000,
                ct);
        }

        throw new TimeoutException(
            "Timeout ComfyUI pendant la génération vidéo.");
    }

    private async Task FreeComfyModelsAsync(
        CancellationToken ct)
    {
        try
        {
            using var response =
                await _http.PostAsync(
                    $"http://127.0.0.1:{_s.ComfyPort}/free",
                    new StringContent(
                        "{\"unload_models\":true,\"free_memory\":true}",
                        Encoding.UTF8,
                        "application/json"),
                    ct);

            _log(
                "ComfyUI",
                response.IsSuccessStatusCode
                    ? "Mémoire des modèles vidéo ComfyUI libérée."
                    : $"Libération modèles vidéo : HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            _log(
                "ComfyUI !",
                "Libération modèles vidéo avant VAE : " +
                ex.Message);
        }

        await Task.Delay(
            600,
            ct);
    }

    private string? FindComfyOutput(
        JsonElement job,
        string collectionName,
        string requiredExtension)
    {
        if (!job.TryGetProperty(
                "outputs",
                out var outputs))
        {
            return null;
        }

        foreach (var node in outputs.EnumerateObject())
        {
            if (!node.Value.TryGetProperty(
                    collectionName,
                    out var items) ||
                items.ValueKind !=
                    JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty(
                        "filename",
                        out var filenameNode))
                {
                    continue;
                }

                var filename =
                    filenameNode.GetString();

                if (string.IsNullOrWhiteSpace(filename) ||
                    !filename.EndsWith(
                        requiredExtension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var subfolder =
                    item.TryGetProperty(
                        "subfolder",
                        out var subfolderNode)
                        ? subfolderNode.GetString() ??
                          string.Empty
                        : string.Empty;

                var type =
                    item.TryGetProperty(
                        "type",
                        out var typeNode)
                        ? typeNode.GetString()
                        : "output";

                var root =
                    string.Equals(
                        type,
                        "input",
                        StringComparison.OrdinalIgnoreCase)
                        ? Path.Combine(
                            PortablePreflight.GetComfyRoot(_s),
                            "input")
                        : Path.Combine(
                            PortablePreflight.GetComfyRoot(_s),
                            "output");

                var path =
                    Path.Combine(
                        root,
                        subfolder,
                        filename);

                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }

    private static string RequireNodeId(
        JsonObject workflow,
        string classType)
    {
        foreach (var entry in workflow)
        {
            if (entry.Value is not JsonObject node)
                continue;

            if (string.Equals(
                    node["class_type"]
                        ?.GetValue<string>(),
                    classType,
                    StringComparison.Ordinal))
            {
                return entry.Key;
            }
        }

        throw new InvalidDataException(
            $"Workflow Wan incompatible : nœud {classType} absent.");
    }

    private static string NextNumericNodeId(
        JsonObject workflow)
    {
        var max =
            workflow
                .Select(entry =>
                    int.TryParse(
                        entry.Key,
                        out var value)
                        ? value
                        : 0)
                .DefaultIfEmpty(0)
                .Max();

        return
            (max + 1)
            .ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void RemoveNodesByClassType(
        JsonObject workflow,
        params string[] classTypes)
    {
        var remove =
            workflow
                .Where(entry =>
                    entry.Value is JsonObject node &&
                    classTypes.Contains(
                        node["class_type"]
                            ?.GetValue<string>(),
                        StringComparer.Ordinal))
                .Select(entry => entry.Key)
                .ToArray();

        foreach (var key in remove)
            workflow.Remove(key);
    }

    private static void SetInput(
        JsonObject workflow,
        string classType,
        string input,
        JsonNode? value)
    {
        var found = false;

        foreach (var entry in workflow)
        {
            if (entry.Value is not JsonObject node ||
                !string.Equals(
                    node["class_type"]?.GetValue<string>(),
                    classType,
                    StringComparison.Ordinal) ||
                node["inputs"] is not JsonObject inputs)
            {
                continue;
            }

            inputs[input] = value?.DeepClone();
            found = true;
        }

        if (!found)
        {
            throw new InvalidDataException(
                $"Workflow Wan incompatible : nœud {classType} absent.");
        }
    }

    private static void SetInputById(
        JsonObject workflow,
        string nodeId,
        string input,
        JsonNode? value)
    {
        if (workflow[nodeId] is not JsonObject node ||
            node["inputs"] is not JsonObject inputs)
        {
            throw new InvalidDataException(
                $"Workflow Wan incompatible : nœud {nodeId} absent.");
        }

        inputs[input] = value?.DeepClone();
    }

    private static bool TryGetFailure(
        JsonElement job,
        out string error)
    {
        error = string.Empty;

        if (!job.TryGetProperty(
                "status",
                out var status))
        {
            return false;
        }

        if (status.TryGetProperty(
                "status_str",
                out var statusText) &&
            string.Equals(
                statusText.GetString(),
                "error",
                StringComparison.OrdinalIgnoreCase))
        {
            error =
                "ComfyUI a signalé une erreur pendant la génération vidéo.";

            if (status.TryGetProperty(
                    "messages",
                    out var messages) &&
                messages.ValueKind ==
                    JsonValueKind.Array)
            {
                foreach (var message in
                         messages.EnumerateArray())
                {
                    if (message.ValueKind !=
                        JsonValueKind.Array)
                        continue;

                    var parts =
                        message.EnumerateArray().ToArray();

                    if (parts.Length < 2 ||
                        parts[0].GetString() !=
                            "execution_error" ||
                        parts[1].ValueKind !=
                            JsonValueKind.Object)
                        continue;

                    if (parts[1].TryGetProperty(
                            "exception_message",
                            out var exception))
                    {
                        error =
                            "ComfyUI vidéo : " +
                            exception.GetString();
                    }
                }
            }

            return true;
        }

        return false;
    }

    private string? FindVideoOutput(JsonElement job)
    {
        if (!job.TryGetProperty(
                "outputs",
                out var outputs))
        {
            return null;
        }

        foreach (var node in outputs.EnumerateObject())
        {
            if (!node.Value.TryGetProperty(
                    "images",
                    out var files) ||
                files.ValueKind !=
                    JsonValueKind.Array)
            {
                continue;
            }

            foreach (var file in
                     files.EnumerateArray())
            {
                var name =
                    file.TryGetProperty(
                        "filename",
                        out var filename)
                        ? filename.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(name) ||
                    !(name.EndsWith(
                          ".mp4",
                          StringComparison.OrdinalIgnoreCase) ||
                      name.EndsWith(
                          ".webm",
                          StringComparison.OrdinalIgnoreCase) ||
                      name.EndsWith(
                          ".mkv",
                          StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var subfolder =
                    file.TryGetProperty(
                        "subfolder",
                        out var folder)
                        ? folder.GetString() ?? string.Empty
                        : string.Empty;

                var path = Path.Combine(
                    PortablePreflight.GetComfyRoot(_s),
                    "output",
                    subfolder,
                    name);

                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }

    private void Progress(int value, string text)
    {
        try
        {
            ProgressChanged?.Invoke(
                Math.Clamp(value, 0, 100),
                text);
        }
        catch
        {
        }
    }
}
