# Dread

> **Atmospheric horror overhaul for R.E.P.O.**  
> Seven runtime systems that layer ambient dread, scarier monsters, a tension system that reads your proximity to danger in real time, a psychotic break episode when you are alone and scared, and automatic error reporting.

![Version](https://img.shields.io/badge/version-1.6.1-crimson?style=flat-square)
![Status](https://img.shields.io/badge/status-release-brightgreen?style=flat-square)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.21-blueviolet?style=flat-square)
![Game](https://img.shields.io/badge/game-R.E.P.O.-orange?style=flat-square)
![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)
![Thunderstore](https://img.shields.io/badge/thunderstore-elytraking/Dread-243e58?style=flat-square)

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
- [Features](#features)
- [Configuration](#configuration)
- [Netcode Model](#netcode-model)
- [Mod Compatibility](#mod-compatibility)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Version History](#version-history)
- [Getting Started](#getting-started)
- [Building from Source](#building-from-source)
- [License](#license)

---

## Overview

Dread is a BepInEx plugin that transforms R.E.P.O. into a genuinely unsettling experience at the IL level. It uses **Harmony 2 runtime patching** to intercept enemy spawn, movement, and detection methods, while seven independent MonoBehaviour systems run on persistent game objects that survive scene transitions.

Every feature is independently toggleable via `BepInEx/config/elytraking.dread.cfg`. Players without Dread can join modded lobbies: monster changes are host-authoritative, while audio and tension effects are client-local.

---

## How It Works

```
Plugin.Awake()
  +-- LoggingService.Initialize()      # level-gated logging
  +-- DreadConfig.Initialize()          # 9 config sections
  +-- Harmony patch application         # conditional patches
  |
Plugin.Start() / deferred retry
  +-- DreadSystemInitializer.TryInitialize()
       +-- DreadSystemRegistry (ordered registrations, config gates)
            +-- AudioDreadSystem              # coroutine: weighted ambient sounds
            +-- MonsterOverhaulSystem         # scan loop + Harmony patches (Systems/Patches/)
            +-- TensionSystem                 # 0.5s proximity scan drives 4 features
            +-- PsychoticBreakSystem          # Systems/PsychoticBreak/* episode state machine
            +-- ErrorReporterSystem           # Systems/ErrorReporting/* telemetry + consent
            +-- ErrorReportingPromptSystem    # first-run disclosure (ERR-2)
            +-- TestCrashSystem               # config button to trigger intentional crash
            +-- DebugOverlaySystem            # F10 HUD (Systems/DebugOverlay/)
            +-- DebugServerSystem             # TCP debug server for AI agents (default off)
```

Runtime systems are registered in `DreadSystemRegistry` and spawned with per-system fail-safe isolation (ARCH-3). **Audio** downloads version-pinned OGG files from the matching [GitHub Release](https://github.com/grompen91-droid/dreadREPO/releases) on first run (into `audio-cache/v{version}/`), then decodes via **NVorbis** (`AudioAssetSystem`, `AudioClipLoader`). The Thunderstore package is DLL-only; features start progressively as each clip arrives. A failure in one system does not prevent others from starting.

---

## Features

### Ambient Audio

A coroutine loop places rare, positional sounds in the world during runs. Every 60 to 180 seconds (scaled by config multiplier), a sound is selected using weighted random distribution and spawned at a random 3D position 5 to 15 meters from the player's camera. Pitch randomized per-play from 0.5x to 1.5x.

| Sound | Weight | Rarity | Description |
|-------|--------|--------|-------------|
| `scraping.ogg` | 0.6 | Common | Something dragging across the floor |
| `footsteps.ogg` | 0.6 | Common | Steps from a direction nobody is in |
| `breathing.ogg` | 0.3 | Uncommon | Close, slow breathing nearby |
| `whisper.ogg` | 0.1 | Rare | A voice at the edge of hearing |
| `scream_peak.ogg` | -- | Psychotic Break | Peak phase intense scream |
| `scream_distant.ogg` | -- | Psychotic Break | Buildup/crescendo distant ambience |
| `scream_threat.ogg` | -- | Psychotic Break | Peak-window phantom threat (3D) |
| `footsteps.ogg` | -- | Psychotic Break | Circling footsteps during episode (also used for ambient/fake footsteps) |

- Fully spatialized (`spatialBlend = 1.0`), linear rolloff, falloff from 1m to 25m
- Each AudioSource self-destructs after `clip.length + 0.5s`
- Pitch randomized `0.5x`-`1.5x` on every spawn for variety
- Automatically disabled on menu screens via `SemiFunc.MenuLevel()`

---

### Monster Overhaul

Three Harmony patches and a dynamic audio scan run independently, applied at different lifecycle points:

#### EnemyNavMeshAgent.Awake (Postfix)
- **Effect:** Multiplies `agent.speed` and `agent.acceleration` by **1.2x** at spawn
- **Why Postfix:** Runs after the game's own Awake, so enemy defaults are already set
- **Persistence:** Also patches cached `DefaultSpeed` and `DefaultAcceleration` via `Traverse`, keeping the boost through speed resets
- **Scope:** Catches all enemies including modded ones (Mimic, WesleysEnemies, etc.)

#### EnemyDirector.SetInvestigate (Prefix)
- **Effect:** Multiplies investigate radius by **1.5x**
- **Cap:** Intentionally limited: higher values desync enemy positions over Photon PUN on remote clients

#### Audio Overhaul (Scan loop, 4s interval)
- Randomizes enemy `AudioSource.pitch` to **0.5x-1.5x** of base (clamped 0.3-1.5), applied once per enemy via marker
- Sets `reverbZoneMix = 1.0` for spatial presence
- Forces `spatialBlend = 1.0` on all child audio sources
- A marker component (`DreadAudioTweaked`) prevents double-patching
- Works retroactively on any newly-spawned or modded enemy

---

### Tension System

A single `Update()` loop scans for the nearest `EnemyHealth` every **0.5 seconds** and drives four features based on proximity:

#### Adrenaline
- Sprint energy drain reduced by up to **70%** when enemy within 15m
- Scales linearly: closer = less drain
- Smooth lerp back to normal on threat clearance
- Full stamina restoration on scene transition

#### Panic Sprint
- Sprint start within **15m** of an enemy triggers **1.25x** speed burst for 2 seconds
- 20-second cooldown
- Uses `Traverse` to access private `SprintSpeedMultiplier` field
- Clean multiplier restoration on cooldown or scene change

#### Low stamina sound
- Plays a gasping breath sound when stamina drops below 5 after sprinting
- 60-second cooldown
- Supports multiple variants (`breathing.ogg`, `breath2.ogg`, `breath3.ogg`)

#### Fake Footsteps
- Coroutine fires every 3 to 6 minutes with **20%** chance to play
- Spawns a 3D footstep sound 2.5m to 5m behind the player, slightly off-center
- Pitch randomized `0.5x`-`1.5x` per spawn
- Low volume, short falloff (0.5m min, 8m max)

---

### Psychotic Break

A 2-second trigger check fires when you are **solo** (no other alive player within 30m), have a **recent threat** (any enemy within 15m in the last 30 seconds), have **lost line of sight** to all enemies, and are **crouching**. On success (1% base chance, configurable), a **20-second episode** plays out in phases:

| Time (approx) | Phase | Effects |
|---------------|-------|---------|
| 0s-3s | Buildup | Screen darkens, edge shadows begin flickering |
| 3s-10s | Crescendo | Vignette flicker accelerates, footsteps begin circling (stereo panning), distant scream |
| 10s-16s | Peak | Circling footsteps intensify, peak scream audio plays, random phantom threat sounds |
| 16s-20s | Climax | Footsteps close + fade, screen cuts, camera stumble (roll + dip) |

During the episode, the **flashlight is disabled** and **all player input is locked** (movement, interaction, menus). The episode is **client-local**: other players in multiplayer see the flashlight flicker but not the overlay.

See [Psychotic Break Configuration](#psychotic-break) for the toggle, trigger chance, and duration settings.

---

### Error Reporting

Anonymous telemetry via Unity `Application.logMessageReceived` (`Exception` / `Error` logs), batched and sent to a Cloudflare Worker (may create public GitHub issues). Default **on** for new cfg files. The first time you enter a gameplay level, a one-time in-game prompt shows the full disclosure (same text as cfg). Choose **Keep reporting on** or **Turn off reporting**; the prompt does not appear again unless you reset cfg. The full disclosure is also in the generated cfg description for `ErrorReportingEnabled` (section `7. Error Reporting`; REPOConfig shows the toggle only, full text in cfg or F1 Configuration Manager). Summary table:

| Data | Details |
|------|---------|
| Crash fingerprint | SHA-256 prefix hash of stack trace + message for deduplication |
| Exception info | Type, message, truncated stack trace (3000 chars) |
| Game state | Scene name, enemies alive/nearby/total, player HP/stamina, play time |
| System info | OS, CPU, GPU, RAM, VRAM, driver version, device model |
| Display info | Resolution, refresh rate, DPI, fullscreen mode |
| Config snapshot | Eleven named Dread settings (toggles plus audio frequency/volume); see cfg section 7 for full text |

- Reports buffer in-memory and flush every 5 minutes or when the buffer is full (max 50 per batch)
- No reports are sent until you acknowledge the first-run prompt (`ErrorReportingPromptShown`)
- Disabled when `ErrorReportingEnabled` is false (turn off in the prompt, cfg, or REPOConfig)
- Includes a **Test Crash** toggle (section 11; REPOConfig or Configuration Manager) to verify the pipeline end-to-end

---

### QOL

| Feature | Value | Implementation |
|---------|-------|----------------|
| Crouch Speed Boost | +30% | Harmony postfix on `PlayerController.Awake`, patches cached field via `Traverse` |

---

## Configuration

`BepInEx/config/elytraking.dread.cfg` is generated on first run. Compatible with **REPOConfig** for live editing.

**REPOConfig slider labels (temporary workaround):** REPOConfig passes an empty description to MenuLib sliders, so setting names were invisible on float/int rows. When REPOConfig and MenuLib are installed, Dread applies a **best-effort** Harmony compat (`RepoConfigSliderLabelCompat`): restores the name on the left, hides the empty description row, keeps a compact row height. This uses a fixed label X offset and may not match toggle label styling exactly. **Proper fix belongs in REPOConfig or MenuLib upstream.** Without REPOConfig, use `elytraking.dread.cfg` or BepInEx Configuration Manager. Full timeline: [docs/repo-config-slider-labels-investigation.md](docs/repo-config-slider-labels-investigation.md).

<details>
<summary>Full config reference</summary>

```
[1. Audio Dread]
Enabled = true
Frequency = 1.0       # multiplier, 2.0 = twice as often
Volume = 0.4          # 0.0 to 1.0

[2. Monster Overhaul]
AggressionEnabled = true   # HOST ONLY
AudioEnabled = true

[3. Tension]
AdrenalineEnabled = true
PanicSprintEnabled = true
LowStaminaSoundEnabled = true
FakeFootstepsEnabled = true

[4. Psychotic Break]
PsychoticBreakEnabled = true
PsychoticBreakTriggerChance = 0.01     # 1% per 2s check
PsychoticBreakDuration = 20            # episode length in seconds
PsychoticBreakOncePerMatch = true

[5. QOL]
CrouchSpeedBoost = true

[6. Compatibility]
CompatibilityMode = false

[7. Error Reporting]
ErrorReportingEnabled = true     # anonymous crash telemetry (see cfg description; first-run prompt on first level)
ErrorReportingPromptShown = false   # internal: set by first-run prompt

[8. Debug Overlay]
DebugOverlayEnabled = false

[9. Debug Server]
DebugServerEnabled = false           # TCP debug server for AI agents (default off)
DebugServerPort = 15432              # port, falls back to +1 if unavailable

[10. Logging]
LogLevel = Debug                     # None | Error | Debug | Verbose

[11. Testing]
Crash Game = false                   # turn ON in REPOConfig or cfg to test crash reporting (resets to off)
```

</details>

---

## Netcode Model

R.E.P.O. uses Photon PUN for multiplayer. Dread's netcode approach is pragmatic:

| Feature | Authority | Who needs the mod |
|---------|-----------|-------------------|
| Enemy speed, acceleration, detection | Host | Host only |
| Enemy audio overhaul | Local | Per client |
| Ambient audio | Local | Per client |
| Adrenaline, panic sprint, breath, footsteps | Local | Per client |
| Psychotic Break episodes | Local | Per client |
| Crouch speed boost | Local | Per client |

Monster changes are **host-authoritative** because the Harmony patches run on the host's game instance, which owns the Photon `EnemyNavMeshAgent` sync. Ambient audio and tension features are entirely client-local and invisible to other players.

---

## Mod Compatibility

Dread is **dependency-free** (BepInEx only) and is tested alongside common mod stacks, but **cannot promise zero conflicts** with every Thunderstore mod. Harmony load order, shared game types, and platform (Windows vs Proton) all matter.

See **[docs/mod-compatibility.md](docs/mod-compatibility.md)** for the full matrix, isolation test steps, Linux DLL notes, and manual test checklist.

**Planned work:** [docs/ROADMAP.md](docs/ROADMAP.md) (unified UI kit, debug overlay UX/fonts/drag, performance pass, non-blocking telemetry flush). ARCH-1/2/3, ERR-2/3, and audio hardening shipped 2026-05-29. Pending: error-report Core capture fix in [CHANGELOG.md](CHANGELOG.md) [Unreleased].

**Contributor glossary:** [CONTEXT.md](CONTEXT.md) (domain terms for issues, PRs, and agents).

**Quick guidance:**

- **Modded enemies** (Mimic, WesleysEnemies, etc.): supported via `EnemyHealth` scan and host-only aggression patches when you are lobby host.
- **REPOConfig / MenuLib**: usually fine; broken `DebugConsoleUI` hooks are mitigated by `DebugConsoleGuardEnabled` (default on). Slider names use a **temporary** Dread compat when REPOConfig is present (see [Configuration](#configuration)); upstream REPOConfig/MenuLib fix still desired.
- **Sprint or stamina overhauls**: if movement feels wrong, disable `AdrenalineEnabled` / `PanicSprintEnabled` or enable **Compatibility mode** (ambient audio only).
- **Broken profiles**: set `CompatibilityMode = true` or `ErrorReportingEnabled = false` without uninstalling Dread.
- **REPOLib**: not required (removed in v1.4.0).

The repo `audio/` tree includes `ambient_dread/door_creak.ogg` on the release manifest but not loaded by any system yet. It is available for future ambient variants.

**Offline / slow network:** ambient and tension features work with whatever clips are already cached; missing files stay queued until download succeeds. Pin parallel downloads in config (`1b. Audio Assets` > `MaxConcurrentDownloads`, 0 = auto).

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET Framework 4.8, `net48`) |
| Mod framework | BepInEx 5.4.2100+ (`BaseUnityPlugin`) |
| Patcher | Harmony 2 (runtime IL patching) |
| Game engine | Unity (via `Assembly-CSharp.dll`) |
| Networking | Photon PUN (`PhotonUnityNetworking`, `Photon3Unity3D`) |
| Audio | OGG Vorbis via **NVorbis** (`AudioClipLoader`); UWR fallback when usable |
| Error reporting | Cloudflare Worker (`workers/error-reporter`, Vitest 4 + `@cloudflare/vitest-pool-workers`) |
| Debug server | TCP socket (JSON-over-line protocol) |
| AI agent bridge | MCP server (TypeScript 6, Zod 4, `@modelcontextprotocol/sdk`) |
| CI / security | GitHub Actions, Dependabot, CodeQL (C# stub build), GPL-3.0 |

---

## Project Structure

```
Dread/
  Plugin.cs                          # BepInEx entry: config, Harmony, defers to DreadSystemInitializer
  Dread.csproj                       # net48, references BepInEx/Harmony/Unity/Photon/Assembly-CSharp
  Config/
    DreadConfig.cs                   # ConfigEntry bindings (gameplay, error reporting, debug, compat)
  Systems/
    DreadSystemRegistry.cs           # Ordered runtime system registrations (ARCH-3)
    DreadSystemInitializer.cs        # Fail-safe AddComponent loop
    AudioDreadSystem.cs, AudioClipLoader.cs, AudioPlayUtil.cs
    MonsterOverhaulSystem.cs, TensionSystem.cs, TestCrashSystem.cs
    Patches/                         # Harmony patch classes (enemy, player, debug console guard)
    PsychoticBreak/                  # Episode, trigger, audio, overlay, player lockdown
    ErrorReporting/                  # Reporter, uploader, consent, privacy copy, prompt UI
    DebugOverlay/                    # F10 overlay panel + styles
    DebugServerSystem.cs, LoggingService.cs, HarmonyPatchCompat.cs, ...
  audio/
    scraping.ogg                     # Common ambient sound
    footsteps.ogg                    # Common ambient + fake footsteps + psychotic break
    breathing.ogg                    # Uncommon ambient + out-of-breath sound
    breath2.ogg                      # Out-of-breath variant
    breath3.ogg                      # Out-of-breath variant
    whisper.ogg                      # Rare ambient
    door_creak.ogg                   # Ambient variant
    scream_peak.ogg                  # Psychotic Break peak scream
    scream_distant.ogg               # Psychotic Break distant scream
    scream_threat.ogg                # Psychotic Break phantom threat sound
  dread-mcp-server/                  # MCP server for AI-assisted debugging
    src/index.ts                     # Zod 4 strictObject tool schemas
    package.json                     # TypeScript 6, @modelcontextprotocol/sdk
  workers/error-reporter/            # Cloudflare Worker (Vitest 4, vitest.config.mts)
  docs/ROADMAP.md                    # Backlog + 2026-05-29 merge log
  .github/dependabot.yml             # NuGet, npm (MCP + worker), GitHub Actions
  build.ps1                          # PowerShell build + Thunderstore packaging
  manifest.json                      # Thunderstore package metadata
  LICENSE                            # GPL-3.0
  SECURITY.md                        # Vulnerability reporting policy
```

---

## Version History

| Version | Highlights |
|---------|------------|
| **v1.6.0** | ARCH-1/2/3 (split systems, registry, reflection docs), ERR-2/3 (default-on telemetry + privacy prompt), audio/stub hardening, GPL-3.0, Dependabot/CodeQL, Vitest 4 + Zod 4 toolchain |
| **v1.5.3** | Accidental patch publish (superseded by v1.6.0 on Thunderstore) |
| **v1.5.2** | Debug server, MCP server, configurable logging system, stub fixes, Psychotic Break instantiation fix, audio loading fix, cross-platform build |
| **v1.5.1** | CD pipeline: `vmajor`/`vminor`/`vpatch` tags trigger version bumps, auto-generated GitHub Releases, changelog management |
| **v1.5.0** | Pitch randomization across all audio systems, rarer ambient sounds (60-180s, reduced weights), rarer fake footsteps (3-6 min, 20%), state leak fixes, config bounds fixes |
| **v1.4.1** | Thunderstore reupload, no functional changes |
| **v1.4.0** | Major refactor: removed 3 broken systems, added weighted audio, marker components, shared proximity scan, first real assets |
| **v1.3.x** | Rapid fixes: REPOLib GUID, HarmonyLib imports, Photon paths |
| **v1.0.0** | Initial release: 6 systems designed via dnSpy binary analysis |

Full version history: [CHANGELOG.md](CHANGELOG.md)

---

## Getting Started

### Prerequisites

- [BepInEx 5.4.21+](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
- R.E.P.O. (Steam)
- No other dependencies (REPOLib dependency removed in v1.4.0)

### Installation

#### Via Mod Manager (Recommended)
Install through r2modman or Thunderstore Mod Manager. Search for **Dread** under R.E.P.O.

#### Manual
1. Download the latest `.zip` from [Thunderstore](https://thunderstore.io/c/repo/p/elytraking/Dread/)
2. Extract into your r2modman profile directory or directly into the R.E.P.O. game folder
3. Verify folder structure:

```
BepInEx/
  plugins/
    elytraking-Dread/
      Dread.dll
      audio/
        breathing.ogg
        breath2.ogg
        breath3.ogg
        footsteps.ogg
        scraping.ogg
        whisper.ogg
        door_creak.ogg
        scream_peak.ogg
        scream_distant.ogg
        scream_threat.ogg
```

---

## Building from Source

Requires .NET SDK 4.8 targeting pack and a local R.E.P.O. installation (for `Assembly-CSharp.dll` and Photon references in `Dread.csproj`).

### Testing

This mod has no test suite. All testing is done manually in-game. The seven-system architecture (independent MonoBehaviours on `DontDestroyOnLoad` hosts) makes each system testable in isolation by disabling the others via config.

### MCP Server

The `dread-mcp-server/` directory contains a TypeScript MCP server for AI-assisted debugging. Build with:

```bash
cd dread-mcp-server
npm run build
```

### Available Scripts

| Command | Description |
|---------|-------------|
| `.\build.ps1 -Version "X.Y.Z"` | Build + package for Thunderstore upload |
| `dotnet build` | Debug build only |
| `dotnet build -c Release` | Release build only |

```powershell
.\build.ps1 -Version "1.5.3"
```

Output in `dist/` (version from `manifest.json` after CD bump):
- `elytraking-Dread-<version>/`: unpacked package folder
- `elytraking-Dread-<version>.zip`: ready for Thunderstore upload

---

## License

GNU General Public License v3.0. See [LICENSE](LICENSE).



