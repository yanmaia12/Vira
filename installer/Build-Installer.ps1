param(
    [ValidateSet('win-x64')]
    [string] $RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'ConversorFicheiros\ConversorFicheiros.csproj'
$artifactDirectory = Join-Path $projectRoot 'artifacts\installer'
$publishDirectory = Join-Path $artifactDirectory "publish\$RuntimeIdentifier"
$setupFile = Join-Path $artifactDirectory 'Vira-Setup.exe'
$iexpress = Join-Path $env:WINDIR 'System32\iexpress.exe'

if (-not (Test-Path -LiteralPath $iexpress)) {
    throw 'O IExpress do Windows nao foi encontrado.'
}

New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

dotnet publish $projectFile -c Release -r $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'A publicacao da aplicacao falhou.'
}

$executableName = 'ConversorFicheiros.exe'
Copy-Item -LiteralPath (Join-Path $publishDirectory $executableName) -Destination $artifactDirectory -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install.cmd') -Destination $artifactDirectory -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Vira.ps1') -Destination $artifactDirectory -Force

$sedFile = Join-Path $artifactDirectory 'Vira-Setup.sed'
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=$setupFile
FriendlyName=Vira - Conversor de Ficheiros
AppLaunched=Install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0=$artifactDirectory
[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=Instalacao do Vira concluida.
FILE0=$executableName
FILE1=Install.cmd
FILE2=Install-Vira.ps1
"@
Set-Content -LiteralPath $sedFile -Value $sed -Encoding Ascii

& $iexpress /N /Q $sedFile
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $setupFile)) {
    throw 'O IExpress nao conseguiu criar o instalador.'
}

Write-Host "Instalador criado: $setupFile"
