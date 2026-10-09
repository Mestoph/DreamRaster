# DreamRaster

[English documentation](README.en.md)

DreamRaster est un studio Windows portable de génération d’images et de vidéos IA locales. L’application orchestre **OpenCode**, **Ollama**, **ComfyUI** et **FLUX.2** sans utiliser les installations système correspondantes.

![Icône DreamRaster](src/DreamRaster/Assets/AppIcon.png)

## Fonctionnalités

- application WinForms sombre et portable ;
- OpenCode intégré dans WebView2 ;
- ComfyUI intégré ;
- génération FLUX.2 text-to-image et image-to-image ;
- dimensions exactes et seed fixe/aléatoire pour les générations ;
- amélioration automatique des prompts avec un modèle Ollama local dédié ;
- comparatif intégré qwen3:1.7b / qwen3:4b avec 8 images, timings et rapport JSON/CSV ;
- validation structurelle des modèles FLUX.2 Klein, text encoder et VAE ;
- sélecteurs Vision / Prompt IA / FLUX / Text Encoder / VAE avec état installé/compatible ;
- génération vidéo Wan 2.1 via ComfyUI, avec installation explicite et SHA-256 des modèles ;
- workflows ComfyUI embarqués ;
- logs séparés UI / OpenCode / Ollama / ComfyUI / FLUX.2 / Vidéo / Installation / Système ;
- console Ollama avec progression structurée ;
- interface français / anglais ;
- onglet À propos ;
- mise à jour via GitHub Releases ;
- téléchargement multi-curl Windows pour les gros fichiers ;
- modèles, configuration, logs et caches confinés au dossier portable.

## Vidéo Wan 2.1 I2V 14B — validation locale

Sur une **RTX 4060 Ti 16 Go** avec **32 Go de RAM**, le pipeline Wan I2V 14B a été validé à **832 × 480, 33 images, 16 FPS et 50 étapes** : calcul latent achevé, redémarrage ComfyUI pour le VAE, MP4 H.264 exporté et **33 images entièrement décodées avec FFmpeg**. Le calcul latent du test a duré environ **24 min 31 s**.

DreamRaster démarre automatiquement ComfyUI avec `--lowvram` pour ce modèle. L'onglet Vidéo permet de renseigner le chemin de l'image de référence ; le profil 14B demande au moins 12 étapes. La conformité du conteneur MP4 est contrôlée avant l'export ; une vérification complète avec FFmpeg reste manuelle.

Les qualités existantes sont **Rapide 20**, **Standard 30**, **Qualité 40** et **Meilleure 50** étapes. Les essais 20/30/40 étapes sont des recommandations, pas des validations comparatives de qualité. Voir [Guide de validation vidéo Wan](docs/VIDEO_VALIDATION.fr.md).

## Structure du dépôt

```text
DreamRaster/
├─ src/
│  └─ DreamRaster/          # C#, Designer, resx, csproj, assets, profil publish
├─ scripts/
│  ├─ windows/              # scripts .bat
│  ├─ linux/                # cross-build Linux -> Windows
│  └─ common/               # diagnostic PowerShell commun
├─ config/                  # settings.sample.json
├─ docs/
├─ .github/
├─ .nuget/packages/       # cache NuGet généré localement, non archivé
├─ DreamRaster.sln
├─ LICENSE
├─ NOTICE
└─ CLA.md
```

Voir [Structure du dépôt](docs/REPOSITORY_LAYOUT.fr.md).

## Compilation Windows

Ouvrir `DreamRaster.sln` dans Visual Studio, ou lancer :

```bat
scripts\windows\build-debug.bat
```

Publication portable single-file :

```bat
scripts\windows\publish-portable.bat
```

Sortie :

```text
src\DreamRaster\bin\Publish\Portable\DreamRaster.exe
```

## Compilation depuis Linux

DreamRaster reste une application **WinForms Windows**. Elle ne s’exécute donc pas nativement sous Linux.

En revanche, avec le **SDK .NET 9**, Linux peut compiler et publier la cible Windows `win-x64` :

```bash
./scripts/linux/build-debug.sh
./scripts/linux/build-release.sh
./scripts/linux/publish-windows.sh
```

Le projet utilise `EnableWindowsTargeting=true` pour ce cross-build. Voir [Compilation Linux](docs/BUILD_LINUX.fr.md).

## Qwen3-VL

**Qwen3-VL n’est pas requis pour FLUX.2 ou ComfyUI.** Il est uniquement utile pour des fonctions de vision/compréhension d’image via Ollama/OpenCode et reste optionnel.

## Diagnostic de compilation

Sous Windows :

```bat
scripts\windows\build-diagnostic.bat
```

Sous Linux avec PowerShell 7 (`pwsh`) :

```bash
./scripts/linux/build-diagnostic.sh
```

Les rapports HTML/Markdown/TXT sont créés dans `build-reports/`.

## Documentation

- [Installation](docs/INSTALLATION.fr.md)
- [Architecture](docs/ARCHITECTURE.fr.md)
- [Structure du dépôt](docs/REPOSITORY_LAYOUT.fr.md)
- [Compilation Linux](docs/BUILD_LINUX.fr.md)
- [Diagnostic de compilation](docs/BUILD_DIAGNOSTIC.fr.md)
- [Mises à jour GitHub](docs/UPDATES.fr.md)
- [Qwen3-VL](docs/QWEN_VL.fr.md)
- [Préparation GitHub](docs/GITHUB.fr.md)
- [Sources de téléchargement](DOWNLOAD_SOURCES.md)
- [Branding](BRANDING.md)

## Dépôt public et forks

Un dépôt GitHub public est forkable. DreamRaster reste distribué sous **GNU AGPL-3.0-or-later** : les forks et versions modifiées doivent respecter cette licence et ses obligations applicables.

## Licence / copyright

Copyright © 2026 **Mestoph**.

DreamRaster est distribué sous **GNU AGPL-3.0-or-later**. Voir [`LICENSE`](LICENSE), [`COPYRIGHT.md`](COPYRIGHT.md), [`NOTICE`](NOTICE) et [`CLA.md`](CLA.md).

## Distribution GitHub

Le dossier de publication local conserve l’objectif **un seul EXE**. Le ZIP de release GitHub ajoute uniquement les documents juridiques/documentaires nécessaires autour de `DreamRaster.exe`.

## Sources de téléchargement

Toutes les URL externes utilisées par l’installeur sont documentées dans [`DOWNLOAD_SOURCES.md`](DOWNLOAD_SOURCES.md).
