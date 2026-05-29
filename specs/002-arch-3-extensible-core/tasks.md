# Tasks: ARCH-3 Extensible mod design and hardened core

**Input**: Design documents from `/specs/002-arch-3-extensible-core/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/system-lifecycle.md](./contracts/system-lifecycle.md), [contracts/extension-registry.md](./contracts/extension-registry.md)

**Tests**: No new unit test suite. Verification uses `scripts/verify-dread.ps1` Tier 0, `tests/Dread.ErrorReportJson.Tests`, and manual compat matrix per [quickstart.md](./quickstart.md).

**Organization**: Phases follow user story priority (US1, US2, US3). **Foundational registry (T004-T005) blocks US1 verify guard and final CI (T020).**

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 per [spec.md](./spec.md)

## Path Conventions

- Repository root: `Plugin.cs`, `Systems/`, `Config/`, `scripts/`
- Spec kit: `specs/002-arch-3-extensible-core/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Branch hygiene and baseline before changes

- [x] T001 Rebase `002-arch-3-extensible-core` onto `origin/master` and confirm ARCH-1 (#167) is merged; prefer ARCH-2 (#168) on master if available
- [x] T002 Record baseline stub build pass/fail in `specs/002-arch-3-extensible-core/quickstart.md` notes section
- [x] T003 Run baseline verification: `pwsh -NoProfile .github/scripts/gen-stubs.ps1`, stub `dotnet build`, and `pwsh -NoProfile ./scripts/verify-dread.ps1` per [quickstart.md](./quickstart.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: System registry and fail-safe initializer MUST exist before verify guard and compat doc sign-off

**CRITICAL**: Do not start T007 or Phase 4+ doc tasks that assume registry until T004-T005 are complete

- [x] T004 Create `Systems/DreadSystemRegistry.cs` with `SystemRegistration` records for all rows in [contracts/extension-registry.md](./contracts/extension-registry.md) (Core then Debug order; debug `IsEnabled` predicates per research.md)
- [x] T005 Refactor `Systems/DreadSystemInitializer.cs` to iterate `DreadSystemRegistry` with per-system try/catch, preserve UI defer gate and post-init `RepoConfigSliderLabelCompat.TryApply`; remove inline `TryAddSystem<>` calls

**Checkpoint**: Registry drives init; `Plugin.cs` unchanged for system spawning

---

## Phase 3: User Story 1 - Maintainer adds a system without touching Plugin (Priority: P1)

**Goal**: New systems register only in registry; isolated init failures; Tier 0 guard enforces thin Plugin

**Independent Test**: Follow [contracts/system-lifecycle.md](./contracts/system-lifecycle.md); stub build + verify pass; `rg TryAddSystem` only in initializer/registry files

### Implementation for User Story 1

- [x] T006 [US1] Confirm `Plugin.cs` contains no `TryAddSystem` calls; patch apply/remove and scene hook only
- [x] T007 [US1] Add ARCH-3 Tier 0 check to `scripts/verify-dread.ps1` (forbid `TryAddSystem<` outside `Systems/DreadSystemInitializer.cs` and `Systems/DreadSystemRegistry.cs`, or registry manifest match per [research.md](./research.md))
- [x] T008 [US1] Validate SC-002: add temporary `Systems/Arch3ProbeSystem.cs` + registry row per [quickstart.md](./quickstart.md), stub build, then remove probe before merge unless team keeps test host
- [x] T009 [US1] Run stub Release build on `Dread.csproj` with `-p:GameDir=.github/stubs/refs` after T004-T008

**Checkpoint**: US1 code path complete; probe removed from shipping branch

---

## Phase 4: User Story 2 - Player profile survives optional mods and compatibility mode (Priority: P1)

**Goal**: Documented compat matrix matches shipped behavior; no regressions when REPOConfig absent or compatibility mode on

**Independent Test**: Manual matrix in [quickstart.md](./quickstart.md); `docs/agents/guides/compatibility.md` matches `Plugin.cs` and `HarmonyPatchCompat` behavior

### Implementation for User Story 2

- [x] T010 [P] [US2] Update `docs/agents/guides/compatibility.md` with ARCH-3 matrix (REPOConfig absent, compatibility mode on, non-host client, foreign patch skip) aligned to [quickstart.md](./quickstart.md)
- [x] T011 [US2] Cross-link matrix from `docs/mod-compatibility.md` if player-facing table is missing rows
- [x] T012 [US2] Audit `Systems/RepoConfigSliderLabelCompat.cs` and `Plugin.cs` for no-throw when REPOConfig/MenuLib absent; fix only if audit finds gap (no DBG-4 removal)
- [x] T013 [US2] Confirm `HarmonyPatchCompat.IsMasterClient()` and `DreadConfig.CompatibilityMode` gating in `Plugin.cs` match ADR-0004 and compatibility guide (code comment only if already correct)

**Checkpoint**: US2 docs and audit complete; manual matrix ready for PR author

---

## Phase 5: User Story 3 - Agents and debug tools see a stable extension model (Priority: P2)

**Goal**: Single ADR for extension model; agent docs point at contracts and registry

**Independent Test**: Open `docs/adr/0016-arch-3-extension-model.md`; links from CONTEXT, mod-architecture, guides README

### Implementation for User Story 3

- [x] T014 [P] [US3] Create `docs/adr/0016-arch-3-extension-model.md` (boot order, registry, compat deferral of ARCH-4) per FR-007
- [x] T015 [P] [US3] Update `CONTEXT.md` **System initializer** and **Compat layer** entries to link ADR and contracts
- [x] T016 [US3] Rewrite `docs/agents/guides/mod-architecture.md` section "Adding a new runtime system" to use registry + [contracts/system-lifecycle.md](./contracts/system-lifecycle.md)
- [x] T017 [US3] Add ADR and `specs/002-arch-3-extensible-core/` links in `docs/agents/guides/README.md`
- [x] T018 [P] [US3] Document supported `DreadRuntimeState` fields for overlay/MCP in `docs/agents/guides/debug-overlay.md` or ADR appendix (no new reflection in debug paths)

**Checkpoint**: US3 documentation complete

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Format, release notes, final verification (depends on Phases 2-5)

- [x] T019 Run `dotnet format --verify-no-changes --no-restore` on touched C# files
- [x] T020 Add ARCH-3 entry under `[Unreleased]` in `CHANGELOG.md` (registry, fail-safe init, verify guard, docs; no version bump)
- [x] T021 Set ARCH-3 to `done` in `docs/ROADMAP.md` when PR merges; confirm `in-progress` during implementation
- [x] T022 Record manual compat matrix results in PR description per [quickstart.md](./quickstart.md) (SC-003)
- [x] T023 Run `pwsh -NoProfile ./scripts/verify-dread.ps1` and confirm Tier 0 passes including new ARCH-3 check
- [x] T024 Run `dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj -c Release --nologo`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Start immediately
- **Phase 2 (Foundational)**: After Phase 1; **blocks T007-T009 and final T023**
- **Phase 3 (US1)**: After Phase 2; T008 probe optional before T009
- **Phase 4 (US2)**: After Phase 2 (docs/audit); can overlap Phase 5 after T006 confirms Plugin thin
- **Phase 5 (US3)**: After T004-T005 (ADR references registry); **T014-T018** parallel with US2 docs
- **Phase 6 (Polish)**: After Phases 3-5 checkpoints

### User Story Dependencies

- **US1 (registry)**: Requires Phase 2; drives SC-001, SC-002
- **US2 (compat)**: Mostly docs/audit; independent of US3; manual matrix in Phase 6
- **US3 (ADR)**: References implemented registry; best after T005, parallel with US2

### Recommended execution order (single developer)

1. T001-T003 (setup + baseline)
2. T004-T005 (registry + initializer)
3. T006-T009 (US1 Plugin check, verify guard, probe validation, stub build)
4. T014-T018 (US3 docs) in parallel with T010-T013 (US2 compat)
5. T019-T024 (polish + final verify + matrix notes)

### Parallel Opportunities

- **T010** and **T014** (compat guide vs ADR) after T005
- **T015**, **T016**, **T017** (CONTEXT, mod-architecture, README) after T014 draft
- **T018** (runtime state docs) parallel with US2 audit tasks

### Parallel Example: After registry (T005)

```bash
# Agent A: T007-T009 US1 verify + probe
# Agent B: T010-T013 US2 compat docs/audit
# Agent C: T014-T018 US3 ADR and cross-links
```

---

## Implementation Strategy

### MVP First (registry + stub CI)

1. Phase 1-2: T001-T005 (setup + registry + initializer)
2. Phase 3: T006-T009 (thin Plugin, verify guard, stub build)
3. Phase 6: T023-T024 (final Tier 0 + ErrorReportJson)
4. Stop for early review if registry-only PR desired before full doc slice

### Full ARCH-3 delivery

1. Complete MVP above
2. Phase 4-5: T010-T018 (compat matrix + ADR + agent index)
3. Phase 6: T019-T022 (format, CHANGELOG, ROADMAP, manual matrix in PR)

### Incremental PR strategy

- **PR A**: T004-T009 + T023 (registry + verify guard)
- **PR B**: T010-T018 + T019-T022 (docs + polish)

Or single PR `Fixes #175` with full task list.

---

## Notes

- Reference roadmap ID **ARCH-3** and issue **#175** in PR body
- Do not remove `RepoConfigSliderLabelCompat` (DBG-4); no ARCH-4 public API in this feature
- No manual version bump in `manifest.json` or `Plugin.VERSION`
- Prefer `export SPECIFY_FEATURE=002-arch-3-extensible-core` for speckit commands
