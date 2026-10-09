# Changelog — DreamRaster
## 38.1.0 — stabilization and validated Wan I2V 14B workflow (2026-10-09)

- Consolidated Designer-owned WinForms layout: the former `MainForm.UiPolish.cs` responsibilities are integrated into `MainForm.cs`, and the obsolete file is removed.
- Strengthened Image/Video interactions, configuration persistence, integrated preview/history, runtime model handling, installer safety and diagnostics.
- Enabled editable paths for Wan I2V reference images while validating that the selected file exists before generation.
- Automatically use ComfyUI `--lowvram` for the large Wan I2V 14B model and switch process memory profiles when appropriate.
- Preserved two-phase Wan latent sampling and VAE decoding in a fresh ComfyUI process, with clean intermediate-file handling and cancellation support.
- Adapted generation timeouts to workload: latent sampling 20/60 minutes, VAE decode/encode 8/20 minutes (short/heavy jobs).
- Checked the MP4/WebM/MKV output header before reporting a successful Wan I2V export; this is not a full container integrity check.
- Added focused WinForms/pipeline regression tests; **82/82** automated tests passed with a warning-free Release build.
- Verified local Wan I2V 14B end to end at 832 × 480, 16 FPS, 5/9/17/33 frames at 12 steps and **33 frames at 50 steps**. The full 50-step latent job took about **24 min 31 sec**; the H.264 MP4 decoded successfully in FFmpeg with 33/33 distinct decoded frame hashes.
- Documented tested settings and manual FFprobe/FFmpeg checks in [French](docs/VIDEO_VALIDATION.fr.md) and [English](docs/VIDEO_VALIDATION.en.md).

## 38.0.0 - UI polish, categorized configuration and optional technical tabs

- Promoted DreamRaster application, assembly, file and release-tag version to 38.0.0.
- Kept OpenCode and ComfyUI hidden by default while preserving optional permanent display through Configuration and temporary Dashboard access.
- Kept Image and Video editors usable when required models/backends are missing; only generation actions are blocked.
- Completed automatic persistence for Image/Video generation settings directly from their own tabs without duplicating creative settings in Configuration.
- Reworked the Dashboard with machine, RAM and storage/free-space information.
- Added consistent dark-theme borders and aligned model/style/LoRA rows across Image and Video.
- Added visual section frames around prompt/source, generation, model/preset, preview/output and history areas without introducing overlapping WinForms controls.
- Reorganized Configuration into framed categories for portable location, local services, Image models, Video models, resources/downloads, preferences/updates and technical interfaces.
- Hardened GPU UI restoration so internal WinForms children such as NumericUpDown UpDownEdit are never restored independently.
- Added v38 regression coverage for categorized Configuration and Image/Video header alignment.

## 37.0.0 - Unified Image/Video catalog, LoRA and download experience

- Unified the Image and Video workspaces so model, quality, style, negative prompt, LoRA, preview and history controls share the same layout behavior across compact and large windows.
- Added catalog-backed Image and Video model selectors that keep compatible models visible even when they are not installed, with installed/downloadable/Hugging Face-gated states.
- Added catalog-backed Image and Video LoRA selectors with configurable strength and support for anime, realism, sci-fi and adult/18+ entries.
- Added explicit uncensored/18+ model and LoRA catalog metadata including direct download links, source pages and declared license information where available.
- Added visible Download buttons beside non-installed models and LoRAs, file-size display, dedicated progress bars, cancellation and clear download errors.
- Added cleanup of incomplete destination files and multi-curl fragments after failed or cancelled catalog downloads.
- Added video prompt enhancement and kept Image/Video generation controls aligned while preserving advanced steps, CFG, sampler, scheduler and quality presets.
- Added regression coverage for layout parity, catalog visibility, license metadata, download sizes, progress UI, cancellation cleanup and Video download-button visibility.
- Promoted the application, assembly, file and release-tag version to 37.0.0.

## 36.0.0 - Image/video workspace, Wan robustness and local model flexibility

- Added wheel zoom and left-drag panning to the Image preview, with double-click reset.
- Added Image and Video runtime model selectors plus local safetensors import and direct URL download with optional SHA256 verification; compatible third-party checkpoints can be added without replacing bundled defaults.
- Added Video quality presets while retaining advanced Wan steps/CFG/shift/sampler/scheduler controls.
- Added integrated video preview on history click, external opening on double-click, and a reorganized Video history inspired by the Image tab.
- Added image-reference selection and rectangular crop tooling for video; Wan I2V checkpoints use a dedicated bundled Image-to-Video workflow when the required CLIP-Vision model is present.
- Added VLM prompt extraction from imported/reference images in both Image and Video using the configured portable Vision model.
- Added global Image/Video GPU UI locking so generation, benchmarks and vision extraction cannot collide.
- Added MSTest regression coverage for Image/Video GPU locking and exact UI-state restoration after success, cancellation and timeout.
- Fixed Wan cancellation and timeout by retaining ComfyUI prompt IDs, calling `/interrupt`, deleting queued prompts and cleaning intermediate latents.
- Added a DreamRaster ComfyUI status/retry page instead of exposing raw `ERR_CONNECTION_REFUSED` browser pages, fixed the WebView2 `data:` navigation loop, and made generation wait for port 8188 even when the ComfyUI process is already starting.
- Removed the Video-tab model installer button; model installation remains centralized in Installation.
- Added color-coded categorized log tabs and reduced repeated WebView2 runtime log noise.
- Added CLIP-Vision configuration for Wan I2V and bundled the Wan Image-to-Video API workflow.
- Reworked Image/Video control spacing and aligned the 1.7B/4B comparison button.

