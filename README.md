# Dread

Atmospheric horror overhaul for R.E.P.O.

## Features

- **Ambient Audio**: Rare positional horror sounds (scraping, footsteps, breathing, door creaks, whispers)
- **Visual Corruption**: Random light flickering, vignette pulses, shadow glitches at the edge of view
- **Environmental Wrongness**: Small objects and lights subtly shift between visits
- **Monster Overhaul**: 2x HP, increased aggression, deeper audio, screen distortion when close

## Netcode

| Feature | Who needs mod |
|---|---|
| Monster HP & aggression | Host only |
| All audio/visual effects | All players |

Players without the mod can join host-modded lobbies. Monster HP/aggression changes apply to everyone (host-authoritative), but atmospheric effects only appear for players who have the mod installed.

## Audio Files

Place OGG sound files in `BepInEx/plugins/elytraking-Dread/audio/`:

- `scraping.ogg`
- `footsteps.ogg`
- `breathing.ogg`
- `door_creak.ogg`
- `whisper.ogg`

Free horror sounds available at [freesound.org](https://freesound.org). Without audio files, the audio system logs a warning and skips silently.

## Config

All systems independently configurable via `BepInEx/config/elytraking.dread.cfg` (generated on first run).

## Compatible With

- Mimic / Mimic Patcher
- Wesley's Enemies
- REPOLib, REPOConfig, MenuLib
- MoreUpgrades, KeybindLib, UnlimitedOrbs
- Empress LateJoin, DeathMinimap
