using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Host-side enemy investigate alerts via EnemyDirector.SetInvestigate(Vector3, float, bool).
    /// </summary>
    internal static class EnemyDirectorCompat
    {
        // Large enough that distance / rangeMultiplier is always within radius on any level.
        private const float GlobalInvestigateRadius = 100000f;

        public static bool AlertAllEnemiesToPoint(Vector3 position)
        {
            if (!HarmonyPatchCompat.IsMasterClient())
                return false;

            var director = GetInstance();
            if (director == null)
            {
                LoggingService.LogWarning("[PsychoticBreak] EnemyDirector.instance not found; enemy alert skipped");
                return false;
            }

            if (TryCallSetInvestigate(director, position, GlobalInvestigateRadius, pathfindOnly: false))
            {
                LoggingService.LogInfo($"[PsychoticBreak] All enemies alerted to investigate {position}");
                return true;
            }

            if (TryForceInvestigateOnEachEnemy(director, position))
                return true;

            LoggingService.LogWarning("[PsychoticBreak] Could not alert enemies to psychotic break location");
            return false;
        }

        private static object? GetInstance()
        {
            try
            {
                var type = AccessTools.TypeByName("EnemyDirector");
                if (type == null)
                    return null;

                try
                {
                    var fromProp = Traverse.Create(type).Property("instance").GetValue();
                    if (fromProp != null)
                        return fromProp;
                }
                catch { }

                var field = AccessTools.Field(type, "instance");
                return field?.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryCallSetInvestigate(object director, Vector3 position, float radius, bool pathfindOnly)
        {
            var method = AccessTools.Method(
                director.GetType(),
                "SetInvestigate",
                new[] { typeof(Vector3), typeof(float), typeof(bool) });
            if (method == null)
                return false;

            try
            {
                method.Invoke(director, new object[] { position, radius, pathfindOnly });
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[PsychoticBreak] SetInvestigate failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryForceInvestigateOnEachEnemy(object director, Vector3 position)
        {
            try
            {
                var list = Traverse.Create(director).Field("enemiesSpawned").GetValue();
                if (list is not IEnumerable enemies)
                    return false;

                var count = 0;
                foreach (var enemyParent in enemies)
                {
                    if (enemyParent == null)
                        continue;
                    if (!TryGetSpawned(enemyParent))
                        continue;
                    if (TryForceEnemyInvestigate(enemyParent, position))
                        count++;
                }

                if (count <= 0)
                    return false;

                LoggingService.LogInfo($"[PsychoticBreak] Forced investigate on {count} enemies at {position}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[PsychoticBreak] Per-enemy investigate failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetSpawned(object enemyParent)
        {
            try
            {
                return Traverse.Create(enemyParent).Field<bool>("Spawned").Value;
            }
            catch
            {
                return true;
            }
        }

        private static bool TryForceEnemyInvestigate(object enemyParent, Vector3 position)
        {
            try
            {
                var enemy = Traverse.Create(enemyParent).Field("Enemy").GetValue();
                if (enemy == null)
                    return false;

                if (!TryGetBoolField(enemy, "HasStateInvestigate"))
                    return false;

                var stateInvestigate = Traverse.Create(enemy).Field("StateInvestigate").GetValue();
                if (stateInvestigate == null)
                    return false;

                var setMethod = AccessTools.Method(
                    stateInvestigate.GetType(),
                    "Set",
                    new[] { typeof(Vector3), typeof(bool) });
                if (setMethod == null)
                    return false;

                setMethod.Invoke(stateInvestigate, new object[] { position, false });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetBoolField(object target, string name)
        {
            try
            {
                return Traverse.Create(target).Field<bool>(name).Value;
            }
            catch
            {
                return false;
            }
        }
    }
}
