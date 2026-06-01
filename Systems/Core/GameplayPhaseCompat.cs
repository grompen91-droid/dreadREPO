using System;
using System.Reflection;
using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Resolves R.E.P.O. session phase (menu, truck/shop, extraction level).
    /// Uses native game signals when available; falls back to an extraction latch
    /// set by <see cref="NotifyExtractionLevelStarted"/> after level generation.
    /// </summary>
    internal static class GameplayPhaseCompat
    {
        private static bool _extractionLatch;
        private static bool _loggedLatchFallback;
        private static bool _nativeResolved;
        private static Func<bool>? _isMenu;
        private static Func<bool>? _isInTruckOrShop;
        private static Func<bool>? _isInExtractionLevel;

        internal static void NotifyExtractionLevelStarted() => _extractionLatch = true;

        internal static void ResetForSceneLoad() => _extractionLatch = false;

        internal static GameplayPhase ResolvePhase()
        {
            try
            {
                if (SemiFunc.MenuLevel())
                    return GameplayPhase.Menu;

                ResolveNativeSignals();
                if (_isMenu != null && _isMenu())
                    return GameplayPhase.Menu;
                if (_isInTruckOrShop != null && _isInTruckOrShop())
                    return GameplayPhase.TruckOrShop;
                if (_isInExtractionLevel != null && _isInExtractionLevel())
                    return GameplayPhase.ExtractionLevel;
                if (_extractionLatch)
                    return GameplayPhase.ExtractionLevel;

                LogLatchFallbackOnce();
                return GameplayPhase.TruckOrShop;
            }
            catch
            {
                return GameplayPhase.Unknown;
            }
        }

        private static void ResolveNativeSignals()
        {
            if (_nativeResolved)
                return;

            _nativeResolved = true;
            _isMenu = TryBindAnyTrue(
                TryBindSemiFuncBool("IsSplashScreen", "IsMainMenu"),
                TryBindStaticBool("SharedSceneData", "IsInMainMenu"));

            _isInTruckOrShop = TryBindAnyTrue(
                TryBindSemiFuncBool(
                    "RunIsLobbyMenu",
                    "RunIsShop",
                    "TruckLevel",
                    "InTruck",
                    "ShopLevel",
                    "InShop"),
                TryBindStaticBool(
                    "SharedSceneData",
                    "IsInShop",
                    "IsInLobby",
                    "IsInTruckLobby"));

            _isInExtractionLevel = TryBindAnyTrue(
                TryBindSemiFuncBool(
                    "RunIsLevel",
                    "RunIsExtraction",
                    "RunLevel",
                    "InRun",
                    "LevelActive",
                    "InLevel",
                    "InExtractionLevel"),
                TryBindStaticBool("SharedSceneData", "IsInGame"));
        }

        private static Func<bool>? TryBindAnyTrue(params Func<bool>?[] probes)
        {
            var active = new System.Collections.Generic.List<Func<bool>>();
            foreach (var probe in probes)
            {
                if (probe != null)
                    active.Add(probe);
            }

            if (active.Count == 0)
                return null;

            return () =>
            {
                foreach (var probe in active)
                {
                    try
                    {
                        if (probe())
                            return true;
                    }
                    catch
                    {
                        // try next probe
                    }
                }

                return false;
            };
        }

        private static Func<bool>? TryBindStaticBool(string typeName, params string[] memberNames)
        {
            Type? type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, false);
                if (type != null)
                    break;
            }

            if (type == null)
                return null;

            var readers = new System.Collections.Generic.List<Func<bool>>();
            foreach (var name in memberNames)
            {
                try
                {
                    var prop = type.GetProperty(
                        name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (prop != null && prop.PropertyType == typeof(bool) && prop.GetMethod != null)
                    {
                        readers.Add(() => (bool)prop.GetValue(null)!);
                        continue;
                    }

                    var field = type.GetField(
                        name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (field != null && field.FieldType == typeof(bool))
                        readers.Add(() => (bool)field.GetValue(null)!);
                }
                catch
                {
                    // try next member
                }
            }

            if (readers.Count == 0)
                return null;

            return () =>
            {
                foreach (var read in readers)
                {
                    try
                    {
                        if (read())
                            return true;
                    }
                    catch
                    {
                        // try next reader
                    }
                }

                return false;
            };
        }

        private static Func<bool>? TryBindSemiFuncBool(params string[] methodNames)
        {
            foreach (var name in methodNames)
            {
                try
                {
                    var method = AccessTools.Method(typeof(SemiFunc), name);
                    if (method == null || method.ReturnType != typeof(bool))
                        continue;

                    return () =>
                    {
                        try
                        {
                            return (bool)method.Invoke(null, null)!;
                        }
                        catch
                        {
                            return false;
                        }
                    };
                }
                catch
                {
                    // try next candidate
                }
            }

            return null;
        }

        private static void LogLatchFallbackOnce()
        {
            if (_loggedLatchFallback)
                return;

            _loggedLatchFallback = true;
            if (_isInTruckOrShop == null && _isInExtractionLevel == null && _isMenu == null)
            {
                LoggingService.LogVerbose(
                    "[GameplayPhaseCompat] native phase API not found; using latch fallback");
            }
        }
    }
}
