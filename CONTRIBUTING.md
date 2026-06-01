# Contributing to Dread

## Before you start

| Resource | Purpose |
|----------|---------|
| [docs/ROADMAP.md](docs/ROADMAP.md) | Planned backlog with roadmap IDs and GitHub issue links (#163-#175) |
| [docs/agents/README.md](docs/agents/README.md) | Agent orchestration hub (start here for autonomous work) |
| [docs/agents/orchestration.md](docs/agents/orchestration.md) | Solo, subagent, verify, and PR workflows |
| [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md) | Create and triage issues with `gh` |
| [docs/agents/domain.md](docs/agents/domain.md) | How agents should read ADRs, overlay, REPOConfig compat |
| [docs/agents/triage-labels.md](docs/agents/triage-labels.md) | Label vocabulary (`ready-for-agent`, etc.) |
| [AGENTS.md](AGENTS.md) | Build, release, and repo conventions for coding agents |
| [`CONTEXT.md`](CONTEXT.md) | Root glossary and bounded context for contributors and agents (DOCS-1) |

**Picking work:** Use [docs/ROADMAP.md](docs/ROADMAP.md) **Execution order** (P0 first, then P1/P2). Prefer the linked [GitHub issue](https://github.com/grompen91-droid/dreadREPO/issues) for that row. Comment on the issue before large changes. Reference the roadmap ID in your PR (e.g. `DBG-1`, `ARCH-1`). Suggested starters: #171, #170, #167.

**Verify locally (optional):**

```shell
pwsh ./scripts/verify-dread.ps1
```

Tier 0/1 are internal groupings inside the script; there is no `-Tier` parameter. See output lines prefixed with check ids (`arch3_*`, `mcp_build`, etc.).

See [docs/agents/verify-dread.md](docs/agents/verify-dread.md). Tier 1+ needs a running game with the debug server enabled.

## Building

### Prerequisites

- .NET 8+ SDK (for build tooling; the project targets `net48`)
- .NET 10 SDK (for `tests/Dread.ErrorReportJson.Tests` and `tests/Dread.AudioManifestJson.Tests`, run in CI after the mod build)
- Node.js 20+ (for `workers/error-reporter` Vitest suite: `npm ci && npm test`)
- PowerShell 7+ (`pwsh`)

> The `net48` target works cross-platform via the `Microsoft.NETFramework.ReferenceAssemblies`
> NuGet package, which provides the .NET Framework 4.8 reference assemblies on Linux/macOS.

### Setup

The project references R.E.P.O.'s game DLLs (`Assembly-CSharp.dll`, `UnityEngine*.dll`, `Photon*.dll`)
and BepInEx (`BepInEx.dll`, `0Harmony.dll`).

Without a local R.E.P.O. install, the build generates stub assemblies automatically. Run:

```shell
pwsh .github/scripts/gen-stubs.ps1
```

This compiles the stubs in `.github/scripts/UnityEngine_stubs.cs` into `.github/stubs/refs/UnityEngine.dll`,
creates empty DLLs for all other game references, and downloads BepInEx (placing `BepInEx.dll`
and `0Harmony.dll` in `.github/stubs/refs/core/`).

### Building (without a local game install)

```shell
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

These override flags are required:
- `GameDir` -- where the build looks for `UnityEngine.dll`, `Assembly-CSharp.dll`, `Photon*.dll`, etc.
- `BepInExDir` -- where the build looks for `core/BepInEx.dll` and `core/0Harmony.dll`
- `DeployToProfile=false` -- skips copying to an r2modman profile directory (Windows-only path)
- `DeployToDist=false` -- skips the Thunderstore packaging step (needs a pre-existing dist folder)

Output: `bin/Release/net48/Dread.dll`

### Building (with a local Windows R.E.P.O. install)

If you have the game installed, the default `GameDir` in `Dread.csproj` points to the Steam install path
and the build works out of the box:

```shell
dotnet build Dread.csproj -c Release
```

### Packaging for Thunderstore

```shell
pwsh ./build.ps1 -Version "X.Y.Z"
```

The packaging script auto-detects whether stub assemblies exist in `.github/stubs/refs/` and
passes the required `-p:` overrides automatically. On Linux or macOS, generate stubs first
(see Setup above) and then `build.ps1` uses them.

Output in `dist/`:
- `elytraking-Dread-X.Y.Z/` -- unpacked package folder
- `elytraking-Dread-X.Y.Z.zip` -- Thunderstore upload zip

### Arch Linux

1. Install the .NET SDK and PowerShell:
   ```shell
   sudo pacman -S dotnet-sdk powershell
   ```

2. Generate stub assemblies (downloads BepInEx automatically):
   ```shell
   pwsh .github/scripts/gen-stubs.ps1
   ```

3. Build, overriding the default Windows paths:
   ```shell
   dotnet build Dread.csproj -c Release \
     -p:GameDir=.github/stubs/refs \
     -p:BepInExDir=.github/stubs/refs \
     -p:DeployToProfile=false \
     -p:DeployToDist=false
   ```

   Or use the packaging script directly (it auto-detects version and stubs):
   ```shell
   pwsh ./build.ps1
   ```

Output: `bin/Release/net48/Dread.dll`

#### Troubleshooting

**"CS0246 The type or namespace name 'BepInEx' could not be found"**
  → You must pass `-p:BepInExDir=.github/stubs/refs` to `dotnet build`. Without it, the build
    uses the default Windows r2modman path from `Dread.csproj`, which does not exist on Linux.
    Run `pwsh .github/scripts/gen-stubs.ps1` first to generate the stubs (this also downloads
    BepInEx).

**"CS0246 The type or namespace name 'UnityEngine' (or 'Assembly-CSharp') could not be found"**
  → You must also pass `-p:GameDir=.github/stubs/refs`. The default `GameDir` in `Dread.csproj`
    points to the Windows Steam install path. Without the override, none of the Unity or game
    assemblies resolve.

**"error MSB3073: The command 'pwsh ... gen-stubs.ps1' exited with code 1"**
  → The auto-stub-generation MSBuild target (`GenerateStubs`) tries to run `gen-stubs.ps1` when
    `UnityEngine.dll` is missing. If `pwsh` is not in PATH, this fails. Either install PowerShell,
    or run `pwsh .github/scripts/gen-stubs.ps1` manually before building, then add
    `-p:GenerateStubs=false` to skip the automatic target.

**"error MSB4062: The task 'ZipDirectory' could not be found"**
  → The `DeployToDist` target uses `ZipDirectory` which requires .NET SDK 5+. If you still see
    this, pass `-p:DeployToDist=false`.

**"Failed to download BepInEx"**
  → The stub generator downloads BepInEx from GitHub Releases. If the download fails (proxy,
    firewall, or rate limiting), manually download from
    https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21, extract `BepInEx/core/BepInEx.dll`
    and `BepInEx/core/0Harmony.dll`, and place them in `.github/stubs/refs/core/`. Then re-run
    `pwsh .github/scripts/gen-stubs.ps1`.

### CI

The GitHub Actions CI workflow at `.github/workflows/ci.yml` runs the gen-stubs step then builds.
Verify output passes all checks before opening or updating a PR.

```shell
# Run the CI steps locally in order:
pwsh .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

## Pull Requests

- **Coding agents:** meaningful commits on a feature branch; push only for PRs or when asked; default **Debug** builds. See [AGENTS.md](AGENTS.md) and [docs/agents/orchestration.md](docs/agents/orchestration.md).
- Open PRs against `master`.
- Ensure all CI checks pass (build + analyze).
- Update `CHANGELOG.md` under `[Unreleased]` with your changes.
- The version number in `manifest.json` and `Plugin.cs` is bumped on release by CD pipeline. Do not edit version strings manually.
- Link the GitHub issue (`Fixes #NNN` or `Related to #NNN`) when applicable.
- For behavior or compat changes, update relevant docs (`README.md`, `docs/mod-compatibility.md`, or an ADR under `docs/adr/`).

## Documentation and architecture

- **ADRs** (`docs/adr/`): record non-obvious design decisions (error reporting, debug server, host-only patches).
- **Mod compatibility** ([docs/mod-compatibility.md](docs/mod-compatibility.md)): update when Harmony or optional-mod behavior changes.
- **REPOConfig slider workaround** ([docs/repo-config-slider-labels-investigation.md](docs/repo-config-slider-labels-investigation.md)): temporary compat in `Systems/RepoConfigSliderLabelCompat.cs`; prefer upstream fixes (roadmap DBG-4, [#166](https://github.com/grompen91-droid/dreadREPO/issues/166)).
- **Roadmap** ([docs/ROADMAP.md](docs/ROADMAP.md)): add planned items here and open a matching GitHub issue; close both when shipped.
- **[`CONTEXT.md`](CONTEXT.md):** root domain glossary; use its terms in issues and PRs. See [`docs/agents/domain.md`](docs/agents/domain.md) for how agents consume it.

## Reporting bugs

Use [GitHub issues](https://github.com/grompen91-droid/dreadREPO/issues/new). Include mod list (especially REPOConfig, MenuLib), platform (Windows vs Proton/Linux), Dread version, and `BepInEx/LogOutput.log` excerpts. For overlay or REPOConfig UI bugs, note whether BepInEx Configuration Manager (F1) shows labels correctly.