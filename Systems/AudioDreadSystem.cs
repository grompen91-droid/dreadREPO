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
        private bool _inLevel;
        private Camera? _mainCam;

        private static readonly string[] ClipNames =
        {
            "scraping.ogg", "footsteps.ogg", "breathing.ogg", "door_creak.ogg", "whisper.ogg"
        };

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(LoadClips());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _inLevel = !scene.name.Contains("Menu") && !scene.name.Contains("Main");
            _mainCam = null;
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
                var baseDelay = Random.Range(30f, 90f) / DreadConfig.AudioFrequency.Value;
                yield return new WaitForSeconds(baseDelay);

                if (!DreadConfig.AudioEnabled.Value || !_inLevel || _clips.Count == 0)
                    continue;

                if (_mainCam == null)
                    _mainCam = Camera.main;
                if (_mainCam == null)
                    continue;

                PlayRandomSound();
            }
        }

        private void PlayRandomSound()
        {
            var clip = _clips[Random.Range(0, _clips.Count)];
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
