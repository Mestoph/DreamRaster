# VERSION v24 — PROGRESSION DU MODÈLE OLLAMA

Le téléchargement du modèle Ollama utilise maintenant directement l'API
streaming du serveur Ollama portable (`/api/pull`) au lieu d'essayer de
parser la sortie console de `ollama pull`.

L'onglet Installation et l'onglet Console Ollama affichent maintenant :
- statut réel du pull ;
- digest/bloc en cours ;
- pourcentage du bloc ;
- quantité reçue / taille ;
- vitesse estimée en MiB/s ;
- vérification et écriture du manifeste ;
- erreur détaillée si Ollama en retourne une.

Exemple :
`[Ollama pull] qwen3-vl:8b · pulling sha256:... · 42% · 1.73 GiB/4.12 GiB · 11.8 MiB/s`

# VERSION v23 — WORKFLOWS COMFYUI INTÉGRÉS

Ajout des workflows ComfyUI intégrés dans l'exécutable :

- `workflows\flux2_text_to_image_api.json`
- `workflows\flux2_img_to_img_api.json`
- `workflows\flux2_workflow_api.json` (alias de compatibilité pour les anciennes versions)

Fonctionnement :
- les JSON ne sont plus à chercher manuellement dans l'archive ;
- ils sont embarqués dans `OpenCodeLocalAI.exe` ;
- au premier lancement, ils sont créés automatiquement dans `workflows\` si absents ;
- si tu modifies tes propres workflows, l'application ne les écrase pas tant que les fichiers existent déjà.

Important :
- l'UI actuelle utilise directement le workflow **text-to-image** ;
- le workflow **img-to-img** est maintenant livré dans le pack et prêt pour une utilisation future / manuelle côté ComfyUI ;
- tu peux remplacer les JSON par tes propres exports API ComfyUI si tu adaptes ton pipeline.

# VERSION v22 — SUPPRESSION DES XML WEBVIEW2

Correction :
- `Microsoft.Web.WebView2.Core.xml` n'est plus copié dans la publication ;
- `Microsoft.Web.WebView2.WinForms.xml` n'est plus copié dans la publication ;
- MSBuild est configuré pour ne plus copier les fichiers de documentation XML
  associés aux références ;
- `PUBLISH_PORTABLE.bat` supprime aussi ces deux fichiers après publication
  au cas où une ancienne sortie les aurait conservés.

Objectif de `bin\Publish\Portable\` avant premier lancement :
`OpenCodeLocalAI.exe` uniquement.

Les sous-dossiers portables sont ensuite créés au premier lancement.

# VERSION v21 — UN SEUL EXÉCUTABLE À LA RACINE

Objectif : après `PUBLISH_PORTABLE.bat`, le dossier
`bin\Publish\Portable\` doit pouvoir démarrer avec uniquement :

`OpenCodeLocalAI.exe`

Changements :
- WebView2Loader.dll est intégré dans l'EXE comme ressource ;
- au premier lancement il est extrait automatiquement dans
  `runtime\native\WebView2Loader.dll` ;
- les DLL managées WebView2 restent intégrées dans le bundle single-file ;
- aucune DLL WebView2 n'est volontairement publiée à côté de l'EXE ;
- aucune extraction automatique .NET dans `%TEMP%\.net` ;
- `config`, `runtime`, `models`, `downloads`, `logs`, etc. sont créés
  après le premier lancement dans le dossier portable.

Le dossier devient donc propre à la distribution : un seul EXE à la base,
puis les sous-dossiers de données apparaissent quand l'application démarre.

# VERSION v20 — FIX MULTI-CURL + WEBVIEW2 HORS RACINE

Corrections :

- le multi-curl n'utilise plus l'URL finale signée récupérée par HttpClient ;
- chaque curl reçoit l'URL d'origine et suit lui-même les redirections ;
- l'URL est passée explicitement avec `--url` ;
- cela corrige le cas `curl: (3) URL using bad/illegal format or missing URL` ;
- le log n'affiche que le domaine source, jamais une URL CDN signée complète.

WebView2 :
- `WebView2Loader.dll` reste externe car c'est une DLL native ;
- elle n'est plus placée à côté de `OpenCodeLocalAI.exe` ;
- elle est publiée dans `runtime\native\WebView2Loader.dll` ;
- un résolveur natif charge explicitement cette DLL depuis le dossier portable.

La racine portable, l'onglet Configuration, Ollama, ComfyUI, les BAT,
le single-file sans extraction TEMP et le multi-curl sont conservés.

# VERSION v19 — SINGLE FILE SANS EXTRACTION TEMP

Objectif : supprimer la forêt de DLL de la publication portable sans revenir
au problème `%TEMP%\.net`.

Publication portable :
- `SelfContained=true` ;
- `PublishSingleFile=true` ;
- `IncludeNativeLibrariesForSelfExtract=false` ;
- `IncludeAllContentForSelfExtract=false` ;
- `PublishTrimmed=false` pour éviter les problèmes WinForms/WebView2 ;
- compression du bundle activée.

Conséquence :
- la grande majorité des DLL managées du runtime .NET est intégrée dans
  `OpenCodeLocalAI.exe` ;
- les rares DLL natives indispensables restent physiques à côté de l'EXE ;
- `WebView2Loader.dll` est explicitement conservé à côté de l'EXE ;
- aucune extraction volontaire du runtime dans `%TEMP%\.net` ;
- la racine portable reste déterminée par le vrai `OpenCodeLocalAI.exe`.

Il faut toujours distribuer l'ensemble du dossier `bin\Publish\Portable\`.

# VERSION v18 — VRAIE RACINE PORTABLE + CONFIGURATION + MULTI-CURL

Corrections importantes :

1. RACINE PORTABLE
- le pack ne doit plus utiliser `%TEMP%\.net\...` ;
- la racine est le dossier du vrai `OpenCodeLocalAI.exe` ;
- au démarrage, l'application crée dans ce dossier :
  `bin`, `config`, `downloads`, `images`, `logs`, `models`, `runtime`,
  `workflows`, `workspace`, etc. ;
- `config\settings.json` est créé automatiquement au premier lancement.

2. PUBLICATION PORTABLE
- le profil `Portable.pubxml` publie maintenant en dossier self-contained ;
- `PublishSingleFile=false` volontairement ;
- cela évite l'extraction de tout le bundle dans `%TEMP%\.net` ;
- il faut distribuer tout `bin\Publish\Portable\`.

3. ONGLET CONFIGURATION
- racine portable visible ;
- ports OpenCode/Ollama/ComfyUI/Proxy/API ;
- modèles Vision/FLUX/Text Encoder/VAE ;
- taille de génération et steps ;
- seuils VRAM/RAM ;
- nombre de connexions téléchargement + buffer ;
- option arrêt ComfyUI après génération ;
- bouton Enregistrer vers `config\settings.json`.

4. TÉLÉCHARGEMENT
- moteur Windows natif `C:\Windows\System32\curl.exe` ;
- jusqu'à 8 téléchargements Range en parallèle par défaut ;
- chaque curl écrit dans son propre segment ;
- assemblage final séquentiel ;
- aucun plafond de débit applicatif ;
- fallback curl mono-connexion si le serveur ne supporte pas Range.

Au démarrage les logs affichent la version et la racine utilisée.

# VERSION v17 — curl.exe NATIF WINDOWS

aria2 a été retiré du chemin de téléchargement.

Le moteur principal est `curl.exe` fourni avec Windows :
- `%WINDIR%\System32\curl.exe` via `Environment.SystemDirectory` ;
- aucun outil de téléchargement à installer ;
- aucune recherche dans PATH ;
- redirections CDN suivies ;
- reprise via `--continue-at -` ;
- retries réseau ;
- aucun plafond de débit ;
- fichier temporaire `*.curl.part` puis renommage après succès ;
- annulation propre ;
- fallback HttpClient seulement si curl Windows échoue.

Log attendu :
`[Install] CURL WINDOWS ACTIF · débit illimité · ...`

L'interface sombre, Console Ollama, ComfyUI et les BAT build/publish sont conservés.

# VERSION v16 — CORRECTION DU VERROU aria2

Correction du message :
`The process cannot access the file because it is being used by another process.`

Le ZIP aria2 était renommé alors que son FileStream pouvait encore être ouvert.
v16 ferme explicitement tous les flux avant le renommage et applique un retry
court si Windows Defender/antivirus conserve momentanément un handle.

Un log très visible permet de confirmer le moteur réellement actif :

`[Install] ARIA2 ACTIF · 16 connexions · débit illimité · ...`

Si ce message apparaît, le fallback HttpClient n'est plus utilisé.

Les scripts `BUILD_DEBUG.bat` et `PUBLISH_PORTABLE.bat` sont conservés.

# VERSION v15 — SCRIPTS BUILD / PUBLISH

Scripts ajoutés à la racine :

- `BUILD_DEBUG.bat`
  - supprime `bin` et `obj` ;
  - restaure les packages NuGet ;
  - compile en Debug ;
  - sortie : `bin\Debug\net9.0-windows\`.

- `PUBLISH_PORTABLE.bat`
  - conserve le cache `.nuget\packages` ;
  - restaure pour `win-x64` ;
  - compile en Release ;
  - publie avec `Properties\PublishProfiles\Portable.pubxml` ;
  - sortie : `bin\Publish\Portable\` ;
  - ouvre automatiquement le dossier de publication en cas de succès.

# VERSION v14 — aria2c PORTABLE POUR LES TÉLÉCHARGEMENTS

Le moteur de téléchargement principal n'est plus HttpClient.

v14 :
- télécharge automatiquement aria2 1.37.0 Windows x64 dans `runtime\tools\aria2` ;
- vérifie le SHA256 officiel de l'archive aria2 ;
- utilise uniquement cet `aria2c.exe` portable ;
- 16 connexions par fichier ;
- aucun plafond de débit (`--max-download-limit=0`) ;
- reprise native aria2 (`--continue=true`) ;
- allocation fichier désactivée (`--file-allocation=none`) ;
- progression et vitesse affichées dans l'installeur ;
- annulation tue uniquement le processus aria2 appartenant au pack ;
- fallback HttpClient uniquement si aria2c ne peut pas être installé ;
- interface sombre + onglets ComfyUI/Ollama conservés.

# VERSION v13 — CONSOLE OLLAMA DÉDIÉE

Ajout :
- nouvel onglet `Console Ollama` ;
- console sombre dédiée ;
- reçoit uniquement les sources `Ollama`, `Ollama !` et `Ollama pull` ;
- conservation des couleurs ANSI ;
- les mêmes messages continuent également d'apparaître dans les logs globaux ;
- interface sombre et téléchargement v12 conservés.

# VERSION v12 — TÉLÉCHARGEMENT TYPE NAVIGATEUR

Correction du problème de vitesse de v10/v11.

La v11 utilisait plusieurs requêtes Range et plusieurs écritures à des
offsets différents du même fichier. Elle demandait aussi HTTP/2 avec
`RequestVersionOrLower`, ce qui empêchait HTTP/3.

v12 :
- flux réseau unique et séquentiel ;
- écriture disque séquentielle ;
- HTTP/3 si disponible, sinon HTTP/2, sinon HTTP/1.1 ;
- aucun plafond de débit ;
- buffer 8 MiB ;
- reprise d'un `.part.single` existant ;
- protocole réellement négocié affiché dans les logs ;
- anciens fichiers segmentés v10/v11 supprimés proprement ;
- interface sombre conservée.

# VERSION v11 — TÉLÉCHARGEMENT SANS LIMITE DE DÉBIT

L'application n'applique plus aucune limitation logicielle de bande passante.

- aucun plafond en MiB/s ;
- aucun `Task.Delay` utilisé pour ralentir les transferts ;
- `MaxConnectionsPerServer = int.MaxValue` ;
- 8 segments simultanés par défaut pour les gros fichiers ;
- seuil parallèle abaissé à 64 MiB ;
- reprise après interruption conservée ;
- vitesse instantanée affichée en MiB/s dans l'installateur ;
- interface sombre conservée.

La vitesse réelle reste naturellement limitée par :
1. le serveur/CDN distant ;
2. la connexion Internet ;
3. le stockage local ;
4. éventuellement l'antivirus/pare-feu.

`DownloadConnections` contrôle le nombre de segments, pas un plafond de débit.

# VERSION v10 — TÉLÉCHARGEMENTS ACCÉLÉRÉS

L'interface sombre est conservée.

Améliorations :
- jusqu'à 4 connexions HTTP simultanées pour les fichiers >= 128 MiB ;
- détection automatique du support HTTP Range ;
- reprise des segments déjà terminés après annulation/redémarrage ;
- reprise aussi en mono-connexion via `.part.single` quand le serveur le permet ;
- buffers réseau/disque de 4 MiB ;
- HTTP/2 demandé lorsque le serveur le supporte ;
- repli automatique en mono-connexion si le serveur ne supporte pas les plages ;
- paramètres `DownloadConnections` (défaut 4, max 8) et
  `ParallelDownloadThresholdMiB` (défaut 128).

Pour une très bonne connexion, `DownloadConnections = 6` peut être testé.
Au-delà de 8 connexions, certains CDN peuvent ralentir ou limiter.

# VERSION v9 — ONGLET COMFYUI + LOG [UI] + BUILD LÉGER

Nouveautés :
- onglet `ComfyUI` intégré avec WebView2 portable séparé ;
- bouton `Ouvrir ComfyUI intégré` dans le tableau de bord ;
- profil WebView2 séparé `runtime\webview2-comfy` ;
- `[UI]` et `[WebView]` ont maintenant une couleur dédiée dans les logs ;
- les builds Debug/Release normaux sont framework-dependent et beaucoup plus légers ;
- le mode portable self-contained/single-file est déplacé dans
  `Properties\PublishProfiles\Portable.pubxml`.

Pour produire la vraie version portable :
- Visual Studio > Publier > profil `Portable`
ou
- `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true`

# VERSION v8 — CORRECTION COMPILATION WEBVIEW2 + THÈME

Corrections :
- switch du thème : FlowLayoutPanel/TableLayoutPanel placés avant Panel ;
- WebView2 Core + WinForms pointent maintenant vers `lib_manual\netcoreapp3.0` ;
- chemins WebView2 calculés via `$(RestorePackagesPath)` + `$(WebView2Version)` ;
- aucune référence à `Microsoft.Web.WebView2.Wpf.dll` ;
- cache NuGet toujours local dans `.nuget\packages` ;
- interface toujours sombre.

# VERSION v7 — SOMBRE, PLUS RÉACTIVE ET WEBVIEW2 WINFORMS-ONLY

L'interface reste entièrement sombre.

Nouveautés v7 :
- logs processus mis en file puis rendus par lots toutes les 100 ms ;
- maximum 60 entrées de log traitées par tick UI ;
- écritures disque regroupées en lot ;
- callbacks de progression de téléchargement limités ;
- sortie 7-Zip résumée au lieu d'inonder les RichTextBox ;
- annulation avec nettoyage des extractions partielles ;
- thème sombre réappliqué récursivement au démarrage ;
- WebView2 restauré dans `.nuget\packages`, avec références explicites Core + WinForms seulement ;
- `Microsoft.Web.WebView2.Wpf.dll` n'est plus importée par MSBuild.

# VERSION v6 — INSTALLATION RÉACTIVE + THÈME UNIFORME

Corrections principales :
- le formulaire n'est plus désactivé pendant une opération asynchrone ;
- le bouton Annuler reste utilisable pendant toute l'installation ;
- annulation propagée aux téléchargements, extraction ZIP, 7zr.exe et ollama pull ;
- fermeture de l'application pendant l'installation = annulation + nettoyage propre ;
- extraction ZIP asynchrone/cancellable au lieu de bloquer le thread UI ;
- écriture des logs sur disque hors du thread graphique ;
- progression Ollama limitée à un message par pourcentage pour ne pas saturer l'UI ;
- taille des RichTextBox de logs limitée pour éviter le ralentissement au fil du temps ;
- palette sombre uniforme dans tous les onglets ;
- textes de l'installeur fortement contrastés ;
- codes ANSI sombres remappés vers des couleurs lisibles ;
- onglets dessinés avec la même palette que l'application.

# VERSION DESIGNER v5

Correction importante : tous les contrôles WinForms sont désormais déclarés et configurés
directement dans `MainForm.Designer.cs` / `InitializeComponent()`.

Le concepteur Visual Studio doit donc afficher les composants des cinq onglets :
- Tableau de bord
- OpenCode
- Générer
- Installation
- Logs

Aucune méthode auxiliaire personnalisée n'est utilisée pour construire l'interface dans le Designer.

# VERSION DESIGNER + NUGET LOCAL

Cette version restaure les packages NuGet dans :

    .nuget\packages

à l'intérieur du dossier source, au lieu d'utiliser :

    C:\Users\<user>\.nuget\packages

Le projet est WinForms uniquement (`UseWPF=false`) et retire la référence
`Microsoft.Web.WebView2.Wpf.dll` inutile pour éviter le conflit WindowsBase.

# VERSION DESIGNER WINFORMS

Cette version contient `MainForm.cs`, `MainForm.Designer.cs` et `MainForm.resx`.
Dans Visual Studio : clic droit sur `MainForm.cs` > **Afficher le concepteur**.

# OpenCode Local AI — source corrigé

Cette archive contient une version source **corrigée et nettoyée** axée sur les problèmes suivants :

- aucune installation Ollama / OpenCode / ComfyUI du système n'est utilisée ;
- tous les exécutables AI sont résolus uniquement sous le dossier portable ;
- si les composants portables manquent, l'application propose l'installation au démarrage ;
- les erreurs provenant des boutons/événements UI sont interceptées et affichées au lieu de fermer brutalement l'application ;
- l'installeur utilise un texte à contraste élevé et une interface plus compacte ;
- OpenCode est lancé avec `serve`, `BROWSER=none`, `NO_BROWSER=1` ;
- WebView2 bloque les nouvelles fenêtres et les navigations externes ;
- les logs live interprètent les couleurs ANSI ;
- le `.csproj` référence explicitement `Microsoft.Web.WebView2` ;
- SharpCompress a été supprimé ; ComfyUI est extrait avec `7zr.exe` téléchargé dans `runtime\tools`, donc aucune installation 7-Zip système n'est utilisée.

## Compilation

Prérequis : .NET SDK 9.x.

```powershell
dotnet restore
dotnet build -c Release
```

Publication Windows x64 mono-fichier :

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false
```

