# ADR-0007: Centralize OGG Loading via AudioClipLoader

**Date:** 2026-05-22
**Status:** Accepted (updated 2026-05-22 to match implementation; 2026-06-01 doc note: API unchanged on `master`)

---

## Context

Three independent coroutine loaders existed for the same pattern: resolve `audio/` path from the DLL location, call `UnityWebRequestMultimedia.GetAudioClip()`, parse the result, and add to a list. Two files (`footsteps.ogg`, `breathing.ogg`) were loaded twice — once by `AudioDreadSystem` and once by `TensionSystem` — creating 8 `AudioClip` instances when 6 unique files would suffice.

Each loader had inconsistent error handling:
- `AudioDreadSystem.LoadClips()` logged warnings on both missing files and load failures.
- `TensionSystem.LoadBreathClips()` silently skipped missing files and logged on failure.
- `TensionSystem.LoadFootstepClip()` silently skipped missing files and had **no error handling** on load failure (the `else` branch was absent entirely).

---

## Decision

Introduce a **static utility class** `AudioClipLoader` that:

1. **Resolves the `audio/` directory once** in a static constructor from `Assembly.GetExecutingAssembly().Location`.
2. **Maintains a `Dictionary<string, AudioClip>` cache** keyed by filename. If two callers request `footsteps.ogg`, the second returns the cached clip immediately.
3. **Sets `clip.name = fileName`** after loading, preserving the contract `AudioDreadSystem.PickWeightedClip()` depends on (it looks up clip names in `ClipWeights`).
4. **Clears the cache on every scene transition** (`ClearCache()` is called unconditionally in `TensionSystem.OnSceneLoaded`), preventing stale clips from accumulating across play sessions. Clearing on every scene load (not just menu transitions) is simpler than per-scene-type gating and avoids stale state between level rounds.
5. **Normalizes error handling**: all failures log consistently with the `[AudioClipLoader]` prefix. The silent-skip bug is fixed — `TensionSystem.LoadFootstepClip` now warns on missing files and failed loads.

```csharp
public static IEnumerator LoadClip(string fileName, Action<AudioClip?> onLoaded)
{
    if (Cache.TryGetValue(fileName, out var cached))
    {
        onLoaded(cached);
        yield break;
    }
    // ... load from disk, cache, and invoke callback
}
```

---

## Consequences

- 6 unique `AudioClip` instances in memory instead of 8 (~2-3 MB saving for short OGGs).
- I/O at startup reduced by 2 redundant web requests.
- Callers shrink from 20-30 lines to 6-11 lines each.
- Unused imports (`System.IO`, `System.Reflection`, `UnityEngine.Networking`) removed from both caller files.
- Cache lifetime matches AppDomain lifetime. Cache is cleared on every scene load, incurring a one-time reload of audio at the start of each round. With only 6 small OGG files (~500 KB each), this reload cost is negligible.

---

## Agent note (2026-06)

`AudioClipLoader.LoadClip` / `LoadClips` remain the supported API for bundled `audio/*.ogg` on `master`. Do not resurrect removed `EnemyScanCache` or per-system duplicate loaders. Snitch and ambient systems load through this helper. For adding clips, see [docs/agents/guides/audio-dread-and-loading.md](../agents/guides/audio-dread-and-loading.md).

---

## Rejected Alternatives

- **MonoBehaviour**: The loader has no lifecycle, no Unity events, and no need for scene awareness (the cache clear is triggered externally). A `static` class keeps the interface simple.
- **`IAudioClipProvider` interface**: With only 6 files and one loading mechanism, an interface abstraction would be speculative. The static class meets the need with less indirection. If a second loading strategy is ever needed (e.g., loading from an external URL), the interface can be introduced at that point.
- **Preload all clips at startup**: Lazy loading means the first atmospheric sound plays slightly later (the first coroutine tick must finish loading). Preloading would consume memory and I/O before any clip is needed. For a horror mod that plays sounds unpredictably, lazy loading is more natural.
- **Unity `Resources.Load()`**: R.E.P.O. does not use Unity's Resources system for mod assets. The `audio/` folder is outside any Resources directory.
- **Rate-limit or priority queue**: The loader handles at most 6 files, loaded sequentially in coroutines. A queue system would be over-engineering.