## 35.0.3 - Memory safety, HD About artwork and log cleanup

- Fixed the blurry About-tab artwork by embedding and displaying the full 1254x1254 AppIcon PNG instead of stretching the executable icon.
- Detects a low fixed Windows page file and logs a clear warning; the tested machine is limited to a 2 GiB maximum page file.
- Prevents Ollama and portable ComfyUI from competing for committed memory, including stale portable services left by a previous DreamRaster process.
- Converts Windows ERROR_COMMITMENT_LIMIT / page-file start failures into actionable DreamRaster memory guidance.
- Limits portable Ollama to one loaded model and one parallel request on the 16 GiB GPU target.
- Added committed-memory headroom checks before the FLUX VAE decode phase.
- Hardened ManagedProcess cleanup after failed Process.Start calls.
- Fixed repeated WebView2 Fixed Runtime log lines by logging only on first WebView initialization.
- Suppressed expected local-proxy browser/socket disconnect noise while preserving real proxy failures.
- Wan 2.1 now uses a two-phase latent/decode workflow: Wan+UMT5 generate and save the latent, ComfyUI is restarted, then a fresh VAE process decodes and encodes the MP4.
- Wan generation now unloads Ollama before starting ComfyUI.
- Validated FLUX 512x512 split decode and Wan 512x320 / 9-frame / 8 FPS / 4-step generation with the 2 GiB page-file constraint.

## 35.0.2 — Final validation and installer hardening

- Validated a complete Wan 2.1 T2V generation on the RTX 4060 Ti using the portable ComfyUI stack: 512×320, 9 frames, 8 FPS, 4 steps, H.264 MP4 output.
- Confirmed the generated validation video contains 9 frames at 8 FPS, 512×320, with a 1.125 s duration.
- Hardened Windows multi-curl downloads: stalled or faulted segments are cancelled and automatically fall back to the existing single-connection curl path.
- Added a 45-second no-progress watchdog to individual curl range downloads so a dead CDN segment cannot hang the installer indefinitely.
- Updated GitHub Actions to Node 24-compatible actions/checkout@v7 and actions/setup-dotnet@v6.
- Revalidated the integrated 1.7B/4B benchmark output: 8 PNG images plus JSON/CSV timing reports are produced successfully.

## 35.0.1 — FLUX memory stability and benchmark hardening

- Split FLUX.2 generation into a sampling phase and a lightweight VAE decode phase so the large Qwen/FLUX models can be released before image decoding.
- Added a targeted ComfyUI 0.38.0 Windows AIMDO compatibility patch with a preserved original-file backup and safe fallback behavior.
- Hardened the portable Ollama/ComfyUI GPU handoff and releases every actually loaded Ollama model before FLUX starts.
- Improved benchmark cleanup by stopping ComfyUI between cases and waiting for safe Windows commit availability.
- Made the optional Vision-model preflight derive the Ollama manifest path from the configured model name instead of hard-coding qwen3-vl:8b.
- Completed config/settings.sample.json with the current image, prompt, seed, video, ports, model URLs and SHA-256 settings.
- Validated a real 1024x1024, 4-step, fixed-seed FLUX.2 generation after the memory changes.
- Validated the integrated 8-image qwen3:1.7b vs qwen3:4b benchmark and its JSON/CSV report output.

## 35.0.0 — Portable WebView2 Fixed Runtime

