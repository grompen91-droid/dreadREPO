using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem : MonoBehaviour
    {
        private bool _visible;
        private bool _loggedDisabledWhileRunning;

        // Performance sampling.
        private float _smoothedDelta;
        private float _minFps;
        private float _minFpsResetAt;
        private float _memMB;
        private float _nextStatRefresh;

        private void Awake()
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }

        private void OnDestroy()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
        }

        private void OnOverlayConfigChanged(object? sender, System.EventArgs e)
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }

        private void Update()
        {
            if (!GuardOverlayEnabled())
                return;

            SampleFrameStats();

            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;

            if (!IsOverlayVisible())
                return;

            if (Time.realtimeSinceStartup >= _nextStatRefresh)
            {
                _nextStatRefresh = Time.realtimeSinceStartup + 0.5f;
                DreadRuntimeState.DreadPatchCount = CountDreadPatches();
                _memMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            }
        }

        // Runs every frame (even while toggled off) so FPS is accurate the instant the HUD is shown.
        private void SampleFrameStats()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            _smoothedDelta = _smoothedDelta <= 0f ? dt : _smoothedDelta + (dt - _smoothedDelta) * 0.1f;

            float fps = 1f / dt;
            float now = Time.realtimeSinceStartup;
            if (now >= _minFpsResetAt)
            {
                _minFps = fps;
                _minFpsResetAt = now + 2f;
            }
            else if (fps < _minFps)
            {
                _minFps = fps;
            }
        }

        private bool IsOverlayVisible() => _visible && !SemiFunc.MenuLevel();

        private bool GuardOverlayEnabled()
        {
            if (DreadConfig.DebugOverlayEnabled.Value)
                return true;

            if (!_loggedDisabledWhileRunning)
            {
                _loggedDisabledWhileRunning = true;
                LoggingService.LogError(
                    "DebugOverlaySystem ran while DebugOverlayEnabled is false: "
                    + "enable/disable wiring regressed (PERF-2).");
            }

            return false;
        }
    }
}
