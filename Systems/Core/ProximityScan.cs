using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Shared enemy list refreshed at a fixed interval (proximity scan for tension and related features).
    /// </summary>
    internal static class ProximityScan
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

        public static float NearestDistance(Vector3 origin)
        {
            RefreshIfNeeded();
            float nearest = float.MaxValue;
            for (int i = 0; i < _enemies.Length; i++)
            {
                var e = _enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;
                float d = Vector3.Distance(origin, GetFocusPosition(e));
                if (d < nearest)
                    nearest = d;
            }

            return nearest;
        }

        public static Vector3 GetFocusPosition(EnemyHealth enemy)
        {
            if (!EnemyHealthCompat.IsValid(enemy))
                return Vector3.zero;

            try
            {
                return enemy.transform.position;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        public static void Invalidate() => _nextRefresh = -1f;

        private static void RefreshIfNeeded()
        {
            if (Time.time < _nextRefresh)
                return;

            _nextRefresh = Time.time + RefreshInterval;

            var found = Object.FindObjectsOfType<EnemyHealth>();
            if (found == null || found.Length == 0)
            {
                _enemies = System.Array.Empty<EnemyHealth>();
                return;
            }

            int write = 0;
            for (int i = 0; i < found.Length; i++)
            {
                if (EnemyHealthCompat.IsValid(found[i]))
                    found[write++] = found[i];
            }

            if (write == 0)
            {
                _enemies = System.Array.Empty<EnemyHealth>();
                return;
            }

            var trimmed = new EnemyHealth[write];
            System.Array.Copy(found, trimmed, write);
            _enemies = trimmed;
        }
    }
}
