# Psychotic Break

Rare **client-local Episode** when the player meets tension and stealth conditions. Code: `Systems/PsychoticBreak/` (`PsychoticBreakSystem.cs` plus Trigger, Episode, Overlay, PlayerLockdown, Audio partials). ADR: `docs/adr/0011-psychotic-break-system.md`.

## Glossary (use in issues/PRs)

| Term | Meaning |
|------|---------|
| **Episode** | Active psychotic break window (`PsychoticBreakDuration` seconds) |
| **Solo** | No other alive player within 30m |
| **Recent threat** | Any `EnemyHealth` within 15m in the last 30s (single memory timestamp, refreshed while in range) |
| **LoS lost** | No living enemy visible via line-of-sight from camera |
| **Hiding** | Crouch/crawl or REPO tumble/fallen pose (`PlayerControllerCompat.IsHidingVulnerable`) |
| **Interaction lockdown** | Episode disables player interaction (episode logic) |

Full definitions: [CONTEXT.md](../../../CONTEXT.md).

## Trigger loop

While enabled in a level:

1. **`UpdateThreatTimestamps()`** runs every frame: if any enemy is within 15m of the player, extend threat memory by 30s.
2. Every **2 seconds**, when not blocked by menu/compat/once-per-match:
   - `CanTrigger()` requires: solo, recent threat, no visible living enemy, hiding, clips loaded
   - Roll `Random.value < perRollProbability` (derived from `PsychoticBreakChancePercent`, default 1% target per hide window)

Block reasons on `DreadRuntimeState.PsychoticBreakBlockReason` (overlay + `dread_get_runtime_state`). `PsychoticBreakThreatCount` is **seconds left** on threat memory (0 = none).

## Enemy scan

Uses shared **`ProximityScan`** in `Systems/Core/` (0.5s refresh, same pattern as tension proximity). Do not add a second static enemy list for psychotic break.

## Audio clips

Loaded from `audio/` via **`AudioClipLoader`** (shared cache with tension/ambient):

- `scream_peak.ogg`, `scream_distant.ogg`, `scream_threat.ogg`
- `footsteps.ogg`, `footsteps_run.ogg` (staggered walk then run during episode)

`AreClipsLoaded()` must pass before a natural trigger. Menu-level load is deferred like other systems.

## UI overlay

Runtime `Canvas` + `RawImage` (reflection for stub/Proton builds). `OverlayTextureUtil` for vignette textures. Episode ends on timer even if overlay creation fails.

## Config (`6. Psychotic Break`)

| Key | Default | Notes |
|-----|---------|-------|
| `PsychoticBreakEnabled` | true | Master toggle |
| `PsychoticBreakChancePercent` | 1.0 | Target % per full eligible hide window (internals derived) |
| `PsychoticBreakAccentEnabled` | true | Horror edge accents on blackout overlay |
| `PsychoticBreakDuration` | 20s | Episode length |
| `PsychoticBreakOncePerMatch` | true | Uses **Match** scope (glossary) |

**Compatibility mode** disables psychotic break entirely.

## Debug and MCP

| Entry | Method |
|-------|--------|
| Force episode | `PsychoticBreakSystem.ForceEpisodeForDebug()` |
| TCP / MCP | `force_psychotic_break` / `dread_force_psychotic_break` |

Requires debug server enabled, in-level (not menu). Force skips trigger guards and does **not** consume once-per-match. Local loopback only.

## Netcode

Client-local only. Other players do not see the overlay. Host monster state unchanged except existing game sync.

## Agents: common changes

| Change | File |
|--------|------|
| Tune conditions | `CanTrigger`, `GetTriggerBlockReason`, constants at top of class |
| New block reason | Update `GetTriggerBlockReason` + publish in `PublishRuntimeState` |
| Audio timing | Episode update loop / `StartEpisode` |
| Threat memory | `_threatMemoryUntil`, `UpdateThreatTimestamps` |

## Verify

- Tier 1: `dread_get_runtime_state` shows `psychoticBreak*` fields (including `psychoticBreakEnemyCount`, threat seconds)
- Manual episode: MCP `dread_force_psychotic_break` ([debug-tooling.md](debug-tooling.md))
