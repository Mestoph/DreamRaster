# Changelog

## 33.0.0
- Reorganized repository into `src/`, `scripts/`, `docs/`, `config/`, and GitHub metadata.
- Moved WinForms project to `src/DreamRaster/`.
- Moved Windows scripts to `scripts/windows/`.
- Added Linux-to-Windows cross-build and cross-publish scripts under `scripts/linux/`.
- Added `EnableWindowsTargeting=true` for Linux/macOS build hosts.
- Added `DreamRaster.sln` at repository root.
- Kept project-local NuGet isolation at repository-root `.nuget/packages/`.
- Added Linux cross-build and cross-publish GitHub Actions.
- Preserved single-EXE publish, portability, image-to-image, AGPL, CLA, NOTICE and HTML diagnostics.


## 32.0.0
- Renamed the application and project to **DreamRaster**.
- Renamed executable to `DreamRaster.exe`.
- Prepared GitHub repository `Mestoph/DreamRaster`.
- Added `DOWNLOAD_SOURCES.md` with every default external download/source URL.
- Moved the 7-Zip URL into portable settings instead of leaving it hardcoded.
- Documented optional Qwen3-VL source and WebView2 Fixed Version distribution reference.
- GitHub release ZIP now includes `DOWNLOAD_SOURCES.md`.
- Preserved AGPL, CLA, NOTICE, image-to-image and bilingual HTML build diagnostics.


## 31.0.0
- Renamed the public application and project to **DreamRaster**.
- Renamed the executable to `DreamRaster.exe`.
- Prepared repository `Mestoph/DreamRaster`.
- Added bilingual `CLA.md` and bilingual `NOTICE`.
- Added CLA acknowledgement to pull requests and a CLA-check GitHub Action.
- Updated build, publish, diagnostic, release and updater configuration.
- Preserved GNU AGPL-3.0-or-later, image-to-image and HTML build reports.


## 30.0.0
- Added Mestoph as author and copyright owner.
- Licensed the project under GNU AGPL-3.0-or-later.
- Added the official GNU AGPL v3 license text.
- Added SPDX AGPL identifiers to source files.
- Updated About, README, CONTRIBUTING and GitHub documentation.
- Finalized bilingual HTML build report with visual summary, error and warning highlighting.


## 28.0.0
- Added `BUILD_DIAGNOSTIC.bat`.
- Added bilingual PowerShell build diagnostics.
- Captures restore/build output and extracts compiler/MSBuild/NuGet issues.
- Generates Markdown, text and raw-log reports.
- Added Debug and Release diagnostic launchers.


## 27.0.0
- Fixed GitHub updater CS0201 compilation error.
- Added explicit nullable context to the WinForms Designer source.
- Removed nullable WebView2 dereference warnings.
- Removed obsolete Ollama pull progress field.
- Preserved image-to-image mode from v26.


## 26.0.0
- Image-to-image mode added to the Generate tab.
- Browse/clear source image controls and denoise strength.
- FLUX.2 generator now supports portable img-to-img workflow.

## 25.0.0
- About tab.
- French/English UI localization.
- GitHub Releases updater.
- GitHub repository scaffolding and bilingual documentation.
- New AI image-generation application icon.
- Qwen3-VL made optional.
- Bilingual source-code documentation headers.

Older detailed development notes are preserved in `docs/CHANGELOG_LEGACY.md`.
