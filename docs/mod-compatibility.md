# Mod compatibility

Dread is **dependency-free** (BepInEx only) and works alongside many popular REPO mods, but **no mod can guarantee compatibility with every other mod**. Harmony patches, shared game types, and load order all affect behavior.

## What limits compatibility

| Category | What Dread does | Risk |
|----------|-----------------|------|
| Harmony on shared methods | Postfix/prefix on `EnemyNavMeshAgent.Awake`, `PlayerController.Awake`, `EnemyDirector.SetInvestigate` | Another mod patching the same method can win or lose by load order |
| Error telemetry | `Application.logMessageReceived` (opt-in, default off) | Was catastrophic when paired with broken mods flooding `Debug.LogException` (mitigated in v1.5.2+) |
| Debug console guard | Optional finalizer on `DebugConsoleUI.Update` | Masks NREs from broken third-party hooks; console may still be broken underneath |
| Runtime player mutation | `TensionSystem` changes sprint drain and `SprintSpeedMultiplier` | Conflicts with stamina or sprint overhaul mods |
| Enemy audio scan | `MonsterOverhaulSystem` tweaks child `AudioSource` pitch/spatial blend | Can clash with mods that own enemy audio |
| Shipped assemblies | NVorbis + `System.Memory` in the plugin folder | Rare `AssemblyResolve` conflicts |
| Linux / Proton | NVorbis disk load + Wine path mapping | Missing DLLs or `file://` issues looked like mod conflicts |
| Multiplayer | Monster patches are **host-only** when `SemiFunc.IsMasterClient()` is available | Clients without Dread still join; host runs aggression patches |

## Known mods (mk profile and common stacks)

| Mod | Risk | Notes |
|-----|------|-------|
| Zehs-REPOLib | Low | Bundle/assets; no Dread patch overlap |
| randomlygenerated-Mimic_Patcher | Low | Removes Mimics `SetupEnemies` patch only |
| eth9n-Mimic | Low | Works with Mimic_Patcher |
| BULLETBOT-MoreUpgrades | Low | |
| Magic_Wesley-Wesleys_Enemies | Low | Dread aggression/audio apply via `EnemyHealth` |
| nickklmao-MenuLib / REPOConfig | Low to medium | Can break `SemiFunc.DebugTester`; use `DebugConsoleGuard` (default on). Slider labels: Dread **temporary** compat when REPOConfig present ([investigation](repo-config-slider-labels-investigation.md)) |
| elytraking-Dread | — | |

**Mimic_Patcher (`3.0.0-Final`) is unrelated** to Dread audio. Its startup warning only removes a conflicting **Mimics** patch.

## DebugConsoleUI spam (MenuLib / REPOConfig)

`DebugConsoleUI.Update` may call `SemiFunc.DebugTester` through a broken Harmony hook, causing `NullReferenceException` every frame.

Dread mitigations:

- `DebugConsoleGuardPatch` (config: `DebugConsoleGuardEnabled`, default **true**): finalizer suppresses NRE spam
- `ErrorReportingEnabled` default **false**: telemetry no longer amplifies other mods' error floods

Disable the guard if you need vanilla debug console exception behavior for diagnosing other mods.

## Isolation test

1. Duplicate your r2modman profile.
2. Enable **only** BepInEx + Dread (add REPOLib only if you use it for other mods).
3. Launch, enter a run, confirm ambient audio and config sections load.
4. Add mods **one at a time**, relaunch, note first mod that breaks audio, console, or FPS.
5. If a stack breaks, enable **Compatibility mode** in config (ambient audio only) or disable individual toggles.

Confirmed audio failure pattern (Linux): `System.Memory.dll` missing from `BepInEx/plugins/elytraking-Dread/` (only `NVorbis.dll` deployed). Not caused by Mimic or REPOLib.

## Linux and Proton

Ship the full plugin folder from the Thunderstore zip:

- `Dread.dll`
- `NVorbis.dll`, `System.Memory.dll`, `System.Buffers.dll`, `System.Numerics.Vectors.dll`, `System.Runtime.CompilerServices.Unsafe.dll`
- `audio/*.ogg`

Dread loads OGG via NVorbis disk read first, then `UnityWebRequest` on Windows-native paths.

## Config toggles for broken profiles

| Key | Section | Default | Effect |
|-----|---------|---------|--------|
| `CompatibilityMode` | 10. Compatibility | false | Ambient audio only: no monster Harmony patches, no adrenaline/panic sprint mutation, no psychotic break |
| `CompatibilitySkipConflictingPatches` | 10. Compatibility | false | Skip Dread Harmony patch if target method already patched by another mod |
| `ErrorReportingEnabled` | 5. Error Reporting | false | Opt-in anonymous crash telemetry |
| `DebugConsoleGuardEnabled` | 10. Compatibility | true | Suppress DebugConsoleUI NRE spam from broken hooks |
| `MonsterAggressionEnabled` | 2. Monster Overhaul | true | Host-only speed/investigate patches |
| `AdrenalineEnabled` / `PanicSprintEnabled` | 3. Tension | true | Disable if sprint/stamina mods conflict |

## Manual compatibility test matrix

Use this checklist before a release or after Harmony changes.

### Baseline

- [ ] BepInEx + Dread only: mod loads, no `BadImageFormatException` in log
- [ ] Main menu 2 min: stable FPS, log growth under ~50 lines/min
- [ ] Enter run: ambient audio plays within 2 min
- [ ] Config: `CompatibilityMode` off, monster/tension defaults on

### Popular stacks (add to baseline profile)

- [ ] + Zehs-REPOLib: still loads, ambient audio OK
- [ ] + eth9n-Mimic + Mimic_Patcher: enemies spawn, no startup Harmony errors from Dread
- [ ] + Magic_Wesley-Wesleys_Enemies: aggression/audio scan runs on modded enemies
- [ ] + BULLETBOT-MoreUpgrades: no sprint/stamina regression with tension on
- [ ] + nickklmao-MenuLib + REPOConfig: menu stable; enable `DebugConsoleGuard` if console NRE spam
- [ ] `CompatibilityMode = true`: only ambient audio; no psychotic break, no sprint mutation

### Linux Proton smoke

- [ ] Full DLL set in plugin folder (see Linux section)
- [ ] Ambient + psychotic break audio clips load (check log for `[PsychoticBreak] Loaded`)
- [ ] No log explosion in menu 2 min
- [ ] `ErrorReportingEnabled = false` by default unless testing telemetry

### Telemetry test (optional)

- [ ] Set `ErrorReportingEnabled = true`
- [ ] Click **Crash Game** in ConfigurationManager (or set `Crash Game = true` for one frame)
- [ ] Game exits with `[Dread TestCrash]` in log; report reaches worker if enabled

## Related docs

- [ADR-0013: Debug server and MCP verification](adr/0013-debug-server.md)
- [Agent verify runbook](agents/verify-dread.md)
- [ADR-0004: Host-authoritative monster changes](adr/0004-host-authoritative-monster-changes.md)
- [ADR-0009: Toggleable Harmony patches](adr/0009-toggleable-harmony-patches.md)
- [ADR-0010: Error telemetry](adr/0010-error-telemetry.md)
- [ADR-0012: Test crash button](adr/0012-test-crash-button.md)
- [ADR-0015: Error report JSON serialization](adr/0015-error-report-json-serialization.md)
