# Compatibility and optional mods

How Dread degrades when other mods conflict. Player-facing matrix: [docs/mod-compatibility.md](../../mod-compatibility.md). Code: `HarmonyPatchCompat`, `DreadConfig` section 10, `RepoConfigSliderLabelCompat`.

## Compatibility mode

Config: `CompatibilityMode` (section `10. Compatibility`).

When **true**:

| Still runs | Disabled |
|------------|----------|
| Audio Dread (ambient) | Monster Harmony patches (aggression / investigate) |
| Monster audio scan (`MonsterAudioEnabled`) | Adrenaline / panic sprint mutation |
| | Psychotic break |

Use when a profile breaks with full Dread enabled. Document in issue if recommending to users.

## Skip conflicting patches

Config: `CompatibilitySkipConflictingPatches`.

If another mod already owns the target method (`Harmony.GetPatchInfo`), Dread skips apply and logs once. See [harmony-and-patches.md](harmony-and-patches.md).

## Debug console guard

Config: `DebugConsoleGuardEnabled` (default **true**).

`DebugConsoleGuardPatch` suppresses `DebugConsoleUI` NRE spam from broken MenuLib/REPOConfig hooks. Pair with error reporting default off so telemetry does not amplify floods.

## Host-only monster patches

`HarmonyPatchCompat.IsMasterClient()` gates aggression/investigate patches. ADR-0004.

## REPOConfig slider labels

Temporary Harmony compat when REPOConfig assembly is loaded:

- `RepoConfigSliderLabelCompat.TryApply` from `Plugin.Start` and `DreadSystemInitializer`
- Fixes empty slider descriptions from `CreateREPOSlider(name, string.Empty, ...)`

Full timeline: [docs/repo-config-slider-labels-investigation.md](../../repo-config-slider-labels-investigation.md). Also summarized in [domain.md](../domain.md).

Agents: do not hard-reference REPOConfig types; use reflection like existing compat.

## Plugin dependencies (NVorbis)

`PluginDependencyResolver` registers `AssemblyResolve` for DLLs next to `Dread.dll`:

- `NVorbis.dll`, `System.Memory.dll`, etc.

Linux/Proton: ship full Thunderstore folder. Missing `System.Memory.dll` caused false "audio broken" reports.

## Optional mod detection pattern

1. `AppDomain` / `AccessTools.TypeByName` for optional assembly
2. No NuGet dependency on optional mod
3. Log verbose once when skipped
4. Document in [mod-compatibility.md](../../mod-compatibility.md) if user-visible

## Isolation test (for issues)

1. Profile with only BepInEx + Dread
2. Confirm ambient + config sections
3. Add mods one at a time
4. Try compatibility mode before closing as wontfix

## ADRs

- `docs/adr/0004-host-authoritative-monster-changes.md`
- `docs/adr/0001-remove-repolib-and-broken-systems.md` (removed broken visual systems)
