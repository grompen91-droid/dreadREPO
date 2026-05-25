using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Zero-cost toggle polling while the overlay is hidden (called from TensionSystem).
    /// </summary>
    internal static class DebugOverlayTogglePoll
    {
        private static DebugOverlaySystem? _instance;

        internal static bool HasInstance => _instance != null;

        internal static bool IsOverlayVisible => _instance != null && _instance.Visible;

        internal static void RequestHide() => _instance?.ForceHide();

        internal static void Register(DebugOverlaySystem instance) => _instance = instance;

        internal static void Unregister(DebugOverlaySystem instance)
        {
            if (_instance == instance)
                _instance = null;
        }

        internal static void Poll()
        {
            if (_instance == null)
            {
                // #region agent log
                DebugAgentLog.Write(
                    "F",
                    "DebugOverlayTogglePoll.cs:Poll",
                    "no_instance",
                    "post-fix",
                    ("overlayEnabled", DreadConfig.DebugOverlayEnabled.Value),
                    ("frame", Time.frameCount));
                // #endregion
                return;
            }

            if (!DreadConfig.DebugOverlayEnabled.Value)
                return;

            _instance.PollToggleFromExternal();
        }
    }
}
