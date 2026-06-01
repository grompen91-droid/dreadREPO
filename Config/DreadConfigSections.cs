namespace Dread.Config
{
    /// <summary>
    /// BepInEx config section headers. Numbers shift when debug-only sections are compiled out.
    /// </summary>
    internal static class DreadConfigSections
    {
        public const string Logging =
#if DREAD_DEBUG
            "10. Logging";
#else
            "8. Logging";
#endif

#if DREAD_DEBUG
        public const string DebugOverlay = "8. Debug Overlay";
        public const string DebugServer = "9. Debug Server";
        public const string Testing = "11. Testing";
#endif
    }
}
