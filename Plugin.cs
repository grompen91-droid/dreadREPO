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
        public const string VERSION = "1.5.3";

        internal static new ManualLogSource Logger = null!;
        internal static Harmony HarmonyInstance { get; private set; } = null!;

        private readonly Harmony _harmony = new(GUID);
        private EventHandler? _logLevelHandler;

        // ADR-0004 host gates are inside patch postfixes; CompatibilityMode disables apply here (ADR-0009).
        private static bool MonsterPatchesEnabled =>
            DreadConfig.MonsterAggressionEnabled.Value && !DreadConfig.CompatibilityMode.Value;

        private void Awake()
        {
            PluginDependencyResolver.Register();
            Logger = base.Logger;
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

            SceneManager.sceneLoaded += OnSceneLoaded;
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

        private void Start()
        {
            RepoConfigCompat.TryApply(_harmony);
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

