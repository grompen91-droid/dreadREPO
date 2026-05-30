# ERR-3 Research

**Date**: 2026-05-30  
**Feature**: `003-err-3-privacy-copy`

## Decision: Primary surface is BepInEx config description

**Decision**: v1 player disclosure lives in `DreadConfig.ErrorReportingEnabled` `ConfigDescription` and the generated `elytraking.dread.cfg` file. REPOConfig shows the same description when present.

**Rationale**: Roadmap scopes ERR-3 as copy/disclosure; config is always available without new UI dependencies; matches "how to disable" acceptance (cfg key). Issue #173 mentions in-game text; issue also references ERR-2 for prompt placement. Roadmap order is ERR-3 before ERR-2.

**Alternatives considered**:

- IMGUI modal in ERR-3: rejected (duplicates ERR-2, needs UI gate and persistence).
- README-only: rejected (players who enable via REPOConfig never read README).
- BepInEx chat message on load: rejected (spam, no established pattern in Dread).

## Decision: Canonical copy module

**Decision**: Add `ErrorReportingPrivacyCopy` (or similar) static class under `Systems/ErrorReporting/` with `FullDescription`, `ShortSummary`, and `DisableInstructions` constants. `DreadConfig` references `FullDescription` for bind text.

**Rationale**: FR-005 and US2; ERR-2 imports same constants for modal body later.

**Alternatives considered**:

- Inline string only in `DreadConfig`: rejected (ERR-2 duplication risk).
- JSON resource file: rejected (overkill for three strings).

## Decision: Payload categories (truth table)

**Decision**: Disclosure bullets map 1:1 to capture paths below.

| Category | Source | Fields (summary) |
|----------|--------|------------------|
| Error | `ErrorReporterSystem` + queue | Exception type, message (max 500), stack (max 3000), hash, timestamp, scene |
| Game state | `CaptureGameState` | Scene, enemy alive/total/nearby, player HP/stamina/position, play time |
| System | `CaptureSystemInfoSafe` | OS, CPU, RAM, GPU, device model |
| Display | `CaptureDisplayInfoSafe` | Resolution, refresh, DPI, fullscreen mode |
| Config | `CaptureConfigSafe` | Dread feature toggles including `ErrorReportingEnabled` |

**Not in client payload**: player name, Steam ID, IP (Worker sees IP at edge per ADR-0010; do not claim IP is in JSON).

**Rationale**: Matches ADR-0010 table and `ErrorReportPayloadCapture.cs`.

**Alternatives considered**:

- Claim "no game state": false; position and HP are sent.

## Decision: No localization in v1

**Decision**: English strings only, consistent with existing config descriptions.

**Rationale**: No i18n API in mod; R.E.P.O. mod ecosystem rarely localizes BepInEx cfg.

**Alternatives considered**:

- BepInEx localization plugin: out of scope.

## Decision: Stale public docs

**Decision**: ERR-3 implementation PR may fix README/THUNDERSTORE telemetry section where it still says Harmony hooks on `Debug.LogError` / default-on wording. Scope limited to error-reporting paragraphs.

**Rationale**: FR-006; reduces false disclosure; separate from ERR-2 default change.

**Alternatives considered**:

- Full README audit: out of scope.

## Decision: P2 optional in-game line

**Decision**: Optional single `LoggingService.LogInfo` when user sets `ErrorReportingEnabled` true (not on every session): one line pointing to cfg section. Default off unless tasks add it.

**Rationale**: US3 partial satisfaction without building ERR-2 UI.

**Alternatives considered**:

- Debug overlay row: only visible with debug flags; poor for general players.

## Issue #173 vs roadmap dependency note

**Finding**: GitHub issue body says "Depends on ERR-2 for placement"; roadmap says ERR-3 depends ERR-1 and blocks ERR-2.

**Resolution for this plan**: Follow roadmap and user brief: ERR-3 delivers copy + cfg disclosure first; ERR-2 owns first-run placement using exported strings. Update issue #173 comment in implementation PR if needed.
