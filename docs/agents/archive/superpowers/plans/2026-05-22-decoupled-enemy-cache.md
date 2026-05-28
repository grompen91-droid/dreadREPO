# Decoupled Enemy Cache Implementation Plan (Issue #103)

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development (recommended) or executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the shared static `MonsterOverhaulSystem.CachedEnemies` cache and have each system use `FindObjectsOfType` directly.

**Architecture:** TensionSystem scans at 0.5s intervals inline in `FindNearestEnemyDist()`. MonsterOverhaulSystem scans at 4s intervals in `MonsterAudioLoop()`. No shared state, no cache management.

**Tech Stack:** Unity, C#, BepInEx

---

### Task 1: Decouple TensionSystem from shared cache

**Files:**
- Modify: `Systems/TensionSystem.cs:82, 210-225`

- [ ] **Step 1: Change scan interval to 0.5s and remove shared cache dependency**

Change `_nextScan` interval from 2.0s to 0.5s so proximity reacts faster:
```csharp
_nextScan = Time.time + 0.5f;
```

Replace `FindNearestEnemyDist()` body that reads the shared static with a direct `FindObjectsOfType` call:
```csharp
private float FindNearestEnemyDist()
{
    if (_mainCam == null) _mainCam = Camera.main;
    var cam = _mainCam;
    if (cam == null) return float.MaxValue;

    float nearest = float.MaxValue;
    foreach (var e in FindObjectsOfType<EnemyHealth>())
    {
        if (e == null) continue;
        float d = Vector3.Distance(cam.transform.position, e.transform.position);
        if (d < nearest) nearest = d;
    }
    return nearest;
}
```

No null-culling needed — `FindObjectsOfType` returns a fresh array of live objects each call.

- [ ] **Step 2: Commit**

```bash
git add Systems/TensionSystem.cs
git commit -m "fix: decouple TensionSystem enemy cache from MonsterOverhaulSystem
TensionSystem now calls FindObjectsOfType directly at 0.5s intervals
instead of reading MonsterOverhaulSystem.CachedEnemies. This fixes
the phantom cache bug where proximity features silently broke when
MonsterAudio was disabled.
Issue: #103"
```

---

### Task 2: Remove shared cache from MonsterOverhaulSystem

**Files:**
- Modify: `Systems/MonsterOverhaulSystem.cs:5, 18, 41-49, 63`

- [ ] **Step 1: Remove `using System.Collections.Generic;`**

Delete line 5 (`using System.Collections.Generic;`) — no `List<T>` remains in this file.

- [ ] **Step 2: Remove the static cache field**

Delete line 18:
```csharp
internal static readonly List<EnemyHealth> CachedEnemies = new();
```

- [ ] **Step 3: Remove `RefreshEnemyCache()` method**

Delete entire method (lines 41-49):
```csharp
private void RefreshEnemyCache()
{
    if (Time.time < _nextEnemyRefresh) return;
    _nextEnemyRefresh = Time.time + 5f;

    var found = FindObjectsOfType<EnemyHealth>();
    CachedEnemies.Clear();
    CachedEnemies.AddRange(found);
}
```

- [ ] **Step 4: Simplify MonsterAudioLoop to use direct FindObjectsOfType**

Replace the refresh+null-cull+iterate block with a direct scan:
```csharp
private IEnumerator MonsterAudioLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(4f);

        if (!DreadConfig.MonsterAudioEnabled.Value || !_inLevel) continue;

        foreach (var e in FindObjectsOfType<EnemyHealth>())
        {
            if (e == null) continue;
            if (e.GetComponent<DreadAudioTweaked>() != null) continue;
            e.gameObject.AddComponent<DreadAudioTweaked>();
            ApplyAudioTweaks(e.gameObject);
        }
    }
}
```

No need for `_nextEnemyRefresh` or the old RefreshEnemyCache — the `WaitForSeconds(4f)` in the loop header handles the cadence.

- [ ] **Step 5: Remove unused field `_nextEnemyRefresh`**

Delete:
```csharp
private float _nextEnemyRefresh;
```

- [ ] **Step 6: Commit**

```bash
git add Systems/MonsterOverhaulSystem.cs
git commit -m "fix: remove shared static CachedEnemies from MonsterOverhaulSystem
MonsterAudioLoop now calls FindObjectsOfType directly at its own
4s cadence. No shared state, no stale cache.
Issue: #103"
```

---

### Task 3: Update ADR-0008

**Files:**
- Modify: `docs/adr/0008-shared-enemy-cache.md`

- [ ] **Step 1: Add superseded note**

Add a line at the top, after the title:
```markdown
**Status:** Superseded by ADR-0010 — the shared cache caused invisible cross-system coupling (Issue #103) and was replaced with direct per-system FindObjectsOfType calls.
```

- [ ] **Step 2: Commit**

```bash
git add docs/adr/0008-shared-enemy-cache.md
git commit -m "docs: mark ADR-0008 as superseded by decoupled cache approach"
```

---

### Task 4: Update CHANGELOG

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add entry under `### Fixed` in `[Unreleased]`**

```markdown
### Fixed

- TensionSystem proximity features (adrenaline, panic sprint, low stamina breath) silently failed when MonsterAudio was disabled due to cross-system cache coupling. Each system now scans independently (Issue: #103)
```

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update changelog for decoupled enemy cache"
```

---

### Self-Review Checklist

- [ ] No remaining references to `MonsterOverhaulSystem.CachedEnemies` anywhere in the codebase
- [ ] No `using System.Collections.Generic;` in MonsterOverhaulSystem.cs (removed)
- [ ] `_nextEnemyRefresh` removed from MonsterOverhaulSystem
- [ ] CI build still works (no stubs changes needed)
