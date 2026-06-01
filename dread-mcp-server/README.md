# dread-mcp-server

MCP (Model Context Protocol) stdio bridge for the Dread mod **debug server** (`DebugServerSystem`). Agents use it during Tier 1 verify with a running game and a **development** `Dread.dll` (`DREAD_DEBUG`).

**Agent docs:** [docs/agents/guides/debug-tooling.md](../docs/agents/guides/debug-tooling.md), [docs/agents/verify-dread.md](../docs/agents/verify-dread.md).

## Prerequisites

- Node.js 18+
- R.E.P.O. running with Dread built using debug features (`dotnet build -c Debug` or `build.ps1 -DebugBuild`)
- `DebugServerEnabled` true in cfg (default on in development builds)

## Build

```bash
cd dread-mcp-server
npm ci
npm run build
```

Output: `dist/index.js`. Cursor loads this via `.cursor/mcp.json`.

## Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `DREAD_HOST` | `127.0.0.1` | Game machine (use host IP if MCP runs outside the game VM) |
| `DREAD_PORT` | `15432` | TCP port from `DebugServerPort` |
| `DREAD_TIMEOUT` | `15000` | Command timeout ms (minimum 100) |

## Tools

Tool names map 1:1 to TCP commands (`dread_ping`, `dread_verify`, `dread_get_runtime_state`, `dread_set_config`, `dread_get_patches`, `dread_get_logs`, `dread_trigger_test_crash`, `dread_force_psychotic_break`, etc.). Inspect `src/index.ts` or run the server with MCP inspector after build.

## Production builds

Thunderstore/CD **Release** DLLs exclude the debug server. This package is for agent workflows only.
