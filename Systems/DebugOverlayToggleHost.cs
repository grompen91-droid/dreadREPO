using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Lightweight always-on host for F10 polling. No OnGUI.
    /// </summary>
    internal class DebugOverlayToggleHost : MonoBehaviour
    {
        private int _lastPollLogFrame = -9999;

        private void Start()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
            ApplyEnabled();
        }

        private void OnDestroy()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
        }

        private void OnOverlayConfigChanged(object? sender, System.EventArgs e) => ApplyEnabled();

        private void ApplyEnabled()
        {
            enabled = DreadConfig.DebugOverlayEnabled.Value;
            if (!enabled)
                DebugOverlayTogglePoll.RequestHide();
        }

        private void Update()
        {
            DebugOverlayTogglePoll.Poll();

            // #region agent log
            if (Time.frameCount - _lastPollLogFrame >= 600)
            {
                _lastPollLogFrame = Time.frameCount;
                DebugAgentLog.Write(
                    "G",
                    "DebugOverlayToggleHost.cs:Update",
                    "poll_host_tick",
                    "post-fix",
                    ("overlayEnabled", DreadConfig.DebugOverlayEnabled.Value),
                    ("overlayVisible", DebugOverlayTogglePoll.IsOverlayVisible),
                    ("hasInstance", DebugOverlayTogglePoll.HasInstance),
                    ("frame", Time.frameCount));
            }
            // #endregion
        }
    }
}
