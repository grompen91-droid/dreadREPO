# REPOConfig slider labels investigation

**Status:** Mitigated with a **temporary** Dread-side Harmony workaround. Needs upstream fix or a less hacky layout approach.  
**Last updated:** 2026-05-26  
**Code:** `Systems/RepoConfigSliderLabelCompat.cs`  
**Related:** Debug overlay performance work in the same session (separate issue, fixed).

---

## Symptoms

1. In **REPOConfig** (Mods menu), Dread **float/int sliders** show values on the bar but **no left-side setting name** (e.g. `Frequency`, `OffsetX`).
2. **Bool toggles** in the same menu (e.g. `DebugOverlayEnabled`) show labels correctly.
3. Problem became more noticeable after **section 11. Debug Overlay** added more slider rows (same bind pattern as older sliders).

**Environment (verified):** REPO via Proton/Linux, r2modman profile `mk`, **REPOConfig 1.2.6**, **MenuLib 2.5.3**, Dread 1.5.2.

---

## Root cause (runtime evidence)

Not a `DreadConfig` regression. Float slider keys (`Frequency`, `Volume`, etc.) unchanged since v1.0.0; PR #162 only touched compatibility and error-reporting binds.

| Layer | What happens |
|-------|----------------|
| **REPOConfig** | Always calls `MenuAPI.CreateREPOSlider(modName, string.Empty, ...)`; commented line in `ConfigMenu.cs` would pass `entry.Description.Description` but is disabled |
| **MenuLib** | Slider template `Slider - microphone`: `labelTMP` = first child TMP (`Element Name`), `descriptionTMP` = `Big Setting Text` |
| **Layout** | `REPOSlider.Awake` shifts label **left** 5.3px; `REPOToggle.Awake` shifts label **right** 100px with controls |
| **Visibility** | Name is set on `labelTMP` but sits off-layout (~44.7 local X); empty description row stays allocated |

**NDJSON debug session `e15468` (instrumentation removed after fix):**

| Hypothesis | Result | Evidence |
|------------|--------|----------|
| H1: same TMP, empty description clears name | **Rejected** | `sameReference: false` on all sampled sliders |
| H3: label text never set | **Rejected** | `labelText: "Frequency"` etc. on `Element Name` |
| H2: text set but not visible in left column | **Confirmed** | Text on `Element Name`, `descriptionText: ""` on `Big Setting Text` |
| H6: prefix description = text | **Bad UX** | Name under bar, ~2x row height via `HandleDescription` |
| H7/H8: label-only or label x=0 | **Partial** | Bar stayed put; label still wrong or invisible |
| H10: root transform +100 | **Rejected** | Entire slider row shifted right in menu |
| H11/compat-v4: label x=100, hide description | **User confirmed fixed** | Text visible; styling differs slightly from toggle labels |

**BepInEx load order:** Dread often loads **before** MenuLib. Patches must apply from `Plugin.Start()` and `DreadSystemInitializer.TryInitialize()` (reflection on loaded `MenuLib.MenuAPI`), not `Awake` alone.

---

## What we tried (chronological)

### Phase 1: Config shape experiments (reverted)

| # | Approach | Result |
|---|----------|--------|
| 1 | Duplicate int slider keys (`FrequencyMultiplier`, etc.) | Failed; duplicate cfg rows, still no labels |
| 2 | Friendly key renames + `HideFromREPOConfig` on legacy floats | Failed; orphan cfg keys added confusion |
| 3 | Revert binds to PR #162 float shape | Failed; proved not a bind-shape bug |
| 6 | Hide legacy/orphan cfg keys from REPOConfig | Partial cleanup only; reverted |

### Phase 2: MenuLib Harmony (first agent)

| # | Approach | Result |
|---|----------|--------|
| 4 | `RepoConfigSliderLabelPatch`: postfix fills `descriptionTMP` when description empty | Patch skipped in first build (MenuLib not loaded in `Awake`) |
| 5 | Deferred patch in `Start` + soft `BepInDependency("nickklmao.menulib")` | Labels visible but **under** slider bar and **~2x row height**; dependency removed per BepInEx-only policy |

### Phase 3: Debug session (second agent, NDJSON logs)

