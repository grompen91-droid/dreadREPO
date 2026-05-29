# Implementation guides (for agents)

Current-state reference for working in the Dread codebase. These replace the old `docs/superpowers/` plans and specs (now archived).

**When to use:** You have a GitHub issue or roadmap task and need patterns, file locations, and guardrails without reading 1,000-line historical checklists.

**When not to use:** Picking work or release process. Use [orchestration.md](../orchestration.md) and [docs/ROADMAP.md](../../ROADMAP.md) instead.

## Guide index

| Guide | Topics |
|-------|--------|
| [mod-architecture.md](mod-architecture.md) | Plugin boot, system hosts, netcode, adding systems |
| [reflection-inventory.md](reflection-inventory.md) | ARCH-2: reflection sites, stub/full, hot paths |
| [audio-dread-and-loading.md](audio-dread-and-loading.md) | Ambient audio, OGG loading, NVorbis, weights |
| [monster-overhaul.md](monster-overhaul.md) | Monster audio loop, aggression patches |
| [tension-and-proximity.md](tension-and-proximity.md) | Proximity scan, adrenaline, panic sprint |
| [psychotic-break.md](psychotic-break.md) | Episode triggers, solo/LoS/crouch, debug force |
| [harmony-and-patches.md](harmony-and-patches.md) | Apply/Remove, compat skip, host-only |
| [error-reporting.md](error-reporting.md) | Worker pipeline, opt-in telemetry, tests |
| [debug-tooling.md](debug-tooling.md) | TCP debug server, MCP, overlay, TestCrash |
| [config-and-logging.md](config-and-logging.md) | DreadConfig sections, LoggingService |
| [compatibility.md](compatibility.md) | Compatibility mode, REPOConfig, optional mods |

## By task type

| You need to… | Start here |
|--------------|------------|
| Orient in repo | [mod-architecture.md](mod-architecture.md) |
| Change gameplay near enemies | [tension-and-proximity.md](tension-and-proximity.md) or [monster-overhaul.md](monster-overhaul.md) |
| Patch game methods | [harmony-and-patches.md](harmony-and-patches.md) |
| Add sounds | [audio-dread-and-loading.md](audio-dread-and-loading.md) |
| Psychotic break behavior | [psychotic-break.md](psychotic-break.md) |
| Crashes / GitHub auto-issues | [error-reporting.md](error-reporting.md) |
| Agent verify / MCP | [debug-tooling.md](debug-tooling.md) + [verify-dread.md](../verify-dread.md) |
| New config knob | [config-and-logging.md](config-and-logging.md) |
| Mod conflicts | [compatibility.md](compatibility.md) + [mod-compatibility.md](../../mod-compatibility.md) |
| Reduce reflection / stub CI | [reflection-inventory.md](reflection-inventory.md), [mod-architecture.md](mod-architecture.md) build profiles |

## Archive (superseded)

| Archive path | Replaced by |
|--------------|-------------|
| `../archive/superpowers/specs/2026-05-16-dread-design.md` | [mod-architecture.md](mod-architecture.md) |
| `../archive/superpowers/plans/2026-05-16-dread.md` | [mod-architecture.md](mod-architecture.md) (do not execute tasks) |
| `../archive/superpowers/specs/2026-05-17-panic-sprint-design.md` | [tension-and-proximity.md](tension-and-proximity.md) |
| `../archive/superpowers/plans/2026-05-17-panic-sprint.md` | [tension-and-proximity.md](tension-and-proximity.md) |
| `../archive/superpowers/specs/2026-05-22-decoupled-enemy-cache-design.md` | [tension-and-proximity.md](tension-and-proximity.md) |
| `../archive/superpowers/plans/2026-05-22-decoupled-enemy-cache.md` | [tension-and-proximity.md](tension-and-proximity.md) |
| `../archive/superpowers/plans/2026-05-22-toggleable-harmony-patches.md` | [harmony-and-patches.md](harmony-and-patches.md) |

Original superpowers files live under [../archive/superpowers/](../archive/superpowers/). Read only for archaeology.

## Related

- [CONTEXT.md](../../../CONTEXT.md): glossary terms
- [domain.md](../domain.md): ADRs and REPOConfig compat
- [docs/adr/](../../adr/): architecture decisions
