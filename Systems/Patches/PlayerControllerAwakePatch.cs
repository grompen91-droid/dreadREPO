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

            var type = AccessTools.TypeByName("PlayerController");
            _original = type != null ? AccessTools.Method(type, "Awake") : null;
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

        private static void Postfix(object __instance)
        {
            if (!DreadConfig.CrouchSpeedBoostEnabled.Value) return;

            try
            {
                var field = AccessTools.Field(__instance.GetType(), "CrouchSpeed");
                if (field == null || field.FieldType != typeof(float)) return;
                var speed = (float)field.GetValue(__instance)!;
                field.SetValue(__instance, speed * 1.3f);
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] CrouchSpeedBoost patch skipped: {ex.Message}");
            }
        }
    }
}
