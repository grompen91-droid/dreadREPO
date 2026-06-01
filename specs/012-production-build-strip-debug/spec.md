# Feature Specification: Production build strip debug (012)

**Feature Branch**: `012-production-build-strip-debug`

**Created**: 2026-06-01

**Status**: Complete (stub/Tier 0 verified; in-game Matrix 3/4 pending before release tag)

**Input**: CD pipeline and Thunderstore releases must ship a production `Dread.dll` that excludes developer/agent debug tooling (overlay, TCP debug server). **TestCrash** (section 11) stays in production for error-report verification. Error reporting for real exceptions stays in production.

**Amendment (2026-06-01):** TestCrash removed from strip list per maintainer request. Agent guide: [development-only-features.md](../../docs/agents/guides/development-only-features.md).

## Problem statement

Release builds today compile all debug systems into `Dread.dll`. Config defaults disable them, but the code, config sections, and TCP surface remain present. ADR-0013 intended debug server to be absent from release builds. Players and Thunderstore should not receive agent tooling, intentional crash buttons, or localhost command servers.

## User stories

### US1 - Compile-time production profile (Priority: P1)

**As a** player installing Dread from Thunderstore  
**I want** the mod DLL to exclude debug overlay and TCP server  
**So that** production builds cannot enable agent-only features via config.

**Acceptance criteria**:

- Release builds (default CD and `build.ps1`) define no `DREAD_DEBUG` and exclude agent debug source files from compilation.
- Production DLL does not contain type names `DebugOverlaySystem` or `DebugServerSystem`; **does** contain `TestCrashSystem` (verified via `.github/scripts/verify-production-dll.sh`).
- BepInEx config sections **8** and **9** are not bound in production; section **11. Testing** is bound.
- Error reporting (`ErrorReporterSystem`, prompt, real-exception POST) remains in production.
- `DebugConsoleGuardPatch` (compat, not agent tooling) remains in production.

**Independent test**: `dotnet build -c Release -p:EnableDebugFeatures=false` against stubs; `strings Dread.dll | grep DebugServerSystem` returns nothing; game loads with core features.

---

### US2 - CD and CI enforce production build (Priority: P1)

**As a** maintainer releasing via CD  
**I want** the pipeline to build and package the production profile explicitly  
**So that** Thunderstore and GitHub Release artifacts never accidentally include debug code.

**Acceptance criteria**:

- [`.github/workflows/cd.yml`](../../.github/workflows/cd.yml) build job passes `-p:EnableDebugFeatures=false` (explicit).
- Post-build step fails if debug type names appear in `bin/Release/net48/Dread.dll`.
- [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) uses the same production profile for PR builds.
- [`build.ps1`](../../build.ps1) documents Release = production; optional `-DebugBuild` for local MCP testing.

**Independent test**: CI green on PR; CD build artifact passes string check.

---

### US3 - Developer builds retain debug tooling (Priority: P2)

**As a** mod developer or agent  
**I want** Debug configuration (or `EnableDebugFeatures=true`) to include full debug tooling  
**So that** MCP/Tier 1 verify workflows continue to work locally.

**Acceptance criteria**:

- `dotnet build -c Debug` (or `-p:EnableDebugFeatures=true`) compiles agent debug systems and binds config sections 8 and 9 (section 11 also in production).
- [`dread-mcp-server/`](../../dread-mcp-server/) remains in repo; not bundled in Thunderstore zip.
- [`docs/agents/guides/debug-tooling.md`](../../docs/agents/guides/debug-tooling.md) documents production vs dev build commands.

**Independent test**: Debug build succeeds; Tier 1 MCP can connect when debug server enabled in-game.

---

### US4 - Verify script and registry contracts updated (Priority: P2)

**As an** agent running Tier 0 verify  
**I want** scripts and contracts to reflect conditional debug registry rows  
**So that** production builds do not fail spurious manifest checks.

**Acceptance criteria**:

- [`scripts/verify-dread.ps1`](../../scripts/verify-dread.ps1) requires core registry types always; debug types only when `#if DREAD_DEBUG` present in registry or `-RequireDebugRegistry` flag.
- [`specs/002-arch-3-extensible-core/contracts/extension-registry.md`](../002-arch-3-extensible-core/contracts/extension-registry.md) notes debug rows are debug-build only.

**Independent test**: `./scripts/verify-dread.ps1` passes on Release stub build.

## Functional requirements

- **FR-001**: MSBuild property `DreadDebug` MUST be true when `Configuration=Debug` OR `EnableDebugFeatures=true`.
- **FR-002**: When `DreadDebug` is false, MUST exclude `Systems/DebugOverlay/**` and `DebugServerSystem.cs` from compile (not `TestCrashSystem.cs`).
- **FR-003**: Shared files referencing debug types MUST use `#if DREAD_DEBUG` preprocessor guards.
- **FR-004**: CD MUST NOT change Thunderstore package layout (single `Dread.dll` + deps + audio).
- **FR-005**: CHANGELOG `[Unreleased]` MUST document production vs dev build behavior.

## Key entities

- **BuildProfile**: `Production` (Release, no `DREAD_DEBUG`) vs `Development` (Debug or explicit flag).
- **DebugSurface**: Overlay, TCP server, MCP-only hooks (`ForceEpisodeForDebug`). TestCrash is production; sync POST helpers are not debug-only.
- **ProductionSurface**: Core gameplay systems, error reporting, compat patches, logging config.

## Success criteria

- **SC-001**: Thunderstore zip built by CD contains no debug system type names in `Dread.dll` (see US1 string check; verified post-package in CD).
- **SC-002**: Debug build produces functional MCP/TCP path per existing verify docs.
- **SC-003**: Tier 0 verify passes on Release stub build without game install.
- **SC-004**: No manual version bump; CD release tags unchanged.

## Assumptions

- Error reporting stays in production (user confirmed in planning).
- `dread-mcp-server` is repo-only; never shipped to players.
- Orphaned cfg keys from old saves are harmless when sections are not bound.
- Single `Dread.dll` per release (no separate Dev Thunderstore listing in v1).

## Edge cases

- Player cfg file contains `DebugServerEnabled=true` from an older dev DLL: ignored in production (key not bound).
- Agent runs verify on Release DLL with MCP: Tier 1 fails as expected; docs must say use Debug build.
- `EnableDebugFeatures=true` on Release configuration: compiles debug code (escape hatch for CI experiments).
