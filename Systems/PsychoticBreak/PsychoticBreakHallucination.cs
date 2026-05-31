using System;
using System.Collections;
using System.Reflection;
using Dread.Systems.Core;
using Dread.Systems.Patches;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
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

        private void PlanHallucinations(System.Random rng)
        {
            _hallucinationsPlayed = 0;
            _hallucinationsPlanned = 1;
            if (_episodeDuration >= 18f && rng.NextDouble() < 0.5)
                _hallucinationsPlanned = 2;

            float peakStart = _phase2Start;
            _nextHallucinationTime = peakStart + (float)rng.NextDouble() * (_phase3Start - peakStart - 2f);
            if (_nextHallucinationTime < peakStart + 1f)
                _nextHallucinationTime = peakStart + 1f;
        }

        private void UpdateHallucinationSchedule(float raw)
        {
            if (_hallucinationsPlayed >= _hallucinationsPlanned)
                return;
            if (raw < _phase2Start || raw >= _phase3Start + (_episodeDuration - _phase3Start) * 0.5f)
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
            PsychoticBreakHallucinationInvuln.Active = true;

            var template = PickHallucinationTemplate();
            if (template == null)
            {
                _hallucinationActive = false;
                PsychoticBreakHallucinationInvuln.Active = false;
                yield break;
            }

            var pc = PlayerController.instance;
            var cam = _mainCam ?? Camera.main;
            if ((object)pc == null || cam == null)
            {
                _hallucinationActive = false;
                PsychoticBreakHallucinationInvuln.Active = false;
                yield break;
            }

            Vector3 spawnPos = pc.transform.position + cam.transform.forward * Random.Range(4f, 7f);
            spawnPos.y = pc.transform.position.y;

            GameObject? clone = null;
            try
            {
                clone = (GameObject)UnityEngine.Object.Instantiate(
                    template.gameObject,
                    spawnPos,
                    Quaternion.LookRotation(pc.transform.position - spawnPos));
            }
            catch
            {
                clone = null;
            }

            if (clone == null)
            {
                _hallucinationActive = false;
                PsychoticBreakHallucinationInvuln.Active = false;
                yield break;
            }

            clone.name = "DreadHallucination_" + template.gameObject.name;
            _hallucinationClone = clone;
            clone.AddComponent<DreadHallucinationMob>();

            StripNetworking(clone);
            DisableCloneDamageColliders(clone);
            DisableNavAgents(clone);

            TryTriggerAttack(clone, pc.transform.position);

            float windUp = 0.55f;
            float elapsed = 0f;
            while (elapsed < windUp)
            {
                if (clone == null)
                    break;
                FaceTarget(clone.transform, pc.transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }

            PulseAttackAccentFlash();
            float flashHold = 0.12f;
            float flashElapsed = 0f;
            while (flashElapsed < flashHold)
            {
                flashElapsed += Time.deltaTime;
                yield return null;
            }

            ApplyAccentRgb();

            float tail = 2.5f;
            float tailElapsed = 0f;
            while (tailElapsed < tail && clone != null)
            {
                tailElapsed += Time.deltaTime;
                yield return null;
            }

            if (clone != null)
                Destroy(clone);
            _hallucinationClone = null;
            _hallucinationActive = false;
            PsychoticBreakHallucinationInvuln.Active = false;
        }

        private void DestroyActiveHallucination()
        {
            if (_hallucinationClone != null)
            {
                Destroy(_hallucinationClone);
                _hallucinationClone = null;
            }

            _hallucinationActive = false;
            PsychoticBreakHallucinationInvuln.Active = false;
        }

        private static EnemyHealth? PickHallucinationTemplate()
        {
            var enemies = ProximityScan.GetEnemies();
            EnemyHealth? best = null;
            float bestDist = float.MaxValue;
            var origin = PlayerController.instance != null
                ? PlayerController.instance.transform.position
                : Vector3.zero;

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;
                if (DreadHallucinationMob.IsHallucination(e))
                    continue;

                float d = Vector3.Distance(origin, ProximityScan.GetFocusPosition(e));
                if (d < ThreatRange && d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }

            if (best != null)
                return best;

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;
                float d = Vector3.Distance(origin, ProximityScan.GetFocusPosition(e));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }

            return best;
        }

        private static void StripNetworking(GameObject root)
        {
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null)
                    continue;
                var typeName = comp.GetType().FullName ?? "";
                if (typeName.IndexOf("Photon", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("PhotonView", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        Destroy(comp);
                    }
                    catch { }
                }
            }
        }

        private static void DisableNavAgents(GameObject root)
        {
            foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true))
            {
                if (agent == null)
                    continue;
                try
                {
                    agent.enabled = false;
                }
                catch { }
            }
        }

        private static void DisableCloneDamageColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                if (col == null || col.isTrigger)
                    continue;
                try
                {
                    col.enabled = false;
                }
                catch { }
            }
        }

        private static void FaceTarget(Transform t, Vector3 target)
        {
            var dir = target - t.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;
            t.rotation = Quaternion.LookRotation(dir.normalized);
        }

        private static void TryTriggerAttack(GameObject clone, Vector3 targetPos)
        {
            FaceTarget(clone.transform, targetPos);

            var animator = clone.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                foreach (var trigger in new[] { "Attack", "attack", "Attacking", "AttackPlayer", "Melee" })
                {
                    try
                    {
                        animator.SetTrigger(trigger);
                        return;
                    }
                    catch { }
                }
            }

            foreach (var comp in clone.GetComponentsInChildren<Component>(true))
            {
                if (comp == null)
                    continue;
                var type = comp.GetType();
                foreach (var methodName in new[] { "Attack", "AttackPlayer", "SetAttack", "TriggerAttack" })
                {
                    var method = AccessTools.Method(type, methodName);
                    if (method == null)
                        continue;
                    try
                    {
                        if (method.GetParameters().Length == 0)
                            method.Invoke(comp, null);
                        else if (method.GetParameters().Length == 1)
                            method.Invoke(comp, new object[] { targetPos });
                        return;
                    }
                    catch { }
                }
            }

            PlayFallbackAttackAudio(clone);
        }

        private static void PlayFallbackAttackAudio(GameObject clone)
        {
            foreach (var src in clone.GetComponentsInChildren<AudioSource>(true))
            {
                if (src == null || src.clip == null)
                    continue;
                if (src.clip.length < 0.2f)
                    continue;
                try
                {
                    src.Play();
                    return;
                }
                catch { }
            }
        }
    }
}
