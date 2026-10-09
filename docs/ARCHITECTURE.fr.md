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

## Génération vidéo Wan I2V

`VideoGenerator` soumet ses workflows à ComfyUI via `/prompt`, suit `/history/{id}` et gère l'annulation des jobs. En I2V, une image de référence valide est requise.

Pour le modèle **Wan I2V 14B**, DreamRaster démarre ComfyUI en mode `--lowvram` et exécute deux phases :

1. **Calcul latent** avec Wan, UMT5 et CLIP-Vision ; stockage temporaire du latent.
2. **Libération des modèles, arrêt/redémarrage de ComfyUI**, chargement du VAE seul, décodage et export H.264 MP4. Les fichiers latents temporaires sont ensuite nettoyés.

Les délais sont adaptatifs : **20 ou 60 minutes** pour le latent suivant la charge, **8 ou 20 minutes** pour le décodage VAE. L'export Wan I2V est conditionné à la reconnaissance de l'en-tête vidéo ; cela ne remplace pas un décodage complet du fichier.

Les essais locaux validés comprennent **832 × 480, 33 images, 16 FPS, 50 étapes** sur RTX 4060 Ti 16 Go. Voir [Validation vidéo Wan](VIDEO_VALIDATION.fr.md).

## Mise à jour

`GitHubUpdater` consulte `releases/latest`, télécharge l’archive de release dans `runtime\updates`, extrait le nouvel EXE, puis lance un script local qui attend la fermeture de l’application avant de remplacer l’exécutable.


Le mode image→image utilise `flux2_img_to_img_api.json`, copie temporairement l'image source dans le dossier `input` de ComfyUI, règle le `denoise`, puis supprime le fichier temporaire après génération.
