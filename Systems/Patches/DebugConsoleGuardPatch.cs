using System;
using System.Reflection;
using HarmonyLib;

namespace Dread.Systems
{
    /// <summary>
    /// Suppresses NullReferenceException spam from DebugConsoleUI.Update calling a broken
    /// SemiFunc.DebugTester Harmony hook (common with REPOConfig / MenuLib profiles).
    /// </summary>
    internal static class DebugConsoleGuardPatch
    {
        private static MethodInfo? _original;
        private static bool _loggedSuppression;

        internal static void Apply(Harmony harmony)
        {
            if (_original != null)
                return;

            var type = AccessTools.TypeByName("DebugConsoleUI");
            _original = type != null ? AccessTools.Method(type, "Update") : null;
            if (_original == null)
            {
                LoggingService.LogVerbose("[Dread] DebugConsoleUI.Update not found; debug console guard skipped");
                return;
            }

            harmony.Patch(
                _original,
                finalizer: new HarmonyMethod(typeof(DebugConsoleGuardPatch), nameof(SuppressNullReference)));
            LoggingService.LogInfo("[Dread] Debug console NRE guard active");
        }

        internal static void Remove(Harmony harmony)
        {
            if (_original == null) return;
            harmony.Unpatch(
                _original,
                AccessTools.Method(typeof(DebugConsoleGuardPatch), nameof(SuppressNullReference)));
            _original = null;
        }

        private static Exception SuppressNullReference(Exception __exception)
        {
            if (__exception is not NullReferenceException)
                return __exception;

            if (!_loggedSuppression)
            {
                _loggedSuppression = true;
                LoggingService.LogVerbose(
                    "[Dread] Suppressed DebugConsoleUI NullReferenceException from broken debug hook");
            }

            return null!;
        }
    }
}
