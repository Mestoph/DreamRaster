# Repository layout — English

v33 separates source code, scripts and documentation so the repository root stays clean.

`src/DreamRaster/` contains the WinForms project only: `.cs`, `.resx`, `DreamRaster.csproj`, `Assets/`, and `Properties/PublishProfiles/`.

`scripts/windows/` contains Windows build, publish, diagnostic, GitHub creation and release scripts.

`scripts/linux/` contains Linux-to-Windows cross-build scripts.

`scripts/common/` contains the shared PowerShell diagnostic engine.

The NuGet cache remains at repository root under `.nuget/packages/`, even though the `.csproj` lives under `src/DreamRaster/`.

`bin/` and `obj/` remain under `src/DreamRaster/` and are ignored by Git.
