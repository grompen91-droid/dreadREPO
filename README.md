# Dread

> **Atmospheric horror overhaul for R.E.P.O.**  
> Five runtime systems that layer ambient dread, scarier monsters, a tension system that reads your proximity to danger in real time, and a psychotic break episode when you are alone and scared.

![Version](https://img.shields.io/badge/version-1.6.0-crimson?style=flat-square)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.21-blueviolet?style=flat-square)
![Game](https://img.shields.io/badge/game-R.E.P.O.-orange?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)
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

Dread is a BepInEx plugin that transforms R.E.P.O. into a genuinely unsettling experience at the IL level. It uses **Harmony 2 runtime patching** to intercept enemy spawn, movement, and detection methods, while five independent MonoBehaviour systems run on a persistent game object that survives scene transitions.

Every feature is independently toggleable via `BepInEx/config/elytraking.dread.cfg`. Players without Dread can join modded lobbies: monster changes are host-authoritative, while audio and tension effects are client-local.

---

## How It Works

```
Plugin.Awake()
  +-- Harmony.PatchAll()          # patches enemy/player methods at runtime
  +-- ConfigFile binding          # creates elytraking.dread.cfg
  |
Plugin.Start()
  +-- DontDestroyOnLoad("DreadHost")
       +-- AudioDreadSystem       # coroutine: weighted ambient sounds
       +-- MonsterOverhaulSystem  # scan loop + 3 Harmony patches
       +-- TensionSystem          # 0.5s proximity scan drives 4 features
       +-- PsychoticBreakSystem   # 2s trigger check: solo + threat memory + LoS loss + crouching -> 20s episode
```

All five systems load their audio assets from a DLL-adjacent `audio/` folder via `UnityWebRequestMultimedia.GetAudioClip` with `AudioType.OGGVORBIS`. They are independent by design: a failure in one system does not affect the others.

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
| `shadow_scream_1.ogg` | -- | Psychotic Break | Close, guttural scream (variant 1) |
| `shadow_scream_2.ogg` | -- | Psychotic Break | Close, guttural scream (variant 2) |
| `shadow_scream_3.ogg` | -- | Psychotic Break | Close, guttural scream (variant 3) |
| `phantom_footsteps.ogg` | -- | Psychotic Break | Footsteps circling the player |

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

#### Out of Breath
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
| 3s-10s | Crescendo | Vignette flicker accelerates, footsteps begin circling (stereo panning) |
| 10s-16s | Peak | Circling footsteps intensify, shadow scream audio plays (one of three variants), random phantom monster sounds |
| 16s-20s | Climax | Footsteps close + fade, screen cuts, player stumbles (camera roll + dip) |

During the episode, the **flashlight is disabled** and **all player input is locked** (movement, interaction, menus). The episode is **client-local**: other players in multiplayer see the flashlight flicker but not the overlay.

See [Psychotic Break Configuration](#psychotic-break) for the toggle, trigger chance, and duration settings.

---

### QOL

| Feature | Value | Implementation |
|---------|-------|----------------|
| Crouch Speed Boost | +30% | Harmony postfix on `PlayerController.Awake`, patches cached field via `Traverse` |

---

## Configuration

`BepInEx/config/elytraking.dread.cfg` is generated on first run. Compatible with **REPOConfig** for live editing.

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

[4. QOL]
CrouchSpeedBoost = true

[5. Psychotic Break]
PsychoticBreakEnabled = true
PsychoticBreakTriggerChance = 0.01     # 1% per 2s check
PsychoticBreakDuration = 20            # episode length in seconds
PsychoticBreakOncePerMatch = true
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

Dread works alongside other mods without conflicts:

- **Modded enemies** (Mimic, WesleysEnemies, etc.): fully supported. The 4s audio scan loop catches any `EnemyHealth` component regardless of origin, and Harmony patches apply to all `EnemyNavMeshAgent` derivatives.
- **REPOConfig**: compatible for live config editing.
- **Other audio mods**: no conflict. Dread only modifies its own spawned `AudioSource` objects and enemy child audio sources via the marker guard.
- **REPOLib**: no longer required (removed in v1.4.0). Dread is dependency-free.

The `audio/` folder includes `door_creak.ogg` which is shipped but not currently loaded by any system. It is available for future ambient variants or custom sound replacement.

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET Framework 4.8, `net48`) |
| Mod framework | BepInEx 5.4.2100+ (`BaseUnityPlugin`) |
| Patcher | Harmony 2 (runtime IL patching) |
| Game engine | Unity (via `Assembly-CSharp.dll`) |
| Networking | Photon PUN (`PhotonUnityNetworking`, `Photon3Unity3D`) |
| Audio | OGG Vorbis, loaded via `UnityWebRequestMultimedia` |

---

## Project Structure

```
Dread/
  Plugin.cs                          # BepInEx entry: Awake (config + Harmony) / Start (systems)
  Dread.csproj                       # net48, references BepInEx/Harmony/Unity/Photon/Assembly-CSharp
  Config/
    DreadConfig.cs                   # Static ConfigEntry bindings, 5 config sections
  Systems/
    AudioDreadSystem.cs              # Coroutine: weighted ambient at 60-180s intervals, pitch randomized
    MonsterOverhaulSystem.cs         # 3 Harmony patches + 4s audio scan loop
    TensionSystem.cs                 # 0.5s proximity scan + 4 subsystems
    PsychoticBreakSystem.cs          # 2s trigger check + 20s episode state machine
  audio/
    scraping.ogg                     # Common ambient sound
    footsteps.ogg                    # Common ambient + fake footsteps
    breathing.ogg                    # Uncommon ambient + out-of-breath sound
    whisper.ogg                      # Rare ambient
    door_creak.ogg                   # Ambient variant
    shadow_scream_1.ogg              # Psychotic Break scream variant 1
    shadow_scream_2.ogg              # Psychotic Break scream variant 2
    shadow_scream_3.ogg              # Psychotic Break scream variant 3
    phantom_footsteps.ogg            # Psychotic Break circling footsteps
  build.ps1                          # PowerShell build + Thunderstore packaging
  manifest.json                      # Thunderstore package metadata
```

---

## Version History

| Version | Highlights |
|---------|------------|
| **v1.0.0** | Initial release: 6 systems designed via dnSpy binary analysis |
| **v1.3.x** | Rapid fixes: REPOLib GUID, HarmonyLib imports, Photon paths |
| **v1.4.0** | Major refactor: removed 3 broken systems, added weighted audio, marker components, shared proximity scan, first real assets |
| **v1.4.1** | Thunderstore reupload, no functional changes |
| **v1.5.0** | Pitch randomization across all audio systems, rarer ambient sounds (60-180s, reduced weights), rarer fake footsteps (3-6 min, 20%), state leak fixes, config bounds fixes |

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
        footsteps.ogg
        scraping.ogg
        whisper.ogg
        door_creak.ogg
        shadow_scream_1.ogg
        shadow_scream_2.ogg
        shadow_scream_3.ogg
        phantom_footsteps.ogg
```

---

## Building from Source

Requires .NET SDK 4.8 targeting pack and a local R.E.P.O. installation (for `Assembly-CSharp.dll` and Photon references in `Dread.csproj`).

### Testing

This mod has no test suite. All testing is done manually in-game. The five-system architecture (independent MonoBehaviours on a `DontDestroyOnLoad` host) makes each system testable in isolation by disabling the others via config.

### Available Scripts

| Command | Description |
|---------|-------------|
| `.\build.ps1 -Version "X.Y.Z"` | Build + package for Thunderstore upload |
| `dotnet build` | Debug build only |
| `dotnet build -c Release` | Release build only |

```powershell
.\build.ps1 -Version "1.5.0"
```

Output in `dist/`:
- `elytraking-Dread-1.5.0/`: unpacked package folder
- `elytraking-Dread-1.5.0.zip`: ready for Thunderstore upload

---

## License

MIT
