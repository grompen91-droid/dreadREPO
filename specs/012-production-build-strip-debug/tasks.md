---
description: "Task list for production CD builds that exclude debug tooling from Dread.dll"
---

# Tasks: Production build strip debug (012)

**Input**: Design documents from `specs/012-production-build-strip-debug/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Branch**: `012-production-build-strip-debug`

**Tests**: No automated play-mode tests (constitution Principle II). Tier 0 stub build + post-build string check + manual matrices in [quickstart.md](./quickstart.md).

**MVP scope**: Phase 1-3 (US1 compile-time production profile). US2-US4 complete pipeline and docs.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

**Purpose**: Branch, baseline verify, pin feature directory.

- [x] T001 Confirm branch `012-production-build-strip-debug` and `.specify/feature.json` points to `specs/012-production-build-strip-debug`
- [x] T002 [P] Run Tier 0 baseline per [quickstart.md](./quickstart.md) Matrix 1 steps 1-3 on current master (pre-change) and record pass/fail *(pre-change master: Release build included debug types; baseline documented as motivation for 012)*
- [x] T003 [P] Read [research.md](./research.md) `#if` inventory; confirm file list matches repo before editing

**Checkpoint**: Baseline documented; branch ready.

---

## Phase 2: Foundational - MSBuild production profile + `#if` bridges (US1)

**Purpose**: Core build switch and shared preprocessor guards. Csproj (T004) and `#if` bridges (T007–T012) land together before pipeline edits; Release production build verifies only after both.

**CRITICAL**: No workflow or verify script changes until T004 and T007–T012 compile both profiles.

- [x] T004 Add `DreadDebug`, `DefineConstants`, and conditional `Compile Remove` to [Dread.csproj](../../Dread.csproj) per [contracts/build-profile.md](./contracts/build-profile.md)
- [x] T007 [US1] Wrap debug config fields and Bind calls (sections 8, 9, 11) in `#if DREAD_DEBUG` in [Config/DreadConfig.cs](../../Config/DreadConfig.cs)
- [x] T008 [US1] Wrap debug `SystemRegistration` rows in `#if DREAD_DEBUG` in [Systems/DreadSystemRegistry.cs](../../Systems/DreadSystemRegistry.cs) per [contracts/debug-registry-conditional.md](./contracts/debug-registry-conditional.md)
- [x] T009 [P] [US1] Wrap `NotifyDebug` and call sites in `#if DREAD_DEBUG` in [Systems/CampLureSystem.cs](../../Systems/CampLureSystem.cs)
- [x] T010 [P] [US1] Wrap overlay-only notification block in `#if DREAD_DEBUG` in [Systems/SnitchSystem.cs](../../Systems/SnitchSystem.cs)
- [x] T011 [P] [US1] Wrap `ForceEpisodeForDebug()` in `#if DREAD_DEBUG` in [Systems/PsychoticBreak/PsychoticBreakSystem.cs](../../Systems/PsychoticBreak/PsychoticBreakSystem.cs)
- [x] T012 [P] [US1] Wrap `ReportTestCrashAndWait` in [Systems/ErrorReporting/ErrorReporterSystem.cs](../../Systems/ErrorReporting/ErrorReporterSystem.cs) and `BuildTestCrashReport` in [Systems/ErrorReporting/ErrorReportPayloadCapture.cs](../../Systems/ErrorReporting/ErrorReportPayloadCapture.cs)
- [x] T005 [P] Verify Release production build: `dotnet build -c Release -p:EnableDebugFeatures=false -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs`
- [x] T006 [P] Verify Debug build: `dotnet build -c Debug -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs`

**Checkpoint**: Both profiles compile.

---

## Phase 3: User Story 1 - Production verification (Priority: P1)

**Goal**: Confirm production DLL excludes debug systems (Matrix 1).

**Independent Test**: [quickstart.md](./quickstart.md) Matrix 1.

### Implementation for User Story 1

- [x] T013 [US1] Run Matrix 1 string check on `bin/Release/net48/Dread.dll`; confirm no debug type names

**Checkpoint**: US1 complete; production stub build green.

---

## Phase 4: User Story 2 - CD and CI enforce production (Priority: P1)

**Goal**: Pipeline explicitly builds and verifies production profile.

**Independent Test**: CI green; CD build step includes verify (simulate locally).

### Implementation for User Story 2

- [x] T014 [US2] Add `-p:EnableDebugFeatures=false` to build step in [.github/workflows/cd.yml](../../.github/workflows/cd.yml)
- [x] T015 [US2] Add post-build debug-type string verify step to CD build job in [.github/workflows/cd.yml](../../.github/workflows/cd.yml) per [contracts/build-profile.md](./contracts/build-profile.md)
- [x] T016 [P] [US2] Add `-p:EnableDebugFeatures=false` and optional verify to build job in [.github/workflows/ci.yml](../../.github/workflows/ci.yml)
- [x] T017 [P] [US2] Document Release=production and add optional `-DebugBuild` switch in [build.ps1](../../build.ps1)

**Checkpoint**: US2 complete; local Matrix 1 + CI config aligned.

---

## Phase 5: User Story 3 - Developer builds retain debug (Priority: P2)

**Goal**: Debug configuration preserves MCP/agent workflows.

**Independent Test**: [quickstart.md](./quickstart.md) Matrix 2 and Matrix 4.

