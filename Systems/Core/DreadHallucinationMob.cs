using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Marks a client-local psychotic-break hallucination clone (excluded from proximity/threat scans).
    /// </summary>
    internal sealed class DreadHallucinationMob : MonoBehaviour
    {
        public float SpawnedAt { get; private set; }
        public float MaxLifetimeSeconds { get; set; } = 4f;

        private void Awake()
        {
            SpawnedAt = Time.time;
        }

        public static bool IsHallucination(GameObject? go)
        {
            if (go == null)
                return false;
            if (go.GetComponent<DreadHallucinationMob>() != null)
                return true;

            var t = go.transform.parent;
            while (t != null)
            {
                if (t.GetComponent<DreadHallucinationMob>() != null)
                    return true;
                t = t.parent;
            }

            return false;
        }

        public static bool IsHallucination(Component? c) =>
            c != null && IsHallucination(c.gameObject);
    }
}
