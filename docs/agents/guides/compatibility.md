# Compatibility and optional mods

How Dread degrades when other mods conflict. Player-facing matrix: [docs/mod-compatibility.md](../../mod-compatibility.md). Code: `HarmonyPatchCompat`, `DreadConfig` section 6, `RepoConfigSliderLabelCompat`.

## Compatibility mode

Config: `CompatibilityMode` (section `10. Compatibility`).

When **true**:

| Still runs | Disabled |
|------------|----------|
| Audio Dread (ambient) | Monster Harmony patches (aggression / investigate) |
| Monster audio scan (`MonsterAudioEnabled`) | Adrenaline / panic sprint mutation |
| | Low stamina sound |
| | Fake footsteps |
| | Psychotic break |

Use when a profile breaks with full Dread enabled. Document in issue if recommending to users.

## Skip conflicting patches

Config: `CompatibilitySkipConflictingPatches`.

If another mod already owns the target method (`Harmony.GetPatchInfo`), Dread skips apply and logs once. See [harmony-and-patches.md](harmony-and-patches.md).

## Debug console guard

Config: `DebugConsoleGuardEnabled` (default **true**).

`DebugConsoleGuardPatch` suppresses `DebugConsoleUI` NRE spam from broken MenuLib/REPOConfig hooks. Pair with `ErrorReportingEnabled = false` if telemetry should not amplify floods.

## Host-only monster patches

`HarmonyPatchCompat.IsMasterClient()` gates aggression/investigate patches. ADR-0004.

## REPOConfig (in-game menu)

Temporary Harmony compat when the `REPOConfig` assembly is loaded. Entry point: `RepoConfigCompat.TryApply` from `Plugin.Start` and `DreadSystemInitializer`. Do not hard-reference REPOConfig types; use reflection like existing compat.

**Slider label workaround:** full investigation timeline and rejected approaches in [docs/repo-config-slider-labels-investigation.md](../../repo-config-slider-labels-investigation.md). Agents changing REPOConfig UI should read that file before editing `RepoConfigSliderLabelCompat`.

### Hidden from REPOConfig (`HideFromREPOConfig` tag)

None for Dread currently. All sections including `11. Testing` (`Crash Game` toggle) appear in REPOConfig.

### Error reporting disclosure (REPOConfig limitation)

REPOConfig upstream ([`REPOConfig/Entry.cs`](https://github.com/IsThatTheRealNick/REPOConfig): `showDescriptions` removed; [`ConfigMenu.cs`](https://github.com/IsThatTheRealNick/REPOConfig) passes `string.Empty` for slider descriptions and has **no** bool toggle description path). `CreateREPOLabel` is only used for **section headers** (bold orange), not setting help text. Dread does **not** inject privacy copy into REPOConfig.

| Surface | Text |
|---------|------|
| `elytraking.dread.cfg` / Configuration Manager (F1) | `ErrorReportingPrivacyCopy.FullDescription` on `ErrorReportingEnabled` |
| REPOConfig | Toggle **Error Reporting Enabled** only (no description field) |

### Slider labels

Fixes empty slider descriptions from `CreateREPOSlider(name, string.Empty, ...)`. Full timeline: [docs/repo-config-slider-labels-investigation.md](../../repo-config-slider-labels-investigation.md). Also summarized in [domain.md](../domain.md).

## Plugin dependencies (NVorbis)

`PluginDependencyResolver` registers `AssemblyResolve` for DLLs next to `Dread.dll`:

- `NVorbis.dll`, `System.Memory.dll`, etc.

Linux/Proton: ship full Thunderstore folder. Missing `System.Memory.dll` caused false "audio broken" reports.

## Optional mod detection pattern

1. `AppDomain` / `AccessTools.TypeByName` for optional assembly
2. No NuGet dependency on optional mod
3. Log verbose once when skipped
4. Document in [mod-compatibility.md](../../mod-compatibility.md) if user-visible

## ARCH-3 manual matrix (issue #175)

Record results in the PR when changing init or compat paths. Stub CI covers Tier 0 only; full-game rows are manual.

| Scenario | Steps | Expected |
|----------|-------|----------|
| REPOConfig absent | Profile without REPOConfig/MenuLib | Dread loads; `RepoConfigSliderLabelCompat` no-ops; verbose may note skip |
| Compatibility mode on | `6. Compatibility` → `CompatibilityMode` true | Monster patches off via `PatchLifecycle` / `HarmonyPatchRegistry`; ambient runs; psychotic break/tension mutations off per table above |
| Non-host client | Join host as client | Local audio/tension OK; monster Harmony postfixes gated by `HarmonyPatchCompat.IsMasterClient()` |
| Foreign patch owner | Another mod patches same method; `SkipConflictingPatches` true | Dread skips apply; one log line |
| Stub CI | `verify-dread.ps1` Tier 0 | Build + `arch3_try_add_system` pass |

Spec kit copy: [specs/002-arch-3-extensible-core/quickstart.md](../../../specs/002-arch-3-extensible-core/quickstart.md).

## Isolation test (for issues)

1. Profile with only BepInEx + Dread
2. Confirm ambient + config sections
3. Add mods one at a time
4. Try compatibility mode before closing as wontfix

## ADRs

- `docs/adr/0016-arch-3-extension-model.md` (registry, boot order, ARCH-4 boundary)
- `docs/adr/0004-host-authoritative-monster-changes.md`
- `docs/adr/0001-remove-repolib-and-broken-systems.md` (removed broken visual systems)
