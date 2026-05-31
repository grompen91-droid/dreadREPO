# Reflection inventory (ARCH-2)

Canonical list of reflection, Harmony `AccessTools` resolution, and Harmony `Traverse` usage in `Systems/`. Maintained for issue [#168](https://github.com/grompen91-droid/dreadREPO/issues/168).

**Last reviewed**: 2026-05-30

### Harmony `Traverse` policy

`Traverse` is in scope for ARCH-2 inventory (same as raw reflection): version-tolerant field/property access when compile-time members are missing or renamed. Rows below mark **keep** unless a site was explicitly **reduce**d with caches (compat layers) or **replace**d with direct stub fields (patch postfixes).

| ID | File | Method / site | Trigger | Optional-mod gate | Stub build | Full build | Disposition | Rationale |
|----|------|---------------|---------|-------------------|------------|------------|-------------|-----------|
| `harmony-enemy-navmesh-apply` | `Systems/Patches/EnemyNavMeshAgentAwakePatch.cs` | `Apply` / `Remove` | startup | none | required | required | **replace** | `EnemyNavMeshAgent` in `Assembly-CSharp` stubs; use `typeof(EnemyNavMeshAgent)` |
| `harmony-enemy-navmesh-postfix` | `Systems/Patches/EnemyNavMeshAgentAwakePatch.cs` | `Postfix` | event (Awake) | none | required | required | **replace** | Postfix uses `EnemyNavMeshAgent.Agent` (stub field) instead of `Traverse` only |
| `harmony-player-controller-apply` | `Systems/Patches/PlayerControllerAwakePatch.cs` | `Apply` / `Remove` | startup | none | required | required | **replace** | `PlayerController` in stubs; use `typeof(PlayerController)` |
| `harmony-player-controller-postfix` | `Systems/Patches/PlayerControllerAwakePatch.cs` | `Postfix` | event (Awake) | none | required | required | **replace** | Direct `CrouchSpeed` field on `PlayerController` |
| `harmony-enemy-director-apply` | `Systems/Patches/EnemyDirectorSetInvestigatePatch.cs` | `Apply` / `Remove` | startup | none | required | required | **replace** | `EnemyDirector` in stubs; use `typeof(EnemyDirector)` |
| `harmony-debug-console-apply` | `Systems/Patches/DebugConsoleGuardPatch.cs` | `Apply` / `Remove` | startup | none | required | required | **keep** | `DebugConsoleUI` not in stub assemblies; `TypeByName` + skip if missing |
| `harmony-patch-compat-master` | `Systems/Core/HarmonyPatchCompat.cs` | `IsMasterClient` | per patch gate | none | required | required | **replace** | `SemiFunc` in stubs; cache `MethodInfo` for `IsMasterClient` |
| `harmony-patch-compat-foreign` | `Systems/Core/HarmonyPatchCompat.cs` | `ShouldSkipDueToForeignPatches` | startup | none | required | required | **keep** | `Harmony.GetPatchInfo`; no compile-time alternative |
| `tension-sprint-multiplier` | `Systems/TensionSystem.cs` | `Traverse` on `SprintSpeedMultiplier` | event (panic sprint / restore) | none | required | required | **keep** | Field not exposed on stub `PlayerController`; panic sprint gameplay |
| `enemy-health-compat-read` | `Systems/Core/EnemyHealthCompat.cs` | `Traverse.Create` + member name scan | per visibility check | none | required | required | **keep** | Version-tolerant HP field names on `EnemyHealth` |
| `psychotic-break-lockdown` | `Systems/PsychoticBreak/PsychoticBreakPlayerLockdown.cs` | `Traverse` bool fields (`inputLocked`, etc.) | episode | none | required | required | **keep** | Alternate field names across game builds |
| `player-controller-compat-crouch` | `Systems/Core/PlayerControllerCompat.cs` | `TryReadCrouch` field scan | per call (hide check) | none | required | required | **reduce** | Cache bool fields matching crouch/crawl per `Type`; Traverse tried first |
| `player-tumble-compat-resolve` | `Systems/Core/PlayerTumbleCompat.cs` | `ResolveTumble` / `GetLocalAvatar` | per call | none | required | required | **reduce** | Cache `tumble` `FieldInfo` per avatar type; `PlayerAvatar` still `TypeByName` (not in stubs) |
| `player-tumble-compat-invoke` | `Systems/Core/PlayerTumbleCompat.cs` | `InvokeTumble` | event | none | required | required | **reduce** | Cache `TumbleSet` / `TumbleRequest` `MethodInfo` per tumble runtime type |
| `player-tumble-compat-scan` | `Systems/Core/PlayerTumbleCompat.cs` | `IsInTumble` field scan | per call | none | required | required | **reduce** | Same field cache pattern as controller compat |
| `repoconfig-slider-compat` | `Systems/Core/RepoConfigSliderLabelCompat.cs` | Harmony + UI field/property access | on slider create | REPOConfig + MenuLib | required | required | **keep** | DBG-4 owns removal; optional mod types absent from stubs |
| `psychotic-break-overlay-ui` | `Systems/PsychoticBreak/PsychoticBreakOverlay.cs` | UI type/property resolution | episode | none | required | required | **keep** | Unity UI deferred load; stub UI types incomplete for compile-time bind |
| `psychotic-break-hallucination` | `Systems/PsychoticBreak/PsychoticBreakHallucination.cs` | Photon strip, attack anim/method, `PlayerHealth` hurt prefix | episode | none | required | required | **keep** | Local clone + version-tolerant attack/damage hooks |
| `dread-init-ui-load` | `Systems/DreadSystemInitializer.cs` | `EnsureUnityEngineUiLoaded` | startup | none | required | required | **keep** | Defers psychotic break / UI systems until `UnityEngine.UI` loads |
| `debug-overlay-patch-count` | `Systems/DebugOverlay/DebugOverlayPanel.cs` | `CountDreadPatches` | on-demand (0.5s when visible) | none | required | required | **keep** | `Harmony.GetAllPatchedMethods`; PERF-2: only when overlay visible (`DebugOverlaySystem.Update`) |
| `debug-server-read-player` | `Systems/DebugServerSystem.cs` | `ReadPlayerFloat` | MCP command | debug server on | required | required | **keep** | Uses Harmony `Traverse`, not raw reflection; version-tolerant member names |
| `debug-server-find-enemies` | `Systems/DebugServerSystem.cs` | `FindObjectsOfType<EnemyHealth>` | MCP command | debug server on | optional | required | **keep** | Compile-time generic; not reflection |
| `audio-clip-loader-handler` | `Systems/AudioClipLoader.cs` | `GetDownloadHandlerError` | load | none | required | required | **keep** | Stub `downloadHandler` shape; one-time property lookup per handler type |
| `overlay-texture-format` | `Systems/OverlayTextureUtil.cs` | `IsFormatUsable` | texture create | none | required | required | **keep** | `SupportsTextureFormat` missing or throws on some Proton builds |
| `monster-overhaul-isplaying` | `Systems/MonsterOverhaulSystem.cs` | `AudioSource.isPlaying` property | periodic | none | optional | required | **keep** | Fallback if property absent; low frequency |
| `error-payload-game-state` | `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` | `CaptureGameState` | log batch | error reporting on | optional | required | **keep** | Uses `EnemyScanCache` + `EnemyHealthCompat.CountAliveAndNearby` (no compile-time `CurrentHealth`) |
| `error-payload-exception-name` | `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` | `ex.GetType().Name` | log batch | error reporting on | required | required | **keep** | Standard exception metadata, not game API reflection |
| `plugin-dependency-resolve` | `Systems/PluginDependencyResolver.cs` | `AssemblyResolve` | startup | none | required | required | **keep** | Load NVorbis etc. from plugin folder |
| `psychotic-break-find-players` | `Systems/PsychoticBreak/PsychoticBreakTrigger.cs` | `FindObjectsOfType<PlayerController>` | 0.25s / 2s | none | optional | required | **keep** | Compile-time generic; gameplay scan interval |

## Hot-path summary

| Trigger | Sites | ARCH-2 action |
|---------|-------|---------------|
| per-frame | `PlayerControllerCompat`, `PlayerTumbleCompat` (via psychotic break / tension) | **reduce**: per-type caches |
| event (gameplay) | `TensionSystem` sprint multiplier `Traverse` | **keep**: version-tolerant field |
| when overlay visible | `CountDreadPatches` | **keep**: already gated (PERF-2) |
| startup | Harmony patch `Apply`, REPOConfig, UI load | **replace** where stub types exist |
| event / on-demand | Patches postfix, error capture, MCP | Mostly **keep** |

## Stub type coverage

Types defined in `.github/scripts/Assembly-CSharp_stubs.cs` and safe for `typeof()` in patch `Apply`:

- `EnemyNavMeshAgent`, `EnemyDirector`, `PlayerController`, `SemiFunc`, `EnemyHealth`

Types **not** in stubs (keep `TypeByName` or assembly scan):

- `DebugConsoleUI`, `PlayerAvatar`, MenuLib / REPOConfig types, full Unity UI graph

## Related

- [mod-architecture.md](mod-architecture.md): stub vs full build profiles
- [contracts/build-profiles.md](../../../specs/001-arch-2-reduce-reflection/contracts/build-profiles.md)
- [harmony-and-patches.md](harmony-and-patches.md): patch apply/remove flow
