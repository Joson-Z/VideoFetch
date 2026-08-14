[CmdletBinding()]
param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $repoRoot 'artifacts\VideoFetch-win-x64'
$sourceToolsDirectory = Join-Path $repoRoot 'tools'
$publishedToolsDirectory = Join-Path $publishDirectory 'tools'

Push-Location $repoRoot
try {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'

    if (-not $SkipTests) {
        & (Join-Path $PSScriptRoot 'verify.ps1')
    }

    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    dotnet publish src\VideoFetch.App\VideoFetch.App.csproj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    New-Item -ItemType Directory -Path $publishedToolsDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $sourceToolsDirectory -File | Copy-Item -Destination $publishedToolsDirectory -Force

    Write-Host "Published: $publishDirectory"
    Write-Host 'Before distribution, confirm tools\ contains yt-dlp.exe, ffmpeg.exe, and ffprobe.exe.'
}
finally {
    Pop-Location
}
