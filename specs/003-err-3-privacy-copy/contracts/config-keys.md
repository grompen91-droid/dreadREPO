# Contract: Error reporting config keys (ERR-3)

**Feature**: ERR-3 | **Scope**: disclosure references only (no new keys)

## ErrorReportingEnabled

| Property | Value |
|----------|--------|
| Section | `5. Error Reporting` |
| Key | `ErrorReportingEnabled` |
| Type | `bool` |
| Default (ERR-3) | `false` |
| C# | `DreadConfig.ErrorReportingEnabled` |
| File | `BepInEx/config/elytraking.dread.cfg` |

**Behavior when false**: `ErrorReportLogQueue.EnqueueLog` returns without enqueueing; no POST to Worker.

**Behavior when true**: `Application.logMessageReceived` pipeline may batch and POST per ADR-0010.

**Disclosure**: Full privacy text in `ConfigDescription` per [privacy-copy.md](./privacy-copy.md).

## Related keys (mentioned in config snapshot only)

When reporting is enabled, payload `Config` object may include other Dread toggles (audio, aggression, tension, etc.). ERR-3 copy MUST say "Dread mod settings" or enumerate "feature toggles" without implying non-reporting keys are transmitted when reporting is off.

## Out of scope for ERR-3

| Key | Note |
|-----|------|
| Default change to `true` | ERR-2 |
| `TestCrash` / section 7 | ADR-0012; separate from privacy bullets unless test path mentioned as dev-only |
