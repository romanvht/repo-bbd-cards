$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'manifest.json') -Raw | ConvertFrom-Json
$files = [ordered]@{
    'BbdCards.dll' = (Join-Path $projectRoot 'bin\Release\netstandard2.1\BbdCards.dll')
    'manifest.json' = (Join-Path $projectRoot 'manifest.json')
    'README.md' = (Join-Path $projectRoot 'THUNDERSTORE.md')
    'CHANGELOG.md' = (Join-Path $projectRoot 'CHANGELOG.md')
    'icon.png' = (Join-Path $projectRoot 'assets\icon.png')
}

foreach ($path in $files.Values) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Package input missing: $path"
    }
}

$dist = Join-Path $projectRoot 'dist'

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$archive = Join-Path $dist "BbdCards-$($manifest.version_number).zip"
$temporaryArchive = "$archive.$([Guid]::NewGuid().ToString('N')).tmp"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

try {
    $zip = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)

    try {
        foreach ($entry in $files.GetEnumerator()) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $entry.Value,
                $entry.Key,
                [IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }

    Move-Item -LiteralPath $temporaryArchive -Destination $archive -Force
}
finally {
    if (Test-Path -LiteralPath $temporaryArchive) {
        Remove-Item -LiteralPath $temporaryArchive
    }
}

Copy-Item -LiteralPath $files['BbdCards.dll'] -Destination (Join-Path $dist 'BbdCards.dll')

Write-Output "Created $archive"
