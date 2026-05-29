# ARCH-3 Quickstart

**Feature**: `002-arch-3-extensible-core` | **Issue**: [#175](https://github.com/grompen91-droid/dreadREPO/issues/175)

## Prerequisites

- ARCH-1 merged (`DreadSystemInitializer`, split `Systems/` layout)
- .NET SDK 8+, PowerShell 7+
- Stub build path from [ARCH-2 contracts](../001-arch-2-reduce-reflection/contracts/build-profiles.md) if on branch without game install

## Branch

```bash
git fetch origin
git checkout 002-arch-3-extensible-core
export SPECIFY_FEATURE=002-arch-3-extensible-core
```

## Tier 0 (required before PR)

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
pwsh -NoProfile ./scripts/verify-dread.ps1
dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj -c Release --nologo
dotnet format --verify-no-changes --no-restore
```

Record pass/fail in PR description.

## Implementation baseline notes

| Check | Result | Date |
|-------|--------|------|
| `gen-stubs.ps1` + stub Release build | PASS | 2026-05-30 |
| `verify-dread.ps1` Tier 0 (incl. `arch3_try_add_system`) | PASS | 2026-05-30 |
| `Arch3ProbeSystem` registry-only smoke (T008) | PASS (probe removed) | 2026-05-30 |
| Manual compat matrix (US2) | PR author: fill **PR compatibility matrix** table below | |

Stub CI does not substitute for full-game matrix rows.

## Add a probe system (US1 smoke)

After implementation, follow [contracts/system-lifecycle.md](./contracts/system-lifecycle.md):

1. Add `Systems/Arch3ProbeSystem.cs` (empty `Update` or log once).
2. Register in `DreadSystemRegistry` only.
3. Confirm `Plugin.cs` has no new `TryAddSystem` line.
4. Stub build + verify Tier 0.

Remove probe before merge unless product wants a permanent test host (default: remove).

## Manual compatibility matrix (US2)

Run on a full game profile. Copy the **PR compatibility matrix** into the GitHub PR description and fill every cell.

### PR compatibility matrix (paste into PR)

| Scenario | Tester | Date | Pass/Fail | Notes |
|----------|--------|------|-----------|-------|
| REPOConfig absent (no REPOConfig/MenuLib in profile) | | | | Dread loads; no slider compat exceptions; verbose may note skip |
| Compatibility mode on (`10. Compatibility` true; patches toggled per guide) | | | | Monster patches off; ambient runs; psychotic break off per [compatibility.md](../../docs/agents/guides/compatibility.md) |
| Non-host client (join host lobby as client) | | | | Local tension/audio OK; no host-only patch errors in log |
| Stub CI (Tier 0 only; no game) | | | | `gen-stubs` + stub build + `verify-dread.ps1` green |

### Reference steps (full game)

| Scenario | Steps | Expected |
|----------|-------|----------|
| REPOConfig absent | Profile without REPOConfig/MenuLib | Dread loads; no slider compat errors; verbose may note skip |
| Compatibility mode on | `10. Compatibility` → true, restart or toggle patches | Monster patches off; ambient still runs; psychotic break off per guide |
| Non-host client | Join host's game as client | Local tension/audio OK; no host-only patch errors in log |
| Stub CI | Tier 0 only | Build + verify green |

## Full-game smoke (optional)

Deploy to r2modman profile; confirm BepInEx loads Dread, F10 overlay if enabled, one level transition triggers initializer once.

## Docs to update in implementation PR

- `docs/adr/0016-arch-3-extension-model.md` (new)
- `docs/agents/guides/mod-architecture.md`
- `docs/agents/guides/compatibility.md`
- `CHANGELOG.md` under `[Unreleased]`
- `docs/ROADMAP.md` ARCH-3 → `in-progress` then `done` on merge
