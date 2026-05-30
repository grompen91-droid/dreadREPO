# Contract: Error reporting privacy copy

**Feature**: ERR-3 | **Consumers**: players (via cfg), ERR-2 prompt, agents

## Normative rules

1. All player-facing disclosure for error reporting MUST use strings from the canonical module (`ErrorReportingPrivacyCopy` or name chosen in implementation).
2. Copy MUST NOT claim collection of account names, Steam IDs, voice/chat, or files outside Unity log context.
3. Copy MUST state reports go to the developer via an automated service (Worker) and may create public GitHub issues labeled auto-reported.
4. Copy MUST include disable instructions referencing `ErrorReportingEnabled` and `BepInEx/config/elytraking.dread.cfg`.
5. Copy MUST describe behavior when enabled (data sent on `Exception` / `Error` logs per pipeline), including default-on wording after ERR-2.

## Required bullets (canonical)

Implementers MUST include equivalents of:

| # | Bullet | Code / doc reference |
|---|--------|----------------------|
| 1 | Exception type, message (length-capped), stack trace (length-capped), dedupe hash | `ErrorReporterSystem`, `MaxMessageLength`, `MaxStackTraceLength` |
| 2 | Active scene name and session play time | `GameStateData.SceneName`, `PlayTimeSeconds` |
| 3 | Enemy counts (alive, total, nearby) | `CaptureGameState` |
| 4 | Player HP, stamina, world position (when available) | `CaptureGameState` player block |
| 5 | OS, CPU, RAM, GPU, device model; may include VRAM, driver, shader level | `CaptureSystemInfoSafe` |
| 6 | Screen resolution, refresh, DPI, fullscreen mode | `CaptureDisplayInfoSafe` |
| 7 | Eleven named Dread settings (toggles plus audio frequency/volume) | `CaptureConfigSafe` / `ConfigData` |
| 8 | Not sent: your username, Steam profile, or deliberate PII | Product statement; verify no such fields in `ErrorReportTypes` |
| 9 | Disable: `ErrorReportingEnabled = false` in section 5 | `DreadConfig.cs` bind |
| 10 | Default on for new installs; turn off via first-run prompt or cfg | `ErrorReportingEnabled` default `true` (ERR-2); existing saved `false` retained on upgrade |

## Review checklist (pre-merge)

- [x] Read `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` and ADR-0010 payload table side by side with final strings.
- [x] Run `dotnet test tests/Dread.ErrorReportJson.Tests` (payload shape unchanged).
- [x] Confirm `ErrorReportLogQueue` returns immediately when `ErrorReportingEnabled` is false.
- [x] If README/THUNDERSTORE touched, pipeline description matches `Application.logMessageReceived` (not Harmony on Debug.Log).
- [x] No em dash in any edited markdown file.
- [x] ERR-2 default-on copy and first-run prompt implemented per issue #172 (separate feature branch).

## ERR-2 integration

ERR-2 first-run prompt (`ErrorReportingPromptSystem`) MUST:

- Import canonical `ShortSummary` + `DataBullets` + `DisableInstructions` (no paraphrase of payload categories).
- Set `ErrorReportingPromptShown` on dismiss; set `ErrorReportingEnabled` per button choice.
- Gate enqueue/send via `ErrorReportingConsent` until `ErrorReportingPromptShown` is true.

## Example shape (non-normative wording)

```
Anonymous error reporting (opt-in, default off).
When enabled, serious game errors may be sent to the developer to fix bugs.
May include: exception details; scene and gameplay stats (enemy counts, HP, position);
PC and GPU info (may include VRAM and driver); screen settings; eleven named Dread settings.
No account name or Steam ID.
To disable: set ErrorReportingEnabled = false in BepInEx/config/elytraking.dread.cfg
(section 11. Error Reporting), or the same toggle in REPOConfig when listed.
```

Final wording is implementation detail; checklist rows are normative.
