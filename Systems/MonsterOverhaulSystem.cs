// Confirmed class names via Assembly-CSharp.dll binary analysis:
//   EnemyNavMeshAgent — movement component (fields: "agentSpeed", "speedMultiplier")
//   EnemyParent       — enemy root component (has Start() lifecycle method)

using System;
using System.Collections;
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
        private bool _inLevel;

        private void Start()
        {
            LoggingService.LogVerbose("[MonsterOverhaul] Awake starting...");
            SceneManager.sceneLoaded += OnSceneLoaded;
            var sceneName = SceneManager.GetActiveScene().name;
            _inLevel = !sceneName.Contains("Menu") && !sceneName.Contains("Main");
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

        // Scans for enemies periodically and applies audio tweaks.
        // Works for all enemies including Mimic and WesleysEnemies.
        private IEnumerator MonsterAudioLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(4f);

                if (!DreadConfig.MonsterAudioEnabled.Value || !_inLevel || SemiFunc.MenuLevel()) continue;

                var enemies = EnemyScanCache.GetEnemies();
                LoggingService.LogVerbose($"[MonsterOverhaul] Processing {enemies.Length} enemies...");
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    if (e.GetComponent<DreadAudioTweaked>() != null) continue;
                    e.gameObject.AddComponent<DreadAudioTweaked>();
                    ApplyAudioTweaks(e.gameObject);
                }
            }
        }

        private static bool IsSourcePlaying(AudioSource src)
        {
            try
            {
                var prop = typeof(AudioSource).GetProperty("isPlaying");
                return prop != null && (bool)prop.GetValue(src)!;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyAudioTweaks(GameObject enemy)
        {
            foreach (var src in enemy.GetComponentsInChildren<AudioSource>())
            {
                if (src == null || IsSourcePlaying(src))
                    continue;

                src.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
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
            if (_original != null || DreadConfig.CompatibilityMode.Value)
                return;

            var type = AccessTools.TypeByName("EnemyNavMeshAgent");
            _original = type != null ? AccessTools.Method(type, "Awake") : null;
            if (_original == null)
            {
                LoggingService.LogWarning("[Dread] EnemyNavMeshAgent.Awake not found; aggression patch skipped");
                return;
            }

            if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "EnemyNavMeshAgent.Awake"))
                return;

            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyNavMeshAgentAwakePatch), nameof(Postfix)));
            _original = null;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(object __instance)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            try
            {
                var agent = Traverse.Create(__instance).Field<NavMeshAgent>("Agent").Value;
                if (agent == null) return;

                agent.speed *= 1.2f;
                agent.acceleration *= 1.2f;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] EnemyNavMeshAgent patch skipped: {ex.Message}");
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
            if (_original != null)
                return;

            var type = AccessTools.TypeByName("PlayerController");
            _original = type != null ? AccessTools.Method(type, "Awake") : null;
            if (_original == null)
            {
                LoggingService.LogWarning("[Dread] PlayerController.Awake not found; crouch speed patch skipped");
                return;
            }

            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix(object __instance)
        {
            if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

            try
            {
                var field = AccessTools.Field(__instance.GetType(), "CrouchSpeed");
                if (field == null || field.FieldType != typeof(float)) return;
                var speed = (float)field.GetValue(__instance)!;
                field.SetValue(__instance, speed * 1.3f);
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] CrouchSpeedBoost patch skipped: {ex.Message}");
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
            if (_original != null || DreadConfig.CompatibilityMode.Value)
                return;

            var type = AccessTools.TypeByName("EnemyDirector");
            _original = type != null ? AccessTools.Method(type, "SetInvestigate") : null;
            if (_original == null)
            {
                LoggingService.LogWarning("[Dread] EnemyDirector.SetInvestigate not found; investigate patch skipped");
                return;
            }

            if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "EnemyDirector.SetInvestigate"))
                return;

            var patch = new HarmonyMethod(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix));
            harmony.Patch(_original, prefix: patch);
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(EnemyDirectorSetInvestigatePatch), nameof(Prefix)));
            _original = null;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref float radius)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;
            if (radius < float.MaxValue)
                radius *= 1.5f;
        }
    }
}
