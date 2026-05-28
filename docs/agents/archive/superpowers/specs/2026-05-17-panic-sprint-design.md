# Panic Sprint — Design Spec
Date: 2026-05-17

## Summary

Brief speed burst when player starts sprinting while an enemy is within proximity range. Pure background gameplay feel — no visual indicator.

## Trigger Conditions

- Enemy within 15m (reuse `_nearestDist` from existing proximity scan in `TensionSystem`)
- Player begins sprinting (detected via `EnergyCurrent` dropping while stamina > 0)
- Cooldown not active

## Effect

- Multiply PlayerController sprint move speed by **1.25x** for **2 seconds**
- Lerp back to base speed after burst expires
- **Cooldown:** 20 seconds between bursts

## Config

- New entry `PanicSprintEnabled` (bool, default true) in section `3. Tension` inside `DreadConfig.cs`

## Implementation Location

All logic in `TensionSystem.cs`. No new files needed.

Steps:
1. Confirm correct `PlayerController` sprint speed field name via binary analysis (likely `SprintSpeed` or `MoveSpeed`)
2. Add `PanicSprintEnabled` config entry to `DreadConfig.cs`
3. Add panic sprint state vars to `TensionSystem` (`_panicActive`, `_panicTimer`, `_panicCooldown`, `_originalSprintSpeed`)
4. Detect sprint start in `Update()` — watch for stamina draining while `_nearestDist < 15f`
5. Apply 1.25x multiplier, start 2s timer
6. After timer expires, lerp speed back and start 20s cooldown

## Netcode

Per-client only. Speed is local — no sync needed.
