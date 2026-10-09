# Audit de nettoyage DreamRaster

Ce document recense les éléments réellement nécessaires, régénérables ou supprimables dans le dépôt de développement et dans l'installation portable DreamRaster_FreshTest.

## Règle absolue pour le déploiement

Le déploiement final met l'exécutable ici :

D:\AI\DreamRaster_FreshTest\DreamRaster.exe

Le dossier DreamRaster_FreshTest ne doit jamais être vidé ni remplacé intégralement.

Ne jamais supprimer, déplacer, renommer ou écraser automatiquement :

- models\
- les modèles présents sous bin\comfyui\...\models\
- config\settings.json
- images\
- videos\
- workspace\
- les données téléchargées par l'utilisateur

Le déploiement doit être construit dans un dossier de staging séparé, puis copier uniquement les fichiers applicatifs explicitement autorisés.

## Dépôt de développement

### Régénérable et supprimable

Ces éléments sont produits par Visual Studio, MSBuild ou les tests et n'ont aucune valeur source :

- .vs\
- src\DreamRaster\bin\
- src\DreamRaster\obj\
- tests\DreamRaster.Tests\bin\
- tests\DreamRaster.Tests\obj\
- tests\DreamRaster.Tests\TestResults\
- src\DreamRaster\DreamRaster.csproj.user

Ils peuvent être recréés automatiquement par une compilation ou Visual Studio.

### Cache NuGet

.nuget\packages\ est régénérable, mais il reste utile pour compiler sans retélécharger toutes les dépendances. Il ne doit donc pas être supprimé automatiquement lors d'un nettoyage courant.

### Fichiers et dossiers temporaires de travail

Les dossiers de staging ou de sauvegarde créés pour une intervention ponctuelle doivent être supprimés après validation, par exemple :

- D:\AI\DreamRaster_DeployStage
- D:\AI\DreamRaster_MainFormMergeBackup

Ils ne font pas partie du produit.

## Installation portable DreamRaster_FreshTest

### Indispensable au fonctionnement

À conserver :

- DreamRaster.exe
- config\
- workflows\
- bin\comfyui\
- bin\ollama\
- bin\opencode\
- bin\webview2-fixed\
- runtime\native\
- runtime\tools\

### Données utilisateur

À conserver sauf demande explicite de l'utilisateur :

- models\
- modèles ComfyUI sous bin\comfyui\...\models\
- images\
- videos\
- workspace\
- config\settings.json

### Archives d'installation régénérables

Après installation réussie des composants, les archives du dossier downloads\ ne sont plus nécessaires à l'exécution :

- ComfyUI.7z
- ollama.zip
- Microsoft.WebView2.FixedVersionRuntime.*.cab
- opencode.zip

Lors du dernier audit elles représentaient environ 3,84 Go. Elles peuvent être retéléchargées par l'installateur si une réparation est nécessaire.

Elles ne sont pas supprimées automatiquement afin de ne pas modifier l'installation portable sans décision explicite.

### Caches régénérables

Ces éléments peuvent être reconstruits par les composants concernés :

- dossiers Python __pycache__
- runtime\webview2-* et leurs caches WebView2
- caches CUDA de runtime\comfyui\cuda-cache
- caches CUDA de runtime\ollama\cuda-cache
- dossiers temp vides ou temporaires
- journaux anciens dans logs\ lorsque leur conservation n'est plus utile

Lors du dernier audit, les __pycache__ hors modèles représentaient environ 229 Mo.

### Métadonnées ComfyUI

bin\comfyui\...\ComfyUI\.github\ n'est pas requis pour exécuter ComfyUI.

bin\comfyui\...\ComfyUI\.git\ n'est pas requis pour générer des images ou des vidéos, mais peut être utile pour identifier la révision ou effectuer certaines mises à jour. Il ne doit donc pas être supprimé automatiquement sans fixer d'abord la stratégie de mise à jour de ComfyUI.

## Code mort

La passe de nettoyage a supprimé les méthodes privées sans aucune référence confirmée :

- AdvancedControlPolish
- CopyComboItemsPolish
- HasFluxModels
- LayoutVideoLeftColumnV36
- SelectedLoraFileV37
- WireCheckMirrorPolish
- WireNumericMirrorPolish

Les analyseurs IDE0051, IDE0052, IDE0060 et IDE0079 n'ont ensuite signalé aucun autre membre privé mort évident. La structure mémoire Win32 du tableau de bord a également été fusionnée avec la structure MEMORYSTATUSEX déjà utilisée par MainForm afin de supprimer un doublon historique.

## Structure MainForm

La structure volontaire est limitée à deux fichiers :

- MainForm.cs
- MainForm.Designer.cs

Le fichier historique MainForm.UiPolish.cs a été absorbé dans MainForm.cs puis supprimé.

## Documentation du code

La passe finale de documentation a vérifié 1273 déclarations de types et membres dans les sources maintenues :

- documentation XML présente : 1273/1273
- déclarations sans documentation XML : 0
- commentaires génériques détectés : 0
- marqueurs historiques FR:/EN: : 0

Les commentaires structurants du code sont désormais rédigés uniquement en français. Les textes anglais conservés dans les données ou appels de traduction servent uniquement à l'interface bilingue et ne sont pas des commentaires de code.

## Validation de référence

La dernière validation complète a donné :

- build Release : 0 erreur
- avertissements : 0
- tests : 82/82 (validation Release du 9 octobre 2026)
- git diff --check : propre

## Recette de l'installation officielle v38.1.0 — 10 octobre 2026

La release GitHub `v38.1.0` a été téléchargée dans un dossier de staging, son ZIP a été comparé au SHA-256 publié, puis **seul `DreamRaster.exe` a été remplacé** dans l'installation portable. L'ancien EXE v38.0.0 est conservé comme secours dans `D:\AI\DreamRaster_Backups\`.

Les essais réels depuis l'application v38.1.0 ont validé :

- **Wan I2V 14B, 832 × 480, 5 images, 12 étapes :** export MP4 H.264, décodage FFmpeg complet, 5/5 images distinctes ;
- **Wan I2V 14B, 832 × 480, 17 images, 20 étapes :** calcul latent 311,28 s, export MP4, 17/17 images distinctes ;
- **Wan I2V 14B, 832 × 480, 33 images, 30 étapes :** calcul latent 15 min 12 s, export MP4, 33/33 images distinctes ;
- **Annulation en cours d'échantillonnage Wan :** interrupt et retrait de file confirmés, aucun MP4 incomplet, boutons rétablis ;
- **FLUX.2 Klein 4B FP8, 512 × 512, 4 étapes sans LoRA :** PNG vérifié et rendu visuellement cohérent ;
- **Profil complet 33 images / 50 étapes :** validé précédemment, 33/33 images décodées et environ 24 min 31 s de calcul latent.

Après la recette : application v38.1.0 réactive et minimisée ; configuration d'origine restaurée avec une empreinte SHA-256 identique ; **5 186 fichiers de modèles conservés**, vidéos et images préservées, ComfyUI arrêté. Ces contrôles couvrent la version installée mais **ne remplacent pas un essai exhaustif de toutes les combinaisons de modèles ni du parcours de mise à jour automatique**. Voir [Validation vidéo Wan](VIDEO_VALIDATION.fr.md).
