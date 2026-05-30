using System.Reflection;
using Dread.Config;
using Dread.Systems.Core;
using HarmonyLib;

namespace Dread.Systems
{
    // Voice/noise detection radius: 1.5x EnemyDirector.SetInvestigate radius (host-only).
    internal static class EnemyDirectorSetInvestigatePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null || DreadConfig.CompatibilityMode.Value)
                return;

            var type = typeof(EnemyDirector);
            _original = AccessTools.Method(type, "SetInvestigate");
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
