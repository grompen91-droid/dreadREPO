# Panic Sprint Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a 1.25x sprint speed burst lasting 2 seconds, triggered when the player starts sprinting within 15m of an enemy, with a 20-second cooldown between bursts.

**Architecture:** All logic lives in `TensionSystem.cs`, which already has the 15m proximity scan (`_nearestDist`) and `PlayerController` access. A new `UpdatePanicSprint()` method tracks sprint start (watching `sprinting` bool transition), applies `SprintSpeedMultiplier * 1.25f`, then restores after 2s. Config entry added to `DreadConfig.cs`.

**Tech Stack:** BepInEx, HarmonyX, Unity MonoBehaviour, `PlayerController` (Assembly-CSharp)

---

### Task 1: Add config entry

**Files:**
- Modify: `Config/DreadConfig.cs`

Binary analysis confirmed `SprintSpeedMultiplier` and `sprinting` exist on `PlayerController`.

- [ ] **Step 1: Add field declaration**

In `DreadConfig.cs`, in the `// Tension` block after `LowStaminaSoundEnabled`:

```csharp
public static ConfigEntry<bool> PanicSprintEnabled = null!;
```

- [ ] **Step 2: Add binding in Initialize()**

After the `LowStaminaSoundEnabled` binding:

```csharp
PanicSprintEnabled = cfg.Bind("3. Tension", "PanicSprintEnabled", true,
    "Brief 1.25x speed burst when sprinting near an enemy (within 15m). 20s cooldown.");
```

- [ ] **Step 3: Build to verify no errors**

```
dotnet build Dread.csproj -c Release --nologo -v quiet
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```
git add Config/DreadConfig.cs
git commit -m "feat: add PanicSprintEnabled config entry"
```

---

### Task 2: Add panic sprint logic to TensionSystem

**Files:**
- Modify: `Systems/TensionSystem.cs`

- [ ] **Step 1: Add state variables**

In `TensionSystem`, after the `// Low stamina state` block (around line 27), add:

```csharp
// Panic sprint state
private bool _wasSprinting;
private bool _panicActive;
private float _panicTimer;
private float _panicCooldown;
private float _originalSprintMultiplier = -1f;
```

- [ ] **Step 2: Add UpdatePanicSprint() method**

Add after `UpdateLowStamina()`:

```csharp
// ── Panic Sprint ──────────────────────────────────────────────────────────

private void UpdatePanicSprint()
{
    if (!DreadConfig.PanicSprintEnabled.Value) return;

    var pc = PlayerController.instance;
    if (pc == null) return;

    _panicCooldown -= Time.deltaTime;

    bool currentlySprinting = pc.sprinting;

    if (_panicActive)
    {
        _panicTimer -= Time.deltaTime;
        if (_panicTimer <= 0f)
        {
            _panicActive = false;
            _panicCooldown = 20f;
            if (_originalSprintMultiplier >= 0f)
            {
                pc.SprintSpeedMultiplier = _originalSprintMultiplier;
                _originalSprintMultiplier = -1f;
            }
        }
    }
    else if (!_wasSprinting && currentlySprinting && _nearestDist < ProximityRange && _panicCooldown <= 0f)
    {
        _originalSprintMultiplier = pc.SprintSpeedMultiplier;
        pc.SprintSpeedMultiplier *= 1.25f;
        _panicActive = true;
        _panicTimer = 2f;
    }

    _wasSprinting = currentlySprinting;
}
```

- [ ] **Step 3: Call UpdatePanicSprint() in Update()**

In `Update()`, after `UpdateLowStamina();`:

```csharp
UpdatePanicSprint();
```

- [ ] **Step 4: Restore on scene change**

In `OnSceneLoaded()`, after `_originalDrain = -1f;`:

```csharp
_panicActive = false;
_panicTimer = 0f;
_panicCooldown = 0f;
_originalSprintMultiplier = -1f;
_wasSprinting = false;
```

- [ ] **Step 5: Build to verify no errors**

```
dotnet build Dread.csproj -c Release --nologo -v quiet
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```
git add Systems/TensionSystem.cs
git commit -m "feat: add panic sprint burst (1.25x for 2s, 20s cooldown)"
```

---

### Task 3: In-game verification

- [ ] **Step 1: Build dist package**

```powershell
.\build.ps1 -Version "1.1.0"
```

- [ ] **Step 2: Install and test**

Copy `dist/elytraking-Dread-1.1.0/BepInEx` to r2modman profile directory.

- [ ] **Step 3: Verify burst triggers**

Enter a level. Get within 15m of an enemy. Begin sprinting — movement should feel noticeably faster for ~2 seconds, then return to normal.

- [ ] **Step 4: Verify cooldown**

After burst ends, sprint again immediately near an enemy — no second burst should occur for 20 seconds.

- [ ] **Step 5: Verify toggle**

Set `PanicSprintEnabled = false` in the config. Confirm no burst occurs.
