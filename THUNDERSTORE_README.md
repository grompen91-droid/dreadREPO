# Dread

![Version](https://img.shields.io/badge/version-1.6.1-crimson?style=flat-square)
![Status](https://img.shields.io/badge/status-release-brightgreen?style=flat-square)

Atmospheric horror overhaul for R.E.P.O. Nine core runtime systems (ambient audio, monster overhaul, tension, psychotic break, error reporting + first-run prompt, in-game notifications, camp lure, snitch). Debug overlay, TCP server, and test crash ship only in **development** builds, not in the Thunderstore package.

## What's new (1.6.0)

- **Error reporting:** default **on** for new installs; one-time in-game privacy prompt before any data is sent (turn off in prompt, cfg, or REPOConfig)
- **Stability:** cleaner mod file layout, safer system startup, better audio on low pitch, fewer stub-build crashes
- **Hosts:** monster changes still host-only; clients without Dread can join

Full notes: [CHANGELOG.md](https://github.com/grompen91-droid/dreadREPO/blob/master/CHANGELOG.md)

## Features

**Ambient Audio:** weighted random sounds spawn 5-15m around you every 60-180s. Pitch randomized 0.5x-1.5x. Fully spatialized.

| Sound | Rarity |
|-------|--------|
| Scraping | Common |
| Footsteps | Common |
| Breathing | Uncommon |
| Whisper | Rare |

**Monster Overhaul:** enemy speed +1.2x, detection range +1.5x, audio pitch lowered and spatialized. Works on modded enemies.

**Tension System:** scans for danger every 0.5s. Adrenaline reduces sprint drain near enemies. Panic sprint gives a speed burst. Fake footsteps play behind you. Breath sounds on low stamina.

**Psychotic Break:** When everyone is dead and you're alone, scared, and hiding in the dark, a terrifying 20-second episode can trigger. Complete with darkness, flickering shadows, circling phantom footsteps, and bone-chilling screams.

**QOL:** crouch speed +30%.

## Installation

**Mod Manager (recommended):** Search Dread under R.E.P.O. in r2modman or Thunderstore Mod Manager.

**Manual:** Download from Thunderstore, extract into `BepInEx/plugins/elytraking-Dread/`. Requires BepInEx 5.4.2100+. No other dependencies.

## Multiplayer

Monster changes are host-authoritative. Audio, tension, and psychotic breaks are client-local. Players without Dread can join normally.

| Feature | Who Needs It |
|---------|-------------|
| Monster speed, detection | Host only |
| Everything else | Per client |

## Configuration

Generated at `BepInEx/config/elytraking.dread.cfg` on first launch. Compatible with REPOConfig for live editing.

**REPOConfig sliders:** Dread includes a **temporary** label workaround when REPOConfig + MenuLib are installed (names on the left, compact rows). Styling may differ from bool toggles; upstream fix in REPOConfig/MenuLib is preferred. Without REPOConfig, or if labels look wrong, edit the cfg file directly. Details: [repo-config-slider-labels-investigation.md](https://github.com/grompen91-droid/dreadREPO/blob/master/docs/repo-config-slider-labels-investigation.md).

```
[1. Audio Dread]
Enabled = true
Frequency = 1.0
Volume = 0.4

[2. Monster Overhaul]
AggressionEnabled = true
AudioEnabled = true

[3. Tension]
AdrenalineEnabled = true
PanicSprintEnabled = true
LowStaminaSoundEnabled = true
FakeFootstepsEnabled = true

[4. Psychotic Break]
PsychoticBreakEnabled = true
PsychoticBreakTriggerChance = 0.01
PsychoticBreakDuration = 20
PsychoticBreakOncePerMatch = true

[5. QOL]
CrouchSpeedBoost = true

[6. Compatibility]
CompatibilityMode = false

[7. Error Reporting]
ErrorReportingEnabled = true     # default on for new cfg; first-run prompt on first gameplay level
ErrorReportingPromptShown = false

[8. Logging]
LogLevel = Error
```

## Compatibility

Dread only requires BepInEx. It works with many popular mods (Mimic, Wesleys Enemies, MoreUpgrades, REPOConfig), but **cannot guarantee compatibility with every mod** because Harmony patches can overlap.

**If something breaks:**

- Enable **Compatibility mode** (`6. Compatibility`) for ambient audio only
- Turn off **Error reporting** in cfg, REPOConfig, or the first-run prompt if you do not want crash telemetry
- Keep **Debug console guard** on if MenuLib/REPOConfig spams console errors

Full matrix, Proton DLL notes, and test checklist: [mod-compatibility.md](https://github.com/grompen91-droid/dreadREPO/blob/master/docs/mod-compatibility.md)

[Changelog](CHANGELOG.md)



