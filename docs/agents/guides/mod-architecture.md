# Mod architecture (current)

Reference for agents implementing features in Dread. Reflects **shipped** layout on `master`, not the 2026-05-16 superpowers bootstrap plan.

**Source of truth for runtime systems:** `Systems/DreadSystemRegistry.cs` (also enforced by `scripts/verify-dread.ps1` `arch3_registry_manifest`).

## Stack

| Layer | Choice |
|-------|--------|
| Game | R.E.P.O. (Unity, Photon multiplayer) |
| Mod loader | BepInEx 5.4.x |
| Patching | Harmony 2 (`HarmonyLib`) |
| Target | .NET Framework 4.8 (`net48`) |
| Build (CI / cloud) | Stub assemblies under `.github/stubs/refs/` |

## Boot sequence

```
Plugin.Awake()
  -> DreadConfig.Initialize
  -> ApplyMonsterPatches() + optional PlayerController / DebugConsole patches
  -> SceneManager.sceneLoaded (deferred)

First gameplay-ready scene:
  DreadSystemInitializer.TryInitialize()
    -> loop DreadSystemRegistry (Core then Debug)
    -> one DontDestroyOnLoad host per registered system
```

Entry: `Plugin.cs` (Harmony + config only). Registry: `Systems/DreadSystemRegistry.cs`. Init loop: `Systems/DreadSystemInitializer.cs` (waits for `UnityEngine.UI` before UI-dependent components). See [ADR-0016](../../adr/0016-arch-3-extension-model.md).

## Runtime systems (one host each)

### Core (nine, all production builds)

| Id | System | Host name | Role |
|----|--------|-----------|------|
| `audio-dread` | `AudioDreadSystem` | `DreadAudioHost` | Weighted 3D ambient horror during a **Run** |
| `monster-overhaul` | `MonsterOverhaulSystem` | `DreadMonsterHost` | Periodic enemy audio tweaks; Harmony lives in same file |
| `tension` | `TensionSystem` | `DreadTensionHost` | Proximity scan + adrenaline, panic sprint, breath, fake footsteps |
| `error-reporter` | `ErrorReporterSystem` | `DreadErrorHost` | Opt-in crash reports to Worker |
| `error-reporting-prompt` | `ErrorReportingPromptSystem` | `DreadErrorReportingPromptHost` | First-run privacy prompt before telemetry sends |
| `psychotic-break` | `PsychoticBreakSystem` | `DreadPsychoticBreakHost` | Client-local episodes |
| `notifications` | `DreadNotificationSystem` | `DreadNotificationHost` | Transient corner toasts (overlay, lure, snitch, etc.) |
| `camp-lure` | `CampLureSystem` | `DreadCampLureHost` | Host anti-camping lure during active **run** |
| `snitch` | `SnitchSystem` | `DreadSnitchHost` | Host snitch item bang + enemy POI during active **run** |

### Debug (development builds only, `#if DREAD_DEBUG`)

| Id | System | Host name | Role |
|----|--------|-----------|------|
| `test-crash` | `TestCrashSystem` | `DreadTestCrashHost` | Intentional crash for error-reporting QA |
| `debug-server` | `DebugServerSystem` | `DreadDebugHost` | Localhost TCP JSON API for MCP/agents |
| `debug-overlay` | `DebugOverlaySystem` | `DreadDebugOverlayHost` | IMGUI HUD (F10 when enabled) |

Glossary names: [CONTEXT.md](../../../CONTEXT.md). Per-system guides: [README.md](README.md).

## Removed systems (do not resurrect without ADR)

`EnvironmentalSystem` and `VisualCorruptionSystem` were removed (ADR-0001). PostProcessing v2 volume patching did not work reliably in R.E.P.O. Do not copy old superpowers tasks that recreate them.

## Systems/Core (shared compat)

Version-tolerant access to game types (`EnemyHealth`, `PlayerController`, optional REPOConfig, Harmony gates) lives under `Systems/Core/` in namespace `Dread.Systems.Core`. Feature systems MUST use Core helpers instead of compile-time properties that may differ between stubs and REPO v0.4.x.

| Helper | Role |
|--------|------|
| `EnemyHealthCompat` | HP read, alive/nearby counts, validity |
| `PlayerControllerCompat` | Health, crouch/hide checks |
| `PlayerTumbleCompat` | Tumble pose for psychotic break |
| `HarmonyPatchCompat` | Host-only and foreign-patch skip |
| `RepoConfigCompat` / `RepoConfigSliderLabelCompat` | Optional REPOConfig UI |
| `UnityWebRequestCompat` | Stub-safe UWR probe |
| `ProximityScan` | Shared nearest-enemy distance for tension, monster audio, lure, snitch, debug |
| `GameplayContext` / `GameplayPhaseCompat` | Menu vs truck/shop vs run gating |

Contract for enemy HP: `specs/004-err-2-default-on-prompt/contracts/core-enemy-health.md`.

## Config

- Source of truth in code: `Config/DreadConfig.cs`
- Section headers: `Config/DreadConfigSections.cs` (numbers differ production vs `DREAD_DEBUG`)
- On disk: `BepInEx/config/elytraking.dread.cfg` after first run
- Sections include: Audio, Tension, Psychotic Break, Monster, Compatibility, Error Reporting, Logging, etc.

Rules for agents:

