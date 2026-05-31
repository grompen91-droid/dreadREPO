using System;
using System.Collections;
using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private System.Random _episodeRng = new();
        private float _phase1End;
        private float _phase2Start;
        private float _phase3Start;

        private void StartEpisode(bool countAsMatchTrigger = true, bool debugDamageProtection = false)
        {
            _episodeActive = true;
            PsychoticBreakEpisodeProtection.SetActive(debugDamageProtection);
            if (countAsMatchTrigger)
                _hasTriggeredThisMatch = true;
            _episodeTimer = 0f;
            _tumbleMaintainTimer = 0f;
            _nextTriggerCheck = Time.time + _checkIntervalSeconds;

            _episodeRng = new System.Random(unchecked((int)Time.time));
            ComputePhaseBoundaries();
            PickEpisodeAccents(_episodeRng);
            PlanHallucinations(_episodeRng);

            LockPlayerForEpisode(PlayerController.instance);

            CreateOverlay();
            StartEpisodeAudio();
            StartFootstepScheduler();

            LoggingService.LogInfo("[Dread] Psychotic Break triggered!");
        }

        private void ComputePhaseBoundaries()
        {
            float jitter1 = 0.92f + (float)_episodeRng.NextDouble() * 0.16f;
            float jitter2 = 0.92f + (float)_episodeRng.NextDouble() * 0.16f;
            float jitter3 = 0.92f + (float)_episodeRng.NextDouble() * 0.16f;

            _phase1End = _episodeDuration * 0.15f * jitter1;
            _phase2Start = _episodeDuration * 0.50f * jitter2;
            _phase3Start = _episodeDuration * 0.80f * jitter3;

            if (_phase2Start <= _phase1End + 0.5f)
                _phase2Start = _phase1End + 0.5f;
            if (_phase3Start <= _phase2Start + 1f)
                _phase3Start = _phase2Start + 1f;
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
                _tumbleMaintainTimer = 2f;
                EnsurePlayerFallenHeld(PlayerController.instance);
            }

            if (raw >= _phase2Start && raw < _phase2Start + 0.05f)
                TransitionAccentToSecondary();

            UpdateScheduledScreams(raw);
            UpdateHallucinationSchedule(raw);

            if (_hallucinationFlashActive)
            {
                ApplyHallucinationFlashOverlay(_hallucinationFlashMobVisible);
                return;
            }

            float flickerFreqMul = 0.85f + (float)_episodeRng.NextDouble() * 0.3f;

            if (raw < _phase1End)
            {
                float alpha = Mathf.Lerp(0f, 0.85f, raw / _phase1End);
                SetDarknessAlpha(alpha);
                SetVignetteAlpha(0f);
                SetAccentAlpha(Mathf.Lerp(0f, 0.15f, raw / _phase1End));
            }
            else if (raw < _phase2Start)
            {
                float progress = (raw - _phase1End) / (_phase2Start - _phase1End);
                float flicker = Mathf.Sin(Time.time * Mathf.Lerp(10f, 30f, progress) * flickerFreqMul) * 0.5f + 0.5f;
                float vignetteBase = Mathf.Lerp(0.1f, 0.6f, progress);
                SetVignetteAlpha(vignetteBase * flicker);
                SetDarknessAlpha(0.85f);
                SetAccentAlpha(Mathf.Lerp(0.15f, 0.35f, progress) * flicker);
            }
            else if (raw < _phase3Start)
            {
                float progress = (raw - _phase2Start) / (_phase3Start - _phase2Start);
                float vignetteIntensity = Mathf.Lerp(0.5f, 0.9f, progress);
                float flicker = Mathf.Sin(Time.time * 35f * flickerFreqMul) * 0.3f + 0.7f;
                SetVignetteAlpha(vignetteIntensity * flicker);
                SetDarknessAlpha(0.85f);
                SetAccentAlpha(Mathf.Lerp(0.35f, 0.45f, progress) * flicker);
            }
            else
            {
                float progress = (raw - _phase3Start) / (_episodeDuration - _phase3Start);
                if (progress < 0.5f)
                {
                    float peak = Mathf.Lerp(0.9f, 1f, progress * 2f);
                    float flicker = Mathf.Sin(Time.time * 40f * flickerFreqMul) * 0.25f + 0.75f;
                    SetVignetteAlpha(0.95f * peak * flicker);
                    SetDarknessAlpha(0.9f * peak);
                    SetAccentAlpha(0.4f * peak * flicker);
                }
                else
                {
                    float fade = (progress - 0.5f) * 2f;
                    SetDarknessAlpha(Mathf.Lerp(0.9f, 0f, fade));
                    SetVignetteAlpha(Mathf.Lerp(0.95f, 0f, fade));
                    SetAccentAlpha(Mathf.Lerp(0.35f, 0f, fade));
                }

                if (raw >= _episodeDuration - 0.5f)
                    EndEpisode();
            }
        }

        private void TransitionAccentToSecondary()
        {
            _accentPrimary = _accentSecondary;
            ApplyAccentRgb();
        }

        private void EndEpisode()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Episode ending...");
            PsychoticBreakEpisodeProtection.SetActive(false);
            _episodeActive = false;
            SetDarknessAlpha(0f);
            SetVignetteAlpha(0f);
            SetAccentAlpha(0f);

            DestroyActiveHallucination();
            StopFootstepScheduler();
            CleanupDistantScreamSource();

            var pc = PlayerController.instance;
            RestorePlayerControl(pc);
            if ((object)pc != null)
                StartCoroutine(DoStumble());

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
