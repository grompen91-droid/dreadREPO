using System;
using System.Reflection;
using UnityEngine;

namespace Dread.Systems
{
    internal static class DreadSystemInitializer
    {
        private static bool _initialized;

        /// <returns>True when all systems were created; false if init was deferred.</returns>
        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            if (!EnsureUnityEngineUiLoaded())
            {
                LoggingService.LogVerbose("[Dread] Deferring system init until UnityEngine.UI is available");
                return false;
            }

            _initialized = true;

            var count = 0;
            var attempted = 0;
            foreach (var registration in DreadSystemRegistry.Registrations)
            {
                if (registration.IsEnabled != null && !registration.IsEnabled())
                    continue;

                attempted++;
                count += TryAddSystem(registration.SystemType, registration.HostName);
            }

            RepoConfigSliderLabelCompat.TryApply(Plugin.HarmonyInstance);

            if (count > 0)
            {
                if (count < attempted)
                    LoggingService.LogInfo($"Systems initialized ({count}/{attempted})");
                else
                    LoggingService.LogInfo($"Systems initialized ({count})");
            }
            else if (attempted > 0)
                LoggingService.LogError("All systems failed to initialize.");
            else
                LoggingService.LogVerbose("[Dread] No runtime systems enabled for initialization.");

            return true;
        }

        private static int TryAddSystem(Type systemType, string hostName)
        {
            try
            {
                var component = CreateSystemHost(hostName).AddComponent(systemType);
                if (component == null)
                {
                    LoggingService.LogError(
                        $"Failed to add {systemType.Name} component: Unity could not instantiate the script "
                        + "(check BepInEx log for TypeLoadException)");
                    return 0;
                }

                return 1;
            }
            catch (Exception ex)
            {
                var detail = ex is TypeLoadException or ReflectionTypeLoadException
                    ? ex.InnerException?.Message ?? ex.Message
                    : ex.Message;
                LoggingService.LogError($"Failed to add {systemType.Name} component: {detail}");
                return 0;
            }
        }

        private static bool EnsureUnityEngineUiLoaded()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(asm.GetName().Name, "UnityEngine.UI", StringComparison.Ordinal))
                        return asm.GetType("UnityEngine.UI.RawImage") != null;
                }

                var loaded = Assembly.Load("UnityEngine.UI");
                return loaded.GetType("UnityEngine.UI.RawImage") != null;
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[Dread] UnityEngine.UI not ready: {ex.Message}");
                return false;
            }
        }

        private static GameObject CreateSystemHost(string name)
        {
            var go = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go;
        }
    }
}
