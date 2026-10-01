@echo off
setlocal EnableExtensions
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%\..\..") do set "ROOT=%%~fI"
cd /d "%ROOT%"

REM FR: Cree le tag correspondant a la v25 et declenche GitHub Actions.
REM EN: Creates the v25 tag and triggers GitHub Actions.

set "TAG=v34.0.0"

git status --short
echo.
set /p CONFIRM=Creer et pousser %TAG% ? [o/N]: 
if /I not "%CONFIRM%"=="o" exit /b 0

git tag "%TAG%"
if errorlevel 1 goto :fail

git push origin "%TAG%"
if errorlevel 1 goto :fail

echo Release declenchee / Release triggered.
pause
exit /b 0

:fail
echo Echec / Failure.
pause
exit /b 1
