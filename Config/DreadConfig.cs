using BepInEx.Configuration;

namespace Dread.Config
{
    public static class DreadConfig
    {
        // Host Options
        public static ConfigEntry<bool> GammaForceEnabled = null!;
        public static ConfigEntry<int> GammaValue = null!;
        public static ConfigEntry<bool> PixelationForceEnabled = null!;
        public static ConfigEntry<int> PixelationValue = null!;

        // Audio Dread
        public static ConfigEntry<bool> AudioEnabled = null!;
        public static ConfigEntry<float> AudioFrequency = null!;
        public static ConfigEntry<float> AudioVolume = null!;

        // Monster Overhaul
        public static ConfigEntry<bool> MonsterAggressionEnabled = null!;
        public static ConfigEntry<bool> MonsterAudioEnabled = null!;

        // QOL
        public static ConfigEntry<bool> CrouchSpeedBoostEnabled = null!;

        // Tension
        public static ConfigEntry<bool> FakeFootstepsEnabled = null!;
        public static ConfigEntry<bool> AdrenalineEnabled = null!;
        public static ConfigEntry<bool> LowStaminaSoundEnabled = null!;
        public static ConfigEntry<bool> PanicSprintEnabled = null!;

        public static void Initialize(ConfigFile cfg)
        {
            GammaForceEnabled = cfg.Bind("0. Host Options", "GammaForceEnabled", false,
                "HOST ONLY. Force all clients to use a specific gamma value on level load.");
            GammaValue = cfg.Bind("0. Host Options", "GammaValue", 40,
                "Gamma value to push to all clients (0-100). Default 40 matches game default.");
            PixelationForceEnabled = cfg.Bind("0. Host Options", "PixelationForceEnabled", false,
                "HOST ONLY. Force all clients to use a specific render size (pixelation) on level load.");
            PixelationValue = cfg.Bind("0. Host Options", "PixelationValue", 100,
                "Render size percentage to push to all clients (1-100). 100 = no pixelation, lower = more pixelated.");

            AudioEnabled = cfg.Bind("1. Audio Dread", "Enabled", true,
                "Ambient horror sounds during runs.");
            AudioFrequency = cfg.Bind("1. Audio Dread", "Frequency", 1.0f,
                "Sound frequency multiplier. 1 = default, 2 = twice as often.");
            AudioVolume = cfg.Bind("1. Audio Dread", "Volume", 0.4f,
                "Ambient sound volume (0.0 - 1.0).");

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
        }
    }
}
