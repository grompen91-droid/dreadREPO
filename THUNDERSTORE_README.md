# Dread

Atmospheric horror overhaul for R.E.P.O. Three systems that layer ambient dread, scarier monsters, and real-time proximity tension.

## Features

**Ambient Audio:** weighted random sounds spawn 5-15m around you every 60-180s. Pitch randomized 0.5x-1.5x. Fully spatialized.

| Sound | Rarity |
|-------|--------|
| Scraping | Common |
| Footsteps | Common |
| Breathing | Uncommon |
| Whisper | Rare |

**Monster Overhaul:** enemy speed +1.2x, detection range +1.5x, audio pitch lowered and spatialized. Works on modded enemies.

**Tension System:** scans for danger every 0.5s. Adrenaline reduces sprint drain near enemies. Panic sprint gives a speed burst. Fake footsteps play behind you. Breath sounds on low stamina.

**QOL:** crouch speed +30%.

## Installation

**Mod Manager (recommended):** Search Dread under R.E.P.O. in r2modman or Thunderstore Mod Manager.

**Manual:** Download from Thunderstore, extract into `BepInEx/plugins/elytraking-Dread/`. Requires BepInEx 5.4.2100+. No other dependencies.

## Multiplayer

Monster changes are host-authoritative. Audio and tension are client-local. Players without Dread can join normally.

| Feature | Who Needs It |
|---------|-------------|
| Monster speed, detection | Host only |
| Everything else | Per client |

## Configuration

Generated at `BepInEx/config/elytraking.dread.cfg` on first launch. Compatible with REPOConfig for live editing.

```
[1. Audio Dread]
Enabled = true
Frequency = 1.0
Volume = 0.4

[2. Monster Overhaul]
AggressionEnabled = true
AudioEnabled = true

[3. Tension]
AdrenalineEnabled = true
PanicSprintEnabled = true
LowStaminaSoundEnabled = true
FakeFootstepsEnabled = true

[4. QOL]
CrouchSpeedBoost = true
```

## Compatibility

Works with modded enemies (Mimic, WesleysEnemies, etc.), REPOConfig, and other audio mods. REPOLib not required.

[Changelog](CHANGELOG.md)

