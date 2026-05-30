# Feature Specification: ERR-3 Error reporting privacy copy

**Feature Branch**: `003-err-3-privacy-copy`

**Created**: 2026-05-30

**Status**: Implemented (pending merge; disclosure aligned with payload capture 2026-05-30)

**Roadmap**: ERR-3 (P1) | **Issue**: [#173](https://github.com/grompen91-droid/dreadREPO/issues/173)

**Input**: ERR-3: In-game privacy copy for error reporting. What is sent, how to disable. Required before ERR-2 default-on.

## User Scenarios & Testing

### User Story 1 - Player understands telemetry before enabling (Priority: P1)

A player opens Dread settings (BepInEx cfg, REPOConfig, or Configuration Manager) and reads what anonymous error reporting collects, where it goes, and how to turn it off, without searching Discord or the GitHub repo.

**Why this priority**: ERR-2 cannot ship default-on until disclosure is accurate and reviewable (roadmap gate).

**Independent Test**: With `ErrorReportingEnabled` false (default), open section `5. Error Reporting` in generated cfg or REPOConfig; disclosure text lists payload categories and names cfg key `ErrorReportingEnabled`.

**Acceptance Scenarios**:

1. **Given** Dread loaded and cfg generated, **When** the player reads the `ErrorReportingEnabled` entry description, **Then** text states reporting is opt-in (current default false), lists data categories sent on errors, and says how to disable (`ErrorReportingEnabled = false`).
2. **Given** the disclosure text, **When** compared to `ErrorReportPayloadCapture` and ADR-0010 payload table, **Then** no category is claimed that code does not capture and no captured category is omitted without explicit "may be omitted on failure" wording.

---

### User Story 2 - Maintainer has one canonical copy source (Priority: P1)

A maintainer updates error reporting behavior and updates privacy copy in one place so ERR-2 first-run UI and public docs stay aligned.

**Why this priority**: Prevents README, cfg description, and future prompt from drifting.

**Independent Test**: Grep shows player-facing strings for error reporting disclosure originate from a single module or contract file referenced by implementers.

**Acceptance Scenarios**:

1. **Given** ERR-3 merged, **When** an agent opens [contracts/privacy-copy.md](./contracts/privacy-copy.md), **Then** it defines required bullets, cfg key, and review checklist against payload code.
2. **Given** a payload field added in a future PR, **When** checklist is followed, **Then** canonical copy and golden test docs are updated in the same PR.

---

### User Story 3 - Optional in-game visibility without ERR-2 (Priority: P2)

A player who never opens cfg still has a path to see disclosure before reports leave the client (deferred full modal to ERR-2).

**Why this priority**: Issue #173 title says "in-game"; roadmap v1 allows copy-only unless UI is required. ERR-2 owns first-run prompt placement per [#172](https://github.com/grompen91-droid/dreadREPO/issues/172).

**Independent Test**: If v1 implements only config description, ERR-2 issue explicitly references ERR-3 contract strings for modal body. If v1 adds a minimal in-game surface, it uses the same strings and does not change defaults.

**Acceptance Scenarios**:

1. **Given** ERR-3 scope (no default change), **When** shipped, **Then** `ErrorReportingEnabled` default remains **false** and no first-run prompt is added (ERR-2 out of scope).
2. **Given** ERR-2 later, **When** first-run prompt ships, **Then** prompt body is composed from ERR-3 canonical strings without rewriting legal meaning.

---

### Edge Cases

- Player uses REPOConfig with truncated descriptions: long text must remain readable in cfg file even if UI truncates (full text in `elytraking.dread.cfg`).
- Reporting enabled but offline: copy must not promise instant GitHub issue creation.
- Compatibility mode / other mods: copy does not blame third-party mods; states only Dread-captured fields.
- Stub CI: disclosure module must not call Unity APIs at type load.

## Requirements

### Functional Requirements

- **FR-001**: Provide **canonical privacy disclosure** (English) covering: purpose (anonymous crash diagnostics), destination (developer via Cloudflare Worker to GitHub issues), no intentional PII (no account names, chat, or file paths from other apps), and categories below.
- **FR-002**: Disclosure MUST enumerate payload categories aligned with code: exception message/type and truncated stack; scene and coarse game state (enemy counts, player HP/stamina, world position); device/OS/GPU/RAM/display; snapshot of Dread config booleans including `ErrorReportingEnabled`.
- **FR-003**: Disclosure MUST state **how to disable**: set `ErrorReportingEnabled` to `false` in `BepInEx/config/elytraking.dread.cfg` section `5. Error Reporting` (and equivalent REPOConfig toggle).
- **FR-004**: Wire canonical text into **`DreadConfig.ErrorReportingEnabled` config description** (primary v1 player surface).
- **FR-005**: Add implementer contract [contracts/privacy-copy.md](./contracts/privacy-copy.md) with review checklist mapping each bullet to `ErrorReportPayloadCapture` / `ErrorReportTypes` / ADR-0010.
- **FR-006**: Align **player-facing README/THUNDERSTORE** error reporting bullets with ADR-0010 pipeline (`Application.logMessageReceived`, opt-in default false). Fix known stale Harmony wording only where touched for ERR-3.
- **FR-007**: Document ERR-2 integration point: exported strings or static API for first-run prompt (implementation in ERR-2, not ERR-3).

### Key Entities

- **PrivacyDisclosure**: Canonical title, summary, bullet list, disable instructions, optional short summary for UI.
- **ConfigSurface**: BepInEx `ConfigDescription` on `ErrorReportingEnabled`; file path `elytraking.dread.cfg`.
- **PayloadCategory**: Logical grouping (Error, GameState, SystemInfo, Display, Config) mapped to capture methods.

## Success Criteria

- **SC-001**: Manual review: every bullet in canonical copy maps to a field or capture method cited in the privacy contract checklist (zero false claims).
- **SC-002**: `ErrorReportingEnabled` default remains `false`; no first-run prompt or default change in ERR-3 PR.
- **SC-003**: Tier 0 stub build and `dotnet test tests/Dread.ErrorReportJson.Tests` pass after implementation.
- **SC-004**: ERR-2 issue #172 can reference `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` without rewriting disclosure.

## Assumptions

- **ERR-1** (test matrix, golden JSON, Worker tests) is done or in progress on `master`; ERR-3 does not add telemetry tests.
- **ERR-2** (default-on + first-run prompt) is blocked until ERR-3 merges; ERR-3 does not change ADR-0010 opt-in decision.
- English-only copy matches rest of mod config descriptions (no localization framework).
- Issue #173 note "depends ERR-2 for placement" is interpreted as **modal placement** belongs to ERR-2; ERR-3 still delivers copy and cfg disclosure first (roadmap: ERR-3 blocks ERR-2).

## Out of Scope

- Changing `ErrorReportingEnabled` default to `true` (ERR-2).
- First-run opt-in/out modal UI (ERR-2).
- Worker rate limits, GitHub labels, or payload schema changes.
- New telemetry fields or removing existing captures.
- Legal privacy policy page or GDPR workflow beyond honest disclosure.
