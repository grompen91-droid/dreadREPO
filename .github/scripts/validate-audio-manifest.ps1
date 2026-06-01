# Validates audio-manifest.json against repo files and Plugin.VERSION.
param(
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$manifestPath = Join-Path $repoRoot "audio/audio-manifest.json"
$pluginPath = Join-Path $repoRoot "Plugin.cs"

if (-not (Test-Path $manifestPath)) {
    Write-Error "Missing audio/audio-manifest.json"
    exit 1
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
if ($manifest.schema -ne 1) {
    Write-Error "Unsupported manifest schema: $($manifest.schema)"
    exit 1
}

if ([string]::IsNullOrEmpty($ExpectedVersion)) {
    if ($manifest.modVersion -match '([\d.]+)') {
        $ExpectedVersion = $Matches[1]
    }
}

if (Test-Path $pluginPath) {
    $plugin = Get-Content $pluginPath -Raw
    if ($plugin -match 'VERSION = "([\d.]+)"') {
        $pluginVer = $Matches[1]
        if ($manifest.modVersion -ne $pluginVer) {
            Write-Error "manifest modVersion $($manifest.modVersion) != Plugin.VERSION $pluginVer"
            exit 1
        }
    }
}

$audioRoot = Join-Path $repoRoot "audio"
$count = 0
foreach ($entry in $manifest.files) {
    $rel = $entry.path -replace '/', [IO.Path]::DirectorySeparatorChar
    $filePath = Join-Path $audioRoot $rel
    if (-not (Test-Path $filePath)) {
        Write-Error "Missing file for manifest entry: $($entry.path)"
        exit 1
    }

    $len = (Get-Item $filePath).Length
    if ($entry.sizeBytes -gt 0 -and $len -ne $entry.sizeBytes) {
        Write-Error "sizeBytes mismatch for $($entry.path): manifest=$($entry.sizeBytes) disk=$len"
        exit 1
    }

    if ($entry.sha256) {
        $hash = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
        $expected = $entry.sha256.ToLowerInvariant()
        if ($hash -ne $expected) {
            Write-Error "sha256 mismatch for $($entry.path): manifest=$expected disk=$hash"
            exit 1
        }
    }

    $count++
}

Write-Host "audio-manifest.json OK ($count files)"
