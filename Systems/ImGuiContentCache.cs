using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Avoid GUIContent.none (missing on some REPO/Proton Unity builds).
    /// </summary>
    internal static class ImGuiContentCache
    {
        private static GUIContent? _empty;

        public static GUIContent Empty => _empty ??= new GUIContent("");
    }
}
