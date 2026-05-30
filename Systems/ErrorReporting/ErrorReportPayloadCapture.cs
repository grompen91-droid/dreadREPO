using System;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    internal static class ErrorReportPayloadCapture
    {
        internal const int MaxStackTraceLength = 3000;
        internal const int MaxMessageLength = 500;
        private const float ProximityRange = 15f;
        private const int PlayerMaxHp = 100;

        internal static string ParseExceptionType(string logString)
        {
            var colonIdx = logString.IndexOf(':');
            return colonIdx > 0 ? logString.Substring(0, colonIdx) : "Unknown";
        }

        internal static ErrorReport BuildTestCrashReport(
            Exception ex, string message, string stack, string scene)
        {
            return new ErrorReport
            {
                Hash = ErrorReportLogQueue.ComputeHash(stack, message + "|testcrash|" + DateTime.UtcNow.Ticks),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Type = "exception",
                ExceptionType = ex.GetType().Name,
                Message = Truncate(message, MaxMessageLength),
                StackTrace = Truncate(stack, MaxStackTraceLength),
                Scene = scene,
                GameState = CreateMinimalGameState(scene),
                SystemInfo = CaptureSystemInfoSafe(),
                Display = CaptureDisplayInfoSafe(),
                Config = CaptureConfigSafe()
            };
        }

        internal static SystemInfoData CaptureSystemInfoSafe()
        {
            var info = new SystemInfoData();
            TrySet(() => info.Os = SystemInfo.operatingSystem);
            TrySet(() => info.OsFamily = SystemInfo.operatingSystemFamily.ToString());
            TrySet(() => info.Cpu = SystemInfo.processorType);
            TrySet(() => info.CpuCores = SystemInfo.processorCount);
            TrySet(() => info.CpuFrequencyMHz = SystemInfo.processorFrequency);
            TrySet(() => info.MemoryMB = SystemInfo.systemMemorySize);
            TrySet(() => info.Gpu = SystemInfo.graphicsDeviceName);
            TrySet(() => info.GpuVendor = SystemInfo.graphicsDeviceVendor);
            TrySet(() => info.GpuDriverVersion = SystemInfo.graphicsDeviceVersion);
            TrySet(() => info.GpuShaderLevel = SystemInfo.graphicsShaderLevel);
            TrySet(() => info.VramMB = SystemInfo.graphicsMemorySize);
            TrySet(() => info.DeviceType = SystemInfo.deviceType.ToString());
            TrySet(() => info.DeviceModel = SystemInfo.deviceModel);
            return info;
        }

        internal static DisplayInfoData CaptureDisplayInfoSafe()
        {
            var display = new DisplayInfoData();
            TrySet(() =>
            {
                var res = Screen.currentResolution;
                display.Width = res.width;
                display.Height = res.height;
                display.RefreshRate = res.refreshRate;
            });
            TrySet(() => display.Dpi = Screen.dpi);
            TrySet(() => display.FullScreenMode = Screen.fullScreenMode.ToString());
            return display;
        }

        internal static ConfigData CaptureConfigSafe()
        {
            var config = new ConfigData();
            TrySet(() => config.AudioEnabled = DreadConfig.AudioEnabled.Value);
            TrySet(() => config.AudioFrequency = DreadConfig.AudioFrequency.Value);
            TrySet(() => config.AudioVolume = DreadConfig.AudioVolume.Value);
            TrySet(() => config.AggressionEnabled = DreadConfig.MonsterAggressionEnabled.Value);
            TrySet(() => config.AggressionAudioEnabled = DreadConfig.MonsterAudioEnabled.Value);
            TrySet(() => config.FakeFootsteps = DreadConfig.FakeFootstepsEnabled.Value);
            TrySet(() => config.Adrenaline = DreadConfig.AdrenalineEnabled.Value);
            TrySet(() => config.LowStaminaSound = DreadConfig.LowStaminaSoundEnabled.Value);
            TrySet(() => config.PanicSprint = DreadConfig.PanicSprintEnabled.Value);
            TrySet(() => config.CrouchSpeedBoost = DreadConfig.CrouchSpeedBoostEnabled.Value);
            TrySet(() => config.ErrorReportingEnabled = DreadConfig.ErrorReportingEnabled.Value);
            return config;
        }

        internal static ConfigData CaptureConfig() => CaptureConfigSafe();

        internal static GameStateData CreateMinimalGameState(string scene)
        {
            return new GameStateData
            {
                SceneName = scene,
                PlayTimeSeconds = (int)Time.realtimeSinceStartup
            };
        }

        internal static GameStateData CaptureGameState()
        {
            var state = new GameStateData
            {
                SceneName = SceneManager.GetActiveScene().name ?? "unknown"
            };

            try
            {
                var enemies = EnemyScanCache.GetEnemies();
                state.EnemiesTotal = enemies.Length;
                PlayerController? player = null;
                try
                {
                    player = UnityEngine.Object.FindObjectOfType<PlayerController>();
                }
                catch
                {
                    // ignore player lookup failures
                }

                EnemyHealthCompat.CountAliveAndNearby(
                    enemies, player, ProximityRange, out var alive, out var nearby);
                state.EnemiesAlive = alive;
                state.EnemiesNearby = nearby;

                if (player != null)
                {
                    try
                    {
                        state.PlayerHp = (int)(player.Health * 100f);
                        state.PlayerMaxHp = PlayerMaxHp;
                        state.PlayerStamina = (int)(player.stamina * 100f);
                        state.PlayerPosition = player.transform.position;
                    }
                    catch
                    {
                        // ignore player field failures
                    }
                }
            }
            catch
            {
                LoggingService.LogWarning("Failed to capture game state for error report");
            }

            state.PlayTimeSeconds = (int)Time.realtimeSinceStartup;
            return state;
        }

        internal static string Truncate(string value, int maxLength)
        {
            return value != null && value.Length > maxLength
                ? value.Substring(0, maxLength)
                : value;
        }

        private static void TrySet(Action setter)
        {
            try
            {
                setter();
            }
            catch
            {
                // Unity / stub API mismatch on some platforms
            }
        }
    }
}
