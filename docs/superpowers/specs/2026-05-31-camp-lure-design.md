# Camp Lure (anti-camping attraction) - Design

**Date:** 2026-05-31
**Feature branch:** `011-camp-lure`
**Status:** implemented (pending in-game verification)

## Goal

Stop a player from staying safe far from enemies while teammates take the heat.
The host watches who has been isolated too long and escalates enemy attention onto
them until they move. Works in solo as well (the lone player counts).

## Behavior

- **Host only.** Enemy AI is host-authoritative; the system no-ops on clients and
  under Compatibility mode (same gate as Monster Aggression).
- **Camp tracking.** Every second, for each player, compute distance to the nearest
  enemy via `ProximityScan.NearestDistance`. A player beyond `LureSafeDistance`
  (default 20 m) is *isolated*; their personal camp timer accumulates and resets the
  moment an enemy comes within that distance.
- **Target selection.** Among players whose camp timer is at least `LureCampSeconds`
  (default 90 s), the most isolated one becomes the lure target.
- **Escalation.** The pull starts gentle and grows: step `1 + (camp - threshold) / 30s`.
  Step drives the investigate radius (`25 m + 15 m per step`, capped at 90 m), so the
  longer they camp, the more / farther enemies are drawn. The lure is re-issued every
  4 s while active.
- **Reset.** Once an enemy reaches the target (nearest < safe distance), the timer and
  escalation reset. Scene changes clear all state.

## Mechanism

- The pull calls the game's own `EnemyDirector.SetInvestigate` at the target's
  position. This is the same method we already patch for the aggression radius, so
  behavior is consistent and there is no new networking or spawning.
- `EnemyLureCompat` resolves the method by reflection and fills arguments from the
  real parameter list (Vector3 to the position param, float to the radius param),
  adapting to signature differences and failing gracefully if absent.
- `PlayerRosterCompat` enumerates players by resolving the networked player type by
  name (`PlayerAvatar`, then `PlayerController`) and reading transform positions,
  falling back to the local player only if nothing resolves.

## Feedback

- **Normal play:** completely silent.
- **Debug (overlay enabled):** a log line plus a `DreadNotificationSystem` toast on
  arm / pull, and a live `Lure` row in the overlay Mod State section
  (`target  step N  camp Ns`) via `DreadRuntimeState`.

## Config (`2. Monster Overhaul`)

- `LureEnabled` (bool, default on)
- `LureSafeDistance` (float, 5-60, default 20)
- `LureCampSeconds` (float, 10-300, default 90)

## Components

- `Systems/CampLureSystem.cs` - the host-side state machine (registered as a Core host).
- `Systems/Core/PlayerRosterCompat.cs` - defensive player enumeration.
- `Systems/Core/EnemyLureCompat.cs` - defensive directed-investigate call.
- `DreadRuntimeState` lure fields + overlay `Lure` row.

## Risks / verification notes

- The two REPO seams (player enumeration and `SetInvestigate` arguments) are resolved
  by reflection because they cannot be verified against the real `Assembly-CSharp.dll`
  in this environment. They degrade gracefully (log + no-op) but the exact behavior
  must be confirmed in-game; verbose logs and the debug overlay row are there to make
  the first run observable.
- Manual verification only (constitution Principle II): solo camp test, multiplayer
  furthest-player test, escalation over time, reset on contact, host-only.