## WebView2 portable

Le code refuse volontairement de basculer silencieusement sur un runtime WebView2 installé dans Windows.

Placez le runtime **Fixed Version** sous :

```text
bin\webview2-fixed\
```

## Isolation portable

Les variables suivantes sont redirigées vers `runtime\...` lors du lancement des composants :

- HOME
- USERPROFILE
- APPDATA
- LOCALAPPDATA
- TEMP / TMP
- CUDA_CACHE_PATH
- XDG_DATA_HOME / XDG_CONFIG_HOME / XDG_STATE_HOME / XDG_CACHE_HOME
- OLLAMA_MODELS

L'application ne recherche jamais `ollama.exe`, `opencode.exe` ou ComfyUI dans `PATH`, AppData ou Program Files.

## Remarque FLUX

`Flux2Generator.cs` attend le workflow :

```text
workflows\flux2_workflow_api.json
```

Le workflow n'est pas inventé par cette archive : utilisez celui validé avec votre installation FLUX.2.


## Services conservés

Cette version source contient également :

- proxy HTTP/WebSocket sur le port `54100` ;
- exposition des images via `/local-images/...` ;
- API locale de génération sur `54101` ;
- onglet de génération FLUX.2 ;
- arrêt de l'API/proxy/ComfyUI/OpenCode/Ollama lors de la fermeture.

Le générateur attend toujours `workflows\flux2_workflow_api.json`.


## Corrections de compilation v2

- `DiagnosticExporter.cs` n'utilise plus de littéral de chaîne brute interpolée.
- `SharpCompress` n'est plus une dépendance NuGet.
- Le projet retire la référence WPF WebView2 inutile dans l'application WinForms avant `ResolveAssemblyReferences`, afin d'éviter le warning `WindowsBase` causé par `Microsoft.Web.WebView2.Wpf.dll`.

