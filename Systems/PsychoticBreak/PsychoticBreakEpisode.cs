using System.Collections;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private void StartEpisode(bool countAsMatchTrigger = true)
        {
            _episodeActive = true;
            if (countAsMatchTrigger)
                _hasTriggeredThisMatch = true;
            _episodeTimer = 0f;
            _phantomSoundAccumulator = 0f;
            _tumbleMaintainTimer = 0f;
            _nextTriggerCheck = Time.time + 10f;
            _hasPlayedPeakScream = false;

            LockPlayerForEpisode(PlayerController.instance);

            CreateOverlay();
            PlayCirclingFootsteps();
            PlayDistantScream();

            LoggingService.LogInfo("[Dread] Psychotic Break triggered!");
        }

        private void UpdateEpisode()
        {
            _episodeTimer += Time.deltaTime;
            float raw = _episodeTimer;

            if (raw >= _episodeDuration)
            {
                EndEpisode();
                return;
            }

            _tumbleMaintainTimer -= Time.deltaTime;
            if (_tumbleMaintainTimer <= 0f)
            {
                _tumbleMaintainTimer = 0.4f;
                MaintainPlayerFallenState(PlayerController.instance);
            }

            float p1 = _episodeDuration * 0.15f;
            float p2 = _episodeDuration * 0.50f;
            float p3 = _episodeDuration * 0.80f;

            if (raw < p1)
            {
                float alpha = Mathf.Lerp(0f, 0.85f, raw / p1);
                SetDarknessAlpha(alpha);
                SetVignetteAlpha(0f);
            }
            else if (raw < p2)
            {
                float progress = (raw - p1) / (p2 - p1);
                float flicker = Mathf.Sin(Time.time * Mathf.Lerp(10f, 30f, progress)) * 0.5f + 0.5f;
                float vignetteBase = Mathf.Lerp(0.1f, 0.6f, progress);
                SetVignetteAlpha(vignetteBase * flicker);
                SetDarknessAlpha(0.85f);

                if (_footstepSource != null)
                    _footstepSource.panStereo = Mathf.Lerp(-1f, 1f, progress);
            }
            else if (raw < p3)
            {
                if (!_hasPlayedPeakScream)
                {
                    _hasPlayedPeakScream = true;
                    PlayPeakScream();
                }

                float progress = (raw - p2) / (p3 - p2);
                float vignetteIntensity = Mathf.Lerp(0.5f, 0.9f, progress);
                float flicker = Mathf.Sin(Time.time * 35f) * 0.3f + 0.7f;
                SetVignetteAlpha(vignetteIntensity * flicker);
                SetDarknessAlpha(0.85f);

                if (_footstepSource != null)
                {
                    _footstepSource.panStereo = Mathf.Sin(Time.time * 4f) * 0.8f;
                    _footstepSource.volume = Mathf.Lerp(0.5f, 1f, progress);
                }

                _phantomSoundAccumulator += Time.deltaTime * 0.3f;
                if (_phantomSoundAccumulator >= 1f)
                {
                    _phantomSoundAccumulator = 0f;
                    SpawnPhantomSound();
                }
            }
            else
            {
                float progress = (raw - p3) / (_episodeDuration - p3);
                if (progress < 0.5f)
                {
                    float peak = Mathf.Lerp(0.9f, 1f, progress * 2f);
                    SetVignetteAlpha(0.95f * peak);
                    SetDarknessAlpha(0.9f * peak);
                }
                else
                {
                    SetDarknessAlpha(0f);
                    SetVignetteAlpha(0f);
                }

                if (_footstepSource != null)
                {
                    if (progress < 0.5f)
                    {
                        _footstepSource.volume = Mathf.Lerp(1f, 0f, progress * 2f);
                        _footstepSource.panStereo = Mathf.Sin(Time.time * 8f) * 0.9f;
                    }
                    else
                    {
                        _footstepSource.Stop();
                    }
                }

                if (raw >= _episodeDuration - 0.5f)
                    EndEpisode();
            }
        }

        private void EndEpisode()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Episode ending...");
            _episodeActive = false;
            SetDarknessAlpha(0f);
            SetVignetteAlpha(0f);

            var pc = PlayerController.instance;
            RestorePlayerControl(pc);
            if ((object)pc != null)
                StartCoroutine(DoStumble());

            CleanupFootstepSource();
            CleanupDistantScreamSource();
            CleanupOverlay();
            LoggingService.LogInfo("[Dread] Psychotic Break ended.");
        }

        private IEnumerator DoStumble()
        {
            var cam = _mainCam;
            if (cam == null) yield break;

            var originalRot = cam.transform.localEulerAngles;
            var originalPos = cam.transform.localPosition;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float roll = Mathf.Lerp(15f, 0f, t);
                float dip = Mathf.Lerp(-0.3f, 0f, t);
                cam.transform.localEulerAngles = new Vector3(originalRot.x, originalRot.y, originalRot.z + roll);
                cam.transform.localPosition = originalPos + new Vector3(0f, dip, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cam.transform.localEulerAngles = originalRot;
            cam.transform.localPosition = originalPos;
        }
    }
}
