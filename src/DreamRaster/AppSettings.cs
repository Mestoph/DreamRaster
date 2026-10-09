/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Modèle sérialisable de toute la configuration portable.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « AppSettings », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Ouvre OpenCodeExe avec le mécanisme approprié tout en vérifiant au préalable que la ressource existe.
    /// </summary>
    public string OpenCodeExe { get; set; } = @"bin\opencode\opencode.exe";
    /// <summary>
    /// Valeur de configuration exe utilisée par DreamRaster.
    /// </summary>
    public string OllamaExe { get; set; } = @"bin\ollama\ollama.exe";
    /// <summary>
    /// Valeur de configuration exe utilisée par DreamRaster.
    /// </summary>
    public string ComfyPython { get; set; } = @"bin\comfyui\ComfyUI_windows_portable\python_embeded\python.exe";
    /// <summary>
    /// Valeur de configuration py utilisée par DreamRaster.
    /// </summary>
    public string ComfyMain { get; set; } = @"bin\comfyui\ComfyUI_windows_portable\ComfyUI\main.py";
    /// <summary>
    /// Valeur de configuration workspace utilisée par DreamRaster.
    /// </summary>
    public string Workspace { get; set; } = @"workspace";
    /// <summary>
    /// Valeur de configuration images utilisée par DreamRaster.
    /// </summary>
    public string Images { get; set; } = @"images";
    /// <summary>
    /// Valeur de configuration videos utilisée par DreamRaster.
    /// </summary>
    public string Videos { get; set; } = @"videos";
    /// <summary>
    /// Valeur de configuration ollama utilisée par DreamRaster.
    /// </summary>
    public string OllamaModels { get; set; } = @"models\ollama";

        /// <summary>
    /// Nom du mod?le Vision Ollama utilis? pour analyser des images et extraire ou am?liorer des prompts ; ce mod?le reste facultatif pour FLUX.2.
    /// </summary>
    public string VisionModel { get; set; } = "qwen3-vl:8b";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string FluxModel { get; set; } = "flux-2-klein-4b-fp8.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string TextEncoderModel { get; set; } = "qwen_3_4b.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string VaeModel { get; set; } = "flux2-vae.safetensors";
    /// <summary>
    /// Prompt négatif Image persistant utilisé pour exclure les défauts, artefacts ou contenus indésirables.
    /// </summary>
    public string NegativePrompt { get; set; } =
        "low quality, blurry, out of focus, jpeg artifacts, watermark, text, logo, " +
        "deformed, distorted, malformed, oversaturated, underexposed, overexposed";
    /// <summary>
    /// Valeur de configuration false utilisée par DreamRaster.
    /// </summary>
    public bool AutoImprovePrompt { get; set; } = false;
    /// <summary>
    /// Valeur de configuration qwen3 utilisée par DreamRaster.
    /// </summary>
    public string PromptModel { get; set; } = "qwen3:1.7b";
    /// <summary>
    /// Valeur de configuration photo4k utilisée par DreamRaster.
    /// </summary>
    public string ImageStyleTemplate { get; set; } = "photo4k";
    /// <summary>
    /// Valeur de configuration safe_quality utilisée par DreamRaster.
    /// </summary>
    public string ImageNegativeTemplate { get; set; } = "safe_quality";
    /// <summary>
    /// Valeur de configuration photo utilisée par DreamRaster.
    /// </summary>
    public string ImagePipelinePreset { get; set; } = "photo";
    /// <summary>
    /// Valeur CFG utilisée par le pipeline Image lorsque le workflow FLUX.2 l’expose.
    /// </summary>
    public double ImageCfg { get; set; } = 1.0;
    /// <summary>
    /// Pourcentage de netteté appliqué à la seconde passe du mode Qualité maximale Image.
    /// </summary>
    public int MaximumQualityImageSharpnessPercent { get; set; } = 75;
    /// <summary>
    /// Graine déterministe utilisée pour reproduire une génération Image.
    /// </summary>
    public long GenerationSeed { get; set; } = 1001;
    /// <summary>
    /// Valeur de configuration true utilisée par DreamRaster.
    /// </summary>
    public bool UseRandomSeed { get; set; } = true;
    /// <summary>
    /// Valeur de configuration Empty utilisée par DreamRaster.
    /// </summary>
    public string ImageLora { get; set; } = string.Empty;
    /// <summary>
    /// Force d’application du LoRA Image sélectionné.
    /// </summary>
    public double ImageLoraStrength { get; set; } = 1.0;

        /// <summary>
    /// Nom du checkpoint Wan utilis? par d?faut pour la g?n?ration vid?o texte?vid?o lorsque l?utilisateur n?a pas choisi un autre mod?le dans l?onglet Vid?o.
    /// </summary>
    public string VideoModel { get; set; } = "wan2.1_t2v_1.3B_fp16.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string VideoTextEncoderModel { get; set; } = "umt5_xxl_fp8_e4m3fn_scaled.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string VideoVaeModel { get; set; } = "wan_2.1_vae.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string VideoClipVisionModel { get; set; } = "clip_vision_h.safetensors";
    /// <summary>
    /// Valeur de configuration safetensors utilisée par DreamRaster.
    /// </summary>
    public string VideoI2vModel { get; set; } = "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors";
    /// <summary>
    /// Valeur de configuration cinematic utilisée par DreamRaster.
    /// </summary>
    public string VideoStyleTemplate { get; set; } = "cinematic";
    /// <summary>
    /// Valeur de configuration safe_stable utilisée par DreamRaster.
    /// </summary>
    public string VideoNegativeTemplate { get; set; } = "safe_stable";
    /// <summary>
    /// Valeur de configuration cinematic utilisée par DreamRaster.
    /// </summary>
    public string VideoPipelinePreset { get; set; } = "cinematic";
    /// <summary>
    /// Largeur de sortie demandée pour les générations Vidéo.
    /// </summary>
    public int VideoWidth { get; set; } = 832;
    /// <summary>
    /// Hauteur de sortie demandée pour les générations Vidéo.
    /// </summary>
    public int VideoHeight { get; set; } = 480;
    /// <summary>
    /// Nombre total de frames demandé au workflow Vidéo.
    /// </summary>
    public int VideoFrames { get; set; } = 33;
    /// <summary>
    /// Fréquence d’images par seconde utilisée pour l’encodage de la vidéo finale.
    /// </summary>
    public int VideoFps { get; set; } = 16;
    /// <summary>
    /// Durée cible de la vidéo en secondes, synchronisée avec le nombre de frames et le FPS.
    /// </summary>
    public double VideoDurationSeconds { get; set; } = 2.06;
    /// <summary>
    /// Nombre d’étapes de diffusion utilisé par le modèle Wan.
    /// </summary>
    public int VideoSteps { get; set; } = 30;
    /// <summary>
    /// Valeur CFG appliquée au workflow Wan.
    /// </summary>
    public double VideoCfg { get; set; } = 6.0;
    /// <summary>
    /// Pourcentage de netteté appliqué au post-traitement du mode Qualité maximale Vidéo.
    /// </summary>
    public int MaximumQualityVideoSharpnessPercent { get; set; } = 65;
    /// <summary>
    /// Valeur de sampling shift transmise au modèle Wan.
    /// </summary>
    public double VideoSamplingShift { get; set; } = 8.0;
    /// <summary>
    /// Valeur de configuration uni_pc utilisée par DreamRaster.
    /// </summary>
    public string VideoSampler { get; set; } = "uni_pc";
    /// <summary>
    /// Valeur de configuration simple utilisée par DreamRaster.
    /// </summary>
    public string VideoScheduler { get; set; } = "simple";
    /// <summary>
    /// Graine déterministe utilisée pour reproduire une génération Vidéo.
    /// </summary>
    public long VideoSeed { get; set; } = 1001;
    /// <summary>
    /// Valeur de configuration true utilisée par DreamRaster.
    /// </summary>
    public bool UseRandomVideoSeed { get; set; } = true;
    /// <summary>
    /// Valeur de configuration Empty utilisée par DreamRaster.
    /// </summary>
    public string VideoLora { get; set; } = string.Empty;
    /// <summary>
    /// Force d’application du LoRA Vidéo sélectionné.
    /// </summary>
    public double VideoLoraStrength { get; set; } = 1.0;
    /// <summary>
    /// Valeur de configuration false utilisée par DreamRaster.
    /// </summary>
    public bool VideoAutoImprovePrompt { get; set; } = false;
    /// <summary>
    /// Valeur de configuration Empty utilisée par DreamRaster.
    /// </summary>
    public string LastImprovedVideoPromptHash { get; set; } = string.Empty;
    /// <summary>
    /// Valeur de configuration best utilisée par DreamRaster.
    /// </summary>
    public string VideoQualityPreset { get; set; } = "best";

    /// <summary>

    /// Ouvre OpenCodePort avec le mécanisme approprié tout en vérifiant au préalable que la ressource existe.

    /// </summary>
    public int OpenCodePort { get; set; } = 54095;
    /// <summary>
    /// Port TCP local utilisé par le serveur Ollama portable.
    /// </summary>
    public int OllamaPort { get; set; } = 11434;
    /// <summary>
    /// Port TCP local utilisé par le serveur ComfyUI portable.
    /// </summary>
    public int ComfyPort { get; set; } = 8188;
    /// <summary>
    /// Port TCP local du proxy Image intégré à DreamRaster.
    /// </summary>
    public int ImageProxyPort { get; set; } = 54100;
    /// <summary>
    /// Port TCP local de l’API de génération exposée par DreamRaster.
    /// </summary>
    public int GenerationApiPort { get; set; } = 54101;

        /// <summary>
    /// Largeur par d?faut des nouvelles g?n?rations Image, exprim?e en pixels et persist?e entre les sessions.
    /// </summary>
    public int DefaultWidth { get; set; } = 1024;
    /// <summary>
    /// Hauteur par défaut des images générées.
    /// </summary>
    public int DefaultHeight { get; set; } = 1024;
    /// <summary>
    /// Nombre d’étapes de diffusion par défaut pour la génération Image.
    /// </summary>
    public int DefaultSteps { get; set; } = 4;
    /// <summary>
    /// Réserve minimale de VRAM, en MiB, que DreamRaster tente de préserver avant un workflow GPU.
    /// </summary>
    public int SafeVramMiB { get; set; } = 1800;
    /// <summary>
    /// Quantité minimale de RAM physique libre, en MiB, requise avant certains workflows lourds.
    /// </summary>
    public int SafeFreeRamMiB { get; set; } = 4096;
    /// <summary>
    /// Valeur de configuration true utilisée par DreamRaster.
    /// </summary>
    public bool HardStopComfyAfterGeneration { get; set; } = true;
    /// <summary>
    /// Valeur de configuration true utilisée par DreamRaster.
    /// </summary>
    public bool AutoSaveConfiguration { get; set; } = true;
    /// <summary>
    /// Affiche ShowOpenCodeTab et prépare les informations nécessaires à l’utilisateur.
    /// </summary>
    public bool ShowOpenCodeTab { get; set; } = false;
    /// <summary>
    /// Affiche ShowComfyUiTab et prépare les informations nécessaires à l’utilisateur.
    /// </summary>
    public bool ShowComfyUiTab { get; set; } = false;
    /// <summary>
    /// Valeur de configuration Empty utilisée par DreamRaster.
    /// </summary>
    public string LastImprovedPromptHash { get; set; } = string.Empty;

    // Langue de l'interface.
    /// <summary>
    /// Valeur de configuration fr utilisée par DreamRaster.
    /// </summary>
    public string Language { get; set; } = "fr";

    // Vérification des releases GitHub au démarrage.
    /// <summary>
    /// Valeur de configuration true utilisée par DreamRaster.
    /// </summary>
    public bool AutoCheckUpdates { get; set; } = true;
    /// <summary>
    /// Valeur de configuration DefaultGitHubOwner utilisée par DreamRaster.
    /// </summary>
    public string GitHubOwner { get; set; } = BrandInfo.DefaultGitHubOwner;
    /// <summary>
    /// Valeur de configuration DefaultGitHubRepository utilisée par DreamRaster.
    /// </summary>
    public string GitHubRepository { get; set; } = BrandInfo.DefaultGitHubRepository;

    // Qwen3-VL est optionnel et sert aux fonctions de vision, pas à FLUX.2.
    // Qwen3-VL est facultatif et sert aux fonctions de vision, pas à la génération FLUX.2.
    /// <summary>
    /// Installe InstallVisionModel dans l’environnement portable de DreamRaster sans modifier les ressources non concernées.
    /// </summary>
    public bool InstallVisionModel { get; set; } = false;

    // Téléchargement Windows natif : plusieurs curl.exe peuvent télécharger
    // des segments séparés, ensuite assemblés séquentiellement.
    /// <summary>
    /// Nombre maximal de connexions parallèles utilisées pour les téléchargements segmentés.
    /// </summary>
    public int DownloadConnections { get; set; } = 8;
    /// <summary>
    /// Taille minimale, en MiB, à partir de laquelle DreamRaster peut utiliser un téléchargement segmenté.
    /// </summary>
    public long ParallelDownloadThresholdMiB { get; set; } = 32;
    /// <summary>
    /// Taille du tampon mémoire, en MiB, utilisée lors des téléchargements de fichiers volumineux.
    /// </summary>
    public int DownloadBufferMiB { get; set; } = 8;

    /// <summary>

    /// Ouvre OpenCodeZipUrl avec le mécanisme approprié tout en vérifiant au préalable que la ressource existe.

    /// </summary>
    public string OpenCodeZipUrl { get; set; } =
        "https://github.com/anomalyco/opencode/releases/latest/download/opencode-windows-x64.zip";
    /// <summary>
    /// URL de l’archive Ollama portable téléchargée par l’installateur.
    /// </summary>
    public string OllamaZipUrl { get; set; } =
        "https://ollama.com/download/ollama-windows-amd64.zip";
    /// <summary>
    /// URL de l’archive ComfyUI portable téléchargée par l’installateur.
    /// </summary>
    public string ComfyZipUrl { get; set; } =
        "https://github.com/Comfy-Org/ComfyUI/releases/latest/download/ComfyUI_windows_portable_nvidia.7z";
    /// <summary>
    /// URL de l’outil 7-Zip utilisé lorsque l’extraction d’une archive l’exige.
    /// </summary>
    public string SevenZipUrl { get; set; } =
        "https://www.7-zip.org/a/7zr.exe";

        /// <summary>
    /// URL source utilis?e par l?installateur pour r?cup?rer le checkpoint FLUX.2 configur? lorsque celui-ci doit ?tre install? ou r?par?.
    /// </summary>
    public string FluxModelUrl { get; set; } =
        "https://huggingface.co/black-forest-labs/FLUX.2-klein-4b-fp8/resolve/main/flux-2-klein-4b-fp8.safetensors";
    /// <summary>
    /// URL source de l’encodeur texte FLUX.2 configuré.
    /// </summary>
    public string TextEncoderUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/flux2-klein/resolve/main/split_files/text_encoders/qwen_3_4b.safetensors";
    /// <summary>
    /// URL source du VAE FLUX.2 configuré.
    /// </summary>
    public string VaeUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/flux2-dev/resolve/main/split_files/vae/flux2-vae.safetensors";

        /// <summary>
    /// URL source utilis?e par l?installateur pour r?cup?rer le checkpoint Wan vid?o configur?.
    /// </summary>
    public string VideoModelUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_t2v_1.3B_fp16.safetensors?download=true";
    /// <summary>
    /// URL source de l’encodeur texte UMT5 configuré.
    /// </summary>
    public string VideoTextEncoderUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/text_encoders/umt5_xxl_fp8_e4m3fn_scaled.safetensors?download=true";
    /// <summary>
    /// URL source du VAE Wan configuré.
    /// </summary>
    public string VideoVaeUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/vae/wan_2.1_vae.safetensors?download=true";
    /// <summary>
    /// URL source du modèle CLIP Vision configuré.
    /// </summary>
    public string VideoClipVisionUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/clip_vision/clip_vision_h.safetensors";
    /// <summary>
    /// URL source du modèle Wan Image→Vidéo configuré.
    /// </summary>
    public string VideoI2vModelUrl { get; set; } =
        "https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors?download=true";

        /// <summary>
    /// Empreinte SHA-256 attendue du mod?le FLUX.2 officiel ; lorsqu?elle est renseign?e, l?installateur v?rifie l?int?grit? avant activation.
    /// </summary>
    public string FluxSha256 { get; set; } =
        "97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6";
    /// <summary>
    /// Empreinte SHA-256 attendue de l’encodeur texte FLUX.2 téléchargé.
    /// </summary>
    public string TextEncoderSha256 { get; set; } =
        "6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a";
    /// <summary>
    /// Empreinte SHA-256 attendue du VAE FLUX.2 téléchargé.
    /// </summary>
    public string VaeSha256 { get; set; } =
        "d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5";
    /// <summary>
    /// Empreinte SHA-256 attendue du modèle Wan principal téléchargé.
    /// </summary>
    public string VideoModelSha256 { get; set; } =
        "be531024cd9018cb5b48c40cfbb6a6191645b1c792eb8bf4f8c1c6e10f924dc5";
    /// <summary>
    /// Empreinte SHA-256 attendue de l’encodeur texte UMT5 téléchargé.
    /// </summary>
    public string VideoTextEncoderSha256 { get; set; } =
        "c3355d30191f1f066b26d93fba017ae9809dce6c627dda5f6a66eaa651204f68";
    /// <summary>
    /// Empreinte SHA-256 attendue du VAE Wan téléchargé.
    /// </summary>
    public string VideoVaeSha256 { get; set; } =
        "2fc39d31359a4b0a64f55876d8ff7fa8d780956ae2cb13463b0223e15148976b";
    /// <summary>
    /// Empreinte SHA-256 attendue du modèle CLIP Vision téléchargé.
    /// </summary>
    public string VideoClipVisionSha256 { get; set; } =
        "64a7ef761bfccbadbaa3da77366aac4185a6c58fa5de5f589b42a65bcc21f161";
    /// <summary>
    /// Empreinte SHA-256 attendue du modèle Wan Image→Vidéo téléchargé.
    /// </summary>
    public string VideoI2vModelSha256 { get; set; } =
        "0ca75338e7a47ca7cacddb7e626647e65829c497387f718ecb6ea0bae456944a";
    // Runtime WebView2 Fixed Version x64 épinglé pour une portabilité réelle.
    // Runtime WebView2 Fixed Version x64 épinglé pour garantir une portabilité réelle.
    /// <summary>
    /// Définit la constante « DefaultWebView2FixedVersion » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string DefaultWebView2FixedVersion = "154.0.4258.53";
    /// <summary>
    /// Définit la constante « DefaultWebView2FixedArchiveUrl » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string DefaultWebView2FixedArchiveUrl =
        "https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/0b89c3a3-0043-4746-b39e-65830da7744d/Microsoft.WebView2.FixedVersionRuntime.154.0.4258.53.x64.cab";
    /// <summary>
    /// Définit la constante « DefaultWebView2FixedSha256 » utilisée comme valeur de référence stable par ce composant.
    /// </summary>
    public const string DefaultWebView2FixedSha256 =
        "ec12b2db6423d127fb8e1935d34e2e68abc70fe8ecb1f6162ba1c1ccc2825f6d";

        /// <summary>
    /// Version exacte du runtime WebView2 Fixed Version embarqu? afin de garantir une interface portable reproductible.
    /// </summary>
    public string WebView2FixedVersion { get; set; } = DefaultWebView2FixedVersion;
    /// <summary>
    /// URL configurée pour le téléchargement de la ressource associée à DefaultWebView2FixedArchiveUrl.
    /// </summary>
    public string WebView2FixedArchiveUrl { get; set; } = DefaultWebView2FixedArchiveUrl;
    /// <summary>
    /// Empreinte SHA-256 attendue pour la ressource associée à DefaultWebView2FixedSha256.
    /// </summary>
    public string WebView2FixedSha256 { get; set; } = DefaultWebView2FixedSha256;

    // Répare les anciennes configurations qui avaient WebView2FixedArchiveUrl vide.
    // Répare les anciennes configurations dans lesquelles WebView2FixedArchiveUrl était vide.
    /// <summary>
    /// Vérifie puis garantit la condition requise par <c>EnsureWebView2FixedDefaults</c> avant de poursuivre.
    /// </summary>
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
