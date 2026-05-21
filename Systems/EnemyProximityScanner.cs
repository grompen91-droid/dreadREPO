using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    // Runs a single 0.5s proximity scan and exposes NearestDist to all tension features.
    // Skips the scan entirely when all consumers are disabled.
    internal class EnemyProximityScanner : MonoBehaviour
    {
        public float NearestDist { get; private set; } = float.MaxValue;

        private float _nextScan;

        private static bool AnyConsumerEnabled =>
            DreadConfig.AdrenalineEnabled.Value ||
            DreadConfig.LowStaminaSoundEnabled.Value ||
            DreadConfig.PanicSprintEnabled.Value ||
            DreadConfig.FakeFootstepsEnabled.Value;

        private void Update()
        {
            if (!AnyConsumerEnabled) return;

            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 0.5f;
                NearestDist = SemiFunc.MenuLevel() ? float.MaxValue : Scan();
            }
        }

        private static float Scan()
        {
            var cam = Camera.main;
            if (cam == null) return float.MaxValue;

            var enemies = FindObjectsOfType<EnemyHealth>();
            float nearest = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(cam.transform.position, e.transform.position);
                if (d < nearest) nearest = d;
            }
            return nearest;
        }
    }
}
