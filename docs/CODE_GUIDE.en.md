# Code guide — English

Every C# file has a bilingual FR/EN header. Sensitive sections (portability, downloading, updating, optional vision model) are also documented next to their implementation.

All C# files listed below live under `src/DreamRaster/`.

## Responsibilities

- `Program.cs`: startup, global errors, native WebView2 extraction/loading.
- `PortablePaths.cs`: real EXE root, pack confinement, folder creation.
- `AppSettings.cs`: persistent settings.
- `MainForm.Designer.cs`: WinForms controls editable in Visual Studio Designer.
- `MainForm.cs`: events, localization, About tab, services and updater.
- `PortableInstaller.cs`: OpenCode/Ollama/ComfyUI/models, curl and progress.
- `PortablePreflight.cs`: presence and port checks.
- `Flux2Generator.cs`: image generation through ComfyUI.
- `GitHubUpdater.cs`: GitHub Releases and delayed EXE replacement.
- `Localization.cs`: FR/EN dictionary.
- `PortableServices.cs`: image proxy and generation API.
- `ManagedProcess.cs`: child processes.
- `AnsiLogRenderer.cs`: console rendering.
- `DiagnosticExporter.cs`: diagnostics.
- `AppTheme.cs`: dark theme.

The project intentionally does not comment every single line: that would duplicate the implementation and reduce readability. Instead it uses bilingual file, section and non-obvious-logic comments.
