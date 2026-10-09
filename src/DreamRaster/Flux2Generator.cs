/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Orchestration de génération FLUX.2 via l'API locale ComfyUI.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit record « ImageGenerationResult », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed record ImageGenerationResult(bool Ok, string? Path, string? Url, string? Error = null);

/// <summary>

/// Définit class « Flux2Generator », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed class Flux2Generator
{
    /// <summary>
    /// Stocke « _s », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly AppSettings _s;
    /// <summary>
    /// Stocke « _ensureComfy », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly Func<Task> _ensureComfy;
    /// <summary>
    /// Stocke « _stopComfy », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly Func<Task> _stopComfy;
    /// <summary>
    /// Stocke « _stopVision », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly Func<Task> _stopVision;
    /// <summary>
    /// Stocke « _log », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly Action<string,string> _log;
    /// <summary>
    /// Stocke « _http », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
    /// <summary>
    /// Stocke « _gate », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1,1);

    /// <summary>

    /// Expose l’événement « ProgressChanged » utilisé pour notifier les composants abonnés d’un changement d’état.

    /// </summary>
    public event Action<int,string>? ProgressChanged;

    /// <summary>

    /// Indique si la condition représentée par IsBusy est satisfaite dans l’état courant.

    /// </summary>
    public bool IsBusy => _gate.CurrentCount == 0;

    /// <summary>
    /// Retourne la liste des dépendances FLUX.2 absentes pour les réglages actuellement chargés.
    /// </summary>
    public IReadOnlyList<(string Label, string Path)> GetMissingModels() =>
        GetMissingModels(_s);

    /// <summary>
    /// Calcule les dépendances FLUX.2 manquantes pour un jeu de réglages donné, sans modifier l’installation.
    /// </summary>
    public static IReadOnlyList<(string Label, string Path)> GetMissingModels(
        AppSettings settings)
    {
        var root = PortablePreflight.GetComfyRoot(settings);
        var checks = new[]
        {
            (
                "FLUX.2",
                Path.Combine(
                    root,
                    "models",
                    "diffusion_models",
                    settings.FluxModel)),
            (
                "Text encoder",
                PortablePreflight.GetComfyTextEncoderPath(
                    settings,
                    settings.TextEncoderModel)),
            (
                "VAE FLUX.2",
                Path.Combine(
                    root,
                    "models",
                    "vae",
                    settings.VaeModel))
        };

        return checks
            .Where(x => !File.Exists(x.Item2))
            .Select(x => (x.Item1, x.Item2))
            .ToArray();
    }


/// <summary>
/// Initialise le g?n?rateur FLUX.2 avec la configuration portable, les callbacks de cycle de vie ComfyUI et le canal de journalisation utilis? pendant chaque g?n?ration.
/// </summary>
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

    /// <summary>

    /// Lance la génération gérée par <c>GenerateAsync</c>, valide les prérequis et retourne le résultat.

    /// </summary>
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
    Génère une image FLUX.2 en mode texte→image ou image→image selon la présence
         d'une image source.
    */
    /// <summary>
    /// Lance la génération gérée par <c>GenerateAsync</c>, valide les prérequis et retourne le résultat.
    /// </summary>
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

                ApplyConfiguredLora(
                    workflow,
                    _s.ImageLora,
                    _s.ImageLoraStrength);

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
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                _log("FLUX", "Génération image annulée.");
                try
                {
                    if (_s.HardStopComfyAfterGeneration)
                        await _stopComfy();
                }
                catch { }

                throw;
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

    /// <summary>

    /// Exécute RunSplitDecodeWorkflowAsync en coordonnant les ressources et les mécanismes d’annulation nécessaires.

    /// </summary>
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

    /// <summary>

    /// Soumet le workflow géré par <c>QueueWorkflowAndWaitAsync</c> à ComfyUI puis attend sa fin ou son annulation.

    /// </summary>
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

    /// <summary>

    /// Libère les ressources ou modèles gérés par <c>FreeComfyModelsAsync</c> afin de réduire l’occupation mémoire.

    /// </summary>
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

    /// <summary>

    /// Recherche la ressource ou valeur demandée par <c>FindComfyOutput</c> dans les données disponibles.

    /// </summary>
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

    /// <summary>

    /// Recherche la valeur exigée par <c>RequireNodeId</c> et signale explicitement son absence.

    /// </summary>
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

    /// <summary>

    /// Calcule le prochain identifiant disponible utilisé par <c>NextNumericNodeId</c>.

    /// </summary>
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

    /// <summary>

    /// Supprime les éléments ciblés par <c>RemoveNodesByClassType</c> sans modifier les éléments non concernés.

    /// </summary>
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

    /// <summary>

    /// Configure les données ou le workflow géré par <c>ConfigureOfficialKleinWorkflow</c> selon les réglages actifs.

    /// </summary>
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

        // FLUX.2 Klein utilise le chemin de conditionnement Qwen3 prévu pour FLUX.2.
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
                // Qwen reçoit les exclusions sous forme d’instructions explicites en langage naturel.
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
            JsonValue.Create(Math.Clamp(_s.ImageCfg, 0.1, 20.0)));

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

    /// <summary>

    /// Recherche la ressource ou valeur demandée par <c>FindNodeInputs</c> dans les données disponibles.

    /// </summary>
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

    /// <summary>

    /// Recherche la valeur exigée par <c>RequireNodeInputs</c> et signale explicitement son absence.

    /// </summary>
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

    /// <summary>

    /// Définit SetRequiredInput et applique immédiatement les effets associés sur l’état de l’application.

    /// </summary>
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

    /// <summary>

    /// Applique ApplyConfiguredLora aux réglages ou contrôles concernés en respectant les contraintes de DreamRaster.

    /// </summary>
    private void ApplyConfiguredLora(
        JsonObject workflow,
        string loraFile,
        double strength)
    {
        if (string.IsNullOrWhiteSpace(loraFile))
            return;

        var fileName = Path.GetFileName(loraFile);
        var loraPath = Path.Combine(
            PortablePreflight.GetComfyRoot(_s),
            "models",
            "loras",
            fileName);

        if (!File.Exists(loraPath))
        {
            throw new FileNotFoundException(
                "LoRA Image configuré mais introuvable dans ComfyUI/models/loras.",
                loraPath);
        }

        var loaderId = RequireNodeId(workflow, "UNETLoader");
        var loraId = NextNumericNodeId(workflow);

        foreach (var entry in workflow.ToArray())
        {
            if (entry.Value is not JsonObject node ||
                node["inputs"] is not JsonObject inputs ||
                inputs["model"] is not JsonArray link ||
                link.Count < 2)
            {
                continue;
            }

            var sourceId = link[0]?.GetValue<string>();
            if (!string.Equals(sourceId, loaderId, StringComparison.Ordinal))
                continue;

            inputs["model"] = new JsonArray(
                JsonValue.Create(loraId),
                JsonValue.Create(0));
        }

        workflow[loraId] = new JsonObject
        {
            ["class_type"] = "LoraLoaderModelOnly",
            ["inputs"] = new JsonObject
            {
                ["model"] = new JsonArray(
                    JsonValue.Create(loaderId),
                    JsonValue.Create(0)),
                ["lora_name"] = JsonValue.Create(fileName),
                ["strength_model"] = JsonValue.Create(Math.Clamp(strength, -2.0, 2.0))
            }
        };

        _log(
            "FLUX",
            $"LoRA Image · {fileName} · force={Math.Clamp(strength, -2.0, 2.0):0.00}.");
    }

    /// <summary>

    /// Tente d’obtenir la valeur gérée par <c>TryGetComfyFailure</c> sans lever d’exception en cas d’absence.

    /// </summary>
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

    /// <summary>

    /// Recherche la ressource ou valeur demandée par <c>FindOutput</c> dans les données disponibles.

    /// </summary>
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

    /// <summary>

    /// Transmet l’avancement courant au callback de progression associé au traitement.

    /// </summary>
    private void Progress(int p, string t)
    {
        try { ProgressChanged?.Invoke(Math.Clamp(p, 0, 100), t); } catch { }
    }

    /// <summary>

    /// Exécute le traitement <c>WaitForSafeMemoryAsync</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
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

            var memory = GetMemorySnapshot();
            var minimumVramMiB =
                Math.Max(256, _s.SafeVramMiB);
            var minimumRamMiB =
                Math.Max(1024, _s.SafeFreeRamMiB);
            var minimumCommitMiB =
                Math.Max(4096, minimumRamMiB);

            if (vram is not null &&
                vram.Value <= minimumVramMiB &&
                memory.FreeRamMiB >= minimumRamMiB &&
                memory.AvailableCommitMiB >= minimumCommitMiB)
            {
                return;
            }

            await Task.Delay(1000, ct);
        }

        var finalMemory = GetMemorySnapshot();
        throw new TimeoutException(
            "La mémoire n'est pas revenue au seuil sûr. " +
            $"RAM libre : {finalMemory.FreeRamMiB:N0} MiB · " +
            $"commit disponible : {finalMemory.AvailableCommitMiB:N0} MiB. " +
            "Fermez les applications lourdes ou augmentez le fichier de pagination Windows.");
    }

    /// <summary>
    /// Lit l'état mémoire global de Windows et retourne la RAM physique libre
    /// ainsi que la réserve de commit encore disponible, exprimées en MiB.
    /// Ces valeurs servent à décider si un nouveau workflow GPU peut démarrer
    /// sans risquer une pression mémoire excessive.
    /// </summary>
    /// <returns>Un instantané contenant la RAM libre et le commit disponible.</returns>
    private static (long FreeRamMiB, long AvailableCommitMiB)
        GetMemorySnapshot()
    {
        var m = new MemoryStatusEx
        {
            Length =
                (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref m))
            return (long.MaxValue, long.MaxValue);

        return (
            (long)(m.AvailPhys / 1048576UL),
            (long)(m.AvailPageFile / 1048576UL));
    }

    /// <summary>

    /// Définit struct « MemoryStatusEx », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        /// <summary>
        /// Stocke « Length », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public uint Length;
        /// <summary>
        /// Stocke « MemoryLoad », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public uint MemoryLoad;
        /// <summary>
        /// Stocke « TotalPhys », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong TotalPhys;
        /// <summary>
        /// Stocke « AvailPhys », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong AvailPhys;
        /// <summary>
        /// Stocke « TotalPageFile », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong TotalPageFile;
        /// <summary>
        /// Stocke « AvailPageFile », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong AvailPageFile;
        /// <summary>
        /// Stocke « TotalVirtual », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong TotalVirtual;
        /// <summary>
        /// Stocke « AvailVirtual », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong AvailVirtual;
        /// <summary>
        /// Stocke « AvailExtendedVirtual », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
        /// </summary>
        public ulong AvailExtendedVirtual;
    }

    /// <summary>
    /// Interroge l’API Win32 GlobalMemoryStatusEx afin d’obtenir l’état mémoire physique et virtuelle du système.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
