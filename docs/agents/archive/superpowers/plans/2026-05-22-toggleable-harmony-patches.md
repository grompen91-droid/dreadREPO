# Toggleable Harmony Patches Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all three Harmony patches conditionally applied at load time and toggleable at runtime via config changes, instead of the current all-or-nothing `PatchAll()` approach.

**Architecture:** Replace `_harmony.PatchAll()` with explicit per-patch `Apply()/Remove()` pattern. Each static patch class exposes lifecycle methods. `Plugin.cs` calls them with config checks, and hooks `ConfigEntry.SettingChanged` for runtime toggling.

**Tech Stack:** C#, HarmonyLib, BepInEx ConfigEntry.SettingChanged

**Fixes:** Issue #107

---

## File Structure

```
Systems/MonsterOverhaulSystem.cs  — MODIFY: add Apply/Remove methods to each patch class, remove [HarmonyPatch] attributes
Plugin.cs                          — MODIFY: replace PatchAll with explicit patching, add SettingChanged handlers
```

---

### Task 1: Add Apply/Remove methods to patch classes in MonsterOverhaulSystem.cs

**Files:**
- Modify: `Systems/MonsterOverhaulSystem.cs:93-163`

**Changes to each patch:**

**EnemyNavMeshAgentAwakePatch (lines 93-117):**
- Remove `[HarmonyPatch(typeof(EnemyNavMeshAgent), "Awake")]` attribute
- Remove `[HarmonyPostfix]` attribute from Postfix method
- Add `private static MethodInfo? _original;` field
- Add `internal static void Apply(Harmony harmony)` method: uses `AccessTools.Method(typeof(EnemyNavMeshAgent), "Awake")` to get original, calls `harmony.Patch(original, postfix: new HarmonyMethod(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)))`, stores original in `_original`
- Add `internal static void Remove(Harmony harmony)` method: if `_original` is not null, calls `harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)))`, sets `_original = null`

**PlayerControllerAwakePatch (lines 123-145):**
- Same pattern: remove attributes, add `_original` field, `Apply()`, `Remove()`

**EnemyDirectorSetInvestigatePatch (lines 153-163):**
- Same pattern: remove attributes, add `_original` field, `Apply()`, `Remove()`

- [ ] **Step 1: Modify EnemyNavMeshAgentAwakePatch**

Replace the entire class definition:

```csharp
internal static class EnemyNavMeshAgentAwakePatch
{
    private static MethodInfo? _original;

    internal static void Apply(Harmony harmony)
    {
        _original = AccessTools.Method(typeof(EnemyNavMeshAgent), "Awake");
        harmony.Patch(_original, postfix: new HarmonyMethod(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
    }

    internal static void Remove(Harmony harmony)
    {
        if (_original == null) return;
        harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
        _original = null;
    }

    private static void Postfix(EnemyNavMeshAgent __instance)
    {
        if (!DreadConfig.MonsterAggressionEnabled.Value) return;

        var t = Traverse.Create(__instance);
        try
        {
            var agent = t.Field<NavMeshAgent>("Agent").Value;
            if (agent == null) return;

            agent.speed *= 1.2f;
            agent.acceleration *= 1.2f;
            t.Field<float>("DefaultSpeed").Value *= 1.2f;
            t.Field<float>("DefaultAcceleration").Value *= 1.2f;
        }
        catch
        {
        }
    }
}
```

Verify: `dotnet build` passes.

- [ ] **Step 2: Modify PlayerControllerAwakePatch**

Replace the entire class definition:

```csharp
internal static class PlayerControllerAwakePatch
{
    private static MethodInfo? _original;

    internal static void Apply(Harmony harmony)
    {
        _original = AccessTools.Method(typeof(PlayerController), "Awake");
        harmony.Patch(_original, postfix: new HarmonyMethod(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
    }

    internal static void Remove(Harmony harmony)
    {
        if (_original == null) return;
        harmony.Unpatch(_original, AccessTools.Method(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
        _original = null;
    }

    private static void Postfix(PlayerController __instance)
    {
        if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

        __instance.CrouchSpeed *= 1.3f;
        var t = Traverse.Create(__instance);
        try
        {
            float orig = t.Field<float>("playerOriginalCrouchSpeed").Value;
            if (orig > 0f)
                t.Field<float>("playerOriginalCrouchSpeed").Value = orig * 1.3f;
            else
                t.Field<float>("playerOriginalCrouchSpeed").Value = __instance.CrouchSpeed;
        }
        catch
        {
        }
    }
}
```

