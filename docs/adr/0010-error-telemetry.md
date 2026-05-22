# ADR-0010: Automated Error Telemetry via Cloudflare Worker

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

Dread had zero error handling infrastructure beyond BepInEx console logging. There was no `try/catch` anywhere in the codebase, no telemetry, and no way for the developer to know when users hit crashes in the wild. Bugs were reported manually through Discord or Thunderstore comments (if at all), giving the developer very low signal on what was actually breaking.

Several questions drove the design:

1. How do we collect errors without requiring a server we manage?
2. How do we avoid overwhelming GitHub Issues with duplicates?
3. How do we respect player privacy and keep the feature opt-out?
4. How do we make the reports useful for triage without the developer needing to ask for system specs every time?

---

## Decision

Add an `ErrorReporterSystem` MonoBehaviour that hooks Unity's `Application.logMessageReceivedThreaded` and sends error reports to a Cloudflare Worker. The Worker acts as a proxy that creates GitHub Issues on the repo.

### Architecture

```
Game (Unity/BepInEx)
  |-- ErrorReporterSystem (C# MonoBehaviour)
  |     |-- Hooks logMessageReceivedThreaded
  |     |-- Buffers errors (max 50, flushed every 300s)
  |     |-- POSTs JSON to Cloudflare Worker
  |
  v
Cloudflare Worker (JavaScript)
  |-- Rate limits: 5 POSTs/hr per IP (in-memory Map)
  |-- Deduplicates: searches GitHub Issues for <!-- hash:<hash> -->
  |-- Creates/updates issues via GitHub API
  |-- Re-opens closed duplicates
  |-- Limits comments: 5/hr per issue
  |
  v
GitHub Issues
  |-- Label: auto-reported, bug
  |-- Title: [auto] <ExceptionType> in <Scene>
  |-- Body: formatted tables + raw JSON
```

### Thread Safety (Key Design Detail)

`Application.logMessageReceivedThreaded` fires on an arbitrary thread. The initial implementation crashed by calling Unity APIs from this thread. The fix uses a two-phase approach:

1. **Background thread:** `HandleLog` enqueues only raw strings (`RawLogEntry { logString, stackTrace, type }`) into a `Queue` with a 100-entry cap. No Unity API calls.
2. **Main thread:** `Update()` dequeues and calls `CaptureGameState()` (which uses `FindObjectsOfType`, `SceneManager`, etc.) and `FlushNow()`.

The transmit buffer uses `lock (_buffer)` for thread-safe swap, and `StartCoroutine` for `UnityWebRequest` is only called from the main thread.

### Payload Design

The JSON payload sent to the Worker includes:

| Category | Fields |
|----------|--------|
| Error | Hash (SHA256 prefix), type, exceptionType, message, stackTrace, timestamp |
| Game State | SceneName, enemiesAlive/total/nearby, playerHp/maxHp/stamina/position, playTimeSeconds |
| System Info | OS, CPU model/cores/frequency, RAM, GPU model/vendor/driver/VRAM, device type/model |
| Display | Resolution (WxH), refresh rate, DPI, fullscreen mode |
| Config | All 11 DreadConfig values |

### Worker Design

- **Edge location:** In-memory rate limiting is per-isolate (not global). Adequate for current scale; would need KV or Durable Objects for global limits at scale.
- **No database:** Deduplication uses GitHub's Search API to find existing issues by hash comment. The Search API is fast enough for low-volume error reporting.
- **Secrets:** GitHub PAT is stored as a Cloudflare Worker secret (`TOKEN`), set during deploy via `wrangler secret put`.

---

## Consequences

- **Positive:** Every crash creates a GitHub Issue with full context (system specs, game state, config). Developer can triage without user follow-up.
- **Positive:** Deduplication by error hash prevents issue spam for repeat crashes.
- **Positive:** Opt-out config toggle respects player privacy.
- **Positive:** Thread-safe design prevents the error reporter itself from crashing the game.
- **Negative:** Requires a Cloudflare account and Workers deployment. Adds operational dependency.
- **Negative:** In-memory rate limiting is per-isolate -- a user hitting different Cloudflare edges could bypass the 5/hr limit.
- **Negative:** GitHub API rate limits (5000 req/hr) and Search API limits apply. At very high error volumes, some reports may be silently dropped.
- **Negative:** The Worker URL is hardcoded in the mod DLL. If the Worker needs to be migrated, a mod update is required.

---

## Rejected Alternatives

- **Direct-to-GitHub from the mod:** Would expose the GitHub PAT in the DLL (trivially reverse-engineered from a BepInEx mod). The Cloudflare Worker hides the token.
- **Sentry/Application Insights:** Third-party services add cost and complexity. GitHub Issues is already the triage workflow. A Worker proxy keeps everything in one place.
- **File-based logging (write errors to disk):** Would require users to manually share log files. Low signal, high friction.
- **BepInEx log watcher (external process):** Requires shipping a separate executable. Adds packaging complexity and anti-virus concerns.
- **No telemetry (status quo):** Zero signal on production crashes. Bug fixes rely on user reports.
