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
        public const string VERSION = "1.4.2";

        internal static new ManualLogSource Logger = null!;

        private readonly Harmony _harmony = new(GUID);

        private void Awake()
        {
            Logger = base.Logger;
            DreadConfig.Initialize(Config);
            _harmony.PatchAll();
            Logger.LogInfo($"{NAME} v{VERSION} loaded.");
        }

        private void Start()
        {
            var host = new GameObject("DreadHost");
            DontDestroyOnLoad(host);
            host.AddComponent<AudioDreadSystem>();
            host.AddComponent<MonsterOverhaulSystem>();
            host.AddComponent<TensionSystem>();
        }
    }
}
