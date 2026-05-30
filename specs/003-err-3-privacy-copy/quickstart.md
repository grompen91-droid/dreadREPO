# ERR-3 Quickstart

**Feature**: `003-err-3-privacy-copy` | **Issue**: [#173](https://github.com/grompen91-droid/dreadREPO/issues/173)

## Prerequisites

- ERR-1 test matrix and golden JSON tests passing on merge base
- .NET SDK 8+, PowerShell 7+
- Optional: game install for REPOConfig description preview

## Branch

```bash
git fetch origin
git checkout 003-err-3-privacy-copy
export SPECIFY_FEATURE=003-err-3-privacy-copy
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

## Copy review (required, manual)

1. Open [contracts/privacy-copy.md](./contracts/privacy-copy.md) checklist.
2. Open `Systems/ErrorReporting/ErrorReportPayloadCapture.cs` and `docs/adr/0010-error-telemetry.md`.
3. Mark each checklist row pass/fail in PR description.
4. In game or cfg: confirm section `5. Error Reporting` shows full description text.

## Verify disable path

1. Set `ErrorReportingEnabled = false` in cfg.
2. Trigger a test error or use checklist in `docs/agents/error-reporting-test-checklist.md` opt-out row.
3. Confirm no Worker POST (BepInEx log: enqueue skipped).

## What not to test in ERR-3

- Default-on behavior (ERR-2)
- First-run modal (ERR-2)
- Worker rate limits (ERR-1 / Worker tests)

## Artifacts

| File | Role |
|------|------|
| [spec.md](./spec.md) | Requirements |
| [plan.md](./plan.md) | Phases |
| [research.md](./research.md) | UI and payload decisions |
| [contracts/privacy-copy.md](./contracts/privacy-copy.md) | Normative copy contract |
