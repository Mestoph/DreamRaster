# Building from Linux — English

DreamRaster uses **WinForms**, `net9.0-windows`, and WebView2. v33 therefore supports **building on Linux for Windows**, but does not make the UI run natively on Linux.

## Requirements

- Linux x64;
- .NET 9 SDK;
- Internet access for the initial NuGet restore;
- `bash`.

Check:

```bash
dotnet --version
```

## Windows Debug build from Linux

```bash
./scripts/linux/build-debug.sh
```

Output:

```text
src/DreamRaster/bin/Debug/net9.0-windows/win-x64/
```

## Release build

```bash
./scripts/linux/build-release.sh
```

## Portable Windows single-file publish

```bash
./scripts/linux/publish-windows.sh
```

Expected output:

```text
src/DreamRaster/bin/Publish/Portable/DreamRaster.exe
```

The script verifies that `DreamRaster.exe` exists and warns if additional root files are produced.

## HTML diagnostics from Linux

If PowerShell 7 (`pwsh`) is installed:

```bash
./scripts/linux/build-diagnostic.sh
```

It uses the same HTML/Markdown/TXT diagnostic engine as Windows with `-NoOpenReport`.

Without `pwsh`, the script falls back to the standard Release build.

## Why this works

The project contains:

```xml
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

The Linux .NET SDK can therefore restore Windows targeting/runtime packs and compile the `win-x64` target.

## Limitation

The result is still a Windows application:

```text
DreamRaster.exe
```

WinForms and WebView2 do not provide a native Linux UI. A true Linux graphical build would require migrating the UI layer to a cross-platform toolkit such as Avalonia, which is a separate project.
