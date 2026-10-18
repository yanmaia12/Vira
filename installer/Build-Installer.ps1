param(
    [ValidateSet('win-x64')]
    [string] $RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $projectRoot 'ConversorFicheiros\ConversorFicheiros.csproj'
$setupProject = Join-Path $PSScriptRoot 'Setup\Setup.csproj'
$artifactDirectory = Join-Path $projectRoot 'artifacts\installer'
$appPublishDirectory = Join-Path $artifactDirectory "publish\$RuntimeIdentifier"
$setupPublishDirectory = Join-Path $artifactDirectory "setup-publish\$RuntimeIdentifier"
$setupFile = Join-Path $projectRoot 'Vira-Setup.exe'
$distributionDirectory = Join-Path $projectRoot 'artifacts\distribution'
$packageStagingDirectory = Join-Path $distributionDirectory 'package'
$packageArchive = Join-Path $distributionDirectory 'Vira-Windows-x64.zip'
$payloadArchive = Join-Path $artifactDirectory ("Vira-Payload-$([guid]::NewGuid().ToString('N')).zip")

New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $appPublishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $setupPublishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $packageStagingDirectory -Force | Out-Null

dotnet publish $appProject -c Release -r $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -o $appPublishDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'The Vira application publish failed.'
}

$appExecutable = Join-Path $appPublishDirectory 'ConversorFicheiros.exe'
if (-not (Test-Path -LiteralPath $appExecutable)) {
    throw 'The published Vira application executable was not found.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($payloadArchive, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $null = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $appExecutable,
        'ConversorFicheiros.exe',
        [System.IO.Compression.CompressionLevel]::Optimal)
}
finally {
    $archive.Dispose()
}

dotnet publish $setupProject -c Release -r $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true "-p:ViraPayloadPath=$payloadArchive" `
    -p:DebugType=None -p:DebugSymbols=false -o $setupPublishDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'The Vira setup publish failed.'
}

$publishedSetup = Join-Path $setupPublishDirectory 'Vira-Setup.exe'
if (-not (Test-Path -LiteralPath $publishedSetup)) {
    throw 'The published Vira setup executable was not found.'
}

Copy-Item -LiteralPath $publishedSetup -Destination $setupFile -Force
Copy-Item -LiteralPath $setupFile -Destination (Join-Path $packageStagingDirectory 'Vira-Setup.exe') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Setup\README-INSTALL.txt') `
    -Destination (Join-Path $packageStagingDirectory 'README-INSTALL.txt') -Force
Compress-Archive -Path (Join-Path $packageStagingDirectory '*') -DestinationPath $packageArchive `
    -CompressionLevel Optimal -Force
Write-Host "Current setup created: $setupFile"
Write-Host "Ready-to-share package created: $packageArchive"
