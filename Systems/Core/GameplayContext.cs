using UnityEngine.SceneManagement;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Unified gates for menu vs in-level vs active run gameplay.
    /// </summary>
    internal static class GameplayContext
    {
        public static bool IsMenuLevel() => SemiFunc.MenuLevel();

        public static bool IsGameplaySceneName(string sceneName) =>
            sceneName.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Main", System.StringComparison.OrdinalIgnoreCase) < 0;

        public static bool IsInLevel() =>
            IsGameplaySceneName(SceneManager.GetActiveScene().name);

        /// <summary>Horror gameplay allowed: not menu UI and in a gameplay scene.</summary>
        public static bool IsRun() => !IsMenuLevel() && IsInLevel();
    }
}