Verify: `dotnet build` passes.

- [ ] **Step 3: Modify EnemyDirectorSetInvestigatePatch**

Replace the entire class definition:

```csharp
internal static class EnemyDirectorSetInvestigatePatch
{
    private static MethodInfo? _original;

    internal static void Apply(Harmony harmony)
    {
        _original = AccessTools.Method(typeof(EnemyDirector), "SetInvestigate");
        harmony.Patch(_original, prefix: new HarmonyMethod(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix)));
    }

    internal static void Remove(Harmony harmony)
    {
        if (_original == null) return;
        harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix)));
        _original = null;
    }

    private static void Prefix(ref float radius)
    {
        if (!DreadConfig.MonsterAggressionEnabled.Value) return;
        if (radius < float.MaxValue)
            radius *= 1.5f;
    }
}
```

Verify: `dotnet build` passes.

- [ ] **Step 4: Commit**

```bash
git add Systems/MonsterOverhaulSystem.cs
git commit -m "refactor: split Harmony patches into Apply/Remove lifecycle methods"
```

Verify: `dotnet build` passes.

---

### Task 2: Replace PatchAll with explicit conditional patching in Plugin.cs

**Files:**
- Modify: `Plugin.cs:21-27` (Awake method)
- Modify: `Plugin.cs` (add using `System.Reflection`)

**Changes:**
- Replace `_harmony.PatchAll()` with explicit calls to each patch's `Apply()` prefixed with config checks
- Add `ConfigEntry.SettingChanged` handler to toggle patches at runtime

- [ ] **Step 1: Update Plugin.cs Awake method**

```csharp
private void Awake()
{
    Logger = base.Logger;
    DreadConfig.Initialize(Config);

    if (DreadConfig.MonsterAggressionEnabled.Value)
    {
        EnemyNavMeshAgentAwakePatch.Apply(_harmony);
        EnemyDirectorSetInvestigatePatch.Apply(_harmony);
    }
    if (DreadConfig.CrouchSpeedBoostEnabled.Value)
        PlayerControllerAwakePatch.Apply(_harmony);

    DreadConfig.MonsterAggressionEnabled.SettingChanged += (_, _) =>
    {
        if (DreadConfig.MonsterAggressionEnabled.Value)
        {
            EnemyNavMeshAgentAwakePatch.Apply(_harmony);
            EnemyDirectorSetInvestigatePatch.Apply(_harmony);
        }
        else
        {
            EnemyNavMeshAgentAwakePatch.Remove(_harmony);
            EnemyDirectorSetInvestigatePatch.Remove(_harmony);
        }
    };

    DreadConfig.CrouchSpeedBoostEnabled.SettingChanged += (_, _) =>
    {
        if (DreadConfig.CrouchSpeedBoostEnabled.Value)
            PlayerControllerAwakePatch.Apply(_harmony);
        else
            PlayerControllerAwakePatch.Remove(_harmony);
    };

    Logger.LogInfo($"{NAME} v{VERSION} loaded.");
}
```

Add `using System.Reflection;` to Plugin.cs imports (for `MethodInfo` used by patch classes).

- [ ] **Step 2: Build and verify**

Run: `dotnet build`
Expected: Build succeeds with no errors or warnings.

- [ ] **Step 3: Commit**

```bash
git add Plugin.cs
git commit -m "feat: toggleable Harmony patches with config-gated apply and runtime SettingChanged handlers"
```

---

### Task 3: Create ADR-0009 documenting the toggleable patches decision

**Files:**
- Create: `docs/adr/0009-toggleable-harmony-patches.md`

- [ ] **Step 1: Write ADR-0009**

