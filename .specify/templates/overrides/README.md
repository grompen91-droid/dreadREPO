# Template overrides

Spec Kit resolves templates in this order (see `.specify/scripts/bash/common.sh`):

1. `.specify/templates/overrides/<name>.md` (this folder)
2. Presets / extensions / shared core templates

Overrides **replace** the entire template file. There is no partial merge with the core `plan-template.md`.

## Current status

No overrides are checked in yet. The core templates under `.specify/templates/` are used as-is.

## When to add an override

Add `plan-template.md` here only when you are willing to maintain a **full** copy of the plan template with Dread defaults baked in. Do not copy templates only to change one section unless you commit to syncing upstream template changes manually.

## Suggested Dread `Technical Context` block

Paste this into **Technical Context** when filling a plan (or embed it in a future full `plan-template.md` override):

```markdown
**Language/Version**: C# / .NET Framework 4.8, BepInEx 5.4, Harmony 2

**Primary Dependencies**: Unity (game), BepInEx, Harmony; optional REPOConfig (soft)

**Storage**: N/A (in-memory + BepInEx cfg on disk)

**Testing**: Tier 0 `./scripts/verify-dread.ps1`; manual quickstart matrix; MCP `dread_verify` when game running

**Target Platform**: R.E.P.O. (Windows player); agent/CI build via `.github/stubs/refs`

**Project Type**: BepInEx mod DLL (`Dread.dll`)

**Constraints**: Host authority for monsters (ADR-0004); compat via `Systems/Core/` (ADR-0016); stub-safe CI build

**Scale/Scope**: [feature-specific]
```

## Suggested Constitution Check table

| Gate | Status |
|------|--------|
| Stub CI build | Pass / Fail |
| No manual version bump | Pass |
| Host authority respected | Pass / N/A |
| Core compat used (no stray game reflection) | Pass / N/A |
| ARCH-3 registry (no `TryAddSystem` in Plugin) | Pass / N/A |
| Quickstart / manual verify documented | Pass |
