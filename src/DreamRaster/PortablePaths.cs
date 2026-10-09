/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Racine portable, confinement des chemins et settings.json.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

using System.Text.Json;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « PortablePaths », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public static class PortablePaths
{
    /// <summary>
    /// Stocke « _root », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private static readonly string _root = ResolvePortableRoot();

        /// <summary>
    /// Racine portable effective de DreamRaster. Tous les chemins de configuration, runtime, mod?les et donn?es sont r?solus relativement ? cette racine confin?e.
    /// </summary>
    public static string Root => _root;

    /// <summary>

    /// Exécute le traitement <c>ResolvePortableRoot</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    private static string ResolvePortableRoot()
    {
        // Pour une application publiée, on veut le dossier du vrai EXE
        // lancé par l'utilisateur, jamais un dossier d'extraction .net dans TEMP.
        var processPath = Environment.ProcessPath;

        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var full = Path.GetFullPath(processPath);
            var fileName = Path.GetFileName(full);

            if (fileName.Equals("DreamRaster.exe", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("MestophAIStudio.exe", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("OpenCodeLocalAI.exe", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrWhiteSpace(dir))
                    return Path.TrimEndingDirectorySeparator(dir);
            }
        }

        // Compilation/Designer : AppContext.BaseDirectory reste la meilleure solution de repli.
        return Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(AppContext.BaseDirectory));
    }
    /// <summary>
    /// Dossier contenant la configuration persistante de DreamRaster.
    /// </summary>
    public static string ConfigDir => Path.Combine(Root, "config");
    /// <summary>
    /// Valeur de configuration downloads utilisée par DreamRaster.
    /// </summary>
    public static string DownloadsDir => Path.Combine(Root, "downloads");
    /// <summary>
    /// Dossier contenant les journaux applicatifs et fichiers de diagnostic.
    /// </summary>
    public static string LogsDir => Path.Combine(Root, "logs");
    /// <summary>
    /// Exécute RuntimeDir en coordonnant les ressources et les mécanismes d’annulation nécessaires.
    /// </summary>
    public static string RuntimeDir => Path.Combine(Root, "runtime");
    /// <summary>
    /// Valeur de configuration models utilisée par DreamRaster.
    /// </summary>
    public static string ModelsDir => Path.Combine(Root, "models");
    /// <summary>
    /// Valeur de configuration workflows utilisée par DreamRaster.
    /// </summary>
    public static string WorkflowsDir => Path.Combine(Root, "workflows");

    /// <summary>

    /// Définit la constante « TextToImageWorkflowFile » utilisée comme valeur de référence stable par ce composant.

    /// </summary>
    public const string TextToImageWorkflowFile = "flux2_text_to_image_api.json";
    /// <summary>
    /// Définit la constante « ImgToImgWorkflowFile » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string ImgToImgWorkflowFile = "flux2_img_to_img_api.json";
    /// <summary>
    /// Définit la constante « WanTextToVideoWorkflowFile » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string WanTextToVideoWorkflowFile = "wan21_text_to_video_api.json";
    /// <summary>
    /// Définit la constante « WanImageToVideoWorkflowFile » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string WanImageToVideoWorkflowFile = "wan21_image_to_video_api.json";
    /// <summary>
    /// Définit la constante « LegacyWorkflowFile » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string LegacyWorkflowFile = "flux2_workflow_api.json";

    /// <summary>

    /// Vérifie puis garantit la condition requise par <c>EnsureLayout</c> avant de poursuivre.

    /// </summary>
    public static void EnsureLayout()
    {
        foreach (var dir in new[]
        {
            "bin", "config", "downloads", "images", "videos", "logs", "models", "runtime",
            "workflows", "workspace", @"models\ollama", @"runtime\opencode",
            @"runtime\ollama", @"runtime\comfyui", @"runtime\webview2-opencode",
            @"runtime\webview2-comfy", @"runtime\native"
        })
            Directory.CreateDirectory(Path.Combine(Root, dir));

        EnsureBundledWorkflows();
    }

    /// <summary>

    /// Vérifie puis garantit la condition requise par <c>EnsureBundledWorkflows</c> avant de poursuivre.

    /// </summary>
    public static void EnsureBundledWorkflows()
    {
        Directory.CreateDirectory(WorkflowsDir);

        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, TextToImageWorkflowFile),
            "OpenCodeLocalAI.Workflows.flux2_text_to_image_api.json");

        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, ImgToImgWorkflowFile),
            "OpenCodeLocalAI.Workflows.flux2_img_to_img_api.json");

        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, WanTextToVideoWorkflowFile),
            "OpenCodeLocalAI.Workflows.wan21_text_to_video_api.json");

        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, WanImageToVideoWorkflowFile),
            "OpenCodeLocalAI.Workflows.wan21_image_to_video_api.json");

        // Compatibilité avec les versions précédentes du générateur.
        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, LegacyWorkflowFile),
            "OpenCodeLocalAI.Workflows.flux2_text_to_image_api.json");
    }

    /// <summary>

    /// Écrit les données gérées par <c>WriteEmbeddedFileIfDifferent</c> vers leur destination.

    /// </summary>
    private static void WriteEmbeddedFileIfDifferent(
        string targetPath,
        string resourceName)
    {
        byte[] bundled;

        using (var input =
            typeof(PortablePaths).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                "Ressource embarquée introuvable : " + resourceName))
        using (var memory = new MemoryStream())
        {
            input.CopyTo(memory);
            bundled = memory.ToArray();
        }

        if (File.Exists(targetPath))
        {
            try
            {
                var current = File.ReadAllBytes(targetPath);
                if (current.AsSpan().SequenceEqual(bundled))
                    return;
            }
            catch
            {
                // Réécriture d'un workflow embarqué endommagé/illisible.
            }
        }

        var temp = targetPath + ".tmp";

        try
        {
            File.WriteAllBytes(temp, bundled);
            File.Move(temp, targetPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch { }
        }
    }

    /// <summary>

    /// Exécute le traitement <c>Resolve</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    public static string Resolve(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Un chemin portable est vide.");

        var full = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(Root, configured));

        var prefix = Root + Path.DirectorySeparatorChar;
        if (!full.Equals(Root, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Chemin hors du pack portable interdit : {full}");

        return full;
    }

    /// <summary>

    /// Indique si la condition représentée par IsInsidePack est satisfaite dans l’état courant.

    /// </summary>
    public static bool IsInsidePack(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var full = Path.GetFullPath(path);
            var prefix = Root + Path.DirectorySeparatorChar;
            return full.Equals(Root, StringComparison.OrdinalIgnoreCase) ||
                   full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>

    /// Retourne GetFixedWebView2RuntimePath calculé à partir de l’état courant de l’application.

    /// </summary>
    public static string? GetFixedWebView2RuntimePath()
    {
        var root = Path.Combine(Root, "bin", "webview2-fixed");
        if (!Directory.Exists(root))
            return null;

        try
        {
            var exe = Directory.EnumerateFiles(
                    root,
                    "msedgewebview2.exe",
                    SearchOption.AllDirectories)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(exe))
                return null;

            var runtime = Path.GetDirectoryName(exe);
            return IsInsidePack(runtime) ? runtime : null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>

/// Définit class « SettingsStore », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public static class SettingsStore
{
    /// <summary>
    /// Chemin complet du fichier settings.json persistant.
    /// </summary>
    private static string FilePath => Path.Combine(PortablePaths.ConfigDir, "settings.json");

    /// <summary>

    /// Charge les réglages persistants depuis settings.json, applique les migrations nécessaires et retourne des valeurs par défaut en cas d’erreur.

    /// </summary>
    public static AppSettings Load()
    {
        PortablePaths.EnsureLayout();
        try
        {
            if (!File.Exists(FilePath))
            {
                var created = new AppSettings();
                Save(created);
                return created;
            }
            var loaded = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(FilePath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new AppSettings();

            // Migration silencieuse des anciennes configurations v34 et antérieures.
            // Migre silencieusement les fichiers de configuration hérités de la v34 et des versions antérieures.
            if (loaded.EnsureWebView2FixedDefaults())
                Save(loaded);

            return loaded;
        }
        catch (Exception ex)
        {
            CrashLog.Write("Settings.Load", ex);
            return new AppSettings();
        }
    }

    /// <summary>

    /// Enregistre Save de manière persistante afin de conserver le choix de l’utilisateur.

    /// </summary>
    public static void Save(AppSettings settings)
    {
        PortablePaths.EnsureLayout();
        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
