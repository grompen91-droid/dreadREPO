---
description: "Task list for Camp Lure and Snitch hardening (gameplay phase gate, lure cooldown, bug fixes)"
---

# Tasks: Camp Lure and Snitch hardening (006)

**Input**: Design documents from `specs/006-lure-snitch-hardening/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Branch**: `006-lure-snitch-hardening`

**Tests**: No automated play-mode tests (constitution Principle II). Tier 0 stub build + manual matrices in [quickstart.md](./quickstart.md).

**Execution**: After Phase 2 completes, use **subagent-driven-development** for Phases 3-4 in parallel (Camp Lure stream vs Snitch stream).

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

**Purpose**: Branch, baseline verify, REPO phase API discovery.

- [x] T001 Create branch `006-lure-snitch-hardening` from `master` (deferred: work on `cursor/snitch-arm-fix-abb2`; `feature.json` pins spec dir)
- [x] T002 [P] Run Tier 0 stub baseline per `specs/006-lure-snitch-hardening/quickstart.md` (gen-stubs, Release build, verify-dread)
- [x] T003 [P] Research REPO truck/shop vs extraction APIs against real `Assembly-CSharp.dll` or game docs; record chosen method names in `specs/006-lure-snitch-hardening/research.md` (append **Implementation notes** section)

**Checkpoint**: Green build; phase API candidate identified or latch-only fallback confirmed.

---

## Phase 2: Foundational - Gameplay phase API (US1)

**Purpose**: Core gate blocking ALL user stories. MUST complete before lure/snitch logic changes.

**CRITICAL**: No Camp Lure or Snitch behavior changes until this phase completes.

- [x] T004 Add `GameplayPhase` enum and phase properties to `Systems/Core/GameplayContext.cs` per `specs/006-lure-snitch-hardening/contracts/gameplay-phase-gate.md`
- [x] T005 [P] Create `Systems/Core/GameplayPhaseCompat.cs` with `ResolvePhase()`, extraction latch, `NotifyExtractionLevelStarted()`, `ResetForSceneLoad()` per `specs/006-lure-snitch-hardening/research.md`
- [x] T006 Wire `SnitchLevelGenDonePatch` postfix to call `GameplayPhaseCompat.NotifyExtractionLevelStarted()` in `Systems/Patches/SnitchLevelGenDonePatch.cs`
- [x] T007 [P] Call `GameplayPhaseCompat.ResetForSceneLoad()` from single-scene handlers in `Systems/SnitchSystem.cs` and `Systems/CampLureSystem.cs`
- [x] T008 [P] Add stub entries for discovered REPO phase APIs in `.github/scripts/Assembly-CSharp_stubs.cs`
- [x] T009 Add `GameplayPhase`, `LureCooldownRemaining` fields to `Systems/DreadRuntimeState.cs`
- [x] T010 [P] Publish phase label in `Systems/DebugOverlay/DebugOverlayPanel.cs` (Mod State section)

**Checkpoint**: `AllowsHostMonsterFeatures` false in truck/shop; true after level gen in dev overlay.

---

## Phase 3: User Story 2 - Camp lure cooldown (Priority: P1)

**Goal**: Per-player cooldown after contact; configurable duration.

**Independent Test**: Matrix 2 in `specs/006-lure-snitch-hardening/quickstart.md`.

### Implementation for User Story 2

- [x] T011 [US2] Add `LureCooldownSeconds` to `Config/DreadConfig.cs` per `specs/006-lure-snitch-hardening/contracts/camp-lure-config.md`
- [x] T012 [US2] Replace per-player float map with camp + cooldown record in `Systems/CampLureSystem.cs` per `specs/006-lure-snitch-hardening/data-model.md`
- [x] T013 [US2] On contact (target nearest <= safe), set `CooldownUntil` and `ClearTarget()` same frame in `Systems/CampLureSystem.cs`
- [x] T014 [US2] Exclude cooldown players from target selection in `Evaluate()` in `Systems/CampLureSystem.cs`
- [x] T015 [P] [US2] Publish `LureCooldownRemaining` in `Systems/CampLureSystem.cs` and show in lure overlay row in `Systems/DebugOverlay/DebugOverlayPanel.cs`

**Checkpoint**: User Story 2 manual matrix passes.

---

## Phase 4: User Story 3 - Camp lure correctness (Priority: P1)

**Goal**: No lure without enemies; phase gate; immediate pull stop.

**Independent Test**: Matrix 3 + phase portions of Matrix 1 in quickstart.

### Implementation for User Story 3

- [x] T016 [P] [US3] Add `ProximityScan.HasEnemies()` (or equivalent) in `Systems/Core/ProximityScan.cs`
- [x] T017 [US3] Gate `Evaluate()` and `MaybePull()` on `HasEnemies()` and `GameplayContext.AllowsHostMonsterFeatures` in `Systems/CampLureSystem.cs`
- [x] T018 [US3] Skip camp timer accumulation when `!HasEnemies()` in `Systems/CampLureSystem.cs`
- [x] T019 [US3] Add contact check before `MaybePull()` for immediate target clear in `Systems/CampLureSystem.cs`
- [x] T020 [P] [US3] Add lure block reason to overlay when gated by phase in `Systems/DebugOverlay/DebugOverlayPanel.cs`

**Checkpoint**: User Stories 2 and 3 complete.

---

## Phase 5: User Story 4 - Snitch reliability (Priority: P2)

**Goal**: Phase gate, clean logs, failed arm state, pickup grace.

**Independent Test**: Matrix 4 + regression #222 in quickstart.

### Implementation for User Story 4

- [x] T021 [US4] Gate snitch `Update`/`TryArm` on `GameplayContext.AllowsHostMonsterFeatures` in `Systems/SnitchSystem.cs`
- [x] T022 [US4] Update `GetBlockReason()` with phase strings (`truck/shop`, etc.) in `Systems/SnitchSystem.cs`
- [x] T023 [P] [US4] Replace `_armed = true` on max retries with `_armFailed` + overlay state `failed` in `Systems/SnitchSystem.cs`
- [x] T024 [US4] Downgrade arm attempt logging to Verbose; keep single Info on successful arm in `Systems/SnitchSystem.cs`
- [x] T025 [US4] Add 2s pickup grace period in `SnitchItemMarker` coroutine in `Systems/SnitchSystem.cs`
- [x] T026 [P] [US4] Remove all `#region agent log` blocks and `AgentDebugLog086b84` references from `Systems/SnitchSystem.cs`, `Systems/Core/ItemRosterCompat.cs`, and related files
- [x] T027 [US4] Delete `Systems/Core/AgentDebugLog086b84.cs` if no remaining references

