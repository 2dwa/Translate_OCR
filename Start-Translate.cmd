@echo off
cd /d "%~dp0"
if exist "dist\Translate-0.4.1\Translate.exe" (
    start "" "dist\Translate-0.4.1\Translate.exe"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\win-app.ps1" -Action run
if errorlevel 1 pause
