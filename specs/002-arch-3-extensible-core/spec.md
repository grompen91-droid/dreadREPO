# Feature Specification: ARCH-3 Extensible mod design and hardened core

**Feature Branch**: `002-arch-3-extensible-core`

**Created**: 2026-05-30

**Status**: Implemented (commit `a574db7`)

**ROADMAP**: ARCH-3 stays `in-progress` until PR merge; set `done` in `docs/ROADMAP.md` on merge (T021).

**Roadmap**: ARCH-3 (P0) | **Issue**: [#175](https://github.com/grompen91-droid/dreadREPO/issues/175)

**Input**: User description: "ARCH-3: Extensible mod design and hardened core. Extension points, fail-safe init, compat patterns."

## User Scenarios & Testing

### User Story 1 - Maintainer adds a runtime system without touching Plugin (Priority: P1)

A contributor adds a new `MonoBehaviour` system with config entries and optional debug surface, registering it through a single documented extension point instead of editing `Plugin.cs` for every feature.

**Why this priority**: ARCH-3 defines how new systems land; without this, every feature re-opens god-file risk after ARCH-1.

**Independent Test**: Follow [contracts/system-lifecycle.md](./contracts/system-lifecycle.md) to add a no-op `ExampleProbeSystem` on a branch; stub build passes; initializer creates exactly one host; `Plugin.cs` diff has zero new system lines.

**Acceptance Scenarios**:

1. **Given** ARCH-3 merged, **When** a developer follows the system lifecycle contract, **Then** they register host name, system type, and optional gates in one registry module without editing patch apply logic in `Plugin.cs`.
2. **Given** one system throws during `AddComponent`, **When** initializer runs, **Then** other systems still start and the failure is logged with the system type name.

---

### User Story 2 - Player profile survives optional mods and compatibility mode (Priority: P1)

A player runs Dread with REPOConfig absent, with **Compatibility mode** on, or as a non-host client. Core ambient audio and documented degraded behavior work; no cascade crash from missing optional assemblies or skipped patches.

**Why this priority**: Issue #175 "harden core" acceptance includes a manual matrix; compat is already partial in code and must be formalized.

**Independent Test**: Manual matrix in [quickstart.md](./quickstart.md); Tier 0 `verify-dread.ps1` passes; normative compat matrix in [quickstart.md](./quickstart.md) and [docs/agents/guides/compatibility.md](../../docs/agents/guides/compatibility.md).

**Acceptance Scenarios**:

1. **Given** REPOConfig not installed, **When** game loads, **Then** Dread loads and `RepoConfigSliderLabelCompat` no-ops without exception.
2. **Given** `CompatibilityMode` true, **When** game runs, **Then** monster Harmony patches are not applied and tension psychotic-break mutations documented as disabled per compatibility guide.
3. **Given** non-host client, **When** monster patches would apply, **Then** host-only gates skip patch postfixes (ADR-0004).

---

### User Story 3 - Agents and debug tools see a stable extension model (Priority: P2)

Maintainers and MCP agents read one architecture note (ADR or CONTEXT section) describing boot order, registry, compat patterns, and `DreadRuntimeState` conventions.

**Why this priority**: Reduces duplicate guidance across `mod-architecture.md`, issue #175, and ad-hoc comments.

**Independent Test**: New ADR `docs/adr/0016-arch-3-extension-model.md` linked from `CONTEXT.md`, `mod-architecture.md`, and `docs/agents/guides/README.md`.

**Acceptance Scenarios**:

1. **Given** ARCH-3 docs, **When** an agent opens the extension model ADR, **Then** it describes Plugin vs initializer vs patch apply order and points to contracts.
2. **Given** debug overlay or MCP, **When** reading runtime state, **Then** documented fields on `DreadRuntimeState` remain the supported surface (no new reflection in debug paths for ARCH-3).

---

### Edge Cases

- `UnityEngine.UI` not loaded on first scene: initializer defers (existing behavior); psychotic break hosts must not spawn until UI ready.
- All systems fail `AddComponent`: log error once; plugin remains loaded; patches already applied in `Awake` follow compat rules.
- MenuLib loads after Dread: REPOConfig slider compat still applies from `Start` and post-init `TryApply`.
- Foreign Harmony owner on patch target: `CompatibilitySkipConflictingPatches` skips apply (existing).
- Stub CI build: registry and fail-safe init must not require game `Managed` DLLs.

## Requirements

### Functional Requirements

- **FR-001**: Introduce a **system registry** (single module) listing runtime systems, host object names, and optional enable predicates (config, compatibility mode, debug flags).
- **FR-002**: **Fail-safe initialization**: each system spawn is isolated; one failure does not prevent other systems from starting; aggregate success/failure is logged.
- **FR-003**: **Documented system lifecycle** contract covering config registration, initializer registration, scene gating, compat checks, and optional `DreadRuntimeState` publishing.
- **FR-004**: **Compat pattern library** update: consolidate optional-mod detection, host-only patch gates, compatibility mode matrix, and load-order notes in agent docs (align with ADR-0004, ADR-0009).
- **FR-005**: **Thin `Plugin.cs`**: patch apply/remove and config wiring stay in Plugin; no new per-system `TryAddSystem` lines after ARCH-3 (registry only).
- **FR-006**: **Verification**: Tier 0 `arch3_try_add_system` fails on any `TryAddSystem<` outside `Systems/DreadSystemInitializer.cs` and `Systems/DreadSystemRegistry.cs`; `arch3_registry_manifest` greps those eight system type names in `DreadSystemRegistry.cs` per [contracts/extension-registry.md](./contracts/extension-registry.md).
- **FR-007**: **Architecture note**: ADR describing extension model and relationship to ARCH-4 (external API deferred).

### Key Entities

- **System registration**: Type, host name, optional `Func<bool>` enable gate, initialization order group (core vs debug).
- **Init result**: Per-system success flag, optional exception message, running count for log summary.
- **Compat profile**: Combination of CompatibilityMode, optional mods present, host/client role affecting which registrations run.

## Success Criteria

- **SC-001**: Stub CI build and `verify-dread.ps1` Tier 0 pass on ARCH-3 branch.
- **SC-002**: Adding a documented probe system requires changes only in registry + new system file + config (not `Plugin.cs` system list).
- **SC-003**: Manual compat matrix in quickstart executed once and recorded in PR (REPOConfig absent, compatibility mode on, non-host noted as manual).
- **SC-004**: `docs/adr/0016-arch-3-extension-model.md` merged and cross-linked from agent index.

## Assumptions

- ARCH-1 file split is merged on `master` (PR #201).
- ARCH-2 reflection inventory is merged or available on branch (soft); no ARCH-4 external API in this feature.
- **Compatibility mode** behavior stays as documented in `compatibility.md`; ARCH-3 documents and tests matrix, not redesigning gameplay toggles.
- Registry v1 uses explicit C# registrations (no reflection-based assembly scan).
- `DreadRuntimeState` remains internal static surface for debug overlay and MCP; no public BepInEx API yet.

## Out of Scope

- ARCH-4 optional feature packs and documented third-party mod API.
- Rewriting all Harmony patches or removing `RepoConfigSliderLabelCompat` (DBG-4).
- Automatic plugin load-order manipulation beyond documentation.
- Full `#if STUB_BUILD` compile splits (ARCH-2 deferred `stub-build.marker`).
