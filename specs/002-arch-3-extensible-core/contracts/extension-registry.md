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

1. All `Core` registrations run before any `Debug` registration (`DreadSystemInitializer` sorts by `OrderGroup`, then declaration order in the registry list).
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
| `notifications` | `DreadNotificationSystem` | `DreadNotificationHost` | Core |
| `camp-lure` | `CampLureSystem` | `DreadCampLureHost` | Core |
| `snitch` | `SnitchSystem` | `DreadSnitchHost` | Core |
| `test-crash` | `TestCrashSystem` | `DreadTestCrashHost` | Debug *(012: `#if DREAD_DEBUG` only)* |
| `debug-server` | `DebugServerSystem` | `DreadDebugHost` | Debug *(012: `#if DREAD_DEBUG` only)* |
| `debug-overlay` | `DebugOverlaySystem` | `DreadDebugOverlayHost` | Debug *(012: `#if DREAD_DEBUG` only)* |

Debug rows **may omit** `IsEnabled` when ADR-0016 applies. **012 amendment:** in production builds (`DREAD_DEBUG` undefined), debug rows are not compiled; hosts do not spawn. In development builds, prior behavior retained (config gates behavior inside systems).

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
- `arch3_registry_manifest`: any of the eight baseline `SystemType` names missing from `DreadSystemRegistry.cs`.

## Versioning

Registry is internal to Dread.dll. ARCH-4 may expose a public registration API; until then, third-party mods MUST NOT rely on this contract.
