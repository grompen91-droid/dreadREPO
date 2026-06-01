# Dread Mod - Agent Instructions

## Implementing work (read this first)

**Rule precedence:** Instructions in this file and linked `docs/agents/` docs override generic assistant defaults. If something conflicts or is unclear, **ask the user** before proceeding.

### Spec Kit feature (active plan in `.specify/feature.json`)

When a plan is active, use the Spec Kit flow on branch **`NNN-kebab-name`** (must match the spec folder, e.g. `014-remote-audio-assets`):

1. **Specify / plan / tasks** under `specs/NNN-.../` (or update existing artifacts)
2. **Implement** per `tasks.md`
3. **Analyze** (consistency check across spec, plan, tasks, code)
4. **Verify** Tier 0 (`scripts/verify-dread.ps1`); in-game/MCP when applicable
5. **Pull request** into `master` when the feature is complete

Remove the `<!-- SPECKIT START -->` block from this file when the feature is merged and `.specify/feature.json` is cleared or repointed.

### Other work (no active Spec Kit plan)

```
GitHub issue or ROADMAP row (ready-for-agent)
  -> CONTEXT.md + docs/agents/domain.md + relevant ADRs
  -> branch: fix/short-description or feat/short-description (kebab-case)
  -> implement (minimal diff)
  -> meaningful git commits on the branch
  -> Tier 0 verify; dotnet format if C# changed
  -> push + PR to master only when opening or updating the PR (or when the user asks)
  -> CHANGELOG [Unreleased] for user-facing changes
```

