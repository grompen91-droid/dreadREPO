using UnityEngine.SceneManagement;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Session phase for gating horror gameplay systems.
    /// </summary>
    internal enum GameplayPhase
    {
        Unknown = 0,
        Menu,
        TruckOrShop,
        ExtractionLevel,
    }

    /// <summary>
    /// Unified gates for menu vs truck/shop vs active extraction level.
    /// </summary>
    internal static class GameplayContext
    {
        public static bool IsMenuLevel() => SemiFunc.MenuLevel();

        public static bool IsGameplaySceneName(string sceneName) =>
            sceneName.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Main", System.StringComparison.OrdinalIgnoreCase) < 0;

        public static bool IsInLevel() =>
            IsGameplaySceneName(SceneManager.GetActiveScene().name);

        /// <summary>
        /// Horror gameplay allowed during an active run. Uses <see cref="IsMenuLevel"/>
        /// (same as tension and psychotic break). Scene-name heuristics in
        /// <see cref="IsInLevel"/> are not used here because R.E.P.O. often keeps
        /// an active scene named Main while the player is in a level.
        /// </summary>
        public static bool IsRun() => !IsMenuLevel();

        public static GameplayPhase CurrentPhase => GameplayPhaseCompat.ResolvePhase();

        /// <summary>
        /// Host-only monster features (camp lure, snitch POI) may run only during extraction.
        /// </summary>
        public static bool AllowsHostMonsterFeatures =>
            CurrentPhase == GameplayPhase.ExtractionLevel;

        public static string PhaseLabel => CurrentPhase switch
        {
            GameplayPhase.Menu => "menu",
            GameplayPhase.TruckOrShop => "truck/shop",
            GameplayPhase.ExtractionLevel => "run",
            _ => "unknown",
        };
    }
}