- Added automatic installation of Microsoft WebView2 Fixed Version Runtime x64 when the portable runtime is missing.
- Pinned WebView2 Fixed Version Runtime `154.0.4258.53` and documented the official Microsoft CAB download URL.
- Added SHA-256 verification for the WebView2 CAB: `ec12b2db6423d127fb8e1935d34e2e68abc70fe8ecb1f6162ba1c1ccc2825f6d`.
- CAB extraction uses the native Windows `expand.exe`; the extracted runtime remains entirely under `bin/webview2-fixed/`.
- Fixed portable runtime discovery so DreamRaster finds `msedgewebview2.exe` recursively inside versioned Fixed Runtime directories.
- Added WebView2 Fixed Runtime to portable preflight checks, so a missing runtime is treated like any other missing portable component.
- DreamRaster continues to refuse silent fallback to a globally installed Evergreen WebView2 Runtime.
- Updated bilingual installation and download-source documentation.
- Added automatic migration for legacy settings where `WebView2FixedArchiveUrl` was empty; the pinned version, URL and SHA-256 are repaired and persisted at startup.
- Validated the missing-runtime repair path on the portable installation: cached CAB verified, 257 files extracted, and DreamRaster WebView2 child processes confirmed to launch from `bin/webview2-fixed/`.
- Added categorized Logs sub-tabs for UI, OpenCode, Ollama, ComfyUI, FLUX.2, Video, Installation and System, with repaint fixes and double buffering.
- Added editable Vision, Prompt AI, FLUX, Text Encoder and VAE model selectors with clear installed/missing/compatible states.
- Added lightweight safetensors-header validation for FLUX.2 Klein diffusion models, Qwen3 Klein text encoders and FLUX.2 VAEs.
- Added a predefined negative prompt and passes exclusions to the official Klein Distilled Qwen conditioning path as an Avoid instruction.
- Width, height and steps now synchronize with bundled workflows; image-to-image uses exact ImageScale dimensions instead of a megapixel approximation.
- Added automatic local prompt enhancement through Ollama with structured JSON output, separate Prompt AI model selection, and automatic GPU handoff between Ollama and ComfyUI.
- Added fixed or random generation seeds and visible Prompt / FLUX / Total timing metrics.
- Added an integrated qwen3:1.7b vs qwen3:4b benchmark: four categories × two models, fixed seeds, 8 generated images and JSON/CSV reports.
- Added a Wan 2.1 text-to-video tab with prompt, negative prompt, width/height, frames, FPS, steps, progress, cancellation and MP4 output.
- Added explicit Wan model installation with user confirmation, official Comfy-Org URLs and SHA-256 verification; no multi-gigabyte video download starts automatically.
- Added safe restart of stale portable Ollama, OpenCode and ComfyUI processes after DreamRaster restarts, restoring valid console pipes while still rejecting external/system services.
- Added Ollama API-readiness polling and stable ComfyUI launch flags for the tested Windows/RTX 4060 Ti configuration.
- Validated end-to-end Auto Prompt -> Ollama -> VRAM release -> ComfyUI -> FLUX.2 at 1024x1024, 4 steps and fixed seed 1001.


## 34.0.0 — Official FLUX.2 Klein workflows

- Replaced the hand-built FLUX.2 graphs with API graphs equivalent to the official Comfy Org FLUX.2 Klein 4B Distilled workflows.
- `CLIPLoader` now uses `type=flux2`, matching the official Qwen 3 4B / FLUX.2 conditioning path.
- Text-to-image now uses `EmptyFlux2LatentImage`, `Flux2Scheduler`, `CFGGuider`, `ConditioningZeroOut`, `KSamplerSelect`, and `SamplerCustomAdvanced`.
- Image editing now uses the official `ReferenceLatent` + `VAEEncode` conditioning path with `ImageScaleToTotalPixels` and `GetImageSize`.
- `Flux2Generator` now locates and mutates workflow nodes by `class_type` instead of brittle numeric IDs.
- ComfyUI execution errors are detected immediately from `/history` instead of holding the generation semaphore until timeout.
- Bundled workflows are refreshed when the embedded graph changes, so existing portable installations no longer keep an obsolete workflow forever.
- Added exact upstream Comfy Org workflow templates under `Assets/Workflows/Official/` and MIT attribution in `THIRD_PARTY_NOTICES.md`.
- Validated the official distilled text-to-image API graph on ComfyUI 0.38.0 with the installed RTX 4060 Ti: 512×512, 4 steps, PNG generated successfully.
- Validated the official single-reference Image Edit graph on the same ComfyUI installation: 512×512 reference, 4 steps, PNG generated successfully.
- The legacy img2img strength control is retained only for configuration/API compatibility and disabled in the UI because the official FLUX.2 Klein Image Edit workflow has no denoise/strength input.
- The previous v33 failure `mat1 and mat2 shapes cannot be multiplied (512x2560 and 7680x3072)` was caused by the old `CLIPLoader type=stable_diffusion` path.

Ce changelog retrace l’évolution complète du projet depuis les premiers prototypes **OpenCode Local AI** jusqu’à **DreamRaster**.

> Historique reconstruit à partir des archives source, des notes de développement conservées et des changements effectivement intégrés au projet. Certaines versions intermédiaires n’ont pas été publiées publiquement ; elles restent documentées ici car elles correspondent à des jalons techniques réels.

## [33.0.0] — 2026-10-02

### Structure du dépôt et build Linux

- Réorganisation complète du dépôt :
  - `src/DreamRaster/` pour le projet WinForms, les sources, le Designer, les assets et le profil de publication ;
  - `scripts/windows/` pour les scripts Windows ;
  - `scripts/linux/` pour le cross-build Linux → Windows ;
  - `scripts/common/` pour le diagnostic PowerShell partagé ;
  - `docs/`, `config/`, `.github/` et `.nuget/` conservés à la racine.
- Ajout de `DreamRaster.sln` à la racine pour Visual Studio.
- Ajout de `<EnableWindowsTargeting>true</EnableWindowsTargeting>` afin de permettre au SDK .NET 9 sous Linux/macOS de compiler la cible Windows.
- Ajout des scripts Linux :
  - `build-debug.sh` ;
  - `build-release.sh` ;
  - `publish-windows.sh` ;
  - `build-diagnostic.sh`.
