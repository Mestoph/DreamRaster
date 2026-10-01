# Installation — English

## Windows development

1. Install the .NET 9 SDK.
2. Open `DreamRaster.sln` in Visual Studio, or run:

```bat
scripts\windows\build-debug.bat
```

The NuGet cache remains under repository-root `.nuget\packages`.

## Portable Windows publish

```bat
scripts\windows\publish-portable.bat
```

Output:

```text
src\DreamRaster\bin\Publish\Portable\DreamRaster.exe
```

The publish is `win-x64`, self-contained, and single-file.

## Linux development

See [BUILD_LINUX.en.md](BUILD_LINUX.en.md). Linux can cross-build/publish the Windows target, but DreamRaster is not a native Linux application.

## Components

The portable installer manages OpenCode, Ollama, ComfyUI and FLUX.2 models. Qwen3-VL is optional.
