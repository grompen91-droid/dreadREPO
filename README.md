# Dread

Atmospheric horror overhaul for R.E.P.O. Ambient dread, scarier monsters, and host tools — without breaking the vanilla experience.

## Features

### Host Options
Controls available to the lobby host via config. Applied to all clients on level load.

- **Force gamma** — push a specific gamma value (0–100) to all clients. Default 40 (matches game default). Disabled by default.
- **Force render size** — push a specific render size (1–100%) to all clients. 100 = no pixelation, lower = more pixelated. Disabled by default.

### Ambient Audio
Rare positional horror sounds placed around the level during runs — scraping, distant footsteps, breathing, door creaks, whispers. Spatial and unpredictable.

### Monster Overhaul
- **1.2x speed and acceleration** — monsters are noticeably more aggressive
- **Deeper audio** — pitch lowered and reverb increased on all enemy sounds
- **Wider detection radius** — voice and physics noise alerts enemies further away

### Tension System
- **Adrenaline** — sprint energy drains up to 70% slower when an enemy is nearby (within 15m)
- **Panic sprint** — brief 1.25x speed burst on sprint start near an enemy, 20 second cooldown
- **Out of breath** — gasp sound plays when stamina drops below 10%, 60 second cooldown
- **Fake footsteps** — footstep sounds occasionally play behind you with no source

### QOL
- **Crouch speed** — 30% faster, making stealth movement actually viable

## Netcode

| Feature | Requires mod |
|---|---|
| Monster speed & detection | Host only |
| Force gamma / render size | All clients |
| Ambient audio & tension effects | Per client |
| Panic sprint & adrenaline | Per client |

Players without Dread can join modded lobbies. Monster changes are host-authoritative. Atmospheric effects only appear for players with the mod installed. Host Options require all clients to have Dread and REPOLib.

## Requirements

- [REPOLib](https://thunderstore.io/c/repo/p/Zehs/REPOLib/) — required for Host Options networking

## Config

All features independently toggleable via `BepInEx/config/elytraking.dread.cfg` (generated on first run). Compatible with REPOConfig for in-game editing.
