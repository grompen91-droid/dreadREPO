# ADR-0002: Decompose TensionSystem and introduce shared AudioLoader cache

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

`TensionSystem.cs` grew to 280+ lines bundling five unrelated responsibilities:

- Enemy proximity scanning (shared infrastructure)
- Adrenaline (sprint drain reduction near enemies)
- Low stamina (breath gasp audio)
- Panic sprint (speed burst on sprint start near enemies)
- Fake footsteps (random positional audio)

Changing one feature risked breaking others through shared `Update()` state. The proximity scan ran `FindObjectsOfType<EnemyHealth>()` every 0.5 seconds even when all features were disabled in config.

Separately, `AudioDreadSystem` and `TensionSystem` each loaded their own copy of `footsteps.ogg` and `breathing.ogg` via `UnityWebRequestMultimedia`, producing two decoded `AudioClip` objects in memory for identical data.

---

## Decision

### TensionSystem decomposition

Extract each feature into a dedicated `MonoBehaviour` in `Systems/Tension/`:

- `EnemyProximityScanner` -- owns the 0.5s scan, exposes `NearestDist`. Skips the scan entirely when all four features are disabled.
- `AdrenalineFeature` -- reads `NearestDist` from scanner.
- `PanicSprintFeature` -- reads `NearestDist` from scanner.
- `LowStaminaFeature` -- self-contained breath audio trigger.
- `FakeFootstepFeature` -- self-contained footstep loop.

`TensionSystem` becomes a 12-line coordinator that adds all five components to the host `GameObject`. Features communicate only through `EnemyProximityScanner.NearestDist` (a public property on the same `GameObject`).

### AudioLoader cache

Add `Systems/AudioLoader.cs`: a static `Dictionary<string, AudioClip>` keyed by filename. First request per filename loads via `UnityWebRequest`; subsequent requests return the cached clip immediately. All systems delegate clip loading to `AudioLoader`.

---

## Consequences

- Each tension feature can be read, tested, or removed independently without touching others.
- `FindObjectsOfType` no longer runs when all tension features are disabled.
- `footsteps.ogg` and `breathing.ogg` are decoded once regardless of how many systems request them.
- `AudioLoader` cache is never cleared at runtime. Clips persist for the session. Acceptable for a mod with a fixed, small audio set.

---

## Rejected Alternatives

- **Partial classes** -- would keep all code in one translation unit, defeating the isolation goal.
- **Event/message bus** -- unnecessary indirection for four features sharing one float value.
- **Asset bundle or `Resources.Load`** -- audio ships alongside the DLL in `BepInEx/plugins/`, not in a Unity asset bundle. `UnityWebRequest` with a file URI is the correct loader for this layout.
