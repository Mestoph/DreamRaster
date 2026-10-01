# Scripts

## Windows

- `windows/build-debug.bat`
- `windows/publish-portable.bat`
- `windows/build-diagnostic.bat`
- `windows/build-diagnostic-release.bat`
- `windows/create-github-repo.bat`
- `windows/tag-release.bat`

## Linux

- `linux/build-debug.sh` — cross-build Debug Windows x64
- `linux/build-release.sh` — cross-build Release Windows x64
- `linux/publish-windows.sh` — publish portable `DreamRaster.exe`
- `linux/build-diagnostic.sh` — HTML diagnostic when `pwsh` is available

## Common

- `common/BuildDiagnostic.ps1` — bilingual HTML/Markdown/TXT diagnostic engine
