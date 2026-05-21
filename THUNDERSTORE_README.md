# Dread

Atmospheric horror overhaul for R.E.P.O. Ambient dread, scarier monsters, and a tension system — without breaking the vanilla experience.

## Features

### Ambient Audio

Rare positional sounds placed around the level during runs — scraping, distant footsteps, breathing, whispers. Fully spatial and unpredictable. Each sound plays at a randomized pitch so no two are identical.

### Monster Overhaul

- **1.2x speed and acceleration** — monsters are noticeably more aggressive
- **Randomized audio pitch** — each enemy has a unique, randomized voice on spawn
- **Wider detection radius** — voice and physics noise alerts enemies further away

### Tension System

- **Adrenaline** — sprint energy drains up to 70% slower when an enemy is within 15m
- **Panic sprint** — brief 1.25x speed burst on sprint start near an enemy, 20 second cooldown
- **Out of breath** — gasp sound plays when stamina runs out after sprinting, 60 second cooldown
- **Fake footsteps** — footstep sounds occasionally spawn behind you with no source

### QOL

- **Crouch speed** — 30% faster, making stealth movement actually viable

## Netcode

| Feature | Requires mod |
|---------|-------------|
| Monster speed and detection | Host only |
| Enemy audio overhaul | Per client |
| Ambient audio | Per client |
| Adrenaline, panic sprint, breath, fake footsteps | Per client |
| Crouch speed boost | Per client |

Monster changes are host-authoritative. Atmospheric effects only appear for players with the mod installed. Players without Dread can join modded lobbies.

## Config

All features independently toggleable via `BepInEx/config/elytraking.dread.cfg` (generated on first run). Compatible with **REPOConfig** for in-game editing.

## Requirements

- BepInEx 5.4.21+
- No other dependencies
