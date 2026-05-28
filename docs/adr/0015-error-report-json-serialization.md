# ADR-0015: Manual JSON Serialization for Error Reports

**Date:** 2026-05-28
**Status:** Accepted

---

## Context

ADR-0010 defined the error telemetry payload shape and Cloudflare Worker contract. The mod originally serialized payloads with Unity `JsonUtility.ToJson()` on `ErrorPayload` / `ErrorReport` DTOs.

During ERR-1 (TestCrash end-to-end verification), runtime evidence showed `JsonUtility` produced JSON containing only top-level metadata:

```json
{"ModVersion":"1.5.2","GameVersion":"0.1","UnityVersion":"2022.3.67f2"}
```

The `Reports` array was omitted entirely (payload length 71 bytes), so the Worker never received report bodies and GitHub issues were not created from the game client.

Moving DTOs from nested private classes to top-level types in `ErrorReportTypes.cs` did not fix the issue. The Worker smoke script (`scripts/test-error-reporter.sh`) and Vitest suite already used hand-written JSON with a populated `Reports` array, which continued to work.

---

## Decision

Replace `JsonUtility` for error payloads with a dedicated manual serializer:

| Component | Role |
|-----------|------|
| `Systems/ErrorReportTypes.cs` | `[Serializable]` DTOs (`ErrorPayload`, `ErrorReport`, nested state objects) |
| `Systems/ErrorReportJson.cs` | `SerializePayload()` builds JSON via `StringBuilder` with invariant-culture numbers and escaped strings |
| `tests/Dread.ErrorReportJson.Tests/` | xUnit golden tests (CI: `dotnet test` after mod build) |

Both production batch flush (`SendBatch` / `UnityWebRequest`) and TestCrash sync POST (`TryPostPayloadSync` / `HttpWebRequest`, ADR-0012) call `ErrorReportJson.SerializePayload()`.

Validation before POST: payload must contain `"Reports":[` (non-empty array allowed; empty array is valid JSON).

### Send failure handling

If batch serialization fails or `UnityWebRequest` fails, `RequeueFailedBatch()` appends the batch back to `_buffer` and logs a warning. `FlushNow()` skips starting a new send while `_sendInProgress` is true.

### Safe capture helpers

`CaptureSystemInfoSafe()`, `CaptureDisplayInfoSafe()`, and `CaptureConfigSafe()` wrap Unity / config access in `TrySet()` so stub mismatches or optional APIs do not abort report building. TestCrash and production paths share these helpers.

---

## Consequences

- **Positive:** Payload matches Worker contract reliably; TestCrash and production use one code path for JSON shape.
- **Positive:** Unit tests catch schema regressions without launching the game.
- **Positive:** Re-queue reduces silent loss of batched reports on transient network failure.
- **Negative:** Manual serializer must be updated when Worker payload fields change (mitigated by tests and `test-error-reporter.sh`).
- **Negative:** No automatic serialization of new DTO fields (unlike reflection-based libraries).

---

## Rejected Alternatives

- **Unity JsonUtility only:** Proven broken for `Reports[]` in this project at runtime.
- **Bundled Newtonsoft.Json:** Extra dependency and packaging size for a single POST body; manual JSON is small and stable.
- **System.Text.Json / source generators:** Not available on net48 / Unity Mono without additional dependencies.

---

## References

- ADR-0010: Error telemetry architecture and Worker contract
- ADR-0012: TestCrash sync POST path (must complete before `Process.Kill()`)
- `scripts/test-error-reporter.sh`: live Worker smoke test
- `workers/error-reporter/test/`: Worker Vitest suite
