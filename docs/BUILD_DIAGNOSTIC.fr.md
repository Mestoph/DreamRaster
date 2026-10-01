# Diagnostic de compilation — Français

Double-cliquez sur :

```text
scripts\windows\build-diagnostic.bat
```

Le script :

1. vérifie que PowerShell, `dotnet` et le projet sont disponibles ;
2. exécute `dotnet restore` ;
3. exécute `dotnet build -c Debug` ;
4. capture stdout et stderr ;
5. extrait les erreurs et avertissements `CS`, `NU`, `MSB`, `NETSDK`, etc. ;
6. déduplique les messages répétés par le résumé MSBuild ;
7. génère un rapport Markdown et un rapport texte bilingues ;
8. ouvre automatiquement le rapport texte dans le Bloc-notes.

Les rapports sont créés dans :

```text
build-reports\AAAA-MM-JJ_HH-mm-ss\
```

et les dernières versions sont copiées sous :

```text
build-reports\LATEST_BUILD_REPORT_FR_EN.md
build-reports\LATEST_BUILD_REPORT_FR_EN.txt
build-reports\LATEST_BUILD_RAW.log
```

Pour diagnostiquer spécifiquement une compilation Release, utilisez :

```text
scripts\windows\build-diagnostic-release.bat
```

En cas d'échec, transmettez `LATEST_BUILD_REPORT_FR_EN.txt` et `LATEST_BUILD_RAW.log`.

Le rapport HTML met en rouge les erreurs, en ambre les avertissements et affiche un résumé visuel Restore/Build en haut de page. Il s’ouvre automatiquement après le diagnostic.
