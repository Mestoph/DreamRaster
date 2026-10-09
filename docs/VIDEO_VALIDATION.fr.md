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
| Essai rapide | 832 × 480 | 17 | 16 | 20 | **Validé le 10 octobre 2026** |
| Équilibré | 832 × 480 | 33 | 16 | 30 | **Validé le 10 octobre 2026** |
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

## Recette post-publication — DreamRaster v38.1.0 (10 octobre 2026)

La release GitHub officielle a été installée dans `DreamRaster_FreshTest` par remplacement du seul exécutable. Son archive et son SHA-256 ont été vérifiés. La configuration personnelle a été restaurée à l'identique et l'inventaire des **5 186 fichiers de modèles** a été préservé.

- **Smoke test I2V :** 832 × 480, 5 images, 12 étapes, 16 FPS ; MP4 H.264 enregistré, décodage FFmpeg complet, 5 images distinctes.
- **Annulation réelle :** calcul Wan 832 × 480, 33 images, 12 étapes interrompu pendant l'échantillonnage. ComfyUI a confirmé `interrupt=OK` et le retrait du prompt ; la file d'attente était vide, aucun MP4 incomplet n'a été ajouté et le bouton Générer a été réactivé.
- **Preset Rapide validé :** 832 × 480, 17 images, 20 étapes, 16 FPS, graine fixe 1. Calcul latent : **311,28 secondes** ; export MP4 H.264 de **1,0625 seconde**, **17/17 images distinctes**, décodage FFmpeg complet sans erreur. Sortie : `DreamRaster_WAN_2026-10-09T22-16-24-259Z_00001_.mp4`.
- **Preset Équilibré validé :** 832 × 480, 33 images, 30 étapes, 16 FPS, **même prompt, même image de référence et même graine fixe 1**. Calcul latent : **15 min 12 s** ; MP4 H.264 de **2,0625 secondes**, **33/33 images distinctes**, décodage FFmpeg complet sans erreur. Sortie : `DreamRaster_WAN_2026-10-09T22-23-24-864Z_00001_.mp4`. Les images échantillonnées montrent une tasse rouge reconnaissable et une composition temporellement stable.

- **Parcours Image FLUX.2 également validé :** sur l'EXE officiel v38.1.0, modèle Klein 4B FP8, **512 × 512, 4 étapes, graine 1001, sans LoRA** ; PNG de **290 292 octets**, signature et dimensions vérifiées, tasse rouge nette et reconnaissable. Sortie : `DreamRaster_FLUX2_2026-10-09T22-41-47-975Z_00001_.png`.

L'inspection visuelle des images échantillonnées des profils Rapide et Équilibré montre une tasse rouge reconnaissable et une composition temporellement stable. Même avec le même prompt, la même référence et la même graine, ces profils ne produisent pas la même **durée** (17 contre 33 images) : leur comparaison ne permet donc pas d'attribuer les différences au seul nombre d'étapes. Le test historique à 50 étapes porte sur un **autre sujet** et ne peut pas être classé directement contre la tasse. Pour cette machine, **Standard / 30 étapes et 33 images** est un compromis quotidien conseillé, tandis que **Meilleure / 50 étapes** reste le profil final validé, plus coûteux. Les 40 étapes restent non testées.

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
