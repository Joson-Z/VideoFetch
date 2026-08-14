[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repoRoot
try {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'

    dotnet restore VideoFetch.sln --disable-parallel --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    dotnet build VideoFetch.sln --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    dotnet test VideoFetch.sln --no-build --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }
}
finally {
    Pop-Location
}
