# Changelog

All notable changes to **Dread** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

## [1.5.2] - 2026-05-22

![Status](https://img.shields.io/badge/status-development-yellow?style=flat-square)

### Changed
- CI pipeline: 4-job architecture (relevance, build, analyze, summary) per spec v1.2
- CI pipeline: relevance hard gate skips build/analyze on non-project PRs
- CI pipeline: summary table prints all job results and exits 1 on failure
- CI pipeline: MSBuild log uploaded as artifact on build failure
- CI pipeline: anti-pattern and format checks fail the build (hard gates)
- CD pipeline: version-specific release tags (`vX.Y.Z` instead of reusing `vmajor`/`vminor`/`vpatch`)
- CD pipeline: idempotency check prevents duplicate releases
- CD pipeline: divergence guard prevents master branch desync on concurrent pushes
- CD pipeline: release step runs before post-release issue creation
- CD pipeline: migrated build to `ubuntu-latest` with NuGet and stubs caching
- CD pipeline: added Thunderstore auto-publish via `tcli`
- CD pipeline: `THUNDERSTORE_README.md` support in package zip

---

## [Unreleased]

![Status](https://img.shields.io/badge/status-development-yellow?style=flat-square)

> **Highlight:** Honest mod compatibility docs, opt-in telemetry, Compatibility mode for broken profiles, and host-only monster patch guards.

<details>
<summary>Planned (not in this release)</summary>

Tracked in [docs/ROADMAP.md](docs/ROADMAP.md):

- Debug overlay: draggable panel, better config, font fixes
- Codebase refactor into smaller files; fewer reflection/DLL dependencies where possible
- Extensibility and hardened core (extension points, fail-safe init, compat patterns)
- Performance pass
- Error reporting: full test matrix; default-on + first-run opt-in/out prompt (today default is off, ADR-0010)

GitHub backlog: issues #163-#175, table in [docs/ROADMAP.md](docs/ROADMAP.md).

</details>

### Added
- **Agent implementation guides:** `docs/agents/guides/` (mod architecture, tension/proximity, Harmony, monster overhaul); superpowers plans/specs archived under `docs/agents/archive/superpowers/` with redirect at `docs/superpowers/README.md`
- **Agent orchestration:** `docs/agents/README.md` hub, `docs/agents/orchestration.md` workflows; cross-links across agent docs, `AGENTS.md`, `CONTEXT.md`, and `.claude/` subagent prompts
- Cloudflare Worker integration tests (Vitest) for error reporting pipeline (ERR-1)
- Manual error reporting test checklist (`docs/agents/error-reporting-test-checklist.md`)
- Live smoke test script for deployed Worker (`scripts/test-error-reporter.sh`)
- **Domain glossary:** root [`CONTEXT.md`](CONTEXT.md) (DOCS-1, #174): behavior-first vocabulary (`run` vs `match`, tension sub-features, psychotic break audio names, compat terms) and agent file map
- **Verify automation:** `scripts/verify-dread.ps1` (Tier 0 static, optional Tier 1 TCP, Tier 2 log patterns), `docs/agents/verify-dread.md` runbook, `docs/agents/verify-dread-checklist.json`
- **Debug APIs:** `TestCrashSystem.TriggerForDebug()`, `PsychoticBreakSystem.ForceEpisodeForDebug()` for debug server / MCP
- **Debug overlay:** `DebugOverlaySystem` IMGUI HUD (section 11. Debug Overlay, `DebugOverlayEnabled` default off). Shows nearest enemy distance, tension/adrenaline/panic sprint, psychotic break readiness with block reasons, audio clip count and next play ETA, config flags, and Dread Harmony patch count. F10 toggles visibility at runtime when enabled. Hidden on menu levels via `SemiFunc.MenuLevel()`.
- **Runtime state:** `DreadRuntimeState` snapshot updated by tension, psychotic break, and audio systems for overlay and tooling.
- **Compatibility:** `docs/mod-compatibility.md` with known-mod table, isolation test, Proton/DLL notes, DebugConsoleUI guidance, and manual test matrix
- **Config:** `CompatibilityMode` (ambient audio only), `CompatibilitySkipConflictingPatches`, `DebugConsoleGuardEnabled` (section 10. Compatibility)
- **Harmony:** `HarmonyPatchCompat` for `IsMasterClient()` gates and optional skip when another mod already patched the target method
- **Host-only:** `EnemyNavMeshAgent` and `EnemyDirector` patches no-op on non-host clients (ADR-0004)
- Debug server: `DebugServerSystem` -- TCP server on `127.0.0.1` for AI-assisted debugging. Supports 11 commands (`ping`, `get_state`, `get_config`, `set_config`, `get_patches`, `get_logs`, `shutdown`, `verify`, `trigger_test_crash`, `force_psychotic_break`, `get_runtime_state`) via newline-delimited JSON. Config entries under "8. Debug Server" (`DebugServerEnabled`, `DebugServerPort`). Default disabled. (ADR-0013)
- Configurable logging: `LoggingService` static wrapper with `LogLevel` enum (None/Error/Debug/Verbose), level-gated log methods, Verbose prefixing, and ASCII art on mod injection. Config entry under "9. Logging" (`LogLevel`, default Debug). All ~110 existing `Plugin.Logger.Log*` calls migrated to `LoggingService.Log*`. (ADR-0014)
- MCP server: `dread-mcp-server` TypeScript MCP server using `@modelcontextprotocol/sdk` wrapping the debug TCP protocol as 11 MCP tools (`dread_ping`, `dread_get_state`, `dread_get_config`, `dread_set_config`, `dread_get_patches`, `dread_get_logs`, `dread_shutdown`, `dread_verify`, `dread_trigger_test_crash`, `dread_force_psychotic_break`, `dread_get_runtime_state`) via stdio transport. Config via env vars (`DREAD_HOST`, `DREAD_PORT`, `DREAD_TIMEOUT`). Supports `json` and `text` response formats. (ADR-0013)
- `FlashlightStateTracker.cs`: standalone MonoBehaviour extracted from nested class in PsychoticBreakSystem to fix Unity type registration failure preventing AddComponent. (PR #158)
- `breath2.ogg`, `breath3.ogg`: audio files for TensionSystem breath variant loading (referenced since v1.4.0 but missing)

### Changed
- **Debug overlay (redesign):** more concise grouped layout (one line per system) on a semi-transparent panel with an accent header and separators. Adds a performance section: smoothed **FPS** (color-coded green/amber/red), rolling **min FPS**, frame time in ms, and managed **memory** (MB) via `GC.GetTotalMemory`. Mod state condensed to Enemy / Tension / Break / Audio / Config / Patches rows
- **Debug overlay (PERF-2):** `DebugOverlaySystem` is now fully dormant when `DebugOverlayEnabled` is off (component disabled, so Unity invokes neither `Update` nor `OnGUI`); `enabled` tracks the config flag live. Adds a one-shot logged guard if the disabled-state invariant is ever violated. (#170)
- **Error reporting:** re-queue reports when the Worker returns per-report GitHub failures; ignore TestCrash log spam in the async pipeline (sync POST still sends)
- **CI:** run `workers/error-reporter` Vitest in CI and before Worker deploy
- **Debug server / MCP:** `get_config` returns flat keys plus grouped `sections` with `debugKey`; `set_config` supports all DreadConfig entries including `debugServer.*`, `overlay.enabled`, `compatibility.*`, `testing.crash`, `logging.level`
- **Docs:** README psychotic break audio table uses shipped clip names (`scream_peak`, `scream_distant`, `scream_threat`); tension feature titled **Low stamina sound** (canonical term in `CONTEXT.md`)
- **Docs:** README and THUNDERSTORE compatibility sections no longer claim conflict-free operation; `mod-profile-conflicts.md` points to `mod-compatibility.md`
- **Error reporting:** default `ErrorReportingEnabled` to **false** (opt-in); hooks `Application.logMessageReceived` instead of Harmony on `Debug.LogError` / `Debug.LogException` (ADR-0010)
- **Debug console guard:** config-toggle `DebugConsoleGuardEnabled` (default on), wired in `Plugin.cs`
- **Compatibility mode:** disables monster Harmony patches, adrenaline/panic sprint mutation, and psychotic break; keeps ambient audio
- **Harmony priority:** `Priority.Last` on enemy speed postfix, `Priority.First` on investigate prefix
- CD pipeline: fixed Thunderstore publish (`tcli` 0.2.2 `--file` / `--package-version` conflict)
- THUNDERSTORE_README.md: Psychotic Break, error telemetry, CD pipeline docs
- Logging: hot-path guards, level demotions, prefix consistency across systems and `dread-mcp-server`

### Fixed
- **Debug overlay (F10 crash):** overlay no longer throws `MissingMethodException: GUIContent.get_none()` or `MissingFieldException: Rect.y` when shown. IMGUI types (`GUI`, `GUIStyle`, `GUIContent`, `GUISkin`) now resolve from `UnityEngine.IMGUIModule` (where the game actually defines them) instead of being lumped into the `UnityEngine` stub assembly; the box draw uses a cached empty `GUIContent` rather than the stub-only `GUIContent.none` property; and the stub `Rect` now models `x`/`y`/`width`/`height` as properties (matching real Unity) so stub-built IL does not emit field access that fails at runtime
- **Debug overlay (PERF-2):** Harmony patch-count reflection no longer runs while the HUD is toggled off with F10; it runs only when the overlay is actually visible. (#170)
- **REPOConfig sliders (temporary):** when REPOConfig + MenuLib are present, `RepoConfigSliderLabelCompat` restores slider setting names for empty descriptions (label at x=100, compact row); upstream REPOConfig/MenuLib fix preferred; skipped when REPOConfig is absent. See `docs/repo-config-slider-labels-investigation.md`
- **TestCrash:** defer from `SettingChanged`, synchronous HTTP POST via `ReportTestCrashAndWait` (completes before `Process.Kill()`)
- **Error reporting:** top-level DTOs in `ErrorReportTypes.cs` plus `ErrorReportJson` manual serializer (runtime: JsonUtility emitted only Mod/Game/Unity fields, omitted `Reports[]`); safe `CaptureSystemInfoSafe` / `CaptureDisplayInfoSafe` for prod path; re-queue batch on failed send; xUnit golden tests in `tests/Dread.ErrorReportJson.Tests`
- **Docs:** ADR-0015 (error report JSON); ADR-0010/0012 updated; ERR-1 checklist dedupe vs TestCrash clarified; ROADMAP ERR-1 marked done
- **PsychoticBreak / AudioDread:** seed `_sceneLoaded` from active scene on `Start` so audio loads without waiting for a second scene load
- **CI stubs:** `UnityEngine.UI.dll` stub for `RawImage` / `RectTransform`; `JsonUtility.FromJson`; cross-platform `build.ps1` stub detection (PR #146, #161)
- **Init (Proton):** `DreadSystemInitializer` defers until `UnityEngine.UI` loads; PsychoticBreak uses runtime `RawImage` and layer mask `-1` instead of stub-only `Physics.DefaultRaycastLayers`
- **Audio (Proton):** NVorbis disk load with dependency DLLs and `PluginDependencyResolver`; Wine `file://` path mapping; PCM via `AudioClip.Create` (no `SetData`); correct PCM read position (no stutter); menu/startup guards for ambient and fake footsteps
- **Harmony:** runtime `AccessTools.TypeByName` + `object` patch args (no `BadImageFormatException` on stub-built DLLs); monster pitch tweaks skip playing sources
- **Debug / MCP:** player HP/stamina via Traverse; nested `logs` / `patches` JSON parsing
- **ErrorReporter:** ignore `DebugConsoleUI` / `DebugTester` spam, dedupe reports, cap processing per frame, capture game state once per batch (fixes lag when other mods flood `Debug.LogException`)
- **Debug console:** Harmony finalizer on `DebugConsoleUI.Update` suppresses broken `SemiFunc.DebugTester` `NullReferenceException` spam (stops console flood at source)

## [1.5.1] - 2026-05-21

![Status](https://img.shields.io/badge/status-stable-brightgreen?style=flat-square)
![Type](https://img.shields.io/badge/type-feature-green?style=flat-square)

> **Highlight:** Psychotic Break episode system and automated error reporting. When you are solo, scared, and hiding, a terrifying 20-second episode can trigger complete with darkness, flickering shadows, circling footsteps, and a shadow scream.

### Added

- Test crash button: `TestCrashSystem` with a clickable "Crash Game" config entry under "7. Testing" that deliberately throws `InvalidOperationException` to verify error telemetry end-to-end (ADR-0012)
- Error telemetry: `ErrorReporterSystem` MonoBehaviour hooks Unity's `logMessageReceivedThreaded`, buffers exceptions and errors, and sends batched reports to a Cloudflare Worker which creates GitHub Issues with `auto-reported` and `bug` labels
- Config toggle: `ErrorReportingEnabled` in section "5. Error Reporting" (default: on). Disable to opt out of telemetry
- Cloudflare Worker (`workers/error-reporter/`): processes error reports via API, deduplicates by error hash, rate-limits per IP (5/hr), auto-reopens closed duplicate issues, and creates formatted issues with system info, game state, config table, and collapsible raw JSON
- GitHub Actions workflow: auto-deploys Worker on push to `workers/error-reporter/**` or manual trigger
- Error payload includes: exception type and stack trace, scene name, enemy count and proximity, player HP/stamina/position, system specs (OS, CPU, GPU, RAM, VRAM, display), and all DreadConfig values
- `ErrorReporterSystem` follows thread-safe design: raw log entry queue on background thread, Unity API calls on main thread via `Update()`, locked buffer for batched transmission
- Psychotic Break: `PsychoticBreakSystem` MonoBehaviour triggers when solo + recent threat memory (15m/30s) + line of sight lost + crouching. 1% chance per 2s check, once per match (configurable)
- Psychotic Break episode: 20-second state machine with 4 phases (buildup, crescendo, peak, climax). Screen darkens, edge shadows pulse with flicker frequency crescendo, footsteps circle with dynamic stereo panning (subtle to frantic to close + cut)
- Psychotic Break audio: `scream_peak.ogg` (9s intense) played as dedicated burst at peak entry. `scream_distant.ogg` (30s escalating ambience) played during buildup/crescendo. `scream_threat.ogg` (30s fade-in) used by phantom sound system with randomized pitch and 3D spatialization. `phantom_footsteps.ogg` circles with stereo panning
- Psychotic Break mechanics: flashlight disabled via `FlashlightStateTracker` component, input fully locked during episode, stumble camera effect at end
- Psychotic Break config: 4 config entries under section "6. Psychotic Break" (Enabled, TriggerChance=0.01, Duration=20s, OncePerMatch=true)

### Fixed

- Harmony patches: `EnemyNavMeshAgentAwakePatch` and `PlayerControllerAwakePatch` no longer corrupt cached default fields (`DefaultSpeed`, `DefaultAcceleration`, `playerOriginalCrouchSpeed`). Only live values are modified, preventing permanent state corruption on config toggle (Issue: #91)
- Panic sprint: consolidated `_panicActive` and `_originalSprintMultiplier` into a single source of truth. Removed `_panicActive` boolean; state is now implied by `_originalSprintMultiplier >= 0`, eliminating desync across scene transitions, null player, and config toggles (Issue: #99)
- System lifecycle: each MonoBehaviour system now has its own `DontDestroyOnLoad` GameObject instead of sharing a single `DreadHost`. Eliminates the single-point-of-failure across AudioDreadSystem, MonsterOverhaulSystem, and TensionSystem (Issue: #93)
- Smoke test: removed `-nographics` flag from game launch. `Camera.main` returns null in headless mode, causing AudioDreadSystem and TensionSystem null-guards to skip all coroutine work. Tests now exercise the full initialization path (Issue: #74)
- CD pipeline: `Dread.dll` now compiled with the correct bumped version instead of the stale pre-bump version. Build job downloads and applies the modified `Plugin.cs` from the version job artifact before compiling (Issue: #70)
- FakeFootstepLoop: restructured with early-exit guards before wait, 60-90s post-cycle cooldown, 35% trigger chance. Effective interval reduced from ~22.5 min to ~3.6 min (Issue: #57)
- TensionSystem proximity features (adrenaline, panic sprint, low stamina breath) silently failed when MonsterAudio was disabled due to cross-system cache coupling. Each system now scans independently (Issue: #103)
- AudioDreadSystem: null-guard `Camera.main` and `Destroy()` clip calls, guard empty `_clips` list in `PickWeightedClip()`, stop coroutines in `OnDestroy`, remove duplicate `footsteps.ogg` load, refresh `_mainCam` on scene transition
- TensionSystem: use cached `_mainCam` instead of `Camera.main` in `FakeFootstepLoop`, percentage-based low stamina threshold, re-save `SprintSpeedMultiplier` before panic activation, framerate-independent adrenaline lerp, guard `_breathCooldown` decrement when disabled, stop coroutines in `OnDestroy`
- MonsterOverhaulSystem: field-access try/catch guards in Harmony patches, initialize `_inLevel` from current scene in `Start()`, save boosted crouch speed when `playerOriginalCrouchSpeed` is 0, stop coroutines in `OnDestroy`
- DreadConfig: added `_initialized` flag and `EnsureInitialized()` guard to prevent null ConfigEntry access before initialization
- Plugin: component initialization validation on `Start()` with failure count logging
- EnemyDirectorSetInvestigatePatch: respect `MonsterAggressionEnabled` config toggle
- CI: tightened null-forgiving grep pattern from `\w+!` to `[\w)\]]!\.`
- CI stubs: added missing `StopAllCoroutines()`, `GetActiveScene()`, `MoveTowards()` methods
- CI cache: key includes stubs source hash to invalidate on stub changes
- CI: removed invalid `metadata: read` permission from workflow (PR #143). `metadata` is not a valid GITHUB_TOKEN scope — setting it caused GitHub Actions to reject the workflow at YAML parse time, producing 0s failures on every branch
- CD: `gh release create` now uses version-specific tags (e.g. `v1.5.2`) instead of the trigger tag literal (`vpatch`/`vminor`/`vmajor`), preventing tag collisions on subsequent releases (PR #135)
- CD: removed incompatible `--package-version` flag from `tcli publish` command. In tcli 0.2.2, `--file` and `--package-version` are mutually exclusive CommandLine sets — the version is now read from the zip's internal `manifest.json` (`eb350e4`)

### Changed
- MonsterOverhaulSystem: shared static `CachedEnemies` list avoids per-frame `FindObjectsOfType` allocations
- TensionSystem proximity scan: interval reduced from 0.5s to 2.0s (reduces GC pressure)
- Harmony patches: split from `PatchAll()` into explicit per-patch lifecycle. Patches are now conditionally applied at startup based on config and can be toggled at runtime via BepInEx config UI. (Issue #107, ADR-0009)

### CI Pipeline Optimizations
![Type](https://img.shields.io/badge/type-ci/cd-yellow?style=flat-square)
- Collapsed 4 jobs into 1 verify job on `ubuntu-latest`, saving ~30s of job overhead
- Runner: `windows-latest` to `ubuntu-latest` (faster startup: ~5s vs ~15s)
- Stub generation: optimized `gen-stubs.ps1` for speed; cached via `actions/cache`
- .NET setup: removed `actions/setup-dotnet` (SDK 10+ pre-installed on runner)
- MAUI workload: removed (uses reference assemblies NuGet package)
- Format check: `dotnet-format` replaced with instant grep-based checks
- NuGet restore: cached with restore-key fallback
- gen-stubs.ps1: fixed path separator for Linux compatibility
- Committed stub DLLs removed from repo (now gitignored, auto-generated)

## [1.5.1] - 2026-05-21

![Status](https://img.shields.io/badge/status-unstable-yellow?style=flat-square)
![Type](https://img.shields.io/badge/type-cd-pipeline-blue?style=flat-square)

> **Highlight:** CD pipeline release automation added. Version bumps via `vmajor`/`vminor`/`vpatch` tags, auto-generated GitHub Releases, and changelog management.

### Added
- CD pipeline: `.github/workflows/cd.yml` triggered on `vmajor`, `vminor`, or `vpatch` tag push
- Version bump: read current from `manifest.json`, increment matching segment, write to all files
- Changelog rename: `[Unreleased]` section renamed to new version on release, fresh `[Unreleased]` recreated above
- GitHub Release: auto-created with actual version title (e.g., `v1.6.0`) and changelog body
- Safety guard: pipeline hard-fails if `[Unreleased]` section is missing when a tag is pushed
- Failure isolation: if build fails, nothing is pushed to remote (no stale commits, no moved tags)

### Edge Cases
- `VPATCH` (uppercase) is rejected; tag names are case-sensitive
- Missing `[Unreleased]` section: workflow fails with clear error before any changes
- Tag pushed from fork: action does not trigger (GitHub restriction on fork tag events)
- Build failure after bump: local changes discarded, remote untouched, user re-pushes tag after fix
- Duplicate release tag: `gh release create` fails; user must delete existing release and re-run

## [1.5.0] - 2026-05-21

![Status](https://img.shields.io/badge/status-stable-brightgreen?style=flat-square)
![Type](https://img.shields.io/badge/type-minor-yellow?style=flat-square)

> **Highlight:** Pitch randomization across all three audio systems, significantly rarer ambient sounds, and a full sweep of state-leak and config-bounds fixes.

### Added
- Pitch randomization (`0.5x`-`1.5x`) on every ambient sound spawn in `AudioDreadSystem`
- Pitch randomization (`0.5x`-`1.5x`) on every fake footstep spawn in `TensionSystem`
- Monster audio pitch now randomized per-enemy at scan time: `Mathf.Clamp(pitch * Random.Range(0.5f, 1.5f), 0.3f, 1.5f)` instead of a fixed multiplier
- `RestoreSprintMultiplier()` method in `TensionSystem` for clean panic sprint cleanup on destroy and scene transition
- `AcceptableValueRange<float>(0.0f, 1.0f)` added to `AudioVolume` config entry
- `AcceptableValueRange<float>(0.1f, 10f)` added to `AudioFrequency` config entry

### Changed
- Ambient audio interval: `30-90s` → `60-180s` (sounds are rarer, each hit harder)
- Clip weights reduced: scraping `1.0` → `0.6`, footsteps `1.0` → `0.6`, breathing `0.6` → `0.3`, whisper `0.25` → `0.1`
- Fake footstep interval: `120-240s` → `180-360s`
- Fake footstep trigger chance: `35%` → `20%`
- `reverbZoneMix` in `MonsterOverhaulSystem` corrected to `1.0` (was `1.1`, outside valid Unity range of `0-1`)
- `TensionSystem.OnDestroy` now calls `RestoreDrain()` and `RestoreSprintMultiplier()` to clean up modified player state
- `TensionSystem.OnSceneLoaded` restores state before resetting sentinel values, preventing multiplier leaks across scene transitions
- `UpdateAdrenaline` and `UpdatePanicSprint` both restore state when their respective config toggles are disabled mid-session
- `_mainCam` cached as a field in `TensionSystem` to avoid `Camera.main` per-frame overhead

---

## [1.4.1] - 2026-05-21

![Status](https://img.shields.io/badge/status-stable-brightgreen?style=flat-square)
![Type](https://img.shields.io/badge/type-patch-blue?style=flat-square)

> **Highlight:** Version bump for Thunderstore reupload. No functional changes from 1.4.0.

### Changed
- Incremented `version_number` in `manifest.json` to satisfy Thunderstore's duplicate-version rejection

---

## [1.4.0] - 2026-05-21

![Status](https://img.shields.io/badge/status-stable-brightgreen?style=flat-square)
![Type](https://img.shields.io/badge/type-minor-yellow?style=flat-square)

> **Highlight:** Major system refactor. Removed three broken systems, hardened the remaining ones, and shipped the first real audio and icon assets.

### Added
- `audio/breathing.ogg` -- ambient breathing sound used by both AudioDreadSystem and TensionSystem
- `audio/footsteps.ogg` -- used for ambient horror loop and fake footstep spawns
- `audio/door_creak.ogg` -- ambient horror loop variant
- `audio/scraping.ogg` -- ambient horror loop, common rarity
- `audio/whisper.ogg` -- ambient horror loop, rare (0.25 weight)
- `icon.png` -- 256x256 Thunderstore icon (required for upload)
- Weighted clip selection in `AudioDreadSystem` -- whisper is 4x rarer than scraping/footsteps
- `DreadAudioTweaked` marker component prevents double-patching enemies in `MonsterOverhaulSystem`
- `FakeFootstepLoop` coroutine in `TensionSystem` -- spawns positional footstep sounds behind the player every 2 to 4 minutes (35% chance)
- Multi-variant breath clip loading (`breathing.ogg`, `breath2.ogg`, `breath3.ogg`) for future audio expansion

### Changed
- `AudioDreadSystem` now uses weighted random selection instead of uniform random
- `MonsterOverhaulSystem` scans for new enemies every 4 seconds instead of relying on patch hooks
- `TensionSystem` proximity scan runs on a 0.5s interval shared across all three tension features (adrenaline, panic sprint, low stamina) instead of three separate scans
- `EnemyDirector.SetInvestigate` radius multiplier capped at 1.5x (previously untested at higher values that caused Photon sync issues)
- `EnemyNavMeshAgentAwakePatch` now also patches `DefaultAcceleration` via Traverse so speed resets stay at the boosted value
- `PlayerControllerAwakePatch` now patches `playerOriginalCrouchSpeed` so crouch speed stays boosted after tumble resets

### Removed
- `HostOptionsSystem` -- removed; Host Options networking required REPOLib dependency that was causing GUID resolution failures
- `EnvironmentalSystem` -- removed; visual corruption effects (chromatic aberration, vignette) were incompatible with current PostProcessing setup
- `VisualCorruptionSystem` -- removed along with EnvironmentalSystem; PostProcessing v2 volume patching unreliable in current build

<details>
<summary>Technical notes on removed systems</summary>

`HostOptionsSystem` relied on `REPOLib` for networked config sync. The BepInDependency GUID went through three incorrect values across commits (`com.github.zehs.repolib`, `com.zehs.repolib`, `REPOLib`) before being confirmed correct, but the networking layer itself was causing load failures on clients without REPOLib. Removed to make Dread dependency-free.

`EnvironmentalSystem` and `VisualCorruptionSystem` both attempted to patch Unity PostProcessing v2 volumes at runtime. R.E.P.O. uses a custom PostProcessing setup that does not expose standard `VolumeProfile` access patterns. These systems never functioned correctly.

</details>

---

## [1.3.x] - Prior Commits

![Status](https://img.shields.io/badge/status-deprecated-red?style=flat-square)

<details>
<summary>View 1.3.x fix history</summary>

These were rapid hotfix commits resolving reference and dependency issues during initial development. No public release was made at these versions.

### [1.3.4] - Fix REPOLib BepInDependency GUID
- Corrected GUID to `REPOLib` (confirmed via assembly inspection)

### [1.3.3] - Fix REPOLib BepInDependency GUID (second attempt)
- Tried `com.zehs.repolib` -- incorrect

### [1.3.2] - Fix HarmonyLib using directive
- Added missing `using HarmonyLib;` to `TensionSystem`

### [1.3.1] - Fix PhotonRealtime reference
- Added `PhotonRealtime` assembly reference to `.csproj`
- Used `Traverse` for `SprintSpeedMultiplier` access instead of direct field access

### [1.3.0] - Fix all reference paths in csproj
- Corrected all assembly reference paths to match local R.E.P.O. installation layout

</details>

---

## [1.0.0] - Initial Release

![Status](https://img.shields.io/badge/status-archived-lightgrey?style=flat-square)

> **Highlight:** First commit. Established project structure, all core systems stubbed.

### Added
- BepInEx plugin scaffold (`Plugin.cs`, `DreadConfig.cs`)
- `AudioDreadSystem` -- ambient positional horror sounds
- `MonsterOverhaulSystem` -- enemy speed, audio, and detection patches
- `TensionSystem` -- adrenaline, panic sprint, low stamina, fake footsteps
- `HostOptionsSystem` -- networked host config sync via REPOLib (later removed)
- `EnvironmentalSystem` -- PostProcessing visual effects (later removed)
- `VisualCorruptionSystem` -- chromatic aberration and vignette (later removed)
- `build.ps1` -- automated Thunderstore package builder

---

*Maintained by [elytraking](https://github.com/grompen91-droid)*

