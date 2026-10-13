@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-local-mp.ps1" %*
if errorlevel 1 (
    echo.
    echo Launch failed.
    pause
)
