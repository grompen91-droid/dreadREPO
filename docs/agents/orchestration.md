# Agent orchestration workflows

How autonomous agents should move from issue to merged PR in this repo. Pair with [README.md](README.md) for the file map.

## Workflow A: Solo agent (default)

Use for scoped bugs, docs, or single-file changes.

```
ROADMAP / GitHub issue (ready-for-agent)
    -> CONTEXT.md + domain.md + relevant ADRs
    -> feature branch off master (AGENTS.md: Spec Kit NNN-kebab, or fix/... / feat/...)
    -> implement (minimal diff, repo conventions)
    -> meaningful git commits on the branch (not one giant commit at the end)
    -> Debug build + Tier 0 verify (scripts/verify-dread.ps1)
    -> dotnet format (if C# touched)
    -> push only when opening/updating PR or user asks; PR to master when complete
    -> CHANGELOG [Unreleased] if user-facing
```

### Branch naming

| Situation | Branch |
|-----------|--------|
| Active Spec Kit plan (`.specify/feature.json`) | `NNN-kebab-name` matching `specs/NNN-.../` |
| Other features / fixes | `feat/short-description` or `fix/short-description` (kebab-case) |

Cloud Agents may still use `cursor/<short-description>-3dd3` when that is how the task was spawned; prefer Spec Kit naming when a plan is active.

### PR checklist

- [ ] Issue linked (`Fixes #NNN` or `Related to #NNN`)
- [ ] Roadmap ID in body when applicable
- [ ] Glossary terms from CONTEXT.md in description
- [ ] No manual version edits in `manifest.json`, `Plugin.cs`, README version badges
- [ ] Tier 0 verify passed (or failure documented with reason)
- [ ] If agent-only surface added: `development-only-features.md` checklist completed; production build verified
- [ ] `[Unreleased]` updated in CHANGELOG.md for notable changes

## Workflow B: Multi-subagent (Claude Code)

Use for larger features where implement and review should stay separate.

```
Issue + guide (optional: docs/agents/guides/*.md)
    -> Subagent 1: implementer (.claude/implementer-prompt.md)
    -> Subagent 2: spec reviewer (.claude/spec-reviewer-prompt.md)
    -> Subagent 3: code quality reviewer (.claude/code-quality-reviewer-prompt.md)
    -> Human or lead agent: merge feedback, Tier 0 verify, PR
```

**Implementer** must:

- Read CONTEXT.md and domain.md before coding
- Follow AGENTS.md build rules (stubs on Linux)
- Agent-only code: [guides/development-only-features.md](guides/development-only-features.md) (`DREAD_DEBUG`, `Compile Remove`, config sections 8-9)
- Return `DONE` or `DONE_WITH_CONCERNS` with git SHA

**Reviewers** must:

- Reference file:line for each finding
- Return explicit APPROVED or ISSUES

Do not skip Tier 0 after subagent handoff; reviewers do not run the full build by default.

## Workflow C: Live game verify (Tier 1+)

Requires R.E.P.O. with Dread installed and debug server on.

1. Enable `debugServer.enabled` (restart game)
2. `dread_ping` then `dread_verify` via MCP (see [verify-dread.md](verify-dread.md))
3. Optional: [error-reporting-test-checklist.md](error-reporting-test-checklist.md) for ERR-1
4. `dread_shutdown` when finished

Agents without a game session stop at Tier 0 and note that in the PR.

## Workflow D: Release (maintainer / CD)

Agents do **not** bump version strings manually.

1. Ensure CHANGELOG `[Unreleased]` is complete
2. Push trigger tag: `vpatch`, `vminor`, or `vmajor` (see AGENTS.md)
3. CD pipeline bumps versions, builds, publishes Thunderstore

## Issue triage (agents)

| Label | Agent action |
|-------|----------------|
| `needs-triage` | Do not implement; wait for maintainer |
| `needs-info` | Ask reporter; do not guess requirements |
| `ready-for-agent` | Safe to implement when spec is clear |
| `ready-for-human` | Do not auto-implement (manual or design) |
| `wontfix` | Close; no PR |

Full mapping: [triage-labels.md](triage-labels.md). CLI: [issue-tracker.md](issue-tracker.md).

## When to write an ADR

- New cross-cutting protocol (debug server, error reporting)
- Host vs client behavior change
- Compatibility contract with other mods

Put ADRs in `docs/adr/`. Mention conflicts in PR body if code diverges from an ADR.

## Implementation guides

Polished, current-state docs live under `docs/agents/guides/`. Use them for architecture, tension/proximity, Harmony patches, and monster overhaul.

Archived checkbox plans from `docs/superpowers/` are under `docs/agents/archive/superpowers/`. Do not execute archived tasks (Windows paths, removed systems, already-shipped work). Prefer ROADMAP + GitHub issues for backlog.
