# ERR-2 Research

**Date**: 2026-05-30  
**Feature**: `004-err-2-default-on-prompt`

## Decision: Default `ErrorReportingEnabled` to true

**Decision**: Change `DreadConfig.ErrorReportingEnabled` bind default from `false` to `true` for newly generated cfg entries.

**Rationale**: Issue #172 and roadmap ERR-2; pairs with first-run disclosure so default-on is informed.

**Alternatives considered**:

- Default true without prompt: rejected (roadmap gate, ERR-3 dependency).
- Stay default false: rejected (not ERR-2).

## Decision: Separate one-time gate config key

**Decision**: Add `ErrorReportingPromptShown` (bool, default `false`, section `7. Error Reporting`). Set `true` when the player clicks either prompt button.

**Rationale**: Issue requires prompt exactly once per profile; `ErrorReportingEnabled` alone cannot distinguish "user chose off" from "never saw prompt" on upgrade. User brief "persist opt-in/out only via ErrorReportingEnabled" applies to the **reporting choice**, not to storing prompt completion.

**Alternatives considered**:

- Infer from cfg file mtime: rejected (unreliable, breaks cfg reset testing).
- PlayerPrefs: rejected (project uses BepInEx cfg).
- Only `ErrorReportingEnabled`: rejected (cannot satisfy once-only + upgrade with existing false).

## Decision: IMGUI modal via dedicated MonoBehaviour

**Decision**: New `ErrorReportingPromptSystem` (name may vary) with `OnGUI`, registered in `DreadSystemRegistry`, enabled when `!ErrorReportingPromptShown`. Pattern follows `DebugOverlaySystem` (IMGUI, menu gate, no `GUIContent.none`).

**Rationale**: No REPOConfig modal API; BepInEx has no built-in first-run UI; IMGUI already used in mod. Issue allows in-game UI.

**Alternatives considered**:

- BepInEx chat/log only: rejected (issue requires UI).
- Harmony patch game menu: rejected (fragile, high compat risk).
- Block game input with Time.timeScale = 0: deferred (optional polish; not required for v1).

## Decision: When to show the prompt

**Decision**: On first `SceneManager.sceneLoaded` where (1) `DreadSystemInitializer` has completed, (2) `!SemiFunc.MenuLevel()`, (3) `!ErrorReportingPromptShown`, show prompt on next `OnGUI` frame. Do not show in `Plugin.Awake` (no UI, systems may not exist).

**Rationale**: Matches issue "first lobby/login" as first **gameplay** session surface; aligns with Psychotic Break menu guard and scene-based init in `Plugin.cs`.

**Alternatives considered**:

- Show on mod load in menu: rejected (menu level guard, poor UX during loading).
- Show only on "lobby" scene name: rejected (scene names vary; `MenuLevel()` is established).

## Decision: Button behavior and upgrade path

**Decision**:

| Button | `ErrorReportingEnabled` | `ErrorReportingPromptShown` |
|--------|-------------------------|-----------------------------|
| Keep reporting on | `true` | `true` |
| Turn off reporting | `false` | `true` |

Before first prompt on upgraded installs, **do not** auto-flip an existing `false` to `true`; default bind change affects **new** keys only. BepInEx retains saved `false` in existing cfg. Prompt explains default-on for **new** players; upgraded users with `false` keep false until they opt in via button.

**Rationale**: Avoid silent enablement for players who already opted out.

**Alternatives considered**:

- Migration script forcing true: rejected (breaks trust).
- Skip prompt if false: rejected (they never see ERR-3 disclosure).

## Decision: Gate telemetry until acknowledgment (best effort)

**Decision**: `ErrorReporterSystem` / queue checks `ErrorReportingPromptShown` (or shared helper `ErrorReportingConsent.IsActive`) and treats reporting as **off** until prompt is shown, even if `ErrorReportingEnabled` is true, **only when** `ErrorReportingPromptShown` is false.

**Rationale**: FR-009; prevents reports leaving before disclosure on first session.

**Alternatives considered**:

- Allow sends before prompt: rejected for first-run session.

## Decision: Update ERR-3 canonical copy in same PR

**Decision**: Update `ErrorReportingPrivacyCopy` strings: `ShortSummary`, `DataBullets[8]`, and regenerated `FullDescription` to state default **on** and first-run prompt. Update `specs/003-err-3-privacy-copy/contracts/privacy-copy.md` row 10 in ERR-2 PR (or add ERR-2 addendum contract).

**Rationale**: FR-005; contract row 10 explicitly allows ERR-2 default change.

**Alternatives considered**:

- Leave "default off" text: rejected (false disclosure after ERR-2).

## Decision: ADR and docs

**Decision**: Amend ADR-0010 to document default-on + first-run opt-out UI; CHANGELOG `[Unreleased]` entry; README/THUNDERSTORE/mod-compatibility bullets.

**Rationale**: Issue #172 and ADR-0010 context section still says opt-in.

## Decision: Merge base

**Decision**: Branch from `master` after ERR-3 merge (or merge `003-err-3-privacy-copy` first). Implementation uses `ErrorReportingPrivacyCopy` from ERR-3.

**Rationale**: Roadmap dependency order.

## Decision: Core namespace + EnemyHealthCompat for error capture

**Decision**: Move all `*Compat.cs` helpers to `Systems/Core/` (`Dread.Systems.Core`). Extend `EnemyHealthCompat` with `TryReadHealth`, `TryIsAlive`, and `CountAliveAndNearby`. `ErrorReportPayloadCapture.CaptureGameState` uses `EnemyScanCache.GetEnemies()` plus Core compat (no compile-time `CurrentHealth`).

**Rationale**: Player logs showed `MissingMethodException` for `EnemyHealth.get_CurrentHealth()` when DeathMinimap logged NREs after death; direct stub property access bypassed per-enemy `catch` and aborted the whole error batch. Reflection-based compat already used for psychotic break and tension.

**Alternatives considered**:

- Catch `MissingMethodException` only in `CaptureGameState`: rejected (does not fix `DebugServerSystem` or future call sites).
- Leave compat in `Systems/` flat layout: rejected (user request for shared Core surface per ARCH-3).
