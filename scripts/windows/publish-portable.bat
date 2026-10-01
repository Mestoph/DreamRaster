@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%\..\..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\src\DreamRaster\DreamRaster.csproj"
set "PROFILE=%ROOT%\src\DreamRaster\Properties\PublishProfiles\Portable.pubxml"
set "OUT=%ROOT%\src\DreamRaster\bin\Publish\Portable"

cd /d "%ROOT%"

title DreamRaster - Publish Portable ONE EXE

echo.
echo ============================================================
echo   DreamRaster - PUBLICATION PORTABLE ONE EXE
echo ============================================================
echo   Racine / Root : %ROOT%
echo   Projet / Project: %PROJECT%
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERREUR / ERROR] Le SDK .NET est introuvable dans PATH.
    pause
    exit /b 1
)

if not exist "%PROJECT%" (
    echo [ERREUR / ERROR] Projet introuvable / Project not found.
    pause
    exit /b 1
)

if not exist "%PROFILE%" (
    echo [ERREUR / ERROR] Profil de publication introuvable.
    pause
    exit /b 1
)

echo [1/4] Nettoyage publication...
if exist "%OUT%" rmdir /s /q "%OUT%"
if exist "%ROOT%\src\DreamRaster\obj\Release\net9.0-windows\win-x64\PubTmp" ^
    rmdir /s /q "%ROOT%\src\DreamRaster\obj\Release\net9.0-windows\win-x64\PubTmp"

echo.
echo [2/4] Restauration NuGet...
dotnet restore "%PROJECT%" -r win-x64
if errorlevel 1 goto :fail

echo.
echo [3/4] Publication single-file...
dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --no-restore ^
    /p:PublishProfile=Portable ^
    --nologo
if errorlevel 1 goto :fail

echo.
echo [4/4] Verification de la sortie...
del /q "%OUT%\Microsoft.Web.WebView2.Core.xml" >nul 2>&1
del /q "%OUT%\Microsoft.Web.WebView2.WinForms.xml" >nul 2>&1

if not exist "%OUT%\DreamRaster.exe" (
    echo [ERREUR / ERROR] DreamRaster.exe absent.
    goto :fail
)

for %%F in ("%OUT%\*") do (
    if /I not "%%~nxF"=="DreamRaster.exe" (
        echo [ATTENTION / WARNING] Fichier supplementaire : %%~nxF
    )
)

echo.
echo ============================================================
echo   PUBLICATION TERMINEE / PUBLISH COMPLETE
echo ============================================================
echo.
echo   %OUT%\
echo.
explorer "%OUT%" >nul 2>&1
pause
exit /b 0

:fail
echo.
echo ============================================================
echo   ECHEC DE LA PUBLICATION / PUBLISH FAILED
echo ============================================================
echo.
pause
exit /b 1
