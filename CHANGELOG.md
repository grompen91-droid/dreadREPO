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

### Changed
- **Psychotic Break:** Single `PsychoticBreakChancePercent` slider derives check interval, LoS-lost delay, and per-roll chance; true LoS-lost gate (saw enemy, then hidden); silent tumble hold (no repeated fall SFX)
- **Psychotic Break:** Staggered walk/run footsteps (`footsteps_run.ogg`), scheduled distant/peak screams, client-local hallucination mob attack with damage block, horror-only side accent colors on blackout overlay

- **Core:** `ProximityScan` in `Systems/Core/` replaces `EnemyScanCache`; tension, monster audio, debug server, psychotic break, and error reporting share one scan seam (ADR-0008 proximity pattern consolidated)
- **Core:** `HarmonyPatchRegistry` + `PatchLifecycle` centralize patch apply/remove; `Plugin.cs` delegates patch wiring (ADR-0009 preserved)
- **Core:** `PlayerInputLockCompat` shared by psychotic break and error-reporting prompt
- **Core:** `SpatialAudio3D` helper for weighted ambient, fake footsteps, and phantom break audio
- **Core:** `GameplayContext` unifies menu/in-level/run gates; `DreadFeaturePolicy` centralizes Compatibility mode (low stamina + fake footsteps now disabled in compat)
- **Core:** `PlayerControllerCompat` stamina/sprint multiplier seams for tension and debug server reads
- **Verify:** Tier 0 registry manifest includes `ErrorReportingPromptSystem`

### Fixed
- **Error reporting:** Game-state capture for crash reports no longer calls compile-time `EnemyHealth.CurrentHealth` (fixes `get_CurrentHealth` MissingMethodException when third-party mods log errors, e.g. DeathMinimap after death); uses `Systems/Core/EnemyHealthCompat` and `EnemyScanCache`

## [1.6.1] - 2026-05-30

### Fixed
- **CD / Thunderstore package:** ship `NVorbis.dll` and `System.*` plugin dependencies beside `Dread.dll` (same as `build.ps1`). v1.6.0 packages were missing them, causing `FileNotFoundException` for NVorbis at runtime.

## [1.6.0] - 2026-05-30

