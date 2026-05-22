using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Dread.Config;

namespace Dread.Systems
{
    public class ErrorReporterSystem : MonoBehaviour
    {
        private const string WorkerUrl = "https://dread-error-reporter.your-worker.workers.dev/api/report";
        private readonly List<ErrorReport> _buffer = new List<ErrorReport>();
        private float _lastFlushTime;
        private volatile bool _shouldFlush;
        private const float FlushInterval = 300f;
        private const int MaxBatchSize = 50;
        private const int MaxStackTraceLength = 3000;
        private const int MaxMessageLength = 500;

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLog;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _lastFlushTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            FlushNow();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
                return;

            if (!DreadConfig.ErrorReportingEnabled.Value)
                return;

            var report = new ErrorReport
            {
                Hash = ComputeHash(stackTrace, logString),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Type = type == LogType.Exception ? "exception" : "error",
                ExceptionType = ParseExceptionType(logString),
                Message = Truncate(logString, MaxMessageLength),
                StackTrace = Truncate(stackTrace, MaxStackTraceLength),
                Scene = SceneManager.GetActiveScene()?.name ?? "unknown",
                GameState = CaptureGameState(),
                SystemInfo = CaptureSystemInfo(),
                Display = CaptureDisplayInfo(),
                Config = CaptureConfig()
            };

            lock (_buffer)
            {
                _buffer.Add(report);
                if (_buffer.Count >= MaxBatchSize)
                    _shouldFlush = true;
            }
        }

        private void Update()
        {
            if (_shouldFlush || Time.realtimeSinceStartup - _lastFlushTime >= FlushInterval)
                FlushNow();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FlushNow();
        }

        private void FlushNow()
        {
            List<ErrorReport> batch;
            lock (_buffer)
            {
                if (_buffer.Count == 0)
                    return;
                batch = new List<ErrorReport>(_buffer);
                _buffer.Clear();
            }

            _shouldFlush = false;
            _lastFlushTime = Time.realtimeSinceStartup;
            StartCoroutine(SendBatch(batch));
        }

