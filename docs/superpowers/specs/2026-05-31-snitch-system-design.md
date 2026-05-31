# Snitch System - Design

**Date:** 2026-05-31
**Feature branch:** `012-snitch-system`
**Status:** spec (pending implementation)

## Goal

One random item per run is secretly the snitch. The first player to pick it up
triggers a loud 3D bang audible to everyone in the lobby and marks that position
as a persistent POI for all enemies in the building for several minutes. No player
is ever told which item was the snitch or who triggered it.

## Behavior

- **Host only.** Enemy redirection is host-authoritative; the system no-ops on
  clients and under Compatibility mode (same gate as Camp Lure / Monster Aggression).
- **One snitch per run.** On scene load, `SnitchSystem` enumerates all interactable
  item GameObjects via `Object.FindObjectsOfType` (type resolved by name). One is
  chosen at random. A `SnitchItemMarker` MonoBehaviour is attached to it. If zero
  items are found the system skips silently — no snitch that run.
- **First pickup only.** `SnitchItemMarker` polls every 0.25 s for pickup. Once
  triggered it sets `_triggered = true` and never fires again for that run.
- **Pickup detection.** Three signals, any one sufficient:
  1. `Rigidbody.isKinematic == true` (item grabbed by player)
  2. `transform.parent != null` (item reparented to a hand/socket)
  3. Position delta > 0.5 m from spawn position
- **Bang audio.** Plays `snitch_bang.ogg` at the trigger position, 3D spatial
  (`spatialBlend = 1.0`), loaded via `AudioClipLoader`. Short — a few seconds, no loop.
  All players in the lobby hear it from the correct world direction.
- **POI loop.** Every 30 s `EnemyLureCompat.SetInvestigate` is issued at the trigger
  position with radius 60 m (building-wide), for `SnitchPOIDurationSeconds` total.
  Uses the same re-issue pattern as Camp Lure escalation.
- **State reset.** Scene transitions clear all state. `SnitchItemMarker` is destroyed
  with its GameObject.

## Mechanism

- `SnitchSystem` registers in `DreadSystemRegistry` after `MonsterOverhaulSystem`.
- Item type resolved by name (`PhysGrabObject` is the expected class name — verify
  against Assembly-CSharp.dll before implementing; fall back to a broader search if
  absent).
- `EnemyLureCompat.SetInvestigate` already exists (Camp Lure). Reused without change.
- Audio loaded and played via `AudioClipLoader` + `AudioPlayUtil`, same path as all
  other Dread audio.

## Configuration

Section: `2. Monster Overhaul` (same section as Camp Lure)

| Key | Default | Range | Description |
|-----|---------|-------|-------------|
| `SnitchEnabled` | `true` | on/off | Enable/disable the system |
| `SnitchPOIDurationSeconds` | `180` | 30–300 | Seconds enemies keep returning to the trigger position |

## Debug overlay

New `Snitch` row in the F10 overlay:
- Before trigger: `armed`
- After trigger: `triggered | POI Xs remaining`

## Audio asset

One new file required: `audio/snitch_bang.ogg`

**Spec:** loud single-hit impact, scary not comedic. Suggest a deep concussive bang
or heavy metal slam — something that reads as "something just went very wrong."
Duration 1–3 s. Mono or stereo both fine (Unity spatialises at runtime).
Freesound.org search terms: `bang impact`, `metal slam`, `heavy hit`, `explosion hit`.

## Netcode model

| Concern | Who handles it |
|---------|---------------|
| Item selection | Host only |
| Pickup detection | Host only (host has authoritative physics) |
| Enemy SetInvestigate | Host only |
| Bang audio | 3D spatial AudioSource at world position — Unity propagates to all clients naturally |

## Stubs required

- `Object.FindObjectsOfType(Type)` — already added for Camp Lure
- `Rigidbody.isKinematic` — likely needs adding to `UnityEngine_stubs.cs`
- `Transform.parent` — likely already present; verify before build
