# ARCH-3 Research

**Date**: 2026-05-30  
**Feature**: `002-arch-3-extensible-core`

## Decision: System registry (explicit C# list)

**Decision**: Add `DreadSystemRegistry` with a static ordered list of `SystemRegistration` records (type, host name, optional enable predicate). `DreadSystemInitializer` iterates the list inside existing UI-ready gate.

**Rationale**: Matches current `TryAddSystem<T>` pattern; stub-safe; reviewable in PR; no assembly scanning or plugin discovery.

**Alternatives considered**:

- Reflection over `Systems/*.cs`: rejected (fragile, stub noise, hides intent).
- BepInEx chainloader plugins: out of scope (ARCH-4).

## Decision: Fail-safe per-system init

**Decision**: Keep try/catch per registration (already in `TryAddSystem`); add summary log with succeeded/failed counts; optional registry flag `required` vs `optional` for future (v1: all optional except log severity).

**Rationale**: Issue #175 requires isolated failures; current code already catches per type.

**Alternatives considered**:

- Fail-fast on first error: rejected (one broken debug system would kill audio).

## Decision: Plugin.cs stays patch-focused

**Decision**: `Plugin.cs` retains Harmony apply/remove, config `SettingChanged`, scene hook to initializer. All `TryAddSystem` calls move to registry consumed only by initializer.

**Rationale**: FR-005; ARCH-1 split goal; patches are not "systems" in the MonoBehaviour sense.

**Alternatives considered**:

- Move patches into registry: rejected (different lifecycle and compat rules per ADR-0009).

## Decision: Init ordering groups

**Decision**: Three groups in registry order: (1) core gameplay/audio/tension/psychotic/error, (2) test crash (debug), (3) debug server/overlay gated on config.

**Rationale**: Debug hosts should not block core if misconfigured; matches current implicit order in initializer.

**Alternatives considered**:

- Alphabetical: rejected (psychotic break depends on UI load, not name).

## Decision: Compat documentation over new toggles

**Decision**: ARCH-3 documents and tests existing `CompatibilityMode`, `CompatibilitySkipConflictingPatches`, host-only patches, and REPOConfig soft deps. No new compatibility enum in v1.

**Rationale**: CONTEXT.md already defines **Compatibility mode** as in development; shipped toggle exists in `DreadConfig`.

**Alternatives considered**:

- Per-system compat flags: deferred (config surface explosion).

## Decision: Tier 0 verify guard

**Decision**: Add verify check: every `TryAddSystem<` in repo must appear only in `DreadSystemInitializer.cs` (or registry file if inlined), and registry type list must match a maintained manifest array in the verify script OR grep for duplicate registrations.

**Rationale**: FR-006; cheap static enforcement for agents.

**Alternatives considered**:

- Roslyn analyzer project: rejected (CI complexity for one repo).

## Decision: Extension model ADR

**Decision**: Ship `docs/adr/0016-arch-3-extension-model.md` as the canonical narrative; link from CONTEXT **System initializer** and **Compat layer** entries.

**Rationale**: Issue #175 acceptance; single place for ARCH-4 boundary.

**Alternatives considered**:

- CONTEXT-only: rejected (ADRs already hold lifecycle decisions).

## Technology notes

- **Related ADRs**: 0004 (host authority), 0009 (toggleable patches), 0001 (removed systems).
- **Related guides**: `mod-architecture.md`, `compatibility.md`, `harmony-and-patches.md`, ARCH-2 `reflection-inventory.md`.
- **Debug surface**: `DreadRuntimeState` + MCP commands documented in `debug-server.md`; ARCH-3 documents convention, no new MCP commands required.