        private IEnumerator SendBatch(List<ErrorReport> batch)
        {
            var payload = new ErrorPayload
            {
                ModVersion = Plugin.VERSION,
                GameVersion = Application.version,
                UnityVersion = Application.unityVersion,
                Reports = batch.ToArray()
            };

            var json = JsonUtility.ToJson(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(WorkerUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Plugin.Logger.LogWarning($"Error report failed: {request.error}");
            }
            else
            {
                Plugin.Logger.LogInfo($"Sent {batch.Count} error report(s)");
            }
        }

        private static string ComputeHash(string stackTrace, string message)
        {
            using var sha = SHA256.Create();
            var input = $"{stackTrace}\n{message}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString().Substring(0, 16);
        }

        private static string ParseExceptionType(string logString)
        {
            var colonIdx = logString.IndexOf(':');
            return colonIdx > 0 ? logString.Substring(0, colonIdx) : "Unknown";
        }

        private static GameStateData CaptureGameState()
        {
            var state = new GameStateData
            {
                SceneName = SceneManager.GetActiveScene()?.name ?? "unknown"
            };

            try
            {
                var enemies = FindObjectsOfType<EnemyHealth>();
                state.EnemiesTotal = enemies.Length;
                int alive = 0, nearby = 0;
                foreach (var e in enemies)
                {
                    if (e.CurrentHealth > 0) alive++;
                    var player = FindObjectOfType<PlayerController>();
                    if (player != null && Vector3.Distance(e.transform.position, player.transform.position) < 15f)
                        nearby++;
                }
                state.EnemiesAlive = alive;
                state.EnemiesNearby = nearby;

                var pc = FindObjectOfType<PlayerController>();
                if (pc != null)
                {
                    state.PlayerHp = (int)(pc.Health * 100f);
                    state.PlayerMaxHp = 100;
                    state.PlayerStamina = (int)(pc.stamina * 100f);
                    state.PlayerPosition = pc.transform.position;
                }
            }
            catch { Plugin.Logger.LogWarning("Failed to capture game state for error report"); }

            state.PlayTimeSeconds = (int)Time.realtimeSinceStartup;
            return state;
        }

        private static SystemInfoData CaptureSystemInfo()
        {
            return new SystemInfoData
            {
                Os = SystemInfo.operatingSystem,
                OsFamily = SystemInfo.operatingSystemFamily.ToString(),
                Cpu = SystemInfo.processorType,
                CpuCores = SystemInfo.processorCount,
                CpuFrequencyMHz = SystemInfo.processorFrequency,
                MemoryMB = SystemInfo.systemMemorySize,
                Gpu = SystemInfo.graphicsDeviceName,
                GpuVendor = SystemInfo.graphicsDeviceVendor,
                GpuDriverVersion = SystemInfo.graphicsDeviceVersion,
                GpuShaderLevel = SystemInfo.graphicsShaderLevel,
                VramMB = SystemInfo.graphicsMemorySize,
                DeviceType = SystemInfo.deviceType.ToString(),
                DeviceModel = SystemInfo.deviceModel
            };
        }

        private static DisplayInfoData CaptureDisplayInfo()
        {
            var res = Screen.currentResolution;
            return new DisplayInfoData
            {
                Width = res.width,
                Height = res.height,
                RefreshRate = res.refreshRate,
                Dpi = Screen.dpi,
                FullScreenMode = Screen.fullScreenMode.ToString()
            };
        }

        private static ConfigData CaptureConfig()
        {
            return new ConfigData
            {
                AudioEnabled = DreadConfig.AudioEnabled.Value,
                AudioFrequency = DreadConfig.AudioFrequency.Value,
                AudioVolume = DreadConfig.AudioVolume.Value,
                AggressionEnabled = DreadConfig.MonsterAggressionEnabled.Value,
                AggressionAudioEnabled = DreadConfig.MonsterAudioEnabled.Value,
                FakeFootsteps = DreadConfig.FakeFootstepsEnabled.Value,
                Adrenaline = DreadConfig.AdrenalineEnabled.Value,
                LowStaminaSound = DreadConfig.LowStaminaSoundEnabled.Value,
                PanicSprint = DreadConfig.PanicSprintEnabled.Value,
                CrouchSpeedBoost = DreadConfig.CrouchSpeedBoostEnabled.Value,
                ErrorReportingEnabled = DreadConfig.ErrorReportingEnabled.Value
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            return value != null && value.Length > maxLength
                ? value.Substring(0, maxLength)
                : value;
        }

        [Serializable]
        private class ErrorPayload
        {
            public string ModVersion;
            public string GameVersion;
            public string UnityVersion;
            public ErrorReport[] Reports;
        }

        [Serializable]
        private class ErrorReport
        {
            public string Hash;
            public string Timestamp;
            public string Type;
            public string ExceptionType;
            public string Message;
            public string StackTrace;
            public string Scene;
            public GameStateData GameState;
            public SystemInfoData SystemInfo;
            public DisplayInfoData Display;
            public ConfigData Config;
        }

        [Serializable]
        private class GameStateData
        {
            public string SceneName;
            public int EnemiesAlive;
            public int EnemiesTotal;
            public int EnemiesNearby;
            public int PlayerHp;
            public int PlayerMaxHp;
            public int PlayerStamina;
            public Vector3 PlayerPosition;
            public int PlayTimeSeconds;
        }

        [Serializable]
        private class SystemInfoData
        {
            public string Os;
            public string OsFamily;
            public string Cpu;
            public int CpuCores;
            public int CpuFrequencyMHz;
            public int MemoryMB;
            public string Gpu;
            public string GpuVendor;
            public string GpuDriverVersion;
            public int GpuShaderLevel;
            public int VramMB;
            public string DeviceType;
            public string DeviceModel;
        }

        [Serializable]
        private class DisplayInfoData
        {
            public int Width;
            public int Height;
            public int RefreshRate;
            public float Dpi;
            public string FullScreenMode;
        }

        [Serializable]
        private class ConfigData
        {
            public bool AudioEnabled;
            public float AudioFrequency;
            public float AudioVolume;
            public bool AggressionEnabled;
            public bool AggressionAudioEnabled;
            public bool FakeFootsteps;
            public bool Adrenaline;
            public bool LowStaminaSound;
            public bool PanicSprint;
            public bool CrouchSpeedBoost;
            public bool ErrorReportingEnabled;
        }
    }
}
