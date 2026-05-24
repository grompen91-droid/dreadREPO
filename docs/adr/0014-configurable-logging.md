# ADR-0014: Configurable Logging Service

**Date:** 2026-05-24
**Status:** Accepted

---

## Context

Early development of Dread used raw `Plugin.Logger.Log*` calls directly throughout the codebase. After the mod grew to 7 MonoBehaviour systems with ~110 logging calls, several problems became apparent:

1. **No runtime verbosity control.** Every `LogDebug` and `LogInfo` call wrote to the BepInEx console unconditionally, flooding the log with trace noise during normal play.
2. **No way to suppress output.** Players who found the console spam distracting had no option to reduce it short of disabling BepInEx console entirely.
3. **No grep-friendly structure.** Debug trace calls (`entering method X`, `exiting method Y`) mixed with diagnostic info (`config value Z = 42`) under the same BepInEx log level, making post-mortem analysis harder.
4. **Config deprecation risk.** Without a logging abstraction, migrating config entries or changing the logging backend would require touching every call site.

We needed a lightweight logging abstraction that supports runtime level gating, grep-friendly verbosity tiers, and zero allocation at suppressed levels -- all without introducing new dependencies.

Research confirmed that a static service class with integer-compared `LogLevel` gating adds negligible overhead (single `int` comparison per call) and integrates cleanly with BepInEx's `SettingChanged` event for runtime reconfiguration.

### Requirements

- Runtime log level changes via BepInEx config UI (no restart).
- Four tiers: None (suppress all), Error, Debug, Verbose.
- Verbose traces clearly distinct from diagnostic Debug output.
- Zero new NuGet packages or DLLs.
- All existing `Plugin.Logger.Log*` calls migrated to `LoggingService.Log*`.
- Backward compatible -- existing log output at Debug level unchanged.
- Configurable through the standard `BepInEx.cfg` or r2modman UI.

---

## Decision

Add a **`LoggingService`** static class with level-gated methods mirroring BepInEx's `ManualLogSource` API, controlled by a `LogLevel` enum bound to a `ConfigEntry<LogLevel>`.

### Architecture

```
Config UI (BepInEx)
  |
  | SettingChanged event
  v
DreadConfig.LogLevelEntry (ConfigEntry<LogLevel>)
  |
  | On change: LoggingService.SetLevel(value)
  v
LoggingService (static)
  |
  |-- LogLevel _current  (compared per-call)
  |
  |-- LogError(message)   --> _current >= None?   --> Plugin.Logger.LogError
  |-- LogWarning(message) --> _current >= Debug?  --> Plugin.Logger.LogWarning
  |-- LogInfo(message)    --> _current >= Debug?  --> Plugin.Logger.LogInfo
  |-- LogDebug(message)   --> _current >= Debug?  --> Plugin.Logger.LogDebug
  |-- LogVerbose(message) --> _current >= Verbose?--> Plugin.Logger.LogInfo("[V] " + message)
  |
  |-- PrintAsciiArt()     --> Plugin.Logger.LogInfo (unconditional)
  |
  v
BepInEx Console / Log File
```

### Level Gating Logic

```
LogLevel:  None=0  Error=1  Debug=2  Verbose=3

Call              Suppressed when _current is:
LogError          None
LogWarning        None, Error
LogInfo           None, Error
LogDebug          None, Error
LogVerbose        None, Error, Debug
```

At the default level (`Debug`), all calls except `LogVerbose` produce output. At `Error`, only `LogError` produces output. At `None`, everything is suppressed. At `Verbose`, every call produces output.

### Level semantics

| Level | When to use | Example |
|-------|-------------|---------|
| `None` | All mod output suppressed | Player wants no console noise |
| `Error` | Recoverable/partial failures only | `"Failed to load audio clip: {path}"` |
| `Debug` | Useful diagnostic information | `"Config value Z set to 42"`, `"Systems initialized (7)"` |
| `Verbose` | Entry/exit tracing, inner-loop diagnostics | `"ApplyPatch entering..."`, `"Update cycle took 3ms"` |

### Config Entry

```toml
[9. Logging]
## Logging verbosity. None = suppress all output, Error = only errors,
## Debug = info + warnings + errors, Verbose = everything including debug traces.
# Setting type: Dread.Systems.LogLevel
# Default value: Debug
LogLevel = Debug
```

The `LogLevel` enum is defined in `Dread.Systems` and BepInEx's `TomlTypeConverter` serializes it by name, making the config file human-readable (`None`, `Error`, `Debug`, `Verbose`) rather than numeric.

### Runtime Reconfiguration

In `Plugin.Awake()`:

1. `LoggingService.Initialize(DreadConfig.LogLevelEntry.Value)` sets initial level.
2. `DreadConfig.LogLevelEntry.SettingChanged += handler` subscribes to changes.
3. Handler calls `LoggingService.SetLevel(DreadConfig.LogLevelEntry.Value)`.

This means the player can change the log level at any time through the BepInEx Configuration Manager (F1) without restarting the game. The handler is unsubscribed in `Plugin.OnDestroy()` to avoid dangling event references.

