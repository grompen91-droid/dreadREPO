using Dread.Config;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    // Boosts CrouchSpeed and the cached original so speed-resets also use the
    // boosted value (e.g. after tumbling).
    [HarmonyPatch(typeof(PlayerController), "Awake")]
    internal static class PlayerControllerAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerController __instance)
        {
            if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

            __instance.CrouchSpeed *= 1.3f;
            var t = Traverse.Create(__instance);
            float orig = t.Field<float>("playerOriginalCrouchSpeed").Value;
            if (orig > 0f) t.Field<float>("playerOriginalCrouchSpeed").Value = orig * 1.3f;
        }
    }
}
