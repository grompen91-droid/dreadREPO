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

        /// <summary>Label of the player currently being lured (empty = none).</summary>
        public static string LureTarget { get; internal set; } = "";
        /// <summary>Seconds the lure target has been camping.</summary>
        public static float LureCampTimer { get; internal set; }
        /// <summary>Escalation step of the active lure (0 = inactive).</summary>
        public static int LurePullStep { get; internal set; }
        public static bool SnitchEnabled { get; internal set; }
        public static string SnitchBlockReason { get; internal set; } = "";
        /// <summary>Current snitch state: "disarmed", "armed", or "triggered".</summary>
        public static string SnitchState { get; internal set; } = "disarmed";
        /// <summary>Seconds remaining on the snitch POI loop (0 = inactive).</summary>
        public static float SnitchPoiRemaining { get; internal set; }
        /// <summary>Distance in metres from local camera to snitch item (-1 = unknown).</summary>
        public static float SnitchItemDistance { get; internal set; } = -1f;
        /// <summary>Seconds until next arm attempt (-1 = armed/triggered).</summary>
        public static float SnitchNextCheckIn { get; internal set; } = -1f;

        /// <summary>Current gameplay phase label for overlay (menu, truck/shop, extraction).</summary>
        public static string GameplayPhase { get; internal set; } = "";
        /// <summary>Seconds remaining on lure cooldown for active target (0 = none).</summary>
        public static float LureCooldownRemaining { get; internal set; }
        /// <summary>Why camp lure is inactive (empty when running).</summary>
        public static string LureBlockReason { get; internal set; } = "";

        public static int DreadPatchCount { get; internal set; }
    }
}
