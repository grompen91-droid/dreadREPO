# Decoupled Enemy Cache Design

**Issue:** #103 — "phantom cache" where TensionSystem's proximity features (adrenaline, panic sprint, etc.) silently break

**Current Bug:** `TensionSystem` reads `MonsterOverhaulSystem.CachedEnemies` (a shared static list) but never triggers a refresh itself. The cache is only populated inside `MonsterOverhaulSystem.MonsterAudioLoop()`, which is guarded by `DreadConfig.MonsterAudioEnabled.Value`. When monster audio is disabled, the cache stays empty forever, and `_nearestDist` is always `float.MaxValue`.

**Root Cause:** Issue #56 (previous fix) introduced a shared static cache to reduce `FindObjectsOfType` allocations, but created an invisible dependency: TensionSystem works only if MonsterOverhaulSystem's MonsterAudio feature is enabled.

**Goal:** Each system owns its proximity data. No shared statics.

---

## Architecture

| System | Field | Scan Interval | Fill Pattern |
|--------|-------|---------------|--------------|
| `TensionSystem` | `private readonly List<EnemyHealth> _enemyCache` | 0.5s (down from 2.0s) | `Clear()` + `AddRange(FindObjectsOfType<EnemyHealth>())` |
| `MonsterOverhaulSystem` | `private readonly List<EnemyHealth> _localEnemyCache` | 4s (unchanged) | Same pattern, own list |

**Changes:**

| File | Change |
|------|--------|
| `Systems/TensionSystem.cs` | Remove static cache dependency. Add per-instance `_enemyCache` list. Refresh every 0.5s, `FindNearestEnemyDist()` iterates own cache. |
| `Systems/MonsterOverhaulSystem.cs` | Remove `public static List<EnemyHealth> CachedEnemies`. Add per-instance `_localEnemyCache`. |

---

## Trade-offs

**Against the shared cache approach (rejected):**
- Couples two independent systems
- Cache stays empty silently when MonsterAudio is disabled
- Hard to debug (symptoms appear in unrelated features)

**Against frame-rate scan:**
- Unnecessary CPU work every frame when 0.5s is plenty fast for player experience
- 0.5s -> 2 allocation/sec, negligible GC pressure

---

## Testing & Verification

- No build-time test harness available
- Verify by reading final code for logic errors: correct list ownership, no stale references to `MonsterOverhaulSystem.CachedEnemies`
- Rollback: revert the relevant commits
