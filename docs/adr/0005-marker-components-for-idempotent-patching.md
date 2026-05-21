# ADR-0005: Use Marker Components for Idempotent Runtime Patching

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

The `MonsterOverhaulSystem.AudioOverhaul` runs a scan loop every 4 seconds that finds all `EnemyHealth` instances and applies pitch/reverb/spatial tweaks to their child `AudioSource` components. Without protection, enemies that persist across scan cycles would get re-patched every 4 seconds, stacking pitch shifts and reverb values multiplicatively.

---

## Decision

Define a marker component `DreadAudioTweaked : MonoBehaviour` with no fields or methods: purely a flag. Before applying audio tweaks to an enemy, check if it already has the component. If not, apply tweaks and add the marker.

```csharp
private class DreadAudioTweaked : MonoBehaviour { }

// In the scan loop:
if (enemy.GetComponent<DreadAudioTweaked>() != null)
    continue;
// ... apply audio tweaks ...
enemy.gameObject.AddComponent<DreadAudioTweaked>();
```

The marker persists for the lifetime of the enemy GameObject. When the enemy is destroyed, the marker is destroyed with it. Newly spawned enemies lack the marker and get patched on the next scan cycle.

---

## Consequences

- Audio tweaks are applied exactly once per enemy, regardless of how many scan cycles the enemy lives through.
- Zero allocation for the marker itself (empty class). Minimal allocation for `AddComponent`.
- Works automatically with modded enemies: they don't need to know about Dread or implement any interface.
- The pattern is reusable. Any future system that needs idempotent one-time application to game objects can use the same approach.

---

## Rejected Alternatives

- **Track enemies in a HashSet**: requires cleanup on enemy death. `EnemyHealth` does not expose a reliable death event across all enemy types (including modded ones). Leaked references would cause memory growth over long sessions.
- **Check audio values before applying**: comparing floating-point pitch/reverb values is fragile. An enemy might legitimately have a low pitch from another mod.
- **Timed re-application**: reapplying every N cycles doesn't fix stacking and wastes CPU.
