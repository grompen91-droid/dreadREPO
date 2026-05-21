# Dread Mod - Agent Instructions

> Build, versioning, changelog, and GitHub rules live in **[CLAUDE.md](CLAUDE.md)**.
> Read that file first. This file adds agent-specific tooling on top of it.

---

## Agent Skills

### Issue Tracker

Issues live as GitHub issues in this repo. See `docs/agents/issue-tracker.md` for `gh` CLI conventions.

Repository: `grompen91-droid/dreadREPO`

### Triage Labels

Five-role label vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`.
See `docs/agents/triage-labels.md` for the full mapping.

### Domain Docs

Single-context layout. See `docs/agents/domain.md` for how to consume `CONTEXT.md` and `docs/adr/` before exploring the codebase.
