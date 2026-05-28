namespace Dread.Systems
{
    /// <summary>
    /// Lightweight snapshot of live mod state for debug overlay and tooling.
    /// Updated by gameplay systems on their existing refresh cadence.
    /// </summary>
    internal static class DreadRuntimeState
    {
        public static float NearestEnemyDist { get; internal set; } = float.MaxValue;

        public static bool AdrenalineActive { get; internal set; }
        public static bool PanicSprintActive { get; internal set; }
        public static float PanicSprintCooldown { get; internal set; }

        public static bool PsychoticBreakEnabled { get; internal set; }
        public static bool PsychoticBreakCanTrigger { get; internal set; }
        public static string PsychoticBreakBlockReason { get; internal set; } = "";
        public static bool PsychoticBreakEpisodeActive { get; internal set; }
        public static float PsychoticBreakEpisodeTimer { get; internal set; }
        public static float PsychoticBreakEpisodeDuration { get; internal set; }
        public static float PsychoticBreakNextCheckIn { get; internal set; }
        /// <summary>Seconds remaining on recent-threat memory (0 = none).</summary>
        public static int PsychoticBreakThreatCount { get; internal set; }
        public static int PsychoticBreakEnemyCount { get; internal set; }
        public static bool PsychoticBreakClipsLoaded { get; internal set; }

        public static int AudioClipCount { get; internal set; }
        public static float AudioNextPlayIn { get; internal set; } = -1f;

        public static int DreadPatchCount { get; internal set; }
    }
}
