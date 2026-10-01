@echo off
setlocal EnableExtensions
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%\..\..") do set "ROOT=%%~fI"
cd /d "%ROOT%"

REM FR: Cree le depot public officiel DreamRaster via GitHub CLI.
REM EN: Creates the official public DreamRaster repository through GitHub CLI.

set "REPO=Mestoph/DreamRaster"

where gh >nul 2>&1
if errorlevel 1 (
    echo [ERREUR / ERROR] GitHub CLI ^(gh^) est introuvable.
    echo Installe GitHub CLI puis execute: gh auth login
    pause
    exit /b 1
)

gh auth status
if errorlevel 1 (
    echo.
    echo [ERREUR / ERROR] GitHub CLI n'est pas authentifie.
    echo Execute: gh auth login
    pause
    exit /b 1
)

if not exist ".git" (
    git init
    if errorlevel 1 goto :fail
)

git add .
git commit -m "Initial public release of DreamRaster"
if errorlevel 1 (
    echo Aucun nouveau commit ou erreur Git. Continuation...
)

git branch -M main

gh repo view "%REPO%" >nul 2>&1
if errorlevel 1 (
    gh repo create "%REPO%" ^
        --public ^
        --description "Portable local AI image generation studio with FLUX.2, ComfyUI, Ollama and OpenCode." ^
        --source "." ^
        --remote origin ^
        --push
    if errorlevel 1 goto :fail
) else (
    git remote get-url origin >nul 2>&1
    if errorlevel 1 git remote add origin "https://github.com/%REPO%.git"

    git push -u origin main
    if errorlevel 1 goto :fail
)

gh repo edit "%REPO%" --enable-issues=true >nul 2>&1
gh repo edit "%REPO%" --add-topic image-generation --add-topic local-ai --add-topic comfyui --add-topic flux --add-topic ollama --add-topic winforms --add-topic portable >nul 2>&1

echo.
echo ============================================================
echo   DEPOT PRET / REPOSITORY READY
echo ============================================================
echo   https://github.com/%REPO%
echo.
echo Licence / License: GNU AGPL-3.0-or-later
echo Auteur / Author: Mestoph
echo CLA: CLA.md
echo NOTICE: NOTICE
echo Sources: DOWNLOAD_SOURCES.md
echo.
pause
exit /b 0

:fail
echo.
echo ECHEC / FAILURE
pause
exit /b 1
