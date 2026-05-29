# ARCH-2 Quickstart

**Branch**: `001-arch-2-reduce-reflection`  
**Plan**: [plan.md](./plan.md)  
**Issue**: [#168](https://github.com/grompen91-droid/dreadREPO/issues/168)

## 1. Read context (5 min)

- [spec.md](./spec.md)
- [research.md](./research.md)
- [docs/ROADMAP.md](../../docs/ROADMAP.md) ARCH-2 row
- [docs/agents/guides/mod-architecture.md](../../docs/agents/guides/mod-architecture.md)

## 2. Baseline verify (stub)

```bash
cd /path/to/dreadREPO
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
pwsh -NoProfile ./scripts/verify-dread.ps1
dotnet test tests/Dread.ErrorReportJson.Tests/Dread.ErrorReportJson.Tests.csproj -c Release --nologo
```

Record pass/fail as ARCH-2 baseline.

## 3. Implementation order

1. Add `docs/agents/guides/reflection-inventory.md` (table of all sites).
2. Apply **replace** / **reduce** changes per inventory (one subsystem per commit).
3. Extend `mod-architecture.md` with stub vs full section linking [contracts/build-profiles.md](./contracts/build-profiles.md).
4. Re-run Tier 0 after each substantive commit.
5. Optional: full-game build + r2modman deploy smoke.

## 4. PR checklist

- [ ] Reflection inventory complete
- [ ] No unintended gameplay changes
- [ ] `[Unreleased]` CHANGELOG entry
- [ ] Roadmap ARCH-2 → `done` when merged
- [ ] Reference `ARCH-2` and `Fixes #168` in PR body

## 5. Next command

After plan approval: `/speckit-tasks` to generate `tasks.md`.
