# ADR-0013: TCP Debug Server for AI-Assisted Debugging

**Date:** 2026-05-24
**Status:** Accepted

---

## Context

The Dread mod has grown to 7 MonoBehaviour systems with complex interactions (Harmony patches, game state tracking, tension calculations, audio management). Debugging issues requires understanding the live state of these systems at runtime, which is currently limited to:

1. **BepInEx log output** (one-way, unstructured text).
2. **Attaching a debugger** (requires replacing mono.dll, impractical for end users and CI).
3. **In-game tools** (UnityExplorer, Imperium) -- manual, not automatable.

We needed a way for an external process (AI agent, CI pipeline, developer tool) to inspect and control the mod at runtime without restarting the game or attaching a debugger.

Research confirmed that an embedded TCP server on localhost (using `System.Net.Sockets`, which is fully available in net48/Unity Mono) is architecturally sound. The approach uses the same thread-safe queuing pattern already proven in `ErrorReporterSystem` (background thread producer -> locked queue -> `Update()` drain on main thread).

### Requirements

- Bidirectional JSON command/response protocol.
- No new dependencies (no NuGet packages, no additional DLLs).
- Zero game modifications -- the mod is loaded through normal r2modman/BepInEx path.
- Default-off (disabled in release builds).
- Safe for use during active gameplay (main-thread dispatch for all Unity API calls).
- AI-agent-verifiable: the agent can launch the game, connect, run commands, and kill the process autonomously.

---

## Decision

Add a **`DebugServerSystem`** MonoBehaviour that hosts a TCP server on `127.0.0.1` (loopback only). Communication uses newline-delimited JSON with a standard request/response envelope.

### Architecture

```
┌──────────────────────────────────┐     TCP/JSON      ┌──────────────────────────┐
│  Unity Game Process              │  127.0.0.1:PORT   │  AI Agent / Dev Tool     │
│                                  │ ◄────────────────►│                          │
│  ┌──────────────────────────┐   │                    │  Commands:               │
│  │ DebugServerSystem        │   │                    │  - ping                  │
│  │  ┌───────────────────┐   │   │                    │  - get_state             │
│  │  │ Background Thread  │───┼───┼──►                │  - get_config            │
│  │  │ (TcpListener)      │   │   │                    │  - set_config            │
│  │  └────────┬──────────┘   │   │                    │  - get_patches           │
│  │           │ Queue<Cmd>   │   │                    │  - get_logs              │
│  │  ┌────────▼──────────┐   │   │                    │  - shutdown              │
│  │  │ Update() drain     │   │   │                    └──────────────────────────┘
│  │  │ (main thread)      │   │   │
│  │  └────────┬──────────┘   │   │
│  │           │              │   │
│  │  ┌────────▼──────────┐   │   │
│  │  │ Mod Systems       │   │   │
│  │  │ Harmony API       │   │   │
│  │  │ Unity API         │   │   │
│  │  └───────────────────┘   │   │
│  └──────────────────────────┘   │
└──────────────────────────────────┘
```

### Protocol

**Request envelope:**
```json
{"id":1,"cmd":"get_state","data":{}}
```

**Success response:**
```json
{"id":1,"ok":true,"data":{...}}
```

**Error response:**
```json
{"id":1,"ok":false,"error":"message","code":-1}
```

Error codes: `-1` generic, `-2` unknown command, `-3` invalid parameters.

Newline-delimited JSON. Each command is a single line terminated by `\n`. Responses also end with `\n`.

### Commands (v1)

| Command | Purpose | Safe from main menu? |
|---------|---------|---------------------|
| `ping` | Liveness check | Yes |
| `get_state` | Full mod + game state snapshot | No (null-guarded) |
| `get_config` | All config entries | Yes |
| `set_config` | Modify a config entry | Yes |
| `get_patches` | Harmony patch info for all patched methods | Yes |
| `get_logs` | Buffered BepInEx log entries | Yes |
| `shutdown` | Graceful server shutdown | Yes |

Deferred to v2: `inspect` (reflection-based object inspection), `trigger_event` (invoke mod events), `list_objects` (scene enumeration), `subscribe` (event push channel).

### Security

- **Loopback only** (`IPAddress.Loopback`, port configurable via `ConfigEntry<int>`).
- **Default disabled** (`DebugServerEnabled` defaults to `false`).
- **Single connection at a time** -- new connection while active is accepted once the previous closes.
- **4096 byte max read buffer** -- larger payloads rejected.
- **5s read timeout** on the TCP stream.
- **10s command processing timeout** via `ManualResetEventSlim.Wait()`.
- **Port fallback** -- if the configured port is in use, fall back to configured + 1, log the result.

### Thread Safety

- Background thread calls `TcpListener.AcceptTcpClient()`, reads JSON, enqueues `DebugCommand` with a `ManualResetEventSlim`, and waits on it.
- `Update()` drains the entire queue each frame on the main thread, executes each command, sets response, and signals completion.
- Every dequeue is wrapped in try-catch-finally, guaranteeing `Done.Set()` fires even on handler exceptions.
- `OnDestroy()` sets a `_running` flag, calls `_listener.Stop()`, and joins the background thread (1s timeout).

### Log Listener

The system registers a custom `ILogListener` on BepInEx's `Logger.Listeners` to buffer recent log entries (up to 200) for the `get_logs` command. The listener unregisters in `OnDestroy()`.

### Why TCP (Not HTTP)

