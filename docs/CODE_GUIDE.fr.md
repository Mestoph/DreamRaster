# Guide du code — Français

Tous les fichiers C# possèdent un en-tête bilingue FR/EN. Les sections sensibles (portabilité, téléchargements, mise à jour, vision optionnelle) sont également commentées au point d'implémentation.

Tous les fichiers C# ci-dessous se trouvent dans `src/DreamRaster/`.

## Responsabilités

- `Program.cs` : démarrage, erreurs globales, extraction/chargement natif WebView2.
- `PortablePaths.cs` : racine du vrai EXE, confinement dans le pack, création des dossiers.
- `AppSettings.cs` : paramètres persistants.
- `MainForm.Designer.cs` : contrôles WinForms modifiables dans Visual Studio Designer.
- `MainForm.cs` : événements, traduction, onglet À propos, services et updater.
- `PortableInstaller.cs` : OpenCode/Ollama/ComfyUI/modèles, curl et progression.
- `PortablePreflight.cs` : vérification de présence et ports.
- `Flux2Generator.cs` : génération image via ComfyUI.
- `GitHubUpdater.cs` : GitHub Releases et remplacement différé de l'EXE.
- `Localization.cs` : dictionnaire FR/EN.
- `PortableServices.cs` : proxy image et API de génération.
- `ManagedProcess.cs` : processus enfants.
- `AnsiLogRenderer.cs` : rendu de consoles.
- `DiagnosticExporter.cs` : diagnostic.
- `AppTheme.cs` : thème sombre.

Un commentaire sur chaque ligne n'est volontairement pas utilisé : cela dupliquerait le code et réduirait sa lisibilité. Le projet utilise plutôt des commentaires bilingues de fichier, de section et de logique non évidente.