- Le build Linux produit toujours une application **Windows x64 WinForms** ; il ne s’agit pas d’un port Linux natif.
- Ajout d’un job GitHub Actions Windows classique.
- Ajout d’un job GitHub Actions Linux cross-build vers `win-x64`.
- Ajout d’un workflow Linux cross-publish vérifiant la sortie portable.
- Le cache NuGet reste isolé dans `.nuget/packages/` à la racine du dépôt.
- L’objectif de publication reste : **un seul `DreamRaster.exe` dans la racine de publication locale**.
- La release GitHub ajoute autour de l’EXE les documents juridiques et de projet nécessaires.
- Validation locale Release effectuée avant la première release v33 :
  - SDK .NET `9.0.317` ;
  - `dotnet restore` OK ;
  - `dotnet build -c Release -r win-x64` OK ;
  - **0 erreur, 0 avertissement**.

## [32.0.0]

### DreamRaster et sources de téléchargement

- Nom public définitif adopté : **DreamRaster**.
- Projet renommé `DreamRaster.csproj`.
- Exécutable renommé `DreamRaster.exe`.
- Asset de release renommé `DreamRaster-win-x64.zip`.
- Dépôt cible renommé `Mestoph/DreamRaster`.
- Ajout de `DOWNLOAD_SOURCES.md` avec les URL externes par défaut utilisées ou référencées par l’application :
  - OpenCode Windows x64 ;
  - Ollama Windows amd64 ;
  - ComfyUI Windows NVIDIA ;
  - `7zr.exe` ;
  - FLUX.2 Klein 4B FP8 ;
  - Qwen 3 4B text encoder ;
  - FLUX.2 VAE ;
  - page officielle Qwen3-VL `qwen3-vl:8b` ;
  - API GitHub Releases de DreamRaster ;
  - documentation officielle WebView2 Fixed Version Runtime.
- L’URL 7-Zip n’est plus codée en dur dans l’installeur : elle devient `SevenZipUrl` dans la configuration.
- Qwen3-VL est documenté comme téléchargement via l’API locale Ollama `/api/pull`, et non via une URL de fichier statique.
- La release GitHub inclut désormais `DOWNLOAD_SOURCES.md`.
- Compatibilité de migration conservée pour les anciens noms d’EXE lors de la résolution de la racine portable.

## [31.0.0]

### CLA, NOTICE et préparation GitHub

- Étape de transition de branding : le nom **Mestoph AI Studio** a été utilisé temporairement avant l’adoption de DreamRaster en v32.
- Ajout de `CLA.md`, Contributor License Agreement bilingue FR/EN.
- Le contributeur conserve son copyright sur ses contributions originales.
- Le contributeur accorde à Mestoph les droits nécessaires pour intégrer, modifier et distribuer sa contribution avec le projet sous AGPL-3.0-or-later.
- Ajout d’une clause de licence de brevet limitée aux revendications nécessairement enfreintes par la contribution.
- Ajout de `NOTICE`, bilingue FR/EN, avec :
  - Copyright © 2026 Mestoph ;
  - licence GNU AGPL-3.0-or-later ;
  - mentions à conserver dans les distributions ;
  - rappel que les noms/marques des projets tiers restent à leurs propriétaires.
- Ajout d’une case d’acceptation du CLA dans le modèle de pull request.
- Ajout du workflow `.github/workflows/cla-check.yml` qui refuse une PR sans confirmation CLA.
- Mise à jour de `CONTRIBUTING.md`, `COPYRIGHT.md`, GitHub Actions, scripts de création du dépôt et scripts de release.

## [30.0.0]

### Licence open source et propriété

- Mestoph devient explicitement l’auteur et titulaire du copyright du code original.
- Copyright ajouté : `Copyright © 2026 Mestoph`.
- Adoption de **GNU AGPL-3.0-or-later**.
- Ajout du texte officiel GNU AGPL v3 dans `LICENSE`.
- Ajout de `PackageLicenseExpression=AGPL-3.0-or-later` dans le projet.
- Ajout de `SPDX-License-Identifier: AGPL-3.0-or-later` dans les sources.
- Mise à jour de l’onglet À propos avec auteur, copyright et licence.
- Mise à jour des README, de CONTRIBUTING et de la documentation GitHub.

## [29.0.0]

### Rapport de compilation HTML

- Ajout d’un rapport HTML bilingue autonome.
- Résumé visuel de la compilation avec cartes :
  - erreurs ;
  - avertissements ;
  - restore ;
  - build.
- Erreurs mises en évidence en rouge.
- Avertissements mis en évidence en ambre.
- Tableaux détaillés avec code, fichier, ligne, colonne et message.
- Section environnement avec SDK .NET, OS, architecture, projet et configuration.
- Liens vers les logs générés.
- Mise en page responsive.
- Le rapport HTML s’ouvre automatiquement après le diagnostic.
- Fallback vers le rapport texte si l’ouverture HTML échoue.
- Fichier « latest » ajouté : `LATEST_BUILD_REPORT_FR_EN.html`.

## [28.0.0]

### Diagnostic de compilation automatisé

