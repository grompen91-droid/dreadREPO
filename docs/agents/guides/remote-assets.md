# Remote assets (audio today, images later)

Cross-cutting pattern for version-pinned files downloaded at runtime.

## Audio (shipped)

| Piece | Location |
|-------|----------|
| Source OGG + manifest | `audio/` in git |
| Embedded manifest | `Dread.audio.audio-manifest.json` in DLL |
| Runtime cache | `{pluginDir}/audio-cache/v{VERSION}/` |
| Release URLs | `https://github.com/grompen91-droid/dreadREPO/releases/download/v{VERSION}/{assetName}` |
| Core system | `Systems/AudioAssets/AudioAssetSystem.cs` |
| Feature API | `AudioAssetApi` |

See [audio-dread-and-loading.md](audio-dread-and-loading.md) for feature usage.

## Images (future, ASSET-1)

Do not implement in AUDIO-5. When added, reuse: manifest schema version, versioned cache folder, reconcile + prune, release asset upload in CD, and `AudioDownloadPolicy`-style concurrency. Document new ADR before shipping.

## Agent tasks

| Task | Action |
|------|--------|
| Add sound | Place OGG under `audio/{category}/`, update `audio-manifest.json`, run `validate-audio-manifest.ps1` |
| Release | CD `upload-audio-release-assets.ps1` runs on GitHub Release |
| Local test without network | Seed `audio-cache/v{VERSION}/` mirroring manifest paths (see smoke-test workflow) |
