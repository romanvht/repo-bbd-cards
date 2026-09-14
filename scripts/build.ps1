param(
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\REPO',
    [string]$ProfileDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\REPO\profiles\Default'),
    [switch]$UseLocalDependencies,
    [switch]$NoPackage
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent

Push-Location $projectRoot

try {
    $gameAssembly = Join-Path $GameDir 'REPO_Data\Managed\Assembly-CSharp.dll'

    if (-not (Test-Path -LiteralPath $gameAssembly)) {
        throw 'R.E.P.O. game assemblies not found.'
    }

    $dependencies = Join-Path $projectRoot '.deps'

    New-Item -ItemType Directory -Force -Path $dependencies | Out-Null

    if (-not $UseLocalDependencies) {
        foreach ($file in @('BepInEx.dll', '0Harmony.dll')) {
            $source = Join-Path $ProfileDir "BepInEx\core\$file"
            $destination = Join-Path $dependencies $file

            Copy-Item -LiteralPath $source -Destination $destination
        }

        $plugins = Join-Path $ProfileDir 'BepInEx\plugins'
        $repoLib = @(Get-ChildItem -LiteralPath $plugins -Recurse -File -Filter REPOLib.dll)

        if ($repoLib.Count -ne 1) {
            throw 'Expected exactly one REPOLib.dll in the selected profile.'
        }

        Copy-Item -LiteralPath $repoLib[0].FullName -Destination (Join-Path $dependencies 'REPOLib.dll')
    }

    foreach ($file in @('BepInEx.dll', '0Harmony.dll', 'REPOLib.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $dependencies $file))) {
            throw "Build dependency missing: $file"
        }
    }

    $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source

    if (-not $dotnet) {
        $dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    }

    & $dotnet build BbdCards.csproj -c Release --configfile NuGet.Config "-p:GameDir=$GameDir"

    if ($LASTEXITCODE -ne 0) {
        throw 'Build failed; no release package created.'
    }

    if (-not $NoPackage) {
        & (Join-Path $PSScriptRoot 'package.ps1')
    }
}
finally {
    Pop-Location
}
