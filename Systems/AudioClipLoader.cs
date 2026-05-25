using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NVorbis;
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

            var path = Path.GetFullPath(Path.Combine(AudioDirectory, fileName));
            if (!File.Exists(path))
            {
                LoggingService.LogWarning($"[AudioClipLoader] Missing audio file: {fileName}");
                onLoaded(null);
                yield break;
            }

            if (TryLoadWithNvorbis(path, fileName, out var nvClip))
            {
                Cache[fileName] = nvClip;
                onLoaded(nvClip);
                yield break;
            }

            var fileUri = ToFileUri(path);
            using var req = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.OGGVORBIS);
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
                var handlerError = GetRequestHandlerError(req);
                var errorMsg = !string.IsNullOrEmpty(req.error)
                    ? req.error
                    : !string.IsNullOrEmpty(handlerError)
                        ? handlerError
                        : "no error details";
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

        public static string ToFileUri(string path)
        {
            path = Path.GetFullPath(path);

            if (path.Length > 0 && path[0] == '/')
                path = "Z:" + path.Replace('/', Path.DirectorySeparatorChar);

            return "file:///" + path.Replace('\\', '/');
        }

        internal static bool TryLoadWithNvorbis(string path, string clipName, out AudioClip clip)
        {
            clip = null!;
            try
            {
                using var reader = new VorbisReader(path);
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;
                int frameCount = (int)reader.TotalSamples;
                if (frameCount <= 0 || channels <= 0)
                    return false;

                var samples = new float[frameCount * channels];
                int offset = 0;
                while (offset < samples.Length)
                {
                    int read = reader.ReadSamples(samples, offset, samples.Length - offset);
                    if (read == 0)
                        break;
                    offset += read;
                }

                int usedFrames = offset / channels;
                if (usedFrames <= 0)
                    return false;

                var pcm = samples;
                if (usedFrames * channels < pcm.Length)
                {
                    var trimmed = new float[usedFrames * channels];
                    Array.Copy(pcm, trimmed, trimmed.Length);
                    pcm = trimmed;
                }

                int pcmReadPos = 0;
                clip = AudioClip.Create(
                    Path.GetFileNameWithoutExtension(clipName),
                    usedFrames,
                    channels,
                    sampleRate,
                    false,
                    data =>
                    {
                        int remaining = pcm.Length - pcmReadPos;
                        if (remaining <= 0)
                        {
                            for (int i = 0; i < data.Length; i++)
                                data[i] = 0f;
                            return;
                        }

                        int toCopy = Math.Min(data.Length, remaining);
                        Array.Copy(pcm, pcmReadPos, data, 0, toCopy);
                        pcmReadPos += toCopy;
                        for (int i = toCopy; i < data.Length; i++)
                            data[i] = 0f;
                    },
                    position =>
                    {
                        var pos = (int)(position * channels);
                        if (pos < 0) pos = 0;
                        else if (pos > pcm.Length) pos = pcm.Length;
                        pcmReadPos = pos;
                    });
                clip.name = clipName;
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[AudioClipLoader] NVorbis failed for {clipName}: {ex.Message}");
                return false;
            }
        }

        internal static string? GetDownloadHandlerError(object? handler)
        {
            if (handler == null) return null;
            var errorProp = handler.GetType().GetProperty("error");
            return errorProp?.GetValue(handler) as string;
        }

        internal static string? GetRequestHandlerError(UnityWebRequest req)
        {
            try
            {
                var prop = typeof(UnityWebRequest).GetProperty("downloadHandler");
                return GetDownloadHandlerError(prop?.GetValue(req));
            }
            catch
            {
                return null;
            }
        }
    }
}
