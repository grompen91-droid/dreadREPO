# Contract: Dread build profiles (ARCH-2)

**Amended by:** [012 production profile](../../012-production-build-strip-debug/contracts/build-profile.md) (2026-06-01)

## Stub profile (CI / agents without game)

**Inputs**

| Property | Value |
|----------|--------|
| `Configuration` | `Release` (production) or `Debug` (development) |
| `EnableDebugFeatures` | `false` (production, explicit in CI/CD) or `true` via `-c Debug` |
| `GameDir` | `{repo}/.github/stubs/refs` |
| `BepInExDir` | Profile or stub BepInEx core path |
| `DeployToProfile` | `false` (typical) |
| `DeployToDist` | `false` (typical) |

**Commands**

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
# Production (Thunderstore/CD default)
dotnet build Dread.csproj -c Release \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:EnableDebugFeatures=false \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
# Development (MCP/Tier 1)
dotnet build Dread.csproj -c Debug \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
pwsh -NoProfile ./scripts/verify-dread.ps1
```

**Guarantees**

- Build completes with 0 errors.
- `Dread.dll` is produced.
- Production Release build excludes debug overlay and TCP server (`DREAD_DEBUG` undefined). `TestCrashSystem` ships in production.
- CI runs `.github/scripts/verify-production-dll.sh` on Release `Dread.dll`.
- Runtime may still use reflection for optional mods and Unity UI; stub build does not execute game code.

**Non-guarantees**

- All game types resolve at compile time.
- Harmony patch targets exist in stub assemblies (patches use `TypeByName` + null checks).

## Full profile (local developer with R.E.P.O.)

**Inputs**

| Property | Value |
|----------|--------|
| `GameDir` | `{steam}/REPO/REPO_Data/Managed` (platform-specific) |
| `BepInExDir` | r2modman profile `BepInEx` folder |

**Commands**

```bash
dotnet build Dread.csproj -c Release \
  -p:GameDir="/path/to/REPO/REPO_Data/Managed" \
  -p:BepInExDir="$HOME/.config/r2modmanPlus-local/REPO/profiles/<profile>/BepInEx" \
  -p:PluginDir="$HOME/.config/r2modmanPlus-local/REPO/profiles/<profile>/BepInEx/plugins/elytraking-Dread" \
  -p:DeployToProfile=true
```

**Guarantees**

- Stronger compile-time binding to real game assemblies.
- Deploy target copies `Dread.dll` and plugin deps (no bundled `audio/`; use seeded `audio-cache` or future `BundleAudio=true`).

## Verification contract

| Tier | Stub required | Full optional |
|------|---------------|---------------|
| Tier 0 | Yes | No |
| Tier 1 MCP | No | Yes (game running) |
| In-game smoke | No | Yes |

ARCH-2 PR must not regress **Stub profile** Tier 0.
