$ErrorActionPreference = 'Stop'

$sourceExe = Join-Path $PSScriptRoot 'ConversorFicheiros.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw 'O executavel da aplicacao nao foi encontrado no pacote.'
}

$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\Vira'
$startMenuDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Vira'
New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $startMenuDirectory -Force | Out-Null
$installedExe = Join-Path $installDirectory 'ConversorFicheiros.exe'
Copy-Item -LiteralPath $sourceExe -Destination $installedExe -Force

$shell = New-Object -ComObject WScript.Shell
$startShortcut = $shell.CreateShortcut((Join-Path $startMenuDirectory 'Vira.lnk'))
$startShortcut.TargetPath = $installedExe
$startShortcut.WorkingDirectory = $installDirectory
$startShortcut.Description = 'Conversor de documentos e assinatura PDF'
$startShortcut.Save()

$desktopShortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Vira.lnk'
$desktopShortcut = $shell.CreateShortcut($desktopShortcutPath)
$desktopShortcut.TargetPath = $installedExe
$desktopShortcut.WorkingDirectory = $installDirectory
$desktopShortcut.Description = 'Conversor de documentos e assinatura PDF'
$desktopShortcut.Save()

& $installedExe --register-context-menu
if ($LASTEXITCODE -ne 0) {
    throw 'A aplicacao foi copiada, mas nao foi possivel registar o menu de contexto.'
}

Write-Host 'Vira foi instalado para este utilizador.'
Write-Host 'Abra a aplicacao pelo atalho Vira no Menu Iniciar ou no Ambiente de Trabalho.'
