# Monster overhaul

**Monster Overhaul** covers host-side aggression Harmony patches and client-side periodic enemy audio treatment. Code: `Systems/MonsterOverhaulSystem.cs` (patches in same file).

## Two halves

| Half | Mechanism | Config |
|------|-----------|--------|
| Aggression | Harmony on `EnemyNavMeshAgent`, `EnemyDirector` | `MonsterAggressionEnabled`, blocked by `CompatibilityMode` |
| Audio treatment | Coroutine `MonsterAudioLoop` every 4s | `MonsterAudioEnabled` |

Harmony details: [harmony-and-patches.md](harmony-and-patches.md).

## Audio loop

1. Every **4 seconds** while `_inLevel` and not menu
2. `FindObjectsOfType<EnemyHealth>()`
3. Skip enemies with `DreadAudioTweaked` marker component
4. Add marker + `ApplyAudioTweaks`: random pitch, full 3D spatial blend on child `AudioSource`s

Does not share enemy lists with `TensionSystem`. Independent scan is intentional.

## Aggression patches (host)

**NavMesh awake postfix:** multiply `NavMeshAgent.speed` and `acceleration` by **1.2** when aggression enabled.

**Investigate prefix:** multiply investigate `radius` by **1.5** (capped below `float.MaxValue`). Comment in source: higher multipliers caused Photon sync issues.

Both require `HarmonyPatchCompat.IsMasterClient()`.

## Confirmed game types

Binary analysis notes at top of `MonsterOverhaulSystem.cs`:

- `EnemyNavMeshAgent` with `Agent` (NavMeshAgent), speed fields via Traverse
- `EnemyParent` referenced for lifecycle research

Use dnSpy / ILSpy on `Assembly-CSharp.dll` when stubs are insufficient.

## Optional mods

Works with extra enemy packs (e.g. Mimic, WesleysEnemies) as long as they use `EnemyHealth` on prefabs. No hard dependency.

## Compatibility

| Flag | Effect on monster overhaul |
|------|----------------------------|
| `CompatibilityMode` | Patches not applied; audio loop may still run unless other code blocks |
| `CompatibilitySkipConflictingPatches` | Skip patch if foreign Harmony owner on target method |

See [docs/mod-compatibility.md](../../mod-compatibility.md) for player-facing matrix.

## Agents: common tasks

| Task | Where to edit |
|------|----------------|
| Tune speed multiplier | `EnemyNavMeshAgentAwakePatch.Postfix` |
| Tune investigate radius | `EnemyDirectorSetInvestigatePatch.Prefix` |
| Change audio scan rate | `MonsterAudioLoop` wait seconds |
| New monster-facing Harmony | New static class + `Plugin.ApplyMonsterPatches` wiring |

## Historical docs

Archive: `docs/agents/archive/superpowers/specs/2026-05-16-dread-design.md` (System 4), `docs/agents/archive/superpowers/plans/2026-05-16-dread.md` (Tasks 6+). Design spec described HP multiplier and visual monster effects not present in current code; treat archive as aspirational history.