- Add new entries in `DreadConfig.cs` with clear section names and descriptions
- Wire `debugKey` in `DebugServerSystem` if the value should be MCP-tunable
- Respect `CompatibilityMode`: many gameplay mutations no-op when true

## Host vs client

| Behavior | Authority |
|----------|-----------|
| Monster NavMesh / investigate Harmony patches | Host only (`HarmonyPatchCompat.IsMasterClient()`) |
| Ambient audio, tension, psychotic break | Per client (local) |
| Monster audio pitch/spatial tweaks | Per client scan (`FindObjectsOfType<EnemyHealth>`) |
| Camp lure, snitch | Host only during active **run** |

Players without Dread can join; host-side monster patches still apply from the host.

## Scene gating

Use `SemiFunc.MenuLevel()` for menu/main UI. `MonsterOverhaulSystem` also tracks `_inLevel` from scene name (no Menu/Main). Tension and psychotic break skip work on menu levels.

**Host monster features** (Camp Lure, Snitch) MUST gate on `GameplayContext.AllowsHostMonsterFeatures`, not merely `!SemiFunc.MenuLevel()`. That API resolves menu vs truck/shop vs extraction level via `GameplayPhaseCompat` (native `SemiFunc` probes when present, plus extraction latch from `OnLevelGenDone`). See `specs/006-lure-snitch-hardening/contracts/gameplay-phase-gate.md`.

## Key files

| Area | Path |
|------|------|
| Plugin + patch apply | `Plugin.cs` |
| Config | `Config/DreadConfig.cs` |
| Core compat | `Systems/Core/` |
| Systems (overview) | `Systems/` (subfolders: `Core/`, `Patches/`, `PsychoticBreak/`, `ErrorReporting/`, `DebugOverlay/`; other hosts at `Systems/*.cs`) |
| Audio assets | `audio/*.ogg` |
| ADRs | `docs/adr/` |
| Per-system agent guides | `docs/agents/guides/README.md` |
| Reflection inventory (ARCH-2) | `docs/agents/guides/reflection-inventory.md` |
| Agent verify | `scripts/verify-dread.ps1`, `docs/agents/verify-dread.md` |

## Adding a new runtime system

Follow [specs/002-arch-3-extensible-core/contracts/system-lifecycle.md](../../../specs/002-arch-3-extensible-core/contracts/system-lifecycle.md):

1. Create `Systems/YourSystem.cs` as `MonoBehaviour`
2. Add config entries in `Config/DreadConfig.cs`
3. Add one row to `Systems/DreadSystemRegistry.cs` (Core or Debug group; optional `IsEnabled` predicate)
4. **Do not** add `TryAddSystem` in `Plugin.cs`
5. Subscribe/unsubscribe `SceneManager.sceneLoaded` in `OnDestroy`
6. Gate on `DreadConfig` + `CompatibilityMode` + menu level inside the system (or `IsEnabled` on the row)
7. Publish fields on `DreadRuntimeState` if overlay/MCP should show live values ([ADR-0016](../../adr/0016-arch-3-extension-model.md))
8. Document new domain terms in `CONTEXT.md`
9. Add the `SystemType` name to `$arch3CoreTypes` or `$arch3DebugTypes` in `scripts/verify-dread.ps1` and the table in `specs/002-arch-3-extensible-core/contracts/extension-registry.md`

## Build profiles (stub vs full)

*Estimated read time: ~3 minutes for this section; ~5 minutes with [reflection-inventory.md](reflection-inventory.md) hot-path summary.*

Dread supports two MSBuild profiles. Full contract: [specs/001-arch-2-reduce-reflection/contracts/build-profiles.md](../../../specs/001-arch-2-reduce-reflection/contracts/build-profiles.md).

| Profile | `GameDir` | Use when |
|---------|-----------|----------|
| **Stub** (CI / cloud) | `.github/stubs/refs` | No R.E.P.O. install; agents and GitHub Actions |
| **Full** (local) | Game `REPO_Data/Managed` | Stronger compile-time checks; deploy to r2modman profile |

### Stub profile (default for agents)

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
pwsh -NoProfile ./scripts/verify-dread.ps1
```

Stub builds may still use **runtime** reflection for optional mods (REPOConfig, MenuLib) and deferred Unity UI. Required sites are listed in [reflection-inventory.md](reflection-inventory.md).

### Full profile (Linux r2modman example)

Replace paths with your Steam install and profile name:

```bash
dotnet build Dread.csproj -c Release \
  -p:GameDir="$HOME/.local/share/Steam/steamapps/common/REPO/REPO_Data/Managed" \
  -p:BepInExDir="$HOME/.config/r2modmanPlus-local/REPO/profiles/<profile>/BepInEx" \
  -p:PluginDir="$HOME/.config/r2modmanPlus-local/REPO/profiles/<profile>/BepInEx/plugins/elytraking-Dread" \
  -p:DeployToProfile=true
```

After deploy, smoke: launch R.E.P.O. from that profile, confirm BepInEx loads Dread, menu level does not throw, start a run if possible.

### Verification tiers

| Tier | Stub | Full game |
|------|------|-----------|
| Tier 0 (`verify-dread.ps1`) | Required for merge | Not required in CI |
| Tier 1 MCP | Optional | Game running + debug server |
| In-game smoke | Optional | Full build + deploy |

## Build (agents without game install)

See [AGENTS.md](../../../AGENTS.md) and [verify-dread.md](../verify-dread.md) Tier 0.
