# Snitch System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One random item per run is secretly the snitch; the first player to pick it up plays a 3D bang and turns the pickup position into a 3-minute enemy POI.

**Architecture:** `SnitchSystem` (registered in `DreadSystemRegistry`, host-only) arms on run start by attaching a `SnitchItemMarker` MonoBehaviour to one random item. `SnitchItemMarker` polls for pickup every 0.25 s; on first pickup it calls back to `SnitchSystem.OnSnitchTriggered`, which plays `snitch_bang.ogg` at the position via `SpatialAudio3D` and starts a 30-second re-issue loop using the existing `EnemyLureCompat.Pull`. Item type is found by a new `ItemRosterCompat` seam (reflection by name, degrades gracefully to no-snitch-this-run if type not found).

**Tech Stack:** C# / .NET 4.8, BepInEx 5, Harmony 2, NVorbis, xUnit (build verification only — no unit-test surface on MonoBehaviour systems)

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Systems/SnitchSystem.cs` | `SnitchSystem` lifecycle + POI loop; `SnitchItemMarker` pickup poller |
| Create | `Systems/Core/ItemRosterCompat.cs` | Reflection seam: enumerate item GameObjects by type name |
| Modify | `Config/DreadConfig.cs` | Add `SnitchEnabled`, `SnitchPOIDurationSeconds`; register in null-check list |
| Modify | `Systems/DreadSystemRegistry.cs` | Register `SnitchSystem` after `camp-lure` |
| Modify | `Systems/DreadRuntimeState.cs` | Add `SnitchState`, `SnitchPoiRemaining` |
| Modify | `Systems/DebugOverlay/DebugOverlayPanel.cs` | Add `Snitch` row after `Lure` row |
| Modify | `.github/scripts/UnityEngine_stubs.cs` | Add `Rigidbody` stub with `isKinematic` |

---

## Task 1: Add `Rigidbody.isKinematic` stub

`SnitchItemMarker` reads `Rigidbody.isKinematic`. It is not in the stubs — the build will fail without it.

**Files:**
- Modify: `.github/scripts/UnityEngine_stubs.cs`

- [ ] **Step 1: Open the stubs file and find the `Transform` class block**

Read `.github/scripts/UnityEngine_stubs.cs` and find the line that contains `public class Transform : Component`. The new `Rigidbody` stub goes after the `Transform` block.

- [ ] **Step 2: Add the Rigidbody stub**

Find this block in `.github/scripts/UnityEngine_stubs.cs`:
```csharp
    public class Transform : Component
```

After the closing `}` of the `Transform` class, add:
```csharp
    public class Rigidbody : Component
    {
        public bool isKinematic { get; set; }
        public Vector3 velocity { get; set; }
    }
```

- [ ] **Step 3: Verify build passes**

```powershell
cd C:\Users\kaspe\Downloads\dreadrepo
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 4: Commit**

```powershell
git add .github/scripts/UnityEngine_stubs.cs
git commit -m "stub: add Rigidbody.isKinematic for snitch pickup detection"
```

---

## Task 2: Add config entries

**Files:**
- Modify: `Config/DreadConfig.cs`

- [ ] **Step 1: Add field declarations**

In `Config/DreadConfig.cs`, after the `LureEscalateSeconds` field declaration (around line 20), add:
```csharp
        public static ConfigEntry<bool> SnitchEnabled = null!;
        public static ConfigEntry<float> SnitchPOIDurationSeconds = null!;
```

- [ ] **Step 2: Add Bind calls**

After the `LureEscalateSeconds` Bind block (after line 97, before the `FakeFootstepsEnabled` Bind), add:
```csharp
            SnitchEnabled = cfg.Bind("2. Monster Overhaul", "SnitchEnabled", true,
                "One random item per run is the snitch. Picking it up first triggers a loud bang and draws all enemies to that spot. HOST ONLY.");
            SnitchPOIDurationSeconds = cfg.Bind("2. Monster Overhaul", "SnitchPOIDurationSeconds", 180f,
                new ConfigDescription(
                    "Seconds enemies keep returning to the snitch pickup position.",
                    new AcceptableValueRange<float>(30f, 300f)));
```

- [ ] **Step 3: Register in null-check list**

In the `allFields` array (around line 191), add `SnitchEnabled, SnitchPOIDurationSeconds,` after `MonsterAudioEnabled`:
```csharp
                MonsterAggressionEnabled, MonsterAudioEnabled, SnitchEnabled, SnitchPOIDurationSeconds,
```

