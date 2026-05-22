# Contributing to Dread

## Building

### Prerequisites

- .NET 8+ SDK (for build tooling; the project targets `net48`)
- PowerShell 7+ (`pwsh`)

### Setup

The project references R.E.P.O.'s game DLLs (`Assembly-CSharp.dll`, `UnityEngine*.dll`, `Photon*.dll`). Without a local game install, the build auto-generates stub assemblies via `.github/scripts/gen-stubs.ps1` when it detects the DLLs are missing:

```shell
pwsh .github/scripts/gen-stubs.ps1
```

This compiles `UnityEngine_stubs.cs` into a real `UnityEngine.dll` and creates empty DLLs for all other references (`UnityEngine.CoreModule`, `Assembly-CSharp`, `Photon*`, etc.), placing them in `.github/stubs/refs/`.

### Building

```shell
dotnet build Dread.csproj -c Release
```

Output: `bin/Release/net48/Dread.dll`

### Packaging for Thunderstore

```shell
pwsh ./build.ps1 -Version "X.Y.Z"
```

Output in `dist/`:
- `elytraking-Dread-X.Y.Z/` -- unpacked package folder
- `elytraking-Dread-X.Y.Z.zip` -- Thunderstore upload zip

### Arch Linux

1. Install dotnet SDK and PowerShell:
   ```shell
   sudo pacman -S dotnet-sdk powershell
   ```
2. Generate stub assemblies:
   ```shell
   pwsh .github/scripts/gen-stubs.ps1
   ```
3. Build:
   ```shell
   dotnet build Dread.csproj -c Release
   ```
4. Package:
   ```shell
   pwsh ./build.ps1 -Version "$(grep version_number manifest.json | cut -d'"' -f4)"
   ```

### CI

The GitHub Actions CI workflow at `.github/workflows/verify.yml` runs the gen-stubs step then builds. Verify output passes all checks before opening or updating a PR.

```shell
# Run the CI steps locally in order:
pwsh .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release --nologo
```

## Pull Requests

- Open PRs against `master`.
- Ensure all CI checks pass (build + verify).
- Update `CHANGELOG.md` under `[Unreleased]` with your changes.
- The version number in `manifest.json` and `Plugin.cs` is bumped on release by CD pipeline.
