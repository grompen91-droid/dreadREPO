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

Runtime state for overlay/MCP:

- `DreadRuntimeState.AudioClipCount`
- `DreadRuntimeState.AudioNextPlayIn`

Config section: `1. Audio Dread` in `DreadConfig.cs`.

**Compatibility mode:** ambient audio still runs (core safe path).

## AudioClipLoader (`AudioClipLoader.cs`)

| Step | Detail |
|------|--------|
| Path | `{pluginDir}/audio/{fileName}` |
| Cache | Static dictionary per file name; cleared on scene load via `ClearCache()` from tension/psychotic break |
| Primary load | NVorbis decode to `AudioClip` (Linux-friendly) |
| Fallback | `UnityWebRequestMultimedia` with `file://` URI |
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

## ADRs

- `docs/adr/0003-weighted-random-audio-selection.md`
- `docs/adr/0006-unitywebrequest-for-audio-loading.md`
- `docs/adr/0007-audio-clip-loader.md`
