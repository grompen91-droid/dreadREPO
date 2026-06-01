# ADR-0012: Test Crash Button for Error Telemetry Verification

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

ADR-0010 (Error Telemetry) added an automated crash reporting system, but there was no safe, repeatable way to verify it works end-to-end. During development, errors had to be injected temporarily in source code and reverted after testing. This made regression testing impractical and meant the error pipeline could silently break after a deploy without anyone noticing.

We needed a way to:

1. Verify the error capture path (log hook + buffer) works at runtime.
2. Verify the Cloudflare Worker receives and processes a report.
3. Verify a GitHub Issue is created with the correct format.
4. Test config changes (e.g., opting out) actually suppress reporting.
5. Have a non-developer (playtester, user) trigger the flow without access to source code.

Approaches considered: keybind, config bool toggle, in-game UI button, ConfigurationManager button.

---

## Decision

Add a **clickable button** in the BepInEx ConfigurationManager UI that crashes the game on demand.

### Implementation

- **Config entry:** `DreadConfig.TestCrashButton` (type `bool`, default `false`), bound under section "7. Testing" with `ConfigurationManagerAttributes { ShowAsButton = true }`.
- **No hard dependency on ConfigurationManager:** The `ShowAsButton` attribute is read via reflection. If ConfigurationManager is not installed, the entry is a plain editable field.
- **TestCrashSystem (MonoBehaviour):** Subscribes to `SettingChanged` on `DreadConfig.TestCrashButton`. When the user clicks the button:
  1. Resets the config value to `false` immediately.
  2. Queues a deferred crash on the next `Update()` (avoids BepInEx swallowing exceptions inside `SettingChanged`).
  3. Logs `InvalidOperationException` with a clearly identifiable `[Dread TestCrash]` message.
  4. Runs `ErrorReporterSystem.ReportTestCrashAndWait()` (synchronous HTTP POST via `ErrorReportJson`) so the report completes before the process exits.
  5. Calls `Process.Kill()` on the game process (skipped in the Unity Editor).
- **Production errors** still use the ADR-0010 log hook, buffer, and async `UnityWebRequest` batch flush. TestCrash is intentionally a separate, synchronous path for verification.
- Each TestCrash click uses a unique hash suffix (`|testcrash|` + `DateTime.UtcNow.Ticks`) so repeated button tests create **distinct** GitHub issues. Do not use TestCrash to verify Worker deduplication (see error-reporting checklist section F).
- JSON body is built with `ErrorReportJson` (ADR-0015), not `JsonUtility`.

### Why a Button (Not a Keybind or Toggle)

| Approach | Pros | Cons |
|----------|------|------|
| **Config button (this ADR)** | Discoverable in config UI, no restart, no keybind to remember, works with or without ConfigurationManager | Requires ConfigurationManager for click-to-crash UX |
| **Keyboard shortcut** | Works without ConfigurationManager | Not discoverable, keybind collision risk, requires remembering a key combo |
| **Config bool (restart)** | Trivial to implement | Requires restarting the game after toggling -- defeats the purpose of quick testing |

---

## Consequences

- **Positive:** Error telemetry can be verified end-to-end in seconds by any user with ConfigurationManager installed.
- **Positive:** The crash message includes `[Dread TestCrash]` for easy identification in logs and GitHub Issues.
- **Positive:** No build-time dependencies added. ConfigurationManager is optional.
- **Negative:** Without ConfigurationManager, the "button" appears as a true/false toggle. Users must set it to `true` and wait one frame for the crash.
- **Negative:** The crash is a real unhandled exception -- it cannot be caught. The game terminates immediately. This is intentional for testing but may surprise users who click without understanding the description.
- **Negative:** Adds one more system to the system host init in `Plugin.cs` (now 6 systems total).

---

## Rejected Alternatives

- **Keybind-only:** Not discoverable. The whole point is making it easy for testers to trigger.
- **In-game GUI button:** Requires rendering a UI overlay for a dev-only feature. Unnecessary complexity.
- **Console command / dev menu:** No existing dev console in the mod. Creating one for one command is overkill.
- **Temporary code injection (edit source, test, revert):** The prior approach. Error prone and easy to forget to revert.

---

## Related ADRs

- [ADR-0010](0010-error-telemetry.md): Production log hook, batch flush, Worker contract
- [ADR-0015](0015-error-report-json-serialization.md): Payload serialization

---

## Amendment (012, 2026-06-01)

**Release builds** exclude `TestCrashSystem` and section **11. Testing** at compile time (`DREAD_DEBUG` / production MSBuild profile), same as debug overlay and TCP server. Development builds retain the test-crash button. Production error reporting for real exceptions (ADR-0010) is unchanged. Production **Logging** binds under section **8** via `DreadConfigSections`.