- Ajout de `BUILD_DIAGNOSTIC.bat` et d’un diagnostic Release séparé.
- Ajout du moteur PowerShell de diagnostic.
- Exécution automatique de :
  - `dotnet restore` ;
  - `dotnet build`.
- Capture de stdout/stderr.
- Extraction et déduplication des erreurs/avertissements :
  - `CSxxxx` ;
  - `NUxxxx` ;
  - `MSBxxxx` ;
  - `NETSDKxxxx` ;
  - autres codes connus.
- Génération de :
  - rapport Markdown bilingue ;
  - rapport texte bilingue ;
  - log brut ;
  - logs restore/build séparés.
- Collecte de la version SDK .NET, OS et architecture.
- Copie des derniers rapports sous `build-reports/LATEST_*`.

## [27.0.0]

### Corrections de compilation

- Correction de l’erreur `CS0201` dans `GitHubUpdater.cs`.
- Remplacement de l’expression invalide `Process.Start(...) ?? throw ...` par un appel puis une vérification explicite.
- Ajout de `#nullable enable` dans le Designer pour supprimer `CS8669`.
- Suppression des deux avertissements `CS8602` WebView2 par capture locale sûre de `CoreWebView2`.
- Suppression du champ Ollama devenu inutilisé.
- Conservation du mode image-to-image de v26.

## [26.0.0]

### Image-to-image réel dans l’interface

- Ajout du mode **Image → Image** dans l’onglet Générer.
- Sélecteur :
  - Texte → image ;
  - Image → image.
- Sélection d’une image source via boîte de dialogue.
- Bouton d’effacement de l’image source.
- Prévisualisation de l’image sélectionnée et du résultat.
- Réglage du `denoise` / force img2img.
- Utilisation de `flux2_img_to_img_api.json`.
- Copie temporaire de l’image source dans le dossier `input` du ComfyUI portable.
- Injection du nom de l’image dans le nœud `LoadImage`.
- Réglage du `denoise` dans le KSampler.
- Suppression du fichier source temporaire après génération.
- Le chemin text-to-image existant reste rétrocompatible.

## [25.0.0]

### À propos, traduction, GitHub et Qwen optionnel

- Ajout d’un onglet **À propos**.
- Ajout de la traduction de l’interface Français / English.
- Langue enregistrée dans `config/settings.json`.
- Ajout d’un système de vérification des GitHub Releases.
- Ajout d’un mécanisme de mise à jour différée de l’EXE :
  - téléchargement dans `runtime/updates` ;
  - extraction en staging ;
  - fermeture propre des services ;
  - remplacement de l’EXE après sortie du processus ;
  - redémarrage.
- Ajout de l’icône orientée génération d’image.
- Ajout de la documentation GitHub FR/EN :
  - README ;
  - installation ;
  - architecture ;
  - updater ;
  - code guide ;
  - Qwen3-VL ;
  - préparation GitHub.
- Ajout des workflows Build/Release GitHub.
- Qwen3-VL devient **optionnel et désactivé par défaut**.
- Qwen3-VL n’est plus considéré comme requis pour FLUX.2/ComfyUI.
- Ajout d’en-têtes/commentaires structurels bilingues dans les fichiers C#.

## [24.0.0]

### Progression réelle du téléchargement Ollama

- Le téléchargement du modèle Ollama utilise directement l’API streaming du serveur Ollama portable `/api/pull`.
- Suppression du parsing fragile de la sortie console de `ollama pull`.
- Affichage dans Installation et Console Ollama :
  - statut du pull ;
  - digest/blob en cours ;
  - pourcentage ;
  - octets reçus / taille totale ;
  - vitesse estimée en MiB/s ;
  - vérification du digest ;
  - écriture du manifeste ;
  - succès ou erreur détaillée.
- Exemple de log :
  - `[Ollama pull] qwen3-vl:8b · pulling sha256:... · 42% · 1.73 GiB/4.12 GiB · 11.8 MiB/s`.

## [23.0.0]

### Workflows ComfyUI intégrés

- Ajout des workflows :
  - `flux2_text_to_image_api.json` ;
  - `flux2_img_to_img_api.json` ;
  - `flux2_workflow_api.json` comme alias de compatibilité.
- Les workflows sont embarqués dans l’EXE et extraits automatiquement dans `workflows/` s’ils sont absents.
- Les workflows existants et non vides ne sont pas écrasés.
- `Flux2Generator` recherche le nouveau workflow text-to-image avec fallback sur l’ancien alias.
- À ce stade, l’UI utilisait uniquement le text-to-image ; le fichier img2img était livré pour usage futur/manuel.
- Note historique importante : le workflow FLUX.2 validé utilisé auparavant n’était pas disponible dans l’environnement de reconstruction. Les templates v23 ont donc été construits à partir de la structure connue et n’avaient pas été validés par un test GPU au moment de leur intégration.

## [22.0.0]

### Publication sans XML WebView2

