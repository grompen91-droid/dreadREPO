# Development-only features (agent checklist)

Use this guide when you add **agent/MCP tooling** that must **not** ship on Thunderstore or CD releases.

**Related:** [debug-tooling.md](debug-tooling.md), [config-and-logging.md](config-and-logging.md), [DreadConfigSections.cs](../../../Config/DreadConfigSections.cs), spec [012 build profile](../../../specs/012-production-build-strip-debug/contracts/build-profile.md).

## Config section numbering

Section numbers are **not fixed** across build profiles. Use [DreadConfigSections.cs](../../../Config/DreadConfigSections.cs) for every `cfg.Bind` section string (and MCP `get_config` sections in `DebugServerSystem`).

| Section | Production (`DREAD_DEBUG` off) | Development (`DREAD_DEBUG` on) |
|---------|----------------------------------|--------------------------------|
| 1-7 | unchanged | unchanged |
| Debug Overlay | *(absent)* | 8 |
| Debug Server | *(absent)* | 9 |
| Logging | **8** | 10 |
| Testing (TestCrash) | *(absent)* | 11 |

**Never** hardcode `"10. Logging"` in shared code: production builds must bind `"8. Logging"`.

**REPOConfig bind order:** In `DreadConfig.Initialize`, bind sections in **ascending numeric order** (e.g. `10. Logging` before `11. Testing`). If a higher section is bound first, REPOConfig can nest the next lower section under it (shows as `11.10` instead of separate `10` / `11` headers).

## Two build profiles

| Profile | When | `DREAD_DEBUG` |
|---------|------|---------------|
| **Production** | CD, `build.ps1`, CI `-p:EnableDebugFeatures=false` | undefined |
| **Development** | `-c Debug`, `build.ps1 -DebugBuild`, `-p:EnableDebugFeatures=true` | defined |

## What ships in production

| Surface | Production |
|---------|------------|
| Core gameplay + error reporting | yes |
| `LogLevel` (section **8. Logging**) | yes |
| Debug overlay, TCP server, TestCrash | no |

## Checklist: new development-only feature

### 1. New `.cs` file

Add to **`Dread.csproj`** `Compile Remove` when `DreadDebug != true` (same item group as overlay/server/test-crash).

### 2. Config (`Config/DreadConfig.cs`)

- Fields and `cfg.Bind` inside `#if DREAD_DEBUG`.
- Section header from **`DreadConfigSections`** (add a new constant in [DreadConfigSections.cs](../../../Config/DreadConfigSections.cs) if needed).
- If you insert a new dev-only section **before** Logging, bump the `#else` branch number for `Logging` in `DreadConfigSections.cs`.

### 3. Registry (`Systems/DreadSystemRegistry.cs`)

Register inside `#if DREAD_DEBUG` with `SystemOrderGroup.Debug`.

### 4. Shared gameplay code

Wrap debug-only calls in `#if DREAD_DEBUG`.

### 5. Verify

```bash
dotnet build Dread.csproj -c Release -p:EnableDebugFeatures=false \
  -p:GameDir=.github/stubs/refs -p:BepInExDir=.github/stubs/refs \
  -p:DeployToProfile=false -p:DeployToDist=false
bash .github/scripts/verify-production-dll.sh bin/Release/net48/Dread.dll
```

### 6. CI agent type list

New dev-only **type names** go in `agent_debug_types` in [.github/scripts/verify-production-dll.sh](../../../.github/scripts/verify-production-dll.sh).

## Production-only config (example: Logging)

Bind with `DreadConfigSections.Logging` **outside** `#if DREAD_DEBUG`. The section constant resolves to `8. Logging` or `10. Logging` automatically.

## Common mistakes

| Mistake | Symptom |
|---------|---------|
| Hardcoded `"10. Logging"` in production | Config shows section 10 with gaps (8-9 missing) |
| TestCrash outside `#if` / not in `Compile Remove` | Crash Game in Thunderstore build |
| Forgot to update `DreadConfigSections` | Wrong section order in one profile |
