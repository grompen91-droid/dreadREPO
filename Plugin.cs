using BepInEx;
using BepInEx.Logging;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;
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

        private readonly Harmony _harmony = new(GUID);

        private void Awake()
        {
            Logger = base.Logger;
            DreadConfig.Initialize(Config);

            if (DreadConfig.MonsterAggressionEnabled.Value)
            {
                EnemyNavMeshAgentAwakePatch.Apply(_harmony);
                EnemyDirectorSetInvestigatePatch.Apply(_harmony);
            }
            if (DreadConfig.CrouchSpeedBoostEnabled.Value)
                PlayerControllerAwakePatch.Apply(_harmony);

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

            Logger.LogInfo($"{NAME} v{VERSION} loaded.");
        }

        private void Start()
        {
            int count = 0;
            if (CreateSystemHost("DreadAudioHost").AddComponent<AudioDreadSystem>() != null) count++;
            else Logger.LogError("Failed to add AudioDreadSystem component.");
            if (CreateSystemHost("DreadMonsterHost").AddComponent<MonsterOverhaulSystem>() != null) count++;
            else Logger.LogError("Failed to add MonsterOverhaulSystem component.");
            if (CreateSystemHost("DreadTensionHost").AddComponent<TensionSystem>() != null) count++;
            else Logger.LogError("Failed to add TensionSystem component.");
            if (CreateSystemHost("DreadErrorHost").AddComponent<ErrorReporterSystem>() != null) count++;
            else Logger.LogError("Failed to add ErrorReporterSystem component.");
            if (CreateSystemHost("DreadPsychoticBreakHost").AddComponent<PsychoticBreakSystem>() != null) count++;
            else Logger.LogError("Failed to add PsychoticBreakSystem component.");
            if (CreateSystemHost("DreadTestCrashHost").AddComponent<TestCrashSystem>() != null) count++;
            else Logger.LogError("Failed to add TestCrashSystem component.");
            if (count > 0)
                Logger.LogInfo($"Systems initialized ({count}/6).");
            else
                Logger.LogError("All systems failed to initialize.");
        }

        private static GameObject CreateSystemHost(string name)
        {
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            return go;
        }
    }
}

