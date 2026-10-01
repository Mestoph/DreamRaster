/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Modèle sérialisable de toute la configuration portable.
EN: Serializable model for all portable settings.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

namespace OpenCodeLocalAI;

public sealed class AppSettings
{
    public string OpenCodeExe { get; set; } = @"bin\opencode\opencode.exe";
    public string OllamaExe { get; set; } = @"bin\ollama\ollama.exe";
    public string ComfyPython { get; set; } = @"bin\comfyui\ComfyUI_windows_portable\python_embeded\python.exe";
    public string ComfyMain { get; set; } = @"bin\comfyui\ComfyUI_windows_portable\ComfyUI\main.py";
    public string Workspace { get; set; } = @"workspace";
    public string Images { get; set; } = @"images";
    public string OllamaModels { get; set; } = @"models\ollama";

    public string VisionModel { get; set; } = "qwen3-vl:8b";
    public string FluxModel { get; set; } = "flux-2-klein-4b-fp8.safetensors";
    public string TextEncoderModel { get; set; } = "qwen_3_4b.safetensors";
    public string VaeModel { get; set; } = "flux2-vae.safetensors";

    public int OpenCodePort { get; set; } = 54095;
    public int OllamaPort { get; set; } = 11434;
    public int ComfyPort { get; set; } = 8188;
    public int ImageProxyPort { get; set; } = 54100;
    public int GenerationApiPort { get; set; } = 54101;

    public int DefaultWidth { get; set; } = 1024;
    public int DefaultHeight { get; set; } = 1024;
    public int DefaultSteps { get; set; } = 4;
    public int SafeVramMiB { get; set; } = 1800;
    public int SafeFreeRamMiB { get; set; } = 4096;
    public bool HardStopComfyAfterGeneration { get; set; } = true;

    // FR : Langue de l'interface. EN: UI language.
    public string Language { get; set; } = "fr";

    // FR : Vérification des releases GitHub au démarrage. EN: Check GitHub releases at startup.
    public bool AutoCheckUpdates { get; set; } = true;
    public string GitHubOwner { get; set; } = BrandInfo.DefaultGitHubOwner;
    public string GitHubRepository { get; set; } = BrandInfo.DefaultGitHubRepository;

    // FR : Qwen3-VL est optionnel et sert aux fonctions de vision, pas à FLUX.2.
    // EN: Qwen3-VL is optional and is for vision features, not FLUX.2 generation.
    public bool InstallVisionModel { get; set; } = false;

    // Téléchargement Windows natif : plusieurs curl.exe peuvent télécharger
    // des segments séparés, ensuite assemblés séquentiellement.
    public int DownloadConnections { get; set; } = 8;
    public long ParallelDownloadThresholdMiB { get; set; } = 32;
    public int DownloadBufferMiB { get; set; } = 8;

    public string OpenCodeZipUrl { get; set; } =
        "https://github.com/anomalyco/opencode/releases/latest/download/opencode-windows-x64.zip";
    public string OllamaZipUrl { get; set; } =
        "https://ollama.com/download/ollama-windows-amd64.zip";
    public string ComfyZipUrl { get; set; } =
        "https://github.com/Comfy-Org/ComfyUI/releases/latest/download/ComfyUI_windows_portable_nvidia.7z";
    public string SevenZipUrl { get; set; } =
        "https://www.7-zip.org/a/7zr.exe";

    public string FluxModelUrl { get; set; } =
        "https://huggingface.co/black-forest-labs/FLUX.2-klein-4b-fp8/resolve/main/flux-2-klein-4b-fp8.safetensors";
    public string TextEncoderUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/flux2-klein/resolve/main/split_files/text_encoders/qwen_3_4b.safetensors";
    public string VaeUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/flux2-dev/resolve/main/split_files/vae/flux2-vae.safetensors";

    public string FluxSha256 { get; set; } =
        "97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6";
    public string TextEncoderSha256 { get; set; } =
        "6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a";
    public string VaeSha256 { get; set; } =
        "d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5";

    // Laisser vide si le runtime Fixed WebView2 est fourni manuellement dans bin\webview2-fixed.
    public string WebView2FixedArchiveUrl { get; set; } = "";
}
