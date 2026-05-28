# Agent orchestration workflows

How autonomous agents should move from issue to merged PR in this repo. Pair with [README.md](README.md) for the file map.

## Workflow A: Solo agent (default)

Use for scoped bugs, docs, or single-file changes.

```
ROADMAP / GitHub issue (ready-for-agent)
    -> CONTEXT.md + domain.md + relevant ADRs
    -> feature branch (cursor/<name>-3dd3 for Cloud Agents)
    -> implement (minimal diff, repo conventions)
    -> Tier 0 verify (scripts/verify-dread.ps1)
    -> dotnet format (if C# touched)
    -> commit + push + PR to master
    -> CHANGELOG [Unreleased] if user-facing
```

### Branch naming (Cloud Agents)

Cloud Agent tasks use:

`cursor/<short-description>-3dd3`

Example: `cursor/improve-agent-orchestration-3dd3`

### PR checklist

- [ ] Issue linked (`Fixes #NNN` or `Related to #NNN`)
- [ ] Roadmap ID in body when applicable
- [ ] Glossary terms from CONTEXT.md in description
- [ ] No manual version edits in `manifest.json`, `Plugin.cs`, README version badges
- [ ] Tier 0 verify passed (or failure documented with reason)
- [ ] `[Unreleased]` updated in CHANGELOG.md for notable changes

## Workflow B: Multi-subagent (Claude Code)

Use for larger features where implement and review should stay separate.

```
Issue + plan (optional: docs/superpowers/plans/*.md)
    -> Subagent 1: implementer (.claude/implementer-prompt.md)
    -> Subagent 2: spec reviewer (.claude/spec-reviewer-prompt.md)
    -> Subagent 3: code quality reviewer (.claude/code-quality-reviewer-prompt.md)
    -> Human or lead agent: merge feedback, Tier 0 verify, PR
```

**Implementer** must:

- Read CONTEXT.md and domain.md before coding
- Follow AGENTS.md build rules (stubs on Linux)
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

## Superpowers plans

Older feature plans live under `docs/superpowers/plans/`. They are **historical** checklists, not the live backlog. Prefer ROADMAP + GitHub issues for current work. Plans may still help for step-by-step refactors when an issue explicitly references them.