- [ ] **Step 4: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 5: Commit**

```powershell
git add Config/DreadConfig.cs
git commit -m "config: add SnitchEnabled and SnitchPOIDurationSeconds"
```

---

## Task 3: Add runtime state fields

**Files:**
- Modify: `Systems/DreadRuntimeState.cs`

- [ ] **Step 1: Add snitch state properties**

In `Systems/DreadRuntimeState.cs`, after the `LurePullStep` property, add:
```csharp
        /// <summary>Current snitch state: "disarmed", "armed", or "triggered".</summary>
        public static string SnitchState { get; internal set; } = "disarmed";
        /// <summary>Seconds remaining on the snitch POI loop (0 = inactive).</summary>
        public static float SnitchPoiRemaining { get; internal set; }
```

- [ ] **Step 2: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add Systems/DreadRuntimeState.cs
git commit -m "state: add SnitchState and SnitchPoiRemaining runtime fields"
```

---

## Task 4: Create `ItemRosterCompat`

Mirrors `PlayerRosterCompat` — resolves item type by name via reflection, enumerates item GameObjects. Degrades gracefully if type not found.

**Files:**
- Create: `Systems/Core/ItemRosterCompat.cs`

- [ ] **Step 1: Create the file**

Create `Systems/Core/ItemRosterCompat.cs`:
```csharp
using System;
using System.Collections.Generic;
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
        private static readonly string[] ItemTypeNames = { "PhysGrabObject", "ValuableObject", "ItemPickup" };

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

                foreach (var o in UnityEngine.Object.FindObjectsOfType(_itemType))
                {
                    if (o is Component c && (object)c != null)
                        result.Add(c.gameObject);
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce("GetItemGameObjects failed", ex);
            }
            return result;
        }

        private static void ResolveItemType()
        {
            if (_resolved)
                return;

            _resolved = true;
            foreach (var name in ItemTypeNames)
            {
                _itemType = AccessTools.TypeByName(name);
                if (_itemType != null)
                {
                    LoggingService.LogVerbose($"[Dread] ItemRosterCompat: resolved item type as '{name}'");
                    return;
                }
            }

            LoggingService.LogWarning("[Dread] ItemRosterCompat: no item type found (tried PhysGrabObject, ValuableObject, ItemPickup); snitch will be disabled");
        }

        private static void LogErrorOnce(string context, Exception ex)
        {
            if (_loggedError)
                return;
            _loggedError = true;
            LoggingService.LogWarning($"[Dread] ItemRosterCompat: {context}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add Systems/Core/ItemRosterCompat.cs
git commit -m "feat(core): ItemRosterCompat reflection seam for item enumeration"
```

---

## Task 5: Create `SnitchSystem` and `SnitchItemMarker`

**Files:**
- Create: `Systems/SnitchSystem.cs`

- [ ] **Step 1: Create the file**

Create `Systems/SnitchSystem.cs`:
```csharp
using System.Collections;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    /// <summary>
    /// Arms one random item per run as the "snitch". The first player to
    /// pick it up triggers a loud 3D bang and marks the position as a
    /// persistent enemy POI for SnitchPOIDurationSeconds. Host only.
    /// Silent in normal play; surfaces overlay state and a toast when the
    /// debug overlay is enabled.
    /// </summary>
    public class SnitchSystem : MonoBehaviour
    {
        private const float PoiReissueInterval = 30f;
        private const float PoiRadius = 60f;
        private const float ArmDelaySeconds = 2f;

        private AudioClip? _bangClip;

        private bool _armed;
        private float _armCountdown = ArmDelaySeconds;

        private bool _triggered;
        private Vector3 _triggerPos;
        private float _poiRemaining;
        private float _nextReissue;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(AudioClipLoader.LoadClip("snitch_bang.ogg", clip =>
            {
                _bangClip = clip;
                if (clip == null)
                    LoggingService.LogWarning("[Snitch] snitch_bang.ogg not found — bang audio will be silent");
            }));
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetState();

        private void Update()
        {
            if (!DreadConfig.SnitchEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!GameplayContext.IsRun())
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            if (!_armed)
            {
                _armCountdown -= Time.deltaTime;
                if (_armCountdown <= 0f)
                {
                    _armed = true;
                    ArmSnitch();
                }
                return;
            }

            if (_triggered && _poiRemaining > 0f)
            {
                _poiRemaining -= Time.deltaTime;
                float remaining = Mathf.Max(0f, _poiRemaining);
                DreadRuntimeState.SnitchPoiRemaining = remaining;

                if (remaining > 0f && Time.time >= _nextReissue)
                {
                    _nextReissue = Time.time + PoiReissueInterval;
                    EnemyLureCompat.Pull(_triggerPos, PoiRadius);
                }
            }
        }

        private void ArmSnitch()
        {
            var items = ItemRosterCompat.GetItemGameObjects();
            if (items.Count == 0)
            {
                LoggingService.LogVerbose("[Snitch] No items found this run; skipping");
                DreadRuntimeState.SnitchState = "disarmed";
                return;
            }

            var chosen = items[UnityEngine.Random.Range(0, items.Count)];
            var marker = chosen.AddComponent<SnitchItemMarker>();
            marker.System = this;

            DreadRuntimeState.SnitchState = "armed";
            LoggingService.LogVerbose($"[Snitch] Armed on {chosen.name} (id {chosen.GetInstanceID()})");
        }

        internal void OnSnitchTriggered(Vector3 position)
        {
            if (_triggered)
                return;

            _triggered = true;
            _triggerPos = position;
            _poiRemaining = DreadConfig.SnitchPOIDurationSeconds.Value;
            _nextReissue = Time.time; // pull immediately on trigger

            DreadRuntimeState.SnitchState = "triggered";
            DreadRuntimeState.SnitchPoiRemaining = _poiRemaining;

            if (_bangClip != null)
            {
                SpatialAudio3D.PlayAt(position, _bangClip, new SpatialAudio3D.PlayOptions
                {
                    Volume = 1f,
                    MinDistance = 5f,
                    MaxDistance = 80f,
                    Pitch = 1f,
                    PaddingSeconds = 0.5f,
                    HostName = "DreadSnitchBang",
                });
            }

            if (DreadConfig.DebugOverlayEnabled.Value)
            {
                LoggingService.LogInfo("[Snitch] Triggered");
                DreadNotificationSystem.Bad("Snitch", "item betrayed a player");
            }
        }

        private void ResetState()
        {
            _armed = false;
            _armCountdown = ArmDelaySeconds;
            _triggered = false;
            _poiRemaining = 0f;
            _nextReissue = 0f;
            DreadRuntimeState.SnitchState = "disarmed";
            DreadRuntimeState.SnitchPoiRemaining = 0f;
        }
    }

    /// <summary>
    /// Polls every 0.25 s to detect when its item was picked up, then
    /// calls back to <see cref="SnitchSystem.OnSnitchTriggered"/>.
    /// Three signals: Rigidbody.isKinematic, transform.parent != null,
    /// or position delta > 0.5 m from spawn.
    /// </summary>
    internal class SnitchItemMarker : MonoBehaviour
    {
        internal SnitchSystem? System;

        private Vector3 _spawnPos;
        private Rigidbody? _rb;
        private bool _triggered;

        private void Start()
        {
            _spawnPos = transform.position;
            _rb = GetComponent<Rigidbody>();
            StartCoroutine(PollPickup());
        }

        private IEnumerator PollPickup()
        {
            var wait = new WaitForSeconds(0.25f);
            while (!_triggered)
            {
                yield return wait;
                if (IsPickedUp())
                    Trigger();
            }
        }

        private bool IsPickedUp()
        {
            try
            {
                if (_rb != null && _rb.isKinematic)
                    return true;
                if (transform.parent != null)
                    return true;
                if ((transform.position - _spawnPos).sqrMagnitude > 0.25f) // (0.5m)^2
                    return true;
            }
            catch
            {
                // reflection failure or Unity stub quirk — treat as not picked up
            }
            return false;
        }

        private void Trigger()
        {
            _triggered = true;
            StopAllCoroutines();
            System?.OnSnitchTriggered(transform.position);
        }
    }
}
```

- [ ] **Step 2: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 8
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add Systems/SnitchSystem.cs
git commit -m "feat(monster): SnitchSystem + SnitchItemMarker (snitch item per run)"
```

---

## Task 6: Register `SnitchSystem`

**Files:**
- Modify: `Systems/DreadSystemRegistry.cs`

- [ ] **Step 1: Add registration**

In `Systems/DreadSystemRegistry.cs`, after the `camp-lure` registration block, add:
```csharp
            new SystemRegistration(
                "snitch",
                typeof(SnitchSystem),
                "DreadSnitchHost",
                SystemOrderGroup.Core),
```

- [ ] **Step 2: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add Systems/DreadSystemRegistry.cs
git commit -m "feat(core): register SnitchSystem in DreadSystemRegistry"
```

---

## Task 7: Add `Snitch` row to debug overlay

**Files:**
- Modify: `Systems/DebugOverlay/DebugOverlayPanel.cs`

- [ ] **Step 1: Add the row in `BuildRows`**

In `Systems/DebugOverlay/DebugOverlayPanel.cs`, find the line:
```csharp
            AddRow("Lure", LureSummary(), LureColor());
```

After it, add:
```csharp
            AddRow("Snitch", SnitchSummary(), SnitchColor());
```

- [ ] **Step 2: Add summary and color methods**

Find the `LureColor()` method at the bottom of the file. After its closing `}`, add:
```csharp
        private static string SnitchSummary()
        {
            var state = DreadRuntimeState.SnitchState;
            if (state == "triggered")
                return $"triggered  POI {DreadRuntimeState.SnitchPoiRemaining:F0}s";
            return state;
        }

        private static Color SnitchColor()
            => DreadRuntimeState.SnitchState == "triggered" ? ColBad : ColDim;
