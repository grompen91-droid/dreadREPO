# Contract: System registration record

**Feature**: ARCH-3 | **Implementer**: `Systems/DreadSystemRegistry.cs` (name may vary)

## Record shape

Each registration MUST provide:

| Field | Type | Required |
|-------|------|----------|
| `Id` | `string` | Yes, kebab-case stable id |
| `SystemType` | `Type` (`Component` subclass) | Yes |
| `HostName` | `string` | Yes, unique per session |
| `OrderGroup` | enum `Core` or `Debug` | Yes |
| `IsEnabled` | `Func<bool>?` | No; null = attempt when init runs |

## Ordering rules

1. All `Core` registrations run before any `Debug` registration.
2. Within a group, order is declaration order in source (reviewers treat order as significant).
3. `RepoConfigSliderLabelCompat.TryApply` runs after successful init loop (not a registry row).

## Current systems (baseline set)

Implementations MUST register at least these (host names from [mod-architecture.md](../../../docs/agents/guides/mod-architecture.md)):

| Id | Type | Host | Group |
|----|------|------|-------|
| `audio-dread` | `AudioDreadSystem` | `DreadAudioHost` | Core |
| `monster-overhaul` | `MonsterOverhaulSystem` | `DreadMonsterHost` | Core |
| `tension` | `TensionSystem` | `DreadTensionHost` | Core |
| `error-reporter` | `ErrorReporterSystem` | `DreadErrorHost` | Core |
| `psychotic-break` | `PsychoticBreakSystem` | `DreadPsychoticBreakHost` | Core |
| `test-crash` | `TestCrashSystem` | `DreadTestCrashHost` | Debug |
| `debug-server` | `DebugServerSystem` | `DreadDebugHost` | Debug |
| `debug-overlay` | `DebugOverlaySystem` | `DreadDebugOverlayHost` | Debug |

Debug rows SHOULD use `IsEnabled` matching their config toggles where applicable.

## Predicate examples

```csharp
// Debug server only when enabled in cfg
IsEnabled = () => DreadConfig.DebugServerEnabled.Value

// Core system always attempted when init runs
IsEnabled = null
```

Compatibility mode: gameplay systems remain registered; internal logic and Harmony patches respect `DreadConfig.CompatibilityMode` (see compatibility guide).

## Static analysis

Tier 0 verify MUST fail if:

- `TryAddSystem<` appears outside `DreadSystemInitializer.cs` and the registry module, OR
- Registry id list diverges from the manifest in `verify-dread.ps1` (implementation choice documented in PR).

## Versioning

Registry is internal to Dread.dll. ARCH-4 may expose a public registration API; until then, third-party mods MUST NOT rely on this contract.
