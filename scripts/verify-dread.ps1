#!/usr/bin/env pwsh
# Autonomous verification for Dread mod (Tier 0 static + optional Tier 1 TCP + Tier 2 logs)
param(
    [string]$TargetHost = "",
    [int]$Port = 15432,
    [string]$LogPath = "",
    [switch]$SkipBuild,
    [switch]$SkipMcpBuild,
    [switch]$RequireDebugRegistry
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$report = [ordered]@{
    tier0 = @()
    tier1 = @()
    tier2 = @()
    ok    = $true
}

function Add-Check {
    param([string]$Tier, [string]$Id, [bool]$Ok, [string]$Message)
    $entry = [ordered]@{ id = $Id; ok = $Ok; message = $Message }
    $report[$Tier] += $entry
    if (-not $Ok) { $report.ok = $false }
}

function Invoke-GrepCheck {
    param([string]$Id, [string]$Pattern, [string[]]$Paths, [string]$FailMsg)
    $hits = & grep -rnE $Pattern @Paths 2>$null
    if ($LASTEXITCODE -eq 0 -and $hits) {
        Add-Check -Tier "tier0" -Id $Id -Ok $false -Message $FailMsg
    } else {
        Add-Check -Tier "tier0" -Id $Id -Ok $true -Message "clean"
    }
}

Write-Host "Dread verify (Tier 0 static checks)..."

# Stubs present
$stubDll = ".github/stubs/refs/UnityEngine.dll"
$stubBep = ".github/stubs/refs/core/BepInEx.dll"
Add-Check -Tier "tier0" -Id "stubs" -Ok ((Test-Path $stubDll) -and (Test-Path $stubBep)) `
    -Message $(if ((Test-Path $stubDll) -and (Test-Path $stubBep)) { "stubs ok" } else { "missing stub DLLs; run gen-stubs.ps1" })

# Build
if (-not $SkipBuild) {
    dotnet build Dread.csproj -c Release --nologo -v quiet `
        -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs `
        -p:EnableDebugFeatures=false `
        -p:DeployToProfile=false -p:DeployToDist=false 2>&1 | Out-Null
    Add-Check -Tier "tier0" -Id "dotnet_build" -Ok ($LASTEXITCODE -eq 0) `
        -Message $(if ($LASTEXITCODE -eq 0) { "Release production build ok" } else { "dotnet build failed" })

    $prodDll = "bin/Release/net48/Dread.dll"
    if ((Test-Path $prodDll) -and ($LASTEXITCODE -eq 0)) {
        $debugTypeHits = @()
        foreach ($typeName in @("DebugServerSystem", "DebugOverlaySystem", "TestCrashSystem")) {
            & grep -aq $typeName $prodDll 2>$null
            if ($LASTEXITCODE -eq 0) { $debugTypeHits += $typeName }
        }
        Add-Check -Tier "tier0" -Id "production_dll_no_debug" -Ok ($debugTypeHits.Count -eq 0) `
            -Message $(if ($debugTypeHits.Count -eq 0) {
                "Production DLL has no development-only system types"
            } else {
                "Development-only types in production DLL: $($debugTypeHits -join ', ')"
            })
    }
} else {
    Add-Check -Tier "tier0" -Id "dotnet_build" -Ok $true -Message "skipped"
}

# Analyze grep (mirrors CI)
Invoke-GrepCheck -Id "null_forgiving" -Pattern '\w+!\.|\)!\.(?!.*!= )' `
    -Paths @("*.cs", "Systems/*.cs", "Config/*.cs") -FailMsg "null-forgiving operator found"
Invoke-GrepCheck -Id "hardcoded_paths" -Pattern '[A-Z]:\\' `
    -Paths @("*.cs", "Systems/*.cs", "Config/*.cs") -FailMsg "hardcoded Windows paths"
