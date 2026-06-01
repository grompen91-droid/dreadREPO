using System;
using System.Collections.Generic;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;

namespace Dread.Systems.Core
{
    internal enum PatchGroup
    {
        Monster,
        Player,
        Debug,
    }

    internal sealed class PatchRegistration
    {
        public PatchRegistration(
            string id,
            PatchGroup group,
            Action<Harmony> apply,
            Action<Harmony> remove,
            Func<bool>? isEnabled = null)
        {
            Id = id;
            Group = group;
            Apply = apply;
            Remove = remove;
            IsEnabled = isEnabled;
        }

        public string Id { get; }
        public PatchGroup Group { get; }
        public Action<Harmony> Apply { get; }
        public Action<Harmony> Remove { get; }
        public Func<bool>? IsEnabled { get; }
    }

    /// <summary>
    /// Central registry of explicit Harmony apply/remove pairs (ADR-0009).
    /// </summary>
    internal static class HarmonyPatchRegistry
    {
        public static IReadOnlyList<PatchRegistration> Registrations { get; } =
            new List<PatchRegistration>
            {
                new(
                    "monster-navmesh-awake",
                    PatchGroup.Monster,
                    EnemyNavMeshAgentAwakePatch.Apply,
                    EnemyNavMeshAgentAwakePatch.Remove,
                    MonsterHarmonyPatchesEnabled),
                new(
                    "monster-director-investigate",
                    PatchGroup.Monster,
                    EnemyDirectorSetInvestigatePatch.Apply,
                    EnemyDirectorSetInvestigatePatch.Remove,
                    MonsterHarmonyPatchesEnabled),
                new(
                    "player-controller-awake",
                    PatchGroup.Player,
                    PlayerControllerAwakePatch.Apply,
                    PlayerControllerAwakePatch.Remove,
                    () => DreadConfig.CrouchSpeedBoostEnabled.Value),
                new(
                    "debug-console-guard",
                    PatchGroup.Debug,
                    DebugConsoleGuardPatch.Apply,
                    DebugConsoleGuardPatch.Remove,
                    () => DreadConfig.DebugConsoleGuardEnabled.Value),
                new(
                    "snitch-level-gen-done",
                    PatchGroup.Monster,
                    SnitchLevelGenDonePatch.Apply,
                    SnitchLevelGenDonePatch.Remove),
            };

        private static bool MonsterHarmonyPatchesEnabled() =>
            DreadFeaturePolicy.MonsterHarmonyPatchesEnabled;
    }
}
