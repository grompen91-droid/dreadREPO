# Quickstart: Production build strip debug (012)

**Branch**: `012-production-build-strip-debug`

## Prerequisites

- .NET SDK 8+, PowerShell 7+
- Stubs: `pwsh -NoProfile .github/scripts/gen-stubs.ps1`

## Matrix 1: Production stub build (Tier 0, US1 + US2)

| Step | Command / action | Expected |
|------|------------------|----------|
| 1 | `dotnet build Dread.csproj -c Release -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs -p:EnableDebugFeatures=false -p:DeployToProfile=false -p:DeployToDist=false` | 0 errors |
| 2 | `bash .github/scripts/verify-production-dll.sh bin/Release/net48/Dread.dll` | Pass (no overlay/server; TestCrash present) |
| 3 | `./scripts/verify-dread.ps1` | Tier 0 pass |
| 4 | Inspect `Dread.dll` size vs prior Release (optional) | Smaller or equal |

## Matrix 2: Development stub build (US3)

| Step | Command / action | Expected |
|------|------------------|----------|
| 1 | `dotnet build Dread.csproj -c Debug -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs` | 0 errors |
| 2 | `strings bin/Debug/net48/Dread.dll \| grep DebugServerSystem` | Match found |
| 3 | Registry source contains debug rows inside `#if DREAD_DEBUG` | Yes |

## Matrix 3: In-game production behavior (manual, host)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Deploy **Release production** DLL to r2modman profile | Mod loads |
| 2 | Open BepInEx config (Configuration Manager) | No sections 8 or 9; section **11. Testing** present |
| 3 | Play extraction level | Core features work (audio, tension, etc.) |
| 4 | Press F10 | No debug overlay |
| 5 | Enable error reporting; trigger real exception (not test crash) | Report may send (ERR-2) |

## Matrix 4: In-game development + MCP (manual, US3)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Deploy **Debug** DLL; set `DebugServerEnabled=true`; restart game | Log: `[Dread DebugServer] LISTENING` |
| 2 | `cd dread-mcp-server && npm run build` | MCP builds |
| 3 | `dread_ping` via MCP (game in level) | Success |
| 4 | F10 with `DebugOverlayEnabled=true` | Overlay visible |

## Matrix 5: CD simulation (maintainer)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Push PR; CI build job runs | Green |
| 2 | After merge, release via `vpatch` tag | CD build + package zip verify use production profile |
| 3 | Download Thunderstore zip from GitHub Release | `verify-production-dll.sh` on zip `Dread.dll` *(automated in CD package job)* |

## Rollback

If production build breaks gameplay: revert `Dread.csproj` conditioning and `#if` blocks; CD returns to shipping full DLL with config-gated debug (prior behavior).

## Done checklist

- [ ] Matrix 1 pass (agent/CI)
- [ ] Matrix 2 pass (agent)
- [ ] Matrix 3 pass (human, one session)
- [ ] Matrix 4 pass (human or agent with game, optional pre-release)
- [ ] CHANGELOG `[Unreleased]` updated
