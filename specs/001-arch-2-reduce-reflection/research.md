# ARCH-2 Research

**Date**: 2026-05-30  
**Feature**: `001-arch-2-reduce-reflection`

## Decision: Stub vs full build profiles

**Decision**: Keep two documented MSBuild profiles: **Stub** (`GameDir` + `BepInExDir` → `.github/stubs/refs` and profile/core) and **Full** (`GameDir` → game `REPO_Data/Managed`). No third "hybrid" profile in v1.

**Rationale**: Matches existing AGENTS.md, CI (`gen-stubs.ps1` + `dotnet build`), and contributor workflow. Stubs intentionally omit or simplify some game types; full build validates real APIs.

**Alternatives considered**:

- Vendoring `Managed` into repo: rejected (size, licensing, update churn).
- Single profile only: rejected (breaks cloud/CI agents).

## Decision: Reflection inventory as deliverable

**Decision**: Ship `docs/agents/guides/reflection-inventory.md` as the canonical inventory, generated/maintained during ARCH-2 implementation, referenced from `mod-architecture.md`.

**Rationale**: Issue #168 acceptance explicitly requires inventory with rationale. Table format supports PR review and future ARCH-3 work.

**Alternatives considered**:

- Comments-only in code: rejected (hard for agents to discover).
- ADR per site: rejected (too heavy).

## Decision: Tiered reduction strategy

**Decision**: Classify sites into **Keep** (required for stubs/optional mods), **Reduce** (cache, move off hot path, narrow scope), **Replace** (compile-time type when in stubs). Implement **Replace** and **Reduce** only where risk is low; do not remove REPOConfig/MenuLib reflection without upstream DBG-4.

**Rationale**: Zero-reflection is incompatible with optional MenuLib/REPOConfig and Unity UI deferred load. ARCH-2 success = documented surface + safe wins.

**Alternatives considered**:

- Big-bang removal of all `GetType` calls: rejected (high regression risk).

## Decision: Harmony patch resolution

**Decision (superseded 2026-05-30)**: Initial plan favored `AccessTools.TypeByName` for all patch types. **Implementation** uses `typeof` for `EnemyNavMeshAgent`, `PlayerController`, and `EnemyDirector` where [Assembly-CSharp stubs](../../../.github/scripts/Assembly-CSharp_stubs.cs) define the type; keeps `TypeByName` for `DebugConsoleUI` (not in stubs). See [reflection-inventory.md](../../docs/agents/guides/reflection-inventory.md) rows `harmony-*-apply`.

**Rationale**: Stubs now expose those types; `typeof` improves compile-time checking without breaking stub CI.

**Alternatives considered**:

- `TypeByName` for all patches: rejected for stub-covered types after inventory audit.

## Decision: Hot-path reflection

**Decision**: No new per-frame reflection in ARCH-2; audit existing:

| Site | Path | Cadence | ARCH-2 action |
|------|------|---------|----------------|
| Patch count | `DebugOverlay/DebugOverlayPanel.cs` | When overlay visible | Already gated (PERF-2); document only |
| Player tumble / hiding | `PlayerTumbleCompat.cs`, `PlayerControllerCompat.cs` | Per-frame or frequent | Inventory; cache field `MethodInfo` at init if touched |
| Error payload | `ErrorReportPayloadCapture.cs` | On log batch | Inventory; avoid `FindObjectsOfType` expansion |
| Psychotic break visibility | `PsychoticBreakTrigger.cs` | 0.25s / 2s | Inventory only (gameplay-sensitive) |
| REPOConfig compat | `RepoConfigSliderLabelCompat.cs` | On slider create | Keep until DBG-4 |

**Rationale**: Aligns with PERF-2 and ARCH-1 layout; avoids scope creep into gameplay.

## Decision: `stub-build.marker` (issue mention)

**Decision**: Defer emitting `stub-build.marker` in output unless CI needs it; document stub build via MSBuild property `GameDir` path check in guide. Revisit if error reporter or features need compile-time `#if STUB_BUILD`.

**Rationale**: Grep shows no existing marker in repo; issue listed as documentation goal, not implemented artifact.

**Alternatives considered**:

- Add marker file in `obj/` or output: optional follow-up task in implementation.

## Technology notes

- **Target**: .NET Framework 4.8, Harmony 2, BepInEx 5.4.x.
- **Testing**: `verify-dread.ps1` Tier 0, `dotnet test` ErrorReportJson, optional Tier 1 MCP.
- **Constitution**: Project `.specify/memory/constitution.md` is template-only; gates derived from `AGENTS.md` (no manual version bump, minimal diff, glossary in CONTEXT.md).
