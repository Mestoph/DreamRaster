# Architecture — Français

## Principe

DreamRaster isole ses composants dans la racine portable. `PortablePaths` interdit les chemins sortant du pack pour les exécutables et données pilotés par l’application.

## Services locaux

- OpenCode : `127.0.0.1:54095`
- Ollama : `127.0.0.1:11434`
- ComfyUI : `127.0.0.1:8188`
- proxy image : `127.0.0.1:54100`
- API génération : `127.0.0.1:54101`

Les ports sont configurables.

## Génération

`Flux2Generator` charge un workflow API ComfyUI, remplace le prompt, les modèles, dimensions, steps et seed, envoie le workflow à `/prompt`, suit `/history/{id}` puis copie l’image finale dans `images\`.

## Mise à jour

`GitHubUpdater` consulte `releases/latest`, télécharge l’archive de release dans `runtime\updates`, extrait le nouvel EXE, puis lance un script local qui attend la fermeture de l’application avant de remplacer l’exécutable.


Le mode image→image utilise `flux2_img_to_img_api.json`, copie temporairement l'image source dans le dossier `input` de ComfyUI, règle le `denoise`, puis supprime le fichier temporaire après génération.
