using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private void CleanupFootstepSource()
        {
            if (_footstepSource != null)
            {
                Destroy(_footstepSource.gameObject);
                _footstepSource = null;
            }
        }

        private void CleanupDistantScreamSource()
        {
            if (_distantScreamSource != null)
            {
                Destroy(_distantScreamSource.gameObject);
                _distantScreamSource = null;
            }
        }

        private IEnumerator LoadAudioClips()
        {
            while (!_sceneLoaded || SemiFunc.MenuLevel()) yield return null;

            var files = new[]
            {
                "scream_peak.ogg",
                "scream_distant.ogg",
                "scream_threat.ogg",
                "footsteps.ogg",
            };

            yield return AudioClipLoader.LoadClips(files, (name, clip) =>
            {
                switch (name)
                {
                    case "scream_peak.ogg":
                        _peakScreamClip = clip;
                        break;
                    case "scream_distant.ogg":
                        _distantScreamClip = clip;
                        break;
                    case "scream_threat.ogg":
                        _threatScreamClip = clip;
                        break;
                    case "footsteps.ogg":
                        _footstepClip = clip;
                        break;
                }

                if (clip != null)
                    LoggingService.LogInfo($"[PsychoticBreak] Loaded {name}");
                else
                    LoggingService.LogWarning($"[PsychoticBreak] Missing or failed: {name}");
            });
        }

        private void PlayCirclingFootsteps()
        {
            if (_footstepClip == null) return;

            var go = new GameObject("DreadPsychoticFootsteps");
            DontDestroyOnLoad(go);
            _footstepSource = go.AddComponent<AudioSource>();
            _footstepSource.clip = _footstepClip;
            _footstepSource.loop = true;
            _footstepSource.spatialBlend = 0f;
            _footstepSource.volume = 0.3f;
            _footstepSource.panStereo = -1f;
            _footstepSource.Play();
        }

        private void PlayDistantScream()
        {
            if (_distantScreamClip == null) return;

            var go = new GameObject("DreadDistantScream");
            DontDestroyOnLoad(go);
            _distantScreamSource = go.AddComponent<AudioSource>();
            _distantScreamSource.clip = _distantScreamClip;
            _distantScreamSource.loop = false;
            _distantScreamSource.spatialBlend = 0f;
            _distantScreamSource.volume = 0.35f;
            _distantScreamSource.Play();
        }

        private void PlayPeakScream()
        {
            if (_peakScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var go = new GameObject("DreadPeakScream");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = _peakScreamClip;
            src.spatialBlend = 0f;
            src.volume = 0.9f;
            src.Play();
            Destroy(go, _peakScreamClip.length + 1f);
        }

        private void SpawnPhantomSound()
        {
            if (_threatScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var clip = _threatScreamClip;

            var offset = Random.insideUnitSphere * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var host = new GameObject("DreadPhantomSound");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            var pitch = Random.Range(0.5f, 1.5f);
            src.pitch = pitch;
            src.spatialBlend = 1f;
            src.volume = Random.Range(0.4f, 0.8f);
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.Play();

            Destroy(host, AudioPlayUtil.PlayLifetimeSeconds(clip, pitch, paddingSeconds: 1f));
        }
    }
}
