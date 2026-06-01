using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem : MonoBehaviour
    {
        private bool _visible;
        private bool _loggedDisabledWhileRunning;

        // Cursor and input capture so the overlay can be clicked (pause to inspect).
        // Toggled by F9, independent of the F10 show/hide toggle.
        private bool _interactive;
        private bool _cursorCaptured;
        private CursorLockMode _savedLockState;
        private bool _savedCursorVisible;
        private bool _inputLocked;

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
            LoadLayout();
        }

        private void OnDestroy()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
            ReleaseOverlayCapture();
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

            // F9 toggles interactive mode (free the mouse to click the controls).
            // F10 only shows/hides the panel; it no longer steals the cursor.
            if (Input.GetKeyDown(KeyCode.F9))
                _interactive = !_interactive;

            if (!IsOverlayVisible())
            {
                _interactive = false;
                if (_cursorCaptured)
                    ReleaseOverlayCapture();
                return;
            }

            if (_interactive)
            {
                MaintainOverlayCursor();
                HandleDrag();
            }
            else if (_cursorCaptured)
            {
                ReleaseOverlayCapture();
            }

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

        // Free and show the cursor and suspend player input while the overlay is open.
        // The game re-locks the cursor each frame, so this is reasserted every frame.
        private void MaintainOverlayCursor()
        {
            if (!_cursorCaptured)
            {
                _savedLockState = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _cursorCaptured = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (!_inputLocked)
            {
                var pc = PlayerController.instance;
                if ((object)pc != null)
                {
                    PlayerInputLockCompat.SetLocked(pc, locked: true);
                    _inputLocked = true;
                }
            }
        }

        private void ReleaseOverlayCapture()
        {
            if (_cursorCaptured)
            {
                Cursor.lockState = _savedLockState;
                Cursor.visible = _savedCursorVisible;
                _cursorCaptured = false;
            }

            if (_inputLocked)
            {
                var pc = PlayerController.instance;
                if ((object)pc != null)
                    PlayerInputLockCompat.SetLocked(pc, locked: false);
                _inputLocked = false;
            }
        }
    }
}
