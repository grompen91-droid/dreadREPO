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
        private const string WorkerUrl = "https://dread-error-reporter.nox-heights.workers.dev/api/report";
        private readonly List<ErrorReport> _buffer = new List<ErrorReport>();
        private float _lastFlushTime;
        private volatile bool _shouldFlush;
        private const float FlushInterval = 300f;
        private const int MaxBatchSize = 50;
        private const int MaxStackTraceLength = 3000;
        private const int MaxMessageLength = 500;
        private const float ProximityRange = 15f;
        private const int MaxPendingLogs = 100;
        private const int MaxProcessPerFrame = 3;
        private const int HashPrefixLength = 16;
        private const int PlayerMaxHp = 100;
        private const float DedupeCooldownSeconds = 60f;
        private static readonly Queue<RawLogEntry> _pendingLogs = new Queue<RawLogEntry>(32);
        private static readonly object _logsLock = new object();
        private static readonly Dictionary<string, float> _recentHashes = new Dictionary<string, float>();
        private static bool _permanentlyDisabled;

        private class RawLogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
        }

        private void OnEnable()
        {
            if (StubBuildDetector.IsStubBuild || _permanentlyDisabled)
            {
                enabled = false;
                return;
            }

            LoggingService.LogVerbose("[ErrorReporter] Awake starting...");
            Application.logMessageReceived += OnLogMessageReceived;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _lastFlushTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            FlushNow();
        }

        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            EnqueueLog(logString, stackTrace, type);
        }

        internal static void EnqueueLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
                return;

            if (_permanentlyDisabled || StubBuildDetector.IsStubBuild
                || !DreadConfig.ErrorReportingEnabled.Value)
                return;

            if (IsIgnoredSpam(logString, stackTrace))
                return;

            var hash = ComputeHash(stackTrace, logString);
            var now = Time.realtimeSinceStartup;
            lock (_logsLock)
            {
                if (_recentHashes.TryGetValue(hash, out var last) && now - last < DedupeCooldownSeconds)
                    return;

                _recentHashes[hash] = now;
                if (_pendingLogs.Count >= MaxPendingLogs)
                    return;

                _pendingLogs.Enqueue(new RawLogEntry
                {
                    Message = logString,
                    StackTrace = stackTrace,
                    Type = type
                });
            }
        }

        private static bool IsIgnoredSpam(string message, string stackTrace)
        {
            if (message.Contains("GUIContent.get_none", StringComparison.Ordinal)
                || message.Contains("GUI.DrawTexture", StringComparison.Ordinal)
                || message.Contains("EnemyHealth.get_CurrentHealth", StringComparison.Ordinal)
                || message.Contains("PlayerController.get_Health", StringComparison.Ordinal)
                || message.Contains("PlayerController.get_stamina", StringComparison.Ordinal)
                || message.Contains("SupportsTextureFormat", StringComparison.Ordinal)
                || message.Contains("zero rva", StringComparison.OrdinalIgnoreCase)
                || message.Contains("BadImageFormatException", StringComparison.Ordinal)
                || stackTrace.Contains("ErrorReporterSystem", StringComparison.Ordinal))
                return true;
            if (message.Contains("DebugConsoleUI", StringComparison.Ordinal)
                || stackTrace.Contains("DebugConsoleUI", StringComparison.Ordinal))
                return true;
            if (message.Contains("DebugTester", StringComparison.Ordinal)
                || stackTrace.Contains("SemiFunc.DebugTester", StringComparison.Ordinal)
                || stackTrace.Contains("DebugTester", StringComparison.Ordinal))
                return true;
            return false;
        }

        private void Update()
        {
            if (_permanentlyDisabled || !enabled)
                return;

            try
            {
                ProcessPendingLogs();
            }
            catch (Exception ex)
            {
                DisablePermanently($"Error reporter disabled after failure: {ex.GetType().Name}");
            }

            if (_shouldFlush || Time.realtimeSinceStartup - _lastFlushTime >= FlushInterval)
            {
                try
                {
                    FlushNow();
                }
                catch (Exception ex)
                {
                    DisablePermanently($"Error reporter flush failed: {ex.GetType().Name}");
                }
            }
        }

        private static void DisablePermanently(string reason)
        {
            if (_permanentlyDisabled)
                return;

            _permanentlyDisabled = true;
            Application.logMessageReceived -= OnLogMessageReceived;
            lock (_logsLock)
            {
                _pendingLogs.Clear();
            }

            try
            {
                DreadConfig.ErrorReportingEnabled.Value = false;
            }
            catch { }

            LoggingService.LogError($"[ErrorReporter] {reason}");
        }

        private void ProcessPendingLogs()
        {
            RawLogEntry[] batch;
            lock (_logsLock)
            {
                if (_pendingLogs.Count == 0)
                    return;

                var count = Math.Min(MaxProcessPerFrame, _pendingLogs.Count);
                batch = new RawLogEntry[count];
                for (var i = 0; i < count; i++)
                    batch[i] = _pendingLogs.Dequeue();
            }

            var gameState = CaptureGameState();
            var systemInfo = CaptureSystemInfo();
            var display = CaptureDisplayInfo();
            var config = CaptureConfig();
            var scene = SceneManager.GetActiveScene().name ?? "unknown";

            foreach (var raw in batch)
            {
                var report = new ErrorReport
                {
                    Hash = ComputeHash(raw.StackTrace, raw.Message),
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Type = raw.Type == LogType.Exception ? "exception" : "error",
                    ExceptionType = ParseExceptionType(raw.Message),
                    Message = Truncate(raw.Message, MaxMessageLength),
                    StackTrace = Truncate(raw.StackTrace, MaxStackTraceLength),
                    Scene = scene,
                    GameState = gameState,
                    SystemInfo = systemInfo,
                    Display = display,
                    Config = config
                };

                lock (_buffer)
                {
                    _buffer.Add(report);
                    if (_buffer.Count >= MaxBatchSize)
                        _shouldFlush = true;
                }
            }
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
            LoggingService.LogVerbose("[ErrorReporter] Sending report...");
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
                LoggingService.LogWarning($"Error report failed: {request.error}");
            }
            else
            {
                LoggingService.LogInfo($"Sent {batch.Count} error report(s)");
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
            return sb.ToString().Substring(0, HashPrefixLength);
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
                SceneName = SceneManager.GetActiveScene().name ?? "unknown"
            };

            try
            {
                var enemies = FindObjectsOfType<EnemyHealth>();
                state.EnemiesTotal = enemies.Length;
                int alive = 0, nearby = 0;
                var player = FindObjectOfType<PlayerController>();
                foreach (var e in enemies)
                {
                    if (EnemyHealthCompat.IsAlive(e)) alive++;
                    if (player != null)
                    {
                        var dist = Vector3.Distance(e.transform.position, player.transform.position);
                        if (dist < ProximityRange)
                            nearby++;
                    }
                }
                state.EnemiesAlive = alive;
                state.EnemiesNearby = nearby;

                var pc = player;
                if (pc != null)
                {
                    var health = PlayerControllerCompat.GetHealth(pc);
                    if (health >= 0f)
                        state.PlayerHp = (int)(health * 100f);
                    state.PlayerMaxHp = PlayerMaxHp;
                    var stamina = PlayerControllerCompat.GetStamina(pc);
                    if (stamina >= 0f)
                        state.PlayerStamina = (int)(stamina * 100f);
                    state.PlayerPosition = pc.transform.position;
                }
            }
            catch { LoggingService.LogWarning("Failed to capture game state for error report"); }

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
