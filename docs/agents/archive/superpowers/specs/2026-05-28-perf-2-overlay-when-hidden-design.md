# PERF-2: Overlay does no work when hidden

**Roadmap ID:** PERF-2 (`docs/ROADMAP.md`, Phase 1) | **Issue:** [#170](https://github.com/grompen91-droid/dreadREPO/issues/170) | **Priority:** P1

## Problem

`DebugOverlaySystem` is an IMGUI HUD. It early-returns inside `OnGUI` and `Update` when the
overlay is disabled, not toggled visible, or on a menu level. But a present, enabled MonoBehaviour
still receives `OnGUI` from Unity on every IMGUI event (Layout, Repaint, and each input event) plus
`Update` every frame, even when those callbacks immediately return. The roadmap frames PERF-2 as a
"quick regression guard on the overlay perf fix already in master." We strengthen that fix so the
component does literally no per-frame work when the overlay is off, and we add a guard so the
behavior cannot silently regress.

A secondary inefficiency: `Update` runs `CountDreadPatches()` (reflection over every patched method,
every 0.5s) whenever the overlay is config-enabled, even while the HUD is toggled off via F10.

## Goals

- When the overlay is disabled, Unity invokes neither `Update` nor `OnGUI`.
- The enable/disable coupling is self-documenting and self-checking, so a future change that breaks
  it surfaces in logs rather than silently burning frames.
- Patch-count reflection runs only while the HUD is actually visible.
- A manual verification checklist lets a human (or the MCP debug server) sign off PERF-2.

## Non-goals

- No automated unit-test project for the overlay (decided out of scope for this item).
- No refactor beyond `DebugOverlaySystem.cs` and the two docs plus the changelog.
- No change to the F10 toggle semantics or the rendered overlay contents.

## Design

### 1. Lifecycle: tie `enabled` to the master config flag

Initialization moves from `Start()` to `Awake()`, because `Start()` only runs when the component is
enabled, and the component must be able to start disabled (the default, since `DebugOverlayEnabled`
defaults to false).

```
Awake():
    _visible = DebugOverlayEnabled.Value
    DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged
    enabled = DebugOverlayEnabled.Value

OnOverlayConfigChanged(...):
    _visible = DebugOverlayEnabled.Value
    enabled  = DebugOverlayEnabled.Value

OnDestroy():
    DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged
```

`Awake` runs once regardless of enabled-state, so the subscription is established even when we
immediately disable the component. `SettingChanged` is a plain C# event on the config entry,
independent of MonoBehaviour state, so it still fires while the component is disabled and re-enables
it live when the flag flips through REPOConfig or the MCP `set_config` path.

### 2. Visibility gate and `Update` ordering

Because the component is disabled whenever the master flag is off, `Update` and `OnGUI` only ever run
while `DebugOverlayEnabled` is true. The per-frame conditions collapse to one intent-revealing gate:

```
private bool IsOverlayVisible() => _visible && !SemiFunc.MenuLevel();
```

`OnGUI` early-returns unless `IsOverlayVisible()`, then renders exactly as today. `Update` becomes:

```
Update():
    [guard sentinel, see section 3]
    if (Input.GetKeyDown(F10)) _visible = !_visible;
    if (!_visible) return;
    if (Time.time >= _nextPatchRefresh) { _nextPatchRefresh = Time.time + 0.5f; refresh patch count }
```

F10 is polled before the `_visible` gate so toggling the HUD back on still works. The 0.5s
`CountDreadPatches()` reflection no longer runs while the HUD is toggled off. MenuLevel continues to
gate rendering in `OnGUI` but not the F10 poll, so leaving a menu restores the overlay correctly.

### 3. Runtime guard sentinel

A one-shot logged invariant at the top of `OnGUI` and `Update`:

```
if (!DebugOverlayEnabled.Value)
{
    if (!_loggedDisabledWhileRunning)
    {
        _loggedDisabledWhileRunning = true;
        LoggingService.LogError(
            "DebugOverlaySystem ran while DebugOverlayEnabled is false: enable/disable wiring regressed (PERF-2).");
    }
    return;
}
```

Under correct wiring this can never fire, because disabled components receive no callbacks. If a
future change breaks the enable/disable coupling, it surfaces once in the log instead of silently
burning frames. The `_loggedDisabledWhileRunning` flag prevents log spam. A logged sentinel is used
rather than `Debug.Assert`, because Unity strips assertions from release player builds, which would
leave the guard dead in shipped mod builds.

### 4. Manual verification doc

New `docs/agents/overlay-perf-checklist.md`, a sibling of `error-reporting-test-checklist.md`, with a
checkbox and expected observation per case:

- Disabled (default): overlay off, confirm the `DreadDebugOverlayHost` `DebugOverlaySystem` has
  `enabled == false`; no `OnGUI` cost in a profiler frame; the section 3 sentinel never logs.
- Enabled and F10 hidden: config on, F10 toggled off; `Update` runs (F10 polled) but no patch-count
  reflection, and `OnGUI` early-returns.
- Enabled and visible: HUD renders; patch count refreshes about every 0.5s.
- Menu level: `OnGUI` returns; overlay restores on match load.
- Live toggle: flipping `overlay.enabled` via REPOConfig or MCP `set_config` enables and disables the
  component without a restart.

### 5. Roadmap, changelog, and wiring

- `CHANGELOG.md` `[Unreleased]`: Changed entry (overlay component fully dormant when disabled) and
  Fixed entry (patch-count reflection no longer runs while the HUD is toggled off).
- `docs/ROADMAP.md`: mark PERF-2 `done` and reference the new checklist doc, mirroring how ERR-1
  references its checklist.
- No change to `DebugServerSystem`: its presence check uses `FindObjectOfType`, which is unaffected
  by `enabled`, so the MCP `overlay_present` check keeps working with a dormant component.

## Risks and mitigations

- **Subscription lost while disabled.** Mitigated by subscribing in `Awake` (always runs) rather than
  `Start`, and by `SettingChanged` being a config-level event independent of MonoBehaviour state.
- **Overlay fails to re-enable after a live config flip.** Covered by the "Live toggle" checklist
  case.
- **Menu transitions leave the overlay stuck off.** MenuLevel gates rendering only, never the
  component's enabled-state or the F10 poll, so transitions are handled in-frame.

## Acceptance criteria

- With the default config, `DebugOverlaySystem.enabled` is false and no overlay callbacks run.
- Enabling `overlay.enabled` at runtime makes the overlay functional without a restart; disabling it
  returns the component to dormant.
- While the HUD is toggled off with F10 (config still enabled), no patch-count reflection runs.
- `docs/agents/overlay-perf-checklist.md` exists and all cases pass on a manual run.
- PERF-2 is marked `done` in `docs/ROADMAP.md` and the changelog records the change.
