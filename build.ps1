param(
    [string]$Version = "",
    [switch]$DebugBuild
)

if ([string]::IsNullOrEmpty($Version)) {
    $Version = (Get-Content manifest.json -Raw | ConvertFrom-Json).version_number
    Write-Host "No version specified, using manifest version: $Version"
}

$name = "elytraking-Dread"
$outDir = "dist\$name-$Version"

Remove-Item -Recurse -Force dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $outDir | Out-Null
New-Item -ItemType Directory -Force "$outDir\BepInEx\plugins\$name" | Out-Null

# Release = production (no debug overlay, TCP server, or test crash; Logging is section 8). Use -DebugBuild for agents.
Write-Host "Building..."
$stubsDir = ".github/stubs/refs"
$stubsExist = (Test-Path "$stubsDir/UnityEngine.dll") -and (Test-Path "$stubsDir/core/BepInEx.dll")
$config = if ($DebugBuild) { "Debug" } else { "Release" }
$buildArgs = @(
    "build", "Dread.csproj", "-c", $config, "--nologo", "-v", "quiet"
)
if (-not $DebugBuild) {
    $buildArgs += "-p:EnableDebugFeatures=false"
}
$gameDll = "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\UnityEngine.dll"
if ($stubsExist -and -not (Test-Path $gameDll)) {
    Write-Warning "Building against generated stubs in $stubsDir. Harmony patches may fail at runtime (BadImageFormatException). Install REPO or set GameDir to real Managed folder for production DLLs."
    $buildArgs += "-p:GameDir=$stubsDir", "-p:BepInExDir=$stubsDir", "-p:DeployToProfile=false", "-p:DeployToDist=false"
}
dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

# Copy mod files into Thunderstore package layout
$pluginOut = "$outDir\BepInEx\plugins\$name"
$binDir = if ($DebugBuild) { "bin\Debug\net48" } else { "bin\Release\net48" }
. "$PSScriptRoot/.github/scripts/plugin-deps.ps1"
Copy-PluginBinaries -SourceDir $binDir -DestDir $pluginOut
Test-PluginBinariesPresent -Dir $pluginOut

if (Test-Path "audio") {
    Copy-Item -Recurse "audio" "$outDir\BepInEx\plugins\$name\"
}

Copy-Item "manifest.json" $outDir
if (Test-Path "THUNDERSTORE_README.md") {
    Copy-Item "THUNDERSTORE_README.md" "$outDir\README.md"
} else {
    Copy-Item "README.md" $outDir
}

if (-not (Test-Path "icon.png")) {
    Write-Warning "icon.png missing! Thunderstore requires a 256x256 PNG icon. Add icon.png to the project root before uploading."
} else {
    Copy-Item "icon.png" $outDir
}

# Zip for Thunderstore upload
$zipPath = "dist\$name-$Version.zip"
Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath -Force
Write-Host "Package built: $zipPath"
Write-Host ""
Write-Host "To install locally for testing:"
Write-Host "  Copy '$outDir\BepInEx' to your r2modman profile directory"
if ($DebugBuild) {
    Write-Host ""
    Write-Host "Debug build: sections 8-11 (overlay, server, logging as 10, test crash)."
} else {
    Write-Host ""
    Write-Host "Production build: logging section 8 only. Use -DebugBuild for MCP/test crash."
}
