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

Le runtime WebView2 Fixed Version n'est actuellement **pas téléchargé automatiquement** lorsque `WebView2FixedArchiveUrl` est vide. Il doit être fourni avec le pack ou configuré explicitement.

The WebView2 Fixed Version Runtime is currently **not automatically downloaded** when `WebView2FixedArchiveUrl` is empty. It must be supplied with the pack or explicitly configured.

Documentation Microsoft officielle / Official Microsoft documentation:

https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution

## Windows curl.exe

DreamRaster utilise le `curl.exe` fourni par Windows pour les gros téléchargements. Il n'est pas téléchargé par DreamRaster.

DreamRaster uses the `curl.exe` supplied by Windows for large downloads. DreamRaster does not download it.

## Vérification / Verification

Les modèles FLUX.2 intégrés au processus d'installation sont contrôlés par SHA-256 :

```text
FLUX.2:
97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6

Text encoder:
6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a

VAE:
d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5
```

## Important

Les URL configurées dans `config/settings.json` peuvent être modifiées par l'utilisateur. Le présent document décrit les valeurs par défaut livrées avec DreamRaster.

URLs stored in `config/settings.json` can be changed by the user. This document describes DreamRaster's shipped defaults.
