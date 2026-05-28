# Domain Docs

How agents and skills should consume this repo's domain documentation when exploring the codebase.

**Orchestration hub:** [README.md](README.md) and [orchestration.md](orchestration.md). **Implementation patterns:** [guides/README.md](guides/README.md).

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, or
- **`CONTEXT-MAP.md`** at the repo root if it exists -- it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`docs/adr/`** -- read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.
- **`docs/ROADMAP.md`** -- planned work (debug overlay, refactor, performance, error reporting). Not shipped yet.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The producer skill (`/grill-with-docs`) creates them lazily when terms or decisions actually get resolved.

## File structure

Single-context repo (most repos):

```
/
├── CONTEXT.md
├── docs/adr/
│   ├── 0001-event-sourced-orders.md
│   └── 0002-postgres-for-write-model.md
└── src/
```

Multi-context repo (presence of `CONTEXT-MAP.md` at the root):

```
/
├── CONTEXT-MAP.md
├── docs/adr/                          ← system-wide decisions
└── src/
    ├── ordering/
    │   ├── CONTEXT.md
    │   └── docs/adr/                  ← context-specific decisions
    └── billing/
        ├── CONTEXT.md
        └── docs/adr/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal -- either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/grill-with-docs`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders) -- but worth reopening because..._

## In-game debug overlay

`DebugOverlaySystem` (config `11. Debug Overlay` / `DebugOverlayEnabled`) draws an IMGUI HUD during runs. Live values come from `DreadRuntimeState`, populated by `TensionSystem`, `PsychoticBreakSystem`, and `AudioDreadSystem` on their existing 0.5s cadence. F10 toggles visibility when the config entry is on. For remote tooling, prefer `DebugServerSystem` (ADR-0013).

## REPOConfig slider label compat (temporary)

**Problem:** REPOConfig calls `MenuAPI.CreateREPOSlider(name, string.Empty, ...)`. MenuLib's microphone slider template sets the name on `labelTMP` (`Element Name`) but that column is off-layout; bool toggles use a different template and look fine.

**Mitigation:** `RepoConfigSliderLabelCompat` (only if `REPOConfig` assembly is loaded):

- Postfix `CreateREPOSlider` when description is empty: set `labelTMP.text`, hide `descriptionTMP`, label X = 100, force compact row via `HandleDescription` postfix.
- Applied from `Plugin.Start()` and `DreadSystemInitializer` after MenuLib loads (reflection, no hard dependency).
- **Not a permanent solution:** magic X offset and custom TMP layout; upstream REPOConfig/MenuLib should pass description or align slider rows like toggles.

**Docs:** `docs/repo-config-slider-labels-investigation.md` (full debug timeline, rejected approaches, log evidence).

**Default path without REPOConfig:** `elytraking.dread.cfg` or BepInEx Configuration Manager.
