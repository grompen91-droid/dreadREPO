# Dread — Atmospheric Horror Mod for R.E.P.O.

**Date:** 2026-05-16  
**Platform:** Steam / R.E.P.O.  
**Framework:** BepInEx  
**Scope:** Medium — ~5-7 files, all systems independently toggleable via config

---

## Goal

Transform R.E.P.O. into a more unsettling horror experience without breaking the vanilla gameplay loop. No new monsters or maps — pure atmosphere layered on top, plus monster gameplay tweaks that make encounters genuinely threatening.

---

## System 1: Audio Dread (Client-side, all players)

Inject rare ambient horror sounds throughout a run:

- Distant scraping, wrong-direction footsteps, faint breathing, door creaks with no source
- All sounds are 3D-positioned in world space to trick spatial awareness
- Random timing — never predictable, never looping on a fixed interval
- Sounds fire from randomized positions around the map, not attached to any entity

**Config options:** enable/disable, frequency multiplier, volume

---

## System 2: Visual Corruption (Client-side, all players)

Layer subtle visual effects that create unease:

- Random room lights flicker briefly at random intervals
- Occasional shadow passes across a doorway then vanishes
- Vignette pulse: screen edges darken for 1-2 seconds, then fade
- Film grain intensity increases in dark/low-light areas

**Config options:** enable/disable per effect, intensity multipliers

---

## System 3: Environmental Wrongness (Client-side, all players)

Make the map feel subtly alive and wrong:

- Small props/objects shift position slightly between room visits
- Rare: a door is slightly more open than the player left it
- Rare: a light that was on is now off, or vice versa

These are client-side visual changes only — no actual game object state sync required.

**Config options:** enable/disable, frequency of rare events

---

## System 4: Monster Overhaul (Host-side for gameplay; client-side for audio/visual)

Make monsters feel genuinely threatening and harder to read.

### Gameplay (host only required)
- HP multiplier — configurable, default 2x
- Aggression tuning — faster reaction time, less predictable patrol patterns, shorter cooldown between attacks

### Audio (all players need mod)
- Replace or layer scarier growls, breathing, and chase sounds on existing monsters
- Add anticipation audio (faint sound before monster detects player)

### Visual (all players need mod)
- Subtle screen distortion/blur effect when a monster is close
- Darker texture tint on monsters — harder to make out in low light
- Slight visual shimmer/aberration on monster model edges

**Config options:** HP multiplier value, aggression level (low/medium/high), enable/disable audio overhaul, enable/disable visual effects

---

## Architecture

```
dread/
  Plugin.cs               -- BepInEx entry point, loads all systems
  config/
    DreadConfig.cs        -- Unified config binding
  systems/
    AudioDreadSystem.cs   -- System 1
    VisualCorruptionSystem.cs  -- System 2
    EnvironmentalSystem.cs     -- System 3
    MonsterOverhaulSystem.cs   -- System 4
  assets/
    audio/                -- Custom ambient + monster audio clips
```

Each system initializes independently. If a system fails, others continue.

---

## Netcode Notes

| Feature | Who needs mod |
|---|---|
| Monster HP multiplier | Host only |
| Monster aggression | Host only |
| Monster audio overhaul | All players |
| Monster visual effects | All players |
| Audio Dread (ambient) | All players |
| Visual Corruption | All players |
| Environmental Wrongness | All players |

Players without the mod can still join host-modded lobbies. They experience vanilla audio/visuals but monster HP/aggression changes still apply (host-authoritative).

---

## Config File (dread.cfg)

All systems default to enabled. Players can tune or disable individually. Config generated on first run via BepInEx ConfigFile API.

---

## Out of Scope

- New monster types
- New maps or levels
- UI changes
- Multiplayer-synced environmental wrongness (objects stay client-side only)