### Verbose Prefixing

`LogVerbose` prepends `[V]` to the message string:

```csharp
Plugin.Logger.LogInfo($"[V] {message}");
```

This allows grep-based filtering on the BepInEx console and log file:

```bash
# Show only verbose traces
grep "\[V\]" BepInEx/LogOutput.log

# Show everything except verbose traces
grep -v "\[V\]" BepInEx/LogOutput.log
```

The prefix is baked into the method rather than relying on callers to remember the convention. This is a deliberate design choice: `LogVerbose` is the only method that transforms the message, so there is exactly one place to change the grep convention.

### ASCII Art

`PrintAsciiArt()` renders the "DREAD" logo in Unicode block characters on mod injection. It is called unconditionally in `Plugin.Awake()` (after `LoggingService.Initialize`) and uses `Plugin.Logger.LogInfo` directly rather than `LoggingService.LogInfo`, ensuring it always displays regardless of the current log level. This provides visual confirmation that the mod loaded successfully.

```
         ███             ███             ███             ███
       ███░            ███░            ███░            ███░
     ███░            ███░            ███░            ███░
   ███░            ███░            ███░            ███░
... (28 lines)
```

### No Abstractions, No Interfaces

`LoggingService` is a static class, not an interface-based service. This is intentional:

- The mod has one logging backend (BepInEx `ManualLogSource`).
- There is no use case for swapping implementations at runtime or in tests.
- Static methods eliminate injection overhead and make call sites trivial (`LoggingService.LogDebug(...)`).
- The zero-overhead pattern is already proven: the `_current >= level` comparison is inlined by the JIT and costs a single `int` compare.

### Why Gated Methods (Not a `Log(LogLevel, string)` Signature)

An alternative design would be a single `Log(LogLevel level, string message)` method. The chosen design instead has one method per level:

```csharp
// Chosen: level-gated methods
LogDebug("Something happened");     // one branch: _current >= Debug
LogVerbose("Entering method X");     // one branch: _current >= Verbose

// Rejected: single method
Log(LogLevel.Debug, "Something happened");     // must pass level every call
Log(LogLevel.Verbose, "Entering method X");    // call sites are noisier
```

Pros of the chosen approach:

- **Call site brevity.** `LogDebug("msg")` is shorter than `Log(LogLevel.Debug, "msg")`. Over ~110 call sites, this matters.
- **Self-documenting intent.** `LogError(...)` clearly signals severity without looking up the enum.
- **Migration clarity.** `Plugin.Logger.LogDebug(...)` becomes `LoggingService.LogDebug(...)` -- a mechanical find-and-replace with no level argument to get wrong.
- **JIT de-virtualization.** Static methods are trivially inlined. The level comparison can be hoisted or eliminated entirely if the level is constant-folded.

---

## Consequences

- **Positive:** Players can suppress all mod output (`None`), see only errors (`Error`), see diagnostics (`Debug`), or enable full tracing (`Verbose`) without restarting.
- **Positive:** ~110 logging calls across 9 C# systems migrated from raw `Plugin.Logger.Log*` to `LoggingService.Log*`, making backend changes a single-file edit.
- **Positive:** `[V]` prefix makes verbose traces trivially grep-able in both console and log file.
- **Positive:** Runtime level changes via BepInEx Configuration Manager (F1 key) -- no restart required.
- **Positive:** Zero new dependencies. All APIs (`BepInEx.Logging`, `System.Text.StringBuilder`) are already available.
- **Positive:** Static class design avoids DI framework overhead in a mod that has no DI container.
- **Positive:** ASCII art provides immediate visual confirmation of mod load regardless of log level.
- **Negative:** Adds ~100 lines of C# to the project.
- **Negative:** The gated-method design creates 5 method entry points instead of 1, though each is trivial (single comparison + delegate call).
- **Negative:** `LogVerbose` uses `LogInfo` under the hood (since BepInEx has no `LogVerbose` level), so BepInEx log level filtering cannot distinguish verbose messages from info messages. The `[V]` prefix compensates for this at the text level.

---

## Rejected Alternatives

- **Single `Log(LogLevel, string)` signature:** Noisier call sites, no mechanical migration path from `Plugin.Logger.Log*`, and the level enum value is redundant when the method name already encodes the level.
- **Instance-based service (`ILoggingService` interface):** Over-engineered for a mod with exactly one logging backend. The static class provides identical testability (call assertions on `Plugin.Logger` via the same BepInEx `ManualLogSource`).
- **Conditional compilation (`#if DEBUG` / `[Conditional("VERBOSE")]`):** Requires recompilation to change verbosity. Defeats the runtime reconfiguration requirement.
- **BepInEx log level filtering only:** BepInEx's built-in `LogSource` filtering applies at the sink level and cannot distinguish between log sources in the same mod. A separate gating layer gives per-mod control.
- **No logging abstraction (status quo):** Unconditional log writes at all levels. Floods the console during normal play with no runtime suppression option.