- Suppression de `Microsoft.Web.WebView2.Core.xml` de la publication.
- Suppression de `Microsoft.Web.WebView2.WinForms.xml`.
- Ajout de `AllowedReferenceRelatedFileExtensions=.pdb`.
- Ajout d’une cible MSBuild de nettoyage après publication.
- Le script de publication supprime aussi explicitement les XML résiduels.
- Objectif confirmé : racine de publication avec uniquement l’EXE avant premier lancement.

## [21.0.0]

### Un seul EXE à la racine

- `WebView2Loader.dll` est intégré comme ressource de l’EXE.
- Au premier lancement, le loader natif est extrait dans `runtime/native/WebView2Loader.dll`.
- Les DLL managées WebView2 restent intégrées au single-file.
- Aucun sidecar WebView2 volontairement laissé à côté de l’EXE.
- Pas d’extraction automatique du runtime .NET dans `%TEMP%/.net`.
- Les dossiers `config`, `runtime`, `models`, `downloads`, `logs`, `workflows`, etc. sont créés après lancement.
- Le script de publication contrôle les fichiers supplémentaires à la racine.

## [20.0.0]

### Multi-curl corrigé et WebView2 hors racine

- Correction de `curl: (3) URL using bad/illegal format or missing URL`.
- Le multi-curl reçoit désormais l’URL d’origine, pas l’URL CDN signée finale obtenue par HttpClient.
- Chaque processus curl suit ses propres redirections.
- URL passée via `--url` avec `ProcessStartInfo.ArgumentList`.
- Les logs n’exposent plus les query strings signées ; seul le schéma/hôte est journalisé.
- `WebView2Loader.dll` est déplacé vers `runtime/native`.
- Ajout d’un résolveur natif pour charger le loader depuis le pack portable.

## [19.0.0]

### Single-file sans extraction TEMP

- Retour à une publication single-file self-contained.
- Paramètres principaux :
  - `PublishSingleFile=true` ;
  - `SelfContained=true` ;
  - `IncludeNativeLibrariesForSelfExtract=false` ;
  - `IncludeAllContentForSelfExtract=false` ;
  - compression du bundle ;
  - pas de trimming ;
  - pas de ReadyToRun ;
  - pas de symboles/debug dans la publication.
- Suppression du problème où le runtime portable se retrouvait sous `%TEMP%/.net`.
- Les DLL managées .NET sont intégrées au bundle.
- La racine portable reste déterminée par le vrai EXE.

## [18.0.0]

### Vraie racine portable, Configuration et multi-curl

- `PortablePaths.Root` utilise le chemin du vrai processus `OpenCodeLocalAI.exe`.
- La racine portable n’est plus `AppContext.BaseDirectory` lorsqu’un single-file s’extrait temporairement.
- Création automatique de :
  - `bin` ;
  - `config` ;
  - `downloads` ;
  - `images` ;
  - `logs` ;
  - `models` ;
  - `runtime` ;
  - `workflows` ;
  - `workspace`.
- Création automatique de `config/settings.json`.
- Ajout d’un vrai onglet **Configuration** :
  - ports OpenCode/Ollama/ComfyUI/Proxy/API ;
  - noms des modèles ;
  - dimensions/steps ;
  - seuils VRAM/RAM ;
  - paramètres de téléchargement ;
  - arrêt ComfyUI après génération.
- Publication temporairement passée en dossier self-contained pour résoudre le problème `%TEMP%/.net`.
- Ajout du moteur **multi-curl** :
  - jusqu’à 8 connexions Range par défaut ;
  - segments dans des fichiers séparés ;
  - aucun accès concurrent aléatoire au même fichier ;
  - assemblage séquentiel ;
  - aucun plafond de débit applicatif ;
  - fallback mono-curl puis HttpClient.
- Les logs de démarrage affichent version, racine et fichier de configuration.

## [17.0.0]

### curl.exe natif Windows

- Retrait d’aria2 du chemin principal de téléchargement.
- Utilisation exclusive de `curl.exe` fourni par Windows via `Environment.SystemDirectory`.
- Aucun téléchargement de curl.
- Aucun lookup PATH pour curl.
- Reprise via `--continue-at -`.
- Redirections, retries et annulation pris en charge.
- Fichier temporaire puis renommage après succès.
- Fallback HttpClient si curl Windows échoue.

## [16.0.0]

### Correction du verrou aria2

- Correction du verrou de fichier lors du renommage de l’archive aria2.
- Les flux sont désormais fermés avant `File.Move`.
- Ajout d’un retry court pour les handles conservés temporairement par antivirus/Defender.
- Ajout d’un log explicite indiquant que le moteur aria2 est réellement actif.

## [15.0.0]

### Scripts build et publication

- Ajout de `BUILD_DEBUG.bat`.
- Nettoyage `bin`/`obj`, restore puis build Debug.
- Ajout de `PUBLISH_PORTABLE.bat`.
- Restore `win-x64`, build/publish Release et ouverture du dossier de sortie.
- Le cache NuGet local `.nuget/packages` n’est pas supprimé.

## [14.0.0]

### aria2 portable

