# Feature Specification: ERR-2 Error reporting default on + first-run prompt

**Feature Branch**: `004-err-2-default-on-prompt`

**Created**: 2026-05-30

**Status**: ERR-2 shipped on `master` (1.6.0, PR #208). This branch/PR adds **Phase 7** Core capture fix only (pending merge). Automated verification done (Tier 0, `Dread.ErrorReportJson.Tests`, grep). Manual SC-005/SC-006: [quickstart.md](./quickstart.md) Phase 7 matrix.

**Roadmap**: ERR-2 (P1) | **Issue**: [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)

**Input**: ERR-2: Change error reporting from opt-in to default-on with a one-time in-game prompt to enable or disable.

## User Scenarios & Testing

### User Story 1 - New player sees disclosure before reports leave (Priority: P1)

A player launches R.E.P.O. with Dread installed for the first time (or after resetting cfg). Error reporting defaults to **on**, but the first time they enter a gameplay level they see a one-time in-game prompt with the same disclosure as ERR-3. They choose **Keep reporting on** or **Turn off reporting**. Their choice is saved in cfg and the prompt does not appear again.

**Why this priority**: Default-on without disclosure violates roadmap gate and ADR-0010 privacy intent; issue #172 acceptance.

**Independent Test**: Delete `elytraking.dread.cfg`, launch game, load into a non-menu level, confirm modal once; restart, confirm no modal; confirm `ErrorReportingEnabled` matches button choice.

**Acceptance Scenarios**:

1. **Given** no prior `ErrorReportingPromptShown` (or equivalent) in cfg, **When** the player loads the first gameplay scene after Dread systems init, **Then** the first-run prompt appears before any new reports are required to be "surprise" (reporting may already be true by default; prompt explains and offers opt-out).
2. **Given** the player clicks **Turn off reporting**, **When** the prompt closes, **Then** `ErrorReportingEnabled` is `false` and `ErrorReportingPromptShown` is `true`.
3. **Given** the player clicks **Keep reporting on**, **When** the prompt closes, **Then** `ErrorReportingEnabled` is `true` and `ErrorReportingPromptShown` is `true`.
4. **Given** `ErrorReportingPromptShown` is `true`, **When** the player loads any later scene, **Then** the prompt does not appear again unless cfg is reset.

---

### User Story 2 - Upgrading player with existing cfg (Priority: P1)

A player who already has `elytraking.dread.cfg` from an older Dread version (default `ErrorReportingEnabled = false`) receives the one-time prompt on first gameplay load after upgrade, with buttons reflecting an honest choice (not silently flipping to true without interaction).

**Why this priority**: Breaking change for existing opt-out users; must not enable reporting without acknowledgment after upgrade.

**Independent Test**: Start from a cfg with `ErrorReportingEnabled = false`, upgrade DLL, load level, see prompt once; choose off, value stays false.

**Acceptance Scenarios**:

1. **Given** existing cfg with `ErrorReportingEnabled = false` and no prompt-shown flag, **When** first gameplay load after ERR-2, **Then** prompt appears and reporting stays **false** until the player chooses **Keep reporting on**.
2. **Given** existing cfg with `ErrorReportingEnabled = true` (user opted in early), **When** first load after ERR-2, **Then** prompt appears once for disclosure; **Keep reporting on** leaves `true`.

---

### User Story 3 - Player can change mind later (Priority: P2)

A player disables reporting in the prompt or later via REPOConfig / cfg / Configuration Manager using the same `ErrorReportingEnabled` key and ERR-3 disclosure text.

**Why this priority**: Issue acceptance and ADR-0010 opt-out path.

**Independent Test**: After prompt, toggle `ErrorReportingEnabled` in cfg; confirm `ErrorReportLogQueue` does not enqueue when false.

**Acceptance Scenarios**:

1. **Given** prompt completed, **When** player sets `ErrorReportingEnabled = false` in cfg, **Then** no reports are sent (existing pipeline).
2. **Given** prompt completed, **When** player sets `ErrorReportingEnabled = true`, **Then** reporting behaves per ADR-0010.

---

### Edge Cases

- **Menu / lobby levels**: Do not show prompt on `SemiFunc.MenuLevel()` (same gate as Psychotic Break).
- **Compatibility mode**: Prompt still allowed unless product decision blocks; reporting respects existing `ErrorReportingEnabled` and compat docs.
- **Stub CI**: Prompt system must not call missing Unity GUI APIs at type load; OnGUI path must match Debug Overlay guards.
- **Multiplayer**: Prompt is client-local; each machine uses its own cfg (BepInEx standard).
- **Offline**: Prompt copy must not promise instant delivery (reuse ERR-3 strings).

## Requirements

### Functional Requirements

- **FR-001**: Change `DreadConfig.ErrorReportingEnabled` default from `false` to `true` for **new** cfg generation.
- **FR-002**: Add `ErrorReportingPromptShown` (name may vary in implementation) default `false`; set `true` when the player dismisses the first-run prompt via either button.
- **FR-003**: Show a **one-time** in-game IMGUI prompt on the first gameplay scene load when `ErrorReportingPromptShown` is `false`, after `DreadSystemInitializer` succeeds and scene is not a menu level.
- **FR-004**: Prompt body MUST compose text from `ErrorReportingPrivacyCopy.ShortSummary`, `DataBullets`, and `DisableInstructions` without paraphrasing payload categories (ERR-3 contract).
- **FR-005**: Update canonical copy for default-on: revise `ShortSummary` and bullet 8 (and `FullDescription`) so they no longer claim "default off"; keep checklist alignment in `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` updated in same PR.
- **FR-006**: Persist enable/disable choice **only** by setting `ErrorReportingEnabled` when the user clicks a prompt button (prompt-shown flag is separate metadata).
- **FR-007**: Update **ADR-0010**, **ADR-0016** (`Systems/Core/` section), **CHANGELOG [Unreleased]**, **README**, **THUNDERSTORE_README**, and **docs/mod-compatibility.md** for default-on + first-run prompt (no `manifest.json` / `Plugin.VERSION` bump in feature PR).
- **FR-008**: Register prompt host via `DreadSystemRegistry` (ARCH-3 pattern) or documented initializer hook; fail-safe if IMGUI unavailable.
- **FR-009**: Do not send error reports **before** prompt acknowledgment on first run. **Implemented** via `ErrorReportingConsent.IsReportingAllowed()` (queue, capture, flush, send): while `ErrorReportingPromptShown` is `false`, reporting is off regardless of `ErrorReportingEnabled`. See [data-model.md](./data-model.md) and [research.md](./research.md).
- **FR-010**: Game-state capture for error reports MUST NOT reference `EnemyHealth.CurrentHealth` at compile time; use `EnemyHealthCompat` in `Systems/Core/`.
- **FR-011**: Version-tolerant game-type helpers live under `Systems/Core/` (`Dread.Systems.Core`); feature code imports Core compat instead of duplicating reflection.

### Key Entities

- **FirstRunPrompt**: UI state machine (pending, visible, dismissed).
- **ErrorReportingPromptShown**: BepInEx bool, one-time gate.
- **ErrorReportingEnabled**: Existing bool, player choice and runtime gate.

## Success Criteria

- **SC-001**: Issue #172 acceptance checklist satisfied (once per profile, cfg persistence, ADR/README updated).
- **SC-002**: Tier 0 stub build + `dotnet test tests/Dread.ErrorReportJson.Tests` pass.
- **SC-003**: Manual: fresh cfg, prompt once, both buttons set cfg correctly; upgrade cfg with `false` does not auto-enable without **Keep reporting on**.
- **SC-004**: Grep shows prompt strings originate from `ErrorReportingPrivacyCopy` only.
- **SC-005**: After a third-party mod logs an error during play (e.g. DeathMinimap NRE after both players die), BepInEx log shows no Dread `[ErrorReporter] Failed to process pending logs` mentioning `get_CurrentHealth`. Verified per [quickstart.md](./quickstart.md) Phase 7 manual matrix (not CI-automated).
- **SC-006**: Error report payloads include `GameState` enemy counts when HP is readable via Core compat (best effort).

## Assumptions

- **ERR-1** merged: golden JSON and Worker tests stable.
- **ERR-3** merged: `ErrorReportingPrivacyCopy` and privacy contract exist on merge base.
- English-only UI; scrollable IMGUI window acceptable for long disclosure.
- Issue #172 "first lobby/login or first mod load" is interpreted as **first non-menu gameplay scene** after systems init (matches existing scene-loaded init).

## Dependencies

| Dependency | Status |
|------------|--------|
| ERR-1 test matrix | Done (roadmap) |
| ERR-3 privacy copy | Done on `003-err-3-privacy-copy` / PR #207 |
| ADR-0010 | Updated in ERR-2 PR |
| ADR-0012 TestCrash | No behavior change; verify still works with default true |
| Phase 7 (FR-010/FR-011) | Core compat + error capture fix; same PR as ERR-2; no Worker/schema change |

## Out of Scope

- Worker, GitHub, or payload schema changes (Phase 7 only changes client capture via `EnemyHealthCompat`).
- REPOConfig custom modal (no description API; cfg/CM remain secondary surfaces).
- Localization.
- Re-prompting on every game version bump (only cfg reset re-triggers).
