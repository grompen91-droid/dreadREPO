# Feature Specification: Remote audio assets (AUDIO-5)

**Branch**: `014-remote-audio-assets` | **Roadmap**: AUDIO-5

## Problem

Bundled `audio/` in the Thunderstore package is large and version-coupled. Players need reliable first-run fetch, offline cache reuse, and progressive playback while downloads complete.

## Solution

1. Embed `audio-manifest.json` in `Dread.dll` (paths, sizes, sha256, release URLs).
2. Store files under `{pluginDir}/audio-cache/v{Plugin.VERSION}/`.
3. On startup: validate cache, import from other `v*` folders (newest first), HTTP download missing files, prune old caches.
4. Features call `AudioAssetApi.RequestClip(category, fileName, callback)`.

## Feature consumers (all on AudioAssetApi)

| System | Category | Clips |
|--------|----------|-------|
| AudioDreadSystem | `ambient_dread` | scraping, whisper, door_creak, breathing, footsteps |
| TensionSystem | `tension` | breath2, breath3, breathing, footsteps |
| PsychoticBreakSystem | `psychotic_break` | screams, footsteps |
| SnitchSystem | `monster` | snitch_bang |

Monster overhaul and camp lure do not load custom OGG files.

## Acceptance criteria

- [x] Thunderstore zip has no OGG files
- [x] CD uploads per-file release assets from manifest
- [x] All feature systems use `AudioAssetApi` (no `AudioClipLoader.LoadClip`)
- [x] `door_creak.ogg` in manifest and ambient rotation
- [ ] Manual: first-run download OR seeded cache plays audio in-game
- [ ] Manual: snitch bang after pickup when clip loaded

## Out of scope (follow-up)

- Background-thread HTTP/NVorbis decode
- `BundleAudio=true` MSBuild property (dedicated opt-in; debug uses `DeployToProfile` + `DREAD_DEBUG` bundled import instead)
- Image assets (ASSET-1)
