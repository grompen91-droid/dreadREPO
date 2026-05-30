# Config and logging

How agents add settings and control log verbosity. Files: `Config/DreadConfig.cs`, `Systems/LoggingService.cs`.

## DreadConfig rules

| Rule | Why |
|------|-----|
| Never bump version strings in this file | CD pipeline owns `Plugin.VERSION` and manifest |
| Bind in `Initialize(ConfigFile cfg)` only | Called once from `Plugin.Awake` |
| Use numbered sections | Matches Configuration Manager order |
| Call `EnsureInitialized()` from hot paths if unsure | Guards early access |

### Section map

| Section | Entries |
|---------|---------|
| `1. Audio Dread` | `AudioEnabled`, `AudioFrequency`, `AudioVolume` |
| `2. Monster Overhaul` | `MonsterAggressionEnabled`, `MonsterAudioEnabled` |
| `3. Tension` | Fake footsteps, adrenaline, low stamina, panic sprint |
| `4. Psychotic Break` | enabled, chance, duration, once per match |
| `5. QOL` | `CrouchSpeedBoost` |
| `6. Compatibility` | Compatibility mode, skip conflicting patches, debug console guard |
| `7. Error Reporting` | `ErrorReportingEnabled` (default false) |
| `8. Debug Overlay` | `DebugOverlayEnabled` |
| `9. Debug Server` | enabled, port |
| `10. Logging` | `LogLevel` enum |
| `11. Testing` | `Crash Game` toggle (turn on to test crash; resets to off) |

### Adding a config entry

1. `public static ConfigEntry<T> YourEntry = null!;`
2. `cfg.Bind(...)` in `Initialize` with description
3. Add to `allFields` null-check array at bottom
4. If MCP-visible: extend `DebugServerSystem` config export and [verify-dread.md](../verify-dread.md) table
5. If error-report snapshot: extend `ErrorReporterSystem` config block
6. Respect **Compatibility mode** in gameplay code (do not only hide in UI)

### SettingChanged

`Plugin.cs` subscribes for Harmony patch toggles. Systems may subscribe in `Start` (example: `PsychoticBreakSystem`, overlay).

## LoggingService

| Level | Value | Typical use |
|-------|-------|-------------|
| None | 0 | Silent |
| Error | 1 | Failures only |
| Debug | 2 | Default; info + warnings |
| Verbose | 3 | Per-tick traces (`[Tension]`, `[AudioDread]`, etc.) |

Config: `9. Logging` → `LogLevel`. Hot-reload via `SettingChanged` in `Plugin.Awake`.

API: `LogInfo`, `LogWarning`, `LogError`, `LogVerbose` (gated). Plugin boot prints ASCII art once.

Agents: prefer `LoggingService` over `Plugin.Logger` (migrated codebase-wide).

## Generated cfg file

On disk: `BepInEx/config/elytraking.dread.cfg`

REPOConfig users: slider label compat is separate ([compatibility.md](compatibility.md)). Default path without REPOConfig is cfg or BepInEx Configuration Manager (F1).

## ADR

- `docs/adr/0014-configurable-logging.md`