```

- [ ] **Step 3: Build check**

```powershell
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 5
```

Expected: `Build succeeded.` with `0 Error(s)`

- [ ] **Step 4: Commit**

```powershell
git add Systems/DebugOverlay/DebugOverlayPanel.cs
git commit -m "feat(ui): add Snitch row to debug overlay"
```

---

## Task 8: Final build and dist

- [ ] **Step 1: Full clean build**

```powershell
cd C:\Users\kaspe\Downloads\dreadrepo
$refsDir = ".github/stubs/refs"
dotnet build Dread.csproj -c Release --nologo -p:GameDir="$refsDir" -p:BepInExDir="$refsDir" 2>&1 | Select-Object -Last 10
```

Expected: `Build succeeded.` with `0 Error(s)`, followed by:
```
[Dread] Deployed to dist and re-zipped
```

- [ ] **Step 2: Verify dist output**

```powershell
Get-ChildItem C:\Users\kaspe\Downloads\dreadrepo\dist\ -Recurse | Select-Object Name, Length
```

Expected: `Dread.dll` present and `elytraking-Dread-1.6.1.zip` updated (modified timestamp = now).

- [ ] **Step 3: Update CHANGELOG**

In `CHANGELOG.md`, under `## [Unreleased]` → `### Added`, add:
```markdown
- **Snitch System:** one random item per run is secretly the snitch; the first player to pick it up triggers a loud 3D bang (`snitch_bang.ogg`) and draws all enemies to that position for `SnitchPOIDurationSeconds` (default 3 min, re-issued every 30 s via `EnemyLureCompat`). Host only; disabled under Compatibility mode. Config under `2. Monster Overhaul` (`SnitchEnabled`, `SnitchPOIDurationSeconds`). Debug overlay shows `Snitch` row with POI countdown. New `ItemRosterCompat` reflection seam enumerates item GameObjects by type name.
```

