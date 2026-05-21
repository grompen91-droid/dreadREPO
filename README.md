# Dread

> **Atmospheric horror overhaul for R.E.P.O.**
> Ambient dread, scarier monsters, and a tension system that reacts to danger in real time.

![Version](https://img.shields.io/badge/version-1.4.1-crimson?style=flat-square)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.21-blueviolet?style=flat-square)
![Game](https://img.shields.io/badge/game-R.E.P.O.-orange?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

---

## Overview

Dread transforms R.E.P.O. into a genuinely unsettling experience. It layers three independent systems on top of the base game: a spatial audio horror system that plants ambient sounds around you, a monster overhaul that makes enemies faster and more alarming, and a tension system that responds dynamically to how close danger actually is.

Every feature is independently toggleable and configurable. Players without Dread can still join modded lobbies.

---

## Features

### Ambient Audio

Rare, positional sounds are placed in the world during runs. They have no source and no warning.

| Sound | Rarity | Description |
|-------|--------|-------------|
| `scraping.ogg` | Common | Something dragging across the floor |
| `footsteps.ogg` | Common | Steps from a direction nobody is in |
| `breathing.ogg` | Uncommon | Close, slow breathing nearby |
| `whisper.ogg` | Rare | A voice at the edge of hearing |

- Sounds spawn in 3D space relative to your camera, at randomized distance (5m to 15m)
- Frequency and volume are both configurable
- Disabled automatically on menu screens

---

### Monster Overhaul

> **Warning:** Monster speed and detection changes are host-authoritative. The host must have Dread installed for these to apply.

#### Speed and Aggression
- Enemy `NavMeshAgent` speed and acceleration boosted by **1.2x** at spawn
- Cached default speed also patched so speed resets stay at the boosted value
- Affects all enemies including modded ones (Mimic, WesleysEnemies, etc.)

#### Audio Overhaul
- Enemy pitch lowered to **0.72x** for deeper, more threatening sounds
- Reverb zone mix increased to **1.1** for spatial weight
- Applied dynamically every 4 seconds, works retroactively on newly spawned enemies
- Marker component prevents double-patching

#### Detection Radius
- `EnemyDirector.SetInvestigate` radius increased by **1.5x**
- Voice and physics noise alerts enemies from further away
- Capped at 1.5x intentionally: higher values overwhelm Photon enemy-position sync on clients

---

### Tension System

The tension system scans for nearby enemies every 0.5 seconds and adjusts gameplay in real time based on proximity.

#### Adrenaline
- Sprint energy drain reduced by up to **70%** when an enemy is within 15m
- Scales linearly with distance: closer enemy = slower drain
- Smoothly lerps back to normal drain when the threat clears
- Energy restored on scene transition

#### Panic Sprint
- Starting a sprint within **15m** of an enemy triggers a **1.25x** speed burst for 2 seconds
- 20 second cooldown per trigger
- Multiplier is restored cleanly on cooldown or scene change

#### Out of Breath
- Plays a gasp/breathing sound when sprint stamina drops below ~10% after active sprinting
- 60 second cooldown prevents spam
- Supports multiple breath audio variants (`breathing.ogg`, `breath2.ogg`, `breath3.ogg`)

#### Fake Footsteps
- Randomly plays footstep sounds **behind** the player, from 2.5m to 5m back
- Triggers at a random interval of 2 to 4 minutes, with a 35% chance to actually play
- 3D spatialized, low volume, short falloff range

---

### QOL

| Feature | Value | Notes |
|---------|-------|-------|
| Crouch Speed Boost | +30% | Makes stealth movement viable |

---

## Configuration

All settings are in `BepInEx/config/elytraking.dread.cfg`, generated on first run.

Compatible with **REPOConfig** for in-game editing without restarting.

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

## Netcode Compatibility

| Feature | Who needs the mod |
|---------|-------------------|
| Monster speed and acceleration | Host only |
| Enemy detection radius | Host only |
| Ambient audio | Per client |
| Adrenaline and panic sprint | Per client |
| Out of breath sounds | Per client |
| Fake footsteps | Per client |
| Crouch speed boost | Per client |

Players without Dread **can join** modded lobbies. Monster changes apply only if the host has Dread. All atmospheric and tension effects are local and do not affect other players.

---

## Requirements

- [BepInEx 5.4.21+](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)

No other dependencies required as of v1.4.0.

---

## Installation

### Via Mod Manager (Recommended)
Install through r2modman or Thunderstore Mod Manager. Search for **Dread** under R.E.P.O.

### Manual
1. Download the latest `.zip` from [Thunderstore](https://thunderstore.io/c/repo/p/elytraking/Dread/)
2. Extract into your r2modman profile directory, or directly into your R.E.P.O. game folder
3. Folder structure must match:
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

Requires .NET SDK and a local R.E.P.O. installation (for assembly references).

```powershell
# From the project root
.\build.ps1 -Version "1.4.1"
```

Output goes to `dist\elytraking-Dread-1.4.1\` and `dist\elytraking-Dread-1.4.1.zip`.

The zip is ready to upload directly to Thunderstore.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for full version history.

---

## License

MIT. Use, modify, and redistribute freely with attribution.
