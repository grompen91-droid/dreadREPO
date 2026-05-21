# Dread

> **Atmospheric horror overhaul for R.E.P.O.**  
> Three runtime systems that layer ambient dread, scarier monsters, and a tension system that reads your proximity to danger in real time.

![Version](https://img.shields.io/badge/version-1.4.1-crimson?style=flat-square)
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

Dread is a BepInEx plugin that transforms R.E.P.O. into a genuinely unsettling experience at the IL level. It uses **Harmony 2 runtime patching** to intercept enemy spawn, movement, and detection methods, while three independent MonoBehaviour systems run on a persistent game object that survives scene transitions.

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
```

All three systems load their audio assets from a DLL-adjacent `audio/` folder via `UnityWebRequestMultimedia.GetAudioClip` with `AudioType.OGGVORBIS`. They are independent by design: a failure in one system does not affect the others.

---

## Features

### Ambient Audio

A coroutine loop places rare, positional sounds in the world during runs. Every 30 to 90 seconds (scaled by config multiplier), a sound is selected using weighted random distribution and spawned at a random 3D position 5 to 15 meters from the player's camera.

| Sound | Weight | Rarity | Description |
|-------|--------|--------|-------------|
| `scraping.ogg` | 1.0 | Common | Something dragging across the floor |
| `footsteps.ogg` | 1.0 | Common | Steps from a direction nobody is in |
| `breathing.ogg` | 0.6 | Uncommon | Close, slow breathing nearby |
| `whisper.ogg` | 0.25 | Rare | A voice at the edge of hearing |

- Fully spatialized (`spatialBlend = 1.0`), linear rolloff, falloff from 1m to 25m
- Each AudioSource self-destructs after `clip.length + 0.5s`
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
- Lowers enemy `AudioSource.pitch` to **0.72x** (darker, deeper)
- Raises `reverbZoneMix` to **1.1** (more spatial presence)
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
- Coroutine fires every 2 to 4 minutes with **35%** chance to play
- Spawns a 3D footstep sound 2.5m to 5m behind the player, slightly off-center
- Low volume, short falloff (0.5m min, 8m max)

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
    DreadConfig.cs                   # Static ConfigEntry bindings, 4 config sections
  Systems/
    AudioDreadSystem.cs              # Coroutine: weighted ambient at 30-90s intervals
    MonsterOverhaulSystem.cs         # 3 Harmony patches + 4s audio scan loop
    TensionSystem.cs                 # 0.5s proximity scan + 4 subsystems
  audio/
    scraping.ogg                     # Common ambient sound
    footsteps.ogg                    # Common ambient + fake footsteps
    breathing.ogg                    # Uncommon ambient + out-of-breath sound
    whisper.ogg                      # Rare ambient
    door_creak.ogg                   # Ambient variant
  docs/
    agents/                          # Agent workflow documentation
    superpowers/specs/               # Design specifications (v1.0, panic sprint)
    superpowers/plans/               # Implementation plans (1245-line original plan)
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

See [CHANGELOG.md](CHANGELOG.md) for full version history.

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
```

---

## Building from Source

Requires .NET SDK 4.8 targeting pack and a local R.E.P.O. installation (for `Assembly-CSharp.dll` and Photon references in `Dread.csproj`).

### Testing

This mod has no test suite. All testing is done manually in-game. The three-system architecture (independent MonoBehaviours on a `DontDestroyOnLoad` host) makes each system testable in isolation by disabling the others via config.

### Available Scripts

| Command | Description |
|---------|-------------|
| `.\build.ps1 -Version "X.Y.Z"` | Build + package for Thunderstore upload |
| `dotnet build` | Debug build only |
| `dotnet build -c Release` | Release build only |

```powershell
.\build.ps1 -Version "1.4.1"
```

Output in `dist/`:
- `elytraking-Dread-1.4.1/`: unpacked package folder
- `elytraking-Dread-1.4.1.zip`: ready for Thunderstore upload

---

## License

MIT
