# PERF-2 Overlay When Hidden Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `DebugOverlaySystem` do zero per-frame work when the overlay is disabled, add a logged invariant guard against regression, and stop patch-count reflection while the HUD is toggled off.

**Architecture:** Tie the MonoBehaviour's `enabled` state to the `DebugOverlayEnabled` config flag (set in `Awake`, kept in sync by the existing `SettingChanged` handler) so Unity stops invoking `Update`/`OnGUI` entirely when off. Collapse the remaining runtime conditions into one `IsOverlayVisible()` gate, and add a one-shot `LoggingService.LogError` sentinel that fires only if the enable/disable coupling ever breaks.

**Tech Stack:** C# (.NET Framework 4.8 target via stubs), BepInEx, Unity IMGUI, Harmony. No automated test project for this item (decided out of scope); verification is a compile build plus a manual in-game checklist.

**Spec:** `docs/superpowers/specs/2026-05-28-perf-2-overlay-when-hidden-design.md`

---

## File structure

- **Modify:** `Systems/DebugOverlaySystem.cs` — lifecycle wiring, visibility gate, guard sentinel, `Update` reorder. The only code file touched.
- **Create:** `docs/agents/overlay-perf-checklist.md` — manual verification matrix (sibling of `error-reporting-test-checklist.md`).
- **Modify:** `CHANGELOG.md` — `[Unreleased]` Changed + Fixed entries.
- **Modify:** `docs/ROADMAP.md` — mark PERF-2 `done`, reference the checklist.

---

## Task 1: Rewire `DebugOverlaySystem` lifecycle, gate, and guard

**Files:**
- Modify: `Systems/DebugOverlaySystem.cs`

All six edits below are exact string replacements against the current file. Apply them in order.

- [ ] **Step 1: Add the one-shot guard flag field**

Replace:

```csharp
        private bool _visible;
        private float _nextPatchRefresh;
        private GUIStyle? _boxStyle;
```

with:

```csharp
        private bool _visible;
        private float _nextPatchRefresh;
        private bool _loggedDisabledWhileRunning;
        private GUIStyle? _boxStyle;
```

- [ ] **Step 2: Move init from `Start` to `Awake` and set initial `enabled`**

`Start` only runs when the component is enabled, so initialization must move to `Awake` (which always runs once) to support starting disabled. Replace:

```csharp
        private void Start()
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
        }
```

with:

```csharp
        private void Awake()
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }
```

- [ ] **Step 3: Keep `enabled` in sync when the config flips at runtime**

Replace:

```csharp
        private void OnOverlayConfigChanged(object? sender, System.EventArgs e)
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
        }
```

with:

```csharp
        private void OnOverlayConfigChanged(object? sender, System.EventArgs e)
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }
```

- [ ] **Step 4: Reorder `Update` (F10 poll first, then gate patch reflection on visibility)**

Replace:

```csharp
        private void Update()
        {
            if (!DreadConfig.DebugOverlayEnabled.Value || SemiFunc.MenuLevel())
                return;

            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;

            if (Time.time >= _nextPatchRefresh)
            {
                _nextPatchRefresh = Time.time + 0.5f;
                DreadRuntimeState.DreadPatchCount = CountDreadPatches();
            }
        }
```

with:

```csharp
        private void Update()
        {
            if (!GuardOverlayEnabled())
                return;

            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;

            if (!IsOverlayVisible())
                return;

            if (Time.time >= _nextPatchRefresh)
            {
                _nextPatchRefresh = Time.time + 0.5f;
                DreadRuntimeState.DreadPatchCount = CountDreadPatches();
            }
        }
```

- [ ] **Step 5: Replace the `OnGUI` early-return with the guard + visibility gate**

Replace:

```csharp
        private void OnGUI()
        {
            if (!_visible || !DreadConfig.DebugOverlayEnabled.Value || SemiFunc.MenuLevel())
                return;

            EnsureStyles();
```

with:

```csharp
        private void OnGUI()
        {
            if (!GuardOverlayEnabled())
                return;

            if (!IsOverlayVisible())
                return;

            EnsureStyles();
```

