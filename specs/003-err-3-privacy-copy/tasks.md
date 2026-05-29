# Tasks: ERR-3 Error reporting privacy copy

**Input**: Design documents from `/specs/003-err-3-privacy-copy/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/privacy-copy.md](./contracts/privacy-copy.md), [contracts/config-keys.md](./contracts/config-keys.md)

**Tests**: No new unit test suite (ERR-1 owns `tests/Dread.ErrorReportJson.Tests`). Verification: manual copy review per [contracts/privacy-copy.md](./contracts/privacy-copy.md), Tier 0 per [quickstart.md](./quickstart.md).

**Organization**: Phases follow user stories. **US2 canonical module (T006-T008) blocks US1 config wiring (T009+).**

**Out of scope**: ERR-2 default-on, first-run modal, `manifest.json` / `Plugin.cs` version bumps, Worker/schema changes, new telemetry fields.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 per [spec.md](./spec.md)

## Path Conventions

- Repository root: `Config/`, `Systems/ErrorReporting/`, `docs/adr/`, `docs/agents/guides/`
- Spec kit: `specs/003-err-3-privacy-copy/`

## Requirements map

| ID | Tasks |
|----|-------|
| FR-001 | T006 |
| FR-002 | T006, T011 |
| FR-003 | T006, T009 |
| FR-004 | T009 |
| FR-005 | T011, T016 (contract exists; review) |
| FR-006 | T015 |
| FR-007 | T007, T017 |
| SC-001 | T011, T016 |
| SC-002 | T010, T018 |
| SC-003 | T002, T020 |
| SC-004 | T017 |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Branch hygiene and baseline before copy changes

- [ ] T001 Rebase `003-err-3-privacy-copy` onto `origin/master` and confirm ERR-1 [#171] test matrix is green on merge base
- [ ] T002 Run baseline Tier 0 per [quickstart.md](./quickstart.md): `pwsh -NoProfile .github/scripts/gen-stubs.ps1`, stub `dotnet build` on `Dread.csproj`, `pwsh -NoProfile ./scripts/verify-dread.ps1`, `dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj -c Release --nologo`
- [ ] T003 Record baseline pass/fail in PR description for issue [#173](https://github.com/grompen91-droid/dreadREPO/issues/173)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Payload truth and config contract aligned before writing player copy

**CRITICAL**: Do not start T006 until T004-T005 complete

- [ ] T004 Draft payload-to-bullet mapping in PR notes from `Systems/ErrorReporting/ErrorReportPayloadCapture.cs`, `Systems/ErrorReporting/ErrorReportTypes.cs`, and `docs/adr/0010-error-telemetry.md` per [data-model.md](./data-model.md) `PayloadCategory` rows
- [ ] T005 [P] Verify [contracts/config-keys.md](./contracts/config-keys.md) matches `Config/DreadConfig.cs` (`5. Error Reporting`, `ErrorReportingEnabled`, default `false`)

**Checkpoint**: Truth table ready; canonical copy can be authored

---

## Phase 3: User Story 2 - Maintainer has one canonical copy source (Priority: P1)

**Goal**: Single module holds all player-facing error reporting disclosure strings (FR-005, US2)

**Independent Test**: `rg` shows disclosure strings originate from `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` (plus `Config/DreadConfig.cs` bind reference only)

### Implementation for User Story 2

- [ ] T006 [US2] Add `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` with `FullDescription`, `ShortSummary`, `DisableInstructions`, and bullet text covering FR-001 through FR-003 per [contracts/privacy-copy.md](./contracts/privacy-copy.md) rows 1-10 (no Unity API at type load)
- [ ] T007 [US2] Document ERR-2 consumption API on `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` (which constants ERR-2 [#172] should import; no modal code in ERR-3) per FR-007
- [ ] T008 [US2] Run `rg` across repo for error-reporting disclosure phrases; confirm no duplicate player-facing copy outside `ErrorReportingPrivacyCopy.cs` and `Config/DreadConfig.cs`

**Checkpoint**: Canonical strings exist; US1 wiring can proceed

---

## Phase 4: User Story 1 - Player understands telemetry before enabling (Priority: P1) MVP

**Goal**: `ErrorReportingEnabled` config description is accurate, complete, and disable path is clear (FR-004, US1)

**Independent Test**: Generated `BepInEx/config/elytraking.dread.cfg` section `5. Error Reporting` shows full description; default `ErrorReportingEnabled = false`

### Copy review for User Story 1

- [ ] T009 [US1] Wire `Config/DreadConfig.cs` `ErrorReportingEnabled` `ConfigDescription` to `ErrorReportingPrivacyCopy.FullDescription` (FR-004)
- [ ] T010 [US1] Confirm `ErrorReportingEnabled` bind default remains `false` in `Config/DreadConfig.cs` (SC-002; no ERR-2 default change)
- [ ] T011 [US1] Complete [contracts/privacy-copy.md](./contracts/privacy-copy.md) required bullets 1-10 and pre-merge checklist in PR description with pass/fail per row (SC-001)
- [ ] T012 [P] [US1] Manual review: copy states opt-in default off, does not promise instant GitHub issue when offline, and does not blame third-party mods per [spec.md](./spec.md) edge cases

**Checkpoint**: US1 satisfied; cfg is primary v1 player surface

---

## Phase 5: User Story 3 - Optional in-game visibility without ERR-2 (Priority: P2)

**Goal**: Optional minimal in-game path without first-run modal or default change (US3)

**Independent Test**: If T013 skipped, PR notes US3 deferred; ERR-2 can still use canonical strings from Phase 3

### Implementation for User Story 3

- [ ] T013 [US3] OPTIONAL: On first transition to `ErrorReportingEnabled = true`, log one `LoggingService` info line pointing to `BepInEx/config/elytraking.dread.cfg` section 5 using `ErrorReportingPrivacyCopy` strings per [research.md](./research.md) (no load spam; no default change)

**Checkpoint**: US3 optional slice complete or explicitly deferred in PR

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Docs alignment, contract checklist execution, CI verify

- [ ] T014 [P] Update `docs/agents/guides/error-reporting.md` with link to `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` and canonical module path `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs`
- [ ] T015 [P] Align `README.md` and `THUNDERSTORE_README.md` error reporting paragraphs with ADR-0010 `Application.logMessageReceived` pipeline; fix stale Harmony / `Debug.LogError` wording only in touched sections (FR-006)
- [ ] T016 Execute full [contracts/privacy-copy.md](./contracts/privacy-copy.md) pre-merge checklist: side-by-side `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` + `docs/adr/0010-error-telemetry.md`, `dotnet test tests/Dread.ErrorReportJson.Tests`, `ErrorReportLogQueue` opt-out when false, README pipeline wording if touched, no em dash in edited markdown, no ERR-2 default/prompt in diff
- [ ] T017 Add ERR-2 [#172] integration subsection in `docs/agents/guides/error-reporting.md` referencing `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` for modal body (SC-004, FR-007)
- [ ] T018 Add ERR-3 entry under `[Unreleased]` in `CHANGELOG.md` (privacy copy, cfg disclosure; no version bump)
- [ ] T019 Run `dotnet format --verify-no-changes --no-restore` on touched C# files
- [ ] T020 Re-run [quickstart.md](./quickstart.md) Tier 0 stub build, `verify-dread.ps1`, and `dotnet test tests/Dread.ErrorReportJson.Tests` after all changes (SC-003)

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Start immediately
- **Phase 2 (Foundational)**: After Phase 1; **blocks T006-T008**
- **Phase 3 (US2)**: After Phase 2; **blocks T009-T012**
- **Phase 4 (US1)**: After Phase 3 checkpoint
- **Phase 5 (US3)**: After Phase 4 (optional T013)
- **Phase 6 (Polish)**: After Phase 4 minimum; T020 after all code/doc edits

### User Story Dependencies

- **US2 (canonical)**: Must complete before US1 config bind
- **US1 (player cfg)**: Depends on US2; delivers MVP
- **US3 (optional log)**: Depends on US2 strings; independent of US1 acceptance except shared module

### Recommended execution order (single developer)

1. T001-T003 (setup + baseline)
2. T004-T005 (payload mapping + config contract)
3. T006-T008 (canonical module + grep audit)
4. T009-T012 (DreadConfig wire + copy review)
5. T013 if doing US3 optional line
6. T014-T020 (docs, checklist, changelog, format, final verify)

### Parallel Opportunities

- **T005** with T004 (different artifacts)
- **T012** with T014-T015 after T009 (manual review vs docs)
- **T014** and **T015** (different files)

### Parallel Example: Foundational

```bash
# After T003 baseline:
# Agent A: T004 payload mapping in PR notes
# Agent B: T005 config-keys vs DreadConfig.cs
```

### Parallel Example: Polish docs

```bash
# After T011 checklist draft:
# Agent A: T014 error-reporting.md
# Agent B: T015 README.md + THUNDERSTORE_README.md
```

---

## Implementation Strategy

### MVP First (US2 + US1 only)

1. Phase 1-2: Setup, baseline, payload truth (T001-T005)
2. Phase 3: **T006-T008** canonical `ErrorReportingPrivacyCopy.cs`
3. Phase 4: **T009-T012** wire `Config/DreadConfig.cs` + contract checklist in PR
4. Phase 6: **T016, T018-T020** minimum polish (checklist, changelog, Tier 0)
5. **STOP**: Ship copy-only PR; defer T013, T015 if README not stale enough to block merge

### Full ERR-3 delivery

1. Complete MVP
2. T013 optional US3 log line (or document deferral)
3. T014-T017 agent guide + ERR-2 handoff note
4. T015 README/THUNDERSTORE alignment

### Incremental PR strategy

- **PR A**: T006-T012 + T016-T020 (code + cfg disclosure + verify)
- **PR B** (optional): T013-T015 doc-only follow-up

Or single PR `Fixes #173` with full task list.

---

## Notes

- Roadmap ID **ERR-3**, issue **#173**; blocks ERR-2 [#172] until merged
- Do not change `ErrorReportingEnabled` default to `true` or add first-run prompt (ERR-2)
- Do not add telemetry unit tests; rely on ERR-1 `tests/Dread.ErrorReportJson.Tests`
- Worker edge IP is not in client JSON; do not claim IP in payload bullets
- English-only copy; no em dash in edited markdown per `AGENTS.md`
