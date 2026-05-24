using BepInEx.Configuration;
using Dread.Systems;

namespace Dread.Config
{
    public static class DreadConfig
    {
        // Audio Dread
        public static ConfigEntry<bool> AudioEnabled = null!;
        public static ConfigEntry<float> AudioFrequency = null!;
        public static ConfigEntry<float> AudioVolume = null!;

        // Monster Overhaul
        public static ConfigEntry<bool> MonsterAggressionEnabled = null!;
        public static ConfigEntry<bool> MonsterAudioEnabled = null!;

        private static bool _initialized;

        // QOL
        public static ConfigEntry<bool> CrouchSpeedBoostEnabled = null!;

        // Tension
        public static ConfigEntry<bool> FakeFootstepsEnabled = null!;
        public static ConfigEntry<bool> AdrenalineEnabled = null!;
        public static ConfigEntry<bool> LowStaminaSoundEnabled = null!;
        public static ConfigEntry<bool> PanicSprintEnabled = null!;

        // 5. Error Reporting
        public static ConfigEntry<bool> ErrorReportingEnabled = null!;

        // 6. Psychotic Break
        public static ConfigEntry<bool> PsychoticBreakEnabled = null!;
        public static ConfigEntry<float> PsychoticBreakTriggerChance = null!;
        public static ConfigEntry<float> PsychoticBreakDuration = null!;
        public static ConfigEntry<bool> PsychoticBreakOncePerMatch = null!;

        // 7. Testing
        public static ConfigEntry<bool> TestCrashButton = null!;

        // 8. Logging
        public static ConfigEntry<LogLevel> LogLevelEntry = null!;

        public static void Initialize(ConfigFile cfg)
        {
            if (_initialized) return;

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

            CrouchSpeedBoostEnabled = cfg.Bind("4. QOL", "CrouchSpeedBoost", true,
                "Crouch movement is 30% faster.");

            ErrorReportingEnabled = cfg.Bind("5. Error Reporting", "ErrorReportingEnabled", true,
                "Send anonymous error reports to the developer when crashes occur. "
                    + "Helps fix bugs faster. Disable if you prefer no telemetry.");

            PsychoticBreakEnabled = cfg.Bind("6. Psychotic Break", "PsychoticBreakEnabled", true,
                "Master toggle for the Psychotic Break system.");
            PsychoticBreakTriggerChance = cfg.Bind("6. Psychotic Break", "PsychoticBreakTriggerChance", 0.01f,
                new ConfigDescription(
                    "Probability per 2s check (0-1). 0.01 = 1%.",
                    new AcceptableValueRange<float>(0f, 1f)));
            PsychoticBreakDuration = cfg.Bind("6. Psychotic Break", "PsychoticBreakDuration", 20f,
                new ConfigDescription(
                    "Episode length in seconds.",
                    new AcceptableValueRange<float>(5f, 60f)));
            PsychoticBreakOncePerMatch = cfg.Bind("6. Psychotic Break", "PsychoticBreakOncePerMatch", true,
                "Limit to one episode per match.");

            TestCrashButton = cfg.Bind(
                "7. Testing",
                "Crash Game",
                false,
                new ConfigDescription(
                    "Click to crash the game and verify error reporting works.",
                    null,
                    new ConfigurationManagerAttributes { ShowAsButton = true }
                )
            );

            LogLevelEntry = cfg.Bind("8. Logging", "LogLevel", LogLevel.Debug,
                "Logging verbosity. None = suppress all output, Error = only errors, "
                    + "Debug = info + warnings + errors, Verbose = everything including debug traces.");

            ConfigEntryBase?[] allFields =
            [
                AudioEnabled, AudioFrequency, AudioVolume,
                MonsterAggressionEnabled, MonsterAudioEnabled,
                CrouchSpeedBoostEnabled,
                FakeFootstepsEnabled, AdrenalineEnabled, LowStaminaSoundEnabled, PanicSprintEnabled,
                ErrorReportingEnabled,
                PsychoticBreakEnabled, PsychoticBreakTriggerChance, PsychoticBreakDuration, PsychoticBreakOncePerMatch,
                TestCrashButton, LogLevelEntry,
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
    }

    internal class ConfigurationManagerAttributes
    {
        public bool? ShowAsButton;
    }
}
