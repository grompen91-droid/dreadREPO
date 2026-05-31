using BepInEx.Configuration;
using Dread.Systems;

namespace Dread.Config
{
    public static class DreadConfig
    {
        private static ConfigFile? _configFile;
        // 1. Audio Dread
        public static ConfigEntry<bool> AudioEnabled = null!;
        public static ConfigEntry<float> AudioFrequency = null!;
        public static ConfigEntry<float> AudioVolume = null!;

        // 2. Monster Overhaul
        public static ConfigEntry<bool> MonsterAggressionEnabled = null!;
        public static ConfigEntry<bool> MonsterAudioEnabled = null!;

        // 3. Tension
        public static ConfigEntry<bool> FakeFootstepsEnabled = null!;
        public static ConfigEntry<bool> AdrenalineEnabled = null!;
        public static ConfigEntry<bool> LowStaminaSoundEnabled = null!;
        public static ConfigEntry<bool> PanicSprintEnabled = null!;

        // 4. Psychotic Break
        public static ConfigEntry<bool> PsychoticBreakEnabled = null!;
        public static ConfigEntry<float> PsychoticBreakChancePercent = null!;
        public static ConfigEntry<float> PsychoticBreakDuration = null!;
        public static ConfigEntry<bool> PsychoticBreakOncePerMatch = null!;
        public static ConfigEntry<bool> PsychoticBreakAccentEnabled = null!;

        // 5. QOL
        public static ConfigEntry<bool> CrouchSpeedBoostEnabled = null!;

        // 6. Compatibility
        public static ConfigEntry<bool> CompatibilityMode = null!;
        public static ConfigEntry<bool> CompatibilitySkipConflictingPatches = null!;
        public static ConfigEntry<bool> DebugConsoleGuardEnabled = null!;

        // 7. Error Reporting
        public static ConfigEntry<bool> ErrorReportingEnabled = null!;
        public static ConfigEntry<bool> ErrorReportingPromptShown = null!;

        // 8. Debug Overlay
        public static ConfigEntry<bool> DebugOverlayEnabled = null!;

        // 9. Debug Server
        public static ConfigEntry<bool> DebugServerEnabled = null!;
        public static ConfigEntry<int> DebugServerPort = null!;

        // 10. Logging
        public static ConfigEntry<LogLevel> LogLevelEntry = null!;

        // 11. Testing
        public static ConfigEntry<bool> TestCrashButton = null!;

        private static bool _initialized;

