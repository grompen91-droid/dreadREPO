# Feature Specification: Core deepening (005)

**Feature Branch**: `005-core-deepening`

**Status**: In progress

**Input**: Eight architecture deepening opportunities from core review: proximity scan in Core, Harmony patch registry, player input lock compat, spatial audio helper, gameplay context gate, compatibility feature policy, extended PlayerControllerCompat, registry manifest hygiene.

## Phases

| Phase | Name | Priority |
|-------|------|----------|
| 1 | Proximity scan module in Core | Strong |
| 2 | Harmony patch registry | Strong |
| 3 | Player input lock compat | Strong |
| 4 | Spatial audio module | Worth exploring |
| 5 | Gameplay context gate | Worth exploring |
| 6 | Compatibility policy module | Worth exploring |
| 7 | Extend PlayerControllerCompat | Worth exploring |
| 8 | Registry manifest hygiene | Speculative |

## Robust core definition

Core may change when the **game** breaks (REPO updates). Normal features must not require ad-hoc core fixes unless they need a **new** Core adapter or seam.

## Requirements (summary)

- **FR-001**: Single `ProximityScan` in `Dread.Systems.Core`; no duplicate `FindObjectsOfType<EnemyHealth>` in features.
- **FR-002**: `HarmonyPatchRegistry` + `PatchLifecycle`; thin `Plugin.cs`; ADR-0009 toggle apply/remove preserved.
- **FR-003**: `PlayerInputLockCompat` replaces duplicate Traverse blocks in psychotic break and error prompt.
- **FR-004**: `SpatialAudio3D` helper for temp GO + rolloff + `AudioPlayUtil` lifetime.
- **FR-005**: `GameplayContext` for menu/in-level/run gates.
- **FR-006**: `DreadFeaturePolicy` for Compatibility mode; low stamina and fake footsteps respect compat.
- **FR-007**: Stamina/sprint multiplier seams on `PlayerControllerCompat`.
- **FR-008**: Tier 0 verify includes `ErrorReportingPromptSystem`; document debug `IsEnabled` choice per ADR-0016.