- [ ] **Step 6: Add the `IsOverlayVisible` gate and `GuardOverlayEnabled` sentinel**

Insert the two helper methods immediately before `EnsureStyles`. Replace:

```csharp
        private void EnsureStyles()
        {
            if (_boxStyle != null)
```

with:

```csharp
        private bool IsOverlayVisible() => _visible && !SemiFunc.MenuLevel();

        private bool GuardOverlayEnabled()
        {
            if (DreadConfig.DebugOverlayEnabled.Value)
                return true;

            if (!_loggedDisabledWhileRunning)
            {
                _loggedDisabledWhileRunning = true;
                LoggingService.LogError(
                    "DebugOverlaySystem ran while DebugOverlayEnabled is false: enable/disable wiring regressed (PERF-2).");
            }

            return false;
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
```

- [ ] **Step 7: Generate stubs and compile**

`LoggingService` and `SemiFunc` are already in scope (same namespace / existing references), so no `using` changes are needed.

Run from the project root:

```powershell
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs -p:DeployToProfile=false -p:DeployToDist=false
```

Expected: `Build succeeded`, 0 errors. (Note: master has a pre-existing CS1501 issue in `ErrorReporterSystem.cs` per AGENTS.md "Known issue on master"; if that surfaces it is unrelated to this change. The overlay file itself must compile clean.)

- [ ] **Step 8: Commit**

```powershell
git add Systems/DebugOverlaySystem.cs
git commit -m "perf(PERF-2): disable overlay component when hidden, guard the invariant"
```

---

## Task 2: Manual verification checklist doc

**Files:**
- Create: `docs/agents/overlay-perf-checklist.md`

- [ ] **Step 1: Create the checklist file**

Write `docs/agents/overlay-perf-checklist.md` with exactly this content:

```markdown
# Overlay performance checklist (PERF-2)

Manual verification that `DebugOverlaySystem` does no per-frame work when the debug
overlay is hidden, and that the enable/disable wiring cannot silently regress.

Issue: [#170](https://github.com/grompen91-droid/dreadREPO/issues/170)

---

## Prerequisites

- [ ] R.E.P.O. running with Dread installed (local or Thunderstore build)
- [ ] BepInEx ConfigurationManager (F1) installed for live config toggling
- [ ] Optional: Unity profiler or frame-time overlay for the disabled-cost check
- [ ] Optional: `DebugServerEnabled=true` + MCP for the live-toggle case via `set_config`

---

## Test matrix

### A. Disabled (default config)

- [ ] Leave `DebugOverlayEnabled` at its default (false)
- [ ] Load into a run; confirm no overlay is drawn
- [ ] Confirm the `DreadDebugOverlayHost` `DebugOverlaySystem` component reports `enabled == false` (ConfigurationManager object inspector, profiler, or MCP `get_runtime_state`)
- [ ] Confirm no measurable `OnGUI` cost for the host in a profiler frame
- [ ] Confirm the BepInEx log never contains `enable/disable wiring regressed (PERF-2)`

### B. Enabled but toggled off with F10

- [ ] Set `DebugOverlayEnabled=true`
- [ ] Press F10 until the overlay is hidden
- [ ] Confirm `OnGUI` draws nothing (no box, no labels)
- [ ] Confirm the Harmony patch count stops refreshing (no patch-count reflection while hidden): `DreadPatchCount` should not change while toggled off
- [ ] Press F10 again; the overlay reappears and the patch count resumes refreshing about every 0.5s

### C. Enabled and visible

- [ ] With `DebugOverlayEnabled=true` and F10 toggled on, confirm the HUD renders all sections
- [ ] Confirm the patch count refreshes about every 0.5s

### D. Menu level

- [ ] With the overlay enabled and visible, return to a menu level
- [ ] Confirm `OnGUI` draws nothing while on the menu
- [ ] Load back into a run; confirm the overlay restores automatically

### E. Live config toggle (no restart)

- [ ] Start with `DebugOverlayEnabled=false`
- [ ] Flip `overlay.enabled` to true via ConfigurationManager or MCP `set_config`
- [ ] Confirm the overlay becomes functional without restarting the game (component `enabled == true`)
- [ ] Flip it back to false; confirm the component returns to `enabled == false` and draws nothing
- [ ] Confirm the regression sentinel never logged during any toggle

---

## Sign-off

- [ ] All cases above pass
- [ ] PERF-2 marked `done` in `docs/ROADMAP.md`
- [ ] `CHANGELOG.md` `[Unreleased]` records the change
```

