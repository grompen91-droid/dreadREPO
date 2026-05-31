using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems.AudioAssets
{
    public class AudioAssetSystem : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _clipsByPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Action<AudioClip?>>> _pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _categoryFirstReady = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _decodeInFlight = new(StringComparer.OrdinalIgnoreCase);

        private AudioManifest? _manifest;
        private readonly List<AudioManifestFile> _downloadQueue = new();
        private readonly AudioDownloadPolicy _policy = new();
        private int _activeDownloads;
        private bool _allReadyRaised;
        private bool _firstRunNoticeShown;
        private bool _startupDone;
        private int _decodedCount;

        private void Awake()
        {
            AudioAssetApi.Instance = this;
        }

        private void OnDestroy()
        {
            if (AudioAssetApi.Instance == this)
                AudioAssetApi.Instance = null;
            StopAllCoroutines();
        }

        private void Start()
        {
            StartCoroutine(StartupRoutine());
            StartCoroutine(DownloadWorkerRoutine());
        }

        private void Update()
        {
            if (!_startupDone)
                return;

            var inRun = !GameplayContext.IsMenuLevel();
            _policy.OnFrameSample(Time.unscaledDeltaTime, inRun);
            DreadRuntimeState.AudioAssetsQueueRemaining = _downloadQueue.Count
                + (_activeDownloads > 0 ? _activeDownloads : 0);
        }

        private IEnumerator StartupRoutine()
        {
            if (!AudioManifest.TryLoad(out _manifest, out var err))
            {
                LoggingService.LogError($"[AudioAssets] Failed to load manifest: {err}");
                yield break;
            }

            var reconcile = AudioCacheReconciler.Reconcile(_manifest);
            LoggingService.LogInfo(
                $"[AudioAssets] Cache v{_manifest.ModVersion}: {reconcile.AlreadyValid} present, "
                + $"{reconcile.Imported} imported, {reconcile.NeedDownload.Count} to download");

            _downloadQueue.Clear();
            _downloadQueue.AddRange(reconcile.NeedDownload.OrderBy(f => f.Priority));

            AudioCacheReconciler.PruneOtherVersionCaches();
            MaybeShowFirstRunNotice();

            _startupDone = true;
            DreadRuntimeState.AudioAssetsQueueRemaining = _downloadQueue.Count;
        }

        private void MaybeShowFirstRunNotice()
        {
            if (!DreadConfig.AudioAssetsShowFirstRunNotice.Value || _firstRunNoticeShown)
                return;

            var marker = Path.Combine(AudioCachePaths.CacheRoot, ".notified-v" + _manifest!.ModVersion);
            if (File.Exists(marker))
                return;

            if (_downloadQueue.Count == 0)
                return;

            _firstRunNoticeShown = true;
            LoggingService.LogInfo(
                "[AudioAssets] Downloading audio from GitHub release on first run. "
                + "If sounds are missing, check your connection or see the mod release page.");
            try
            {
                Directory.CreateDirectory(AudioCachePaths.CacheRoot);
                File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
            }
            catch
            {
                // ignored
            }
        }

        private IEnumerator DownloadWorkerRoutine()
        {
            while (true)
            {
                if (!_startupDone || _manifest == null)
                {
                    yield return null;
                    continue;
                }

                var offline = AudioDownloadPolicy.IsNetworkOffline();
                var allowed = _policy.GetAllowedConcurrent(offline);

                while (_activeDownloads < allowed && _downloadQueue.Count > 0 && !offline)
                {
                    var entry = _downloadQueue[0];
                    _downloadQueue.RemoveAt(0);
                    _activeDownloads++;
                    StartCoroutine(DownloadOne(entry));
                }

                if (offline && _downloadQueue.Count > 0)
                    yield return new WaitForSeconds(5f);
                else
                    yield return null;
            }
        }

        private IEnumerator DownloadOne(AudioManifestFile entry)
        {
            try
            {
                var dest = AudioCachePaths.FilePathForManifestEntry(entry.Path);
                if (AudioCacheValidator.IsValidOnDisk(dest, entry))
                {
                    yield return DecodeAndFulfill(entry.Path);
                    yield break;
                }

                LoggingService.LogVerbose($"[AudioAssets] Downloading {entry.Path}...");
                var result = AudioAssetDownloader.TryDownload(_manifest!, entry, dest);
                if (result.Success)
                {
                    _policy.RecordDownload(result.BytesReceived, result.ElapsedSeconds);
                    yield return DecodeAndFulfill(entry.Path);
                }
                else
                {
                    LoggingService.LogWarning($"[AudioAssets] Failed {entry.Path}: {result.Error}");
                    _policy.ResetSessionOnError();
                    _downloadQueue.Add(entry);
                    FulfillPath(entry.Path, null);
                }
            }
            finally
            {
                _activeDownloads = Math.Max(0, _activeDownloads - 1);
            }
        }

        private IEnumerator DecodeAndFulfill(string manifestPath)
        {
            while (_decodeInFlight.Contains(manifestPath))
                yield return null;

            if (_clipsByPath.ContainsKey(manifestPath))
            {
                FulfillPath(manifestPath, _clipsByPath[manifestPath]);
                yield break;
            }

            _decodeInFlight.Add(manifestPath);
            AudioClip? clip = null;
            try
            {
                var entry = _manifest!.FindByPath(manifestPath);
                if (entry == null)
                {
                    FulfillPath(manifestPath, null);
                    yield break;
                }

                var disk = AudioCachePaths.FilePathForManifestEntry(manifestPath);
                if (!AudioClipLoader.TryDecodeFromDisk(disk, Path.GetFileName(manifestPath), out clip))
                {
                    LoggingService.LogWarning($"[AudioAssets] Decode failed: {manifestPath}");
                }
                else if (clip != null)
                {
                    _clipsByPath[manifestPath] = clip;
                    _decodedCount++;
                    LoggingService.LogVerbose($"[AudioAssets] Ready {manifestPath}");
                    CheckAllReady();
                }
            }
            finally
            {
                _decodeInFlight.Remove(manifestPath);
            }

            FulfillPath(manifestPath, clip);
        }

        private void CheckAllReady()
        {
            if (_allReadyRaised || _manifest == null)
                return;

            foreach (var f in _manifest.Files)
            {
                if (!_clipsByPath.ContainsKey(f.Path))
                    return;
            }

            _allReadyRaised = true;
            AudioAssetApi.RaiseAllManifestClipsReady();
        }

        public void RequestClip(string category, string fileName, Action<AudioClip?> onReady)
        {
            if (!AudioAssetPathResolver.TryResolve(category, fileName, out var manifestPath))
            {
                onReady(null);
                return;
            }

            if (_clipsByPath.TryGetValue(manifestPath, out var existing))
            {
                onReady(existing);
                NotifyCategoryReady(category);
                return;
            }

            RegisterPending(manifestPath, onReady);

            var disk = AudioCachePaths.FilePathForManifestEntry(manifestPath);
            var entry = _manifest?.FindByPath(manifestPath);
            if (entry != null && AudioCacheValidator.IsValidOnDisk(disk, entry))
            {
                StartCoroutine(DecodeAndFulfill(manifestPath));
                return;
            }

            EnsureQueued(entry);
        }

        private void EnsureQueued(AudioManifestFile? entry)
        {
            if (entry == null)
                return;

            if (_downloadQueue.Any(f => f.Path == entry.Path))
                return;

            _downloadQueue.Add(entry);
            _downloadQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        private void RegisterPending(string manifestPath, Action<AudioClip?> onReady)
        {
            if (!_pending.TryGetValue(manifestPath, out var list))
            {
                list = new List<Action<AudioClip?>>();
                _pending[manifestPath] = list;
            }

            list.Add(onReady);
        }

        private void FulfillPath(string manifestPath, AudioClip? clip)
        {
            if (_pending.TryGetValue(manifestPath, out var list))
            {
                _pending.Remove(manifestPath);
                foreach (var cb in list)
                    cb(clip);
            }

            if (clip != null)
            {
                foreach (var kv in GetCategoriesForPath(manifestPath))
                    NotifyCategoryReady(kv);
            }
        }

        private static IEnumerable<string> GetCategoriesForPath(string manifestPath)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in AudioAssetPathResolver.CategoriesForManifestPath(manifestPath))
            {
                if (seen.Add(cat))
                    yield return cat;
            }
        }

        private void NotifyCategoryReady(string category)
        {
            if (_categoryFirstReady.Contains(category))
                return;
            _categoryFirstReady.Add(category);
            AudioAssetApi.RaiseCategoryFirstClipReady(category);
        }

        public bool TryGetClip(string category, string fileName, out AudioClip? clip)
        {
            clip = null;
            if (!AudioAssetPathResolver.TryResolve(category, fileName, out var path))
                return false;
            return _clipsByPath.TryGetValue(path, out clip);
        }

        public bool IsClipReady(string category, string fileName)
        {
            if (!AudioAssetPathResolver.TryResolve(category, fileName, out var path))
                return false;
            return _clipsByPath.ContainsKey(path);
        }

        public int CategoryReadyCount(string category, IEnumerable<string> fileNames)
        {
            var count = 0;
            foreach (var name in fileNames)
            {
                if (IsClipReady(category, name))
                    count++;
            }

            return count;
        }

        public AudioClip? GetRandomClip(
            string category,
            IEnumerable<string> candidates,
            Dictionary<string, float>? weights)
        {
            var loaded = new List<AudioClip>();
            float total = 0f;
            foreach (var name in candidates)
            {
                if (!TryGetClip(category, name, out var clip) || clip == null)
                    continue;
                loaded.Add(clip);
                total += weights != null && weights.TryGetValue(name, out var w) ? w : 1f;
            }

            if (loaded.Count == 0 || total <= 0f)
                return null;

            var roll = UnityEngine.Random.Range(0f, total);
            foreach (var name in candidates)
            {
                if (!TryGetClip(category, name, out var clip) || clip == null)
                    continue;
                var w = weights != null && weights.TryGetValue(name, out var weight) ? weight : 1f;
                roll -= w;
                if (roll <= 0f)
                    return clip;
            }

            return loaded[loaded.Count - 1];
        }
    }
}
