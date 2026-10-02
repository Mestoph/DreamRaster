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
    public string Videos { get; set; } = @"videos";
    public string OllamaModels { get; set; } = @"models\ollama";

    public string VisionModel { get; set; } = "qwen3-vl:8b";
    public string FluxModel { get; set; } = "flux-2-klein-4b-fp8.safetensors";
    public string TextEncoderModel { get; set; } = "qwen_3_4b.safetensors";
    public string VaeModel { get; set; } = "flux2-vae.safetensors";
    public string NegativePrompt { get; set; } =
        "low quality, blurry, out of focus, jpeg artifacts, watermark, text, logo, " +
        "deformed, distorted, malformed, oversaturated, underexposed, overexposed";
    public bool AutoImprovePrompt { get; set; } = false;
    public string PromptModel { get; set; } = "qwen3:1.7b";
    public long GenerationSeed { get; set; } = 1001;
    public bool UseRandomSeed { get; set; } = true;

    public string VideoModel { get; set; } = "wan2.1_t2v_1.3B_fp16.safetensors";
    public string VideoTextEncoderModel { get; set; } = "umt5_xxl_fp8_e4m3fn_scaled.safetensors";
    public string VideoVaeModel { get; set; } = "wan_2.1_vae.safetensors";
    public int VideoWidth { get; set; } = 832;
    public int VideoHeight { get; set; } = 480;
    public int VideoFrames { get; set; } = 33;
    public int VideoFps { get; set; } = 16;
    public int VideoSteps { get; set; } = 30;

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

    public string VideoModelUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_t2v_1.3B_fp16.safetensors?download=true";
    public string VideoTextEncoderUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/text_encoders/umt5_xxl_fp8_e4m3fn_scaled.safetensors?download=true";
    public string VideoVaeUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/vae/wan_2.1_vae.safetensors?download=true";

    public string FluxSha256 { get; set; } =
        "97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6";
    public string TextEncoderSha256 { get; set; } =
        "6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a";
    public string VaeSha256 { get; set; } =
        "d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5";
    public string VideoModelSha256 { get; set; } =
        "be531024cd9018cb5b48c40cfbb6a6191645b1c792eb8bf4f8c1c6e10f924dc5";
    public string VideoTextEncoderSha256 { get; set; } =
        "c3355d30191f1f066b26d93fba017ae9809dce6c627dda5f6a66eaa651204f68";
    public string VideoVaeSha256 { get; set; } =
        "2fc39d31359a4b0a64f55876d8ff7fa8d780956ae2cb13463b0223e15148976b";
    // FR : Runtime WebView2 Fixed Version x64 épinglé pour une portabilité réelle.
    // EN: Pinned x64 WebView2 Fixed Version Runtime for true portability.
    public const string DefaultWebView2FixedVersion = "154.0.4258.53";
    public const string DefaultWebView2FixedArchiveUrl =
        "https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/0b89c3a3-0043-4746-b39e-65830da7744d/Microsoft.WebView2.FixedVersionRuntime.154.0.4258.53.x64.cab";
    public const string DefaultWebView2FixedSha256 =
        "ec12b2db6423d127fb8e1935d34e2e68abc70fe8ecb1f6162ba1c1ccc2825f6d";

    public string WebView2FixedVersion { get; set; } = DefaultWebView2FixedVersion;
    public string WebView2FixedArchiveUrl { get; set; } = DefaultWebView2FixedArchiveUrl;
    public string WebView2FixedSha256 { get; set; } = DefaultWebView2FixedSha256;

    // FR : Répare les anciennes configurations qui avaient WebView2FixedArchiveUrl vide.
    // EN: Repairs legacy configurations that stored an empty WebView2FixedArchiveUrl.
    public bool EnsureWebView2FixedDefaults()
    {
        if (!string.IsNullOrWhiteSpace(WebView2FixedArchiveUrl) &&
            !string.IsNullOrWhiteSpace(WebView2FixedVersion) &&
            !string.IsNullOrWhiteSpace(WebView2FixedSha256))
        {
            return false;
        }

        WebView2FixedVersion = DefaultWebView2FixedVersion;
        WebView2FixedArchiveUrl = DefaultWebView2FixedArchiveUrl;
        WebView2FixedSha256 = DefaultWebView2FixedSha256;
        return true;
    }
}
