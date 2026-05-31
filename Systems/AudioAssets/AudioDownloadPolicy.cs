using System;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems.AudioAssets
{
    internal sealed class AudioDownloadPolicy
    {
        private const double EmaAlpha = 0.35;
        private const double SlowBytesPerSec = 256 * 1024;
        private const double MidBytesPerSec = 1.5 * 1024 * 1024;
        private const double FastBytesPerSec = 4 * 1024 * 1024;

        private bool _probeComplete;
        private bool _sessionForceOne;
        private double _emaBytesPerSec;
        private int _networkTier = 1;
        private int _effectiveConcurrent = 1;
        private float _frameSum;
        private int _frameSamples;
        private int _badFrameStreak;

        public int EffectiveConcurrent => _effectiveConcurrent;
        public double MeasuredBytesPerSec => _emaBytesPerSec;
        public bool ProbeComplete => _probeComplete;

        public void ResetSessionOnError()
        {
            _sessionForceOne = true;
            _networkTier = 1;
            ApplyEffective();
        }

        public void RecordDownload(long bytes, double elapsedSeconds)
        {
            if (bytes <= 0 || elapsedSeconds <= 0)
                return;

            var speed = bytes / elapsedSeconds;
            _emaBytesPerSec = _emaBytesPerSec <= 0 ? speed : EmaAlpha * speed + (1 - EmaAlpha) * _emaBytesPerSec;
            _probeComplete = true;
            _sessionForceOne = false;
            RecomputeTiers(step: true);
        }

        public void OnFrameSample(float unscaledDeltaTime, bool inRun)
        {
            if (!_probeComplete)
                return;

            _frameSum += unscaledDeltaTime;
            _frameSamples++;
            if (_frameSamples < 30)
                return;

            var mean = _frameSum / _frameSamples;
            _frameSum = 0;
            _frameSamples = 0;

            if (inRun && mean > 0.03f)
            {
                _badFrameStreak++;
                if (_badFrameStreak >= 3)
                {
                    _effectiveConcurrent = 1;
                    LoggingService.LogVerbose("[AudioAssets] Frame budget: forcing concurrent=1");
                }
            }
            else
            {
                _badFrameStreak = 0;
                if (_effectiveConcurrent == 1 && !_sessionForceOne)
                    RecomputeTiers(step: false);
            }
        }

        public int GetAllowedConcurrent(bool offline)
        {
            if (offline)
                return 1;

            var user = DreadConfig.AudioAssetsMaxConcurrentDownloads?.Value ?? 0;
            if (user > 0)
                return Math.Min(3, user);

            if (!_probeComplete || _sessionForceOne)
                return 1;

            return _effectiveConcurrent;
        }

        private void RecomputeTiers(bool step)
        {
            var targetNet = NetworkTierFromSpeed(_emaBytesPerSec);
            if (step)
            {
                if (targetNet > _networkTier)
                    _networkTier = Math.Min(_networkTier + 1, targetNet);
                else if (targetNet < _networkTier)
                    _networkTier = Math.Max(_networkTier - 1, targetNet);
            }
            else
            {
                _networkTier = targetNet;
            }

            ApplyEffective();
        }

        private void ApplyEffective()
        {
            var cpuCap = ComputeCpuCap();
            var user = DreadConfig.AudioAssetsMaxConcurrentDownloads?.Value ?? 0;
            var cap = user > 0 ? user : Math.Min(_networkTier, cpuCap);
            cap = Math.Max(1, Math.Min(3, cap));
            if (_sessionForceOne)
                cap = 1;

            if (cap != _effectiveConcurrent)
            {
                LoggingService.LogVerbose(
                    $"[AudioAssets] Policy: concurrent={cap} (down={FormatSpeed(_emaBytesPerSec)}, cpuCap={cpuCap}, netTier={_networkTier})");
            }

            _effectiveConcurrent = cap;
            DreadRuntimeState.AudioAssetsConcurrentDownloads = cap;
            DreadRuntimeState.AudioAssetsMeasuredBytesPerSec = (float)_emaBytesPerSec;
        }

        private static int NetworkTierFromSpeed(double bytesPerSec)
        {
            if (bytesPerSec < SlowBytesPerSec)
                return 1;
            if (bytesPerSec < MidBytesPerSec)
                return 1;
            if (bytesPerSec < FastBytesPerSec)
                return 2;
            return 3;
        }

        private static int ComputeCpuCap()
        {
            var cores = SystemInfo.processorCount;
            var mhz = SystemInfo.processorFrequency;
            if (cores <= 0 || mhz <= 0)
                return 1;
            if (cores < 4 || mhz < 2000)
                return 1;
            if (cores < 8 || mhz < 3000)
                return 2;
            return 3;
        }

        private static string FormatSpeed(double bps)
        {
            if (bps <= 0)
                return "n/a";
            if (bps >= 1024 * 1024)
                return $"{bps / (1024 * 1024):F1}MB/s";
            return $"{bps / 1024:F0}KB/s";
        }

        public static bool IsNetworkOffline()
        {
            return Application.internetReachability == NetworkReachability.NotReachable;
        }
    }
}
