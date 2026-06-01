# Upload per-file OGG assets to a GitHub release (names from audio-manifest.json).
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag
)

$ErrorActionPreference = "Stop"
$manifestPath = Join-Path $PSScriptRoot "..\..\audio\audio-manifest.json"
if (-not (Test-Path $manifestPath)) {
    Write-Error "Missing $manifestPath"
    exit 1
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$audioRoot = Join-Path $PSScriptRoot "..\..\audio"
$uploaded = 0

foreach ($entry in $manifest.files) {
    $rel = $entry.path -replace '/', [IO.Path]::DirectorySeparatorChar
    $filePath = Join-Path $audioRoot $rel
    if (-not (Test-Path $filePath)) {
        Write-Error "Manifest file missing on disk: $($entry.path)"
        exit 1
    }

    $assetName = if ($entry.assetName) { $entry.assetName } else { $entry.path -replace '/', '__' }
    Write-Host "Uploading $assetName <= $($entry.path)"
    gh release upload $ReleaseTag $filePath --clobber -n $assetName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "gh release upload failed for $assetName"
        exit 1
    }
    $uploaded++
}

Write-Host "Uploaded $uploaded audio release asset(s) to $ReleaseTag"
