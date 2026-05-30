# Contract: ERR-2 config keys

**Feature**: ERR-2 | **File**: `BepInEx/config/elytraking.dread.cfg` section `7. Error Reporting`

## Keys

| Key | Type | Default (ERR-2) | Set by |
|-----|------|-----------------|--------|
| `ErrorReportingEnabled` | bool | `true` | Prompt buttons, player cfg/REPOConfig |
| `ErrorReportingPromptShown` | bool | `false` | Prompt buttons only (both set true) |

## Descriptions

- `ErrorReportingEnabled`: Existing `FullDescription` from ERR-3 (updated for default-on in ERR-2).
- `ErrorReportingPromptShown`: Short description: "Internal: set true after first-run error reporting disclosure. Do not edit unless resetting the prompt."

## Out of scope for ERR-2

| Change | Owner |
|--------|-------|
| Worker URL, batch sizes | ERR-1 / ADR-0010 |
| New telemetry fields | Future |
| `CompatibilityMode` behavior | ADR-0009 |

## REPOConfig

- Toggle for `ErrorReportingEnabled` only (no prompt API).
- Prompt is IMGUI-only per [first-run-prompt.md](./first-run-prompt.md).
