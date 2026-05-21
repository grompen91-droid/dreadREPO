# ADR-0002: Consolidate Tension Features Into a Single MonoBehaviour

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

In v1.0.0 through v1.3.x, each tension feature was a separate class in `Systems/Tension/`:

- `AdrenalineFeature.cs`
- `FakeFootstepFeature.cs`
- `LowStaminaFeature.cs`
- `PanicSprintFeature.cs`

Each had its own `Update()` loop and independently scanned for nearby enemies via `FindObjectsOfType<EnemyHealth>()`. This meant four proximity scans per frame, all doing the same work, and each requiring its own lifecycle management (coroutines, cooldowns, cleanup).

---

## Decision

Merge all four features into a single `TensionSystem` MonoBehaviour with one shared `Update()` loop that scans for the nearest `EnemyHealth` once every 0.5 seconds and drives all four subsystems from the result.

The shared proximity state (nearest enemy distance) is computed once and passed to each subsystem, which then decides independently whether to act.

---

## Consequences

- Single proximity scan per 0.5s instead of four per frame. Reduced CPU overhead.
- Shared state eliminates edge cases where different features disagreed on threat distance.
- Cleaner lifecycle: one `OnDestroy`, one scene-change handler, one set of coroutines.
- The Tension/ subdirectory was deleted entirely. All four features live inline in `TensionSystem.cs` as private methods.
- The tight coupling between features (e.g., panic sprint consuming stamina that adrenaline preserves) is easier to reason about in one file.

---

## Rejected Alternatives

- **Keep separate classes but inject a shared scanner** -- would have required an `EnemyProximityScanner` singleton or injected dependency. Extra complexity without clear benefit since all features are always enabled together.
- **Event-driven instead of polling** -- would require hooking enemy spawn/death events. R.E.P.O. does not expose these reliably across modded enemy types.
