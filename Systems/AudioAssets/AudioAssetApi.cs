using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dread.Systems.AudioAssets
{
    /// <summary>Static facade for feature systems to request remote audio clips.</summary>
    public static class AudioAssetApi
    {
        internal static AudioAssetSystem? Instance { get; set; }

        public static event Action<string>? OnCategoryFirstClipReady;
        public static event Action? OnAllManifestClipsReady;

        internal static void RaiseCategoryFirstClipReady(string category)
            => OnCategoryFirstClipReady?.Invoke(category);

        internal static void RaiseAllManifestClipsReady()
            => OnAllManifestClipsReady?.Invoke();

        public static void RequestClip(string category, string fileName, Action<AudioClip?> onReady)
        {
            if (Instance == null)
            {
                onReady(null);
                return;
            }

            Instance.RequestClip(category, fileName, onReady);
        }

        public static void RequestClips(
            string category,
            IEnumerable<string> fileNames,
            Action<string, AudioClip?> onEach)
        {
            foreach (var name in fileNames)
            {
                var captured = name;
                RequestClip(category, captured, clip => onEach(captured, clip));
            }
        }

        public static bool TryGetClip(string category, string fileName, out AudioClip? clip)
        {
            clip = null;
            if (Instance == null)
                return false;
            return Instance.TryGetClip(category, fileName, out clip);
        }

        public static bool IsClipReady(string category, string fileName)
            => Instance != null && Instance.IsClipReady(category, fileName);

        public static int CategoryReadyCount(string category, IEnumerable<string> fileNames)
            => Instance?.CategoryReadyCount(category, fileNames) ?? 0;

        public static AudioClip? GetRandomClip(
            string category,
            IEnumerable<string> candidates,
            Dictionary<string, float>? weights = null)
            => Instance?.GetRandomClip(category, candidates, weights);
    }
}
