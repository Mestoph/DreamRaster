/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Catalogue de traduction de l'interface. Les clés restent stables dans le code.
EN: UI translation catalog. Keys remain stable throughout the codebase.
*/
namespace OpenCodeLocalAI;

internal static class L10n
{
    private static readonly IReadOnlyDictionary<string, (string Fr, string En)> Strings =
        new Dictionary<string, (string Fr, string En)>(StringComparer.OrdinalIgnoreCase)
        {
            ["tab.dashboard"] = ("Tableau de bord", "Dashboard"),
            ["tab.opencode"] = ("OpenCode", "OpenCode"),
            ["tab.comfy"] = ("ComfyUI", "ComfyUI"),
            ["tab.ollama"] = ("Console Ollama", "Ollama Console"),
            ["tab.generate"] = ("Générer", "Generate"),
            ["tab.install"] = ("Installation", "Installation"),
            ["tab.config"] = ("Configuration", "Settings"),
            ["tab.logs"] = ("Logs", "Logs"),
            ["tab.about"] = ("À propos", "About"),

            ["button.start"] = ("Démarrer", "Start"),
            ["button.stop"] = ("Tout arrêter", "Stop all"),
            ["button.open_opencode"] = ("Ouvrir OpenCode intégré", "Open embedded OpenCode"),
            ["button.open_comfy"] = ("Ouvrir ComfyUI intégré", "Open embedded ComfyUI"),
            ["button.diagnostic"] = ("Exporter diagnostic", "Export diagnostics"),
            ["button.generate"] = ("Générer", "Generate"),
            ["button.browse"] = ("Parcourir…", "Browse…"),
            ["button.clear"] = ("Effacer", "Clear"),
            ["button.install"] = ("Installer / réparer le pack portable", "Install / repair portable pack"),
            ["button.cancel"] = ("Annuler", "Cancel"),
            ["button.save"] = ("Enregistrer la configuration", "Save settings"),
            ["button.open_config"] = ("Ouvrir dossier config", "Open config folder"),
            ["button.check_updates"] = ("Rechercher une mise à jour", "Check for updates"),
            ["button.github"] = ("Ouvrir GitHub", "Open GitHub"),

            ["label.live_console"] = ("Console en direct", "Live console"),
            ["label.prompt"] = ("Prompt FLUX.2", "FLUX.2 prompt"),
            ["label.mode"] = ("Mode", "Mode"),
            ["generation.mode.txt2img"] = ("Texte → image", "Text → image"),
            ["generation.mode.img2img"] = ("Image → image", "Image → image"),
            ["label.input_image"] = ("Image source", "Source image"),
            ["label.img2img_strength"] = (
                "Force I2I",
                "I2I strength"),
            ["install.title"] = ("Installation portable", "Portable installation"),
            ["install.info"] = (
                "Les composants sont installés uniquement dans le dossier portable. Les versions Ollama, OpenCode ou ComfyUI présentes ailleurs dans Windows ne sont jamais utilisées.",
                "Components are installed only inside the portable folder. Ollama, OpenCode or ComfyUI installed elsewhere in Windows are never used."),

            ["config.title"] = ("Configuration portable", "Portable settings"),
            ["config.root"] = ("Racine portable :", "Portable root:"),
            ["config.language"] = ("Langue", "Language"),
            ["config.auto_update"] = ("Rechercher automatiquement les mises à jour GitHub", "Automatically check GitHub for updates"),
            ["config.github_repo"] = ("Dépôt GitHub", "GitHub repository"),
            ["config.vision_optional"] = ("Installer Qwen3-VL (vision, optionnel)", "Install Qwen3-VL (vision, optional)"),
            ["config.hint"] = (
                "Le fichier config\\settings.json est créé dans la racine portable. Les changements de ports nécessitent un redémarrage de l'application.",
                "config\\settings.json is created in the portable root. Port changes require an application restart."),

            ["about.title"] = ("À propos de", "About"),
            ["about.version"] = ("Version", "Version"),
            ["about.author"] = ("Auteur / propriétaire", "Author / copyright owner"),
            ["about.repository"] = ("Dépôt", "Repository"),
            ["about.license"] = (
                "Licence : GNU AGPL-3.0-or-later — Copyright © 2026 Mestoph.",
                "License: GNU AGPL-3.0-or-later — Copyright © 2026 Mestoph."),
            ["about.update_ready"] = ("Prêt à vérifier les mises à jour.", "Ready to check for updates."),
            ["about.checking"] = ("Recherche de mise à jour…", "Checking for updates…"),
            ["about.latest"] = ("Vous utilisez la dernière version.", "You are running the latest version."),
            ["about.update_available"] = ("Nouvelle version disponible :", "New version available:"),
            ["about.no_release"] = ("Aucune release GitHub disponible pour le moment.", "No GitHub release is available yet."),
            ["about.repo_missing"] = ("Le dépôt GitHub n'existe pas encore ou n'est pas accessible.", "The GitHub repository does not exist yet or is not accessible."),
            ["about.install_update"] = ("Télécharger et installer la mise à jour maintenant ?", "Download and install the update now?"),
            ["about.downloading"] = ("Téléchargement de la mise à jour…", "Downloading update…"),
            ["about.update_staged"] = ("Mise à jour prête. L'application va redémarrer.", "Update ready. The application will restart."),

            ["msg.settings_saved"] = (
                "Configuration enregistrée dans le dossier portable.\n\nLes changements de ports seront appliqués après redémarrage.",
                "Settings saved in the portable folder.\n\nPort changes will be applied after restart."),
            ["msg.config"] = ("Configuration", "Settings"),
            ["msg.update"] = ("Mise à jour", "Update"),
        };

    public static string NormalizeLanguage(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";

    public static bool IsEnglish(string? language)
        => NormalizeLanguage(language) == "en";

    public static string T(string? language, string key)
        => Strings.TryGetValue(key, out var value)
            ? (IsEnglish(language) ? value.En : value.Fr)
            : key;

    public static string Pick(string? language, string fr, string en)
        => IsEnglish(language) ? en : fr;
}
