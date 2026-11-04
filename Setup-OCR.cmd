@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\install-ocr-runtime.ps1"
if errorlevel 1 (
    echo.
    echo Setup failed. Read the error above and retry.
) else (
    echo.
    echo OCR is ready. Open MiOcrDesk.exe, then choose Local model - Detect and start.
)
pause
