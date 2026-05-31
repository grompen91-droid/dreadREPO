using System.Collections;
using System.Collections.Generic;
using Dread.Systems.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private bool _hallucinationActive;
        private GameObject? _hallucinationClone;
        private float _nextHallucinationTime;
        private int _hallucinationsPlayed;
        private int _hallucinationsPlanned = 1;
        private string _hallucinationStatus = "idle";
        private bool _hallucinationFlashActive;
        private bool _hallucinationFlashMobVisible;

        private const float HallucinationWindUpSeconds = 0.7f;
        private const float HallucinationStrobePeriodSeconds = 0.14f;
        private const float HallucinationStrobeOnDuty = 0.32f;
        private const float HallucinationAttackRevealSeconds = 0.8f;

        private void PlanHallucinations(System.Random rng)
        {
            _hallucinationsPlayed = 0;
            _hallucinationsPlanned = 1;
            if (_episodeDuration >= 18f && rng.NextDouble() < 0.5)
                _hallucinationsPlanned = 2;

            float peakStart = _phase2Start;
            float window = Mathf.Max(2f, _phase3Start - peakStart - 2f);
            _nextHallucinationTime = peakStart + (float)rng.NextDouble() * window;
            if (_nextHallucinationTime < peakStart + 1f)
                _nextHallucinationTime = peakStart + 1f;

            _hallucinationStatus = $"planned {_hallucinationsPlanned} @ {_nextHallucinationTime:F1}s";
        }

        private void UpdateHallucinationSchedule(float raw)
        {
            if (_hallucinationsPlayed >= _hallucinationsPlanned)
                return;
            if (raw < _phase2Start || raw >= _phase3Start + 2f)
                return;
            if (_hallucinationActive)
                return;
            if (raw < _nextHallucinationTime)
                return;

            StartCoroutine(RunHallucinationSequence());
            _hallucinationsPlayed++;
            if (_hallucinationsPlayed < _hallucinationsPlanned)
                _nextHallucinationTime = raw + 6f + (float)_episodeRng.NextDouble() * 4f;
        }

        private IEnumerator RunHallucinationSequence()
        {
            _hallucinationActive = true;
            _hallucinationStatus = "spawning";

            var pc = PlayerController.instance;
            var cam = _mainCam ?? Camera.main;
            if ((object)pc == null || cam == null)
            {
                _hallucinationStatus = "no player/camera";
                LoggingService.LogWarning("[PsychoticBreak] Hallucination skipped: no player or camera");
                _hallucinationActive = false;
                yield break;
            }

            var origin = pc.transform.position;
            var candidates = GatherHallucinationCandidates(origin);
            Vector3 spawnPos = origin + cam.transform.forward * Random.Range(4f, 7f);
            spawnPos.y = origin.y;
            var lookRot = Quaternion.LookRotation(origin - spawnPos);

            var (template, build) = PsychoticBreakHallucinationPresenter.BuildBest(
                candidates, spawnPos, lookRot, origin);
            PsychoticBreakHallucinationPresenter.LogBuildResult(build);

            if (build.Root == null)
            {
                _hallucinationStatus = "build failed";
                _hallucinationActive = false;
                yield break;
            }

            var mob = build.Root;
            _hallucinationStatus = build.Mode + ":" + build.TemplateObjectName;
            _hallucinationClone = mob;

            PsychoticBreakHallucinationPresenter.PlayAttackSoundNear(template, mob.transform.position);

            Vector3 lungeTarget = Vector3.Lerp(spawnPos, pc.transform.position, 0.45f);
            lungeTarget.y = spawnPos.y;

            _hallucinationFlashActive = true;
            PsychoticBreakHallucinationPresenter.SetMobVisible(mob, false);
            _hallucinationFlashMobVisible = false;

            float elapsed = 0f;
            while (elapsed < HallucinationWindUpSeconds)
            {
                if (mob == null)
                    break;

                float phase = elapsed % HallucinationStrobePeriodSeconds;
                bool mobOn = phase < HallucinationStrobePeriodSeconds * HallucinationStrobeOnDuty;
                PsychoticBreakHallucinationPresenter.SetMobVisible(mob, mobOn);
                _hallucinationFlashMobVisible = mobOn;

                float t = elapsed / HallucinationWindUpSeconds;
                mob.transform.position = Vector3.Lerp(spawnPos, lungeTarget, t);
                FaceHallucinationTarget(mob.transform, pc.transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (mob != null)
            {
                mob.transform.position = lungeTarget;
                FaceHallucinationTarget(mob.transform, pc.transform.position);
            }

            PsychoticBreakHallucinationPresenter.SetMobVisible(mob, true);
            _hallucinationFlashMobVisible = true;
            PulseAttackAccentFlash();

            float revealElapsed = 0f;
            while (revealElapsed < HallucinationAttackRevealSeconds)
            {
                if (mob == null)
                    break;
                revealElapsed += Time.deltaTime;
                yield return null;
            }

            _hallucinationFlashActive = false;
            ApplyAccentRgb();

            float tail = 2.5f;
            float tailElapsed = 0f;
            while (tailElapsed < tail && mob != null)
            {
                tailElapsed += Time.deltaTime;
                yield return null;
            }

            PsychoticBreakHallucinationPresenter.DestroyBuilt(mob);
            _hallucinationClone = null;
            _hallucinationActive = false;
            _hallucinationFlashActive = false;
            _hallucinationStatus = "done";
        }

        private void DestroyActiveHallucination()
        {
            _hallucinationFlashActive = false;
            PsychoticBreakHallucinationPresenter.DestroyBuilt(_hallucinationClone);
            _hallucinationClone = null;
            _hallucinationActive = false;
            _hallucinationStatus = "cleared";
        }

        private void ApplyHallucinationFlashOverlay(bool mobVisible)
        {
            if (mobVisible)
            {
                SetDarknessAlpha(0.22f);
                SetVignetteAlpha(0.28f);
                SetAccentAlpha(0.55f);
            }
            else
            {
                SetDarknessAlpha(0.99f);
                SetVignetteAlpha(0.99f);
                SetAccentAlpha(0.04f);
            }
        }

        internal string GetHallucinationStatusForDebug() => _hallucinationStatus;

        private static List<EnemyHealth> GatherHallucinationCandidates(Vector3 origin)
        {
            const int maxCandidates = 8;
            var list = GatherHallucinationCandidatesFromScan(origin, maxCandidates, PsychoticBreakHallucinationPresenter.MaxTemplatePickDistanceMeters);
            if (list.Count > 0)
                return list;

            list = GatherHallucinationCandidatesFromScan(
                origin,
                maxCandidates,
                PsychoticBreakHallucinationPresenter.MaxTemplatePickDistanceRelaxedMeters);
            if (list.Count > 0)
            {
                LoggingService.LogInfo(
                    "[PsychoticBreak] Hallucination: no template within "
                    + PsychoticBreakHallucinationPresenter.MaxTemplatePickDistanceMeters
                    + "m; using relaxed pick up to "
                    + PsychoticBreakHallucinationPresenter.MaxTemplatePickDistanceRelaxedMeters
                    + "m");
                return list;
            }

            PsychoticBreakHallucinationPresenter.LogCandidateDiagnostics(
                ProximityScan.GetEnemies(),
                origin,
                PsychoticBreakHallucinationPresenter.MaxTemplatePickDistanceRelaxedMeters);

            return list;
        }

        private static List<EnemyHealth> GatherHallucinationCandidatesFromScan(
            Vector3 origin,
            int maxCandidates,
            float maxDistanceMeters)
        {
            var list = PsychoticBreakHallucinationPresenter.RankTemplates(
                ProximityScan.GetEnemies(), origin, maxCandidates, maxDistanceMeters);
            if (list.Count > 0)
                return list;

            ProximityScan.Invalidate();
            list = PsychoticBreakHallucinationPresenter.RankTemplates(
                ProximityScan.GetEnemies(), origin, maxCandidates, maxDistanceMeters);
            if (list.Count > 0)
                return list;

            var fresh = UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
            if (fresh != null && fresh.Length > 0)
            {
                return PsychoticBreakHallucinationPresenter.RankTemplates(
                    fresh, origin, maxCandidates, maxDistanceMeters);
            }

            return list;
        }

        private static void FaceHallucinationTarget(Transform t, Vector3 target)
        {
            var dir = target - t.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;
            t.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }
}
