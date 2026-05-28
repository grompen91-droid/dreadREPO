# Harmony patches (apply / remove pattern)

How Dread applies Harmony patches at load time and toggles them from config. Fixes the old `PatchAll()` all-or-nothing approach (issue #107).

## Principles

1. **No `[HarmonyPatch]` attributes** on production patch classes in `MonsterOverhaulSystem.cs` (explicit `Apply` / `Remove` instead).
2. **Idempotent `Apply`:** guard with `if (_original != null) return;` before patching.
3. **`Remove` clears `_original`** so re-apply works after config toggle.
4. **Config-driven lifecycle** in `Plugin.cs` via `SettingChanged` handlers.
5. **Compatibility mode** skips monster gameplay patches; keeps ambient/tension where allowed.

## Patch inventory

| Patch class | Target | When applied | Host-only |
|-------------|--------|--------------|-----------|
| `EnemyNavMeshAgentAwakePatch` | `EnemyNavMeshAgent.Awake` | `MonsterAggressionEnabled` && !`CompatibilityMode` | Yes (postfix) |
| `EnemyDirectorSetInvestigatePatch` | `EnemyDirector.SetInvestigate` | Same | Yes (prefix, 1.5x radius) |
| `PlayerControllerAwakePatch` | `PlayerController.Awake` | `CrouchSpeedBoostEnabled` | No |
| `DebugConsoleGuardPatch` | (debug console guard) | `DebugConsoleGuardEnabled` | No |
| `RepoConfigSliderLabelCompat` | REPOConfig slider UI | After MenuLib loads | N/A |

Monster patches are grouped in `Plugin.ApplyMonsterPatches()`.

## Apply / Remove template

```csharp
internal static class ExamplePatch
{
    private static MethodInfo? _original;

    internal static void Apply(Harmony harmony)
    {
        if (_original != null) return;
        _original = AccessTools.Method(...);
        if (_original == null) { /* log warning; return */ }
        if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "Label"))
            return;
        harmony.Patch(_original, postfix: new HarmonyMethod(typeof(ExamplePatch), nameof(Postfix)));
    }

    internal static void Remove(Harmony harmony)
    {
        if (_original == null) return;
        harmony.Unpatch(_original, AccessTools.Method(typeof(ExamplePatch), nameof(Postfix)));
        _original = null;
    }
}
```

Use `AccessTools.TypeByName` when game types are not in stub assemblies.

## HarmonyPatchCompat

`Systems/HarmonyPatchCompat.cs`:

| Helper | Purpose |
|--------|---------|
| `IsMasterClient()` | No-op monster patches on clients |
| `ShouldSkipDueToForeignPatches(...)` | Skip if another mod already patched method and `CompatibilitySkipConflictingPatches` is true |

Priorities: `Priority.Last` on enemy speed postfix, `Priority.First` on investigate prefix (documented in code comments).

## Config toggles at runtime

`Plugin.Awake` wires:

- `MonsterAggressionEnabled` + `CompatibilityMode` -> `ApplyMonsterPatches()`
- `CrouchSpeedBoostEnabled` -> `PlayerControllerAwakePatch.Apply/Remove`
- `DebugConsoleGuardEnabled` -> `DebugConsoleGuardPatch.Apply/Remove`

Postfix/prefix bodies must re-check config flags (patch may stay applied while disabled).

## Adding a new patch

1. Add static patch class with `Apply` / `Remove` (no attributes).
2. Resolve `MethodInfo` with null checks and one-time warning logs.
3. Call from `Plugin` with config gate and `SettingChanged` if user-toggleable.
4. Use `HarmonyPatchCompat` for host-only and foreign-patch skip.
5. Document in an ADR if behavior is host-authoritative or compat-sensitive.

## Verify

Tier 0 build + grep CI. With debug server: `dread_verify` includes `harmony_patches` check when in level.

## Historical doc

Archive: `docs/agents/archive/superpowers/plans/2026-05-22-toggleable-harmony-patches.md`
