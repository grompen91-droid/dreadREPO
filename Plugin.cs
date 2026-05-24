using BepInEx;
using BepInEx.Logging;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;
using System;
using UnityEngine;

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
        }

        private void Start()
        {
            typeof(UnityEngine.UI.RawImage).ToString();
            int count = 0;
            if (CreateSystemHost("DreadAudioHost").AddComponent<AudioDreadSystem>() != null) count++;
            else LoggingService.LogError("Failed to add AudioDreadSystem component.");
            if (CreateSystemHost("DreadMonsterHost").AddComponent<MonsterOverhaulSystem>() != null) count++;
            else LoggingService.LogError("Failed to add MonsterOverhaulSystem component.");
            if (CreateSystemHost("DreadTensionHost").AddComponent<TensionSystem>() != null) count++;
            else LoggingService.LogError("Failed to add TensionSystem component.");
            if (CreateSystemHost("DreadErrorHost").AddComponent<ErrorReporterSystem>() != null) count++;
            else LoggingService.LogError("Failed to add ErrorReporterSystem component.");
            if (CreateSystemHost("DreadPsychoticBreakHost").AddComponent<PsychoticBreakSystem>() != null) count++;
            else LoggingService.LogError("Failed to add PsychoticBreakSystem component.");
            if (CreateSystemHost("DreadTestCrashHost").AddComponent<TestCrashSystem>() != null) count++;
            else LoggingService.LogError("Failed to add TestCrashSystem component.");
            if (CreateSystemHost("DreadDebugHost").AddComponent<DebugServerSystem>() != null) count++;
            else LoggingService.LogError("Failed to add DebugServerSystem component.");
            if (count > 0)
                LoggingService.LogInfo($"Systems initialized ({count})");
            else
                LoggingService.LogError("All systems failed to initialize.");
        }

        private void OnDestroy()
        {
            if (_logLevelHandler != null)
                DreadConfig.LogLevelEntry.SettingChanged -= _logLevelHandler;
        }

        private static GameObject CreateSystemHost(string name)
        {
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            return go;
        }
    }
}

