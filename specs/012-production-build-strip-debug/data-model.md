# Data model: Production build strip debug (012)

## BuildProfile

Compile-time profile selected by MSBuild.

| Field | Production | Development |
|-------|------------|-------------|
| `Configuration` | Release (CD default) | Debug (local default) |
| `EnableDebugFeatures` | false (explicit in CI/CD) | true (implicit via Debug) or explicit |
| `DreadDebug` | false | true |
| `DefineConstants` | (none extra) | `DREAD_DEBUG` |
| Compiled debug files | Excluded | Included |

**Validation**: Production build MUST NOT emit debug system type names in `Dread.dll`.

## DebugSurface (development only)

Runtime systems and config removed from production IL.

| Id | Type | Config section | Host name |
|----|------|----------------|-----------|
| `debug-overlay` | `DebugOverlaySystem` | 8. Debug Overlay | `DreadDebugOverlayHost` |
| `debug-server` | `DebugServerSystem` | 9. Debug Server | `DreadDebugHost` |
| `test-crash` | `TestCrashSystem` | 11. Testing | `DreadTestCrashHost` |

**Relationships**:
- Registered in `DreadSystemRegistry` only when `DREAD_DEBUG`.
- MCP tools (`dread-mcp-server`) depend on TCP server (dev build only).

## ProductionSurface (always compiled)

| Category | Examples |
|----------|----------|
| Core gameplay | Audio, monsters, tension, psychotic break, snitch, camp lure |
| Error reporting | `ErrorReporterSystem`, prompt, real-exception POST |
| Compat | `DebugConsoleGuardPatch`, Harmony lifecycle |
| Shared state | `DreadRuntimeState` (writers in core systems) |

## ConfigKeyVisibility

| Section | Production bind | Development bind |
|---------|-----------------|------------------|
| 1-7, 10 | Yes | Yes |
| 8 Debug Overlay | No | Yes |
| 9 Debug Server | No | Yes |
| 11 Testing | No | Yes |

**State transition**: Switching DLL from dev to production leaves orphan keys in `BepInEx.cfg`; BepInEx ignores unbound keys.

## PipelineArtifact

CD output unchanged structurally.

| Artifact | Build profile |
|----------|---------------|
| `bin/Release/net48/Dread.dll` | Production |
| Thunderstore zip | Production DLL + deps + audio |
| GitHub Release DLL assets | Production |
| `dread-mcp-server/dist` | Built in Tier 0; not in zip |
