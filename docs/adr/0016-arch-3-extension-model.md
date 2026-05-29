# ADR-0016: ARCH-3 extension model (system registry)

**Date:** 2026-05-30
**Status:** Accepted
**Issue:** [#175](https://github.com/grompen91-droid/dreadREPO/issues/175)

---

## Context

After ARCH-1 split `Plugin.cs` and `DreadSystemInitializer.cs`, every new runtime system still required editing the initializer's inline `TryAddSystem<>` list. That reintroduced god-file risk and made isolated init failures harder to review. Issue #175 asks for extension points, fail-safe init, and documented compat patterns without shipping ARCH-4's public mod API.

---

## Decision

### Boot order

1. **`Plugin.Awake`**: `DreadConfig.Initialize`, Harmony patch apply/remove (monster, crouch, debug console guard), config `SettingChanged` handlers, `SceneManager.sceneLoaded` subscription.
2. **`Plugin.Start`**: first `RepoConfigSliderLabelCompat.TryApply` (REPOConfig may load later).
3. **`SceneManager.sceneLoaded`**: `DreadSystemInitializer.TryInitialize()` until Unity UI is ready.
4. **`DreadSystemInitializer`**: iterate `DreadSystemRegistry.Registrations` (Core then Debug declaration order); per-row try/catch; then `RepoConfigSliderLabelCompat.TryApply` again.

Harmony patches are **not** registry rows (ADR-0009 lifecycle differs).

### System registry

| Component | Role |
|-----------|------|
| `Systems/DreadSystemRegistry.cs` | `SystemRegistration` list: id, type, host name, order group, optional `Func<bool>? IsEnabled` |
| `Systems/DreadSystemInitializer.cs` | UI defer gate, loop, `AddComponent`, summary log |

Contributors add systems per [specs/002-arch-3-extensible-core/contracts/system-lifecycle.md](../../specs/002-arch-3-extensible-core/contracts/system-lifecycle.md). **Do not** add `TryAddSystem` in `Plugin.cs`.

### Fail-safe init

Each spawn is wrapped in try/catch. One failure does not block others. Log includes `Systems initialized (N)` or `(N/M)` when partial.

### Compatibility (no new toggles)

ARCH-3 documents existing behavior:

- `DreadConfig.CompatibilityMode` (monster patches, tension mutations, psychotic break)
- `HarmonyPatchCompat.IsMasterClient()` (ADR-0004)
- `CompatibilitySkipConflictingPatches` (foreign patch skip)
- `RepoConfigSliderLabelCompat` soft dependency on REPOConfig (DBG-4 retained)

Matrix: [specs/002-arch-3-extensible-core/quickstart.md](../../specs/002-arch-3-extensible-core/quickstart.md), [docs/agents/guides/compatibility.md](../agents/guides/compatibility.md).

### Verification

`scripts/verify-dread.ps1` Tier 0 `arch3_try_add_system`: `TryAddSystem<` only in initializer/registry paths (registry may use `AddComponent(Type)` only).

### ARCH-4 boundary

Registry and contracts are **internal** to `Dread.dll`. Third-party mods must not depend on `DreadSystemRegistry` until ARCH-4 defines a semver public API.

---

## `DreadRuntimeState` (debug surface)

Supported fields for overlay and MCP (updated by gameplay systems, read-only for tools):

| Field | Meaning |
|-------|---------|
| `NearestEnemyDist` | Closest enemy distance (m) |
| `AdrenalineActive` / `PanicSprintActive` / `PanicSprintCooldown` | Tension sprint modifiers |
| `PsychoticBreak*` | Episode state, timers, threat/enemy counts, clip load |
| `AudioClipCount` / `AudioNextPlayIn` | Ambient audio scheduler |
| `DreadPatchCount` | Harmony patches owned by Dread (overlay refresh) |

Do not add new reflection in debug paths for ARCH-3; extend this type when overlay/MCP need new live values.

---

## Consequences

- **Positive:** One module to review for new systems; Tier 0 blocks stray spawns in `Plugin.cs`.
- **Positive:** ADR + spec contracts give agents a single narrative.
- **Negative:** Registry order is manual; reviewers must keep Core-before-Debug ordering.
- **Negative:** Optional `IsEnabled` predicates are available but debug hosts still spawn when disabled so config `SettingChanged` and F10 overlay wiring keep working (PERF-2).

---

## Related

- [extension-registry.md](../../specs/002-arch-3-extensible-core/contracts/extension-registry.md)
- [system-lifecycle.md](../../specs/002-arch-3-extensible-core/contracts/system-lifecycle.md)
- ADR-0004, ADR-0009, ARCH-2 reflection inventory
