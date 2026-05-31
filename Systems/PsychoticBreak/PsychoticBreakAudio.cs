using System.Collections;
using System.Collections.Generic;
using Dread.Systems.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private AudioClip? _footstepRunClip;
        private readonly List<AudioSource> _distantScreamSources = new();
        private Coroutine? _screamScheduleRoutine;
        private Coroutine? _phantomRoutine;
        private int _peakScreamsPlayed;
        private bool _midPeakScreamPlayed;
        private bool _climaxScreamPlayed;

        private void CleanupFootstepSource()
        {
            StopFootstepScheduler();
        }

        private void CleanupDistantScreamSource()
        {
            StopScreamSchedules();
            for (int i = _distantScreamSources.Count - 1; i >= 0; i--)
            {
                if (_distantScreamSources[i] != null)
                    Destroy(_distantScreamSources[i].gameObject);
            }
            _distantScreamSources.Clear();
        }

        private void StopScreamSchedules()
        {
            if (_screamScheduleRoutine != null)
            {
                StopCoroutine(_screamScheduleRoutine);
                _screamScheduleRoutine = null;
            }

            if (_phantomRoutine != null)
            {
                StopCoroutine(_phantomRoutine);
                _phantomRoutine = null;
            }
        }

        private IEnumerator LoadAudioClips()
        {
            while (!_sceneLoaded || GameplayContext.IsMenuLevel()) yield return null;

            var files = new[]
            {
                "scream_peak.ogg",
                "scream_distant.ogg",
                "scream_threat.ogg",
                "footsteps.ogg",
                "footsteps_run.ogg",
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
                    case "footsteps_run.ogg":
                        _footstepRunClip = clip;
                        break;
                }

                if (clip != null)
                    LoggingService.LogInfo($"[PsychoticBreak] Loaded {name}");
                else
                    LoggingService.LogWarning($"[PsychoticBreak] Missing or failed: {name}");
            });
        }

        private void StartEpisodeAudio()
        {
            _peakScreamsPlayed = 0;
            _midPeakScreamPlayed = false;
            _climaxScreamPlayed = false;
            StopScreamSchedules();
            _screamScheduleRoutine = StartCoroutine(DistantScreamSchedule());
            _phantomRoutine = StartCoroutine(PhantomThreatLoop());
        }

        private IEnumerator DistantScreamSchedule()
        {
            if (_distantScreamClip == null)
                yield break;

            int plays = _episodeRng.Next(2, 5);
            for (int i = 0; i < plays && _episodeActive; i++)
            {
                float t = _phase1End + (float)_episodeRng.NextDouble() * (_phase2Start - _phase1End + 4f);
                while (_episodeActive && _episodeTimer < t)
                    yield return null;

                if (!_episodeActive)
                    yield break;

                PlayDistantScreamVaried();
                yield return new WaitForSeconds(Random.Range(2f, 5f));
            }
        }

        private void PlayDistantScreamVaried()
        {
            if (_distantScreamClip == null) return;

            var go = new GameObject("DreadDistantScream");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = _distantScreamClip;
            src.loop = false;
            src.spatialBlend = 0f;
            src.volume = Random.Range(0.2f, 0.45f);
            src.pitch = Random.Range(0.9f, 1.1f);
            src.panStereo = Random.Range(-0.6f, 0.6f);
            src.Play();
            _distantScreamSources.Add(src);
            Destroy(go, AudioPlayUtil.PlayLifetimeSeconds(_distantScreamClip, src.pitch, paddingSeconds: 1f));
        }

        private IEnumerator PhantomThreatLoop()
        {
            while (_episodeActive)
            {
                if (_episodeTimer >= _phase2Start && _episodeTimer < _phase3Start)
                {
                    if (!_hallucinationActive)
                    {
                        float wait = Random.Range(2f, 5f);
                        yield return new WaitForSeconds(wait);
                        if (_episodeActive && !_hallucinationActive)
                            SpawnPhantomSound();
                    }
                    else
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void UpdateScheduledScreams(float raw)
        {
            if (raw >= _phase2Start && _peakScreamsPlayed == 0)
            {
                PlayPeakScreamVaried();
                _peakScreamsPlayed++;
            }

            if (raw >= _phase2Start + (_phase3Start - _phase2Start) * 0.35f && !_midPeakScreamPlayed)
            {
                if (_episodeRng.NextDouble() < 0.85)
                {
                    PlayPeakScreamVaried();
                    _peakScreamsPlayed++;
                }
                _midPeakScreamPlayed = true;
            }

            float climaxStart = _phase3Start + (_episodeDuration - _phase3Start) * 0.1f;
            if (raw >= climaxStart && !_climaxScreamPlayed)
            {
                bool needClimax = _peakScreamsPlayed < 2 || _episodeRng.NextDouble() < 0.7;
                if (needClimax)
                {
                    PlayPeakScreamVaried();
                    _peakScreamsPlayed++;
                }
                _climaxScreamPlayed = true;
            }
        }

        private void PlayPeakScreamVaried()
        {
            if (_peakScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var go = new GameObject("DreadPeakScream");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = _peakScreamClip;
            src.spatialBlend = 0f;
            src.volume = Random.Range(0.75f, 1f);
            src.pitch = Random.Range(0.85f, 1.15f);
            src.panStereo = Random.Range(-0.5f, 0.5f);
            src.Play();
            Destroy(go, AudioPlayUtil.PlayLifetimeSeconds(_peakScreamClip, src.pitch, paddingSeconds: 1f));
        }

        private void SpawnPhantomSound()
        {
            if (_threatScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var clip = _episodeRng.NextDouble() < 0.2 && _distantScreamClip != null
                ? _distantScreamClip
                : _threatScreamClip;

            var offset = Random.insideUnitSphere * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var pitch = Random.Range(0.5f, 1.5f);
            float vol = _hallucinationActive ? Random.Range(0.2f, 0.4f) : Random.Range(0.4f, 0.8f);
            SpatialAudio3D.PlayAt(
                pos,
                clip,
                new SpatialAudio3D.PlayOptions
                {
                    Volume = vol,
                    MinDistance = 1f,
                    MaxDistance = 25f,
                    Pitch = pitch,
                    PaddingSeconds = 1f,
                    HostName = "DreadPhantomSound",
                });
        }
    }
}
