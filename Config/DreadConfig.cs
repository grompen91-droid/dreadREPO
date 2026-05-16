using BepInEx.Configuration;

namespace Dread.Config
{
    public static class DreadConfig
    {
        // Audio Dread
        public static ConfigEntry<bool> AudioEnabled = null!;
        public static ConfigEntry<float> AudioFrequency = null!;
        public static ConfigEntry<float> AudioVolume = null!;

        // Visual Corruption
        public static ConfigEntry<bool> LightFlickerEnabled = null!;
        public static ConfigEntry<bool> VignetteEnabled = null!;
        public static ConfigEntry<bool> ShadowGlitchEnabled = null!;

        // Environmental
        public static ConfigEntry<bool> EnvironmentalEnabled = null!;
        public static ConfigEntry<float> RareEventChance = null!;

        // Monster Overhaul
        public static ConfigEntry<bool> MonsterHPEnabled = null!;
        public static ConfigEntry<float> MonsterHPMultiplier = null!;
        public static ConfigEntry<bool> MonsterAggressionEnabled = null!;
        public static ConfigEntry<bool> MonsterAudioEnabled = null!;
        public static ConfigEntry<bool> MonsterVisualEnabled = null!;

        public static void Initialize(ConfigFile cfg)
        {
            AudioEnabled = cfg.Bind("1. Audio Dread", "Enabled", true,
                "Ambient horror sounds during runs.");
            AudioFrequency = cfg.Bind("1. Audio Dread", "Frequency", 1.0f,
                "Sound frequency multiplier. 1 = default, 2 = twice as often.");
            AudioVolume = cfg.Bind("1. Audio Dread", "Volume", 0.4f,
                "Ambient sound volume (0.0 - 1.0).");

            LightFlickerEnabled = cfg.Bind("2. Visual Corruption", "LightFlicker", true,
                "Random lights briefly flicker.");
            VignetteEnabled = cfg.Bind("2. Visual Corruption", "Vignette", true,
                "Screen edges pulse dark occasionally.");
            ShadowGlitchEnabled = cfg.Bind("2. Visual Corruption", "ShadowGlitch", true,
                "A shadow briefly appears at the edge of view.");

            EnvironmentalEnabled = cfg.Bind("3. Environmental", "Enabled", true,
                "Small objects and lights subtly shift between visits.");
            RareEventChance = cfg.Bind("3. Environmental", "RareEventChance", 0.15f,
                "Probability a rare event fires on room revisit (0.0 - 1.0).");

            MonsterHPEnabled = cfg.Bind("4. Monster Overhaul", "HPEnabled", true,
                "Multiply monster HP. HOST ONLY.");
            MonsterHPMultiplier = cfg.Bind("4. Monster Overhaul", "HPMultiplier", 2.0f,
                "Monster HP multiplier. Default 2x.");
            MonsterAggressionEnabled = cfg.Bind("4. Monster Overhaul", "AggressionEnabled", true,
                "Increase monster speed. HOST ONLY.");
            MonsterAudioEnabled = cfg.Bind("4. Monster Overhaul", "AudioEnabled", true,
                "Lower monster pitch for deeper, scarier sounds.");
            MonsterVisualEnabled = cfg.Bind("4. Monster Overhaul", "VisualEnabled", true,
                "Screen distortion effect when a monster is nearby.");
        }
    }
}
