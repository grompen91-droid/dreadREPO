using System;
using Dread.Config;
using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Applies and removes Harmony patches from <see cref="HarmonyPatchRegistry"/>; wires config change handlers.
    /// </summary>
    internal static class PatchLifecycle
    {
        private static Harmony? _harmony;
        private static EventHandler? _monsterHandler;
        private static EventHandler? _crouchHandler;
        private static EventHandler? _debugConsoleHandler;

        public static void Initialize(Harmony harmony)
        {
            _harmony = harmony;

            ApplyAllEnabled();

            _monsterHandler = (_, _) => SyncMonsterPatches();
            DreadConfig.MonsterAggressionEnabled.SettingChanged += _monsterHandler;
            DreadConfig.CompatibilityMode.SettingChanged += _monsterHandler;

            _crouchHandler = (_, _) => SyncRegistration(
                "player-controller-awake",
                DreadConfig.CrouchSpeedBoostEnabled.Value);
            DreadConfig.CrouchSpeedBoostEnabled.SettingChanged += _crouchHandler;

            _debugConsoleHandler = (_, _) => SyncRegistration(
                "debug-console-guard",
                DreadConfig.DebugConsoleGuardEnabled.Value);
            DreadConfig.DebugConsoleGuardEnabled.SettingChanged += _debugConsoleHandler;
        }

        public static void Shutdown()
        {
            if (_harmony == null)
                return;

            foreach (var reg in HarmonyPatchRegistry.Registrations)
                reg.Remove(_harmony);

            if (_monsterHandler != null)
            {
                DreadConfig.MonsterAggressionEnabled.SettingChanged -= _monsterHandler;
                DreadConfig.CompatibilityMode.SettingChanged -= _monsterHandler;
                _monsterHandler = null;
            }

            if (_crouchHandler != null)
            {
                DreadConfig.CrouchSpeedBoostEnabled.SettingChanged -= _crouchHandler;
                _crouchHandler = null;
            }

            if (_debugConsoleHandler != null)
            {
                DreadConfig.DebugConsoleGuardEnabled.SettingChanged -= _debugConsoleHandler;
                _debugConsoleHandler = null;
            }

            _harmony = null;
        }

        private static void ApplyAllEnabled()
        {
            if (_harmony == null)
                return;

            foreach (var reg in HarmonyPatchRegistry.Registrations)
            {
                if (reg.IsEnabled == null || reg.IsEnabled())
                    reg.Apply(_harmony);
            }
        }

        private static void SyncMonsterPatches()
        {
            var enabled = DreadFeaturePolicy.MonsterHarmonyPatchesEnabled;
            SyncRegistration("monster-navmesh-awake", enabled);
            SyncRegistration("monster-director-investigate", enabled);
        }

        private static void SyncRegistration(string id, bool enabled)
        {
            if (_harmony == null)
                return;

            foreach (var reg in HarmonyPatchRegistry.Registrations)
            {
                if (reg.Id != id)
                    continue;

                if (enabled)
                    reg.Apply(_harmony);
                else
                    reg.Remove(_harmony);
                return;
            }
        }
    }
}