![Status](https://img.shields.io/badge/status-development-yellow?style=flat-square)

### Added
- **ERR-2 (#172):** Error reporting default-on for new cfg; one-time IMGUI first-run prompt on first non-menu gameplay scene; `ErrorReportingPromptShown` cfg key; `ErrorReportingConsent` blocks enqueue/send until prompt acknowledged; `ErrorReportingPromptSystem` registered in `DreadSystemRegistry`
- **ERR-3 (#173):** Canonical error reporting privacy disclosure (`ErrorReportingPrivacyCopy`) wired to `ErrorReportingEnabled` config description; optional one-line log when reporting is enabled at runtime
- **REPOConfig compat:** `RepoConfigCompat` entry point; `HideFromREPOConfig` only on the test crash button (debug server, overlay, logging, and compatibility toggles stay in REPOConfig)

### Changed
- **Docs:** `docs/ROADMAP.md` merge log for 2026-05-29 PRs (#194-#208); phases 2-3 marked finished; ERR-2/AUDIO-1 status; README project tree + registry init + tech stack (Vitest 4, Zod 4, NVorbis, GPL-3.0)
- **ERR-2 (#172):** First-run error reporting prompt shows clear Dread mod branding, fixes bullet text clipping via measured row heights, unlocks the mouse cursor, blocks gameplay input while open, and uses dark-panel styling aligned with the Dread debug overlay
- **Roadmap:** **UI-1** unified in-game UI kit (modular component for modals, overlay, and future panels; see `docs/ROADMAP.md`)
- **ERR-2 (#172):** `ErrorReportingPrivacyCopy` and README/THUNDERSTORE/mod-compat/ADR-0010 updated for default-on + first-run prompt; existing saved `ErrorReportingEnabled = false` retained on upgrade
- **Config section order:** Gameplay first (audio through compatibility), then error reporting (7), then debug overlay/server/logging (8-10); test crash remains section 11. Renumbered cfg section headers; existing installs may keep values under old section names until reset or manual merge.
- **REPOConfig error reporting:** Removed disclosure label compat (REPOConfig has no bool description API). Full privacy text remains in `elytraking.dread.cfg` and Configuration Manager only.
- **REPOConfig testing:** `Crash Game` (section 11) visible in REPOConfig as a toggle (turn on to test crash pipeline).
- **ARCH-3 (#175):** `DreadSystemRegistry` + fail-safe init loop in `DreadSystemInitializer`; Tier 0 `arch3_try_add_system` guard in `verify-dread.ps1`; ADR-0016 and agent compat/registry docs; `Plugin.cs` remains patch-focused (no system spawns)
- **ARCH-2 (#168):** Reflection inventory in `docs/agents/guides/reflection-inventory.md`; stub vs full build docs in `mod-architecture.md`; Harmony patches use `typeof` for stub-covered game types (`EnemyNavMeshAgent`, `PlayerController`, `EnemyDirector`); cached compat reflection for player crouch/tumble checks; `SemiFunc.IsMasterClient` resolved at type load (no behavior change)
- **ARCH-1 (#167):** Internal file layout only: Harmony patches under `Systems/Patches/`; psychotic break, error reporting, and debug overlay split into focused files under `Systems/PsychoticBreak/`, `Systems/ErrorReporting/`, and `Systems/DebugOverlay/` (no player-facing behavior change)
- **Worker tests:** Vitest 4.1 + `@cloudflare/vitest-pool-workers` 0.16 with `cloudflareTest()` config (Cloudflare docs); worker package is ESM
- **MCP server:** Zod 4 `z.strictObject()` for tool input schemas (replaces deprecated `.strict()`)

### Fixed
- **ERR-2 (#172):** Resetting `ErrorReportingPromptShown` to false in cfg or REPOConfig shows the first-run prompt again on scene load or when the setting changes, without restarting the game
- **ERR-3 disclosure accuracy:** Privacy copy now matches `ErrorReportPayloadCapture` (eleven config fields including audio frequency/volume; system info may include VRAM and GPU driver); `FullDescription` composed from `DataBullets`; REPOConfig/Configuration Manager called out in disable steps
- **Audio:** ambient/psychotic clips no longer cut off early when pitch is below 1.0 (`AudioPlayUtil` destroy lifetime); NVorbis reads full OGG streams
- **Stub/local builds:** error reporter uses `HttpWebRequest` instead of `UnityWebRequest` (fixes `BadImageFormatException: Method has zero rva` log spam); audio loader skips UWR fallback when UWR is unusable; psychotic break no longer `Destroy`s shared cached `AudioClip`s
- **REPOConfig slider labels:** reverted experimental `MidlineLeft`/pivot tweak that overlapped slider text; compat back to `Left` alignment + x=100 column only (see `docs/repo-config-slider-labels-investigation.md`)
- GitHub boilerplate: GPL-3.0 `LICENSE`, Dread-tailored Dependabot (mod, MCP, worker, CI), `SECURITY.md`, CodeQL with stub build and path filters

> **Highlight:** ARCH refactor + system registry, error reporting default-on with first-run privacy prompt, audio/stub hardening, GPL-3.0 and modern CI/deps (Vitest 4, Zod 4).

<details>
<summary>Planned (not in this release)</summary>

Tracked in [docs/ROADMAP.md](docs/ROADMAP.md):

- **UI-1:** unified in-game UI kit (modals, overlay, future panels)
- Debug overlay: draggable panel (DBG-1), fonts (DBG-3), extensible panel API (DBG-5)
- Performance pass (PERF-1)
- Error reporting: non-blocking batch flush (ERR-4)
- Audio: `AudioPlayUtil` unit tests (AUDIO-2), NVorbis perf (AUDIO-3)

**Shipped on master 2026-05-29 (see roadmap merge log):** ARCH-1/2/3 (#201-#204), ERR-2/3 (#207-#208), audio hardening (#203), repo hygiene (#194), toolchain (#195-#198, #200).

GitHub backlog: issues #163-#166, #169, and new rows in [docs/ROADMAP.md](docs/ROADMAP.md).

</details>

---

## [1.5.3] - 2026-05-30

![Status](https://img.shields.io/badge/status-development-yellow?style=flat-square)

> **Note:** Accidental patch publish on Thunderstore during a mis-tagged release. Use **v1.6.0** (minor) for the canonical package and release notes.

---

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




