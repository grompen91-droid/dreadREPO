# Implementation Plan: ARCH-3 Extensible mod design and hardened core

**Branch**: `002-arch-3-extensible-core` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-arch-3-extensible-core/spec.md`

**Roadmap**: ARCH-3 (P0) | **GitHub**: [#175](https://github.com/grompen91-droid/dreadREPO/issues/175)

## Summary

Formalize how Dread grows: a **system registry** and fail-safe `DreadSystemInitializer`, documented lifecycle and compat patterns, thin `Plugin.cs` (patches only), ADR for the extension model, and Tier 0 verify guards. Hardens optional-mod and compatibility-mode paths without changing player-facing defaults unless fixing a defect.

## Technical Context

**Language/Version**: C# (latest in project), .NET Framework 4.8

**Primary Dependencies**: BepInEx 5.4.x, Harmony 2, Unity/game assemblies (stub or full)

**Storage**: N/A

**Testing**: `scripts/verify-dread.ps1` Tier 0 (extended); `tests/Dread.ErrorReportJson.Tests`; manual compat matrix per [quickstart.md](./quickstart.md); optional Tier 1 MCP

**Target Platform**: R.E.P.O. (Windows / Proton / Linux); CI `ubuntu-latest`

**Project Type**: BepInEx plugin (`Dread.csproj`)

**Performance Goals**: No extra per-frame work from registry; init runs once per session after UI ready

**Constraints**: Stub CI must pass; no manual version bumps; minimal behavior change; ARCH-1 layout; depend on merged ARCH-1, soft ARCH-2

**Scale/Scope**: ~5-8 C# files touched (`DreadSystemInitializer`, new registry, `Plugin.cs` cleanup), 1 ADR, agent doc updates, verify script

## Constitution Check

*GATE: `.specify/memory/constitution.md` is a template. Gates use `AGENTS.md` and `CONTEXT.md`.*

| Gate | Status |
|------|--------|
| Stub CI build passes | Required before merge |
| No manual `manifest.json` / `Plugin.VERSION` bump | Pass |
| Minimal diff; slice by US1/US2/US3 | Pass |
| Domain terms in CONTEXT.md / ADR | Pass (FR-007) |
| No em dash in markdown | Pass |

**Post-design**: No violations; registry is simpler than plugin-scan reflection.

## Project Structure

### Documentation (this feature)

```text
specs/002-arch-3-extensible-core/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── system-lifecycle.md
│   └── extension-registry.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
Plugin.cs                          # Patches + config only (FR-005)
Systems/
├── DreadSystemInitializer.cs      # Fail-safe loop over registry
├── DreadSystemRegistry.cs           # NEW: registrations
├── DreadRuntimeState.cs             # Debug surface (document only unless gaps)
├── HarmonyPatchCompat.cs
├── RepoConfigSliderLabelCompat.cs
├── Patches/
└── ... (existing systems)
Config/DreadConfig.cs
docs/
├── adr/0016-arch-3-extension-model.md   # NEW (implement phase)
└── agents/guides/
    ├── mod-architecture.md          # Registry + lifecycle links
    └── compatibility.md             # Matrix alignment
scripts/verify-dread.ps1             # Tier 0 ARCH-3 check
```

**Structure Decision**: Single plugin; ARCH-3 centralizes init extensibility without new assemblies.

## Phase 0: Research

**Status**: Complete. See [research.md](./research.md).

Resolved:

- Explicit registry vs reflection scan (registry wins for v1)
- Init ordering: UI gate first, then core systems, then debug systems (config-gated)
- Compat matrix documented in quickstart; code paths mostly exist
- Verify guard options (registry completeness vs grep ban on stray `TryAddSystem`)

## Phase 1: Design

**Status**: Complete.

### Contracts

- [contracts/system-lifecycle.md](./contracts/system-lifecycle.md): end-to-end steps to add a system
- [contracts/extension-registry.md](./contracts/extension-registry.md): registration record shape and enable predicates

### Data model

- [data-model.md](./data-model.md): `SystemRegistration`, `InitResult`, `CompatProfile`

### Agent context

- Implement phase: add ADR-0016, update `mod-architecture.md` "Adding a new runtime system" to point at registry contract
- Link spec folder from `docs/agents/guides/README.md` when implementing

## Phase 2: Implementation (`tasks.md`)

**Not executed by `/speckit-plan`.** Suggested slices for `/speckit-tasks`:

| Slice | Scope |
|-------|--------|
| US1 | `DreadSystemRegistry` + refactor initializer + verify guard |
| US2 | Compat doc matrix + quickstart manual steps; optional unit-less grep tests |
| US3 | ADR-0016 + cross-links; `DreadRuntimeState` doc table if needed |

**Dependencies**: Rebase onto `origin/master` (ARCH-1 merged; ARCH-2 preferred).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | | |

## Risks

| Risk | Mitigation |
|------|------------|
| Init order regression | UI defer + initializer `OrderBy(OrderGroup)`; document declaration order within group |
| Debug systems skip init when toggled off | ADR-0016: debug hosts always register; toggles gate behavior inside systems |
| Registry drift (system without row) | Tier 0 verify compares registry to expected set |
| Scope creep into ARCH-4 API | FR-007 ADR explicitly defers public mod API |

## Success verification

Verified on implement commit `a574db7` (speckit-analyze alignment pass may refresh docs only).

- [x] [spec.md](./spec.md) success criteria met
- [x] Issue #175 acceptance checkboxes in PR
- [x] `verify-dread.ps1` Tier 0 pass (stub build)
- [x] Manual compat matrix recorded in PR description (PR author fills [quickstart.md](./quickstart.md) template)
