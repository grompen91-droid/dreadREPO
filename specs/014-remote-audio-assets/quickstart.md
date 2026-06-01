# Quickstart: Remote audio assets (014)

**Branch**: `014-remote-audio-assets`

## Debug build and deploy (r2modman `dread` profile)

Use the existing **Debug** configuration (`DREAD_DEBUG`: overlay, debug server, test crash). No separate deploy script.

**Linux** (flat plugin layout under `elytraking-Dread/`):

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1

PLUGIN="$HOME/.config/r2modmanPlus-local/REPO/profiles/dread/BepInEx/plugins/elytraking-Dread"
BEPINEX="$HOME/.config/r2modmanPlus-local/REPO/profiles/dread/BepInEx"

dotnet build Dread.csproj -c Debug \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir="$BEPINEX" \
  -p:PluginDir="$PLUGIN"
```

`DeployToProfile` copies `Dread.dll` + NVorbis deps when `PluginDir` exists. Debug builds also copy repo `audio/**` beside the plugin for offline baked audio (imported into `audio-cache` on startup).

Seed `audio-cache` for offline audio (same as CI smoke-test):

```bash
PLUGIN="$HOME/.config/r2modmanPlus-local/REPO/profiles/dread/BepInEx/plugins/elytraking-Dread"
pwsh -NoProfile -Command '
  $m = Get-Content audio/audio-manifest.json -Raw | ConvertFrom-Json
  $root = Join-Path $env:PLUGIN "audio-cache/v$($m.modVersion)"
  foreach ($e in $m.files) {
    $dest = Join-Path $root $e.path
    New-Item -Force (Split-Path $dest) | Out-Null
    Copy-Item (Join-Path audio $e.path) $dest -Force
  }
' 
```

(Set `$env:PLUGIN` to your plugin path before the block, or inline the path.)

**Windows** (adjust profile name/path):

```powershell
$plugin = "$env:APPDATA\r2modmanPlus-local\REPO\profiles\dread\BepInEx\plugins\elytraking-Dread"
$bep = "$env:APPDATA\r2modmanPlus-local\REPO\profiles\dread\BepInEx"
dotnet build Dread.csproj -c Debug -p:PluginDir=$plugin -p:BepInExDir=$bep
```

For a **Thunderstore-style package** (no profile deploy), use:

```powershell
.\build.ps1 -DebugBuild
```

Pass `-p:GameDir="C:\...\REPO_Data\Managed"` when you have the game installed (better Harmony compatibility than stubs).

## In-game checks

1. Launch R.E.P.O. with the **dread** profile in r2modman.
2. Enable `DebugOverlayEnabled` and `DebugServerEnabled` (restart once).
3. Enter a run (not menu/truck). Wait for ambient sounds (30s warmup).
4. Overlay **Audio** row: clip count toward `5/5`, queue `0` when done.
5. Snitch: pick up armed item, hear `snitch_bang` (host).
6. BepInEx log: `[AudioAssets] Cache v1.6.1:` reconcile lines, no decode errors.

## Network first-run test

Delete `audio-cache` in the plugin folder, launch online, confirm downloads from GitHub Release `v1.6.1` (`category__file.ogg` asset names).

## Tier 0 (no game)

```bash
pwsh -NoProfile scripts/verify-dread.ps1
```