Invoke-GrepCheck -Id "trailing_whitespace" -Pattern '[[:blank:]]$' `
    -Paths @("*.cs", "Systems/*.cs", "Config/*.cs") -FailMsg "trailing whitespace"
Invoke-GrepCheck -Id "tabs" -Pattern "`t" `
    -Paths @("*.cs", "Systems/*.cs", "Config/*.cs") -FailMsg "tab characters"

# ARCH-3: spawn only via DreadSystemRegistry + DreadSystemInitializer (no stray TryAddSystem< elsewhere)
$arch3Allowed = @(
    "Systems/DreadSystemInitializer.cs",
    "Systems/DreadSystemRegistry.cs"
)
$arch3Hits = @()
foreach ($path in @("*.cs", "Systems", "Systems/Patches", "Systems/PsychoticBreak", "Systems/ErrorReporting", "Systems/DebugOverlay", "Config")) {
    if (-not (Test-Path $path)) { continue }
    $grepOut = & grep -rn "TryAddSystem<" $path 2>$null
    if ($LASTEXITCODE -ne 0) { continue }
    foreach ($line in $grepOut) {
        $rel = ($line -split ":", 2)[0]
        if ($arch3Allowed -notcontains $rel) {
            $arch3Hits += $line
        }
    }
}
if ($arch3Hits.Count -gt 0) {
    $sample = ($arch3Hits | Select-Object -First 3) -join "; "
    Add-Check -Tier "tier0" -Id "arch3_try_add_system" -Ok $false `
        -Message "Stray TryAddSystem< outside DreadSystemInitializer/DreadSystemRegistry (register in DreadSystemRegistry only): $sample"
} else {
    Add-Check -Tier "tier0" -Id "arch3_try_add_system" -Ok $true `
        -Message "No stray TryAddSystem<; runtime spawn via registry + initializer only"
}

# ARCH-3: baseline system types from extension-registry contract
$arch3RegistryPath = "Systems/DreadSystemRegistry.cs"
$arch3CoreTypes = @(
    "AudioDreadSystem",
    "MonsterOverhaulSystem",
    "TensionSystem",
    "ErrorReporterSystem",
    "ErrorReportingPromptSystem",
    "PsychoticBreakSystem",
    "DreadNotificationSystem",
    "CampLureSystem",
    "SnitchSystem"
)
$arch3DebugTypes = @(
    "TestCrashSystem",
    "DebugServerSystem",
    "DebugOverlaySystem"
)
$arch3Missing = @()
$checkDebugTypes = $false
if (-not (Test-Path $arch3RegistryPath)) {
    $arch3Missing = $arch3CoreTypes + $arch3DebugTypes
} else {
    foreach ($typeName in $arch3CoreTypes) {
        $hit = & grep -q $typeName $arch3RegistryPath 2>$null
        if ($LASTEXITCODE -ne 0) { $arch3Missing += $typeName }
    }
    $registryText = Get-Content $arch3RegistryPath -Raw
    $checkDebugTypes = $RequireDebugRegistry -or ($registryText -match '#if DREAD_DEBUG')
    if ($checkDebugTypes) {
        foreach ($typeName in $arch3DebugTypes) {
            $hit = & grep -q $typeName $arch3RegistryPath 2>$null
            if ($LASTEXITCODE -ne 0) { $arch3Missing += $typeName }
        }
    }
}
if ($arch3Missing.Count -gt 0) {
    $missingList = $arch3Missing -join ", "
    Add-Check -Tier "tier0" -Id "arch3_registry_manifest" -Ok $false `
        -Message "DreadSystemRegistry missing baseline types: $missingList"
} else {
    $manifestMsg = if ($checkDebugTypes) {
        "Registry manifest: core + debug system types present"
    } else {
        "Registry manifest: core system types present"
    }
    Add-Check -Tier "tier0" -Id "arch3_registry_manifest" -Ok $true -Message $manifestMsg
}

# MCP npm build
if (-not $SkipMcpBuild) {
    Push-Location dread-mcp-server
    npm ci --silent 2>&1 | Out-Null
    $npmCi = $LASTEXITCODE -eq 0
    npm run build --silent 2>&1 | Out-Null
    $npmBuild = $LASTEXITCODE -eq 0
    Pop-Location
    Add-Check -Tier "tier0" -Id "mcp_build" -Ok ($npmCi -and $npmBuild) `
        -Message $(if ($npmCi -and $npmBuild) { "dread-mcp-server built" } else { "npm ci/build failed" })
} else {
    Add-Check -Tier "tier0" -Id "mcp_build" -Ok $true -Message "skipped"
}

