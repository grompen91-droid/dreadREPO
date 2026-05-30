# Implementation Plan: Core deepening (005)

**Branch**: `005-core-deepening` | **Spec**: [spec.md](./spec.md)

## Phase summary

| Phase | Deliverable |
|-------|-------------|
| 1 | `ProximityScan` replaces `EnemyScanCache` |
| 2 | `HarmonyPatchRegistry` + `PatchLifecycle`; thin `Plugin.cs` |
| 3 | `PlayerInputLockCompat` |
| 4 | `SpatialAudio3D` |
| 5 | `GameplayContext` |
| 6 | `DreadFeaturePolicy` |
| 7 | `PlayerControllerCompat` stamina/sprint seams |
| 8 | Verify manifest + registry ADR note |

## Constitution

- Core namespace: `Dread.Systems.Core`
- No version string edits in manifest/Plugin/README
- Stub build required before PR
- ADR-0004 host gates unchanged inside patch postfixes
- ADR-0009 explicit apply/remove preserved via registry
