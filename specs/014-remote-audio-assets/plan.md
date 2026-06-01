# Implementation Plan: Remote audio assets (014 / AUDIO-5)

**Branch**: `014-remote-audio-assets` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)

**Status**: Implementation complete on branch. Manual in-game verify via [quickstart.md](./quickstart.md).

**Note**: `.specify/feature.json` pins this directory so Spec Kit scripts work off any git branch.

## Summary

Ship gameplay OGG via embedded `audio-manifest.json`, GitHub Release per-file assets, and `audio-cache/v{VERSION}/`. `AudioAssetSystem` reconciles cache, imports from older version folders, downloads gaps with adaptive concurrency (1-3), and exposes `AudioAssetApi.RequestClip` to all feature systems. Thunderstore zip is DLL-only.

## Technical Context

**Language/Version**: C# / .NET Framework 4.8, BepInEx 5.4, Unity 2022.3

**Primary Dependencies**: NVorbis, `HttpWebRequest`, embedded manifest, ADR-0017

**Testing**: Tier 0 `scripts/verify-dread.ps1`, `validate-audio-manifest.ps1`, manual quickstart (dread r2modman profile)

**Constraints**: No OGG in Thunderstore zip; `AudioClipLoader` decode-only; CI blocks `AudioClipLoader.LoadClip` in feature code

## Constitution Check

| Gate | Status |
|------|--------|
| Stub / production builds | Pass |
| No manual version bump | Pass (CD only) |
| ADR-0016 registry | `AudioAssetSystem` first Core host |
| All OGG features on `AudioAssetApi` | Pass |

## Project Structure

```text
Systems/AudioAssets/     # AudioAssetSystem, policy, cache, downloader, API
audio/audio-manifest.json
.github/scripts/validate-audio-manifest.ps1
.github/scripts/upload-audio-release-assets.ps1
docs/adr/0017-remote-audio-asset-delivery.md
```

## References

- [docs/agents/guides/remote-assets.md](../../docs/agents/guides/remote-assets.md)
- [docs/agents/guides/audio-dread-and-loading.md](../../docs/agents/guides/audio-dread-and-loading.md)
- [docs/adr/0017-remote-audio-asset-delivery.md](../../docs/adr/0017-remote-audio-asset-delivery.md)