### Implementation for User Story 3

- [x] T018 [US3] Confirm Debug build includes debug type names via `strings bin/Debug/net48/Dread.dll`
- [x] T019 [P] [US3] Update production vs dev build commands in [docs/agents/guides/debug-tooling.md](../../docs/agents/guides/debug-tooling.md)
- [x] T020 [P] [US3] Footnote debug config sections as dev-build-only in [README.md](../../README.md) and [THUNDERSTORE_README.md](../../THUNDERSTORE_README.md) (no version badge edits)

**Checkpoint**: US3 documented; Debug build verified.

---

## Phase 6: User Story 4 - Verify script and contracts (Priority: P2)

**Goal**: Tier 0 passes on production builds with conditional registry check.

**Independent Test**: `./scripts/verify-dread.ps1` on Release build.

### Implementation for User Story 4

- [x] T021 [US4] Split `arch3_registry_manifest` in [scripts/verify-dread.ps1](../../scripts/verify-dread.ps1): core types always; debug types when `#if DREAD_DEBUG` in registry or `-RequireDebugRegistry`
- [x] T022 [P] [US4] Add debug-build-only note to [specs/002-arch-3-extensible-core/contracts/extension-registry.md](../002-arch-3-extensible-core/contracts/extension-registry.md)
- [x] T023 [P] [US4] Add ADR-0013 addendum note in [docs/adr/0013-debug-server.md](../../docs/adr/0013-debug-server.md): release = compile-time exclusion

**Checkpoint**: US4 complete; Tier 0 green on Release.

---

## Phase 7: Polish and cross-cutting

**Purpose**: Changelog, format, final verify, analyzer remediation.

- [x] T024 Add `[Unreleased]` entry to [CHANGELOG.md](../../CHANGELOG.md) for production build debug stripping
- [x] T025 [P] Run `dotnet format --verify-no-changes --no-restore` (or fix formatting)
- [x] T026 Run full [quickstart.md](./quickstart.md) Matrix 1 and `./scripts/verify-dread.ps1`; record results in PR description
- [x] T027 [P] Add Thunderstore zip production verify step to [.github/workflows/cd.yml](../../.github/workflows/cd.yml) package job (SC-001)
- [x] T028 [P] Add `-p:EnableDebugFeatures=false` to [.github/workflows/smoke-test.yml](../../.github/workflows/smoke-test.yml) and [.github/workflows/codeql.yml](../../.github/workflows/codeql.yml)
- [x] T029 [P] Sync [specs/001-arch-2-reduce-reflection/contracts/build-profiles.md](../001-arch-2-reduce-reflection/contracts/build-profiles.md) and [AGENTS.md](../../AGENTS.md) stub build commands; add [docs/adr/0012-test-crash-button.md](../../docs/adr/0012-test-crash-button.md) 012 amendment
- [x] T030 Human gate: run quickstart Matrix 3 (production in-game) and Matrix 4 (Debug + MCP) before `vpatch` *(pending human)*

**Checkpoint**: Ready for review and merge; release tag blocked on T030.

---

## Dependencies

```text
Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2)
                              ↘ Phase 5 (US3) ↗
                              ↘ Phase 6 (US4) ↗
                                        → Phase 7
```

- **US1 blocks US2-US4**: Pipeline and verify assume production DLL shape.
- **US3 and US4** can run in parallel after US1.

## Parallel execution examples

**After US1 (T013)**:
- Stream A: T014-T017 (workflows + build.ps1)
- Stream B: T018-T020 (dev docs)
- Stream C: T021-T023 (verify script + ADR)

**Within US1**:
- T009, T010, T011, T012 in parallel (different files)

## Implementation strategy

1. **MVP**: Complete Phases 1-3 (T001-T013). Production DLL excludes debug; stub build green.
2. **Release-ready**: Add Phases 4-7 for CI enforcement and agent docs.
3. **Human gate**: Matrix 3 in-game before tagging `vpatch`.

## Task summary

| Phase | Tasks | Story |
|-------|-------|-------|
| Setup | T001-T003 | - |
| Foundational | T004-T006 | US1 prep |
| US1 | T007-T013 | P1 |
| US2 | T014-T017 | P1 |
| US3 | T018-T020 | P2 |
| US4 | T021-T023 | P2 |
| Polish | T024-T030 | - |

**Total**: 30 tasks (29 complete; T030 human gate pending)

**Per story**: US1=7, US2=4, US3=3, US4=3 (+10 setup/foundational/polish/analyzer)

**Suggested MVP**: T001-T013 (13 tasks)

## Verification record (T026)

- Release production build: pass (0 errors, stubs)
- `strings Dread.dll`: no DebugServerSystem, TestCrashSystem, DebugOverlaySystem
- Debug build: DebugServerSystem present
- `./scripts/verify-dread.ps1 -SkipMcpBuild`: tier0 ok=true
- `dotnet test` ErrorReportJson: pass
- Manual Matrix 3/4: pending human in-game check before release tag (T030)

## Analyzer remediation (post `/speckit-analyze`)

- spec/plan status updated to complete (pending Matrix 3/4)
- CD package job verifies Thunderstore zip Dread.dll + no dread-mcp-server in zip
- smoke-test, codeql, AGENTS.md, ARCH-2 build-profiles, ADR-0012 aligned with production profile
