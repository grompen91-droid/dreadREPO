# ADR-0001: Remove HostOptionsSystem, EnvironmentalSystem, and VisualCorruptionSystem in v1.4.0

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

Three systems were present in v1.0.0 through v1.3.x and removed entirely in v1.4.0.

### HostOptionsSystem

Networked host config sync (gamma, render size) via REPOLib. Required all clients to have REPOLib installed.

Problem: REPOLib's `BepInDependency` GUID was unstable across versions (`com.github.zehs.repolib`, `com.zehs.repolib`, `REPOLib`). Even after confirming the correct GUID, the networking layer caused load failures on clients without REPOLib. The feature was low-value relative to the added hard dependency.

### EnvironmentalSystem and VisualCorruptionSystem

Attempted to patch Unity PostProcessing v2 volumes at runtime (chromatic aberration, vignette) to create visual distortion effects near enemies.

Problem: R.E.P.O. uses a non-standard PostProcessing setup. Standard `VolumeProfile` access patterns (`FindObjectOfType<Volume>()`, `profile.TryGet<ChromaticAberration>()`) do not work. Neither system ever produced visible output in-game.

---

## Decision

Remove all three systems and make Dread dependency-free (BepInEx only).

---

## Consequences

- Dread no longer requires REPOLib. Players without REPOLib can install Dread freely.
- Host Options (force gamma, force render size) are gone. No replacement planned.
- Visual corruption effects are gone. Atmospheric experience relies entirely on audio.
- `PhotonUnityNetworking.dll` and `Photon3Unity3D.dll` references in `Dread.csproj` became orphaned. Both were removed in v1.4.2 (issue #7).

---

## Rejected Alternatives

- **Keep HostOptionsSystem as optional** -- would require conditional compile or runtime check for REPOLib presence. Complexity not worth the feature.
- **Find a workaround for PostProcessing** -- investigated `Traverse`-based access to PP internals. No stable field path found across R.E.P.O. versions.
