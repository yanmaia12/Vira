@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Vira.ps1"
if errorlevel 1 (
    echo.
    echo A instalacao falhou. Consulte a mensagem acima e tente novamente.
    pause
    exit /b 1
)
exit /b 0
