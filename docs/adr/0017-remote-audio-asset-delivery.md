# ADR-0017: Remote audio asset delivery (AUDIO-5)

**Date:** 2026-05-31
**Status:** Accepted

---

## Context

Dread shipped all OGG files inside the Thunderstore package next to `Dread.dll`, growing every download as the sound library expanded. We need version-pinned assets, progressive feature startup, and minimal package size.

---

## Decision

1. **Manifest in DLL:** `audio/audio-manifest.json` is embedded as `Dread.audio.audio-manifest.json` with `modVersion`, `baseUrl`, per-file `path`, `assetName`, `sizeBytes`, `sha256`, and `priority`.
2. **Runtime cache:** `{pluginDir}/audio-cache/v{VERSION}/` holds downloaded files. On startup: reconcile, cross-import from other `v*` folders (newest donor first), HTTP for gaps, then **always prune** non-current version folders.
3. **`AudioAssetSystem`:** First registry Core system. Downloads via `HttpWebRequest` (parallelism 1-3 from `AudioDownloadPolicy`: measured download speed + CPU cap). Decodes with NVorbis through `AudioClipLoader.TryDecodeFromDisk`.
4. **Feature API:** `AudioAssetApi.RequestClip(category, fileName, callback)` resolves paths via `AudioAssetPathResolver` (e.g. `shared/footsteps.ogg` for multiple categories).
5. **Releases:** CD uploads each OGG as a GitHub Release asset named `category__file.ogg`. Thunderstore zip contains DLL only (no `audio/` folder).

---

## Consequences

- First run needs network unless cache is seeded (smoke tests seed `audio-cache`).
- Downgrade can reuse bytes from a newer cache folder when hash/size match the older manifest.
- ADR-0006/0007 local `audio/` loading is superseded for gameplay; `AudioClipLoader` remains for disk decode only.

---

## Rejected alternatives

- **Bundle all audio in Thunderstore:** rejected (package size).
- **Single release zip only:** rejected (no progressive per-file download without full zip fetch first).
