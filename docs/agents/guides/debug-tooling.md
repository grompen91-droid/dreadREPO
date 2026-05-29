# Debug tooling (server, overlay, MCP, TestCrash)

Agent-facing runtime inspection. All default **off** except logging. ADR: `docs/adr/0013-debug-server.md`, `0012` (TestCrash), `0014` (logging cross-ref in [config-and-logging.md](config-and-logging.md)).

## Debug server (`DebugServerSystem.cs`)

| Setting | Default | Notes |
|---------|---------|-------|
| `DebugServerEnabled` | false | Requires **game restart** after toggle |
| `DebugServerPort` | 15432 | Binds `127.0.0.1` only; may fallback +1 |

Background thread accepts TCP connections. Commands processed on Unity main thread via queue.

### TCP commands (newline-delimited JSON)

| cmd | Purpose |
|-----|---------|
| `ping` | Alive + version + bound port |
| `get_state` | Legacy state snapshot |
| `get_runtime_state` | Tension, psychotic break, audio timers |
| `get_config` | Flat keys + grouped `sections` with `debugKey` |
| `set_config` | Section/key/value (see verify runbook) |
| `get_patches` | Harmony patch info |
| `get_logs` | Ring buffer of recent BepInEx log lines |
| `verify` | Health check bundle for agents |
| `trigger_test_crash` | TestCrash for error reporting |
| `force_psychotic_break` | Start episode |
| `shutdown` | Stop listener |

Log line to confirm: `[Dread DebugServer] LISTENING 127.0.0.1:PORT`

## MCP bridge (`dread-mcp-server/`)

| Item | Detail |
|------|--------|
| Transport | stdio (`@modelcontextprotocol/sdk`) |
| Config | `.cursor/mcp.json` points at `dread-mcp-server/dist/index.js` |
| Env | `DREAD_HOST`, `DREAD_PORT`, `DREAD_TIMEOUT` |

Build before use:

```bash
cd dread-mcp-server && npm install && npm run build
```

Tools map 1:1 to TCP commands (`dread_ping`, `dread_verify`, `dread_set_config`, etc.).

Agent workflow: [verify-dread.md](../verify-dread.md) Tier 1, checklist JSON Tier 1 `mcp_sequence`.

## `DreadRuntimeState` (overlay + MCP)

Internal static snapshot updated by gameplay systems. Supported fields (ARCH-3, see [ADR-0016](../../adr/0016-arch-3-extension-model.md)):

| Field | Source system | Use |
|-------|---------------|-----|
| `NearestEnemyDist` | Tension | Proximity HUD |
| `AdrenalineActive`, `PanicSprintActive`, `PanicSprintCooldown` | Tension | Sprint modifiers |
| `PsychoticBreak*` (enabled, episode, timers, threat/enemy counts, clips) | Psychotic break | Episode HUD |
| `AudioClipCount`, `AudioNextPlayIn` | Audio Dread | Ambient scheduler |
| `DreadPatchCount` | Overlay refresh | Harmony count |

Add fields here when overlay or `get_runtime_state` needs new live data; avoid new reflection in debug paths.

## Debug overlay (`Systems/DebugOverlay/`)

| Setting | Behavior |
|---------|----------|
| `DebugOverlayEnabled` | default false |
| Toggle | **F10** at runtime when enabled |
| Data | `DreadRuntimeState` + patch count refresh every 0.5s |

Hidden on menu levels (`SemiFunc.MenuLevel()`). IMGUI only, no Unity UI package required at init.

## TestCrash (`TestCrashSystem.cs`)

| Trigger | Path |
|---------|------|
| Config button | `7. Testing` → "Crash Game" (Configuration Manager) |
| MCP/TCP | `TestCrashSystem.TriggerForDebug()` |

Requires level loaded (`TestCrashSystem` host exists). Crashes with `[Dread TestCrash]` marker for log filtering.

## Wiring new debug surfaces

1. Publish fields on `DreadRuntimeState` from owning system
2. Add to `DebugServerSystem` verify checks if health-relevant
3. Add `debugKey` in `get_config` sections when MCP-tunable
4. Document keys in [verify-dread.md](../verify-dread.md) config table

## Security

Localhost-only TCP. Do not expose port publicly. MCP runs locally via stdio.

## Verify tiers

| Tier | What |
|------|------|
| 0 | `scripts/verify-dread.ps1` builds MCP npm package |
| 1 | Game running + `dread_ping` / `dread_verify` |
| 2 | Log patterns after session |

Destructive tools: `dread_trigger_test_crash`, `dread_force_psychotic_break` (document in PR if used).
