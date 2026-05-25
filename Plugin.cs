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

        private void Awake()
        {
            PluginDependencyResolver.Register();
            Logger = base.Logger;
            HarmonyInstance = _harmony;
            DreadConfig.Initialize(Config);

            LoggingService.Initialize(DreadConfig.LogLevelEntry.Value);

            _logLevelHandler = (_, _) => LoggingService.SetLevel(DreadConfig.LogLevelEntry.Value);
            DreadConfig.LogLevelEntry.SettingChanged += _logLevelHandler;

            if (DreadConfig.MonsterAggressionEnabled.Value)
            {
                EnemyNavMeshAgentAwakePatch.Apply(_harmony);
                EnemyDirectorSetInvestigatePatch.Apply(_harmony);
            }
            if (DreadConfig.CrouchSpeedBoostEnabled.Value)
                PlayerControllerAwakePatch.Apply(_harmony);
            ErrorReportPatch.Apply(_harmony);

            DreadConfig.MonsterAggressionEnabled.SettingChanged += (_, _) =>
            {
                if (DreadConfig.MonsterAggressionEnabled.Value)
                {
                    EnemyNavMeshAgentAwakePatch.Apply(_harmony);
                    EnemyDirectorSetInvestigatePatch.Apply(_harmony);
                }
                else
                {
                    EnemyNavMeshAgentAwakePatch.Remove(_harmony);
                    EnemyDirectorSetInvestigatePatch.Remove(_harmony);
                }
            };

            DreadConfig.CrouchSpeedBoostEnabled.SettingChanged += (_, _) =>
            {
                if (DreadConfig.CrouchSpeedBoostEnabled.Value)
                    PlayerControllerAwakePatch.Apply(_harmony);
                else
                    PlayerControllerAwakePatch.Remove(_harmony);
            };

            LoggingService.PrintAsciiArt();
            LoggingService.LogInfo($"{NAME} v{VERSION} loaded.");

            SceneManager.sceneLoaded += OnSceneLoaded;
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

