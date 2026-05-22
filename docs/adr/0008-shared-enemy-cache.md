# ADR-0008: Shared Static EnemyHealth Cache Between Systems

**Date:** 2026-05-22
**Status:** Superseded by ADR-0010 -- the shared cache caused invisible cross-system coupling (Issue #103) and was replaced with direct per-system FindObjectsOfType calls.

---

## Context

Both `MonsterOverhaulSystem` and `TensionSystem` call `FindObjectsOfType<EnemyHealth>()` on independent scan intervals (every 2s and 4s respectively). Each call allocates a fresh array, creating unnecessary GC pressure.

The two systems always run on the same scene and operate on the same set of enemies. There is no case where one system needs a different set of enemies than the other.

---

## Decision

Add a shared static `List<EnemyHealth> CachedEnemies` field to `MonsterOverhaulSystem`. `MonsterOverhaulSystem` populates this list during its own scan tick. `TensionSystem` reads from the same static list instead of calling `FindObjectsOfType` itself.

Implementation:
- `MonsterOverhaulSystem._scanTimer` refreshes `CachedEnemies` on its 4-second interval
- `TensionSystem._scanTimer` reads `CachedEnemies` directly, avoiding its own `FindObjectsOfType` call
- `CachedEnemies.Clear()` is called before each repopulation to reuse capacity
- No thread safety concerns: all access is on Unity's main thread via `Update()`

---

## Consequences

- One `FindObjectsOfType` call every 4s instead of two (one every 2s + one every 4s)
- Reduced per-frame allocations: cached list avoids `FindObjectsOfType`'s internal array allocation on every call
- Mild coupling: `TensionSystem` implicitly depends on `MonsterOverhaulSystem` to populate the cache. In practice, both systems are always enabled simultaneously (both are started from `Plugin.Start()`)
- If `MonsterOverhaulSystem` were ever removed or disabled independently, `TensionSystem` would silently scan zero enemies until the next repopulation

---

## Rejected Alternatives

- **Local per-system cache with shared interval**: would require coordinating both systems' timers, adding unnecessary coupling for a 2s timing difference
- **Single `FindObjectsOfType` call in Plugin or a dedicated manager**: adds a new MonoBehaviour just for caching, overengineered for two consumers
- **Event-driven cache invalidation** (e.g., `EnemyHealth.OnEnable`/`OnDisable` hooks): more complex, requires Harmony patches on enemy lifecycle events, no meaningful benefit over polling at 4s intervals
