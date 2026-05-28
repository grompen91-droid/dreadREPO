# Psychotic Break

Rare **client-local Episode** when the player meets tension and stealth conditions. Implements `PsychoticBreakSystem.cs`. ADR: `docs/adr/0011-psychotic-break-system.md`.

## Glossary (use in issues/PRs)

| Term | Meaning |
|------|---------|
| **Episode** | Active psychotic break window (`PsychoticBreakDuration` seconds) |
| **Solo** | No other alive player within 30m |
| **Recent threat** | Enemy within 15m in last 30s (threat memory list) |
| **LoS lost** | No enemy visible via line-of-sight from camera |
| **Interaction lockdown** | Episode disables player interaction (episode logic) |

Full definitions: [CONTEXT.md](../../../CONTEXT.md).

## Trigger loop

Every **2 seconds** when enabled:

1. Skip if disabled, **Compatibility mode**, menu, or **once per match** already fired
2. `UpdateThreatTimestamps()` scans `EnemyHealth`, records times within 15m
3. `CanTrigger()` requires: solo, recent threat, no visible enemy, crouching, clips loaded
4. Roll `Random.value < PsychoticBreakTriggerChance` (default 1%)

Block reasons exposed on `DreadRuntimeState.PsychoticBreakBlockReason` (overlay + `dread_get_runtime_state`).

## Audio clips

Loaded from `audio/`:

- `scream_peak.ogg`, `scream_distant.ogg`, `scream_threat.ogg`
- Footstep clip for phantom steps during episode

`AreClipsLoaded()` must pass before trigger. Menu-level load is deferred like other systems.

## UI overlay

Builds a runtime `Canvas` with darkness/vignette images (Unity UI types resolved via reflection for stub builds). `FlashlightStateTracker` is a separate MonoBehaviour (not nested) so `AddComponent` works.

## Config (`6. Psychotic Break`)

| Key | Default | Notes |
|-----|---------|-------|
| `PsychoticBreakEnabled` | true | Master toggle |
| `PsychoticBreakTriggerChance` | 0.01 | Per 2s check |
| `PsychoticBreakDuration` | 20s | Episode length |
| `PsychoticBreakOncePerMatch` | true | Uses **Match** scope (glossary) |

**Compatibility mode** disables psychotic break entirely.

## Debug and MCP

| Entry | Method |
|-------|--------|
| Force episode | `PsychoticBreakSystem.ForceEpisodeForDebug()` |
| TCP / MCP | `force_psychotic_break` / `dread_force_psychotic_break` |

Requires debug server enabled and in-level init.

## Netcode

Client-local only. Other players do not see the overlay. Host monster state unchanged except existing game sync.

## Agents: common changes

| Change | File |
|--------|------|
| Tune conditions | `CanTrigger`, `GetTriggerBlockReason`, constants at top of class |
| New block reason | Update `GetTriggerBlockReason` + publish in `PublishRuntimeState` |
| Audio timing | Episode coroutines inside `StartEpisode` / update loop |

Do not share enemy caches with `TensionSystem`; psychotic break owns its own `FindObjectsOfType` for threat/visibility.

## Verify

- Tier 1: `dread_get_runtime_state` shows `psychoticBreak*` fields
- Manual: [error-reporting-test-checklist.md](../error-reporting-test-checklist.md) only if testing crashes; episode testing via MCP force command
