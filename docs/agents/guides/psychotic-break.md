# Psychotic Break

Rare **client-local Episode** when the player meets tension and stealth conditions. Code: `Systems/PsychoticBreak/` (`PsychoticBreakSystem.cs` plus Trigger, Episode, Overlay, PlayerLockdown, Audio partials). ADR: `docs/adr/0011-psychotic-break-system.md`.

## Glossary (use in issues/PRs)

| Term | Meaning |
|------|---------|
| **Episode** | Active psychotic break window (`PsychoticBreakDuration` seconds) |
| **Solo** | No other alive player within 30m |
| **Recent threat** | Any `EnemyHealth` within 15m in the last 30s (single memory timestamp, refreshed while in range) |
| **LoS lost** | No living enemy visible from camera after recent engagement (saw enemy, enemy within 15m while threat active, or both) and settle delay elapsed |
| **Engagement** | `_sawEnemyWhileThreatActive`: latched when threat memory refreshes (enemy within 15m); stays true until threat memory expires |
| **Hiding** | Crouch/crawl or REPO tumble/fallen pose (`PlayerControllerCompat.IsHidingVulnerable`) |
| **Interaction lockdown** | Episode disables player interaction (episode logic) |

Full definitions: [CONTEXT.md](../../../CONTEXT.md).

## Trigger loop

While enabled in a level:

1. **`UpdateThreatTimestamps()`** every frame: enemy within 15m extends threat memory 30s.
2. **`UpdateLosLostTracking()`** every frame: arms engagement on visible enemy or enemy in 15m; when not visible, starts LoS settle timer (`LosLostDelaySeconds` from tuning).
3. On each **check interval** (from `PsychoticBreakChancePercent` tuning), when not blocked:
   - `CanTrigger()` requires: solo, recent threat, LoS lost (engagement armed + settle elapsed + no visible enemy), hiding 2s+, clips loaded
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

Requires debug server enabled, in-level (not menu). Force skips trigger guards and does **not** consume once-per-match. Enables client-local damage block for the episode (so mobs on the dev player during a forced test do not kill mid-episode). Natural triggers do **not** use that block. Local loopback only.

## Netcode

Client-local only. Other players do not see the overlay or hallucination mob. The fake mob is a **mesh snapshot** baked from a nearby real enemy (`PsychoticBreakHallucinationPresenter`), not a networked prefab clone.

## Hallucination mob (client-local)

| Approach | Used |
|----------|------|
| Instantiate `Enemy` prefab | No (Photon / `Enemy.Awake` breaks) |
| Bake `SkinnedMeshRenderer` pose into local `MeshFilter` children | Yes |
| Template pick | Ranked list (mesh richness, within 28m, relaxed 65m); skips player avatars; tries next on bake fail |
| Fallback capsule silhouette + lunge | Yes if all candidates fail |
| Attack visibility | Hard strobe (~84% off) during wind-up; full mob + dim overlay on lunge |
| Damage | None; `DreadHallucinationMob` marker + hurt prefix |

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