**Checkpoint**: User Story 4 complete; no agent debug log in repo.

---

## Phase 6: User Story 5 - Investigate coordination (Priority: P2)

**Goal**: Document aggression coupling; optional verbose source tags.

**Independent Test**: Matrix 5 + concurrent lure/snitch in quickstart.

### Implementation for User Story 5

- [x] T028 [P] [US5] Add comment in `Systems/Core/EnemyLureCompat.cs` documenting `EnemyDirectorSetInvestigatePatch` 1.5x interaction
- [x] T029 [P] [US5] Add Verbose pull source tags at call sites in `Systems/CampLureSystem.cs` and `Systems/SnitchSystem.cs`
- [x] T030 [US5] Document snitch bang MP limitation and investigate path in `specs/006-lure-snitch-hardening/research.md` **Implementation notes** (after manual MP test)

**Checkpoint**: User Story 5 complete.

---

## Phase 7: Polish and cross-cutting

**Purpose**: Docs, changelog, domain glossary, verify.

- [x] T031 [P] Add Camp Lure and Snitch glossary entries to `CONTEXT.md`
- [x] T032 [P] Update `[Unreleased]` in `CHANGELOG.md` with phase gate, cooldown, and fix bullets
- [x] T033 [P] Update `docs/agents/guides/mod-architecture.md` gameplay gating section for `AllowsHostMonsterFeatures`
- [x] T034 [P] Update `docs/agents/guides/reflection-inventory.md` if new compat types added
- [x] T035 Run Tier 0 verify and stub Release build per `specs/006-lure-snitch-hardening/quickstart.md`
- [ ] T036 Execute full manual matrix in `specs/006-lure-snitch-hardening/quickstart.md` and record results in plan.md status

---

## Dependencies

```text
Phase 1 (Setup)
    ↓
Phase 2 (Gameplay phase API) ── BLOCKS ──→ Phase 3, 4, 5
    ↓
Phase 3 (US2 cooldown) ──┐
Phase 4 (US3 correctness)┼── both touch CampLureSystem.cs (sequential: T011-T015 then T016-T020)
Phase 5 (US4 snitch) ──────┘── parallel with Phase 3-4 AFTER Phase 2 (disjoint files)
    ↓
Phase 6 (US5) ── parallel with late Phase 4/5
    ↓
Phase 7 (Polish)
```

### User story completion order

1. **US1** (Phase 2): Gameplay phase gate
2. **US2 + US3** (Phases 3-4): Camp lure (same file, sequential)
3. **US4** (Phase 5): Snitch (parallel with 3-4 after Phase 2)
4. **US5** (Phase 6): Docs/logging
5. **Polish** (Phase 7)

---

## Parallel execution examples

### After Phase 2 lands (subagent-driven-development)

**Stream A - Camp Lure agent**  
Tasks: T011-T020 (Phases 3-4)  
Files: `CampLureSystem.cs`, `ProximityScan.cs`, `DreadConfig.cs`, overlay lure row

**Stream B - Snitch agent**  
Tasks: T021-T027 (Phase 5)  
Files: `SnitchSystem.cs`, `ItemRosterCompat.cs`, delete AgentDebugLog

**Stream C - Core/Docs agent**  
Tasks: T028-T034 (Phases 6-7 partial)  
Files: `EnemyLureCompat.cs`, CONTEXT, CHANGELOG, guides

### Within Phase 2 (parallel)

```text
T005 GameplayPhaseCompat.cs
T008 stubs
T010 overlay phase row
(can run alongside T004 after enum shape agreed)
```

---

## Implementation strategy

### MVP (minimum shippable)

Phase 2 (US1) + Phase 3 T011-T014 (cooldown core) + T017 (phase gate on lure) + T021 (phase gate on snitch)

Delivers: shop/truck fix + cooldown + stops worst re-lure loop.

### Full scope

All 36 tasks through manual matrix sign-off.

---

## Task summary

| Metric | Count |
|--------|------:|
| Total tasks | 36 |
| Phase 1 Setup | 3 |
| Phase 2 Foundational (US1) | 7 |
| US2 Cooldown | 5 |
| US3 Lure correctness | 5 |
| US4 Snitch | 7 |
| US5 Coordination | 3 |
| Polish | 6 |

**Suggested MVP**: 12 tasks (T004-T007, T011-T014, T017, T021-T022)

**Parallel opportunities**: Phase 2 (3 tasks), post-Phase-2 streams A/B/C, Phase 6 (2 tasks), Phase 7 (4 tasks)

**Format validation**: All tasks use `- [ ] Tnnn [P?] [USn?] Description with file path`
