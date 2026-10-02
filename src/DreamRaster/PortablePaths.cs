/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Racine portable, confinement des chemins et settings.json.
EN: Portable root, path confinement and settings.json.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Text.Json;

namespace OpenCodeLocalAI;

public static class PortablePaths
{
    private static readonly string _root = ResolvePortableRoot();

    public static string Root => _root;

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

        // Build/Designer : AppContext.BaseDirectory reste le meilleur fallback.
        return Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(AppContext.BaseDirectory));
    }
    public static string ConfigDir => Path.Combine(Root, "config");
    public static string DownloadsDir => Path.Combine(Root, "downloads");
    public static string LogsDir => Path.Combine(Root, "logs");
    public static string RuntimeDir => Path.Combine(Root, "runtime");
    public static string ModelsDir => Path.Combine(Root, "models");
    public static string WorkflowsDir => Path.Combine(Root, "workflows");

    public const string TextToImageWorkflowFile = "flux2_text_to_image_api.json";
    public const string ImgToImgWorkflowFile = "flux2_img_to_img_api.json";
    public const string WanTextToVideoWorkflowFile = "wan21_text_to_video_api.json";
    public const string LegacyWorkflowFile = "flux2_workflow_api.json";

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

        // Compatibilité avec les versions précédentes du générateur.
        WriteEmbeddedFileIfDifferent(
            Path.Combine(WorkflowsDir, LegacyWorkflowFile),
            "OpenCodeLocalAI.Workflows.flux2_text_to_image_api.json");
    }

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

public static class SettingsStore
{
    private static string FilePath => Path.Combine(PortablePaths.ConfigDir, "settings.json");

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

            // FR : Migration silencieuse des anciennes configurations v34 et antérieures.
            // EN: Silently migrate legacy v34-and-earlier configuration files.
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

    public static void Save(AppSettings settings)
    {
        PortablePaths.EnsureLayout();
        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
