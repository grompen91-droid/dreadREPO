# Mod architecture (current)

Reference for agents implementing features in Dread. Reflects **shipped** layout on `master`, not the 2026-05-16 superpowers bootstrap plan.

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
    -> one DontDestroyOnLoad host per runtime system
```

Entry: `Plugin.cs`. System spawning: `Systems/DreadSystemInitializer.cs` (waits for `UnityEngine.UI` before adding UI-dependent components).

## Runtime systems (one host each)

| System | Host name | Role |
|--------|-----------|------|
| `AudioDreadSystem` | `DreadAudioHost` | Weighted 3D ambient horror during a **Run** |
| `MonsterOverhaulSystem` | `DreadMonsterHost` | Periodic enemy audio tweaks; Harmony lives in same file |
| `TensionSystem` | `DreadTensionHost` | Proximity scan + adrenaline, panic sprint, breath, fake footsteps |
| `PsychoticBreakSystem` | `DreadPsychoticBreakHost` | Client-local episodes |
| `ErrorReporterSystem` | `DreadErrorHost` | Opt-in crash reports to Worker |
| `TestCrashSystem` | `DreadTestCrashHost` | Debug TestCrash |
| `DebugServerSystem` | `DreadDebugHost` | TCP JSON API (default off) |
| `DebugOverlaySystem` | `DreadDebugOverlayHost` | IMGUI HUD (default off) |

Glossary names: [CONTEXT.md](../../../CONTEXT.md).

## Removed systems (do not resurrect without ADR)

`EnvironmentalSystem` and `VisualCorruptionSystem` were removed (ADR-0001). PostProcessing v2 volume patching did not work reliably in R.E.P.O. Do not copy old superpowers tasks that recreate them.

## Config

- Source of truth in code: `Config/DreadConfig.cs`
- On disk: `BepInEx/config/elytraking.dread.cfg` after first run
- Sections include: Audio, Tension, Psychotic Break, Monster, Compatibility, Debug Server, Logging, etc.

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

Players without Dread can join; host-side monster patches still apply from the host.

## Scene gating

Use `SemiFunc.MenuLevel()` for menu/main UI. `MonsterOverhaulSystem` also tracks `_inLevel` from scene name (no Menu/Main). Tension and psychotic break skip work on menu levels.

## Key files

| Area | Path |
|------|------|
| Plugin + patch apply | `Plugin.cs` |
| Config | `Config/DreadConfig.cs` |
| Systems (overview) | `Systems/` (subfolders: `Patches/`, `PsychoticBreak/`, `ErrorReporting/`, `DebugOverlay/`; other hosts at `Systems/*.cs`) |
| Audio assets | `audio/*.ogg` |
| ADRs | `docs/adr/` |
| Per-system agent guides | `docs/agents/guides/README.md` |
| Reflection inventory (ARCH-2) | `docs/agents/guides/reflection-inventory.md` |
| Agent verify | `scripts/verify-dread.ps1`, `docs/agents/verify-dread.md` |

## Adding a new runtime system

1. Create `Systems/YourSystem.cs` as `MonoBehaviour`
2. Register in `DreadSystemInitializer.TryAddSystem<YourSystem>("DreadYourHost")`
3. Subscribe/unsubscribe `SceneManager.sceneLoaded` in `OnDestroy`
4. Gate on `DreadConfig` + `CompatibilityMode` + menu level as needed
5. Publish debug state via `DreadRuntimeState` if overlay/MCP should show it
6. Document terms in `CONTEXT.md` if you introduce new domain language

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
