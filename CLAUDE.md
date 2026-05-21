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

Push to GitHub after every change. Run from the project root:
```powershell
git add <files>
git commit -m "type: description"
git push
```

Remote: `https://github.com/grompen91-droid/dreadREPO.git`, branch `master`.
