# ADR-0001: Remove REPOLib, HostOptionsSystem, EnvironmentalSystem, and VisualCorruptionSystem

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

Dread v1.0.0 through v1.3.x shipped with six systems. Three were removed entirely in v1.4.0.

### HostOptionsSystem

Networked host config sync (gamma, render resolution) via REPOLib. Required all clients to have REPOLib installed.

Problem: REPOLib's `BepInDependency` GUID was unstable across versions (`com.github.zehs.repolib`, `com.zehs.repolib`, `REPOLib`). Even after confirming the correct GUID, clients without REPOLib failed to load. The feature was low-value relative to the added hard dependency.

### EnvironmentalSystem and VisualCorruptionSystem

Attempted to patch Unity PostProcessing v2 volumes at runtime (chromatic aberration, vignette) to create visual distortion effects near enemies.

Problem: R.E.P.O. uses a non-standard PostProcessing setup. Standard `VolumeProfile` access patterns (`FindObjectOfType<Volume>()`, `profile.TryGet<ChromaticAberration>()`) do not work. Neither system ever produced visible output in-game.

---

## Decision

Remove all three systems. Drop REPOLib dependency entirely. Dread is now BepInEx-only.

---

## Consequences

- Dread no longer requires REPOLib. Any player can install it directly.
- Host Options (force gamma, force render size) are gone. No replacement planned.
- Visual corruption effects are gone. Atmospheric experience relies entirely on audio.
- The three remaining systems (AudioDread, MonsterOverhaul, Tension) become the mod's core.
- `PhotonUnityNetworking.dll` and `Photon3Unity3D.dll` csproj references are now only used for `Traverse` field access in TensionSystem.

---

## Rejected Alternatives

- **Keep HostOptionsSystem as optional**: would require conditional compile or runtime check for REPOLib. Complexity not worth the feature value.
- **Work around PostProcessing**: investigated `Traverse`-based access to R.E.P.O.'s PP internals. No stable field path found.
