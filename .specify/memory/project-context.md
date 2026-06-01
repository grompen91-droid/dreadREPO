# Dread project context (Spec Kit memory)

Short onboarding pointer for agents running Spec Kit on this repo.

## What Dread is

**Dread** is a BepInEx plugin for **R.E.P.O.** that adds atmospheric horror: ambient audio, host-side monster overhaul, client tension (adrenaline, panic sprint, fake footsteps), rare **Psychotic Break** episodes, error reporting, and an optional debug bridge. See [`CONTEXT.md`](../../CONTEXT.md) for the domain glossary.

## Key paths

| Path | Purpose |
|------|---------|
| `Systems/` | Runtime systems, patches, registry (`DreadSystemRegistry`, initializer) |
| `Systems/Core/` | Compat layer (`Dread.Systems.Core`) |
| `Config/` | `DreadConfig` and BepInEx config bindings |
| `specs/` | Feature specs, plans, tasks, contracts, quickstarts |
| `docs/adr/` | Architecture decision records |
| `.github/stubs/refs/` | Generated game/BepInEx stubs for CI and agents |
| `dist/` | Local Thunderstore build output (gitignored) |

## Active feature (pinned)

From [`.specify/feature.json`](../feature.json):

- **Directory**: `specs/006-lure-snitch-hardening`
- **Branch** (typical): `006-lure-snitch-hardening`
- **Scope**: Camp Lure + Snitch hardening

Scripts resolve `FEATURE_DIR` from `feature.json` when pinned, so work can continue off-branch without renaming.

## Essential links

- Agent hub: [`docs/agents/README.md`](../../docs/agents/README.md)
- Build, versioning, changelog: [`AGENTS.md`](../../AGENTS.md)
- Domain glossary: [`CONTEXT.md`](../../CONTEXT.md)
- Workflows: [`docs/agents/orchestration.md`](../../docs/agents/orchestration.md)
- Verify tiers: [`docs/agents/verify-dread.md`](../../docs/agents/verify-dread.md)
- Constitution: [`constitution.md`](./constitution.md)

## Stub build (no game install)

From repo root:

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Tier 0: `./scripts/verify-dread.ps1`

Reflection limits: [`docs/agents/guides/reflection-inventory.md`](../../docs/agents/guides/reflection-inventory.md)
