# Compilation depuis Linux — Français

DreamRaster utilise **WinForms**, `net9.0-windows` et WebView2. La v33 permet donc de **compiler sous Linux pour Windows**, mais pas d'exécuter l'interface nativement sous Linux.

## Prérequis

- Linux x64 ;
- SDK .NET 9 ;
- accès Internet lors de la restauration NuGet initiale ;
- `bash`.

Vérification :

```bash
dotnet --version
```

## Build Debug Windows depuis Linux

```bash
./scripts/linux/build-debug.sh
```

Sortie :

```text
src/DreamRaster/bin/Debug/net9.0-windows/win-x64/
```

## Build Release

```bash
./scripts/linux/build-release.sh
```

## Publication Windows portable single-file

```bash
./scripts/linux/publish-windows.sh
```

Sortie attendue :

```text
src/DreamRaster/bin/Publish/Portable/DreamRaster.exe
```

Le script vérifie que `DreamRaster.exe` est bien créé et avertit si d'autres fichiers apparaissent dans la racine de publication.

## Diagnostic HTML depuis Linux

Si PowerShell 7 (`pwsh`) est installé :

```bash
./scripts/linux/build-diagnostic.sh
```

Le même moteur de rapport HTML/Markdown/TXT que sous Windows est utilisé, avec `-NoOpenReport`.

Sans `pwsh`, le script bascule vers le build Release standard.

## Pourquoi cela fonctionne ?

Le projet contient :

```xml
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

Le SDK .NET sous Linux peut donc restaurer les targeting/runtime packs Windows et compiler la cible `win-x64`.

## Limite

Le résultat reste une application Windows :

```text
DreamRaster.exe
```

WinForms et WebView2 ne fournissent pas une interface Linux native. Pour produire un vrai binaire Linux graphique, il faudrait remplacer la couche UI WinForms/WebView2 par une technologie multiplateforme telle qu'Avalonia, ce qui serait une migration séparée.
