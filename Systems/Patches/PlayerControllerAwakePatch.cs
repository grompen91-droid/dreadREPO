using System;
using System.Reflection;
using Dread.Config;
using HarmonyLib;

namespace Dread.Systems
{
    // Crouch speed patch: boost CrouchSpeed at PlayerController.Awake.
    internal static class PlayerControllerAwakePatch
    {
        private static MethodInfo? _original;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null)
                return;

            var type = typeof(PlayerController);
            _original = AccessTools.Method(type, "Awake");
            if (_original == null)
            {
                LoggingService.LogWarning("[Dread] PlayerController.Awake not found; crouch speed patch skipped");
                return;
            }

            harmony.Patch(_original, postfix: new HarmonyMethod(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(_original, AccessTools.Method(typeof(PlayerControllerAwakePatch), nameof(Postfix)));
            _original = null;
        }

        private static void Postfix(PlayerController __instance)
        {
            if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

            try
            {
                __instance.CrouchSpeed *= 1.3f;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] CrouchSpeedBoost patch skipped: {ex.Message}");
            }
        }
    }
}
