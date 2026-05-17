# Dread

Atmospheric horror overhaul for R.E.P.O. Makes the game feel genuinely unsettling without breaking the vanilla experience.

## Features

### Host Options
- **Force gamma** — host can push a specific gamma value (0–100) to all clients on level load. Disabled by default. Default value 40 (matches game default).

### Ambient Audio
Rare positional horror sounds (scraping, footsteps, breathing, door creaks, whispers) placed around the level during runs.

### Monster Overhaul
- **1.2x speed and acceleration** — noticeably more aggressive (host only)
- **Deeper audio** — pitch lowered, reverb increased on all enemy sounds
- **Wider detection radius** — voice and noise alerts enemies across the room, not just in melee range

### Tension System
- **Adrenaline** — sprint energy drains up to 70% slower when an enemy is nearby (within 15m)
- **Out of breath** — plays a gasp sound when stamina drops below 10%, 60 second cooldown per trigger
- **Fake footsteps** — rarely plays footstep sounds positioned behind you with no source
- **Panic sprint** — brief 1.25x speed burst when sprinting near an enemy (within 15m), 20 second cooldown

### QOL
- **Crouch speed** — 30% faster, making crouching a viable movement option

## Netcode

| Feature | Who needs mod |
|---|---|
| Monster speed | Host only |
| Detection radius | Host only |
| All audio/visual effects | Per client |
| Adrenaline & stamina sounds | Per client |
| Force gamma | All clients (requires REPOLib) |

Players without the mod can join modded lobbies. Monster changes are host-authoritative and apply to everyone. Atmospheric effects only appear for players with the mod installed.

## Config

All features independently toggleable via `BepInEx/config/elytraking.dread.cfg` (generated on first run). Compatible with REPOConfig for in-game editing.

