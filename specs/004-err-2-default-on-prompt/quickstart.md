# ERR-2 Quickstart

**Feature**: `004-err-2-default-on-prompt` | **Issue**: [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)

## Prerequisites

- ERR-3 merged (`ErrorReportingPrivacyCopy`, privacy contract)
- ERR-1 tests green on merge base
- .NET SDK 8+, PowerShell 7+

## Branch

```bash
git fetch origin
git checkout 004-err-2-default-on-prompt
export SPECIFY_FEATURE=004-err-2-default-on-prompt
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
```

## Manual: first-run prompt

1. Delete or rename `BepInEx/config/elytraking.dread.cfg`.
2. Launch game, load into a **non-menu** level (not main menu only).
3. Confirm IMGUI prompt appears once with ERR-3 disclosure text.
4. Click **Turn off reporting**; confirm cfg has `ErrorReportingEnabled = false` and `ErrorReportingPromptShown = true`.
5. Restart, load level: no prompt.
6. Trigger test error; confirm no POST when disabled (BepInEx log).

Repeat with **Keep reporting on** on a fresh cfg.

## Manual: upgrade path

1. Use cfg with `ErrorReportingEnabled = false` from pre-ERR-2 build.
2. Deploy ERR-2 DLL; ensure `ErrorReportingPromptShown` absent or false.
3. Load gameplay level: prompt appears; reporting stays off until Keep on.
4. Confirm no telemetry left client before prompt acknowledgment (log/network).

## Manual: later opt-out

1. After prompt, set `ErrorReportingEnabled = false` in cfg or REPOConfig.
2. Confirm queue does not enqueue (existing checklist).

## Docs / ADR

- Update `docs/adr/0010-error-telemetry.md` (default-on, first-run UI).
- CHANGELOG `[Unreleased]` entry.
- README + THUNDERSTORE + `docs/mod-compatibility.md` default bullets.

## Artifacts

| File | Role |
|------|------|
| [spec.md](./spec.md) | Requirements |
| [plan.md](./plan.md) | Phases |
| [research.md](./research.md) | UI and migration decisions |
| [contracts/first-run-prompt.md](./contracts/first-run-prompt.md) | Prompt behavior |
| [contracts/config-keys.md](./contracts/config-keys.md) | New cfg key |
| ERR-3 [privacy-copy.md](../003-err-3-privacy-copy/contracts/privacy-copy.md) | Canonical strings |
