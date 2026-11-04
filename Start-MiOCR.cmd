@echo off
cd /d "%~dp0"
if exist "dist\bundle\MiOcrDesk.exe" (
    start "" "dist\bundle\MiOcrDesk.exe"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\win-app.ps1" -Action run
if errorlevel 1 pause
