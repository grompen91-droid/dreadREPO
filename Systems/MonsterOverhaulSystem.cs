// Confirmed class names via Assembly-CSharp.dll binary analysis:
//   EnemyHealth       — enemy health component (field: "health")
//   EnemyNavMeshAgent — movement component (fields: "agentSpeed", "speedMultiplier")
//   EnemyParent       — enemy root component (has Start() lifecycle method)

using System.Collections;
using System.Reflection;
using Dread.Config;
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
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(MonsterAudioLoop());
        }

        private void OnDestroy()
        {
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

                if (!DreadConfig.MonsterAudioEnabled.Value || !_inLevel) continue;

                var enemies = FindObjectsOfType<EnemyHealth>();
                foreach (var e in enemies)
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
                src.pitch = Mathf.Clamp(src.pitch * 0.72f, 0.3f, 1f);
                src.reverbZoneMix = 1.1f;
                src.spatialBlend = 1.0f;
            }
        }
    }

    // Marker so we don't apply audio tweaks twice per enemy
    internal class DreadAudioTweaked : MonoBehaviour { }

    // ── HP Patch ──────────────────────────────────────────────────────────────
    // Targets EnemyHealth.Awake (confirmed class name from binary analysis).
    // Uses reflection for the "health" field to avoid a hard compile dependency
    // in case field name differs. Runs on all clients; Photon sync from master
    // client makes this effectively host-authoritative.

    [HarmonyPatch(typeof(EnemyHealth), "Awake")]
    internal static class EnemyHealthAwakePatch
    {
        private static FieldInfo? _healthField;

        [HarmonyPostfix]
        private static void Postfix(EnemyHealth __instance)
        {
            if (!DreadConfig.MonsterHPEnabled.Value) return;

            _healthField ??= typeof(EnemyHealth).GetField(
                "health",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (_healthField == null)
            {
                Plugin.Logger.LogWarning("[Dread] EnemyHealth.health field not found. HP patch inactive.");
                return;
            }

            var current = System.Convert.ToSingle(_healthField.GetValue(__instance));
            _healthField.SetValue(__instance, Mathf.RoundToInt(current * DreadConfig.MonsterHPMultiplier.Value));
        }
    }

    // ── Aggression Patch ──────────────────────────────────────────────────────
    // Targets EnemyNavMeshAgent.Awake (confirmed class + agentSpeed/speedMultiplier fields).
    // NavMeshAgent is driven by the master client in Photon PUN — inherently host-authoritative.

    [HarmonyPatch(typeof(EnemyNavMeshAgent), "Awake")]
    internal static class EnemyNavMeshAgentAwakePatch
    {
        private static FieldInfo? _agentSpeedField;
        private static FieldInfo? _speedMultField;

        [HarmonyPostfix]
        private static void Postfix(EnemyNavMeshAgent __instance)
        {
            if (!DreadConfig.MonsterAggressionEnabled.Value) return;

            _agentSpeedField ??= typeof(EnemyNavMeshAgent).GetField(
                "agentSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            _speedMultField ??= typeof(EnemyNavMeshAgent).GetField(
                "speedMultiplier",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (_agentSpeedField != null)
            {
                var speed = System.Convert.ToSingle(_agentSpeedField.GetValue(__instance));
                _agentSpeedField.SetValue(__instance, speed * 1.35f);
            }

            if (_speedMultField != null)
            {
                var mult = System.Convert.ToSingle(_speedMultField.GetValue(__instance));
                _speedMultField.SetValue(__instance, mult * 1.35f);
            }

            if (_agentSpeedField == null && _speedMultField == null)
            {
                Plugin.Logger.LogWarning("[Dread] EnemyNavMeshAgent speed fields not found. Aggression patch inactive.");
            }
        }
    }
}
