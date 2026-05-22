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
dotnet build Dread.csproj -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }

# Copy mod files into Thunderstore package layout
Copy-Item "bin\Release\net48\Dread.dll" "$outDir\BepInEx\plugins\$name\"

if (Test-Path "audio") {
    Copy-Item -Recurse "audio" "$outDir\BepInEx\plugins\$name\"
}

Copy-Item "manifest.json" $outDir
Copy-Item "README.md" $outDir

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
