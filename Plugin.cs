using BepInEx;
using BepInEx.Logging;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread
{
    [BepInPlugin(Plugin.GUID, Plugin.NAME, Plugin.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "elytraking.dread";
        public const string NAME = "Dread";
        public const string VERSION = "1.5.2";

        internal static new ManualLogSource Logger = null!;
        internal static Harmony HarmonyInstance { get; private set; } = null!;

        private readonly Harmony _harmony = new(GUID);
        private EventHandler? _logLevelHandler;

        private static bool MonsterPatchesEnabled =>
            DreadConfig.MonsterAggressionEnabled.Value && !DreadConfig.CompatibilityMode.Value;

        private void Awake()
        {
            PluginDependencyResolver.Register();
            Logger = base.Logger;
            StubBuildDetector.Initialize();
            if (StubBuildDetector.IsStubBuild)
            {
                Logger.LogWarning(
                    "[Dread] Built against compile stubs: error reporting is disabled. "
                        + "Install REPO Managed DLLs for a production build to enable telemetry.");
            }
            HarmonyInstance = _harmony;
            DreadConfig.Initialize(Config);

            LoggingService.Initialize(DreadConfig.LogLevelEntry.Value);

            _logLevelHandler = (_, _) => LoggingService.SetLevel(DreadConfig.LogLevelEntry.Value);
            DreadConfig.LogLevelEntry.SettingChanged += _logLevelHandler;

            ApplyMonsterPatches();
            if (DreadConfig.CrouchSpeedBoostEnabled.Value)
                PlayerControllerAwakePatch.Apply(_harmony);
            if (DreadConfig.DebugConsoleGuardEnabled.Value)
                DebugConsoleGuardPatch.Apply(_harmony);

            DreadConfig.MonsterAggressionEnabled.SettingChanged += (_, _) => ApplyMonsterPatches();
            DreadConfig.CompatibilityMode.SettingChanged += (_, _) => ApplyMonsterPatches();

            DreadConfig.CrouchSpeedBoostEnabled.SettingChanged += (_, _) =>
            {
                if (DreadConfig.CrouchSpeedBoostEnabled.Value)
                    PlayerControllerAwakePatch.Apply(_harmony);
                else
                    PlayerControllerAwakePatch.Remove(_harmony);
            };

            DreadConfig.DebugConsoleGuardEnabled.SettingChanged += (_, _) =>
            {
                if (DreadConfig.DebugConsoleGuardEnabled.Value)
                    DebugConsoleGuardPatch.Apply(_harmony);
                else
                    DebugConsoleGuardPatch.Remove(_harmony);
            };

            LoggingService.PrintAsciiArt();
            LoggingService.LogInfo($"{NAME} v{VERSION} loaded.");

            // #region agent log
            DebugAgentLog.Write(
                "F",
                "Plugin.cs:Awake",
                "plugin_loaded",
                "post-fix",
                ("overlayEnabled", DreadConfig.DebugOverlayEnabled.Value),
                ("logLevel", DreadConfig.LogLevelEntry.Value.ToString()),
                ("compatibilityMode", DreadConfig.CompatibilityMode.Value),
                ("debugServerEnabled", DreadConfig.DebugServerEnabled.Value),
                ("audioFrequency", DreadConfig.AudioFrequency.Value));
            // #endregion

            if (DreadConfig.LogLevelEntry.Value == Systems.LogLevel.Verbose)
            {
                LoggingService.LogWarning(
                    "[Dread] LogLevel=Verbose hurts FPS. Set LogLevel=Debug in elytraking.dread.cfg for normal play.");
            }

            if (DreadConfig.AudioFrequency.Value > 3f)
            {
                LoggingService.LogWarning(
                    $"[Dread] Audio Frequency={DreadConfig.AudioFrequency.Value} is very high. "
                        + "Use 1-2 for normal play.");
            }

            if (DreadConfig.DebugOverlayEnabled.Value)
            {
                LoggingService.LogWarning(
                    "[Dread] Debug overlay enabled. Hide with ToggleKey when not debugging.");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RepoConfigSliderLabelCompat.TryApply(_harmony);
        }

        private void ApplyMonsterPatches()
        {
            if (MonsterPatchesEnabled)
            {
                EnemyNavMeshAgentAwakePatch.Apply(_harmony);
                EnemyDirectorSetInvestigatePatch.Apply(_harmony);
            }
            else
            {
                EnemyNavMeshAgentAwakePatch.Remove(_harmony);
                EnemyDirectorSetInvestigatePatch.Remove(_harmony);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (DreadSystemInitializer.TryInitialize())
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_logLevelHandler != null)
                DreadConfig.LogLevelEntry.SettingChanged -= _logLevelHandler;
        }
    }
}
