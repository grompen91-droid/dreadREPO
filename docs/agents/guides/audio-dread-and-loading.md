# Audio Dread and remote asset loading

**Audio Dread** plays rare weighted 3D ambient sounds during a **Run**. **`AudioAssetSystem`** downloads version-pinned OGG files from GitHub Releases into `audio-cache/v{VERSION}/` and decodes them on demand. ADRs: 0003 (weighted random), 0017 (remote delivery), 0007 (NVorbis decode).

## Audio assets (`AudioAssetSystem` + `AudioAssetApi`)

| Aspect | Behavior |
|--------|----------|
| Manifest | Embedded `audio-manifest.json`; repo source at `audio/audio-manifest.json` |
| Cache | `{pluginDir}/audio-cache/v{Plugin.VERSION}/{category}/file.ogg` |
| API | `AudioAssetApi.RequestClip(category, fileName, onReady)` |
| Progressive | Callback fires per clip; features start when first clip is ready |
| Downloads | 1-3 parallel HTTP (auto from speed + CPU); always starts at 1 until probed |
| Prune | After reconcile, deletes other `audio-cache/v*` folders |

Config: `1b. Audio Assets` in `DreadConfig.cs` (`MaxConcurrentDownloads`, `ShowFirstRunNotice`, `KeepOtherCaches`).

Runtime state: `AudioAssetsConcurrentDownloads`, `AudioAssetsMeasuredBytesPerSec`, `AudioAssetsQueueRemaining`.

See [remote-assets.md](remote-assets.md) for cache reconcile, downgrade import, and CD upload.

## Audio Dread (`AudioDreadSystem.cs`)

| Aspect | Behavior |
|--------|----------|
| Category | `ambient_dread` |
| Clips | `scraping.ogg`, `footsteps.ogg`, `breathing.ogg`, `whisper.ogg` (footsteps/breathing via `shared/`) |
| Weights | Lower weight = rarer (`whisper.ogg` = 0.1) |
| Timing | 30s warmup, then **60 to 180s** / `AudioFrequency` |
| Gating | `AudioEnabled`, not menu level, at least one clip loaded |

## AudioClipLoader (`AudioClipLoader.cs`)

Internal disk decode only (`TryDecodeFromDisk`). Feature systems must not call `LoadClip` directly.

## Related systems

| System | Category | Clips |
|--------|----------|-------|
| `AudioDreadSystem` | `ambient_dread` | scraping, whisper, breathing, footsteps |
| `TensionSystem` | `tension` | breathing, breath2, breath3, footsteps |
| `PsychoticBreakSystem` | `psychotic_break` | scream_peak, scream_distant, scream_threat, footsteps |

## Common tasks

| Task | Where |
|------|-------|
| Add sound | OGG under `audio/{category}/`, entry in `audio-manifest.json`, `AudioAssetPathResolver` if aliased |
| Validate manifest | `pwsh .github/scripts/validate-audio-manifest.ps1` |
| Release assets | `.github/scripts/upload-audio-release-assets.ps1` (CD) |
| Tune download parallelism | `DreadConfig.AudioAssetsMaxConcurrentDownloads` (0 = auto) |
| Local offline test | Seed `audio-cache/v{VERSION}/` (see smoke-test workflow) |

## ADRs

- `docs/adr/0003-weighted-random-audio-selection.md`
- `docs/adr/0017-remote-audio-asset-delivery.md`
- `docs/adr/0007-audio-clip-loader.md` (decode path)