| Approach | Pros | Cons |
|----------|------|------|
| **TCP (this ADR)** | Zero dependencies, no admin required, full control over protocol, proven in BepInEx ecosystem (BepInEx.GUI uses same pattern) | Custom protocol (no browser testing) |
| HTTP (HttpListener) | Standard protocol, testable with curl | Requires http.sys, admin for non-localhost, more complex framing |

### Why ManualResetEventSlim (Not TaskCompletionSource)

| Approach | Pros | Cons |
|----------|------|------|
| **ManualResetEventSlim (this ADR)** | No Mono Task deadlock risk, matches existing `lock(Queue)` pattern in ErrorReporterSystem | Requires manual reset |
| TaskCompletionSource | Modern, async/await friendly | Continuations can run off main thread in Unity Mono, causing deadlocks |

---

## MCP Server Companion

The DebugServerSystem (C#, TCP) is paired with a TypeScript MCP server that bridges AI agent protocols to the raw TCP interface.

### dread-mcp-server (TypeScript)

- **Runtime:** Node.js (>=18) using `@modelcontextprotocol/sdk` with stdio transport
- **Role:** Sits on the other side of the TCP connection from `DebugServerSystem`, translating MCP tool calls (JSON-RPC over stdin/stdout) into TCP JSON commands
- **7 tools:** `dread_ping`, `dread_get_state`, `dread_get_config`, `dread_set_config`, `dread_get_patches`, `dread_get_logs`, `dread_shutdown`
- **Configuration** via environment variables: `DREAD_HOST` (default `127.0.0.1`), `DREAD_PORT` (default `15432`), `DREAD_TIMEOUT` (default `15000ms`)
- **Input handling:** Zod schema validation for each tool, strict mode to reject unknown fields
- **Response format:** `TextContent` with `"json"` or `"text"` format option
- **Error handling:** Socket errors, timeouts, and parse failures all surfaced as MCP error responses
- **No state:** Stateless per-request translation layer -- every tool call opens a fresh TCP connection, sends one command, reads one response, and closes

**Architecture:**

```
┌──────────────────────────────────────────────────────────────────┐
│  AI Agent                                                       │
│  (Claude, Cline, etc.)                                          │
└─────────────┬────────────────────────────────────────────────────┘
              │ MCP (JSON-RPC over stdin/stdout)
              ▼
┌──────────────────────────────────────────────────────────────────┐
│  dread-mcp-server (TypeScript, Node.js)                         │
│  @modelcontextprotocol/sdk                                      │
│  • Translates MCP tool calls → TCP JSON commands                │
│  • Stateless per-request translation                             │
└─────────────┬────────────────────────────────────────────────────┘
              │ TCP (newline-delimited JSON, 127.0.0.1:PORT)
              ▼
┌──────────────────────────────────────────────────────────────────┐
│  Unity Game Process                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ DebugServerSystem (C#)                                   │   │
│  │  ┌───────────────────┐  Queue<Cmd>  ┌───────────────┐   │   │
│  │  │ Background Thread │─────────────▶│ Update() drain │   │   │
│  │  │ (TcpListener)     │              │ (main thread)  │   │   │
│  │  └───────────────────┘              └───────┬───────┘   │   │
│  │                                              │           │   │
│  │                                     ┌────────▼────────┐  │   │
│  │                                     │ Dread Systems   │  │   │
│  │                                     │ Harmony API     │  │   │
│  │                                     │ Unity API       │  │   │
│  │                                     └─────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

### Why MCP (Not Direct TCP)

| Approach | Pros | Cons |
|----------|------|------|
| **MCP (this ADR)** | Standard AI agent protocol, tool discovery, schema validation | Requires Node.js runtime |
| **Direct TCP from agent** | No extra dependency | No schema, no discovery, every agent implements a custom protocol |

### Why stdio transport (Not HTTP)

- MCP stdio is designed for local daemon integration -- the MCP server is launched by the AI agent's MCP client, not as a standalone HTTP server
- Simpler lifecycle: starts with the agent, pipes stdin/stdout, shuts down when the agent exits

---

## Consequences

- **Positive:** AI agents can fully inspect and control the mod at runtime via a structured protocol.
- **Positive:** The TCP server is testable autonomously -- launch game, connect, run command suite, kill process.
- **Positive:** Zero new dependencies. All APIs (`System.Net.Sockets`, `System.Threading`, `BepInEx.Logging`, `HarmonyLib`) are already available.
- **Positive:** Uses the same proven patterns as `ErrorReporterSystem` (thread-safe queue, main-thread dispatch).
- **Positive:** Tightly scoped v1 (7 commands) can be extended without breaking changes.
- **Negative:** Debug server is local-process-wide open -- any process on the same machine can connect. Mitigated by default-off and loopback binding.
- **Negative:** Adds ~180 lines of C# code and one more system to the system host init.
- **Negative:** Port conflicts with other software require fallback logic.
- **Negative:** Not useful for end users; strictly a development tool.

---

## Rejected Alternatives

- **HttpListener:** More complex framing, http.sys dependency, no advantage over raw TCP for this use case.
- **Named Pipes:** Spotty support in Unity Mono (`NotImplementedException` on some Mono versions).
- **Config-driven debugging only:** One-way communication (AI writes config, mod acts, AI reads log). No structured responses, no command correlation.
- **File-based queue (state.json):** Polling overhead, no real-time command/response, race conditions on file write.
- **Embedded REPL (Mono.CSharp):** High risk, requires shipping a C# compiler, massive security surface.
