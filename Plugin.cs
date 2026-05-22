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
        public const string VERSION = "1.5.1";

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
            var host = new GameObject("DreadHost");
            DontDestroyOnLoad(host);
            int count = 0;
            if (host.AddComponent<AudioDreadSystem>() != null) count++;
            else Logger.LogError("Failed to add AudioDreadSystem component.");
            if (host.AddComponent<MonsterOverhaulSystem>() != null) count++;
            else Logger.LogError("Failed to add MonsterOverhaulSystem component.");
            if (host.AddComponent<TensionSystem>() != null) count++;
            else Logger.LogError("Failed to add TensionSystem component.");
            if (count > 0)
                Logger.LogInfo($"Systems initialized ({count}/3) on DreadHost.");
            else
                Logger.LogError("All systems failed to initialize on DreadHost.");
        }
    }
}

