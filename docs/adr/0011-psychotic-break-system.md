# ADR-0011: Psychotic Break System

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

The TensionSystem handles proximity-based audio and stamina effects but has no mechanic for extended, cinematic psychological episodes. Playtesting feedback indicated that the mod's horror beats are too predictable: players know exactly what will happen when a monster is near. There is no rare "oh shit" moment that breaks the pattern and creates a story the player tells afterwards.

The Psychotic Break fills this gap: a rare, involuntary hallucinatory episode triggered by sustained monster proximity combined with line-of-sight loss and player vulnerability (crouching while alone).

---

## Decision

Add a new standalone `PsychoticBreakSystem` (MonoBehaviour) with the following design:

### Trigger Conditions (all must be met)

1. **Solo:** No other alive player within 30m
2. **Recent threat:** An `EnemyHealth` was within 15m in the last 30 seconds
3. **LoS lost:** Player camera cannot see any enemy currently (they may have ducked behind cover or the monster walked away)
4. **Crouching:** Player is currently crouching
5. **Roll:** 1% probability check every 2 seconds
6. **Once per match:** Never triggers again after first episode (config-toggleable)

### Episode Sequence (fixed 20s duration, configurable)

| Time | Effect |
|------|--------|
| 0s-3s | Pulsing darkness overlay ramps in. Flashlight disabled. Interaction locked down. |
| 3s-10s | Edge shadow vignette flickers in crescendo pattern. Circling footsteps audio (panning L-to-R, subtle at first). |
| 10s-16s | Footsteps become heavy running. Shadow scream audio plays (1 of 3 variants). Random phantom monster sounds. |
| 16s-20s | Everything peaks, then cuts out. Screen flash/stumble effect. Episode ends. Player control restored. |

### Audio Assets

| Asset | Usage |
|-------|-------|
| `shadow_scream_1.ogg` | Scream variant 1 (played once per episode) |
| `shadow_scream_2.ogg` | Scream variant 2 |
| `shadow_scream_3.ogg` | Scream variant 3 |
| `phantom_footsteps.ogg` | Looping circling footsteps, panned dynamically |

### Camera Effects (client-local, no netcode)

- **Darkness overlay:** Full-screen `Color.black` quad with alpha animated 0 -> 0.85 over 3s, holds, then fades out over 2s at end
- **Edge shadow flicker:** Vignette-style UI image, alpha animated in short random bursts (crescendo pattern) during 3s-16s window
- **Stumble:** Brief camera roll + vertical offset at episode end, ~0.5s duration

### Interaction Lockdown

- `PlayerController` input block: disable interact, item use, drop, swap during episode
- Flashlight toggled off at episode start, re-enabled at end

### Configuration

All entries in `DreadConfig` under a new `PsychoticBreak` section:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `PsychoticBreakEnabled` | bool | true | Master toggle |
| `PsychoticBreakTriggerChance` | float | 0.01 | Probability per 2s check (0-1) |
| `PsychoticBreakDuration` | float | 20.0 | Episode length in seconds |
| `PsychoticBreakOncePerMatch` | bool | true | Limit to one episode per match |

### Implementation Details

- New file: `Systems/PsychoticBreakSystem.cs`
- Follows existing patterns: `DontDestroyOnLoad` spawn from `DreadHost`, self-disabling on menu screens
- Audio loaded via `UnityWebRequest` (as established in ADR-0006)
- Uses `EnemyHealth` cache from `TensionSystem` scan (or independently scans `FindObjectsOfType<EnemyHealth>` on its own 2s interval)
- No Harmony patches needed (client-local only, no netcode)

---

## Consequences

- Players get a rare, memorable horror beat that breaks the predictable tension loop
- The solo + crouching + LoS lost condition set makes it feel earned, not random
- Once-per-match default prevents repeat saturation
- No netcode complexity -- all effects are client-local
- New audio assets (4 OGG files) need to be packaged in Thunderstone build
- Duration and trigger chance are configurable for player preference

---

## Rejected Alternatives

- **Trigger on any damage taken:** Too predictable, overlaps with existing panic mechanics. Would trigger multiple times per match.
- **Always-on sanity / dread meter:** Over-engineered. A single rare episode is more impactful than a gradual mechanic that players learn to ignore.
- **Multiplayer-triggered episode (all players):** High netcode complexity, risk of desync. The solo condition makes it feel personal.
- **Use existing TensionSystem coroutines:** The episode state machine is complex enough to warrant its own MonoBehaviour rather than being bolted onto an existing Update loop.
