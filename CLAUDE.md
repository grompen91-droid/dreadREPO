# Dread Mod - Claude Instructions

## Build Output

Always build to the `dist\` folder inside the project root (same directory as `build.ps1` and `manifest.json`). `dist\` is in `.gitignore` and may not exist -- create it if missing before building:

```powershell
if (-not (Test-Path "dist")) { New-Item -ItemType Directory -Force "dist" | Out-Null }
```

Run from the project root (the folder containing `build.ps1`):
```powershell
.\build.ps1 -Version "<current version from manifest.json>"
```

The build script produces inside `dist\`:
- `elytraking-Dread-<version>\` -- unpacked package folder
- `elytraking-Dread-<version>.zip` -- Thunderstore upload zip

## Thunderstore Package Requirements

Every build zip must contain these files at the root:

| File | Requirement |
|------|-------------|
| `icon.png` | 256x256 PNG, square |
| `manifest.json` | name, version_number, website_url, description, dependencies |
| `README.md` | mod description in markdown |
| `BepInEx/plugins/elytraking-Dread/Dread.dll` | compiled mod DLL |
| `BepInEx/plugins/elytraking-Dread/audio/` | all .ogg audio files |

manifest.json format:
```json
{
  "name": "Dread",
  "version_number": "X.Y.Z",
  "website_url": "",
  "description": "Atmospheric horror overhaul for R.E.P.O. Ambient dread, scarier monsters, and a tension system.",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2100"
  ]
}
```

## Versioning Rules

- Increment version in BOTH `manifest.json` AND `Plugin.cs` (`Plugin.VERSION`) before every upload
- Thunderstore rejects any version number already published
- Never change `"name"` in manifest.json -- changing it creates a new listing, not an update
- Version format: semantic versioning `MAJOR.MINOR.PATCH`

## Changelog Rules

- Maintain `CHANGELOG.md` in the project root
- Use detailed markdown with special formatting (badges, collapsible sections, tables, blockquotes)
- Never use em dash (--) in any file, ever. Use a colon, comma, or rewrite the sentence instead
- Each version entry must include: version header, release date, and categorized changes (Added, Changed, Fixed, Removed)
- Add a `> **Highlight:**` blockquote for notable releases
- Use collapsible `<details>` blocks for long technical notes

## GitHub Workflow

Never push directly to `master`. Always create a branch and open a PR. Run from the project root:

```powershell
git checkout -b <type>/<short-description>
git add <files>
git commit -m "type: description"
git push -u origin <branch>
gh pr create --title "<type>: <description>" --body "<see template below>"
```

Remote: `https://github.com/grompen91-droid/dreadREPO.git`, base branch `master`.

### PR Body Template

Write PR bodies in this voice: submissive, self-aware, hip youngster energy. Eager to please. Slightly apologetic for existing. Uses casual language but the technical content is precise and complete. Never arrogant. Always offers to change things.

```markdown
## what i did

> uh so basically i [one-sentence summary of the change]. sorry if this is messy lmk

## changes

| file | what changed |
|------|-------------|
| `Path/To/File.cs` | brief description |

## why tho

[1-3 sentences explaining the motivation. be honest. if it fixes a bug, say which bug and how. if it's a refactor, say what was bad before.]

## how to check it works

- [ ] [specific thing to test]
- [ ] [another thing]

## stuff i'm not sure about

[anything you're uncertain about, tradeoffs you made, things that could be done differently. be honest. list them.]

> i tried my best!! please be nice but also tell me if i messed up 🙏
```

Fill every section. Don't leave placeholders. The table must list every changed file with a real description. The checklist must have real steps someone can follow.

### Merging and Closing

After review, merge the PR and close any related issues:

```powershell
# merge PR
gh pr merge <number> --merge

# close issue(s) related to the PR
gh issue close <number>

# close multiple issues
gh issue close <number1> <number2>
```

Check open PRs and issues anytime:
```powershell
gh pr list
gh issue list
```