| # | Approach | Result |
|---|----------|--------|
| A | Diagnostics-only postfix (log TMP state) | Confirmed H2/H3 |
| B | Prefix: empty `description` → setting name | Text under bar unless compact forced |
| C | Label + `SliderBG` + `Bar` +100 (not pointer) | Name visible; bar/pointer misaligned |
| D | Label only x=0 | Still wrong |
| E | Root transform +100 (compat-v3) | **Whole slider** moved right (user rejected) |
| F | REPOToggle-style internal +100 on all parts (compat-v5) | User asked revert; same class of layout shift |
| **G** | **compat-v4 (kept):** label `Element Name` at **x=100**, hide `Big Setting Text`, force compact row | **User confirmed fixed** |

---

## Current fix (temporary)

**File:** `Systems/RepoConfigSliderLabelCompat.cs`  
**Gate:** Runs only when `REPOConfig` assembly is loaded (no `BepInDependency` on REPOConfig or MenuLib).

**Behavior:**

1. Postfix on all `MenuAPI.CreateREPOSlider` overloads when `description` is empty (REPOConfig case).
2. Set `labelTMP.text` to the setting name.
3. Clear and deactivate `descriptionTMP` (`Big Setting Text`).
4. Set label `localPosition.x = 100` and tweak TMP (left align, min width 180px) so text is visible left of the bar (~122).
5. Postfix on `REPOSlider.HandleDescription` to keep **15px** compact row (`SliderBG` height, `bottomPadding = 1`).
6. Apply from `Plugin.Start()` and `DreadSystemInitializer` fallback after MenuLib loads.

**Log:** `[Dread] REPOConfig slider label compat active (N hooks)`

**Without REPOConfig:** No patch. Use `elytraking.dread.cfg` or BepInEx Configuration Manager (F1).

### Why this is temporary / needs improvement

- **Magic number** `LabelColumnLocalX = 100` is tuned for one template/resolution; not the same code path as `REPOToggle` (+100 on label **and** controls together).
- **Custom TMP styling** (left align, forced width) can look different from bool toggle labels.
- **Does not fix other mods** that use REPOConfig sliders; only patches `CreateREPOSlider` globally while REPOConfig is present.
- **Proper fix** is upstream: REPOConfig passes non-empty description (or MenuLib aligns slider label column with toggles).

### Rejected approaches (do not reintroduce without new evidence)

- Filling `descriptionTMP` without forcing compact row (2x height, name under bar).
- Moving slider **root** transform (shifts entire control in scroll list).
- Shifting `SliderBG` / `Bar` without **Bar Pointer** (hitbox vs red marker desync).

---

## Recommended long-term fixes

1. **REPOConfig upstream:** Uncomment/use `entry.Description.Description` in `CreateREPOSlider` (see [REPOConfig `ConfigMenu.cs`](https://github.com/IsThatTheRealNick/REPOConfig)).
2. **MenuLib upstream:** Align `Slider - microphone` label column with `Bool Setting - Push to Talk` (+100px row shift on label + chrome together).
3. **Dread:** Remove compat patch once upstream fixed; keep investigation doc for history.
4. **Optional:** ADR for REPOConfig compat policy if patch scope grows.

---

## User workarounds

1. Edit `BepInEx/config/elytraking.dread.cfg` directly (full key list in README).
2. Use BepInEx **Configuration Manager** (F1) if installed (different UI than REPOConfig).
3. **Reset To Default** in REPOConfig after upgrades to drop orphan keys from debug-era experiments (`FrequencyMultiplier`, `Offset X`, etc.).
4. Section headers (`1. Audio Dread`, `11. Debug Overlay`) still identify groups when labels are missing.

---

## Other work from the same debug session (not slider labels)

| Topic | Outcome |
|-------|---------|
| Debug overlay FPS | Fixed: `DebugOverlayGuiRenderer` create/destroy on visibility; F10 via `DebugOverlayToggleHost` |
| `ConfigUiDetector` | Removed MenuLib type spam |
| IMGUI `Rect` IL2CPP | Float coordinates only |
| `build.ps1` | Regenerate stubs when REPO Managed folder missing |

---

## Files touched

| File | Status |
|------|--------|
| `Systems/RepoConfigSliderLabelCompat.cs` | **Active** temporary compat |
| `Plugin.cs` | `Start()` calls `TryApply` |
| `Systems/DreadSystemInitializer.cs` | Fallback `TryApply` |
| `Config/DreadConfig.cs` | Float binds unchanged; section 11 overlay sliders added |
| `README.md`, `THUNDERSTORE_README.md`, `docs/agents/domain.md` | Documented |
| `CHANGELOG.md` | Unreleased entry |
| `Systems/RepoConfigSliderLabelPatch.cs` | Never shipped (superseded) |
| Debug NDJSON (`dread-debug-e15468.log`) | Removed from code after verification |
