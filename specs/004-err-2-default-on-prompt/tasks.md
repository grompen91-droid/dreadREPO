---
description: "Task list for ERR-2 default-on error reporting + first-run prompt + Core EnemyHealth capture fix"
---

# Tasks: ERR-2 Error reporting default on + first-run prompt

**Input**: Design documents from `/home/arch/Documents/Projects/ai/repo/dreadREPO/specs/004-err-2-default-on-prompt/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Branch**: `004-err-2-default-on-prompt` | **Issue**: [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)

**Tests**: No automated test tasks (spec does not request TDD). Tier 0 stub build, `Dread.ErrorReportJson.Tests`, and manual matrices in [quickstart.md](./quickstart.md) are verification tasks.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3) for story phases only
- Every task includes an exact file path

## Path Conventions

- BepInEx plugin at repository root: `Config/`, `Systems/`, `docs/`, `tests/`
- Feature specs: `specs/004-err-2-default-on-prompt/`
- ERR-3 dependency: `specs/003-err-3-privacy-copy/contracts/privacy-copy.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm branch, ERR-3 merge base, and green Tier 0 baseline before code changes.

- [x] T001 Verify checkout on branch `004-err-2-default-on-prompt` and `SPECIFY_FEATURE=004-err-2-default-on-prompt` per `specs/004-err-2-default-on-prompt/quickstart.md`
- [x] T002 [P] Confirm ERR-3 merged on merge base: `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` and `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` exist and match expected API
- [x] T003 [P] Run Tier 0 stub baseline (gen-stubs, Release build, `scripts/verify-dread.ps1`) per `specs/004-err-2-default-on-prompt/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Config keys, consent helper, and canonical copy that all user stories depend on.

**CRITICAL**: No user story implementation until this phase completes.

- [x] T004 Add `ErrorReportingPromptShown` `ConfigEntry<bool>` (default `false`, section `7. Error Reporting`) in `Config/DreadConfig.cs` per `specs/004-err-2-default-on-prompt/contracts/config-keys.md`
- [x] T005 Change `ErrorReportingEnabled` bind default from `false` to `true` in `Config/DreadConfig.cs` (BepInEx must retain existing saved `false` on upgrade)
- [x] T006 [P] Implement `ErrorReportingConsent.IsReportingAllowed()` in `Systems/ErrorReporting/ErrorReportingConsent.cs` per `specs/004-err-2-default-on-prompt/data-model.md`
- [x] T007 Update default-on strings (`ShortSummary`, `DataBullets`, `FullDescription`) in `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` per FR-005 in `specs/004-err-2-default-on-prompt/spec.md`
- [x] T008 [P] Update privacy contract row 10 (default-on wording) in `specs/003-err-3-privacy-copy/contracts/privacy-copy.md`

**Checkpoint**: Config + consent helper + privacy copy ready. Prompt UI and queue gates can proceed.

---

## Phase 3: User Story 1 - New player sees disclosure before reports leave (Priority: P1) MVP

**Goal**: Default-on for new cfg, one-time IMGUI prompt on first non-menu gameplay load, choice persisted, no repeat prompt, no telemetry before acknowledgment.

**Independent Test**: Delete `elytraking.dread.cfg`, launch game, load non-menu level, confirm modal once; restart with no modal; confirm `ErrorReportingEnabled` matches button; confirm no reports before `ErrorReportingPromptShown` is true.

### Implementation for User Story 1

- [x] T009 [US1] Create `ErrorReportingPromptSystem` with `OnGUI` modal, scroll view, and `FirstRunPrompt` state (`Pending`/`Visible`/`Dismissed`) in `Systems/ErrorReporting/ErrorReportingPromptSystem.cs` per `specs/004-err-2-default-on-prompt/contracts/first-run-prompt.md`
- [x] T010 [US1] Compose prompt body only from `ErrorReportingPrivacyCopy.ShortSummary`, `DataBullets`, and `DisableInstructions` in `Systems/ErrorReporting/ErrorReportingPromptSystem.cs` (no duplicate disclosure strings)
- [x] T011 [US1] Implement **Keep reporting on** and **Turn off reporting** handlers (set `ErrorReportingEnabled` and `ErrorReportingPromptShown`, save cfg) in `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`
- [x] T012 [US1] Show prompt on first non-menu scene after init (`!ErrorReportingPromptShown`, `!SemiFunc.MenuLevel()`, systems ready) via `SceneManager.sceneLoaded` in `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`
- [x] T013 [US1] Register `error-reporting-prompt` host in `Systems/DreadSystemRegistry.cs` (ARCH-3 pattern, order after core init)
- [x] T014 [US1] Ensure prompt host is created through `Systems/DreadSystemInitializer.cs` (registration always enabled; system self-gates on cfg)
- [x] T015 [US1] Replace direct `ErrorReportingEnabled` check with `ErrorReportingConsent` in `Systems/ErrorReporting/ErrorReportLogQueue.cs`
- [x] T016 [P] [US1] Apply `ErrorReportingConsent` to capture/flush/send paths in `Systems/ErrorReporting/ErrorReporterSystem.cs` where reporting can occur before prompt acknowledgment
- [x] T017 [P] [US1] Amend default-on and first-run UI sections in `docs/adr/0010-error-telemetry.md`

**Checkpoint**: User Story 1 complete. Fresh cfg manual matrix in quickstart should pass.

---

## Phase 4: User Story 2 - Upgrading player with existing cfg (Priority: P1)

**Goal**: Existing `ErrorReportingEnabled = false` is not silently flipped to true; prompt appears once for disclosure; reporting stays off until **Keep reporting on**.

**Independent Test**: Start from cfg with `ErrorReportingEnabled = false`, deploy ERR-2 DLL, load gameplay level, see prompt once; choose off, value stays false; confirm no telemetry before prompt acknowledged.

### Implementation for User Story 2

- [x] T018 [US2] Verify BepInEx retains saved `false` for `ErrorReportingEnabled` on upgrade (bind default change only affects new keys) in `Config/DreadConfig.cs`
- [x] T019 [US2] Ensure prompt button labels and behavior do not auto-enable reporting when cfg already `false` until **Keep reporting on** in `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`
- [x] T020 [US2] Confirm consent gate blocks enqueue/send when `ErrorReportingPromptShown` is false even if `ErrorReportingEnabled` is true (first session) via `Systems/ErrorReporting/ErrorReportingConsent.cs` and logs
- [x] T021 [US2] Execute manual **upgrade path** section in `specs/004-err-2-default-on-prompt/quickstart.md` (false retained, prompt once, no pre-prompt sends)

**Checkpoint**: User Stories 1 and 2 both satisfied for new and upgraded profiles.

---

## Phase 5: User Story 3 - Player can change mind later (Priority: P2)

**Goal**: After prompt, toggling `ErrorReportingEnabled` via cfg/REPOConfig/Configuration Manager uses existing pipeline and ERR-3 disclosure text.

**Independent Test**: After prompt completion, set `ErrorReportingEnabled = false` in cfg; confirm queue does not enqueue; set `true` and confirm ADR-0010 behavior.

### Implementation for User Story 3

- [x] T022 [US3] Verify `ErrorReportingPromptShown` is not reset when player edits `ErrorReportingEnabled` in cfg in `Config/DreadConfig.cs` / `Systems/ErrorReporting/ErrorReportingPromptSystem.cs`
- [x] T023 [P] [US3] Confirm REPOConfig compat path for `ErrorReportingEnabled` unchanged (no prompt API) via existing `RepoConfigCompat` / `Config/DreadConfig.cs` wiring
- [x] T024 [US3] Execute manual **later opt-out** section in `specs/004-err-2-default-on-prompt/quickstart.md`

**Checkpoint**: Post-prompt opt-out/opt-in matches ADR-0010.

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Player-facing docs, changelog, CI verification, and issue #172 acceptance grep.

- [x] T025 [P] Add ERR-2 entry under `[Unreleased]` in `CHANGELOG.md`
- [x] T026 [P] Update error reporting default-on + first-run prompt bullets in `README.md`
- [x] T027 [P] Update error reporting bullets in `THUNDERSTORE_README.md`
- [x] T028 [P] Update error reporting compatibility notes in `docs/mod-compatibility.md`
- [x] T029 Run Tier 0 (`scripts/verify-dread.ps1`) and `dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj` per `specs/004-err-2-default-on-prompt/quickstart.md`
- [x] T030 Run full manual matrix (first-run, upgrade, later opt-out) in `specs/004-err-2-default-on-prompt/quickstart.md`
- [x] T031 Grep repo: player-visible disclosure strings only in `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` (prompt must not duplicate categories)
- [x] T032 Confirm feature PR does not bump `manifest.json` or `Plugin.cs` version strings
- [x] T033 [P] Touch `docs/agents/guides/error-reporting.md` only if implementation diverges from ADR (link ERR-2 prompt + consent gate)

---

## Phase 7: Core compat + error capture fix (FR-010 / FR-011)

**Purpose**: Fix `[ErrorReporter] Failed to process pending logs: Method not found: int .EnemyHealth.get_CurrentHealth()` when third-party mods log errors (e.g. DeathMinimap after death). Consolidate compat helpers under `Systems/Core/`.

**Prerequisites**: Phases 1-6 complete (ERR-2 prompt + consent shipped). See [contracts/core-enemy-health.md](./contracts/core-enemy-health.md) and Core EnemyHealth plan adjunct.

**Independent Test**: With error reporting enabled and prompt acknowledged, trigger DeathMinimap NRE (die in run with DeathMinimap installed). BepInEx log shows DeathMinimap stack but **no** Dread `get_CurrentHealth` warning; pending error batch processes without `Failed to process pending logs` for that reason.

- [x] T034 Append FR-010 and FR-011 plus SC-005/SC-006 to `specs/004-err-2-default-on-prompt/spec.md` per Core capture plan adjunct
- [x] T035 [P] Add Core capture decision to `specs/004-err-2-default-on-prompt/research.md` and note `GameState` capture uses Core compat in `specs/004-err-2-default-on-prompt/data-model.md`
- [x] T036 Create `Systems/Core/` and `git mv` seven compat files (`EnemyHealthCompat`, `PlayerControllerCompat`, `PlayerTumbleCompat`, `HarmonyPatchCompat`, `RepoConfigCompat`, `RepoConfigSliderLabelCompat`, `UnityWebRequestCompat`) to `Systems/Core/` with namespace `Dread.Systems.Core`; update `Dread.csproj` if needed
- [x] T037 Add `using Dread.Systems.Core` and fix references in `Plugin.cs`, `Systems/DreadSystemInitializer.cs`, `Systems/EnemyScanCache.cs`, `Systems/Patches/EnemyNavMeshAgentAwakePatch.cs`, `Systems/Patches/EnemyDirectorSetInvestigatePatch.cs`, `Systems/PsychoticBreak/PsychoticBreakTrigger.cs`, and any other consumers found by build
- [x] T038 Extend `Systems/Core/EnemyHealthCompat.cs` with `TryReadHealth`, `TryIsAlive`, and `CountAliveAndNearby` per `specs/004-err-2-default-on-prompt/contracts/core-enemy-health.md`
- [x] T039 Refactor `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` `CaptureGameState` to use `EnemyScanCache.GetEnemies()` and `EnemyHealthCompat.CountAliveAndNearby` (remove `e.CurrentHealth`)
- [x] T040 Refactor nearest-enemy loop in `Systems/DebugServerSystem.cs` to use `EnemyHealthCompat.TryIsAlive` instead of `e.CurrentHealth`
- [x] T041 [P] Update `docs/agents/guides/reflection-inventory.md` paths and `error-payload-game-state` disposition in `docs/agents/guides/reflection-inventory.md`
- [x] T042 [P] Add **Systems/Core** section to `docs/agents/guides/mod-architecture.md` and compat pointer in `CONTEXT.md`
- [x] T043 [P] Add Fixed entry under `[Unreleased]` in `CHANGELOG.md` for error reporter game-state capture when `EnemyHealth` API differs from stubs
- [x] T044 Add DeathMinimap / error-capture manual steps to `specs/004-err-2-default-on-prompt/quickstart.md` (no `get_CurrentHealth` warning after third-party NRE)
- [x] T045 Run Tier 0 (`scripts/verify-dread.ps1`) and `dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj` per `specs/004-err-2-default-on-prompt/quickstart.md`
- [x] T046 Grep `Systems/` for `\.CurrentHealth` in `*.cs` (expect zero hits) per `specs/004-err-2-default-on-prompt/contracts/core-enemy-health.md`
- [ ] T046b Execute Phase 7 manual matrix in `specs/004-err-2-default-on-prompt/quickstart.md` (DeathMinimap NRE + no `get_CurrentHealth` warning; SC-005/SC-006)

**Checkpoint**: Error reporting survives DeathMinimap (and similar) Unity errors without dropping batches due to stub `CurrentHealth` mismatch.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. Start immediately.
- **Foundational (Phase 2)**: Depends on Setup (especially T002 ERR-3 merge). **Blocks all user stories.**
- **User Story 1 (Phase 3)**: Depends on Foundational. **MVP scope.**
- **User Story 2 (Phase 4)**: Depends on US1 prompt + consent gate (validates upgrade behavior).
- **User Story 3 (Phase 5)**: Depends on US1 consent gate; independent of US2 manual matrix.
- **Polish (Phase 6)**: Depends on desired user stories complete (minimum US1 + US2 for #172).
- **Core capture (Phase 7)**: Depends on Phase 6 (ERR-2 functional). Can ship in same PR as ERR-2 or immediately after merge.

### User Story Dependencies

| Story | Depends on | Can parallelize with |
|-------|------------|----------------------|
| US1 | Foundational | None (core implementation) |
| US2 | US1 prompt + consent | US3 after US1 gate exists |
| US3 | US1 gate | US2 manual verification |

### Within Each User Story

- Config and `ErrorReportingConsent` before prompt UI and queue gates
- Registry registration after `ErrorReportingPromptSystem` compiles
- Manual quickstart sections after implementation tasks for that story

### Parallel Opportunities

- **Setup**: T002 and T003 in parallel after T001
- **Foundational**: T006 and T008 parallel with T007 after T004-T005
- **US1**: T016 and T017 parallel after T015; T009-T014 are mostly sequential (same new file)
- **Polish**: T025-T028 and T033 parallel; T029-T032 sequential verification
- **Phase 7**: T035 parallel with T034; T041-T043 parallel after T039-T040; T036-T037 sequential before T038-T040

---

## Parallel Example: Phase 7

```bash
# After T034 spec/research updates:
# T036-T037: Systems/Core/ move + consumer usings (sequential)
# T038: Systems/Core/EnemyHealthCompat.cs API
# T039-T040: ErrorReportPayloadCapture + DebugServerSystem (parallel after T038)
# T041-T043: docs + CHANGELOG (parallel)
# T045-T046: automated verification; T046b: manual Phase 7 matrix
```

---

## Parallel Example: Foundational

```bash
# After T004-T005 land:
# Task T006: Systems/ErrorReporting/ErrorReportingConsent.cs
# Task T008: specs/003-err-3-privacy-copy/contracts/privacy-copy.md
# Task T007: Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs (coordinate copy with T008)
```

## Parallel Example: User Story 1 (late)

```bash
# After queue gate (T015):
# Task T016: Systems/ErrorReporting/ErrorReporterSystem.cs
# Task T017: docs/adr/0010-error-telemetry.md
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (through T017)
4. **STOP and VALIDATE**: Fresh cfg manual matrix in `quickstart.md`
5. Proceed to US2 upgrade verification before PR

