# Tension system and enemy proximity

How **Tension System** shares one proximity scan across adrenaline, panic sprint, low stamina sound, and fake footsteps. Implements `TensionSystem.cs`.

## Design rule

**One scan, many features.** Do not add a second `FindObjectsOfType<EnemyHealth>()` cache owned by another system for tension-adjacent behavior. That caused the historical "phantom cache" bug (issue #103): `TensionSystem` depended on `MonsterOverhaulSystem.CachedEnemies`, which only refreshed when monster audio ran.

**Current approach:** `TensionSystem` calls `FindObjectsOfType<EnemyHealth>()` inside `FindNearestEnemyDist()` every **0.5s** (see `Update()` scan interval). `MonsterOverhaulSystem` uses its own 4s scan for audio only. No shared static lists.

## Proximity scan

| Constant / field | Value / role |
|------------------|--------------|
| `ProximityRange` | 15m |
| `_nextScan` interval | 0.5s |
| `_nearestDist` | Distance to nearest `EnemyHealth` from camera |
| Menu | `_nearestDist = float.MaxValue` on menu level |

Published to `DreadRuntimeState.NearestEnemyDist` for overlay and debug server.

## Sub-features (config in section `3. Tension`)

| Feature | Config | Behavior summary |
|---------|--------|------------------|
| Adrenaline | `AdrenalineEnabled` | Lowers sprint drain when enemy within 15m |
| Panic sprint | `PanicSprintEnabled` | 1.25x `SprintSpeedMultiplier` for 2s when sprint starts near enemy; 20s cooldown |
| Low stamina sound | `LowStaminaSoundEnabled` | Breath clips when stamina low after sprint |
| Fake footsteps | `FakeFootstepsEnabled` | Rare 3D footsteps behind player |

All respect `CompatibilityMode` and `SemiFunc.MenuLevel()`.

## Panic sprint (shipped)

Trigger (all required):

- Sprint transition: was not sprinting, now `pc.sprinting`
- `_nearestDist < ProximityRange` (15m)
- `_panicCooldown <= 0`
- Config on, not compatibility mode, not menu

Effect:

- Save `SprintSpeedMultiplier`, multiply by **1.25** for **2** seconds
- Restore original multiplier when timer ends; start **20s** cooldown

Implementation uses `HarmonyLib.Traverse` on `PlayerController` fields (confirmed via game binary analysis).

## Adrenaline

Stores `_originalDrain` from `EnergySprintDrain`, applies reduced drain while near enemy. `RestoreDrain()` on scene change and disable paths.

## Enemy lookup pattern (for new tension features)

```csharp
// Inside TensionSystem only, on the existing 0.5s tick:
_nearestDist = SemiFunc.MenuLevel() ? float.MaxValue : FindNearestEnemyDist();
```

Do not:

- Read `MonsterOverhaulSystem` static caches
- Scan every `Update()` frame unless profiling proves 0.5s is insufficient

## Debugging

- Overlay: panic sprint active + cooldown via `DreadRuntimeState`
- MCP: `dread_get_runtime_state` after Tier 1 verify
- Log: `LoggingService.LogVerbose` under `[Tension]`

## Historical docs

Archive: `docs/agents/archive/superpowers/specs/2026-05-17-panic-sprint-design.md`, `specs/2026-05-22-decoupled-enemy-cache-design.md`, and matching `plans/` files.