```markdown
# ADR-0009: Toggleable Harmony Patches with Runtime Lifecycle Management

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

Issue #107 identified that all three Harmony patches were installed permanently via `_harmony.PatchAll()` in `Plugin.Awake()`. There was no mechanism to:

1. Conditionally skip installing a patch at load time based on config
2. Uninstall a patch at runtime when the user toggles a config option
3. Reinstall a patch at runtime when the user re-enables a config option

This meant patches were "all-or-nothing" and their effects could only be gated internally (via config checks inside the Postfix/Prefix method), leaving the IL permanently modified even when the feature was disabled.

---

## Decision

### 1. Replace PatchAll with Explicit Per-Patch Lifecycle

Each Harmony patch class exposes two static methods:

- `Apply(Harmony harmony)`: locates the target method via `AccessTools.Method` and calls `harmony.Patch()` with the appropriate prefix/postfix
- `Remove(Harmony harmony)`: calls `harmony.Unpatch()` to remove the specific patch from the target method

Each patch class stores a private `MethodInfo? _original` field to track whether the patch is currently applied.

### 2. Config-Gated Initial Application

`Plugin.Awake()` reads each config toggle at startup and only calls `Apply()` for patches whose feature is enabled. If a feature is disabled at startup, the patch is never installed — zero IL impact.

### 3. Runtime Toggle via SettingChanged Handler

Each config entry registers a `SettingChanged` handler. When the user changes the toggle in BepInEx config:

- Enabled -> Disabled: `Remove()` is called, the patch is uninstalled from the target method
- Disabled -> Enabled: `Apply()` is called, the patch is installed on the target method

This allows full lifecycle management without requiring a game restart.

---

## Consequences

- Players can toggle monster aggression, crouch speed boost, and investigate radius at runtime with immediate effect
- Zero IL impact when a feature is disabled at startup — no Harmony overhead for unused patches
- No `[HarmonyPatch]` or `[HarmonyPostfix]` attributes remain; all patching is explicit and self-documenting
- Slightly more verbose code per patch (Apply/Remove boilerplate) but each method is 4-6 lines
- `MonsterAggressionEnabled` controls two patches (EnemyNavMeshAgentAwake and EnemyDirectorSetInvestigate); both are applied/removed together
- If `AccessTools.Method` fails (e.g., the target class/method was removed in a game update), `Apply()` throws immediately at startup rather than failing silently in a Postfix — faster detection of compatibility issues

---

## Rejected Alternatives

- **Keep PatchAll with internal config guards**: patches would still be installed even when disabled, wasting CPU cycles on every enemy spawn
- **Separate Harmony instances per patch**: unnecessary complexity; a single Harmony instance with selective Patch/Unpatch is cleaner
- **HarmonyManipulation.PatchAll with filter delegate**: Harmony 2.x supports passing a predicate to `PatchAll()` to skip certain classes, but this doesn't support runtime toggling
- **Two MonoBehaviours (Patcher + Unpatcher)**: adds game-object lifecycle management overhead for what is essentially a Harmony configuration concern
```

- [ ] **Step 2: Commit**

```bash
git add docs/adr/0009-toggleable-harmony-patches.md
git commit -m "docs: ADR-0009 toggleable Harmony patches"
```

---

### Task 4: Update CHANGELOG.md

- [ ] **Step 1: Add entry under [Unreleased]**

Add to the `[Unreleased]` section:

```markdown
### Changed
- Harmony patches: split from `PatchAll()` into explicit per-patch lifecycle. Patches are now conditionally applied at startup based on config and can be toggled at runtime via BepInEx config UI. (Issue #107, ADR-0009)
```

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update changelog for toggleable Harmony patches"
```

---

### Task 5: Add using directive for System.Reflection in Plugin.cs

If not already present, add `using System.Reflection;` to Plugin.cs imports.

- [ ] **Step 1: Verify using directives in Plugin.cs**

Currently:
```csharp
using BepInEx;
using BepInEx.Logging;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;
using UnityEngine;
```

Needs `using System.Reflection;` added for `MethodInfo` type used by patch classes.

Add after `using HarmonyLib;`:
```csharp
using HarmonyLib;
using System.Reflection;
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Squash into previous commit or commit separately**

If combined with Task 2 changes, include in that commit. Otherwise commit separately.

---

## Verification

- [ ] `dotnet build` succeeds with no errors
- [ ] Config toggle at startup correctly skips patches for disabled features
- [ ] Config toggle at runtime correctly applies/removes patches via SettingChanged
- [ ] Monsters have normal speed/acceleration when MonsterAggressionEnabled is off at startup
- [ ] Toggling MonsterAggressionEnabled on mid-session correctly applies speed/acceleration boost to new enemy spawns
- [ ] Player crouch speed is unaffected when CrouchSpeedBoostEnabled is off
- [ ] No remaining `[HarmonyPatch]` or `[HarmonyPostfix]` attributes on any patch class
