# Tasks: ARCH-2 Reduce reflection and DLL surface

**Input**: Design documents from `/specs/001-arch-2-reduce-reflection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/build-profiles.md](./contracts/build-profiles.md)

**Tests**: No new unit test suite. Verification uses existing `scripts/verify-dread.ps1` Tier 0 and `tests/Dread.ErrorReportJson.Tests` per [quickstart.md](./quickstart.md).

**Organization**: Phases follow user story priority (US1, US2, US3). **Foundational inventory (T004-T005) and audit (T011-T012) block reflection reduction tasks (T013-T016).**

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 per [spec.md](./spec.md)

## Path Conventions

- Repository root: `Dread.csproj`, `Systems/`, `docs/agents/guides/`
- Spec kit: `specs/001-arch-2-reduce-reflection/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Branch hygiene and baseline before changes

- [x] T001 Rebase `001-arch-2-reduce-reflection` onto `origin/master` and confirm ARCH-1 (#167) is merged
- [x] T002 Record baseline stub build output (pass/fail) in PR description or `specs/001-arch-2-reduce-reflection/quickstart.md` notes per [quickstart.md](./quickstart.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Reflection inventory and build documentation MUST exist before code reduction tasks

**CRITICAL**: Do not start T013-T016 until T004-T005 and T011-T012 are complete

- [x] T003 Run baseline verification: `pwsh -NoProfile .github/scripts/gen-stubs.ps1` then stub `dotnet build` and `pwsh -NoProfile ./scripts/verify-dread.ps1` per [contracts/build-profiles.md](./contracts/build-profiles.md)
- [x] T004 [P] Create `docs/agents/guides/reflection-inventory.md` with full table (file, method, trigger, optional-mod gate, stub/full, disposition, rationale) for all `Systems/**/*.cs` reflection, `AccessTools`, and Harmony `Traverse` sites per [data-model.md](./data-model.md)
- [x] T005 [P] Add stub vs full build section to `docs/agents/guides/mod-architecture.md` from [contracts/build-profiles.md](./contracts/build-profiles.md) and link [quickstart.md](./quickstart.md)

**Checkpoint**: Inventory and build docs ready; reduction work can begin

---

## Phase 3: User Story 1 - Maintainer builds without game install (Priority: P1)

**Goal**: Stub/CI build and Tier 0 verify pass on ARCH-2 branch (no regression)

**Independent Test**: `gen-stubs.ps1` + Release build with `GameDir=.github/stubs/refs`; `verify-dread.ps1` returns ok

### Implementation for User Story 1

- [x] T006 [US1] After all code changes, run stub Release build with `-p:GameDir=.github/stubs/refs` on `Dread.csproj` per [contracts/build-profiles.md](./contracts/build-profiles.md)
- [x] T007 [US1] Run `pwsh -NoProfile ./scripts/verify-dread.ps1` and confirm Tier 0 passes
- [x] T008 [US1] Run `dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj -c Release --nologo`

**Checkpoint**: US1 satisfied; CI-equivalent path green

---

## Phase 4: User Story 2 - Developer builds against real game DLLs (Priority: P1)

**Goal**: Document and optionally validate full-game `GameDir` build path

**Independent Test**: Local `dotnet build` with real `REPO_Data/Managed`; game loads Dread after deploy

### Implementation for User Story 2

- [x] T009 [US2] Add full-profile MSBuild example (Linux r2modman paths) to `docs/agents/guides/mod-architecture.md` matching [contracts/build-profiles.md](./contracts/build-profiles.md)
- [x] T010 [US2] Update `specs/001-arch-2-reduce-reflection/quickstart.md` with optional full-build + r2modman deploy smoke checklist

**Checkpoint**: US2 documentation complete; full-build smoke optional for PR

---

## Phase 5: User Story 3 - Documented reflection inventory (Priority: P2)

**Goal**: Reviewers can see every reflection site, disposition, and hot-path classification

**Independent Test**: Open `docs/agents/guides/reflection-inventory.md`; zero unknown sites; each row has rationale

### Implementation for User Story 3

- [x] T011 [US3] Audit `docs/agents/guides/reflection-inventory.md` against `rg` scan of `Systems/` for `Reflection`, `AccessTools`, `Traverse`, `BindingFlags`, `GetType`, `TypeByName`
- [x] T012 [US3] Mark disposition `keep` | `reduce` | `replace` for every row; align with [research.md](./research.md) (REPOConfig compat, psychotic break UI stay `keep`)

**Checkpoint**: US3 acceptance met for issue #168 inventory requirement

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Safe reflection reductions, agent index, release notes (depends on Phase 2 + Phase 5)

- [x] T013 [P] Apply `replace`/`reduce` items for `Systems/Patches/EnemyNavMeshAgentAwakePatch.cs`, `Systems/Patches/PlayerControllerAwakePatch.cs`, `Systems/Patches/EnemyDirectorSetInvestigatePatch.cs`, and `Systems/HarmonyPatchCompat.cs` (`SemiFunc.IsMasterClient` cache) per inventory (only if stub types allow `typeof`)
- [x] T014 [P] Apply `reduce` items for `Systems/PlayerControllerCompat.cs` and `Systems/PlayerTumbleCompat.cs` per inventory (cache handles; no behavior change)
- [x] T015 [P] Confirm `Systems/DebugOverlay/DebugOverlayPanel.cs` `CountDreadPatches` remains documented as `keep` with PERF-2 visibility gate in `docs/agents/guides/reflection-inventory.md`
- [x] T016 Confirm no edits that remove required reflection in `Systems/RepoConfigSliderLabelCompat.cs` or `Systems/PsychoticBreak/PsychoticBreakOverlay.cs` without DBG-4 upstream
- [x] T017 Run `dotnet format --verify-no-changes --no-restore` on touched C# files
- [x] T018 Add ARCH-2 entry under `[Unreleased]` in `CHANGELOG.md` (inventory, stub/full docs, any reduction; no version bump)
- [x] T019 Set ARCH-2 to `done` in `docs/ROADMAP.md` and link `docs/agents/guides/reflection-inventory.md` in `docs/agents/guides/README.md` (roadmap status set at implementation; confirm on PR merge)
- [x] T020 Re-run T006-T008 (US1 final verify) after T013-T016

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Start immediately
- **Phase 2 (Foundational)**: After Phase 1; **blocks T013-T016**
- **Phase 3 (US1)**: Final tasks T006-T008 after Phase 6 reductions (T020)
- **Phase 4 (US2)**: Can run in parallel with Phase 5 after Phase 2 (docs only)
- **Phase 5 (US3)**: After T004 inventory draft; T011-T012 before T013-T016
- **Phase 6 (Polish)**: After Phase 2 and Phase 5 checkpoints

### User Story Dependencies

- **US1 (stub CI)**: Baseline in Phase 1-2; **final** verify after Phase 6
- **US2 (full build)**: Independent documentation (Phase 4); optional smoke manual
- **US3 (inventory)**: T004-T005 in Phase 2; audit T011-T012 before code reductions

### Recommended execution order (single developer)

1. T001-T003 (setup + baseline)
2. T004-T005 (inventory + mod-architecture docs)
3. T011-T012 (inventory audit)
4. T013-T016 (code reductions, constrained)
5. T017-T020 (format, changelog, roadmap, US1 verify)
6. T009-T010 (US2 docs, anytime after T005)

### Parallel Opportunities

- **T004** and **T005** (different files)
- **T009** and **T010** (US2 docs) parallel with **T011-T012** if inventory stable
- **T013**, **T014**, **T015** (different subsystems) after inventory signed off

### Parallel Example: Foundational docs

```bash
# After T003 baseline:
# Agent A: T004 reflection-inventory.md
# Agent B: T005 mod-architecture.md stub/full section
```

### Parallel Example: Reductions

```bash
# After T012 inventory complete:
# Agent A: T013 Systems/Patches/*.cs
# Agent B: T014 PlayerControllerCompat.cs + PlayerTumbleCompat.cs
# Agent C: T015-T016 overlay + compat confirmation (docs-only)
```

---

## Implementation Strategy

### MVP First (inventory + stub CI only)

1. Phase 1-2: Setup, baseline, **T004 inventory**, **T005 stub/full docs**
2. Phase 5: **T011-T012** audit inventory
3. Phase 3: **T006-T008** verify stub path still passes **without** T013-T014 reductions
4. Stop if issue #168 inventory acceptance is the only gate needed for a docs-only PR slice

### Full ARCH-2 delivery

1. Complete MVP above
2. Phase 6: T013-T016 reductions where disposition is not `keep`
3. T017-T020 polish and final US1 verify
4. Phase 4: US2 full-build documentation

### Incremental PR strategy

- **PR A**: T004, T005, T011-T012 (inventory + docs only)
- **PR B**: T013-T016 + T017-T020 (code + verify)

Or single PR `Fixes #168` with full task list.

---

## Notes

- Reference roadmap ID **ARCH-2** and issue **#168** in PR body
- Do not remove `RepoConfigSliderLabelCompat` (DBG-4); do not change player-facing behavior without CHANGELOG callout
- `stub-build.marker` deferred per [research.md](./research.md)
- Prefer merge ARCH-1 before large reduction diffs to avoid merge conflicts in `Systems/`
