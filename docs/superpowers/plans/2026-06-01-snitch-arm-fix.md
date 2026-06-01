# Snitch Arm Fix Implementation Plan

> **Superseded for ongoing work:** use [specs/006-lure-snitch-hardening/plan.md](../../specs/006-lure-snitch-hardening/plan.md) and [docs/agents/guides/camp-lure-and-snitch.md](../../docs/agents/guides/camp-lure-and-snitch.md). Do not add new docs under `docs/superpowers/`.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix [#222](https://github.com/grompen91-droid/dreadREPO/issues/222) so `SnitchSystem` reliably arms one item per extraction run after level generation, without the arm timer being reset by additive scene loads.

**Architecture:** Three coordinated fixes: (1) only call `ResetState()` on `LoadSceneMode.Single` so additive level-gen scenes do not restart the 5s arm countdown; (2) Harmony postfix on `SemiFunc.OnLevelGenDone` to force an arm attempt once valuables exist; (3) harden `ItemRosterCompat` to match `PlayerRosterCompat` (Component assignability check, `Assembly-CSharp` fallback, `FindObjectsOfType` with inactive objects). No new config keys.

**Tech Stack:** C# / .NET Framework 4.8, BepInEx 5, Harmony 2, Unity 2022.3 stubs, `dotnet build` for CI-style verification (no automated play-mode tests in this repo).

**Prerequisite:** Run in a dedicated git worktree (see `using-git-worktrees` / brainstorming skill) on branch `cursor/snitch-arm-fix-abb2` off `master`.

**Closes:** GitHub issue #222 (unblocks cleanup issue #223).

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `.github/scripts/Assembly-CSharp_stubs.cs` | Add `SemiFunc.OnLevelGenDone()` for patch target |
| Modify | `.github/scripts/UnityEngine_stubs.cs` | Add `FindObjectsOfType(Type, bool includeInactive)` overload |
| Modify | `Systems/Core/ItemRosterCompat.cs` | Safer type resolution + inactive object scan |
| Create | `Systems/Patches/SnitchLevelGenDonePatch.cs` | Postfix calls `SnitchSystem.NotifyLevelGenDone()` |
| Modify | `Systems/Core/HarmonyPatchRegistry.cs` | Register snitch level-gen patch |
| Modify | `Systems/SnitchSystem.cs` | Scene reset guard, static notify hook, arm scheduling |
| Modify | `CHANGELOG.md` | `[Unreleased]` Fixed entry |
| Modify | `docs/agents/guides/reflection-inventory.md` | Row for `SnitchLevelGenDonePatch` + `ItemRosterCompat` updates |

---

## Root Cause (context for implementer)

| Symptom | Cause in current code |
|---------|----------------------|
| `[Snitch] Arm attempt 1` never logged | `OnSceneLoaded` calls `ResetState()` on **every** load, including additive scenes during level gen, so `_armCountdown` never reaches 0 |
| Snitch never arms even when gates pass | Arm window can expire before `OnLevelGenDone`; `ItemRosterCompat` may resolve types without verifying `Component` or miss inactive spawned valuables |

---

### Task 1: Extend stubs for level-gen hook and inactive FindObjectsOfType

**Files:**
- Modify: `.github/scripts/Assembly-CSharp_stubs.cs`
- Modify: `.github/scripts/UnityEngine_stubs.cs`

- [ ] **Step 1: Add `OnLevelGenDone` to `SemiFunc` stub**

In `.github/scripts/Assembly-CSharp_stubs.cs`, change the `SemiFunc` block from:

```csharp
public static class SemiFunc
{
    public static bool MenuLevel() => false;
    public static bool IsMasterClient() => false;
}
```

to:

```csharp
public static class SemiFunc
{
    public static bool MenuLevel() => false;
    public static bool IsMasterClient() => false;
    public static void OnLevelGenDone() { }
}
```

- [ ] **Step 2: Add `FindObjectsOfType` overload with `includeInactive`**

In `.github/scripts/UnityEngine_stubs.cs`, inside `public class Object`, after the existing line:

```csharp
        public static Object[] FindObjectsOfType(System.Type type) => System.Array.Empty<Object>();
```

add:

```csharp
        public static Object[] FindObjectsOfType(System.Type type, bool includeInactive) =>
            FindObjectsOfType(type);
```

- [ ] **Step 3: Regenerate stub refs and verify build**

```bash
cd /workspace
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Expected last lines include: `Build succeeded.` and `0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add .github/scripts/Assembly-CSharp_stubs.cs .github/scripts/UnityEngine_stubs.cs .github/stubs/
git commit -m "stub: SemiFunc.OnLevelGenDone and FindObjectsOfType includeInactive"
```

---

### Task 2: Harden `ItemRosterCompat`

**Files:**
- Modify: `Systems/Core/ItemRosterCompat.cs`

- [ ] **Step 1: Replace entire `ItemRosterCompat.cs` with this implementation**

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Defensive enumeration of all interactable item GameObjects.
    /// The item type is resolved by name so stub builds stay clean and
    /// the lookup degrades gracefully (no items returned) if the game's
    /// type names change. Never throws into callers.
    /// </summary>
    internal static class ItemRosterCompat
    {
        private static readonly string[] ItemTypeNames =
            { "ValuableObject", "PhysGrabObject", "ItemPickup", "Valuable" };

        private static Type? _itemType;
        private static bool _resolved;
        private static bool _loggedError;

        public static List<GameObject> GetItemGameObjects()
        {
            var result = new List<GameObject>();
            try
            {
                ResolveItemType();
                if (_itemType == null)
                    return result;

                CollectInstances(result);
            }
            catch (Exception ex)
            {
                LogErrorOnce("GetItemGameObjects failed", ex);
            }

            return result;
        }

        private static void CollectInstances(List<GameObject> result)
        {
            Object[] objects;
            try
            {
                objects = UnityEngine.Object.FindObjectsOfType(_itemType!, true);
            }
            catch (MissingMethodException)
            {
                objects = UnityEngine.Object.FindObjectsOfType(_itemType!);
            }

            foreach (var o in objects)
            {
                if (o is Component c && (object)c != null)
                    result.Add(c.gameObject);
            }
        }

        private static void ResolveItemType()
        {
            if (_resolved)
                return;

            _resolved = true;

            foreach (var name in ItemTypeNames)
            {
                var t = ResolveTypeByName(name);
                if (t != null)
                {
                    _itemType = t;
                    LoggingService.LogVerbose($"[Dread] ItemRosterCompat: resolved item type as '{name}'");
                    return;
                }
            }

            LoggingService.LogWarning(
                "[Dread] ItemRosterCompat: no item type found "
                + "(tried ValuableObject, PhysGrabObject, ItemPickup, Valuable); snitch will be disabled");
        }

        private static Type? ResolveTypeByName(string name)
        {
            try
            {
                var t = AccessTools.TypeByName(name);
                if (IsItemComponentType(t))
                    return t;
            }
            catch
            {
                // fall through to assembly scan
            }

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var asmName = asm.GetName().Name;
                    if (asmName == null
                        || !asmName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fromAsm = asm.GetType(name, throwOnError: false, ignoreCase: false);
                    if (IsItemComponentType(fromAsm))
                        return fromAsm;
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce($"type scan for '{name}' failed", ex);
            }

            return null;
        }

        private static bool IsItemComponentType(Type? t) =>
            t != null && typeof(Component).IsAssignableFrom(t);

        private static void LogErrorOnce(string context, Exception ex)
        {
            if (_loggedError)
                return;
            _loggedError = true;
            LoggingService.LogWarning(
                $"[Dread] ItemRosterCompat: {context}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add Systems/Core/ItemRosterCompat.cs
git commit -m "fix(core): harden ItemRosterCompat type resolution and inactive scan"
```

---

### Task 3: Add `SnitchLevelGenDonePatch` and register it

**Files:**
- Create: `Systems/Patches/SnitchLevelGenDonePatch.cs`
- Modify: `Systems/Core/HarmonyPatchRegistry.cs`

- [ ] **Step 1: Create `Systems/Patches/SnitchLevelGenDonePatch.cs`**

```csharp
using System.Reflection;
using Dread.Config;
using Dread.Systems.Core;
using HarmonyLib;

namespace Dread.Systems
{
    /// <summary>
    /// After R.E.P.O. finishes level generation, nudge SnitchSystem to arm
    /// (valuables are spawned). Host-only; no-op when snitch disabled.
    /// </summary>
    internal static class SnitchLevelGenDonePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null)
                return;

            _original = AccessTools.Method(typeof(SemiFunc), "OnLevelGenDone");
            if (_original == null)
            {
                LoggingService.LogWarning(
                    "[Dread] SemiFunc.OnLevelGenDone not found; snitch level-gen hook skipped");
                return;
            }

            if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "SemiFunc.OnLevelGenDone"))
                return;

            var patch = new HarmonyMethod(typeof(SnitchLevelGenDonePatch), nameof(Postfix));
            harmony.Patch(_original, postfix: patch);
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null)
                return;

            harmony.Unpatch(_original, AccessTools.Method(typeof(SnitchLevelGenDonePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix()
        {
            if (!DreadConfig.SnitchEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            SnitchSystem.NotifyLevelGenDone();
        }
    }
}
```

- [ ] **Step 2: Register patch in `HarmonyPatchRegistry.cs`**

Add this registration after the `"debug-console-guard"` entry (before the closing `};` of the list):

```csharp
                new(
                    "snitch-level-gen-done",
                    PatchGroup.Monster,
                    SnitchLevelGenDonePatch.Apply,
                    SnitchLevelGenDonePatch.Remove,
                    () => DreadConfig.SnitchEnabled.Value && !DreadConfig.CompatibilityMode.Value),
```

- [ ] **Step 3: Verify build**

```bash
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add Systems/Patches/SnitchLevelGenDonePatch.cs Systems/Core/HarmonyPatchRegistry.cs
git commit -m "fix(snitch): hook SemiFunc.OnLevelGenDone to trigger arm attempt"
```

---

### Task 4: Fix `SnitchSystem` scene reset and arm scheduling

**Files:**
- Modify: `Systems/SnitchSystem.cs`

- [ ] **Step 1: Add static instance hook and `NotifyLevelGenDone`**

After the opening brace of `public class SnitchSystem : MonoBehaviour`, add:

```csharp
        private static SnitchSystem? _instance;
```

After `private SnitchItemMarker? _activeMarker;`, add:

```csharp
        private void OnEnable() => _instance = this;

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        internal static void NotifyLevelGenDone() => _instance?.OnLevelGenComplete();
```

- [ ] **Step 2: Add `OnLevelGenComplete` and change `OnSceneLoaded`**

Replace:

```csharp
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetState();
```

with:

```csharp
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Additive loads during level generation must not reset the arm timer.
            if (mode != LoadSceneMode.Single)
                return;

            ResetState();
        }

        private void OnLevelGenComplete()
        {
            if (_armed || _triggered)
                return;

            _armCountdown = 0f;
            LoggingService.LogVerbose("[Snitch] Level gen done; arm attempt scheduled");
        }
```

- [ ] **Step 3: Keep diagnostic log on every arm attempt (not only retry 0)**

In `TryArm()`, replace:

```csharp
            if (_armRetries == 0)
                LoggingService.LogWarning($"[Snitch] Arm attempt 1: {items.Count} item(s) found");
```

with:

```csharp
            LoggingService.LogWarning(
                $"[Snitch] Arm attempt {_armRetries + 1}: {items.Count} item(s) found");
```

This makes issue #222 diagnosable even when retries happen after level gen.

- [ ] **Step 4: Verify build**

```bash
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Expected: `Build succeeded.`

- [ ] **Step 5: Run format check (matches CI)**

```bash
dotnet format --verify-no-changes --no-restore
```

Expected: exit code 0 with no diff.

- [ ] **Step 6: Commit**

```bash
git add Systems/SnitchSystem.cs
git commit -m "fix(snitch): stop additive scene loads from resetting arm timer"
```

---

### Task 5: Documentation and changelog

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `docs/agents/guides/reflection-inventory.md`

- [ ] **Step 1: Add `[Unreleased]` Fixed entry in `CHANGELOG.md`**

Under `## [Unreleased]`, add a `### Fixed` section (or append to existing Fixed) with:

```markdown
### Fixed
- **Snitch:** arm timer no longer resets on additive scene loads during level generation; arm attempt also runs after `SemiFunc.OnLevelGenDone` ([#222](https://github.com/grompen91-droid/dreadREPO/issues/222))
- **Snitch:** `ItemRosterCompat` validates resolved types as `Component`, scans `Assembly-CSharp` when `TypeByName` fails, and includes inactive valuables in `FindObjectsOfType`
```

(Do not use em dash in the file; use comma or rewrite if needed.)

- [ ] **Step 2: Add reflection inventory rows**

In `docs/agents/guides/reflection-inventory.md`, add to the table:

| `snitch-level-gen-done` | `Systems/Patches/SnitchLevelGenDonePatch.cs` | `Apply` / `Postfix` | event (`OnLevelGenDone`) | snitch enabled + !compat | required | required | **keep** | `SemiFunc.OnLevelGenDone` stubbed; notifies `SnitchSystem` |
| `item-roster-compat` | `Systems/Core/ItemRosterCompat.cs` | `ResolveTypeByName` / `GetItemGameObjects` | on arm | none | required | required | **keep** | `TypeByName` + `Assembly-CSharp` scan; inactive `FindObjectsOfType` |

Update **Last reviewed** date to `2026-06-01`.

Add `ValuableObject`, `PhysGrabObject` to the "Types **not** in stubs" bullet list.

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md docs/agents/guides/reflection-inventory.md
git commit -m "docs: changelog and reflection inventory for snitch arm fix"
```

---

### Task 6: Manual verification (host, in-game)

**Files:** none (BepInEx log + debug overlay)

- [ ] **Step 1: Deploy build to game profile**

From project root on a Windows machine with R.E.P.O. installed:

```powershell
dotnet build Dread.csproj -c Release -p:DeployToProfile=true
```

Or copy `bin/Release/net48/Dread.dll` and `audio/snitch_bang.ogg` into the BepInEx plugins folder manually.

- [ ] **Step 2: Enter extraction level as host**

1. Enable debug overlay in config if needed (F9/F10 per README).
2. Start a run on a level that spawns valuables (museum, manor, etc.).
3. Open `BepInEx/LogOutput.log` and confirm within ~40s of level load:

```
[Warning: Dread] [Snitch] Gates check: enabled=True compat=False isMaster=True
[Warning: Dread] [Snitch] Arm attempt 1: N item(s) found
```

where `N > 0`.

4. Overlay `Snitch` row should change from `disarmed  check Xs` to `armed  X.Xm`.

- [ ] **Step 3: Trigger snitch**

Pick up items until bang plays and overlay shows `triggered  POI Xs`.

- [ ] **Step 4: Scene reset check**

Return to lobby/truck (single scene load). Overlay should return to `disarmed`. Enter another level: arm attempt should run again.

- [ ] **Step 5: Note results on GitHub issue #222**

Comment with mod version, level name, `N` from arm attempt log, and pass/fail for arm + trigger.

---

## Self-Review (plan author checklist)

| #222 requirement | Task |
|------------------|------|
| Arm attempt runs after level load | Task 3 (`OnLevelGenDone`), Task 4 (timer not reset) |
| Correct item type enumeration | Task 2 (`ItemRosterCompat`) |
| Diagnostic visibility | Task 4 (log every attempt) |
| Related PR #221 behavior preserved | No config/API changes; same `TryArm` / `SnitchItemMarker` flow |

No placeholder steps. Build commands included. Types/methods consistent across tasks (`NotifyLevelGenDone`, `OnLevelGenComplete`, `SnitchLevelGenDonePatch`).

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-01-snitch-arm-fix.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** - Dispatch a fresh subagent per task, review between tasks, fast iteration. REQUIRED SUB-SKILL: `subagent-driven-development`.

2. **Inline Execution** - Run tasks in this session with `executing-plans`, batch execution with checkpoints. REQUIRED SUB-SKILL: `executing-plans`.

Which approach do you want?
