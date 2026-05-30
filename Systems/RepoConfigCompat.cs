using HarmonyLib;

namespace Dread.Systems
{
    /// <summary>
    /// REPOConfig-specific Harmony compat (slider labels only). REPOConfig has no API for bool toggle descriptions.
    /// </summary>
    internal static class RepoConfigCompat
    {
        internal static void TryApply(Harmony harmony)
        {
            RepoConfigSliderLabelCompat.TryApply(harmony);
        }
    }
}
