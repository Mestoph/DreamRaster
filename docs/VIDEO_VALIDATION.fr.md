# Validation vidéo Wan 2.1 — Windows

Ce document décrit les réglages Wan I2V 14B et les résultats **mesurés localement le 9 octobre 2026** sur une NVIDIA GeForce RTX 4060 Ti 16 Go et 32 Go de RAM. Ils ne constituent pas une garantie de performance sur les autres configurations.

## Workflow Image vers vidéo

1. Dans l'onglet **Vidéo**, choisir `wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors`.
2. Fournir une **image de référence existante** (champ éditable ou bouton de sélection) et un prompt.
3. Régler la résolution, les images, le FPS, les étapes et les paramètres d'échantillonnage.
4. Lancer la génération ; le modèle Wan I2V 14B déclenche automatiquement un démarrage ComfyUI en `--lowvram`.
5. DreamRaster calcule le latent, libère les modèles, redémarre ComfyUI puis charge le VAE dans un processus propre pour produire le MP4 H.264.
6. Vérifier l'aperçu et le fichier dans le dossier portable `videos\`. Le bouton **Annuler** interrompt le job ComfyUI.

Le nombre d'étapes I2V doit être **au moins 12**. Les essais courts à 12 étapes servent à valider le pipeline, pas à établir la qualité visuelle finale.

## Presets conseillés pour cette machine

| Usage | Dimensions | Images | FPS | Étapes | Statut |
| --- | --- | ---: | ---: | ---: | --- |
| Essai rapide | 832 × 480 | 17 | 16 | 20 | Recommandation, non mesurée |
| Équilibré | 832 × 480 | 33 | 16 | 30 | Recommandation, non mesurée |
| Qualité | 832 × 480 | 33 | 16 | 40 | Recommandation, non mesurée |
| Final | 832 × 480 | 33 | 16 | 50 | **Validé de bout en bout** |

L'interface possède déjà les niveaux **Rapide 20**, **Standard 30**, **Qualité 40** et **Meilleure 50** étapes. Le nombre d'images reste à régler séparément si nécessaire. Valeurs de référence : CFG 6, sampling shift 8, sampler `uni_pc`, scheduler `simple`. En I2V, l'image de référence est obligatoire.

## Essais réels du 9 octobre 2026

Tous les essais ci-dessous utilisent le **modèle Wan I2V 14B**, `832 × 480`, 16 FPS et une image de référence, sauf le premier essai antérieur à `256 × 256`.

| Résolution | Images | Étapes | Résultat | MP4 |
| --- | ---: | ---: | --- | --- |
| 256 × 256 | 5 | 12 | Génération I2V et décodage FFmpeg réussis | 5 images |
| 832 × 480 | 5 | 12 | Génération I2V et décodage FFmpeg réussis | 5 images |
| 832 × 480 | 9 | 12 | Génération I2V et décodage FFmpeg réussis | 9 images |
| 832 × 480 | 17 | 12 | Génération I2V et décodage FFmpeg réussis | 17 images |
| 832 × 480 | 33 | 12 | Génération I2V et décodage FFmpeg réussis | 33 images |
| **832 × 480** | **33** | **50** | **50/50 étapes, export et décodage FFmpeg réussis** | **33 images** |

Le dernier calcul latent a pris environ **24 min 31 s**. Son MP4 H.264 fait **2,0625 s**, comporte **33 images distinctes par empreinte de décodage**, et FFmpeg l'a intégralement décodé sans erreur. Exemple de sortie mesurée dans l'installation de test : `videos\DreamRaster_WAN_2026-10-09T14-54-06-435Z_00001_.mp4`.

Des empreintes différentes et un décodage réussi **ne prouvent pas** une qualité esthétique ou une bonne fluidité du mouvement. Comparer les vidéos avec le même prompt et la même image pour évaluer la qualité.

## Diagnostic

Le dossier `logs\` de l'installation portable conserve les journaux. En cas d'échec, rechercher le chargement `LOW_VRAM`, les erreurs CUDA, la progression des étapes, le redémarrage entre calcul latent et VAE, puis le message d'enregistrement du MP4.

Pour vérifier un MP4 avec les outils facultatifs FFmpeg/FFprobe installés sur la machine :

```powershell
ffprobe -v error -show_entries stream=codec_name,width,height,avg_frame_rate,nb_frames -show_entries format=duration,size -of json "chemin\vers\video.mp4"
ffmpeg -v error -i "chemin\vers\video.mp4" -f null NUL
```

Le code contrôle la **signature du conteneur** avant export ; le décodage intégral FFmpeg est une **vérification manuelle supplémentaire**, non une étape automatique de DreamRaster.

## Protection des données

Pour mettre à jour une installation de test, compiler et publier dans un dossier de staging distinct, puis remplacer **uniquement** le fichier EXE prévu. Ne jamais écraser `config\settings.json`, les modèles, `images\`, `videos\` ni `workspace\`. Voir [Audit de nettoyage](AUDIT_NETTOYAGE.md).
