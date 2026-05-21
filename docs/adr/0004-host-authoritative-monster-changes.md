# ADR-0004: Host-Authoritative Monster Changes With Client-Local Effects

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

R.E.P.O. uses Photon PUN for multiplayer. Enemy `NavMeshAgent` position, speed, and state are synced from the host to all clients. Dread needed to decide where to apply its monster modifications: on the host only, or on every client independently.

Applying speed/acceleration changes on every client would work for single-player but would cause desync in multiplayer: clients would compute different NavMeshAgent positions and Photon would fight itself trying to reconcile.

---

## Decision

Split features by network authority:

| Feature | Applied on | Rationale |
|---------|------------|-----------|
| Enemy speed, acceleration, detection radius | Host only | Host owns Photon NavMeshAgent sync |
| Enemy audio overhaul (pitch, reverb, spatial blend) | Every client | Audio is entirely client-local |
| All tension features (adrenaline, panic sprint, footsteps, breath) | Every client | Player-local state |
| Ambient audio | Every client | Fully client-side |
| Crouch speed boost | Every client | Player-local movement |

Host-only features check `SemiFunc.IsMasterClient()` or similar at the point of application. Client-local features always run.

---

## Consequences

- Players without Dread can join a Dread-hosted lobby and experience faster enemies (host applies patches, Photon syncs the result).
- No risk of Photon desync from conflicting speed values.
- Audio and tension features are per-player: one player's ambient whisper doesn't play for everyone.
- Host-only patches (speed, detection) use Harmony postfixes on `EnemyNavMeshAgent.Awake` and `EnemyDirector.SetInvestigate`, which naturally only run on the host if the game's own logic is host-authoritative.

---

## Rejected Alternatives

- **Apply all changes on all clients**: causes Photon position desync for enemies. Tested internally, enemies visibly teleported.
- **Sync modified speed via Photon RPC**: requires new networked custom types. Increases complexity and bandwidth. Not justified for a 1.2x multiplier.