### Incremental Delivery

1. Setup + Foundational: config, consent, copy
2. US1: prompt + gates + ADR (MVP for new installs)
3. US2: upgrade manual matrix (required for #172)
4. US3: post-prompt cfg toggle verification
5. Polish: CHANGELOG, README, CI, grep acceptance
6. Phase 7: Core folder + error capture fix (log-driven bugfix)

### Parallel Team Strategy

- Developer A: Foundational + `ErrorReportingPromptSystem.cs` (T004-T014)
- Developer B: Consent gates in queue/reporter (T015-T016) after T006
- Developer C: Docs and contracts (T008, T017, T025-T028) after copy stable

---

## Notes

- Merge **ERR-3** (PR #207) before implementation if not already on branch (T002).
- `Plugin.cs` changes only if scene/init wiring cannot live entirely in `ErrorReportingPromptSystem.cs`.
- Optional v1: block gameplay input while prompt visible (contract allows overlay-only).
- Stub CI: guard OnGUI like `Systems/DebugOverlay/DebugOverlaySystem.cs` (no missing Unity GUI APIs at type load).
- Do not add Worker, payload, or `manifest.json` / `Plugin.VERSION` changes in this feature.
- Phase 7: DeathMinimap NRE may still log from the third-party mod; Dread must not fail batch processing with `get_CurrentHealth` (see `contracts/core-enemy-health.md`).
