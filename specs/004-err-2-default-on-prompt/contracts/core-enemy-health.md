# Contract: Core EnemyHealth compat

**Feature**: ERR-2 adjunct (Core capture fix) | **Namespace**: `Dread.Systems.Core`

## Normative rules

1. No Dread code outside generated stubs MAY call `EnemyHealth.CurrentHealth` at compile time.
2. HP reads MUST go through `EnemyHealthCompat.TryReadHealth` (Harmony `Traverse`, name fallbacks per reflection inventory).
3. Alive checks for reporting/MCP MUST use `EnemyHealthCompat.TryIsAlive` (valid reference + readable HP > 0).
4. Batch counts for error reports MUST use `EnemyHealthCompat.CountAliveAndNearby`.
5. Destroyed or invalid `EnemyHealth` references MUST be skipped via `EnemyHealthCompat.IsValid` before transform/HP access.

## API surface (`Systems/Core/EnemyHealthCompat.cs`)

| Method | Behavior |
|--------|----------|
| `IsValid(EnemyHealth?)` | False for null or Unity-destroyed refs |
| `IsAliveForVisibility(EnemyHealth)` | LOS/corpse semantics (psychotic break); unchanged |
| `TryReadHealth(EnemyHealth, out float hp)` | Best-effort reflection; false if unreadable |
| `TryIsAlive(EnemyHealth)` | `IsValid` && (`TryReadHealth` && hp > 0) |
| `CountAliveAndNearby(EnemyHealth[], PlayerController?, float range, out int alive, out int nearby)` | Skips invalid; nearby only when player non-null |

## Consumers (ERR-2 adjunct)

| Consumer | Required usage |
|----------|----------------|
| `ErrorReportPayloadCapture.CaptureGameState` | `EnemyScanCache.GetEnemies()` + `CountAliveAndNearby` |
| `DebugServerSystem.CaptureState` | `TryIsAlive` for nearest living enemy |
| `EnemyScanCache`, `PsychoticBreakTrigger` | Existing `IsValid` / `IsAliveForVisibility` (update namespace only) |

## Verification

- BepInEx log after third-party NRE (e.g. DeathMinimap): MUST NOT contain `get_CurrentHealth` from `[ErrorReporter] Failed to process pending logs`.
- `rg '\.CurrentHealth' Systems/ --glob '*.cs'` MUST return no matches.

## Cross-reference

- Reflection inventory: `docs/agents/guides/reflection-inventory.md` (`enemy-health-compat-read`)
- ARCH-3 compat pattern: `docs/adr/0016-arch-3-extension-model.md`
