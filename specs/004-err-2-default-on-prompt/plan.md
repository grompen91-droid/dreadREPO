# Implementation Plan: ERR-2 + Core error capture

**Branch**: `004-err-2-default-on-prompt` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Status**: Phases 1-7 implemented; open task T046b (manual Phase 7 matrix in [quickstart.md](./quickstart.md)).

**Issue**: [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)

## Summary

ERR-2 (default-on error reporting + first-run prompt) is implemented in Phases 1-6. Phase 7 fixes error-report game-state capture: move compat helpers to `Systems/Core/`, route `EnemyHealth` HP reads through `EnemyHealthCompat` (no compile-time `CurrentHealth`), fixing `get_CurrentHealth` MissingMethodException when third-party mods (e.g. DeathMinimap) log errors after death.

## Technical Context

**Language**: C# / .NET Framework 4.8, BepInEx 5.4, Harmony 2

**Testing**: Tier 0 `verify-dread.ps1`, `Dread.ErrorReportJson.Tests`, manual quickstart (DeathMinimap NRE matrix)

**Key paths**: `Systems/Core/`, `Systems/ErrorReporting/ErrorReportPayloadCapture.cs`, [contracts/core-enemy-health.md](./contracts/core-enemy-health.md)

## Constitution Check

| Gate | Status |
|------|--------|
| Stub CI build | Pass |
| No manual version bump | Pass |
| ARCH-3 registry unchanged | Pass |

## Project Structure

```text
Systems/Core/           # Dread.Systems.Core compat helpers
Systems/ErrorReporting/ # Error reporter + prompt
specs/004-err-2-default-on-prompt/
```

See [tasks.md](./tasks.md) for phased execution (46 tasks; Phase 7 complete).
