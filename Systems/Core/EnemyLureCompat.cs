using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Draws enemies toward a world position by invoking the game's own
    /// <c>EnemyDirector.SetInvestigate</c>. The method is resolved by reflection
    /// and its arguments are filled from the real parameter list, so the call
    /// adapts to signature differences across game versions (and never breaks the
    /// stub build). A larger radius reaches more distant enemies.
    /// </summary>
    /// <remarks>
    /// When <c>MonsterAggressionEnabled</c> is on, <see cref="EnemyDirectorSetInvestigatePatch"/>
    /// multiplies the radius by 1.5 for every SetInvestigate call, including compat invokes.
    /// </remarks>
    internal static class EnemyLureCompat
    {
        private static MethodInfo? _setInvestigate;
        private static ParameterInfo[]? _params;
        private static bool _resolved;

        /// <summary>Issue a directed investigate. Returns true if the call was dispatched.</summary>
        public static bool Pull(Vector3 position, float radius)
        {
            var director = ResolveDirector();
            if ((object?)director == null)
                return false;

            ResolveMethod();
            if (_setInvestigate == null || _params == null)
                return false;

            try
            {
                var args = new object[_params.Length];
                for (int i = 0; i < _params.Length; i++)
                {
                    var pt = _params[i].ParameterType;
                    var t = pt.IsByRef ? pt.GetElementType()! : pt;

                    if (t == typeof(Vector3))
                        args[i] = position;
                    else if (t == typeof(float))
                        args[i] = radius;
                    else if (_params[i].HasDefaultValue)
                        args[i] = _params[i].DefaultValue!;
                    else
                        args[i] = t.IsValueType ? Activator.CreateInstance(t)! : null!;
                }

                _setInvestigate.Invoke(director, args);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] EnemyLureCompat pull failed: {ex.Message}");
                return false;
            }
        }

        private static Component? ResolveDirector()
        {
            try
            {
                var d = UnityEngine.Object.FindObjectOfType<EnemyDirector>();
                return (object)d != null ? d : null;
            }
            catch
            {
                return null;
            }
        }

        private static void ResolveMethod()
        {
            if (_resolved)
                return;

            _resolved = true;
            _setInvestigate = AccessTools.Method(typeof(EnemyDirector), "SetInvestigate");
            _params = _setInvestigate?.GetParameters();

            if (_setInvestigate == null)
                LoggingService.LogWarning("[Dread] EnemyLureCompat: EnemyDirector.SetInvestigate not found; camp lure disabled");
        }
    }
}
