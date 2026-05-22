# Contributing to Dread

## Building

### Prerequisites

- .NET 8+ SDK (for build tooling; the project targets `net48`)
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

- Open PRs against `master`.
- Ensure all CI checks pass (build + analyze).
- Update `CHANGELOG.md` under `[Unreleased]` with your changes.
- The version number in `manifest.json` and `Plugin.cs` is bumped on release by CD pipeline.