- Téléchargement automatique d’aria2 1.37.0 Windows x64 dans le pack.
- Vérification SHA-256 de l’archive aria2.
- Utilisation uniquement du `aria2c.exe` portable.
- 16 connexions par fichier.
- Pas de limite de débit.
- Reprise native aria2.
- Allocation de fichier désactivée.
- Affichage de progression et vitesse.
- Annulation limitée au processus aria2 du pack.
- Fallback HttpClient si aria2 ne peut pas être installé.

## [13.0.0]

### Console Ollama dédiée

- Ajout d’un onglet **Console Ollama**.
- Console sombre avec rendu ANSI.
- Routage des sources Ollama vers cette console tout en conservant les logs globaux.

## [12.0.0]

### Téléchargement séquentiel type navigateur

- Abandon du téléchargement multi-segment sur un même fichier à cause des performances d’écriture aléatoire.
- Flux réseau unique et séquentiel.
- Écriture disque séquentielle.
- HTTP/3 si disponible, sinon HTTP/2 puis HTTP/1.1.
- Aucun plafond de débit.
- Buffer 8 MiB.
- Reprise `.part.single`.
- Protocole négocié affiché dans les logs.
- Nettoyage des anciens fichiers segmentés.

## [11.0.0]

### Suppression de toute limite de débit

- Aucun limiteur logiciel.
- `MaxConnectionsPerServer = int.MaxValue`.
- 8 segments simultanés par défaut pour les gros fichiers.
- Seuil parallèle abaissé à 64 MiB.
- Progression et vitesse instantanée en MiB/s.
- `DownloadConnections` contrôle les connexions, pas un plafond de débit.

## [10.0.0]

### Téléchargements segmentés accélérés

- Jusqu’à 4 connexions HTTP Range pour les fichiers ≥128 MiB.
- Détection du support Range.
- Reprise des segments terminés.
- Reprise mono-connexion via `.part.single`.
- Buffers réseau/disque 4 MiB.
- HTTP/2 demandé.
- Fallback mono-connexion si Range indisponible.
- Ajout des paramètres `DownloadConnections` et `ParallelDownloadThresholdMiB`.

## [9.0.0]

### ComfyUI intégré et profil de publication

- Ajout de l’onglet ComfyUI avec WebView2 séparé.
- Ajout du bouton « Ouvrir ComfyUI intégré ».
- Profil WebView2 portable séparé `runtime/webview2-comfy`.
- Couleurs dédiées pour les sources UI/WebView.
- Builds Debug/Release normaux rendus plus légers.
- Déplacement du self-contained/single-file vers `Portable.pubxml`.

## [8.0.0]

### Corrections WebView2 et thème

- Correction de l’ordre des types dans le switch de thème WinForms.
- `FlowLayoutPanel` / `TableLayoutPanel` traités avant `Panel`.
- Références WebView2 Core + WinForms prises depuis `lib_manual/netcoreapp3.0`.
- Chemins calculés depuis le cache NuGet local.
- Aucune référence WPF WebView2.

## [7.0.0]

### Réactivité et logs

- Mise en file des logs processus.
- Rendu par lots toutes les 100 ms.
- Maximum 60 entrées par tick UI.
- Écriture disque regroupée et asynchrone.
- Limitation des callbacks de progression.
- Réduction de la verbosité de 7-Zip.
- Annulation avec nettoyage des extractions partielles.
- Réapplication récursive du thème sombre.
- WebView2 Core + WinForms uniquement.
- Suppression de l’import WPF.

## [6.0.0]

### Installeur réactif et thème uniforme

- Le formulaire n’est plus désactivé pendant les opérations asynchrones.
- Bouton Annuler disponible pendant l’installation.
- CancellationToken propagé aux téléchargements, extraction et pull Ollama.
- Fermeture pendant installation = annulation + nettoyage.
- Extraction ZIP asynchrone/cancellable.
- Logs disque hors thread UI.
- Progression Ollama limitée pour éviter de saturer l’interface.
- Taille des RichTextBox de logs plafonnée.
- Palette sombre uniforme.
- Contraste de l’installateur renforcé.
- Codes ANSI sombres remappés vers des couleurs lisibles.
- Onglets dessinés avec la palette de l’application.

## [5.0.0]

### Designer WinForms conventionnel

- Tous les contrôles sont déclarés/configurés dans `MainForm.Designer.cs`.
- `InitializeComponent()` devient conventionnel et compatible Visual Studio Designer.
- Les onglets du moment sont visibles dans le Designer.
- Suppression des helpers personnalisés utilisés auparavant pour construire l’interface.

## [4.0.0]

### NuGet local et WinForms-only

- Restauration NuGet dans `.nuget/packages` au sein du projet.
- Suppression de la dépendance au cache global `%USERPROFILE%/.nuget`.
- `UseWPF=false`.
- Suppression de `Microsoft.Web.WebView2.Wpf.dll` afin d’éviter les conflits `WindowsBase`.
- Références explicites WebView2 Core + WinForms.

## [3.0.0]

### Vrai split Designer

- Introduction de :
  - `MainForm.cs` ;
  - `MainForm.Designer.cs` ;
  - `MainForm.resx`.
- Le formulaire devient éditable dans le Visual Studio WinForms Designer.

## [2.0.0]

