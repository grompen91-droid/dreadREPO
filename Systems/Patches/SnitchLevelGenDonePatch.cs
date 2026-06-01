using System.Reflection;
using Dread.Config;
using Dread.Systems.Core;
using HarmonyLib;

namespace Dread.Systems
{
    /// <summary>
    /// After R.E.P.O. finishes level generation, nudge SnitchSystem to arm
    /// (valuables are spawned). Host-only; no-op when snitch disabled.
    /// </summary>
    internal static class SnitchLevelGenDonePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null)
                return;

            _original = AccessTools.Method(typeof(SemiFunc), "OnLevelGenDone");
            if (_original == null)
            {
                LoggingService.LogWarning(
                    "[Dread] SemiFunc.OnLevelGenDone not found; snitch level-gen hook skipped");
                return;
            }

            if (HarmonyPatchCompat.ShouldSkipDueToForeignPatches(_original, "SemiFunc.OnLevelGenDone"))
                return;

            var patch = new HarmonyMethod(typeof(SnitchLevelGenDonePatch), nameof(Postfix));
            harmony.Patch(_original, postfix: patch);
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null)
                return;

            harmony.Unpatch(_original, AccessTools.Method(typeof(SnitchLevelGenDonePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix()
        {
            if (!DreadConfig.SnitchEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            SnitchSystem.NotifyLevelGenDone();
        }
    }
}
