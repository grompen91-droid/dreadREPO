# Implementation guides (for agents)

Current-state reference for working in the Dread codebase. These replace the old `docs/superpowers/` plans and specs (now archived).

**When to use:** You have a GitHub issue or roadmap task and need patterns, file locations, and guardrails without reading 1,000-line historical checklists.

**When not to use:** Picking work or release process. Use [orchestration.md](../orchestration.md) and [docs/ROADMAP.md](../../ROADMAP.md) instead.

## Guides

| Guide | Topics | Supersedes (archive) |
|-------|--------|----------------------|
| [mod-architecture.md](mod-architecture.md) | Plugin boot, system hosts, config, netcode | `specs/2026-05-16-dread-design.md`, `plans/2026-05-16-dread.md` |
| [tension-and-proximity.md](tension-and-proximity.md) | Proximity scan, adrenaline, panic sprint, enemy lookup | `specs/2026-05-17-panic-sprint-design.md`, `specs/2026-05-22-decoupled-enemy-cache-design.md`, related plans |
| [harmony-and-patches.md](harmony-and-patches.md) | Apply/Remove patches, compat mode, host-only gates | `plans/2026-05-22-toggleable-harmony-patches.md` |
| [monster-overhaul.md](monster-overhaul.md) | Monster audio loop, aggression patches | Parts of original dread design spec |

## Archive

Original superpowers documents (checkbox plans, Windows paths, pre-ship design) live under [../archive/superpowers/](../archive/superpowers/). Read them only for archaeology or issue context that cites a specific file path.

## Related

- [CONTEXT.md](../../../CONTEXT.md): glossary terms
- [domain.md](../domain.md): ADRs and REPOConfig compat
- [docs/adr/](../../adr/): decisions (debug server, error reporting, removed systems)
