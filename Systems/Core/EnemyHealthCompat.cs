using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    internal static class EnemyHealthCompat
    {
        /// <summary>False for null or Unity-destroyed <see cref="EnemyHealth"/> references.</summary>
        internal static bool IsValid(EnemyHealth? enemy)
        {
            if (enemy == null)
                return false;

            try
            {
                var go = enemy.gameObject;
                return go != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// For line-of-sight only: skip destroyed enemies and corpses with readable HP &lt;= 0.
        /// Threat memory does not call this.
        /// </summary>
        internal static bool IsAliveForVisibility(EnemyHealth enemy)
        {
            if (!IsValid(enemy))
                return false;

            try
            {
                if (!enemy.gameObject.activeInHierarchy)
                    return false;
            }
            catch
            {
                return false;
            }

            if (TryReadHealth(enemy, out var hp))
                return hp > 0f;

            return true;
        }

        internal static bool TryIsAlive(EnemyHealth enemy)
        {
            if (!IsValid(enemy))
                return false;

            if (!TryReadHealth(enemy, out var hp))
                return true;

            return hp > 0f;
        }

        internal static void CountAliveAndNearby(
            EnemyHealth[] enemies,
            PlayerController? player,
            float proximityMeters,
            out int alive,
            out int nearby)
        {
            alive = 0;
            nearby = 0;
            if (enemies == null || enemies.Length == 0)
                return;

            Vector3 playerPos = default;
            var hasPlayer = false;
            if (player != null)
            {
                try
                {
                    playerPos = player.transform.position;
                    hasPlayer = true;
                }
                catch
                {
                    hasPlayer = false;
                }
            }

            for (var i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!IsValid(e))
                    continue;

                if (TryIsAlive(e))
                    alive++;

                if (!hasPlayer)
                    continue;

                try
                {
                    var dist = Vector3.Distance(e.transform.position, playerPos);
                    if (dist < proximityMeters)
                        nearby++;
                }
                catch
                {
                    // skip enemies with missing transform
                }
            }
        }

        internal static bool TryReadHealth(EnemyHealth enemy, out float hp)
        {
            hp = 0f;
            if (!IsValid(enemy))
                return false;

            var traverse = Traverse.Create(enemy);
            foreach (var name in new[]
                     {
                         "CurrentHealth", "currentHealth", "health", "Health", "HP", "hp"
                     })
            {
                if (TryReadFloatMember(traverse, name, out hp))
                    return true;
                if (TryReadIntMember(traverse, name, out var intHp))
                {
                    hp = intHp;
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadFloatMember(Traverse traverse, string name, out float value)
        {
            value = 0f;
            try
            {
                value = traverse.Property<float>(name).Value;
                return true;
            }
            catch { }

            try
            {
                value = traverse.Field<float>(name).Value;
                return true;
            }
            catch { }

            return false;
        }

        private static bool TryReadIntMember(Traverse traverse, string name, out int value)
        {
            value = 0;
            try
            {
                value = traverse.Property<int>(name).Value;
                return true;
            }
            catch { }

            try
            {
                value = traverse.Field<int>(name).Value;
                return true;
            }
            catch { }

            return false;
        }
    }
}
