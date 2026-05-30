// Confirmed class names via Assembly-CSharp.dll binary analysis:
//   EnemyNavMeshAgent — movement component (fields: "agentSpeed", "speedMultiplier")
//   EnemyParent       — enemy root component (has Start() lifecycle method)

using System.Collections;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
namespace Dread.Systems
{
    public class MonsterOverhaulSystem : MonoBehaviour
    {
        private void Start()
        {
            LoggingService.LogVerbose("[MonsterOverhaul] Awake starting...");
            StartCoroutine(MonsterAudioLoop());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        // Scans for enemies periodically and applies audio tweaks.
        // Works for all enemies including Mimic and WesleysEnemies.
        private IEnumerator MonsterAudioLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(4f);

                if (!DreadConfig.MonsterAudioEnabled.Value || !GameplayContext.IsRun()) continue;

                var enemies = ProximityScan.GetEnemies();
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
}
