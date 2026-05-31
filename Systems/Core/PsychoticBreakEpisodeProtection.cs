namespace Dread.Systems.Core
{
    /// <summary>
    /// Client-local guard for debug-forced episodes only (MCP / ForceEpisodeForDebug).
    /// Natural triggers do not enable this: hiding during a real chase should stay dangerous.
    /// </summary>
    internal static class PsychoticBreakEpisodeProtection
    {
        public static bool IsActive { get; private set; }

        public static void SetActive(bool active) => IsActive = active;
    }
}
