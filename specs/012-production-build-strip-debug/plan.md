# Implementation Plan: Production build strip debug (012)

**Branch**: `012-production-build-strip-debug` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Status**: Implementation complete (stub/Tier 0 green). Manual quickstart Matrix 3/4 pending in-game before `vpatch`.

**Input**: Feature specification from `specs/012-production-build-strip-debug/spec.md`

**Note**: Generated via `/speckit-plan`. `.specify/feature.json` pins this directory.

## Summary

Ship **compile-time production builds** for CD/Thunderstore: Release `Dread.dll` excludes debug overlay, TCP debug server, and test-crash systems via `DREAD_DEBUG` + MSBuild `Compile Remove`. Developer Debug builds (or `-p:EnableDebugFeatures=true`) retain full agent tooling. Error reporting and core gameplay unchanged. CI/CD add explicit production flags and a post-build string check on `Dread.dll`.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8, BepInEx 5.4, Harmony 2, MSBuild SDK-style project

**Primary Dependencies**: BepInEx, HarmonyLib, stub refs in `.github/stubs/refs`; no new NuGet packages

**Storage**: N/A (build-time conditioning only)

**Testing**: Tier 0 `scripts/verify-dread.ps1`, stub Release/Debug builds, CD post-build `strings` grep; manual MCP Tier 1 on Debug build per [quickstart.md](./quickstart.md)

**Target Platform**: R.E.P.O. mod DLL; CI/CD on Ubuntu (GitHub Actions)

**Project Type**: BepInEx plugin; build profile split in `Dread.csproj` + workflow scripts

**Performance Goals**: Smaller production DLL (debug IMGUI/TCP/thread code omitted); no runtime perf change for gameplay

**Constraints**: No manual version bump; stub Release build must pass; Thunderstore layout unchanged; no em dash in markdown

**Scale/Scope**: ~1 csproj, ~8 shared files with `#if`, 2 workflow files, verify script, docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Stub CI build | Required | Release AND Debug must compile against stubs |
| No manual version bump | Pass | CHANGELOG only |
| ADR-0016 registry | Pass | Debug rows wrapped in `#if DREAD_DEBUG` |
| ARCH-3 TryAddSystem | Pass | No new spawn sites |
| Manual verify (Principle II) | Pass | quickstart.md matrices |
| CD pipeline (Principle V) | Pass | Enhances existing Release build, no version edit |

**Pre-design**: PASS

**Post-design**: PASS (`#if` bridges are minimal; no new public API)

## Project Structure

### Documentation (this feature)

```text
specs/012-production-build-strip-debug/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── build-profile.md
│   └── debug-registry-conditional.md
├── spec.md
└── tasks.md
```

### Source Code (repository root)

```text
Dread.csproj                           # DreadDebug property, DefineConstants, Compile Remove

Config/DreadConfig.cs                  # #if DREAD_DEBUG for sections 8, 9, 11

Systems/DreadSystemRegistry.cs         # #if DREAD_DEBUG debug registrations

Systems/CampLureSystem.cs              # #if DREAD_DEBUG NotifyDebug
Systems/SnitchSystem.cs                # #if overlay notification
Systems/PsychoticBreak/PsychoticBreakSystem.cs  # ForceEpisodeForDebug
Systems/ErrorReporting/*.cs            # TestCrash report paths

.github/workflows/cd.yml               # EnableDebugFeatures=false + verify step + zip verify
.github/workflows/ci.yml               # Same production profile
.github/workflows/smoke-test.yml       # EnableDebugFeatures=false
.github/workflows/codeql.yml           # EnableDebugFeatures=false
build.ps1                              # Document Release=production; -DebugBuild switch
scripts/verify-dread.ps1               # Conditional debug registry check
docs/agents/guides/debug-tooling.md    # Production vs dev builds
CHANGELOG.md                           # [Unreleased] entry
```

**Structure Decision**: Single-plugin repo; build profile is MSBuild + preprocessor, not a second project or assembly.

## Complexity Tracking

No constitution violations requiring justification.
