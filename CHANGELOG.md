# Changelog

All notable changes to **Dread** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

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
