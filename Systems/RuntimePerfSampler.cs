using UnityEngine;
using Dread.Config;

namespace Dread.Systems
{
    internal static class RuntimePerfSampler
    {
        private static float _nextSample;
        private static int _frames;
        private static float _accumulatedDt;

        public static void RecordFrame()
        {
            _frames++;
            _accumulatedDt += Time.deltaTime;
            if (Time.time < _nextSample)
                return;

            float avgMs = _frames > 0 ? (_accumulatedDt / _frames) * 1000f : 0f;
            float fps = avgMs > 0f ? 1000f / avgMs : 0f;

            // #region agent log
            DebugAgentLog.Write(
                "H",
                "RuntimePerfSampler.cs:RecordFrame",
                "perf_sample",
                "post-fix",
                ("avgFps", fps),
                ("avgFrameMs", avgMs),
                ("enemyCount", EnemyScanCache.Count),
                ("debugServer", DreadConfig.DebugServerEnabled.Value),
                ("debugOverlay", DreadConfig.DebugOverlayEnabled.Value),
                ("overlayVisible", DebugOverlayTogglePoll.IsOverlayVisible),
                ("audioFrequency", DreadConfig.AudioFrequency.Value));
            // #endregion

            _nextSample = Time.time + 5f;
            _frames = 0;
            _accumulatedDt = 0f;
        }
    }
}
