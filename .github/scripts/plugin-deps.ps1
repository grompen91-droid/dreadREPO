# Plugin DLLs that must ship beside Dread.dll (Thunderstore + local deploy).
# Keep in sync with Dread.csproj DeployToProfile PluginDeps list.

$script:PluginDependencyDlls = @(
    'Dread.dll',
    'NVorbis.dll',
    'System.Memory.dll',
    'System.Buffers.dll',
    'System.Numerics.Vectors.dll',
    'System.Runtime.CompilerServices.Unsafe.dll'
)

function Copy-PluginBinaries {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourceDir,
        [Parameter(Mandatory = $true)]
        [string] $DestDir
    )

    if (-not (Test-Path $DestDir)) {
        New-Item -ItemType Directory -Force $DestDir | Out-Null
    }

    foreach ($name in $script:PluginDependencyDlls) {
        $src = Join-Path $SourceDir $name
        if (Test-Path $src) {
            Copy-Item $src $DestDir -Force
        } else {
            Write-Warning "Missing plugin binary: $name (expected under $SourceDir)"
        }
    }
}

function Test-PluginBinariesPresent {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Dir
    )

    $missing = @()
    foreach ($name in $script:PluginDependencyDlls) {
        if (-not (Test-Path (Join-Path $Dir $name))) {
            $missing += $name
        }
    }

    if ($missing.Count -gt 0) {
        throw "Plugin folder missing required DLL(s): $($missing -join ', ')"
    }
}