- [ ] **Step 2: Commit**

```powershell
git add docs/agents/overlay-perf-checklist.md
git commit -m "docs(PERF-2): add overlay performance verification checklist"
```

---

## Task 3: Changelog and roadmap updates

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Add a Changed entry under `[Unreleased]`**

In `CHANGELOG.md`, find the `### Changed` line under the `## [Unreleased]` section. Insert this as the first bullet directly under `### Changed`:

```markdown
- **Debug overlay (PERF-2):** `DebugOverlaySystem` is now fully dormant when `DebugOverlayEnabled` is off (component disabled, so Unity invokes neither `Update` nor `OnGUI`); `enabled` tracks the config flag live. Adds a one-shot logged guard if the disabled-state invariant is ever violated. (#170)
```

- [ ] **Step 2: Add a Fixed entry under `[Unreleased]`**

In `CHANGELOG.md`, find the `### Fixed` line under the `## [Unreleased]` section. Insert this as the first bullet directly under `### Fixed`:

```markdown
- **Debug overlay (PERF-2):** Harmony patch-count reflection no longer runs while the HUD is toggled off with F10; it runs only when the overlay is actually visible. (#170)
```

- [ ] **Step 3: Mark PERF-2 done in the roadmap Performance table**

In `docs/ROADMAP.md`, replace this row:

```markdown
| PERF-2 | P1 | **Overlay when hidden** | Verify no `OnGUI` when HUD hidden | idea | [#170](https://github.com/grompen91-droid/dreadREPO/issues/170) |
```

with:

```markdown
| PERF-2 | P1 | **Overlay when hidden** | Component disabled when off (no `Update`/`OnGUI`); guard + checklist `docs/agents/overlay-perf-checklist.md` | done | [#170](https://github.com/grompen91-droid/dreadREPO/issues/170) |
```

- [ ] **Step 4: Update the Phase 1 execution-order note for PERF-2**

In `docs/ROADMAP.md`, replace the Phase 1 row:

```markdown
| 2 | PERF-2 | P1 | [#170](https://github.com/grompen91-droid/dreadREPO/issues/170) | None | Quick regression guard on overlay perf fix already in `master` |
```

with:

```markdown
| 2 | PERF-2 | P1 | [#170](https://github.com/grompen91-droid/dreadREPO/issues/170) | None | Done: component disabled when off; guard + manual checklist |
```

- [ ] **Step 5: Commit**

```powershell
git add CHANGELOG.md docs/ROADMAP.md
git commit -m "docs(PERF-2): changelog and roadmap (mark PERF-2 done)"
```

---

## Task 4: Final manual run (in-game)

**Files:** none (verification only)

- [ ] **Step 1: Build and deploy a local test DLL**

Run the project build (with the game installed locally) per `build.ps1`, or load the stub-built DLL into a test profile. This step requires R.E.P.O. installed and is performed by a human.

- [ ] **Step 2: Walk `docs/agents/overlay-perf-checklist.md`**

Execute every case (A through E) and tick the boxes. Cases A and E are the core PERF-2 guarantees (dormant when off; clean live toggle).

- [ ] **Step 3: Record the result**

If all cases pass, PERF-2 is verified. If any case fails, file the discrepancy against issue #170 before closing.

---

## Notes for the executor

- Do not add a test project for this item; it was explicitly scoped out. The "test" is the compile build (Task 1 Step 7) plus the manual checklist (Task 4).
- Do not change the rendered overlay contents, the F10 semantics, or `DebugServerSystem` (its presence check uses `FindObjectOfType`, which is unaffected by `enabled`).
- Never use an em dash in any file (AGENTS.md house rule); use a colon, comma, or rewrite.
- Do not bump version strings; the CD pipeline owns versioning.
```
