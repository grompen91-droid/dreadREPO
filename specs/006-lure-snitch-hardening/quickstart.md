# Quickstart: Camp Lure and Snitch hardening (006)

**Branch**: `006-lure-snitch-hardening`  
**Build** (from repo root):

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
pwsh -NoProfile scripts/verify-dread.ps1
```

Enable F10 debug overlay (`DebugOverlayEnabled = true`) for phase/block reason visibility.

---

## Matrix 1: Gameplay phase gating (US1)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Host, features on, enter **truck/shop** between runs | Overlay phase `truck/shop`; Snitch `disarmed` + block `truck/shop`; Lure no target |
| 2 | Wait 30s in shop | No `[Snitch] Armed`, no camp lure pull logs (debug) |
| 3 | Enter extraction level, wait for level gen | Phase `run`; snitch arms within retry window |
| 4 | Return to truck (single load) | Phase `truck/shop`; snitch disarmed; lure cleared |

---

## Matrix 2: Camp lure cooldown (US2)

**Config**: `LureCampSeconds = 10`, `LureSafeDistance = 5`, `LureEscalateSeconds = 5`, `LureCooldownSeconds = 60`

| Step | Action | Expected |
|------|--------|----------|
| 1 | Solo, hide far from enemies 15s+ | Lure arms (overlay shows target) |
| 2 | Let enemy approach within safe distance | Pull stops; cooldown shown (~60s) |
| 3 | Stay hidden, enemy leaves again | Lure does **not** re-arm until cooldown expires |
| 4 | After cooldown, stay isolated | Camp timer accumulates; lure can arm again |

---

## Matrix 3: No enemies (US3)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Level with zero enemies (or dev empty scan) | No lure target; camp timers flat |
| 2 | Enemies spawn | Normal accumulation resumes |

---

## Matrix 4: Snitch hygiene (US4)

| Step | Action | Expected |
|------|--------|----------|
| 1 | Extraction level after gen | One `[Snitch] Armed` at Info; no Warning spam each frame |
| 2 | Pick up non-snitch items | No trigger |
| 3 | Pick up snitch item | Bang at position; POI countdown; enemies investigate |
| 4 | Multiplayer: client picks snitch | Host POI active; document whether client hears bang |
| 5 | Shop items stationary | No instant false trigger from kinematic at spawn |

---

## Matrix 5: Compatibility and config off

| Step | Action | Expected |
|------|--------|----------|
| 1 | `CompatibilityMode = true` | Both features blocked |
| 2 | `LureEnabled = false` / `SnitchEnabled = false` | Respective system blocked |

---

## Regression: Snitch #222 fixes

- Additive scene loads during level gen do not reset arm timer
- `OnLevelGenDone` still triggers arm attempt
- Item scan includes inactive objects

---

## Sign-off

- [ ] All matrices pass on host solo
- [ ] Matrix 1 + 4 partial pass on host + 1 client (MP)
- [ ] Tier 0 verify green
- [ ] `[Unreleased]` CHANGELOG updated
