using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using Dread.Systems.AudioAssets;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class AudioDreadSystem : MonoBehaviour
    {
        private const string Category = "ambient_dread";

        private readonly List<AudioClip> _clips = new();
        private readonly HashSet<string> _pendingNames = new();
        private Camera? _mainCam;
        private bool _sceneLoaded;
        private float _nextPlayAt = -1f;
        private bool _playLoopStarted;

        private static readonly string[] ClipNames =
        {
            "scraping.ogg", "footsteps.ogg", "breathing.ogg", "whisper.ogg"
        };

        private static readonly Dictionary<string, float> ClipWeights = new()
        {
            { "scraping.ogg",   0.6f },
            { "footsteps.ogg",  0.6f },
            { "breathing.ogg",  0.3f },
            { "whisper.ogg",    0.1f },
        };

        private void Start()
        {
            LoggingService.LogVerbose("[AudioDread] Awake starting...");
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            AudioAssetApi.OnCategoryFirstClipReady += OnCategoryFirstClipReady;
            StartCoroutine(WaitAndRequestClips());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioAssetApi.OnCategoryFirstClipReady -= OnCategoryFirstClipReady;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sceneLoaded = true;
            _mainCam = Camera.main;
        }

        private void OnCategoryFirstClipReady(string category)
        {
            if (category == Category && !_playLoopStarted && _clips.Count > 0)
                StartCoroutine(PlayLoop());
        }

        private IEnumerator WaitAndRequestClips()
        {
            while (!_sceneLoaded || GameplayContext.IsMenuLevel())
                yield return null;

            foreach (var name in ClipNames)
            {
                _pendingNames.Add(name);
                AudioAssetApi.RequestClip(Category, name, clip =>
                {
                    _pendingNames.Remove(name);
                    if (clip != null && !_clips.Contains(clip))
                        _clips.Add(clip);
                    DreadRuntimeState.AudioClipCount = _clips.Count;
                    if (_clips.Count > 0 && !_playLoopStarted)
                    {
                        _playLoopStarted = true;
                        LoggingService.LogInfo($"[AudioDread] Loaded {_clips.Count}/{ClipNames.Length} clips (progressive).");
                        StartCoroutine(PlayLoop());
                    }
                });
            }
        }

        private IEnumerator PlayLoop()
        {
            yield return new WaitForSeconds(30f);

            while (true)
            {
                var baseDelay = Random.Range(60f, 180f) / DreadConfig.AudioFrequency.Value;
                _nextPlayAt = Time.time + baseDelay;
                DreadRuntimeState.AudioNextPlayIn = baseDelay;
                yield return new WaitForSeconds(baseDelay);

                LoggingService.LogVerbose("[AudioDread] Checking audio play...");

                if (!DreadConfig.AudioEnabled.Value || GameplayContext.IsMenuLevel() || _clips.Count == 0)
                    continue;

                if (_mainCam == null)
                    _mainCam = Camera.main;
                if (_mainCam == null)
                    continue;

                PlayRandomSound();
                DreadRuntimeState.AudioClipCount = _clips.Count;
            }
        }

        private void Update()
        {
            if (_nextPlayAt > 0f)
            {
                var eta = _nextPlayAt - Time.time;
                DreadRuntimeState.AudioNextPlayIn = eta < 0f ? 0f : eta;
            }
        }

        private AudioClip? PickWeightedClip()
        {
            if (_clips.Count == 0)
                return null;

            float total = 0f;
            foreach (var c in _clips)
            {
                var key = c.name.EndsWith(".ogg") ? c.name : c.name + ".ogg";
                total += ClipWeights.TryGetValue(key, out var w) ? w : 1.0f;
            }

            float roll = Random.Range(0f, total);
            foreach (var c in _clips)
            {
                var key = c.name.EndsWith(".ogg") ? c.name : c.name + ".ogg";
                roll -= ClipWeights.TryGetValue(key, out var w) ? w : 1.0f;
                if (roll <= 0f)
                    return c;
            }

            return _clips[_clips.Count - 1];
        }

        private void PlayRandomSound()
        {
            if (_mainCam == null)
                _mainCam = Camera.main;
            if (_mainCam == null)
                return;

            var clip = PickWeightedClip();
            if (clip == null)
                return;
            var cam = _mainCam;

            var offset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.3f, 0.3f),
                Random.Range(-1f, 1f)).normalized * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var pitch = Random.Range(0.5f, 1.5f);
            SpatialAudio3D.PlayAt(
                pos,
                clip,
                new SpatialAudio3D.PlayOptions
                {
                    Volume = DreadConfig.AudioVolume.Value,
                    MinDistance = 1f,
                    MaxDistance = 25f,
                    Pitch = pitch,
                    HostName = "DreadSound",
                });
        }
    }
}
