using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dread.Config;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class AudioDreadSystem : MonoBehaviour
    {
        private readonly List<AudioClip> _clips = new();
        private Camera? _mainCam;

        private static readonly string[] ClipNames =
        {
            "scraping.ogg", "breathing.ogg", "whisper.ogg"
        };

        // Weight per clip name — lower = rarer. Unlisted clips default to 1.0.
        private static readonly Dictionary<string, float> ClipWeights = new()
        {
            { "scraping.ogg",   0.6f },
            { "breathing.ogg",  0.3f },
            { "whisper.ogg",    0.1f },
        };

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(LoadClips());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _mainCam = Camera.main;
        }

        private IEnumerator LoadClips()
        {
            var audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");

            foreach (var name in ClipNames)
            {
                var path = Path.Combine(audioDir, name);
                if (!File.Exists(path))
                {
                    Plugin.Logger.LogWarning($"[AudioDread] Missing audio file: {path}");
                    continue;
                }

                using var req = UnityWebRequestMultimedia.GetAudioClip(
                    "file:///" + path.Replace('\\', '/'), AudioType.OGGVORBIS);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    clip.name = name;
                    _clips.Add(clip);
                }
                else
                {
                    Plugin.Logger.LogWarning($"[AudioDread] Failed to load {name}: {req.error}");
                }
            }

            Plugin.Logger.LogInfo($"[AudioDread] Loaded {_clips.Count}/{ClipNames.Length} clips.");
            StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            while (true)
            {
                var baseDelay = Random.Range(60f, 180f) / DreadConfig.AudioFrequency.Value;
                yield return new WaitForSeconds(baseDelay);

                if (!DreadConfig.AudioEnabled.Value || SemiFunc.MenuLevel() || _clips.Count == 0)
                    continue;

                if (_mainCam == null)
                    _mainCam = Camera.main;
                if (_mainCam == null)
                    continue;

                PlayRandomSound();
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
            if (clip == null) return;
            var cam = _mainCam;

            var offset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.3f, 0.3f),
                Random.Range(-1f, 1f)).normalized * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var host = new GameObject("DreadSound");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.pitch = Random.Range(0.5f, 1.5f);
            src.spatialBlend = 1.0f;
            src.volume = DreadConfig.AudioVolume.Value;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.Play();

            if (clip != null)
                Destroy(host, clip.length + 0.5f);
            else
                Destroy(host, 0.5f);
        }
    }
}