- [ ] **Step 4: Commit changelog**

```powershell
git add CHANGELOG.md
git commit -m "docs: changelog entry for snitch system"
```

---

## In-game verification checklist

These cannot be verified against stubs — test in a live R.E.P.O. run:

- [ ] Pick up any item in a run — one of them plays the bang sound and enemies converge
- [ ] After trigger, enemies keep returning to the pickup spot for ~3 minutes
- [ ] Picking up other items after the snitch fired does nothing
- [ ] Scene transition (new floor) resets state; a new snitch is selected next run
- [ ] F10 overlay shows `Snitch: armed` before trigger, `Snitch: triggered POI Xs` after
- [ ] `SnitchEnabled = false` in config disables the system entirely
- [ ] Works in solo (host = local player)

---

## Notes

- **Item type name:** `ItemRosterCompat` tries `PhysGrabObject`, `ValuableObject`, `ItemPickup` in order. If R.E.P.O. uses a different name, add it to `ItemTypeNames` in `ItemRosterCompat.cs` and update the stub accordingly.
- **Pickup detection false-positives:** The 0.5 m position-delta fallback may trigger on physics-heavy items that slide on spawn. If this is observed, raise the threshold or remove the position-delta check and rely on `isKinematic` + parent checks only.
- **Audio host name collision:** `SpatialAudio3D` creates a GameObject named `DreadSnitchBang` — this name must not collide with other Dread hosts. It is unique in the registry.
