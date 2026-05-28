# Issue tracker: GitHub

Issues and PRDs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

**See also:** [README.md](README.md) (agent entry), [orchestration.md](orchestration.md) (pick work and PR flow), [triage-labels.md](triage-labels.md).

Repository: `grompen91-droid/dreadREPO` (https://github.com/grompen91-droid/dreadREPO)

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue**: `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: `gh issue comment <number> --body "..."`
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close**: `gh issue close <number> --comment "..."`

Infer the repo from `git remote -v`. `gh` resolves the repo automatically inside a clone.

## Agent workflow (issues)

1. List open work: `gh issue list --label ready-for-agent --state open`
2. Read spec: `gh issue view <number> --comments`
3. Comment before large work: `gh issue comment <number> --body "Starting work on ..."`
4. On PR open: reference `Fixes #<number>` in the PR body
5. After merge: issue closes automatically when `Fixes` is used

Pick roadmap-backed issues from [docs/ROADMAP.md](../ROADMAP.md) when possible.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.
