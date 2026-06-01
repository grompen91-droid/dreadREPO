# Contract: Gameplay phase gate

**Feature**: 006-lure-snitch-hardening  
**Consumers**: `CampLureSystem`, `SnitchSystem`, future host-only monster features

## API (Systems/Core/GameplayContext.cs)

| Member | Contract |
|--------|----------|
| `GameplayPhase CurrentPhase { get; }` | Returns current phase enum value |
| `bool AllowsHostMonsterFeatures { get; }` | `true` only when `CurrentPhase == ExtractionLevel` |
| `string PhaseLabel { get; }` | Human-readable for overlay (`menu`, `truck/shop`, `run`, `unknown`) |

## GameplayPhaseCompat (internal)

| Member | Contract |
|--------|----------|
| `void NotifyExtractionLevelStarted()` | Called from `SnitchLevelGenDonePatch` postfix; sets extraction latch |
| `void ResetForSceneLoad()` | Called on single scene load; clears latch |
| `GameplayPhase ResolvePhase()` | Never throws; returns `Unknown` on failure |

## Consumer rules

1. Host-only systems MUST check `AllowsHostMonsterFeatures` before any gameplay mutation (arm, pull, timer tick).
2. Block reason strings MUST include phase when inactive due to location (e.g. `"truck/shop"`).
3. Do NOT use `!IsMenuLevel()` alone as run gate for monster host features.
4. Client-local systems (tension, psychotic break) unchanged in v1 unless they incorrectly fire in shop (out of scope unless reported).

## Degradation

If REPO native phase API unavailable:

- Rely on extraction latch from `OnLevelGenDone` + single-scene reset
- Log once at Verbose: `[GameplayPhaseCompat] native API not found; using latch fallback`

## Stub requirements

Add discovered REPO types/methods to `.github/scripts/Assembly-CSharp_stubs.cs` as implementation confirms them.
