# Research: Production build strip debug (012)

**Date**: 2026-06-01

## R1: Compile-time vs runtime exclusion

**Decision**: Use MSBuild `Compile Remove` + `DREAD_DEBUG` preprocessor symbol.

**Rationale**:
- Removes debug types from IL entirely (smaller DLL, no config bypass).
- Matches ADR-0013 wording: "disabled in release builds" intended compile-time absence.
- SDK-style projects support conditional `Compile Remove` without duplicate csproj files.

**Alternatives considered**:
- **Runtime `IsEnabled` only (status quo)**: Rejected; code still shipped and discoverable.
- **Separate `Dread.Dev.dll` Thunderstore package**: Rejected; extra listing/maintenance; out of scope.
- **Linker/IL trimming**: Rejected; not supported for net48 Unity mods; high risk.

## R2: When is DREAD_DEBUG defined?

**Decision**:
- `DreadDebug=true` when `Configuration==Debug` OR MSBuild property `EnableDebugFeatures==true`.
- CD/CI/build.ps1 Release: `EnableDebugFeatures=false` (explicit).
- Local dev default: `dotnet build -c Debug` gets debug tooling.

**Rationale**: Debug configuration is the natural developer default; explicit property allows Release+debug for one-off experiments without changing Configuration.

**Alternatives considered**:
- **Invert: define `DREAD_PRODUCTION` on Release only**: Equivalent; chose positive `DREAD_DEBUG` to match existing `#if DEBUG` mental model for dev code.

## R3: What stays in production?

**Decision**: Keep error reporting, `DebugConsoleGuardPatch`, `LogLevelEntry`, `DreadRuntimeState` field updates.

**Rationale**:
- Error reporting is player-facing telemetry with consent (ERR-2), not agent tooling.
- `DebugConsoleGuard` is compatibility (MenuLib/REPOConfig), not MCP surface.
- `DreadRuntimeState` updates are cheap; overlay reads them only when debug build present.

**Alternatives considered**:
- **Strip `DreadRuntimeState` in production**: Rejected; would require `#if` in many gameplay systems for marginal savings.

## R4: CD verification method

**Decision**: Post-build `strings` (or `grep -a`) on `Dread.dll` for `DebugServerSystem`, `TestCrashSystem`, `DebugOverlaySystem`.

**Rationale**: Simple, runs on Ubuntu CI without .NET reflection tools; catches accidental inclusion.

**Alternatives considered**:
- **Reflection-based test project**: Heavier; stub build sufficient for type-name check.
- **ILSpy in CI**: Overkill.

## R5: verify-dread registry manifest

**Decision**: Tier 0 `arch3_registry_manifest` always checks core types; debug types required only when registry source contains `#if DREAD_DEBUG` block OR caller passes `-RequireDebugRegistry`.

**Rationale**: Production Release build must pass Tier 0 without requiring debug types in registry at runtime.

**Alternatives considered**:
- **Always require nine types in source file**: Fails after conditional compile removes names from production-only builds of registry (names still in source inside `#if`).

## R6: Shared file `#if` inventory

**Decision**: Guard all compile-time references to excluded types:

| File | Guard |
|------|-------|
| `DreadConfig.cs` | Debug config fields + Bind |
| `DreadSystemRegistry.cs` | Debug registrations |
| `CampLureSystem.cs` | `NotifyDebug` |
| `SnitchSystem.cs` | Overlay notification |
| `PsychoticBreakSystem.cs` | `ForceEpisodeForDebug` |
| `ErrorReporterSystem.cs` | `ReportTestCrashAndWait` |
| `ErrorReportPayloadCapture.cs` | `BuildTestCrashReport` |

**Rationale**: `Compile Remove` alone causes CS0246 in files that reference removed types.

## R7: MCP server packaging

**Decision**: No change; `dread-mcp-server/` stays repo-only, built by Tier 0 verify, never copied to Thunderstore zip.

**Rationale**: MCP is stdio bridge to TCP; useless without debug server in DLL.
