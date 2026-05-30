using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class AudioDreadSystem : MonoBehaviour
    {
        private readonly List<AudioClip> _clips = new();
        private Camera? _mainCam;
        private bool _sceneLoaded;
        private float _nextPlayAt = -1f;

        private static readonly string[] ClipNames =
        {
            "scraping.ogg", "footsteps.ogg", "breathing.ogg", "whisper.ogg"
        };

        // Weight per clip name — lower = rarer. Unlisted clips default to 1.0.
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
            StartCoroutine(LoadClips());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sceneLoaded = true;
            _mainCam = Camera.main;
        }

        private IEnumerator LoadClips()
        {
            while (!_sceneLoaded || GameplayContext.IsMenuLevel())
                yield return null;

            yield return AudioClipLoader.LoadClips(ClipNames, (name, clip) =>
            {
                if (clip != null) _clips.Add(clip);
            });

            LoggingService.LogInfo($"[AudioDread] Loaded {_clips.Count}/{ClipNames.Length} clips.");
            DreadRuntimeState.AudioClipCount = _clips.Count;
            StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            // Avoid ambient sounds during initial level load / spawn
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

        private AudioClip PickWeightedClip()
        {
            if (_clips.Count == 0) return null;
            float total = 0f;
            foreach (var c in _clips)
                total += ClipWeights.TryGetValue(c.name, out var w) ? w : 1.0f;

            float roll = Random.Range(0f, total);
            foreach (var c in _clips)
            {
                roll -= ClipWeights.TryGetValue(c.name, out var w) ? w : 1.0f;
                if (roll <= 0f) return c;
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
