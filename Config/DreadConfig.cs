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

        // 10. Compatibility
        public static ConfigEntry<bool> CompatibilityMode = null!;
        public static ConfigEntry<bool> CompatibilitySkipConflictingPatches = null!;
        public static ConfigEntry<bool> DebugConsoleGuardEnabled = null!;

        // 6. Psychotic Break
        public static ConfigEntry<bool> PsychoticBreakEnabled = null!;
        public static ConfigEntry<float> PsychoticBreakTriggerChance = null!;
        public static ConfigEntry<float> PsychoticBreakDuration = null!;
        public static ConfigEntry<bool> PsychoticBreakOncePerMatch = null!;

        // 7. Testing
        public static ConfigEntry<bool> TestCrashButton = null!;

        // 8. Debug Server
        public static ConfigEntry<bool> DebugServerEnabled = null!;
        public static ConfigEntry<int> DebugServerPort = null!;

        // 11. Debug Overlay
        public static ConfigEntry<bool> DebugOverlayEnabled = null!;
        public static ConfigEntry<string> DebugOverlayScreenAnchor = null!;
        public static ConfigEntry<float> DebugOverlayOffsetX = null!;
        public static ConfigEntry<float> DebugOverlayOffsetY = null!;
        public static ConfigEntry<float> DebugOverlayPanelWidth = null!;
        public static ConfigEntry<int> DebugOverlayFontSize = null!;
        public static ConfigEntry<float> DebugOverlayBackgroundAlpha = null!;
        public static ConfigEntry<string> DebugOverlayToggleKey = null!;

        // 9. Logging
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

            ErrorReportingEnabled = cfg.Bind("5. Error Reporting", "ErrorReportingEnabled", false,
                "Send anonymous error reports to the developer when crashes occur. "
                    + "Opt-in. Helps fix bugs faster. Leave off if you prefer no telemetry.");

            CompatibilityMode = cfg.Bind("10. Compatibility", "CompatibilityMode", false,
                "Ambient audio only: disables monster Harmony patches, adrenaline/panic sprint "
                    + "mutation, and psychotic break. Use when another mod conflicts with Dread.");

            CompatibilitySkipConflictingPatches = cfg.Bind(
                "10. Compatibility",
                "SkipConflictingPatches",
                false,
                "If another mod already patched the same game method, skip Dread's patch and log once.");

            DebugConsoleGuardEnabled = cfg.Bind(
                "10. Compatibility",
                "DebugConsoleGuardEnabled",
                true,
                "Suppress NullReferenceException spam from broken DebugConsoleUI hooks "
                    + "(common with MenuLib/REPOConfig). Disable to see raw console errors.");

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

            DebugServerEnabled = cfg.Bind("8. Debug Server", "DebugServerEnabled", false,
                "Enable the TCP debug server for AI-assisted debugging (localhost only).");
            DebugServerPort = cfg.Bind("8. Debug Server", "DebugServerPort", 15432,
                new ConfigDescription(
                    "Port for the debug server. Falls back to +1 if unavailable.",
                    new AcceptableValueRange<int>(1024, 65535)));

            DebugOverlayEnabled = cfg.Bind("11. Debug Overlay", "DebugOverlayEnabled", false,
                "Enable the in-game IMGUI debug HUD (hidden until you press ToggleKey). "
                    + "Hidden on menu levels. Keep off unless debugging.");

            DebugOverlayScreenAnchor = cfg.Bind(
                "11. Debug Overlay",
                "Anchor",
                "TopLeft",
                new ConfigDescription(
                    "Screen corner for the overlay panel.",
                    new AcceptableValueList<string>(new[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" })));

            DebugOverlayOffsetX = cfg.Bind("11. Debug Overlay", "OffsetX", 0f,
                new ConfigDescription(
                    "Horizontal offset in pixels from the anchor corner.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            DebugOverlayOffsetY = cfg.Bind("11. Debug Overlay", "OffsetY", 0f,
                new ConfigDescription(
                    "Vertical offset in pixels from the anchor corner.",
                    new AcceptableValueRange<float>(-2000f, 2000f)));
            DebugOverlayPanelWidth = cfg.Bind("11. Debug Overlay", "PanelWidth", 400f,
                new ConfigDescription(
                    "Overlay panel width in pixels.",
                    new AcceptableValueRange<float>(260f, 900f)));

            DebugOverlayFontSize = cfg.Bind("11. Debug Overlay", "FontSize", 14,
                new ConfigDescription(
                    "Overlay text size in pixels.",
                    new AcceptableValueRange<int>(8, 32)));

            DebugOverlayBackgroundAlpha = cfg.Bind("11. Debug Overlay", "BackgroundAlpha", 0.82f,
                new ConfigDescription(
                    "Panel background opacity (0 = transparent, 1 = opaque).",
                    new AcceptableValueRange<float>(0.2f, 1f)));

            DebugOverlayToggleKey = cfg.Bind("11. Debug Overlay", "ToggleKey", "F10",
                "Hotkey to show/hide the overlay at runtime (e.g. F10, F9). Uses Unity Input System when available.");

            LogLevelEntry = cfg.Bind("9. Logging", "LogLevel", LogLevel.Debug,
                "Logging verbosity. None = suppress all output, Error = only errors, "
                    + "Debug = info + warnings + errors, Verbose = everything including debug traces.");

            ConfigEntryBase?[] allFields =
            [
                AudioEnabled, AudioFrequency, AudioVolume,
                MonsterAggressionEnabled, MonsterAudioEnabled,
                CrouchSpeedBoostEnabled,
                FakeFootstepsEnabled, AdrenalineEnabled, LowStaminaSoundEnabled, PanicSprintEnabled,
                ErrorReportingEnabled,
                CompatibilityMode, CompatibilitySkipConflictingPatches, DebugConsoleGuardEnabled,
                PsychoticBreakEnabled, PsychoticBreakTriggerChance, PsychoticBreakDuration, PsychoticBreakOncePerMatch,
                TestCrashButton, DebugServerEnabled, DebugServerPort,
                DebugOverlayEnabled, DebugOverlayScreenAnchor, DebugOverlayOffsetX, DebugOverlayOffsetY,
                DebugOverlayPanelWidth, DebugOverlayFontSize, DebugOverlayBackgroundAlpha,
                DebugOverlayToggleKey,
                LogLevelEntry,
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
        public bool? Browsable;
        public bool? ShowRangeAsPercent;
        public int? Order;
    }
}
