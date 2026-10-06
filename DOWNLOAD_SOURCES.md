# DreamRaster — Sources de téléchargement / Download Sources

Copyright © 2026 Mestoph  
License / Licence: GNU AGPL-3.0-or-later

Cette page liste les sources externes utilisées par DreamRaster pour installer ou mettre à jour ses composants portables.

This page lists the external sources used by DreamRaster to install or update its portable components.

## Téléchargements automatiques / Automatic downloads

| Composant / Component | Utilisation / Purpose | URL utilisée / URL used |
|---|---|---|
| OpenCode Windows x64 | Serveur OpenCode portable / Portable OpenCode server | https://github.com/anomalyco/opencode/releases/latest/download/opencode-windows-x64.zip |
| Ollama Windows amd64 | Runtime Ollama portable / Portable Ollama runtime | https://ollama.com/download/ollama-windows-amd64.zip |
| ComfyUI Windows NVIDIA | Runtime ComfyUI portable | https://github.com/Comfy-Org/ComfyUI/releases/latest/download/ComfyUI_windows_portable_nvidia.7z |
| 7-Zip `7zr.exe` | Extraction de ComfyUI / ComfyUI extraction | https://www.7-zip.org/a/7zr.exe |
| FLUX.2 Klein 4B FP8 | Modèle de génération / Generation model | https://huggingface.co/black-forest-labs/FLUX.2-klein-4b-fp8/resolve/main/flux-2-klein-4b-fp8.safetensors |
| Qwen 3 4B text encoder | Encodeur de texte FLUX.2 / FLUX.2 text encoder | https://huggingface.co/Comfy-Org/flux2-klein/resolve/main/split_files/text_encoders/qwen_3_4b.safetensors |
| FLUX.2 VAE | VAE FLUX.2 | https://huggingface.co/Comfy-Org/flux2-dev/resolve/main/split_files/vae/flux2-vae.safetensors |
| Wan 2.1 T2V 1.3B FP16 | Modèle vidéo texte → vidéo / Text-to-video model | https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/diffusion_models/wan2.1_t2v_1.3B_fp16.safetensors?download=true |
| Wan UMT5 XXL FP8 | Encodeur de texte vidéo / Video text encoder | https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/text_encoders/umt5_xxl_fp8_e4m3fn_scaled.safetensors?download=true |
| Wan 2.1 VAE | VAE vidéo / Video VAE | https://huggingface.co/Comfy-Org/Wan_2.1_ComfyUI_repackaged/resolve/main/split_files/vae/wan_2.1_vae.safetensors?download=true |

## Qwen3-VL optionnel / Optional Qwen3-VL

DreamRaster ne télécharge pas Qwen3-VL via une URL de fichier statique. Quand l'option vision est activée, DreamRaster démarre **son Ollama portable** et appelle son API locale `/api/pull` pour le modèle :

```text
qwen3-vl:8b
```

Page officielle du modèle / Official model page:

https://ollama.com/library/qwen3-vl%3A8b

Qwen3-VL est optionnel et n'est pas requis pour FLUX.2 text-to-image ou image-to-image.

Qwen3-VL is optional and is not required for FLUX.2 text-to-image or image-to-image.

## Mise à jour de DreamRaster / DreamRaster update

Le système de mise à jour consulte la dernière release GitHub :

https://api.github.com/repos/Mestoph/DreamRaster/releases/latest

Le ZIP attendu dans la release est :

```text
DreamRaster-win-x64.zip
```

Dépôt officiel prévu / Planned official repository:

https://github.com/Mestoph/DreamRaster

## WebView2 Fixed Version Runtime

DreamRaster télécharge automatiquement le **Microsoft WebView2 Fixed Version Runtime x64** dans le pack portable lorsqu'il est absent.

DreamRaster automatically downloads the **Microsoft WebView2 Fixed Version Runtime x64** into the portable pack when it is missing.

Version épinglée / Pinned version:

`154.0.4258.53`

Archive officielle Microsoft / Official Microsoft archive:

https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/0b89c3a3-0043-4746-b39e-65830da7744d/Microsoft.WebView2.FixedVersionRuntime.154.0.4258.53.x64.cab

SHA-256 vérifié / Verified SHA-256:

`ec12b2db6423d127fb8e1935d34e2e68abc70fe8ecb1f6162ba1c1ccc2825f6d`

Page officielle de téléchargement / Official download page:

https://developer.microsoft.com/microsoft-edge/webview2/

Documentation Microsoft / Microsoft documentation:

https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution

L'archive CAB est extraite avec `expand.exe`, utilitaire natif de Windows. Le runtime final est conservé uniquement sous `bin/webview2-fixed/`. DreamRaster n'utilise pas automatiquement le runtime WebView2 Evergreen installé globalement.

The CAB is extracted with Windows' native `expand.exe`. The final runtime is kept only under `bin/webview2-fixed/`. DreamRaster does not automatically fall back to a globally installed Evergreen WebView2 Runtime.
## Windows curl.exe

DreamRaster utilise le `curl.exe` fourni par Windows pour les gros téléchargements. Il n'est pas téléchargé par DreamRaster.

DreamRaster uses the `curl.exe` supplied by Windows for large downloads. DreamRaster does not download it.

## Vérification / Verification

Les modèles FLUX.2 et Wan intégrés au processus d'installation sont contrôlés par SHA-256 :

```text
FLUX.2:
97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6

Text encoder:
6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a

VAE:
d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5

Wan 2.1 T2V 1.3B FP16:
be531024cd9018cb5b48c40cfbb6a6191645b1c792eb8bf4f8c1c6e10f924dc5

Wan UMT5 XXL FP8:
c3355d30191f1f066b26d93fba017ae9809dce6c627dda5f6a66eaa651204f68

Wan 2.1 VAE:
2fc39d31359a4b0a64f55876d8ff7fa8d780956ae2cb13463b0223e15148976b
```

## Important

Les URL configurées dans `config/settings.json` peuvent être modifiées par l'utilisateur. Le présent document décrit les valeurs par défaut livrées avec DreamRaster.

URLs stored in `config/settings.json` can be changed by the user. This document describes DreamRaster's shipped defaults.


## Workflows officiels ComfyUI / Official ComfyUI workflows

DreamRaster's bundled API graphs are derived from the official Comfy Org FLUX.2 Klein templates:

- Text to Image: https://github.com/Comfy-Org/workflow_templates/blob/main/templates/image_flux2_klein_text_to_image.json
- Image Edit 4B Distilled: https://github.com/Comfy-Org/workflow_templates/blob/main/templates/image_flux2_klein_image_edit_4b_distilled.json

The exact upstream UI workflow files are preserved under `src/DreamRaster/Assets/Workflows/Official/`.
