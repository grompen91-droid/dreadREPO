# Implementation Plan: ERR-2 Error reporting default on + first-run prompt

**Branch**: `004-err-2-default-on-prompt` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-err-2-default-on-prompt/spec.md`

**Roadmap**: ERR-2 (P1) | **GitHub**: [#172](https://github.com/grompen91-droid/dreadREPO/issues/172)

## Summary

Ship **default-on** anonymous error reporting with a **one-time IMGUI disclosure prompt** on the first non-menu gameplay load. Reuse `ErrorReportingPrivacyCopy` (ERR-3) for all prompt text; add `ErrorReportingPromptShown` for once-only semantics; gate telemetry until acknowledgment; update ADR-0010 and player docs. No Worker or payload changes.

## Technical Context

**Language/Version**: C# (latest in project), .NET Framework 4.8

**Primary Dependencies**: BepInEx ConfigFile, `ErrorReportingPrivacyCopy`, `DreadSystemRegistry` / `DreadSystemInitializer`, Unity IMGUI, `SemiFunc.MenuLevel()` (game), existing `ErrorReporterSystem` pipeline

**Storage**: BepInEx cfg (`elytraking.dread.cfg` section `7. Error Reporting`)

**Testing**: Tier 0 `verify-dread.ps1`; `dotnet test tests/Dread.ErrorReportJson.Tests`; manual matrix in [quickstart.md](./quickstart.md); optional TestCrash with default true after prompt

**Target Platform**: R.E.P.O. (Windows / Proton / Linux); CI `ubuntu-latest` stubs

**Project Type**: BepInEx plugin (`Dread.csproj`)

**Performance Goals**: Prompt OnGUI only while pending; zero cost after dismissal

**Constraints**: ERR-3 merged first; no manifest/Plugin version bump; no em dash in markdown; stub CI; ARCH-3 registry for new system

**Scale/Scope**: ~4-6 C# files (`DreadConfig`, new prompt system, consent helper, queue/reporter gate, privacy copy strings), ADR + README updates

## Constitution Check

*GATE: `.specify/memory/constitution.md` is a template. Gates use `AGENTS.md` and `CONTEXT.md`.*

| Gate | Status |
|------|--------|
| Stub CI build passes | Required before merge |
| No manual `manifest.json` / `Plugin.VERSION` bump | Pass |
| Minimal diff; disclosure reuse (ERR-3) | Pass |
| Domain terms (Error reporting, cfg) | Pass |
| No em dash in markdown | Pass |
| ERR-3 dependency satisfied on merge base | Required |

**Post-design**: No unjustified violations. New cfg key and IMGUI surface are required for #172 acceptance; simpler than external UI mod.

## Project Structure

### Documentation (this feature)

```text
specs/004-err-2-default-on-prompt/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── first-run-prompt.md
│   └── config-keys.md
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Source Code (repository root)

```text
Config/DreadConfig.cs                          # default true, new PromptShown bind
Systems/ErrorReporting/
├── ErrorReportingPrivacyCopy.cs               # default-on copy pass
├── ErrorReportingPromptSystem.cs              # NEW: IMGUI first-run
├── ErrorReportingConsent.cs                   # NEW: gate helper (optional static)
├── ErrorReportLogQueue.cs                     # respect consent gate
└── ErrorReporterSystem.cs                     # if needed for gate
Systems/DreadSystemRegistry.cs                 # register prompt system
docs/adr/0010-error-telemetry.md               # default-on + prompt
CHANGELOG.md                                   # [Unreleased]
README.md, THUNDERSTORE_README.md
docs/mod-compatibility.md
```

**Structure Decision**: Single plugin; prompt is a small MonoBehaviour alongside existing error reporting folder.

## Phase 0: Research

**Status**: Complete. See [research.md](./research.md).

Resolved:

- Default true + separate `ErrorReportingPromptShown`
- IMGUI modal on first non-menu scene
- Upgrade path retains existing `false` until explicit opt-in
- Telemetry gated until prompt acknowledged
- ERR-3 strings reused; copy updated for default-on

## Phase 1: Design

**Status**: Complete.

### Contracts

- [contracts/first-run-prompt.md](./contracts/first-run-prompt.md): UI, buttons, consent gate, checklist
- [contracts/config-keys.md](./contracts/config-keys.md): cfg keys and defaults
- ERR-3 [privacy-copy.md](../003-err-3-privacy-copy/contracts/privacy-copy.md): update row 10 in implementation PR

### Data model

- [data-model.md](./data-model.md): `FirstRunPrompt`, cfg entities, `ErrorReportingConsent`

### Agent context

- `AGENTS.md` SPECKIT pointer updated to this plan (see `<!-- SPECKIT START -->` block).
- Implementation: link from `docs/agents/guides/error-reporting.md` when touched.

## Phase 2: Implementation (`tasks.md`)

**Not executed by `/speckit-plan`.** Suggested slices for `/speckit-tasks`:

| Slice | Scope |
|-------|--------|
| US1 cfg + copy | Default true, `PromptShown`, privacy copy default-on, ADR/README |
| US1 prompt UI | `ErrorReportingPromptSystem`, registry, menu gate |
| US1 consent gate | Queue/reporter block until prompt shown |
| US2 upgrade | Manual test matrix in quickstart |
| US3 later opt-out | Verify existing cfg path unchanged |

## Complexity Tracking

> No constitution violations requiring justification.
