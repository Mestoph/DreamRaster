# Installation — Français

## Développement Windows

1. Installer le SDK .NET 9.
2. Ouvrir `DreamRaster.sln` dans Visual Studio, ou lancer :

```bat
scripts\windows\build-debug.bat
```

Le cache NuGet reste dans `.nuget\packages` à la racine du dépôt.

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

## WebView2 Fixed Version portable

Le runtime Microsoft WebView2 Fixed Version x64 est un composant obligatoire de DreamRaster. L'installateur le télécharge automatiquement, vérifie son SHA-256 puis l'extrait sous `bin\webview2-fixed\`.

DreamRaster ne bascule pas silencieusement vers un runtime WebView2 Evergreen installé dans Windows.