        public static void Initialize(ConfigFile cfg)
        {
            if (_initialized) return;

            _configFile = cfg;

            AudioEnabled = cfg.Bind("1. Audio Dread", "Enabled", true,
                "Ambient horror sounds during runs.");
            AudioFrequency = cfg.Bind("1. Audio Dread", "Frequency", 1.0f,
                new ConfigDescription(
                    "Sound frequency multiplier. 1 = default, 2 = twice as often.",
                    new AcceptableValueRange<float>(0.1f, 10f)));
            AudioVolume = cfg.Bind("1. Audio Dread", "Volume", 0.4f,
                new ConfigDescription(
                    "Ambient sound volume (0.0 - 1.0).",
                    new AcceptableValueRange<float>(0.0f, 1.0f)));

            MonsterAggressionEnabled = cfg.Bind("2. Monster Overhaul", "AggressionEnabled", true,
                "Increase monster speed. HOST ONLY.");
            MonsterAudioEnabled = cfg.Bind("2. Monster Overhaul", "AudioEnabled", true,
                "Lower monster pitch for deeper, scarier sounds.");

            FakeFootstepsEnabled = cfg.Bind("3. Tension", "FakeFootstepsEnabled", true,
                "Occasionally plays footstep sounds behind you with no source.");
            AdrenalineEnabled = cfg.Bind("3. Tension", "AdrenalineEnabled", true,
                "Sprint drains up to 70% slower when an enemy is nearby (within 15m).");
            LowStaminaSoundEnabled = cfg.Bind("3. Tension", "LowStaminaSoundEnabled", true,
                "Plays a gasp sound when sprint energy drops below 10%.");
            PanicSprintEnabled = cfg.Bind("3. Tension", "PanicSprintEnabled", true,
                "Brief 1.25x speed burst when sprinting near an enemy (within 15m). 20s cooldown.");

            PsychoticBreakEnabled = cfg.Bind("4. Psychotic Break", "PsychoticBreakEnabled", true,
                "Master toggle for the Psychotic Break system.");
            PsychoticBreakChancePercent = cfg.Bind("4. Psychotic Break", "PsychoticBreakChancePercent", 1.0f,
                new ConfigDescription(
                    "Target chance (percent) per full eligible hide window when solo and hiding. "
                        + "Internal timing adjusts automatically (0.1-25).",
                    new AcceptableValueRange<float>(0.1f, 25f)));
            PsychoticBreakAccentEnabled = cfg.Bind("4. Psychotic Break", "PsychoticBreakAccentEnabled", true,
                "Horror-colored edge accents during psychotic break (darkness/vignette always on).");
            PsychoticBreakDuration = cfg.Bind("4. Psychotic Break", "PsychoticBreakDuration", 20f,
                new ConfigDescription(
                    "Episode length in seconds.",
                    new AcceptableValueRange<float>(5f, 60f)));
            PsychoticBreakOncePerMatch = cfg.Bind("4. Psychotic Break", "PsychoticBreakOncePerMatch", true,
                "Limit to one episode per match.");

            CrouchSpeedBoostEnabled = cfg.Bind("5. QOL", "CrouchSpeedBoost", true,
                "Crouch movement is 30% faster.");

            CompatibilityMode = cfg.Bind("6. Compatibility", "CompatibilityMode", false,
                "Ambient audio only: disables monster Harmony patches, adrenaline/panic sprint "
                    + "mutation, and psychotic break. Use when another mod conflicts with Dread.");

            CompatibilitySkipConflictingPatches = cfg.Bind(
                "6. Compatibility",
                "SkipConflictingPatches",
                false,
                "If another mod already patched the same game method, skip Dread's patch and log once.");

            DebugConsoleGuardEnabled = cfg.Bind(
                "6. Compatibility",
                "DebugConsoleGuardEnabled",
                true,
                "Suppress NullReferenceException spam from broken DebugConsoleUI hooks "
                    + "(common with MenuLib/REPOConfig). Disable to see raw console errors.");

            ErrorReportingEnabled = cfg.Bind(
                "7. Error Reporting",
                "ErrorReportingEnabled",
                true,
                ErrorReportingPrivacyCopy.FullDescription);

            ErrorReportingPromptShown = cfg.Bind(
                "7. Error Reporting",
                "ErrorReportingPromptShown",
                false,
                "Internal: set true after first-run error reporting disclosure. "
                    + "Do not edit unless resetting the prompt.");

            DebugOverlayEnabled = cfg.Bind(
                "8. Debug Overlay",
                "DebugOverlayEnabled",
                false,
                "Show an in-game IMGUI debug HUD during runs. Press F10 to toggle visibility at runtime. "
                    + "Hidden on menu levels.");

            DebugServerEnabled = cfg.Bind(
                "9. Debug Server",
                "DebugServerEnabled",
                false,
                "Enable the TCP debug server for AI-assisted debugging (localhost only).");
            DebugServerPort = cfg.Bind("9. Debug Server", "DebugServerPort", 15432,
                new ConfigDescription(
                    "Port for the debug server. Falls back to +1 if unavailable.",
                    new AcceptableValueRange<int>(1024, 65535)));

            LogLevelEntry = cfg.Bind(
                "10. Logging",
                "LogLevel",
                LogLevel.Debug,
                "Logging verbosity. None = suppress all output, Error = only errors, "
                    + "Debug = info + warnings + errors, Verbose = everything including debug traces.");

            TestCrashButton = cfg.Bind(
                "11. Testing",
                "Crash Game",
                false,
                new ConfigDescription(
                    "Turn ON to deliberately crash the game and verify error reporting (resets to off). "
                        + "Use only when error reporting is enabled.",
                    null,
                    new ConfigurationManagerAttributes { ShowAsButton = true }));

            ConfigEntryBase?[] allFields =
            [
                AudioEnabled, AudioFrequency, AudioVolume,
                MonsterAggressionEnabled, MonsterAudioEnabled,
                FakeFootstepsEnabled, AdrenalineEnabled, LowStaminaSoundEnabled, PanicSprintEnabled,
                PsychoticBreakEnabled, PsychoticBreakChancePercent, PsychoticBreakDuration,
                PsychoticBreakOncePerMatch, PsychoticBreakAccentEnabled,
                CrouchSpeedBoostEnabled,
                CompatibilityMode, CompatibilitySkipConflictingPatches, DebugConsoleGuardEnabled,
                ErrorReportingEnabled, ErrorReportingPromptShown,
                DebugOverlayEnabled, DebugServerEnabled, DebugServerPort,
                LogLevelEntry, TestCrashButton,
            ];
            for (int i = 0; i < allFields.Length; i++)
            {
                if (allFields[i] == null)
                {
                    LoggingService.LogError($"[Dread] Config field at index {i} is null after Initialize!");
                }
            }

            _initialized = true;
            LoggingService.LogInfo("[Dread] Config initialized successfully.");
        }

        public static void EnsureInitialized()
        {
            if (!_initialized)
                LoggingService.LogError("[Dread] DreadConfig accessed before Initialize() was called!");
        }

        public static void SaveToDisk()
        {
            _configFile?.Save();
        }
    }

    internal class ConfigurationManagerAttributes
    {
        public bool? ShowAsButton;
    }
}
