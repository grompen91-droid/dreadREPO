using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Shared enemy list refreshed at a fixed interval to avoid repeated FindObjectsOfType scans.
    /// </summary>
    internal static class EnemyScanCache
    {
        private static EnemyHealth[] _enemies = System.Array.Empty<EnemyHealth>();
        private static float _nextRefresh = -1f;
        private const float RefreshInterval = 0.5f;

        public static int Count => _enemies.Length;

        public static EnemyHealth[] GetEnemies()
        {
            RefreshIfNeeded();
            return _enemies;
        }

        public static float NearestDistance(Camera? cam)
        {
            if (cam == null)
                return float.MaxValue;

            RefreshIfNeeded();
            float nearest = float.MaxValue;
            var origin = cam.transform.position;
            for (int i = 0; i < _enemies.Length; i++)
            {
                var e = _enemies[i];
                if (e == null)
                    continue;
                float d = Vector3.Distance(origin, e.transform.position);
                if (d < nearest)
                    nearest = d;
            }

            return nearest;
        }

        public static void Invalidate() => _nextRefresh = -1f;

        private static void RefreshIfNeeded()
        {
            if (Time.time < _nextRefresh)
                return;

            _nextRefresh = Time.time + RefreshInterval;
            _enemies = Object.FindObjectsOfType<EnemyHealth>() ?? System.Array.Empty<EnemyHealth>();
        }
    }
}
