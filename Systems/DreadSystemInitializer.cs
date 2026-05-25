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

            int count = 0;
            count += TryAddSystem<AudioDreadSystem>("DreadAudioHost");
            count += TryAddSystem<MonsterOverhaulSystem>("DreadMonsterHost");
            count += TryAddSystem<TensionSystem>("DreadTensionHost");
            count += TryAddSystem<ErrorReporterSystem>("DreadErrorHost");
            count += TryAddSystem<PsychoticBreakSystem>("DreadPsychoticBreakHost");
            count += TryAddSystem<TestCrashSystem>("DreadTestCrashHost");
            count += TryAddSystem<DebugServerSystem>("DreadDebugHost");
            count += TryAddSystem<DebugOverlaySystem>("DreadDebugOverlayHost");

            if (count > 0)
                LoggingService.LogInfo($"Systems initialized ({count})");
            else
                LoggingService.LogError("All systems failed to initialize.");

            return true;
        }

        private static int TryAddSystem<T>(string hostName) where T : Component
        {
            try
            {
                var component = CreateSystemHost(hostName).AddComponent<T>();
                return component != null ? 1 : 0;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to add {typeof(T).Name} component: {ex.Message}");
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
