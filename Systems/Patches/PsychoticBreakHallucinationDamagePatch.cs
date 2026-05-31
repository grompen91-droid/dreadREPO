using System.Reflection;
using Dread.Systems.Core;
using HarmonyLib;
using Dread.Systems;
using UnityEngine;

namespace Dread.Systems.Patches
{
    /// <summary>
    /// Blocks player damage originating from psychotic-break hallucination clones.
    /// </summary>
    internal static class PsychoticBreakHallucinationDamagePatch
    {
        private static MethodInfo? _original;
        private static bool _applied;

        public static void Apply(Harmony harmony)
        {
            if (_applied)
                return;

            var playerHealthType = AccessTools.TypeByName("PlayerHealth");
            if (playerHealthType == null)
                return;

            foreach (var name in new[] { "Hurt", "Damage", "TakeDamage", "OnHurt" })
            {
                var method = AccessTools.Method(playerHealthType, name);
                if (method == null)
                    continue;

                _original = method;
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(PsychoticBreakHallucinationDamagePatch), nameof(HurtPrefix)));
                _applied = true;
                LoggingService.LogVerbose($"[PsychoticBreak] Patched PlayerHealth.{name} for hallucination damage block");
                return;
            }
        }

        public static void Remove(Harmony harmony)
        {
            if (!_applied || _original == null)
                return;

            harmony.Unpatch(_original, HarmonyPatchType.Prefix);
            _applied = false;
            _original = null;
        }

        private static bool HurtPrefix(object[] __args)
        {
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is GameObject go && DreadHallucinationMob.IsHallucination(go))
                    return false;
                if (__args[i] is Component c && DreadHallucinationMob.IsHallucination(c))
                    return false;
                if (__args[i] is EnemyHealth eh && DreadHallucinationMob.IsHallucination(eh))
                    return false;
            }

            if (PsychoticBreakHallucinationInvuln.Active)
                return false;

            return true;
        }
    }

    internal static class PsychoticBreakHallucinationInvuln
    {
        public static bool Active { get; set; }
    }
}
