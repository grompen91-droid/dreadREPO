# Implementation Plan: ARCH-2 Reduce reflection and DLL surface

**Branch**: `001-arch-2-reduce-reflection` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-arch-2-reduce-reflection/spec.md`

**Roadmap**: ARCH-2 (P1) | **GitHub**: [#168](https://github.com/grompen91-droid/dreadREPO/issues/168)

## Summary

Inventory every reflection and Harmony `AccessTools` resolution site in `Systems/`, classify by necessity and hot path, then apply **safe** compile-time or caching improvements without breaking stub CI builds or optional-mod paths (REPOConfig, MenuLib). Document stub vs full MSBuild profiles for agents and contributors. No player-facing behavior changes unless fixing an existing defect.

## Technical Context

**Language/Version**: C# (latest in project), .NET Framework 4.8

**Primary Dependencies**: BepInEx 5.4.x, Harmony 2, Unity engine/game assemblies (stub or full), NVorbis

**Storage**: N/A (build artifacts in `bin/Release/net48/`)

**Testing**: `scripts/verify-dread.ps1` Tier 0; `tests/Dread.ErrorReportJson.Tests`; optional Tier 1 MCP + in-game smoke on full build

**Target Platform**: R.E.P.O. (Windows / Proton/Linux); CI on `ubuntu-latest`

**Project Type**: BepInEx plugin (single `Dread.csproj`)

**Performance Goals**: No new per-frame reflection; reduce or document existing hot-path sites

**Constraints**: Stub build must pass CI; soft optional-mod dependencies; no manual version string edits; ARCH-1 file layout assumed merged

**Scale/Scope**: ~15 C# files with reflection; inventory + targeted edits + docs

## Constitution Check

*GATE: Project `.specify/memory/constitution.md` is a template. Gates below use `AGENTS.md` and `CONTEXT.md`.*

| Gate | Status |
|------|--------|
| Stub CI build passes | Required before merge |
| No manual `manifest.json` / `Plugin.VERSION` bump | Pass |
| Minimal diff per subsystem | Pass (implement in slices) |
| Domain terms from CONTEXT.md in docs/PR | Pass |
| No em dash in markdown | Pass |

**Post-design**: No constitution violations; complexity table empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-arch-2-reduce-reflection/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── build-profiles.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
Dread.csproj
Plugin.cs
Systems/
├── Patches/              # Harmony Apply/Remove (AccessTools)
├── PsychoticBreak/       # UI reflection (overlay)
├── ErrorReporting/
├── DebugOverlay/
├── RepoConfigSliderLabelCompat.cs
├── HarmonyPatchCompat.cs
├── PlayerControllerCompat.cs
├── PlayerTumbleCompat.cs
├── DreadSystemInitializer.cs
├── AudioClipLoader.cs
└── ...
.github/stubs/refs/      # Stub assemblies
docs/agents/guides/
├── mod-architecture.md   # Extend stub vs full
└── reflection-inventory.md  # NEW (ARCH-2 deliverable)
```

**Structure Decision**: Single-plugin repo; ARCH-2 touches `Systems/` and agent docs only.

## Phase 0: Research

**Status**: Complete. See [research.md](./research.md).

Resolved:

- Stub vs full profiles documented in [contracts/build-profiles.md](./contracts/build-profiles.md)
- Tiered keep / reduce / replace strategy
- `stub-build.marker` deferred; MSBuild `GameDir` is source of truth
- Hot-path audit list (overlay patch count, compat layers, error capture)

## Phase 1: Design

**Status**: Complete.

### Reflection inventory (initial scan)

| Area | File(s) | Trigger | Disposition (planned) |
|------|---------|---------|------------------------|
| Harmony patches | `Systems/Patches/*.cs` | Startup | Keep `TypeByName` or replace if stub defines type |
| REPOConfig compat | `RepoConfigSliderLabelCompat.cs` | On slider create | **Keep** until DBG-4 |
| Psychotic break UI | `PsychoticBreak/PsychoticBreakOverlay.cs` | Episode | **Keep** (UI not in stubs) |
| Init defer UI | `DreadSystemInitializer.cs` | Startup | **Keep** (load `UnityEngine.UI`) |
| Optional mod API | `HarmonyPatchCompat.cs` | Patch apply | **Keep** (`SemiFunc`) |
| Player compat | `PlayerControllerCompat.cs`, `PlayerTumbleCompat.cs` | Gameplay | Inventory; **reduce** cache if hot |
| Debug overlay | `DebugOverlay/DebugOverlayPanel.cs` | When visible | **Keep** (document; PERF-2 gated) |
| Debug server | `DebugServerSystem.cs` | MCP command | Inventory `ReadPlayerFloat` |
| Audio loader | `AudioClipLoader.cs` | Load | Inventory |
| Error capture | `ErrorReporting/ErrorReportPayloadCapture.cs` | Log batch | Inventory `Find*` usage |

Full table with line-level detail: implement in `docs/agents/guides/reflection-inventory.md` during `/speckit-tasks` / implement phase.

### Contracts

- [contracts/build-profiles.md](./contracts/build-profiles.md): stub vs full MSBuild inputs and verification tiers

### Data model

- [data-model.md](./data-model.md): `BuildProfile`, `ReflectionSite`, `CompileTimeRef`

### Agent context

- Add to [docs/agents/README.md](../../docs/agents/README.md) index: link `reflection-inventory.md` and ARCH-2 spec folder when inventory lands.
- Optional: `<!-- SPECKIT START -->` block in `AGENTS.md` pointing to this plan (if project adopts speckit pointer convention).

## Phase 2: Implementation planning (outline for `/speckit-tasks`)

**Not executed by `/speckit-plan`.** Suggested task slices:

1. **T1**: Add `reflection-inventory.md` (complete table, no code change).
2. **T2**: Document stub vs full in `mod-architecture.md` + `quickstart` cross-links.
3. **T3**: Patch types: evaluate `typeof` vs `TypeByName` per stub coverage.
4. **T4**: Compat layers: cache reflection handles where profiling shows cost (if any).
5. **T5**: Verify Tier 0 + ErrorReportJson tests; CHANGELOG; roadmap ARCH-2 → done.

**Dependencies**: ARCH-1 merged preferred; can proceed on branch rebased onto master.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | | |

## Risks

| Risk | Mitigation |
|------|------------|
| Breaking REPOConfig compat | Keep `RepoConfigSliderLabelCompat` reflection; DBG-4 owns removal |
| Breaking psychotic break on stub | Do not remove UI reflection without stub UI types |
| False sense of "zero reflection" | Inventory explicitly marks required sites |
| ARCH-1 branch drift | Rebase `001-arch-2-reduce-reflection` onto master before implement |

## Success verification

- [ ] [spec.md](./spec.md) success criteria met
- [ ] Issue #168 acceptance checkboxes satisfied in PR description
- [ ] `pwsh ./scripts/verify-dread.ps1` Tier 0 pass on stub build
