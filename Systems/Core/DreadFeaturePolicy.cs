using Dread.Config;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Single source for Compatibility mode and related feature gates (ADR-0016).
    /// </summary>
    internal static class DreadFeaturePolicy
    {
        public static bool CompatibilityMode => DreadConfig.CompatibilityMode.Value;

        public static bool MonsterHarmonyPatchesEnabled =>
            DreadConfig.MonsterAggressionEnabled.Value && !CompatibilityMode;

        public static bool TensionMutationsEnabled => !CompatibilityMode;

        public static bool AdrenalineEnabled =>
            DreadConfig.AdrenalineEnabled.Value && TensionMutationsEnabled;

        public static bool PanicSprintEnabled =>
            DreadConfig.PanicSprintEnabled.Value && TensionMutationsEnabled;

        public static bool LowStaminaSoundEnabled =>
            DreadConfig.LowStaminaSoundEnabled.Value && TensionMutationsEnabled;

        public static bool FakeFootstepsEnabled =>
            DreadConfig.FakeFootstepsEnabled.Value && TensionMutationsEnabled;

        public static bool PsychoticBreakEnabled =>
            DreadConfig.PsychoticBreakEnabled.Value && !CompatibilityMode;
    }
}
