# Error reporting

Opt-in telemetry from game to Cloudflare Worker to GitHub issues. Default **off** (ADR-0010). Code: `Systems/ErrorReporting/` (`ErrorReporterSystem.cs`, `ErrorReportLogQueue.cs`, `ErrorReportPayloadCapture.cs`, `ErrorReportUploader.cs`), `Systems/ErrorReportJson.cs`, `workers/error-reporter/`.

**Privacy copy (ERR-3):** canonical strings in `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs`; review checklist and required bullets in [specs/003-err-3-privacy-copy/contracts/privacy-copy.md](../../../specs/003-err-3-privacy-copy/contracts/privacy-copy.md).

## Pipeline

```
Unity Application.logMessageReceived (Exception/Error)
  -> enqueue + dedupe (60s per hash)
  -> batch flush (5 min or buffer full)
  -> POST JSON via `HttpWebRequest` (`TryPostPayloadSync`; main thread, see roadmap ERR-4)
  -> Worker
  -> Worker creates/updates GitHub issue (label auto-reported)
```

Test path: `TestCrashSystem` or MCP `trigger_test_crash` (see [debug-tooling.md](debug-tooling.md)).

## Config

| Key | Section | Default |
|-----|---------|---------|
| `ErrorReportingEnabled` | `7. Error Reporting` | **false** (opt-in) |

When off, `EnqueueLog` returns immediately.

## Spam and dedupe

Ignored in pipeline:

- `[Dread TestCrash]` / `TestCrashSystem` (sync test still sends via dedicated path)
- `DebugConsoleUI` noise (MenuLib/REPOConfig broken hooks)

Dedupe: SHA-based hash prefix, 60s cooldown per hash. Buffer caps: pending logs, batch size, stack/message length trims.

## Payload

Serialized by `ErrorReportJson.SerializePayload()` (ADR-0015). Includes:

- Exception message/stack
- System/display info
- Game state tables
- Config snapshot (eleven named `DreadConfig` fields: toggles plus `AudioFrequency` and `AudioVolume`; see `ErrorReportingPrivacyCopy`)
- Raw JSON in issue body for debugging

## Worker

| Path | Role |
|------|------|
| `workers/error-reporter/` | Cloudflare Worker (Vitest in CI) |
| Production URL | `https://dread-error-reporter.nox-heights.workers.dev/api/report` |

CI: `cd workers/error-reporter && npm test`

Live smoke: `scripts/test-error-reporter.sh`

## Automated tests

| Suite | Command |
|-------|---------|
| Mod JSON golden | `dotnet test tests/Dread.ErrorReportJson.Tests/` |
| Worker integration | `npm test` in `workers/error-reporter/` |

## Manual matrix

Full checklist: [docs/agents/error-reporting-test-checklist.md](../error-reporting-test-checklist.md) (ERR-1).

## Agents: common tasks

| Task | Touch |
|------|-------|
| New config in report | `ErrorReporterSystem` config snapshot + `ErrorReportTypes` |
| Payload field | `ErrorReportJson.cs` + golden tests |
| Worker validation | `workers/error-reporter` handlers |
| Default opt-in change | Requires ADR + changelog; product decision |

Never enable reporting by default without explicit issue/ADR approval.

## ERR-2 first-run prompt (future)

Issue [#172](https://github.com/grompen91-droid/dreadREPO/issues/172) owns default-on and first-run UI. When implementing:

- Import `ErrorReportingPrivacyCopy.ShortSummary`, `DataBullets`, and `DisableInstructions` (do not rewrite disclosure).
- Modal body should follow [privacy-copy.md](../../../specs/003-err-3-privacy-copy/contracts/privacy-copy.md) checklist rows 1-10.
- Persist choice only via `ErrorReportingEnabled` in section `7. Error Reporting`.

## ADRs

- `docs/adr/0010-error-telemetry.md`
- `docs/adr/0012-test-crash-button.md`
- `docs/adr/0015-error-report-json-serialization.md`
