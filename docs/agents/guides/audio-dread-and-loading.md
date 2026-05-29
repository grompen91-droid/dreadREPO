# Audio Dread and clip loading

**Audio Dread** plays rare weighted 3D ambient sounds during a **Run**. **AudioClipLoader** loads OGG files from the plugin `audio/` folder. ADRs: 0003 (weighted random), 0006/0007 (loading).

## Audio Dread (`AudioDreadSystem.cs`)

| Aspect | Behavior |
|--------|----------|
| Clips | `scraping.ogg`, `footsteps.ogg`, `breathing.ogg`, `whisper.ogg` |
| Weights | Lower weight = rarer (`whisper.ogg` = 0.1) |
| Timing | 30s warmup after load, then random **60 to 180s** interval divided by `AudioFrequency` |
| Position | Random offset around camera (3D spatial, not entity-attached) |
| Gating | `AudioEnabled`, not menu level, clips must load |
| Playback lifetime | `AudioPlayUtil.PlayLifetimeSeconds(clip, pitch)` for one-shot hosts: `clip.length / pitch + padding` (pitch below 1.0 needs longer destroy delay) |

Runtime state for overlay/MCP:

- `DreadRuntimeState.AudioClipCount`
- `DreadRuntimeState.AudioNextPlayIn`

Config section: `1. Audio Dread` in `DreadConfig.cs`.

**Compatibility mode:** ambient audio still runs (core safe path).

## AudioClipLoader (`AudioClipLoader.cs`)

| Step | Detail |
|------|--------|
| Path | `{pluginDir}/audio/{fileName}` |
| Cache | Static dictionary per file name; cleared on scene load via `ClearCache()` from `TensionSystem` |
| Primary load | NVorbis decode to `AudioClip` (read until EOF, Linux-friendly) |
| Fallback | `UnityWebRequestMultimedia` with `file://` URI only when `UnityWebRequestCompat.IsUsable` |
| Stub builds | If NVorbis fails and UWR is unusable (zero-RVA stub compile), warning + `onLoaded(null)` |
| Missing file | Warning log, `onLoaded(null)` |

Agents adding sounds:

1. Place `.ogg` in repo `audio/` (packaged by `build.ps1`)
2. Reference name in system clip list
3. Load via `AudioClipLoader.LoadClip` or `LoadClips` coroutine
4. Tier 0 verify checks manifest audio layout

## Related systems

| System | Clips |
|--------|-------|
| `TensionSystem` | `breathing.ogg`, `breath2.ogg`, `breath3.ogg`, `footsteps.ogg` (fake steps) |
| `PsychoticBreakSystem` | `scream_peak.ogg`, `scream_distant.ogg`, `scream_threat.ogg`, footstep clip |

Use glossary names from [CONTEXT.md](../../../CONTEXT.md) (e.g. **Low stamina sound**).

## Common tasks

| Task | Where |
|------|-------|
| Add ambient clip | `AudioDreadSystem.ClipNames` + `ClipWeights` + file in `audio/` |
| Tune frequency | `DreadConfig.AudioFrequency` |
| Fix Linux load | Ensure full plugin folder deployed (NVorbis + dependencies); see [compatibility.md](compatibility.md) |
| Truncated ambient sound | Check pitch vs `Destroy` timing; use `AudioPlayUtil` for new one-shots |
| `BadImageFormatException` spam | Stub build + UWR: use game `Managed` refs for release DLLs; see `UnityWebRequestCompat` and error reporter HTTP path |

## ADRs

- `docs/adr/0003-weighted-random-audio-selection.md`
- `docs/adr/0006-unitywebrequest-for-audio-loading.md`
- `docs/adr/0007-audio-clip-loader.md`
