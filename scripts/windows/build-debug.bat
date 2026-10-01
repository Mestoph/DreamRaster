@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%\..\..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\src\DreamRaster\DreamRaster.csproj"

cd /d "%ROOT%"

title DreamRaster - Build Debug

echo.
echo ============================================================
echo   DreamRaster - BUILD DEBUG
echo ============================================================
echo   Racine / Root : %ROOT%
echo   Projet / Project: %PROJECT%
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERREUR / ERROR] Le SDK .NET est introuvable dans PATH.
    echo Installe .NET SDK 9 puis relance ce script.
    pause
    exit /b 1
)

if not exist "%PROJECT%" (
    echo [ERREUR / ERROR] Projet introuvable / Project not found:
    echo   %PROJECT%
    pause
    exit /b 1
)

echo [1/3] Nettoyage du projet...
if exist "%ROOT%\src\DreamRaster\bin" rmdir /s /q "%ROOT%\src\DreamRaster\bin"
if exist "%ROOT%\src\DreamRaster\obj" rmdir /s /q "%ROOT%\src\DreamRaster\obj"

echo.
echo [2/3] Restauration NuGet...
dotnet restore "%PROJECT%" -r win-x64
if errorlevel 1 goto :fail

echo.
echo [3/3] Compilation Debug...
dotnet build "%PROJECT%" ^
    -c Debug ^
    -r win-x64 ^
    --no-restore ^
    --nologo
if errorlevel 1 goto :fail

echo.
echo ============================================================
echo   BUILD DEBUG OK
echo ============================================================
echo.
echo Sortie / Output:
echo   %ROOT%\src\DreamRaster\bin\Debug\net9.0-windows\win-x64\
echo.
pause
exit /b 0

:fail
echo.
echo ============================================================
echo   ECHEC / FAILURE
echo ============================================================
echo.
pause
exit /b 1
