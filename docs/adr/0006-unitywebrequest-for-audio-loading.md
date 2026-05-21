# ADR-0006: Load Audio via UnityWebRequestMultimedia

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

Dread ships 5 OGG audio files in a `audio/` folder next to the DLL. The mod needs to load these at runtime and play them as 3D spatialized clips. Several loading strategies were available.

---

## Decision

Use `UnityWebRequestMultimedia.GetAudioClip()` with `AudioType.OGGVORBIS` to load audio files from the DLL-adjacent `audio/` folder at runtime.

```csharp
string path = Path.Combine(Plugin.Location, "audio", fileName);
using var www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS);
www.SendWebRequest();
while (!www.isDone) { }
clip = DownloadHandlerAudioClip.GetContent(www);
```

Clips are loaded once at startup (in a coroutine), cached in a `List<AudioClip>`, and referenced by index during gameplay. Both `AudioDreadSystem` and `TensionSystem` use the same loading approach independently.

---

## Consequences

- OGG files sit alongside the DLL in the Thunderstore package, visible and replaceable by users.
- No build step for audio: drop a `.ogg` into `audio/` and add it to the clip list.
- `UnityWebRequest` works in both Unity Editor (for development) and in the BepInEx runtime environment.
- Files are loaded synchronously in a coroutine (blocking the main thread briefly). For five small OGGs (< 1 MB each) the load is imperceptible. For a larger audio pool, async loading with callbacks would be needed.

---

## Rejected Alternatives

- **Embedded resources (`.resx`)**: audio is embedded in the DLL, increasing DLL size and making it impossible for users to add or replace sounds without recompiling.
- **AssetBundles**: over-engineered for 5 files. AssetBundles require a build step with Unity's `BuildPipeline.BuildAssetBundle`, which adds friction to development.
- **`Resources.Load()`**: R.E.P.O. does not use Unity's Resources system for mod assets. The `audio/` folder is outside any Resources directory.
- **`File.ReadAllBytes()` + `AudioClip.Create()`**: would require manual OGG decoding. Unity's `UnityWebRequestMultimedia` handles this natively.
