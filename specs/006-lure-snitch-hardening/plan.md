# Implementation Plan: Camp Lure and Snitch hardening (006)

**Branch**: `006-lure-snitch-hardening` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Status**: Implementation complete (stub build green). Manual quickstart matrices pending in-game verification (T036).

**Input**: Feature specification from `specs/006-lure-snitch-hardening/spec.md`

**Note**: Generated via `/speckit-plan`. `.specify/feature.json` pins this directory so setup scripts work off any git branch.

## Summary

Harden Camp Lure and Snitch with a **gameplay phase gate** in Core (menu / truck-shop / extraction level), add a **per-player lure cooldown** after contact, fix **empty-enemy false positives**, and clean up snitch **instrumentation, logging, arm state, and pickup heuristics**. Reuse existing `EnemyLureCompat`, `OnLevelGenDone` hook, and ADR-0016 registry pattern.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8, BepInEx 5.4, Harmony 2, Unity 2022.3 stubs

**Primary Dependencies**: BepInEx, HarmonyLib, game `Assembly-CSharp` (reflection compat), stub refs in `.github/stubs/refs`

**Storage**: N/A (runtime state in MonoBehaviour systems + `DreadRuntimeState`)

**Testing**: Tier 0 `scripts/verify-dread.ps1`, stub Release build, manual matrix in [quickstart.md](./quickstart.md)

**Target Platform**: R.E.P.O. (Windows/Linux via BepInEx); CI builds against stubs only

**Project Type**: BepInEx game mod (single plugin, `Systems/` layout)

**Performance Goals**: No per-frame `FindObjectsOfType` beyond existing patterns; phase resolve cached/invalidated on scene events

**Constraints**: Host-only monster features (ADR-0004); no manual version bump; stub build must pass

**Scale/Scope**: 2 gameplay systems + 1-2 Core compat modules + config/overlay/docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Stub CI build | Required | Extend stubs for discovered phase APIs |
| No manual version bump | Pass | CHANGELOG only |
| ADR-0004 host-only | Pass | No client enemy authority |
| ADR-0016 registry | Pass | No new hosts unless needed |
| ARCH-3 TryAddSystem | Pass | Modify existing systems only |
| Manual verify (Principle II) | Pass | quickstart.md matrix |

**Pre-design**: PASS

**Post-design**: PASS (phase compat is Core seam, not new public API)

## Project Structure

### Documentation (this feature)

```text
specs/006-lure-snitch-hardening/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
├── spec.md
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
Systems/Core/
├── GameplayContext.cs           # Phase enum + AllowsHostMonsterFeatures
├── GameplayPhaseCompat.cs       # NEW: REPO phase reflection + latch
├── ProximityScan.cs             # HasEnemies() helper
└── EnemyLureCompat.cs           # Document aggression coupling

Systems/
├── CampLureSystem.cs            # Cooldown, guards, phase gate
└── SnitchSystem.cs              # Phase gate, hygiene, marker grace

Config/DreadConfig.cs            # LureCooldownSeconds
Systems/DreadRuntimeState.cs     # Phase + cooldown overlay fields
Systems/DebugOverlay/DebugOverlayPanel.cs
Systems/Patches/SnitchLevelGenDonePatch.cs
.github/scripts/Assembly-CSharp_stubs.cs
```

**Structure Decision**: Single BepInEx plugin; new Core compat seam follows ADR-0016 / 005-core-deepening patterns.

## Phase summary

| Phase | Deliverable | User story |
|-------|-------------|------------|
| 0 | [research.md](./research.md) | Resolve REPO phase API + audio model |
| 1 | Gameplay phase Core API | US1 |
| 2 | Camp lure cooldown + correctness | US2, US3 |
| 3 | Snitch hygiene + phase integration | US4 |
| 4 | EnemyLureCompat docs/logging | US5 |
| 5 | Docs, overlay, changelog, cleanup | All |

## Architecture

### Gameplay phase model

```text
                    ┌─────────────┐
                    │    Menu     │  SemiFunc.MenuLevel() == true
                    └──────┬──────┘
                           │ load lobby / truck
                    ┌──────▼──────┐
                    │ Truck/Shop  │  !MenuLevel && !InExtractionLevel
                    └──────┬──────┘
                           │ enter extraction / OnLevelGenDone
                    ┌──────▼──────────┐
                    │ ExtractionLevel │  AllowsHostMonsterFeatures == true
                    └─────────────────┘
```

**Default-safe rule**: If phase cannot be determined, treat as **not** extraction level (features off).

### Subagent execution strategy

Use **subagent-driven-development** after Phase 1 (phase API) lands:

| Stream | Focus | Files |
|--------|-------|-------|
| A | Camp lure | `CampLureSystem.cs`, config, runtime state |
| B | Snitch | `SnitchSystem.cs`, marker, remove debug log |
| C | Core + overlay | `GameplayContext`, compat, overlay, docs |

## Complexity Tracking

No constitution violations requiring justification.

## Risks

| Risk | Mitigation |
|------|------------|
| REPO has no stable shop API | Latch from `OnLevelGenDone` + reset on truck scene load |
| Cooldown too long/short | Configurable `LureCooldownSeconds` |
| Snitch bang host-only | Document; research networked sound path |
| Pickup heuristics false-positive | 2s grace after marker spawn |

## Generated artifacts

- [research.md](./research.md)
- [data-model.md](./data-model.md)
- [contracts/gameplay-phase-gate.md](./contracts/gameplay-phase-gate.md)
- [contracts/camp-lure-config.md](./contracts/camp-lure-config.md)
- [quickstart.md](./quickstart.md)
- [tasks.md](./tasks.md)
