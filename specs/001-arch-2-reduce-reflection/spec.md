# Feature Specification: ARCH-2 Reduce reflection and DLL surface

**Feature Branch**: `001-arch-2-reduce-reflection`

**Created**: 2026-05-30

**Status**: Draft

**Roadmap**: ARCH-2 (P1) | **Issue**: [#168](https://github.com/grompen91-droid/dreadREPO/issues/168)

**Input**: User description: "ARCH-2: Reduce DLL and reflection dependencies. Shrink reflection-heavy paths and document stub vs full game-DLL builds."

## User Scenarios & Testing

### User Story 1 - Maintainer builds without game install (Priority: P1)

A contributor or CI agent builds Dread on Linux using generated stubs and gets a passing Release build without a local R.E.P.O. `Managed` folder.

**Why this priority**: CI and cloud agents depend on stub builds; ARCH-2 must not break this path.

**Independent Test**: Run `gen-stubs.ps1` + `dotnet build` with `GameDir` pointing at `.github/stubs/refs`; `verify-dread.ps1` Tier 0 passes.

**Acceptance Scenarios**:

1. **Given** no game Managed DLLs, **When** stub Release build runs, **Then** build succeeds with zero errors.
2. **Given** stub build output, **When** `verify-dread.ps1` runs, **Then** all Tier 0 checks pass.

---

### User Story 2 - Developer builds against real game DLLs (Priority: P1)

A developer with R.E.P.O. installed builds with `GameDir` set to the game's `Managed` folder and gets stronger compile-time checking where types exist in stubs and game assemblies align.

**Why this priority**: Full builds catch API drift before runtime.

**Independent Test**: Local `dotnet build` with real `GameDir`; mod loads in r2modman profile.

**Acceptance Scenarios**:

1. **Given** valid `GameDir`, **When** Release build runs, **Then** build succeeds.
2. **Given** full build deployed to profile, **When** game starts, **Then** Dread loads and systems initialize (smoke).

---

### User Story 3 - Documented reflection inventory (Priority: P2)

Maintainers can read which reflection call sites remain, why each is required, and whether it runs on hot paths.

**Why this priority**: ARCH-2 acceptance requires an inventory with rationale; prevents blind removal.

**Independent Test**: `docs/agents/guides/reflection-inventory.md` (or equivalent) lists every categorized site; reviewed in PR.

**Acceptance Scenarios**:

1. **Given** ARCH-2 PR, **When** reviewer opens inventory doc, **Then** each remaining reflection site has file, purpose, stub/full behavior, and hot-path flag.

---

### Edge Cases

- REPOConfig / MenuLib not loaded: optional-mod paths must no-op without throwing.
- Psychotic break overlay: Unity UI types may be absent at compile time; runtime reflection may remain required on stub builds.
- Harmony patches: `AccessTools.TypeByName` when game types are missing from stub assemblies.
- Error reporter payload capture: reflection on game objects during log flush must not regress dedupe or opt-out.

## Requirements

### Functional Requirements

- **FR-001**: Produce and maintain a **reflection inventory** covering all `Systems/` reflection and Harmony `AccessTools` resolution sites, with rationale and hot-path classification.
- **FR-002**: **Prefer compile-time references** for types present in stub or game assemblies where removal of reflection does not break optional-mod or stub builds.
- **FR-003**: **Reduce hot-path reflection** where ARCH-1 file layout makes safe extraction obvious (e.g. debug overlay patch count already gated on visibility).
- **FR-004**: **Document stub vs full build** in agent docs: commands, MSBuild properties, what features rely on reflection at runtime, and CI expectations.
- **FR-005**: **No player-facing behavior change** unless a reflection removal fixes an existing bug (must be called out in CHANGELOG).

### Key Entities

- **Build profile**: Stub (CI/cloud) vs Full (local game `Managed`).
- **Reflection site**: File, method, trigger (startup / per-frame / event), optional-mod gate.
- **Compile-time surface**: Types and methods referenced directly in C# vs resolved at runtime.

## Success Criteria

- **SC-001**: Linux stub CI build and `verify-dread.ps1` Tier 0 pass on ARCH-2 branch.
- **SC-001**: Reflection inventory merged with zero "unknown" sites.
- **SC-003**: At least one measurable reduction: fewer per-frame reflection call sites **or** documented justification for each remaining hot-path site.
- **SC-004**: `docs/agents/guides/mod-architecture.md` (or new guide) describes stub vs full build in under 5 minutes read time.

## Assumptions

- ARCH-1 file split is merged or available on the implementation branch (soft dependency).
- Stub assemblies under `.github/stubs/refs/` remain the CI source of truth until game DLLs are vendored differently.
- Optional mods (REPOConfig, MenuLib) stay soft dependencies; hard references only behind `#if` or compile guards if ever introduced.
- `RepoConfigSliderLabelCompat` and psychotic break UI reflection may remain **required** on stub builds in v1 of ARCH-2; goal is inventory + targeted wins, not zero reflection.

## Out of Scope

- ARCH-3 initializer registry and fail-safe init.
- ARCH-4 external mod API.
- Removing `RepoConfigSliderLabelCompat` (DBG-4 / upstream).
- Vendoring full game `Managed` DLLs into the repo.
