using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Dread.Systems
{
    public static class AudioClipLoader
    {
        public static string AudioDirectory { get; }

        private static readonly Dictionary<string, AudioClip> Cache = new();

        static AudioClipLoader()
        {
            AudioDirectory = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");
        }

        public static IEnumerator LoadClip(string fileName, Action<AudioClip?> onLoaded)
        {
            if (Cache.TryGetValue(fileName, out var cached))
            {
                onLoaded(cached);
                yield break;
            }

            var path = Path.Combine(AudioDirectory, fileName);
            if (!File.Exists(path))
            {
                LoggingService.LogWarning($"[AudioClipLoader] Missing audio file: {fileName}");
                onLoaded(null);
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
                onLoaded(clip);
            }
            else
            {
                var errorMsg = string.IsNullOrEmpty(req.error) ? "no error details" : req.error;
                LoggingService.LogWarning($"[AudioClipLoader] Failed to load {fileName}: {errorMsg}");
                onLoaded(null);
            }
        }

        public static IEnumerator LoadClips(IEnumerable<string> fileNames, Action<string, AudioClip?> onLoaded)
        {
            foreach (var name in fileNames)
            {
                AudioClip? clip = null;
                yield return LoadClip(name, c => clip = c);
                onLoaded(name, clip);
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
