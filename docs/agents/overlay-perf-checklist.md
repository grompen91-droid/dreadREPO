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
