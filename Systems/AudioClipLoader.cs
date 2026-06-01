using System;
using System.Collections.Generic;
using System.IO;
using Dread.Systems.Core;
using NVorbis;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// NVorbis decode from disk for <see cref="AudioAssets.AudioAssetSystem"/>.
    /// Feature systems must load clips via <see cref="AudioAssets.AudioAssetApi"/>.
    /// </summary>
    public static class AudioClipLoader
    {
        /// <summary>Decode an OGG file from disk into an AudioClip (used by AudioAssetSystem).</summary>
        public static bool TryDecodeFromDisk(string diskPath, string clipName, out AudioClip? clip)
        {
            clip = null;
            if (!File.Exists(diskPath))
                return false;

            if (TryLoadWithNvorbis(diskPath, clipName, out var nvClip))
            {
                clip = nvClip;
                return true;
            }

            return false;
        }

        internal static bool TryLoadWithNvorbis(string path, string clipName, out AudioClip clip)
        {
            clip = null!;
            try
            {
                using var reader = new VorbisReader(path);
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;
                if (channels <= 0 || sampleRate <= 0)
                    return false;

                var chunk = new float[Math.Max(channels * 2048, (sampleRate / 10) * channels)];
                int listCapacity = Math.Max(channels * 4096, (int)Math.Min(reader.TotalSamples, 2_000_000));
                var sampleList = new List<float>(listCapacity);
                int read;
                while ((read = reader.ReadSamples(chunk, 0, chunk.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                        sampleList.Add(chunk[i]);
                }

                int sampleCount = sampleList.Count;
                if (sampleCount < channels)
                    return false;

                int usedFrames = sampleCount / channels;
                var pcm = sampleList.ToArray();

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
    }
}
