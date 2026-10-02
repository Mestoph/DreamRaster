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
            _s.NegativePrompt,
            width,
            height,
            inputImagePath: null,
            imgToImgStrength: 1.0,
            ct,
            seed: null);

    /*
    FR : Génère une image FLUX.2 en mode texte→image ou image→image selon la présence
         d'une image source.
    EN: Generates a FLUX.2 image in text-to-image or image-to-image mode depending
        on whether a source image is provided.
    */
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        string? negativePrompt,
        int width,
        int height,
        string? inputImagePath,
        double imgToImgStrength,
        CancellationToken ct,
        long? seed = null)
    {
        if (!await _gate.WaitAsync(0, ct))
            return new(false, null, null, "Une génération FLUX.2 est déjà en cours.");

        var stagedInputPath = (string?)null;

        try
        {
            try
            {
                width = Math.Clamp((int)Math.Round(width / 16d) * 16, 256, 1536);
                height = Math.Clamp((int)Math.Round(height / 16d) * 16, 256, 1536);

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

                ConfigureOfficialKleinWorkflow(
                    workflow,
                    prompt,
                    negativePrompt,
                    width,
                    height,
                    isImgToImg);

                var seedValue =
                    seed is > 0
                        ? seed.Value
                        : Random.Shared.NextInt64(1, long.MaxValue);

                SetRequiredInput(
                    workflow,
                    "RandomNoise",
                    "noise_seed",
                    JsonValue.Create(seedValue));

                _log(
                    "FLUX",
                    $"Seed : {seedValue} · {width}x{height} · {_s.DefaultSteps} steps.");

                if (isImgToImg)
                {
                    Progress(24, "Préparation de l'image de référence…");

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
                        "DreamRaster_FLUX2_reference_" +
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ") +
                        sourceExt;

                    stagedInputPath =
                        Path.Combine(comfyInputDir, inputFileName);

                    File.Copy(inputImagePath!, stagedInputPath, true);

                    SetRequiredInput(
                        workflow,
                        "LoadImage",
                        "image",
                        JsonValue.Create(inputFileName));

                    _log(
                        "FLUX",
                        "Image Edit officiel : ReferenceLatent actif. " +
                        $"Le paramètre legacy strength={Math.Clamp(imgToImgStrength, 0.05, 1.0):0.00} " +
                        "n'est pas appliqué par le workflow ComfyUI officiel.");
                }

                var prefix =
                    (isImgToImg
                        ? "DreamRaster_FLUX2_img2img_"
                        : "DreamRaster_FLUX2_") +
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffZ");

                Progress(30, "Sampling FLUX.2…");
                var source = await RunSplitDecodeWorkflowAsync(
                    workflow,
                    prefix,
                    isImgToImg,
                    ct);

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

    private async Task<string> RunSplitDecodeWorkflowAsync(
        JsonObject configuredWorkflow,
        string imagePrefix,
        bool isImgToImg,
        CancellationToken ct)
    {
        var comfyRoot = PortablePreflight.GetComfyRoot(_s);
        var outputRoot = Path.Combine(comfyRoot, "output");
        var inputRoot = Path.Combine(comfyRoot, "input");
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(inputRoot);

        string? latentOutputPath = null;
        string? latentInputPath = null;

        try
        {
            var phase1 = configuredWorkflow.DeepClone().AsObject();

            RemoveNodesByClassType(
                phase1,
                "VAEDecode",
                "SaveImage");

            if (!isImgToImg)
                RemoveNodesByClassType(phase1, "VAELoader");

            var samplerId =
                RequireNodeId(
                    phase1,
                    "SamplerCustomAdvanced");

            var saveLatentId =
                NextNumericNodeId(phase1);

            phase1[saveLatentId] =
                new JsonObject
                {
                    ["class_type"] = "SaveLatent",
                    ["inputs"] =
                        new JsonObject
                        {
                            ["samples"] =
                                new JsonArray(
                                    JsonValue.Create(samplerId),
                                    JsonValue.Create(0)),
                            ["filename_prefix"] =
                                JsonValue.Create(
                                    "latents/DreamRaster_FLUX2_intermediate_" +
                                    Guid.NewGuid().ToString("N"))
                        }
                };

            var phase1Job =
                await QueueWorkflowAndWaitAsync(
                    phase1,
                    TimeSpan.FromMinutes(5),
                    32,
                    68,
                    isImgToImg
                        ? "FLUX.2 transforme l'image…"
                        : "FLUX.2 calcule le latent…",
                    ct);

            latentOutputPath =
                FindComfyOutput(
                    phase1Job,
                    "latents",
                    ".latent");

            if (latentOutputPath is null)
                throw new FileNotFoundException(
                    "Latent intermédiaire FLUX.2 introuvable.");

            var latentInputName =
                "DreamRaster_FLUX2_intermediate_" +
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
                72,
                "Libération de Qwen et FLUX avant le VAE…");

            await FreeComfyModelsAsync(ct);
            await WaitForSafeMemoryAsync(ct);

            var phase2 =
                new JsonObject
                {
                    ["1"] =
                        new JsonObject
                        {
                            ["class_type"] = "LoadLatent",
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
                            ["class_type"] = "VAELoader",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["vae_name"] =
                                        JsonValue.Create(
                                            _s.VaeModel)
                                }
                        },
                    ["3"] =
                        new JsonObject
                        {
                            ["class_type"] = "VAEDecode",
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
                            ["class_type"] = "SaveImage",
                            ["inputs"] =
                                new JsonObject
                                {
                                    ["filename_prefix"] =
                                        JsonValue.Create(
                                            imagePrefix),
                                    ["images"] =
                                        new JsonArray(
                                            JsonValue.Create("3"),
                                            JsonValue.Create(0))
                                }
                        }
                };

            Progress(
                78,
                "Décodage VAE…");

            var phase2Job =
                await QueueWorkflowAndWaitAsync(
                    phase2,
                    TimeSpan.FromMinutes(3),
                    80,
                    90,
                    "Décodage de l'image…",
                    ct);

            return
                FindComfyOutput(
                    phase2Job,
                    "images",
                    ".png")
                ?? throw new FileNotFoundException(
                    "PNG final introuvable.");
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
                    client_id = Guid.NewGuid().ToString()
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
            await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"ComfyUI HTTP {response.StatusCode}: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var id =
            doc.RootElement
                .GetProperty("prompt_id")
                .GetString()
            ?? throw new InvalidOperationException(
                "prompt_id absent.");

        var until =
            DateTime.UtcNow + timeout;

        var progress =
            progressStart;

        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();

            using var hdoc =
                JsonDocument.Parse(
                    await _http.GetStringAsync(
                        $"http://127.0.0.1:{_s.ComfyPort}/history/{id}",
                        ct));

            if (hdoc.RootElement.TryGetProperty(
                    id,
                    out var job))
            {
                if (TryGetComfyFailure(
                        job,
                        out var comfyError))
                {
                    throw new InvalidOperationException(
                        comfyError);
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
                700,
                ct);
        }

        throw new TimeoutException(
            "Timeout ComfyUI.");
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
                    ? "Modèles FLUX/Qwen libérés avant décodage VAE."
                    : $"Libération modèles : HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _log(
                "ComfyUI !",
                "Libération modèles avant VAE : " +
                ex.Message);
        }

        await Task.Delay(
            800,
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
            return null;

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
                    continue;

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
                        ? subfolderNode.GetString() ?? string.Empty
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
                    node["class_type"]?.GetValue<string>(),
                    classType,
                    StringComparison.Ordinal))
            {
                return entry.Key;
            }
        }

        throw new InvalidDataException(
            $"Workflow FLUX.2 Klein incompatible : nœud {classType} absent.");
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
                        node["class_type"]?.GetValue<string>(),
                        StringComparer.Ordinal))
                .Select(entry => entry.Key)
                .ToArray();

        foreach (var key in remove)
            workflow.Remove(key);
    }

    private void ConfigureOfficialKleinWorkflow(
        JsonObject workflow,
        string prompt,
        string? negativePrompt,
        int width,
        int height,
        bool isImgToImg)
    {
        SetRequiredInput(
            workflow,
            "UNETLoader",
            "unet_name",
            JsonValue.Create(_s.FluxModel));

        SetRequiredInput(
            workflow,
            "UNETLoader",
            "weight_dtype",
            JsonValue.Create("default"));

        SetRequiredInput(
            workflow,
            "CLIPLoader",
            "clip_name",
            JsonValue.Create(_s.TextEncoderModel));

        // FLUX.2 Klein uses the Qwen3 FLUX.2 conditioning path.
        SetRequiredInput(
            workflow,
            "CLIPLoader",
            "type",
            JsonValue.Create("flux2"));

        SetRequiredInput(
            workflow,
            "CLIPLoader",
            "device",
            JsonValue.Create("default"));

        SetRequiredInput(
            workflow,
            "VAELoader",
            "vae_name",
            JsonValue.Create(_s.VaeModel));

        var textEncoders = FindNodeInputs(workflow, "CLIPTextEncode");
        if (textEncoders.Count == 0)
            throw new InvalidDataException(
                "Workflow FLUX.2 Klein incompatible : nœud CLIPTextEncode absent.");

        var positivePrompt = prompt;
        if (!string.IsNullOrWhiteSpace(negativePrompt))
        {
            if (textEncoders.Count > 1)
            {
                textEncoders[0]["text"] = JsonValue.Create(prompt);
                textEncoders[1]["text"] = JsonValue.Create(negativePrompt);
            }
            else
            {
                // Klein Distilled has no separate negative-conditioning input.
                // Qwen receives the exclusions as explicit natural-language instructions.
                positivePrompt =
                    prompt + Environment.NewLine + Environment.NewLine +
                    "Avoid: " + negativePrompt.Trim();
                textEncoders[0]["text"] = JsonValue.Create(positivePrompt);
                _log(
                    "FLUX",
                    "Klein Distilled : le négatif prompt est transmis à Qwen comme instruction 'Avoid', " +
                    "car le workflow officiel Distilled n'expose pas de conditioning négatif séparé.");
            }
        }
        else
        {
            textEncoders[0]["text"] = JsonValue.Create(prompt);
        }

        SetRequiredInput(
            workflow,
            "CFGGuider",
            "cfg",
            JsonValue.Create(1.0));

        SetRequiredInput(
            workflow,
            "KSamplerSelect",
            "sampler_name",
            JsonValue.Create("euler"));

        SetRequiredInput(
            workflow,
            "Flux2Scheduler",
            "steps",
            JsonValue.Create(Math.Max(1, _s.DefaultSteps)));

        if (!isImgToImg)
        {
            SetRequiredInput(
                workflow,
                "Flux2Scheduler",
                "width",
                JsonValue.Create(width));

            SetRequiredInput(
                workflow,
                "Flux2Scheduler",
                "height",
                JsonValue.Create(height));

            SetRequiredInput(
                workflow,
                "EmptyFlux2LatentImage",
                "width",
                JsonValue.Create(width));

            SetRequiredInput(
                workflow,
                "EmptyFlux2LatentImage",
                "height",
                JsonValue.Create(height));

            SetRequiredInput(
                workflow,
                "EmptyFlux2LatentImage",
                "batch_size",
                JsonValue.Create(1));

            RequireNodeInputs(workflow, "ConditioningZeroOut");
            RequireNodeInputs(workflow, "SamplerCustomAdvanced");
            return;
        }

        SetRequiredInput(
            workflow,
            "ImageScale",
            "upscale_method",
            JsonValue.Create("lanczos"));

        SetRequiredInput(
            workflow,
            "ImageScale",
            "width",
            JsonValue.Create(width));

        SetRequiredInput(
            workflow,
            "ImageScale",
            "height",
            JsonValue.Create(height));

        SetRequiredInput(
            workflow,
            "ImageScale",
            "crop",
            JsonValue.Create("center"));

        RequireNodeInputs(workflow, "LoadImage");
        RequireNodeInputs(workflow, "GetImageSize");
        RequireNodeInputs(workflow, "VAEEncode");
        RequireNodeInputs(workflow, "ReferenceLatent");
        RequireNodeInputs(workflow, "ConditioningZeroOut");
        RequireNodeInputs(workflow, "SamplerCustomAdvanced");
    }

    private static List<JsonObject> FindNodeInputs(
        JsonObject workflow,
        string classType)
    {
        var result = new List<JsonObject>();

        foreach (var entry in workflow)
        {
            if (entry.Value is not JsonObject node)
                continue;

            var type =
                node["class_type"]?.GetValue<string>();

            if (!string.Equals(
                    type,
                    classType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (node["inputs"] is JsonObject inputs)
                result.Add(inputs);
        }

        return result;
    }

    private static JsonObject RequireNodeInputs(
        JsonObject workflow,
        string classType)
    {
        var nodes =
            FindNodeInputs(workflow, classType);

        if (nodes.Count == 0)
        {
            throw new InvalidDataException(
                $"Workflow FLUX.2 Klein incompatible : nœud {classType} absent.");
        }

        return nodes[0];
    }

    private static void SetRequiredInput(
        JsonObject workflow,
        string classType,
        string inputName,
        JsonNode? value)
    {
        var nodes =
            FindNodeInputs(workflow, classType);

        if (nodes.Count == 0)
        {
            throw new InvalidDataException(
                $"Workflow FLUX.2 Klein incompatible : nœud {classType} absent.");
        }

        foreach (var inputs in nodes)
            inputs[inputName] = value?.DeepClone();
    }

    private static bool TryGetComfyFailure(
        JsonElement job,
        out string error)
    {
        error = string.Empty;

        if (!job.TryGetProperty("status", out var status))
            return false;

        var statusIsError =
            status.TryGetProperty("status_str", out var statusText) &&
            string.Equals(
                statusText.GetString(),
                "error",
                StringComparison.OrdinalIgnoreCase);

        if (status.TryGetProperty("messages", out var messages) &&
            messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var message in messages.EnumerateArray())
            {
                if (message.ValueKind != JsonValueKind.Array)
                    continue;

                var parts =
                    message.EnumerateArray().ToArray();

                if (parts.Length < 2 ||
                    parts[0].ValueKind != JsonValueKind.String ||
                    !string.Equals(
                        parts[0].GetString(),
                        "execution_error",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var details = parts[1];

                if (details.ValueKind == JsonValueKind.Object)
                {
                    var exception =
                        details.TryGetProperty("exception_message", out var ex)
                            ? ex.GetString()
                            : null;

                    var nodeType =
                        details.TryGetProperty("node_type", out var nt)
                            ? nt.GetString()
                            : null;

                    var nodeId =
                        details.TryGetProperty("node_id", out var ni)
                            ? ni.ToString()
                            : null;

                    var where =
                        !string.IsNullOrWhiteSpace(nodeType)
                            ? $" [{nodeType}" +
                              (!string.IsNullOrWhiteSpace(nodeId)
                                  ? $" #{nodeId}]"
                                  : "]")
                            : string.Empty;

                    error =
                        "ComfyUI" +
                        where +
                        " : " +
                        (string.IsNullOrWhiteSpace(exception)
                            ? "erreur d'exécution."
                            : exception);

                    return true;
                }

                error = "ComfyUI : erreur d'exécution.";
                return true;
            }
        }

        if (statusIsError)
        {
            error = "ComfyUI a signalé une erreur d'exécution.";
            return true;
        }

        return false;
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
