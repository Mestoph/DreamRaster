# Build diagnostic — English

Double-click:

```text
scripts\windows\build-diagnostic.bat
```

The script:

1. checks PowerShell, `dotnet`, and the project;
2. runs `dotnet restore`;
3. runs `dotnet build -c Debug`;
4. captures stdout and stderr;
5. extracts `CS`, `NU`, `MSB`, `NETSDK`, and similar errors/warnings;
6. de-duplicates messages repeated by MSBuild's summary;
7. generates bilingual Markdown and plain-text reports;
8. automatically opens the text report in Notepad.

Reports are stored under:

```text
build-reports\YYYY-MM-DD_HH-mm-ss\
```

The latest copies are also written to:

```text
build-reports\LATEST_BUILD_REPORT_FR_EN.md
build-reports\LATEST_BUILD_REPORT_FR_EN.txt
build-reports\LATEST_BUILD_RAW.log
```

For a Release build diagnostic, use:

```text
scripts\windows\build-diagnostic-release.bat
```

When reporting a failure, provide `LATEST_BUILD_REPORT_FR_EN.txt` and `LATEST_BUILD_RAW.log`.

The HTML report highlights errors in red, warnings in amber, and shows a visual Restore/Build summary at the top. It opens automatically after diagnostics.
