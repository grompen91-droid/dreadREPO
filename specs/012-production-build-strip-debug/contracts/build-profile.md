# Contract: Dread build profiles (production vs development)

**Feature**: 012 | **Extends**: [001 build-profiles](../../001-arch-2-reduce-reflection/contracts/build-profiles.md)

**Agent checklist:** [development-only-features.md](../../../docs/agents/guides/development-only-features.md)

## Production profile (CD, Thunderstore, CI default)

**Inputs**

| Property | Value |
|----------|--------|
| `Configuration` | `Release` |
| `EnableDebugFeatures` | `false` (explicit) |
| `DreadDebug` | `false` (derived) |
| `GameDir` | `{repo}/.github/stubs/refs` (CI) or local Managed |
| `BepInExDir` | stub or profile path |
| `DeployToProfile` | `false` (CI/CD) |

**Commands**

```bash
pwsh -NoProfile .github/scripts/gen-stubs.ps1
dotnet build Dread.csproj -c Release --nologo \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs \
  -p:EnableDebugFeatures=false \
  -p:DeployToProfile=false \
  -p:DeployToDist=false
```

**Post-build verify (CI/CD)**

```bash
bash .github/scripts/verify-production-dll.sh bin/Release/net48/Dread.dll
```

**Guarantees**

- Build completes with 0 errors against stubs.
- `Dread.dll` contains **no** `DebugServerSystem` or `DebugOverlaySystem` type names.
- `Dread.dll` contains `TestCrashSystem` (error-reporting verification).
- BepInEx config sections **8** and **9** are not registered; section **11. Testing** is registered.
- Error reporting and core gameplay systems present.

**Non-guarantees**

- MCP/Tier 1 TCP commands (requires development profile).

## Development profile (local agents, MCP verify)

**Inputs**

| Property | Value |
|----------|--------|
| `Configuration` | `Debug` **or** `Release` with `EnableDebugFeatures=true` |
| `DreadDebug` | `true` |
| `DefineConstants` | includes `DREAD_DEBUG` |

**Commands**

```bash
# Default dev build
dotnet build Dread.csproj -c Debug \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs

# Release + debug features (optional)
dotnet build Dread.csproj -c Release \
  -p:EnableDebugFeatures=true \
  -p:GameDir=.github/stubs/refs \
  -p:BepInExDir=.github/stubs/refs
```

**Guarantees**

- Debug overlay and TCP server compile and register.
- MCP server can connect when `DebugServerEnabled=true` in-game.
- TestCrash present (same as production).

## CD pipeline contract

| Job | Profile | Notes |
|-----|---------|-------|
| `build` | Production | `verify-production-dll.sh` on artifact DLL |
| `package` | Production | Thunderstore zip + same verify on zip `Dread.dll`; no `dread-mcp-server` in zip |
| `release` | Production | GitHub Release + Thunderstore publish |

No second artifact or zip for development builds in v1.

## Local packaging (`build.ps1`)

- Default: Production (same as CD).
- Optional `-DebugBuild`: `-c Debug` for modder/agent testing (overlay + TCP).

## Verification contract

| Tier | Profile required |
|------|------------------|
| Tier 0 stub build | Production (Release) |
| Tier 0 MCP npm build | N/A (repo tool) |
| Tier 1 MCP/TCP | Development DLL in game |
| In-game debug overlay | Development DLL |
| TestCrash button (section 11) | Production or development |

ARCH-2 stub profile MUST NOT regress; 012 adds production-specific agent exclusions on top.
