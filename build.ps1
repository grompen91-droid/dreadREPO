param(
    [string]$Version = ""
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

# Build release DLL
Write-Host "Building..."
$stubsDir = ".github/stubs/refs"
$stubsExist = (Test-Path "$stubsDir/UnityEngine.dll") -and (Test-Path "$stubsDir/core/BepInEx.dll")
$buildArgs = @(
    "build", "Dread.csproj", "-c", "Release", "--nologo", "-v", "quiet"
)
$gameDll = "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\UnityEngine.dll"
if ($stubsExist -and -not (Test-Path $gameDll)) {
    Write-Warning "Building against generated stubs in $stubsDir. Harmony patches may fail at runtime (BadImageFormatException). Install REPO or set GameDir to real Managed folder for production DLLs."
    $buildArgs += "-p:GameDir=$stubsDir", "-p:BepInExDir=$stubsDir", "-p:DeployToProfile=false", "-p:DeployToDist=false"
}
dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

# Copy mod files into Thunderstore package layout
$pluginOut = "$outDir\BepInEx\plugins\$name"
. "$PSScriptRoot/.github/scripts/plugin-deps.ps1"
Copy-PluginBinaries -SourceDir "bin\Release\net48" -DestDir $pluginOut
Test-PluginBinariesPresent -Dir $pluginOut

pwsh -NoProfile "$PSScriptRoot/.github/scripts/validate-audio-manifest.ps1"
if ($LASTEXITCODE -ne 0) { Write-Error "audio-manifest validation failed"; exit 1 }

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