Full checklist: [docs/agents/orchestration.md](docs/agents/orchestration.md) (Workflow A), [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Version control (agents)

| Rule | Detail |
|------|--------|
| **Commit** | After **meaningful** changes only (a coherent unit you could revert to). Not every edit; enough history to backtrack. Messages: `feat:`, `fix:`, `docs:`, `ci:`, etc. |
| **Branch** | No direct commits to `master` for feature work. |
| **Spec Kit branch** | `NNN-kebab-name` when `.specify/feature.json` points at `specs/NNN-.../`. |
| **Otherwise** | `fix/...`, `feat/...`, or similar descriptive names (kebab-case). |
| **Push** | Only when the user asks, or to **open or update a pull request**. Do not push after every commit by default. |
| **Release tags** | Do **not** push `vpatch`, `vminor`, or `vmajor` unless the user explicitly asks. |
| **PR** | Open from your feature branch into `master` when work is ready for review. |

Remote: `https://github.com/grompen91-droid/dreadREPO.git`, default branch `master`.

```powershell
# After a meaningful change (project root)
git add <files>
git commit -m "type: short description"

# When opening or updating a PR (or if the user asked to push)
git push -u origin HEAD
gh pr create --base master --title "..." --body "..."
```

---

## Build (agents)

**Default: always produce a Debug build** (`DREAD_DEBUG`: overlay, TCP server, test crash, debug config sections) unless the user explicitly requests a **production Release** build.

### Day-to-day: `dotnet build`

Use stubs when the game is not installed (Cursor Cloud, Linux CI-style dev):

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1

dotnet build Dread.csproj -c Debug \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:PluginDir="<profile>/BepInEx/plugins/elytraking-Dread" \
  -p:DeployToDist=false
```

- **`DeployToProfile`:** runs automatically when `PluginDir` exists; copies `Dread.dll`, NVorbis deps, and **`audio/**`** beside the plugin (offline debug audio).
- **Production Release** (only when asked): add `-c Release`, `-p:EnableDebugFeatures=false`, `-p:DeployToProfile=false`.

See [debug-tooling.md](docs/agents/guides/debug-tooling.md) and [development-only-features.md](docs/agents/guides/development-only-features.md).

### When to use `build.ps1`

Use **`.\build.ps1 -DebugBuild`** (agents) or **`.\build.ps1 -Version "<from manifest.json>"`** (release packaging) when files that affect the **Thunderstore zip layout** changed, for example:

- `manifest.json`, `icon.png`, `README.md` / `THUNDERSTORE_README.md`
- `audio/audio-manifest.json` or `audio/**` layout
- Packaging scripts under `.github/scripts/` that affect the zip

Otherwise **`dotnet build -c Debug`** is enough for code-only changes.

`-DebugBuild` also copies **`audio/**`** into the packaged plugin folder under `dist/` so local zip installs include baked audio for testing.

### r2modman plugin path (canonical layout)

Thunderstore and `build.ps1` use a **flat** plugin folder:

```text
BepInEx/plugins/elytraking-Dread/
  Dread.dll
  audio/          # Debug deploy / local testing (category subfolders)
  audio-cache/    # Runtime download cache (production)
```

Do **not** deploy to a nested `elytraking-Dread/elytraking-Dread/` folder unless your profile truly uses that layout. Set `-p:PluginDir` to the directory that already contains (or should contain) `Dread.dll`.

Example (Linux, `dread` profile):

```bash
PLUGIN="$HOME/.config/r2modmanPlus-local/REPO/profiles/dread/BepInEx/plugins/elytraking-Dread"
```

More detail: [specs/014-remote-audio-assets/quickstart.md](specs/014-remote-audio-assets/quickstart.md) (when present for active audio work).

### Debug audio

Debug builds must ship **local OGG** next to the plugin (or in a Debug `build.ps1` package): repo `audio/**` is copied on Debug deploy. At runtime, `#if DREAD_DEBUG` can import those files into `audio-cache/` before HTTP. Production Thunderstore zips stay **DLL-only**; players fetch OGG from GitHub Releases (AUDIO-5).

---

## Agent skills

**Start at [docs/agents/README.md](docs/agents/README.md)** for orchestration, verify tiers, and MCP.

| Topic | Doc |
|-------|-----|
| Implementation guides | [docs/agents/guides/README.md](docs/agents/guides/README.md) |
| Workflows | [docs/agents/orchestration.md](docs/agents/orchestration.md) |
| Issue tracker (`gh`) | [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md) |
| Triage labels | [docs/agents/triage-labels.md](docs/agents/triage-labels.md) |
| Domain + ADRs | [docs/agents/domain.md](docs/agents/domain.md) + [CONTEXT.md](CONTEXT.md) |
| Autonomous verify | [docs/agents/verify-dread.md](docs/agents/verify-dread.md) |
| Remote / audio assets | [docs/agents/guides/remote-assets.md](docs/agents/guides/remote-assets.md) |
| Subagent prompts | `.claude/implementer-prompt.md`, `.claude/spec-reviewer-prompt.md`, `.claude/code-quality-reviewer-prompt.md` |

Backlog: [docs/ROADMAP.md](docs/ROADMAP.md). Prefer issues labeled `ready-for-agent`.

<!-- SPECKIT START -->
**Active plan:** [specs/014-remote-audio-assets/plan.md](specs/014-remote-audio-assets/plan.md) on branch **`014-remote-audio-assets`**. Quickstart: [specs/014-remote-audio-assets/quickstart.md](specs/014-remote-audio-assets/quickstart.md).
<!-- SPECKIT END -->

---

## Cursor Cloud

### Dependencies

**.NET SDK 8.0.x** and **PowerShell 7+** (`pwsh`). Stubs: `.github/stubs/refs/` (regenerate when stub sources change).

### Build and verify

Always **Debug** + stubs (see [Build (agents)](#build-agents)). Tier 0: `pwsh -NoProfile ./scripts/verify-dread.ps1`.

### MCP

```bash
cd dread-mcp-server && npm install && npm run build
```

### Lint and format

- CI analyze job: null-forgiving, Windows paths, whitespace, line length (see `.github/workflows/ci.yml`)
- `dotnet format --verify-no-changes --no-restore`

### Stub maintenance

| Script | When |
|--------|------|
| `pwsh -NoProfile .github/scripts/gen-stubs.ps1` | After `UnityEngine_stubs.cs` or game API surface changes |
| `pwsh -NoProfile .github/scripts/clean-stubs.ps1` | Full stub regen |

---

## Thunderstore package (release / packaging)

Every upload zip root must include:

| File | Requirement |
|------|-------------|
| `icon.png` | 256x256 PNG |
| `manifest.json` | name, version_number, website_url, description, dependencies |
| `README.md` | mod description |
| `BepInEx/plugins/elytraking-Dread/Dread.dll` | + embedded `audio-manifest.json` |

**No OGG** in the Thunderstore zip. Audio ships on the matching [GitHub Release](https://github.com/grompen91-droid/dreadREPO/releases) and downloads to `audio-cache/v{version}/`. Source OGG in repo `audio/` is for CD upload and **Debug** local copies only.

Build release zip from project root:

```powershell
if (-not (Test-Path "dist")) { New-Item -ItemType Directory -Force "dist" | Out-Null }
.\build.ps1 -Version "<current version from manifest.json>"
```

Output: `dist/elytraking-Dread-<version>/` and `.zip`.

manifest.json shape (do not change `"name"`):

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

---

## Versioning rules

- Semantic versioning `MAJOR.MINOR.PATCH`
- **Never manually edit** version strings in `manifest.json`, `Plugin.cs`, `README.md`, or `THUNDERSTORE_README.md` (CD bumps them)
- Add user-facing work under `## [Unreleased]` in `CHANGELOG.md`
- **Maintainer release:** push tag `vpatch`, `vminor`, or `vmajor` to `master` (agents only if the user asks)

---

## Changelog convention

- Keep `[Unreleased]` up to date; CD uses it for release notes
- CD fails if `[Unreleased]` is missing when a release tag is pushed
- Never use em dash (`--`) in any file: use a colon, comma, or rewrite
- Version entries: date, Added / Changed / Fixed / Removed; optional `> **Highlight:**` and `<details>` for long notes

---

## CD pipeline

See `.github/workflows/cd.yml`. Trigger (maintainer or explicit user request):

```powershell
git tag vpatch
git push origin vpatch
```

Produces version tag (e.g. `v1.6.1`), GitHub Release (DLL + zip), Thunderstore publish (`TCLI_AUTH_TOKEN`).
