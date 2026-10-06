# DreamRaster

[Documentation française](README.md)

DreamRaster is a portable Windows studio for local AI image and video generation. It orchestrates **OpenCode**, **Ollama**, **ComfyUI**, and **FLUX.2** without using corresponding system-installed copies.

![DreamRaster icon](src/DreamRaster/Assets/AppIcon.png)

## Features

- dark portable WinForms application;
- embedded OpenCode through WebView2;
- embedded ComfyUI;
- FLUX.2 text-to-image and image-to-image generation;
- exact dimensions plus fixed/random seeds for generation;
- automatic prompt enhancement with a dedicated local Ollama model;
- built-in qwen3:1.7b / qwen3:4b comparison generating 8 images with timings and JSON/CSV reports;
- structural validation for FLUX.2 Klein, text encoder, and VAE models;
- Vision / Prompt AI / FLUX / Text Encoder / VAE selectors with installed/compatibility status;
- Wan 2.1 video generation through ComfyUI with explicit model installation and SHA-256 verification;
- embedded ComfyUI workflows;
- separated UI / OpenCode / Ollama / ComfyUI / FLUX.2 / Video / Installation / System logs;
- structured Ollama download progress;
- French / English UI;
- About tab;
- GitHub Releases updater;
- Windows multi-curl downloader for large files;
- models, settings, logs and caches confined to the portable folder.

## Repository layout

```text
DreamRaster/
├─ src/
│  └─ DreamRaster/          # C#, Designer, resx, csproj, assets, publish profile
├─ scripts/
│  ├─ windows/              # .bat scripts
│  ├─ linux/                # Linux -> Windows cross-build
│  └─ common/               # shared PowerShell diagnostic
├─ config/                  # settings.sample.json
├─ docs/
├─ .github/
├─ .nuget/packages/       # locally generated NuGet cache, not archived
├─ DreamRaster.sln
├─ LICENSE
├─ NOTICE
└─ CLA.md
```

See [Repository layout](docs/REPOSITORY_LAYOUT.en.md).

## Windows build

Open `DreamRaster.sln` in Visual Studio, or run:

```bat
scripts\windows\build-debug.bat
```

Single-file portable publish:

```bat
scripts\windows\publish-portable.bat
```

Output:

```text
src\DreamRaster\bin\Publish\Portable\DreamRaster.exe
```

## Building from Linux

DreamRaster remains a **Windows WinForms** application and therefore does not run natively on Linux.

With the **.NET 9 SDK**, however, Linux can compile and publish the Windows `win-x64` target:

```bash
./scripts/linux/build-debug.sh
./scripts/linux/build-release.sh
./scripts/linux/publish-windows.sh
```

The project uses `EnableWindowsTargeting=true` for this cross-build. See [Linux build](docs/BUILD_LINUX.en.md).

## Qwen3-VL

**Qwen3-VL is not required for FLUX.2 or ComfyUI.** It is only useful for vision/image-understanding features through Ollama/OpenCode and remains optional.

## Build diagnostic

On Windows:

```bat
scripts\windows\build-diagnostic.bat
```

On Linux with PowerShell 7 (`pwsh`):

```bash
./scripts/linux/build-diagnostic.sh
```

HTML/Markdown/TXT reports are generated under `build-reports/`.

## Documentation

- [Installation](docs/INSTALLATION.en.md)
- [Architecture](docs/ARCHITECTURE.en.md)
- [Repository layout](docs/REPOSITORY_LAYOUT.en.md)
- [Linux build](docs/BUILD_LINUX.en.md)
- [Build diagnostic](docs/BUILD_DIAGNOSTIC.en.md)
- [GitHub updates](docs/UPDATES.en.md)
- [Qwen3-VL](docs/QWEN_VL.en.md)
- [GitHub setup](docs/GITHUB.en.md)
- [Download sources](DOWNLOAD_SOURCES.md)
- [Branding](BRANDING.md)

## Public repository and forks

A public GitHub repository can be forked. DreamRaster remains distributed under **GNU AGPL-3.0-or-later**, and forks/modified versions must comply with the applicable license obligations.

## License / copyright

Copyright © 2026 **Mestoph**.

DreamRaster is distributed under **GNU AGPL-3.0-or-later**. See [`LICENSE`](LICENSE), [`COPYRIGHT.md`](COPYRIGHT.md), [`NOTICE`](NOTICE), and [`CLA.md`](CLA.md).

## GitHub distribution

The local publish directory keeps the **single EXE** goal. The GitHub release ZIP adds only the required legal/documentation files around `DreamRaster.exe`.

## Download sources

All external URLs used by the installer are documented in [`DOWNLOAD_SOURCES.md`](DOWNLOAD_SOURCES.md).
