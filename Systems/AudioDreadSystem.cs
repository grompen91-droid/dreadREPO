using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public class AudioDreadSystem : MonoBehaviour
    {
        private readonly List<AudioClip> _clips = new();
        private Camera? _mainCam;

        private static readonly string[] ClipNames =
        {
            "scraping.ogg", "footsteps.ogg", "breathing.ogg", "whisper.ogg"
        };

        // Lower weight = rarer. Unlisted clips default to 1.0.
        private static readonly Dictionary<string, float> ClipWeights = new()
        {
            { "scraping.ogg",   1.0f },
            { "footsteps.ogg",  1.0f },
            { "breathing.ogg",  0.6f },
            { "whisper.ogg",    0.25f },
        };

        private void Start()
        {
            StartCoroutine(LoadClips());
        }

        private IEnumerator LoadClips()
        {
            foreach (var name in ClipNames)
            {
                yield return AudioLoader.Load(name, clip =>
                {
                    if (clip != null) _clips.Add(clip);
                });
            }

            Plugin.Logger.LogInfo($"[AudioDread] Loaded {_clips.Count}/{ClipNames.Length} clips.");
            StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            while (true)
            {
                var baseDelay = Random.Range(30f, 90f) / DreadConfig.AudioFrequency.Value;
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
            var clip = PickWeightedClip();
            var cam = _mainCam!;

            var offset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.3f, 0.3f),
                Random.Range(-1f, 1f)).normalized * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var host = new GameObject("DreadSound");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1.0f;
            src.volume = DreadConfig.AudioVolume.Value;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.Play();

            Destroy(host, clip.length + 0.5f);
        }
    }
}
