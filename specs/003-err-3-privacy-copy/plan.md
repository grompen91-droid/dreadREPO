# Implementation Plan: ERR-3 Error reporting privacy copy

**Branch**: `003-err-3-privacy-copy` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-err-3-privacy-copy/spec.md`

**Roadmap**: ERR-3 (P1) | **GitHub**: [#173](https://github.com/grompen91-droid/dreadREPO/issues/173)

## Summary

Ship **accurate, reviewable privacy disclosure** for opt-in error reporting: one canonical English copy source, wired into `DreadConfig.ErrorReportingEnabled` description and documented in a privacy contract mapped to `ErrorReportPayloadCapture`. No default or first-run behavior change (ERR-2). Optional minimal in-game log line only if implementer chooses P2; ERR-2 reuses the same strings for its prompt.

## Technical Context

**Language/Version**: C# (latest in project), .NET Framework 4.8

**Primary Dependencies**: BepInEx ConfigFile, existing `DreadConfig`, `Systems/ErrorReporting/*`

**Storage**: N/A (strings in code; cfg on disk unchanged except description text)

**Testing**: Manual copy review per [contracts/privacy-copy.md](./contracts/privacy-copy.md); Tier 0 `verify-dread.ps1`; `dotnet test tests/Dread.ErrorReportJson.Tests` (no new tests required unless copy asserts in golden)

**Target Platform**: R.E.P.O. (Windows / Proton / Linux); CI stub build

**Project Type**: BepInEx plugin (`Dread.csproj`)

**Performance Goals**: Zero per-frame cost; strings allocated once at config bind

**Constraints**: No version bumps; no em dash in markdown; ERR-1 dependency for payload truth; ERR-2 out of scope

**Scale/Scope**: ~2-4 files (`Config/DreadConfig.cs`, new `PrivacyDisclosure` or `ErrorReportingCopy.cs`, docs), optional README touch

## Constitution Check

*GATE: `.specify/memory/constitution.md` is a template. Gates use `AGENTS.md` and `CONTEXT.md`.*

| Gate | Status |
|------|--------|
| Stub CI build passes | Required before merge |
| No manual `manifest.json` / `Plugin.VERSION` bump | Pass |
| Minimal diff; copy/disclosure only | Pass |
| No em dash in markdown | Pass |
| ERR-2 default/prompt deferred | Pass |

**Post-design**: No violations; no new systems or Harmony patches.

## Project Structure

### Documentation (this feature)

```text
specs/003-err-3-privacy-copy/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── privacy-copy.md
│   └── config-keys.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
Config/DreadConfig.cs                    # ErrorReportingEnabled description uses canonical copy
Systems/ErrorReporting/                  # Optional ErrorReportingPrivacyCopy.cs (NEW)
docs/agents/guides/error-reporting.md    # Link to contract + canonical bullets
README.md / THUNDERSTORE_README.md       # Align telemetry bullets if edited
```

**Structure Decision**: Single plugin; disclosure is config + docs, not a new runtime system.

## Phase 0: Research

**Status**: Complete. See [research.md](./research.md).

Resolved:

- Primary v1 surface: BepInEx config description (+ full text in cfg file)
- In-game modal: ERR-2; ERR-3 exports strings only
- Localization: English only
- Payload truth source: `ErrorReportPayloadCapture`, ADR-0010, golden tests

## Phase 1: Design

**Status**: Complete.

| Artifact | Path |
|----------|------|
| Data model | [data-model.md](./data-model.md) |
| Privacy contract | [contracts/privacy-copy.md](./contracts/privacy-copy.md) |
| Config keys | [contracts/config-keys.md](./contracts/config-keys.md) |
| Quickstart | [quickstart.md](./quickstart.md) |

## Phase 2: Implementation (for /speckit-tasks)

**Not created by speckit-plan.** Expected task slices:

1. Add canonical copy class/constants per contract.
2. Update `DreadConfig.ErrorReportingEnabled` bind description.
3. Manual checklist: compare copy to payload capture line-by-line.
4. Touch `error-reporting.md` / README only where stale vs ADR-0010.
5. CHANGELOG `[Unreleased]` entry under Added/Changed.

## Dependencies and blockers

| Item | Role |
|------|------|
| ERR-1 [#171](https://github.com/grompen91-droid/dreadREPO/issues/171) | Assumes payload and tests stable on master |
| ERR-2 [#172](https://github.com/grompen91-droid/dreadREPO/issues/172) | Blocked until ERR-3 merges; consumes canonical strings |
| ARCH-3 | No hard dependency; optional registry doc link only |

**Blockers**: None for planning. Implementation should wait until ERR-1 checklist is green on target merge base if payload still in flux.
