// Confirmed class names via Assembly-CSharp.dll binary analysis:
//   EnemyNavMeshAgent — movement component (fields: "agentSpeed", "speedMultiplier")
//   EnemyParent       — enemy root component (has Start() lifecycle method)

using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine.AI;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class MonsterOverhaulSystem : MonoBehaviour
    {
        internal static readonly List<EnemyHealth> CachedEnemies = new();
        private float _nextEnemyRefresh;

        private bool _inLevel;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _inLevel = !SceneManager.GetActiveScene().name.Contains("Menu") && !SceneManager.GetActiveScene().name.Contains("Main");
            StartCoroutine(MonsterAudioLoop());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _inLevel = !scene.name.Contains("Menu") && !scene.name.Contains("Main");
        }

        private void RefreshEnemyCache()
        {
            if (Time.time < _nextEnemyRefresh) return;
            _nextEnemyRefresh = Time.time + 5f;

            var found = FindObjectsOfType<EnemyHealth>();
            CachedEnemies.Clear();
            CachedEnemies.AddRange(found);
        }

        // Scans for enemies periodically and applies audio tweaks.
        // Works for all enemies including Mimic and WesleysEnemies.
        private IEnumerator MonsterAudioLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(4f);

                if (!DreadConfig.MonsterAudioEnabled.Value || !_inLevel) continue;

                RefreshEnemyCache();

                CachedEnemies.RemoveAll(e => e == null);
                foreach (var e in CachedEnemies)
                {
                    if (e.GetComponent<DreadAudioTweaked>() != null) continue;
                    e.gameObject.AddComponent<DreadAudioTweaked>();
                    ApplyAudioTweaks(e.gameObject);
                }
            }
        }

        private static void ApplyAudioTweaks(GameObject enemy)
        {
            foreach (var src in enemy.GetComponentsInChildren<AudioSource>())
            {
                src.pitch = Mathf.Clamp(src.pitch * Random.Range(0.5f, 1.5f), 0.3f, 1.5f);
                src.reverbZoneMix = 1.0f;
                src.spatialBlend = 1.0f;
            }
        }
    }

    // Marker so we don't apply audio tweaks twice per enemy
    internal class DreadAudioTweaked : MonoBehaviour { }

    // ── Aggression Patch ──────────────────────────────────────────────────────
    // Decompiled fields (via ILSpy):
    //   Agent          = NavMeshAgent component
    //   DefaultSpeed   = Agent.speed cached at Awake (used by speed-reset logic)
    //   DefaultAcceleration = Agent.acceleration cached at Awake
    // We multiply both the live agent value and the cached default so resets stay fast.

    internal static class EnemyNavMeshAgentAwakePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            _original = AccessTools.Method(typeof(EnemyNavMeshAgent), "Awake");
            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix(EnemyNavMeshAgent __instance)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value) return;

            var t = Traverse.Create(__instance);
            try
            {
                var agent = t.Field<NavMeshAgent>("Agent").Value;
                if (agent == null) return;

                agent.speed *= 1.2f;
                agent.acceleration *= 1.2f;
                t.Field<float>("DefaultSpeed").Value *= 1.2f;
                t.Field<float>("DefaultAcceleration").Value *= 1.2f;
            }
            catch
            {
            }
        }
    }

    // ── Crouch Speed Patch ────────────────────────────────────────────────────
    // Boosts CrouchSpeed and the cached original so speed-resets also use the
    // boosted value (e.g. after tumbling).

    internal static class PlayerControllerAwakePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            _original = AccessTools.Method(typeof(PlayerController), "Awake");
            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix(PlayerController __instance)
        {
            if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

            __instance.CrouchSpeed *= 1.3f;
            var t = Traverse.Create(__instance);
            try
            {
                float orig = t.Field<float>("playerOriginalCrouchSpeed").Value;
                if (orig > 0f)
                    t.Field<float>("playerOriginalCrouchSpeed").Value = orig * 1.3f;
                else
                    t.Field<float>("playerOriginalCrouchSpeed").Value = __instance.CrouchSpeed;
            }
            catch
            {
            }
        }
    }

    // ── Voice / Noise Detection Radius Patch ──────────────────────────────────
    // Increases EnemyDirector investigate radius so voice and physics noise alert
    // enemies further away. Kept at 1.5× — 3× caused too many simultaneous
    // investigate events which overwhelms Photon enemy-position sync on clients.

    internal static class EnemyDirectorSetInvestigatePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            _original = AccessTools.Method(typeof(EnemyDirector), "SetInvestigate");
            harmony.Patch(_original, prefix: new HarmonyMethod(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix)));
            _original = null;
        }

        private static void Prefix(ref float radius)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value) return;
            if (radius < float.MaxValue)
                radius *= 1.5f;
        }
    }
}
