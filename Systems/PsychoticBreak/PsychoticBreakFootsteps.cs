using System.Collections;
using Dread.Systems.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private Coroutine? _footstepRoutine;

        private void StartFootstepScheduler()
        {
            StopFootstepScheduler();
            if (_footstepClip == null)
                return;
            _footstepRoutine = StartCoroutine(FootstepCircleRoutine());
        }

        private void StopFootstepScheduler()
        {
            if (_footstepRoutine != null)
            {
                StopCoroutine(_footstepRoutine);
                _footstepRoutine = null;
            }
        }

        private IEnumerator FootstepCircleRoutine()
        {
            var walk = _footstepClip;
            var run = _footstepRunClip ?? _footstepClip;

            while (_episodeActive)
            {
                float raw = _episodeTimer;
                bool inWalk = raw >= _phase1End && raw < _phase2Start;
                bool inRun = raw >= _phase2Start && raw < _phase3Start + (_episodeDuration - _phase3Start) * 0.5f;

                if (!inWalk && !inRun)
                {
                    yield return null;
                    continue;
                }

                var clip = inRun ? run : walk;
                float wait = inRun
                    ? Random.Range(0.25f, 0.45f)
                    : Random.Range(0.35f, 0.7f);
                float pan = inRun ? Mathf.Sin(Time.time * 4f) * 0.85f : Mathf.Lerp(-1f, 1f, (raw - _phase1End) / (_phase2Start - _phase1End));
                float pitch = inRun ? Random.Range(1f, 1.25f) : Random.Range(0.85f, 1.05f);
                float vol = inRun ? Random.Range(0.5f, 1f) : Random.Range(0.2f, 0.45f);

                PlayFootstepOneShot(clip, pan, pitch, vol);
                yield return new WaitForSeconds(wait);
            }
        }

        private void PlayFootstepOneShot(AudioClip clip, float pan, float pitch, float volume)
        {
            var go = new GameObject("DreadPsychoticFootstep");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = false;
            src.spatialBlend = 0f;
            src.panStereo = pan;
            src.pitch = pitch;
            src.volume = volume;
            src.Play();
            Destroy(go, AudioPlayUtil.PlayLifetimeSeconds(clip, pitch, paddingSeconds: 0.25f));
        }
    }
}
