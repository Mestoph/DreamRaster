@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%\..\..") do set "ROOT=%%~fI"

cd /d "%ROOT%"

powershell.exe ^
    -NoLogo ^
    -NoProfile ^
    -ExecutionPolicy Bypass ^
    -File "%ROOT%\scripts\common\BuildDiagnostic.ps1" ^
    -Configuration Release ^
    -Project "src/DreamRaster/DreamRaster.csproj"

set "RESULT=%ERRORLEVEL%"
pause
exit /b %RESULT%
