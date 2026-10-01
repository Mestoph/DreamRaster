/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Métadonnées centrales de marque, de dépôt GitHub et de publication.
EN: Central branding, GitHub repository and release metadata.
*/
namespace OpenCodeLocalAI;

internal static class BrandInfo
{
    public const string ProductName = "DreamRaster";
    public const string AuthorName = "Mestoph";
    public const string CopyrightNotice = "Copyright © 2026 Mestoph";
    public const string LicenseName = "GNU AGPL-3.0-or-later";
    public const string ExecutableName = "DreamRaster.exe";

    public const string DefaultGitHubOwner = "Mestoph";
    public const string DefaultGitHubRepository = "DreamRaster";
    public const string ReleaseAssetName = "DreamRaster-win-x64.zip";

    public const string DescriptionFr =
        "Studio Windows portable de Mestoph pour la génération d’images IA locale : FLUX.2, ComfyUI, Ollama et OpenCode.";

    public const string DescriptionEn =
        "Mestoph portable Windows studio for local AI image generation: FLUX.2, ComfyUI, Ollama and OpenCode.";
}
