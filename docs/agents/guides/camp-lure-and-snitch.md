# Camp Lure and Snitch (host monster features)

Agent guide for **Camp Lure** (`CampLureSystem`) and **Snitch** (`SnitchSystem`). Feature spec and verification live under Spec Kit, not `docs/superpowers/`.

## Spec Kit (source of truth)

| Artifact | Path |
|----------|------|
| Spec | [specs/006-lure-snitch-hardening/spec.md](../../../specs/006-lure-snitch-hardening/spec.md) |
| Plan | [specs/006-lure-snitch-hardening/plan.md](../../../specs/006-lure-snitch-hardening/plan.md) |
| Tasks | [specs/006-lure-snitch-hardening/tasks.md](../../../specs/006-lure-snitch-hardening/tasks.md) |
| Manual QA | [specs/006-lure-snitch-hardening/quickstart.md](../../../specs/006-lure-snitch-hardening/quickstart.md) |
| Phase gate contract | [specs/006-lure-snitch-hardening/contracts/gameplay-phase-gate.md](../../../specs/006-lure-snitch-hardening/contracts/gameplay-phase-gate.md) |
| Lure config contract | [specs/006-lure-snitch-hardening/contracts/camp-lure-config.md](../../../specs/006-lure-snitch-hardening/contracts/camp-lure-config.md) |

Pinned in [`.specify/feature.json`](../../../.specify/feature.json).

## Phase gating

Host monster features use `GameplayContext.AllowsHostMonsterFeatures` (active **run** only):

- **menu**: `SemiFunc.MenuLevel()` or native menu probes
- **truck/shop**: `RunIsLobbyMenu`, `RunIsShop`, `SharedSceneData.IsInShop`, etc.
- **run**: `SharedSceneData.IsInGame`, native run probes, or latch from `SemiFunc.OnLevelGenDone`

Overlay **Phase** row shows `menu`, `truck/shop`, or `run` (not `extraction`).

Implementation: `Systems/Core/GameplayPhaseCompat.cs`, latch from `Systems/Patches/SnitchLevelGenDonePatch.cs` (always registered; not snitch-gated).

## Camp Lure

- Host-only anti-camping pulls via `EnemyLureCompat.Pull`
- Per-player cooldown after contact (`LureCooldownSeconds`, default 60)
- Skips when `ProximityScan.HasEnemies()` is false
- Config: `DreadConfig` section `2. Monster Overhaul`

## Snitch

- One random valuable per run; pickup triggers bang + POI reissues
- Arms after level gen (`OnLevelGenDone`) and 5s delay; retries if no items
- Pickup: 2s grace, then baseline parent/kinematic/position (spawn parent is not pickup)
- `ItemRosterCompat` for inactive valuables
- Config: `SnitchEnabled`, `SnitchPOIDurationSeconds`

## Related issues

- [#222](https://github.com/grompen91-droid/dreadREPO/issues/222): snitch arm timer reset on additive scene loads (fixed on this branch)

## See also

- [mod-architecture.md](mod-architecture.md) (scene gating, adding systems)
- [monster-overhaul.md](monster-overhaul.md) (enemy patches, `EnemyLureCompat`)
- [reflection-inventory.md](reflection-inventory.md) (`gameplay-phase-compat`, `item-roster-compat`, `snitch-level-gen-done`)
