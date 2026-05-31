# Dread Mod - Agent Instructions

## Build Output

Build from project root (`build.ps1`, `manifest.json`). Create `dist\` if missing (`.gitignore`):

```powershell
if (-not (Test-Path "dist")) { New-Item -ItemType Directory -Force "dist" | Out-Null }
.\build.ps1 -Version "<version from manifest.json>"
```

Outputs: `dist\elytraking-Dread-<version>\` (unpacked), `dist\elytraking-Dread-<version>.zip` (Thunderstore).

## Thunderstore zip (root)

| Path | Requirement |
|------|-------------|
| `icon.png` | 256x256 square PNG |
| `manifest.json` | name, version_number, website_url, description, dependencies |
| `README.md` | mod description |
| `BepInEx/plugins/elytraking-Dread/Dread.dll` | compiled DLL |
| `BepInEx/plugins/elytraking-Dread/audio/` | all `.ogg` |

`manifest.json`: `"name": "Dread"` (never rename: new listing), `version_number` semver, dependency `BepInEx-BepInExPack-5.4.2100`. Full template in repo `manifest.json`.

## Versioning, changelog, CD

- Semver `MAJOR.MINOR.PATCH`. **Never edit** versions in `manifest.json`, `Plugin.cs`, `README.md`, `THUNDERSTORE_README.md` (CD bumps them).
- Unreleased work: `CHANGELOG.md` `## [Unreleased]` (required before release tag; CD uses it for notes, then renames section).
- Changelog style: categorized Added/Changed/Fixed/Removed, date + version header, `> **Highlight:**` for notable releases, `<details>` for long notes. **Never em dash (`--`)** in any file.
- Release: push tag on `master` to `origin`: `vmajor` (X.0.0), `vminor` (X.Y.0), `vpatch` (X.Y.Z). Example: `git tag vpatch && git push origin vpatch`.
- CD (`.github/workflows/cd.yml`): bumps versions, builds DLL, Thunderstore zip, GitHub Release, publishes (needs `TCLI_AUTH_TOKEN`). Creates version tag (e.g. `v1.6.1`). Thunderstore rejects duplicate version numbers; use `vpatch` for successive releases.

## Git

After changes: `git add`, `git commit -m "type: description"`, `git push`. Remote: `https://github.com/grompen91-droid/dreadREPO.git`, branch `master`.

## r2modman

Not Thunderstore Mod Manager. Base: **Linux** `$R2="${XDG_CONFIG_HOME:-$HOME/.config}/r2modmanPlus-local"` | **Windows** `$R2="$env:AppData\r2modmanPlus-local"`. Use `$R2`; do not hardcode `~/.config` on Linux.

`<profile>` = r2modman profile name (`dread`, `mk`, `REALMULTIPLAYER`, ...). Under `$R2/REPO/profiles/<profile>/`:

| Path | Notes |
|------|--------|
| `BepInEx/` | core + plugins |
| `BepInEx/config/elytraking.dread.cfg` | Dread config |
| `BepInEx/LogOutput.log` | BepInEx log |
| `BepInEx/plugins/elytraking-Dread/` | **outer**: `manifest.json`, `icon.png`, `README.md` |
| `BepInEx/plugins/elytraking-Dread/elytraking-Dread/` | **inner**: `Dread.dll`, deps, `audio/*.ogg`; `PluginDir` + `DeployToProfile` target |

`Dread.csproj` defaults: Windows `REALMULTIPLAYER`, `$(AppData)\r2modmanPlus-local\...`. Linux: pass `BepInExDir` / `PluginDir` or copy manually.

Deploy (`DeployToProfile` runs only if `PluginDir` exists; CI/cloud: `false`):

```bash
# Linux
P="$R2/REPO/profiles/<profile>"
dotnet build Dread.csproj -c Release \
  -p:GameDir=<Managed_or_.github/stubs/refs> \
  -p:BepInExDir="$P/BepInEx" \
  -p:PluginDir="$P/BepInEx/plugins/elytraking-Dread/elytraking-Dread" \
  -p:DeployToProfile=true
```

```powershell
# Windows
$p = "$env:AppData\r2modmanPlus-local\REPO\profiles\<profile>"
dotnet build Dread.csproj -c Release `
  -p:BepInExDir="$p\BepInEx" `
  -p:PluginDir="$p\BepInEx\plugins\elytraking-Dread\elytraking-Dread" `
  -p:DeployToProfile=true
```

Manual: `bin/Release/net48/` DLLs per `.github/scripts/plugin-deps.ps1`, `audio/*.ogg`, optional outer metadata. Stub vs full-game: [mod-architecture.md](docs/agents/guides/mod-architecture.md).

## Agent docs

Start: `docs/agents/README.md`. Guides: `docs/agents/guides/README.md` | Workflows: `orchestration.md` | Issues: `issue-tracker.md`, `triage-labels.md` | Domain: `domain.md`, `CONTEXT.md` | Verify: `verify-dread.md` | Prompts: `.claude/*-prompt.md`. Backlog: `docs/ROADMAP.md`; prefer `ready-for-agent` issues.

<!-- SPECKIT START -->
**Active plan (ERR-2):** [specs/004-err-2-default-on-prompt/plan.md](specs/004-err-2-default-on-prompt/plan.md) (branch `004-err-2-default-on-prompt`, [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)).
<!-- SPECKIT END -->

## Cursor Cloud

Needs **.NET SDK 8.0+**, **pwsh 7+**. No game install: build stubs only ([reflection-inventory.md](docs/agents/guides/reflection-inventory.md)).

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1   # regen when stub sources change; cache .github/stubs/refs/
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false -p:DeployToDist=false
```

MCP: `cd dread-mcp-server && npm install && npm run build` -> `dist/index.js`.

Lint: CI `analyze` job (grep: null-forgiving, Windows paths, whitespace, tabs, >120 cols, BOM). Format: `dotnet format --verify-no-changes --no-restore`.

**Known (master):** `ErrorReporterSystem.cs` `String.Contains(..., StringComparison)` invalid on .NET 4.8 (5x CS1501); CI fails until fixed.
