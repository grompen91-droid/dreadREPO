# Agent orchestration (Dread repo)

Entry point for coding agents (Cursor, Claude Code, Cloud Agents, and similar). Human contributors can use the same map; start at [CONTRIBUTING.md](../../CONTRIBUTING.md) for PR conventions.

## Start here

| Step | Doc | Why |
|------|-----|-----|
| 1 | [CONTEXT.md](../../CONTEXT.md) | Domain vocabulary (use these terms in issues and PRs) |
| 2 | [domain.md](domain.md) | How to read ADRs, roadmap, and repo layout |
| 3 | [docs/ROADMAP.md](../ROADMAP.md) | Backlog IDs, execution order, linked GitHub issues |
| 4 | [orchestration.md](orchestration.md) | End-to-end workflows (pick work, implement, verify, ship) |
| 5 | [AGENTS.md](../../AGENTS.md) | Build, release tags, changelog, Thunderstore rules |

## File index

| File | Role |
|------|------|
| [orchestration.md](orchestration.md) | Workflows: solo agent, multi-subagent, verify tiers, PR checklist |
| [issue-tracker.md](issue-tracker.md) | `gh` CLI for issues (create, triage, comment, close) |
| [triage-labels.md](triage-labels.md) | Label vocabulary (`ready-for-agent`, etc.) |
| [domain.md](domain.md) | ADR consumption, debug overlay, REPOConfig compat |
| [verify-dread.md](verify-dread.md) | Autonomous verify runbook (Tier 0 to 3) |
| [verify-dread-checklist.json](verify-dread-checklist.json) | Machine-readable verify steps |
| [error-reporting-test-checklist.md](error-reporting-test-checklist.md) | Manual ERR-1 matrix (in-game + MCP) |
| [../../AGENTS.md](../../AGENTS.md) | Build stubs, CI lint, version bump policy |
| [../../.cursor/mcp.json](../../.cursor/mcp.json) | MCP stdio config for `dread` debug tools |
| [../../dread-mcp-server/](../../dread-mcp-server/) | TypeScript MCP bridge (build before Tier 1) |
| [../../.claude/](../../.claude/) | Subagent prompt templates (implementer + reviewers) |
| [../superpowers/plans/](../superpowers/plans/) | Historical implementation plans (checkbox tracking) |

## MCP debug bridge

When the game runs with `DebugServerEnabled=true`:

1. Build MCP: `cd dread-mcp-server && npm install && npm run build`
2. Cursor loads `.cursor/mcp.json` (stdio to `dread-mcp-server/dist/index.js`)
3. Follow [verify-dread.md](verify-dread.md) Tier 1 tool sequence

Cloud VMs without R.E.P.O. still run **Tier 0** via `scripts/verify-dread.ps1`.

## Multi-subagent prompts

Claude Code (and similar) can spawn specialized subagents using templates under `.claude/`:

| Prompt | Role |
|--------|------|
| [implementer-prompt.md](../../.claude/implementer-prompt.md) | TDD fix for a scoped GitHub issue |
| [spec-reviewer-prompt.md](../../.claude/spec-reviewer-prompt.md) | Spec compliance review |
| [code-quality-reviewer-prompt.md](../../.claude/code-quality-reviewer-prompt.md) | Quality and safety review |

See [orchestration.md](orchestration.md) for when to chain these roles.

## Picking work

1. Open [docs/ROADMAP.md](../ROADMAP.md) **Execution order** (P0 first).
2. Find a row with an open GitHub issue link.
3. Confirm the issue has `ready-for-agent` (see [triage-labels.md](triage-labels.md)).
4. Comment on the issue before large edits.
5. Reference the roadmap ID in the PR title or body (e.g. `DBG-1`, `ARCH-1`).

Suggested starter issues (from CONTRIBUTING): #171, #170, #167.
