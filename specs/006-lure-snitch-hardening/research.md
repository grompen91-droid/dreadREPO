# Research: Camp Lure and Snitch hardening (006)

**Date**: 2026-06-01  
**Feature**: `006-lure-snitch-hardening`

## R1: Truck/shop vs extraction level detection

**Decision**: Extend `GameplayContext` with `GameplayPhase` enum and `AllowsHostMonsterFeatures`. Resolve phase via new `GameplayPhaseCompat` that tries game APIs in order, then falls back to an internal latch.

**Rationale**: `SemiFunc.MenuLevel()` alone is insufficient: truck/shop is non-menu but not an extraction run (confirmed by user report and snitch arm fix docs referencing "return to lobby/truck").

**Resolution order**:

1. `SemiFunc.MenuLevel()` → `Menu`
2. Reflection probe for REPO run-state methods (candidates to verify against real `Assembly-CSharp.dll`):
   - `RunManager.instance` fields/methods suggesting in-level vs truck (e.g. level active, reality state)
   - Additional `SemiFunc` static bools if present in game build
3. **Latch fallback**: `GameplayPhaseCompat.NotifyExtractionLevelStarted()` called from existing `SnitchLevelGenDonePatch` postfix; cleared on `SceneManager.sceneLoaded` with `LoadSceneMode.Single` (truck return). While latch false and not menu → `TruckOrShop`.

**Alternatives considered**:

- Scene name heuristics only: rejected (R.E.P.O. keeps scene named Main during level; same problem as original `IsRun()`).
- Enemy count as sole signal: rejected (shop may have zero enemies; level start may briefly have zero enemies before spawn).
- Always on when `!MenuLevel()`: rejected (current bug).

**Stub strategy**: Add minimal stub fields/methods discovered during implementation to `.github/scripts/Assembly-CSharp_stubs.cs`; gate compat logs once at Verbose if API missing (latch-only mode).

---

## R2: Camp lure cooldown semantics

**Decision**: Per-player `cooldownUntil` timestamp set when a lure cycle ends due to **contact** (nearest enemy distance ≤ `LureSafeDistance`). While `Time.time < cooldownUntil`, exclude player from target selection and do not increment camp timer.

**Rationale**: User feedback: with minimum threshold settings, timer resets to 0 when mob leaves but immediately re-accumulates while hiding. Cooldown creates intentional breathing room.

**Config**: `LureCooldownSeconds` default **60**, range 10-300.

**Alternatives considered**:

- Global lure cooldown (one timer for all players): rejected (unfair in MP; one player contact would shield campers).
- Slow camp timer decay instead of hard cooldown: rejected (does not fix min-threshold instant re-arm).
- Cooldown only on target player: **chosen** (per-player map keyed by roster label).

**Contact reset timing**: Check proximity on current target every frame (or before `MaybePull`); clear target and stop pulls same frame when contact detected (fixes 1s evaluate lag from review).

---

## R3: Empty enemy scan handling

**Decision**: Add `ProximityScan.HasEnemies()` (or `IsBeyondEnemyScan(origin, safe)` helper) treating `NearestDistance >= float.MaxValue * 0.5f` as no enemies. Camp lure must not accumulate camp time or select targets when `!HasEnemies()`.

**Rationale**: Matches overlay/debug server semantics; prevents lure when no threat exists.

**Alternatives considered**:

- Require minimum enemy count > 0 in level: same as HasEnemies check.

---

## R4: Snitch bang multiplayer audio

**Decision**: **v1**: Keep `SpatialAudio3D.PlayAt` on host; document in ADR/quickstart that clients may not hear bang unless REPO replicates local AudioSources. **Research spike** during implementation: grep real game for RPC/networked one-shot sound patterns; if a stable hook exists (e.g. game SFX manager), optional follow-up task.

**Rationale**: Tension/ambient audio is intentionally client-local; snitch bang is social feedback. Host-only spawn is a known risk from code review. Full netcode is out of scope unless trivial hook found.

**Alternatives considered**:

- Photon RPC to all clients to play local spatial audio: deferred (needs network seam, compat risk).
- Play on all clients via config sync event: deferred (no existing pattern in Dread).

---

## R5: Snitch pickup false positives

**Decision**: After marker attach, wait **2 seconds** (constant, not config) before evaluating pickup heuristics; re-snapshot spawn position after grace. Ignore kinematic/parent true at marker `Start` if still true after grace only when combined with movement signal.

**Rationale**: Logs show kinematic/parent checked at Start; shop items or spawn physics may already satisfy signals.

**Alternatives considered**:

- Harmony postfix on grab method: best long-term but needs verified method name; keep as future ADR if heuristics fail manual test.

---

## R6: Snitch failed arm state

**Decision**: Replace `_armed = true` on max retries with explicit `_armFailed` flag; overlay state `failed (no items)`. Do not block future level via `_armed`.

**Rationale**: Overloaded `_armed` caused limbo state in review.

---

## R7: Agent debug instrumentation

**Decision**: Remove `Systems/Core/AgentDebugLog086b84.cs` and all `#region agent log` blocks added for diagnosis. Use existing `LoggingService` Verbose/Info levels gated by overlay or log level.

**Rationale**: Temporary debug code should not ship; violates silent normal play for snitch Warning logs.

---

## R8: EnemyLureCompat and aggression patch coupling

**Decision**: Document that `EnemyDirectorSetInvestigatePatch` multiplies radius ×1.5 for all `SetInvestigate` calls including compat invokes when aggression enabled. Add optional Verbose log prefix `[CampLure]` / `[Snitch]` at call sites (not inside patch). No coordinator in v1.

**Rationale**: Shared game API is correct integration point; stacking pulls is acceptable if phase gating prevents shop pulls.

**Alternatives considered**:

- Bypass patch for lure pulls: rejected (would need duplicate method invoke or Harmony priority hack).

---

## R9: Subagent execution

**Decision**: Use subagent-driven-development after Phase 1 (gameplay phase API) completes. Three streams: Camp Lure, Snitch, Core/overlay/docs.

**Rationale**: User requested multitasking; streams touch mostly disjoint files after shared gate lands.

---

## Implementation notes (2026-06-01)

**Phase API**: Shipped `GameplayPhaseCompat` with latch from `SnitchLevelGenDonePatch` and optional native probes (`TruckLevel`, `RunLevel`, `InTruck`, `InShop`, `InLevel`, `InExtractionLevel`). Stubs include `TruckLevel()` and `RunLevel()` returning false. In-game: confirm which `SemiFunc` methods exist on real `Assembly-CSharp.dll` and extend probe list if latch-only misclassifies truck vs level.

**Camp lure cooldown**: `LureCooldownSeconds` default 60; per-player `CooldownUntil` after contact (nearest enemy within safe distance).

**Snitch bang MP**: Still host-spawned `SpatialAudio3D`; manual MP matrix in quickstart required to confirm client audibility. No networked sound hook found in stub build.

**Manual verify**: Full quickstart matrices (T036) pending in-game host session; stub Release build and Tier 0 verify pass in CI/agent VM.
