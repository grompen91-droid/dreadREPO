# Spec Kit (`.specify/`) for Dread

This folder configures [Spec Kit](https://github.com/github/spec-kit) style spec-driven development for the Dread R.E.P.O. mod repo.

## What lives here

| Path | Role |
|------|------|
| `feature.json` | Pins the active feature directory (see below) |
| `memory/constitution.md` | Project principles for `/speckit-constitution` and plan gates |
| `memory/project-context.md` | Short agent onboarding pointer |
| `templates/` | Plan, spec, tasks, checklist templates |
| `templates/overrides/` | Optional full-template replacements (see README there) |
| `scripts/bash/` | `setup-plan.sh`, `setup-tasks.sh`, `check-prerequisites.sh` |
| `workflows/` | Spec Kit workflow registry |
| `extensions.yml` | Git extension hooks (auto-commit, feature branch) |

## Active feature pinning

[`feature.json`](./feature.json) currently pins:

**[`specs/006-lure-snitch-hardening`](../specs/006-lure-snitch-hardening)** (Camp Lure + Snitch hardening)

When `feature_directory` matches an existing `specs/<dir>/` tree, scripts use that path even if the git branch name differs. Update `feature.json` when switching features.

## Bash scripts (run from repo root)

```bash
# Prerequisite check (plan.md, optional tasks)
bash .specify/scripts/bash/check-prerequisites.sh --json

# Resolve paths / copy plan template (destructive to plan.md)
bash .specify/scripts/bash/setup-plan.sh --json

# Tasks phase: list design docs + tasks template path
bash .specify/scripts/bash/setup-tasks.sh --json
```

### Warning: `setup-plan.sh` overwrites `plan.md`

`setup-plan.sh` copies the resolved plan template onto `specs/<feature>/plan.md`. **Do not run it** after a plan is already filled, or you will lose implementation content. Use it only when bootstrapping a new feature plan.

Safe workflow:

1. `/speckit-specify` creates `spec.md` and feature folder
2. Run `setup-plan.sh` once for an empty plan, then `/speckit-plan` fills it
3. Later changes: edit `plan.md` directly; never re-run `setup-plan.sh`

## Constitution and memory

- Principles: [`.specify/memory/constitution.md`](./memory/constitution.md) (version **1.0.0**, ratified 2026-05-26)
- Quick context: [`.specify/memory/project-context.md`](./memory/project-context.md)
- Repo agents: [`docs/agents/README.md`](../docs/agents/README.md)

## Git hooks (`extensions.yml`)

With `auto_execute_hooks: true`, the git extension may prompt for:

| Hook | Typical action |
|------|----------------|
| `before_constitution` | `speckit.git.initialize` |
| `before_specify` | `speckit.git.feature` (feature branch) |
| `before_plan` / `before_tasks` / `before_implement` | Optional `speckit.git.commit` |
| `after_*` | Optional auto-commit of spec/plan/tasks output |

Hooks are optional where marked; constitution init may require initialize.

## Agent skills in this repo

Project skills under `.agents/skills/speckit-*` wrap these scripts for Cursor/Claude workflows (`speckit-plan`, `speckit-tasks`, `speckit-implement`, etc.).