# Package layout (manifest + icon + audio)
$manifestOk = Test-Path "manifest.json"
$iconOk = Test-Path "icon.png"
$audioOk = (Test-Path "audio") -and ((Get-ChildItem audio -Filter *.ogg -ErrorAction SilentlyContinue).Count -ge 4)
Add-Check -Tier "tier0" -Id "manifest" -Ok $manifestOk -Message $(if ($manifestOk) { "manifest.json present" } else { "missing manifest.json" })
Add-Check -Tier "tier0" -Id "icon" -Ok $iconOk -Message $(if ($iconOk) { "icon.png present" } else { "missing icon.png" })
Add-Check -Tier "tier0" -Id "audio" -Ok $audioOk -Message $(if ($audioOk) { "audio/*.ogg present" } else { "missing audio clips" })

# Tier 1: TCP ping + verify when host provided
if ($TargetHost) {
    Write-Host "Tier 1 TCP checks against ${TargetHost}:${Port}..."
    try {
        $tcp = [System.Net.Sockets.TcpClient]::new()
        $tcp.Connect($TargetHost, $Port)
        $stream = $tcp.GetStream()
        $writer = [System.IO.StreamWriter]::new($stream)
        $reader = [System.IO.StreamReader]::new($stream)
        $writer.AutoFlush = $true

        function Send-Cmd {
            param([string]$Cmd)
            $writer.WriteLine("{`"id`":1,`"cmd`":`"$Cmd`",`"data`":{}}")
            return $reader.ReadLine() | ConvertFrom-Json
        }

        $ping = Send-Cmd "ping"
        Add-Check -Tier "tier1" -Id "ping" -Ok ($ping.ok -eq $true) `
            -Message $(if ($ping.ok) { "pong version=$($ping.data.version) port=$($ping.data.port)" } else { $ping.error })

        $verify = Send-Cmd "verify"
        if ($verify.ok) {
            foreach ($check in $verify.data.checks) {
                Add-Check -Tier "tier1" -Id $check.id -Ok ([bool]$check.ok) -Message $check.message
            }
        } else {
            Add-Check -Tier "tier1" -Id "verify" -Ok $false -Message $verify.error
        }

        $tcp.Close()
    } catch {
        Add-Check -Tier "tier1" -Id "tcp_connect" -Ok $false -Message $_.Exception.Message
    }
}

# Tier 2: log pattern checks
if ($LogPath -and (Test-Path $LogPath)) {
    Write-Host "Tier 2 log checks on $LogPath..."
    $log = Get-Content $LogPath -Raw
    Add-Check -Tier "tier2" -Id "mod_loaded" -Ok ($log -match '\[Dread\]') `
        -Message "Dread log lines present"
    Add-Check -Tier "tier2" -Id "systems_init" -Ok ($log -match 'Systems initialized') `
        -Message "systems initialized log"
    Add-Check -Tier "tier2" -Id "debug_server" -Ok ($log -match '\[Dread DebugServer\] LISTENING') `
        -Message "debug server listening log"
} elseif ($LogPath) {
    Add-Check -Tier "tier2" -Id "log_path" -Ok $false -Message "log file not found: $LogPath"
}

$json = $report | ConvertTo-Json -Depth 6
Write-Output $json

if (-not $report.ok) { exit 1 }
exit 0
