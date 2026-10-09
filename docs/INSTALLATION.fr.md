# Installation — Français

## Développement Windows

1. Installer le SDK .NET 9.
2. Ouvrir `DreamRaster.sln` dans Visual Studio, ou lancer :

```bat
scripts\windows\build-debug.bat
```

Le cache NuGet est généré par `dotnet restore` dans `.nuget\packages` à la racine du dépôt. Il est volontairement exclu des archives source et peut être supprimé sans perdre de code.

## Publication portable Windows

```bat
scripts\windows\publish-portable.bat
```

Sortie :

```text
src\DreamRaster\bin\Publish\Portable\DreamRaster.exe
```

La publication est `win-x64`, self-contained et single-file.

## Développement Linux

Voir [BUILD_LINUX.fr.md](BUILD_LINUX.fr.md). Linux peut cross-compiler/publier la cible Windows, mais DreamRaster n'est pas une application Linux native.

## Composants

L’installeur portable gère OpenCode, Ollama, ComfyUI et les modèles FLUX.2. Qwen3-VL est optionnel.

## Wan 2.1 I2V : installation et précautions

La génération Image vers vidéo nécessite le modèle Wan I2V sélectionné, un text encoder compatible, le VAE Wan, CLIP-Vision et une **image de référence existante**. Les modèles sont gérés dans l'installation portable, sans téléchargement automatique de plusieurs gigaoctets au lancement.

Le modèle I2V 14B utilise `--lowvram` et DreamRaster redémarre ComfyUI entre le calcul latent et le décodage VAE pour limiter la pression mémoire. Sur la RTX 4060 Ti 16 Go testée, **832 × 480, 33 images, 50 étapes** a réussi, mais le temps et l'usage mémoire dépendent du matériel.

Pour les presets, les fichiers MP4 et les vérifications de sortie, voir [Validation vidéo Wan](VIDEO_VALIDATION.fr.md).

**Sécurité des mises à jour :** publier dans un dossier distinct et remplacer uniquement l'exécutable autorisé ; conserver `config\settings.json`, les modèles, les images, les vidéos et le workspace. Voir [Audit de nettoyage](AUDIT_NETTOYAGE.md).

## WebView2 Fixed Version portable

Le runtime Microsoft WebView2 Fixed Version x64 est un composant obligatoire de DreamRaster. L'installateur le télécharge automatiquement, vérifie son SHA-256 puis l'extrait sous `bin\webview2-fixed\`.

DreamRaster ne bascule pas silencieusement vers un runtime WebView2 Evergreen installé dans Windows.
