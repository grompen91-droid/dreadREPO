# Dread Mod - Agent Instructions

## Build Output

Always build to the `dist\` folder inside the project root (same directory as `build.ps1` and `manifest.json`). `dist\` is in `.gitignore` and may not exist -- create it if missing before building:

```powershell
if (-not (Test-Path "dist")) { New-Item -ItemType Directory -Force "dist" | Out-Null }
```

Run from the project root (the folder containing `build.ps1`):
```powershell
.\build.ps1 -Version "<current version from manifest.json>"
```

The build script produces inside `dist\`:
- `elytraking-Dread-<version>\` -- unpacked package folder
- `elytraking-Dread-<version>.zip` -- Thunderstore upload zip

## Thunderstore Package Requirements

Every build zip must contain these files at the root:

| File | Requirement |
|------|-------------|
| `icon.png` | 256x256 PNG, square |
| `manifest.json` | name, version_number, website_url, description, dependencies |
| `README.md` | mod description in markdown |
| `BepInEx/plugins/elytraking-Dread/Dread.dll` | compiled mod DLL |
| `BepInEx/plugins/elytraking-Dread/audio/` | all .ogg audio files |

manifest.json format:
```json
{
  "name": "Dread",
  "version_number": "X.Y.Z",
  "website_url": "",
  "description": "Atmospheric horror overhaul for R.E.P.O. Ambient dread, scarier monsters, and a tension system.",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2100"
  ]
}
```

## Versioning Rules

- Version format: semantic versioning `MAJOR.MINOR.PATCH`
- **Never manually edit version strings** in `manifest.json`, `Plugin.cs`, `README.md`, or `THUNDERSTORE_README.md`. The CD pipeline handles all version bumps.
- When working on changes for a future release, add entries under `## [Unreleased]` in `CHANGELOG.md`. The CD pipeline reads this section for release notes.
- When ready to release, push a trigger tag to master:
  - `vmajor` -- bumps major (1.5.0 > 2.0.0)
  - `vminor` -- bumps minor (1.5.0 > 1.6.0)
  - `vpatch` -- bumps patch (1.5.0 > 1.5.1)
- The CD pipeline auto-increments `manifest.json` + `Plugin.cs` + `README.md` + `THUNDERSTORE_README.md` version badges, creates a git tag, pushes to master, builds the DLL, packages the Thunderstore zip, creates a GitHub Release, and publishes to Thunderstore.
- Thunderstore rejects any version number already published. Use `vpatch` for successive releases -- never reuse a version number.
- Never change `"name"` in manifest.json -- changing it creates a new listing, not an update.

## Changelog Convention

- Maintain `[Unreleased]` section in `CHANGELOG.md` with all unreleased changes
- CD pipeline reads `[Unreleased]` for release notes, renames it to the new version, and recreates an empty `[Unreleased]` header
- Workflow fails if `[Unreleased]` section is missing when a release tag is pushed
- Use detailed markdown with special formatting (badges, collapsible sections, tables, blockquotes)
- Never use em dash (--) in any file, ever. Use a colon, comma, or rewrite the sentence instead
- Each version entry must include: version header, release date, and categorized changes (Added, Changed, Fixed, Removed)
- Add a `> **Highlight:**` blockquote for notable releases
- Use collapsible `<details>` blocks for long technical notes

## CD Pipeline

The CD pipeline (`.github/workflows/cd.yml`) handles version bumps, builds, packaging, release, and Thunderstore publishing.

Trigger a release by pushing one of these tags:
- `vmajor` -- bumps major version (X.0.0)
- `vminor` -- bumps minor version (X.Y.0)
- `vpatch` -- bumps patch version (X.Y.Z)

```powershell
git tag vpatch
git push origin vpatch
```

The pipeline produces a version-specific tag (e.g., `v1.6.1`) and creates a GitHub Release with the DLL and Thunderstore zip attached. Thunderstore publish requires the `TCLI_AUTH_TOKEN` secret.

## GitHub Workflow

Push to GitHub after every change. Run from the project root:
```powershell
git add <files>
git commit -m "type: description"
git push
```

Remote: `https://github.com/grompen91-droid/dreadREPO.git`, branch `master`.

## Agent skills

**Start at `docs/agents/README.md`** for the full orchestration map, verify tiers, and MCP setup.

| Topic | Doc |
|-------|-----|
| Implementation guides (all systems) | `docs/agents/guides/README.md` |
| Workflows (solo, subagent, verify, release) | `docs/agents/orchestration.md` |
| Issue tracker (`gh` CLI) | `docs/agents/issue-tracker.md` |
| Triage labels | `docs/agents/triage-labels.md` |
| Domain + ADRs | `docs/agents/domain.md` + `CONTEXT.md` |
| Autonomous verify | `docs/agents/verify-dread.md` |
| Subagent prompts | `.claude/implementer-prompt.md`, `.claude/spec-reviewer-prompt.md`, `.claude/code-quality-reviewer-prompt.md` |

Backlog: `docs/ROADMAP.md`. Pick issues labeled `ready-for-agent` unless the task says otherwise.

<!-- SPECKIT START -->
**Active implementation plan (006):** [specs/006-lure-snitch-hardening/plan.md](specs/006-lure-snitch-hardening/plan.md) (branch `006-lure-snitch-hardening`, Camp Lure + Snitch hardening).
<!-- SPECKIT END -->

## Cursor Cloud specific instructions

### System dependencies

The VM needs **.NET SDK 8.0.x** and **PowerShell 7+** (`pwsh`) installed before any build commands work. The update script handles `dotnet restore` and stub generation; system-level installs are done once per VM snapshot.

### Building without the game

Since R.E.P.O. is not installed in the cloud VM, always build against generated stubs. Reflection and stub limitations: [docs/agents/guides/reflection-inventory.md](docs/agents/guides/reflection-inventory.md).

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

Stubs are cached in `.github/stubs/refs/` and only need regeneration when stub source files change.

### MCP server

```bash
cd dread-mcp-server && npm install && npm run build
```

Output lands in `dread-mcp-server/dist/index.js`.

### Lint and format

- **Code analysis:** CI runs grep-based checks for null-forgiving operators, hardcoded Windows paths, trailing whitespace, tabs, lines >120 chars, and BOM markers. See `.github/workflows/ci.yml` `analyze` job.
- **Format check:** `dotnet format --verify-no-changes --no-restore`

### Known issue on master

`ErrorReporterSystem.cs` uses `String.Contains(string, StringComparison)` which is not available in .NET Framework 4.8. This causes 5 CS1501 build errors. The CI also fails on this. This is a code issue, not an environment issue.
