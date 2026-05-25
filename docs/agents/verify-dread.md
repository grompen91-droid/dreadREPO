# Dread autonomous verify runbook

Agents use this sequence to verify the Dread mod without manual in-game steps.

## Prerequisites

1. REPO running with Dread installed from a local or Thunderstore build.
2. BepInEx config: `DebugServerEnabled=true` under **8. Debug Server** (requires game restart after first enable).
3. Cursor MCP server `dread` configured in `.cursor/mcp.json` (stdio to `dread-mcp-server/dist/index.js`).
4. Optional: set `DREAD_PORT` if the server bound to fallback port (check BepInEx log for `LISTENING 127.0.0.1:PORT`).

## Tier 0: Static (no game)

From repo root:

```powershell
./scripts/verify-dread.ps1
```

Checks: stub DLLs, Release build, CI-style grep analysis, MCP npm build, manifest/icon/audio layout. Emits JSON; exit code 1 on failure.

## Tier 1: Live TCP (game running)

```powershell
./scripts/verify-dread.ps1 -Host 127.0.0.1 -Port 15432
```

Or use MCP tools (preferred for agents):

| Step | MCP tool | Purpose |
|------|----------|---------|
| 1 | `dread_ping` | Confirm debug server alive, read bound port |
| 2 | `dread_verify` | Health checks (version, systems, clips, overlay, patches) |
| 3 | `dread_get_runtime_state` | Tension / psychotic break / audio snapshot |
| 4 | `dread_get_config` | Flat keys + grouped `sections` with `debugKey` |
| 5 | `dread_get_logs` | Recent BepInEx buffer (init errors) |

## Tier 2: Log file (post-session)

```powershell
./scripts/verify-dread.ps1 -LogPath "C:\path\to\BepInEx\LogOutput.log"
```

Patterns: `[Dread]`, `Systems initialized`, `[Dread DebugServer] LISTENING`.

## Config keys for `dread_set_config`

Split `debugKey` from `dread_get_config` sections on the first dot:

| debugKey | section | key | Notes |
|----------|---------|-----|-------|
| `audio.enabled` | `audio` | `enabled` | |
| `debugServer.enabled` | `debugServer` | `enabled` | Restart required |
| `debugServer.port` | `debugServer` | `port` | Restart required |
| `overlay.enabled` | `overlay` | `enabled` | Debug overlay HUD |
| `psychoticBreak.enabled` | `psychoticBreak` | `enabled` | |
| `compatibility.mode` | `compatibility` | `mode` | Ambient-only mode |
| `errorReporting` | `errorReporting` | `` | Bare key (empty suffix) |
| `logging.level` | `logging` | `level` | None, Error, Debug, Verbose |

## Feature test commands (destructive)

Only available when `DebugServerEnabled=true`:

| MCP tool | TCP cmd | Effect |
|----------|---------|--------|
| `dread_force_psychotic_break` | `force_psychotic_break` | Starts psychotic break episode |
| `dread_trigger_test_crash` | `trigger_test_crash` | Crashes game (error reporting test) |

After forcing psychotic break, call `dread_get_runtime_state` and confirm `psychoticBreakEpisodeActive: true`.

## Example agent workflow

1. Run `./scripts/verify-dread.ps1` (Tier 0).
2. Ask user to launch REPO with debug server enabled, or enable via config and restart.
3. `dread_ping` until success.
4. `dread_verify` — all checks should pass in a loaded run (audio/psychotic clips may fail on main menu before level load).
5. `dread_get_runtime_state` — inspect block reasons for psychotic break.
6. Optional: `dread_set_config` section=`overlay` key=`enabled` value=`true`, then confirm overlay host via verify.
7. `dread_shutdown` when done (optional).

See `docs/agents/verify-dread-checklist.json` for machine-readable step IDs.
