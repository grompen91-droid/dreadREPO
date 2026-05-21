using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Dread.Systems
{
    // Shared clip cache: each .ogg is decoded once and reused across systems.
    internal static class AudioLoader
    {
        private static readonly Dictionary<string, AudioClip> Cache = new();

        public static string AudioDir => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "audio");

        public static IEnumerator Load(string fileName, System.Action<AudioClip?> onDone)
        {
            if (Cache.TryGetValue(fileName, out var cached))
            {
                onDone(cached);
                yield break;
            }

            var path = Path.Combine(AudioDir, fileName);
            if (!File.Exists(path))
            {
                Plugin.Logger.LogWarning($"[AudioLoader] Missing: {path}");
                onDone(null);
                yield break;
            }

            using var req = UnityWebRequestMultimedia.GetAudioClip(
                "file:///" + path.Replace('\\', '/'), AudioType.OGGVORBIS);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(req);
                clip.name = fileName;
                Cache[fileName] = clip;
                onDone(clip);
            }
            else
            {
                Plugin.Logger.LogWarning($"[AudioLoader] Failed {fileName}: {req.error}");
                onDone(null);
            }
        }
    }
}