### Corrections de compilation et durcissement portable

- `DiagnosticExporter.cs` n’utilise plus de littéral de chaîne brute interpolée fragile.
- Génération du diagnostic via `StringBuilder`.
- Suppression de SharpCompress.
- Extraction ComfyUI via `7zr.exe` pack-local.
- Suppression de la référence WebView2 WPF inutile.
- Gestion globale des erreurs renforcée :
  - `Application.ThreadException` ;
  - AppDomain unhandled exception ;
  - tâches non observées ;
  - journalisation des erreurs.
- Introduction des chemins portables contrôlés et rejet des chemins sortant du pack.
- Gestion explicite des composants portables manquants.
- Les erreurs UI normales ne doivent plus terminer l’application brutalement.

## [1.0.0] — baseline historique reconstruit

### Fondations d’OpenCode Local AI

- Premier socle de l’application Windows portable.
- Principe fondamental : ne jamais utiliser silencieusement les installations système d’Ollama, OpenCode ou ComfyUI.
- Les exécutables des composants sont résolus sous le dossier portable.
- Les composants manquants déclenchent une proposition d’installation.
- OpenCode est lancé avec :
  - `serve` ;
  - hôte `127.0.0.1` ;
  - port configurable ;
  - `BROWSER=none` ;
  - `NO_BROWSER=1`.
- OpenCode est intégré dans WebView2 au lieu d’ouvrir Chrome ou une nouvelle fenêtre.
- Les navigations externes et nouvelles fenêtres WebView2 sont bloquées.
- Utilisation d’un runtime WebView2 Fixed Version portable.
- Redirection des environnements des processus enfants vers le pack :
  - `HOME` ;
  - `USERPROFILE` ;
  - `APPDATA` ;
  - `LOCALAPPDATA` ;
  - `TEMP` / `TMP` ;
  - cache CUDA ;
  - variables XDG OpenCode ;
  - `OLLAMA_MODELS`.
- Logs live avec interprétation ANSI.
- Proxy HTTP/WebSocket local sur le port historique `54100`.
- API locale de génération sur le port historique `54101`.
- Générateur FLUX.2 via ComfyUI.
- Exposition des images générées via `/local-images/...`.
- Arrêt des services OpenCode, Ollama, ComfyUI, proxy et API lors de la fermeture.
- L’application a été conçue pour rester entièrement portable : configuration, caches, modèles, logs et données générées doivent rester dans le pack.

---

## Jalons de validation historiques

- Une validation complète « fresh portable install » a été réalisée avec succès sur Windows et une NVIDIA RTX 4060 Ti.
- Le diagnostic global était OK.
- Le runtime WebView2 Fixed Version portable était utilisé.
- Ollama, OpenCode et ComfyUI provenaient bien du pack portable.
- Le modèle Qwen3-VL et FLUX.2 ont été installés dans le pack lors de ce test historique.
- Le contrôle anti-ouverture-Chrome a réussi.
- Une génération FLUX.2 512×512 a réussi.
- Les services ont été arrêtés proprement à la fin du test.
- Les tests GPU lourds ne sont plus répétés inutilement dans les itérations suivantes afin d’éviter une charge inutile.

## Architecture et principes conservés au fil des versions

- Port historique OpenCode : `54095`.
- Port historique Ollama : `11434`.
- Port historique ComfyUI : `8188`.
- Port historique proxy : `54100`.
- Port historique API de génération : `54101`.
- Portabilité stricte des composants applicatifs.
- `nvidia-smi` reste autorisé comme composant système car il appartient au pilote NVIDIA.
- `curl.exe` Windows est autorisé comme composant système natif.
- Les dépendances propres à DreamRaster restent pack-locales.
- La publication Windows cible `win-x64`, self-contained et single-file.
- La racine de publication locale vise un seul EXE avant premier lancement.
- Après lancement, les sous-dossiers portables sont créés sous la racine de l’application.
- Le projet conserve un vrai Visual Studio WinForms Designer.
- L’interface reste sombre, compacte et uniforme.
- Les logs restent colorés et lisibles.
- Qwen3-VL est optionnel pour les fonctions de vision et n’est pas requis pour FLUX.2 text-to-image ou image-to-image.

## Modèles FLUX.2 — hashes historiques de référence

- FLUX.2 Klein 4B FP8 :
  `97ed34fe0567e436200f2faee3939b88f2b5d99f8af2a4dc16532c4245c0ccb6`
- Text encoder :
  `6c671498573ac2f7a5501502ccce8d2b08ea6ca2f661c458e708f36b36edfc5a`
- VAE :
  `d64f3a68e1cc4f9f4e29b6e0da38a0204fe9a49f2d4053f0ec1fa1ca02f9c4b5`

## Noms historiques

- `OpenCode Local AI` — nom d’origine.
- `Mestoph AI Studio` — nom de transition durant la préparation CLA/NOTICE/GitHub.
- `DreamRaster` — nom public retenu depuis v32.

## Notes historiques détaillées

Les notes brutes et plus longues des versions anciennes restent archivées dans :

`docs/CHANGELOG_LEGACY.md`