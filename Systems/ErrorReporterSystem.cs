using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
        private bool _sendInProgress;
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

        private class RawLogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
        }

        private void OnEnable()
        {
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

            if (!DreadConfig.ErrorReportingEnabled.Value)
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
            if (message.Contains("[Dread TestCrash]", StringComparison.Ordinal)
                || stackTrace.Contains("TestCrashSystem", StringComparison.Ordinal))
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
            ProcessPendingLogs();

            if (_shouldFlush || Time.realtimeSinceStartup - _lastFlushTime >= FlushInterval)
                FlushNow();
        }

        /// <summary>TestCrash: synchronous POST so report completes before Process.Kill().</summary>
        internal IEnumerator ReportTestCrashAndWait(Exception ex)
        {
            if (!DreadConfig.ErrorReportingEnabled.Value)
            {
                LoggingService.LogWarning(
                    "[ErrorReporter] ErrorReportingEnabled is false; enable it to send test crash reports.");
                yield break;
            }

            LoggingService.LogInfo("[ErrorReporter] Sending test crash report (sync POST)...");

            try
            {
                var message = $"{ex.GetType().Name}: {ex.Message}";
                var stack = ex.StackTrace ?? string.Empty;
                var scene = SceneManager.GetActiveScene().name ?? "unknown";
                var report = BuildTestCrashReport(ex, message, stack, scene);

                var payload = new ErrorPayload
                {
                    ModVersion = Plugin.VERSION,
                    GameVersion = Application.version,
                    UnityVersion = Application.unityVersion,
                    Reports = new[] { report }
                };

                if (TryPostPayloadSync(payload, out var responseBody, out var postError))
                {
                    if (HasWorkerReportFailures(responseBody, payload.Reports))
                    {
                        LoggingService.LogWarning(
                            $"Test crash report reached worker but GitHub step failed. Response: {responseBody}");
                    }
                    else
                    {
                        LoggingService.LogInfo($"Test crash report sent. Response: {responseBody}");
                    }
                }
                else
                {
                    LoggingService.LogWarning($"Test crash report POST failed: {postError}");
                }
            }
            catch (Exception e)
            {
                LoggingService.LogWarning($"[ErrorReporter] Test crash report failed: {e.Message}");
            }

            yield break;
        }

        private static bool TryPostPayloadSync(ErrorPayload payload, out string responseBody, out string error)
        {
            responseBody = string.Empty;
            error = string.Empty;
            try
            {
                var json = ErrorReportJson.SerializePayload(payload);
                if (string.IsNullOrEmpty(json) || json.IndexOf("\"Reports\":[", StringComparison.Ordinal) < 0)
                {
                    error = $"JSON serializer produced invalid payload (length={json?.Length ?? 0})";
                    return false;
                }

                var request = (HttpWebRequest)WebRequest.Create(WorkerUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 15000;
                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(bytes, 0, bytes.Length);

                using var response = (HttpWebResponse)request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null);
                responseBody = reader.ReadToEnd();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errResponse)
                {
                    using var reader = new StreamReader(errResponse.GetResponseStream() ?? Stream.Null);
                    responseBody = reader.ReadToEnd();
                    error = $"HTTP {(int)errResponse.StatusCode}: {responseBody}";
                }
                else
                {
                    error = ex.Message;
                }

                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void ProcessPendingLogs()
        {
            try
            {
                ProcessPendingLogsCore();
            }
            catch (Exception e)
            {
                LoggingService.LogWarning($"[ErrorReporter] Failed to process pending logs: {e.Message}");
            }
        }

        private void ProcessPendingLogsCore()
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
            var systemInfo = CaptureSystemInfoSafe();
            var display = CaptureDisplayInfoSafe();
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
            if (_sendInProgress)
                return;

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

        private void RequeueFailedBatch(List<ErrorReport> batch)
        {
            if (batch.Count == 0)
                return;

            lock (_buffer)
            {
                _buffer.AddRange(batch);
            }

            LoggingService.LogWarning(
                $"[ErrorReporter] Re-queued {batch.Count} report(s) after failed send.");
        }

        private void HandleWorkerResponse(string body, List<ErrorReport> batch)
        {
            var failed = CollectFailedReports(body, batch);
            if (failed.Count > 0)
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Worker reported {failed.Count} GitHub failure(s). Response: {body}");
                RequeueFailedBatch(failed);
                return;
            }

            if (HasUnmappedWorkerErrors(body))
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Worker returned errors; re-queuing full batch. Response: {body}");
                RequeueFailedBatch(batch);
                return;
            }

            LoggingService.LogInfo($"Sent {batch.Count} error report(s). Response: {body}");
        }

        private static List<ErrorReport> CollectFailedReports(string body, List<ErrorReport> batch)
        {
            var failed = new List<ErrorReport>();
            foreach (var report in batch)
            {
                if (IsReportFailedInResponse(body, report.Hash))
                    failed.Add(report);
            }

            return failed;
        }

        private static bool HasWorkerReportFailures(string body, ErrorReport[] reports)
        {
            if (string.IsNullOrEmpty(body))
                return false;

            foreach (var report in reports)
            {
                if (report != null && IsReportFailedInResponse(body, report.Hash))
                    return true;
            }

            return HasUnmappedWorkerErrors(body);
        }

        private static bool HasUnmappedWorkerErrors(string body)
        {
            return body.IndexOf("\"status\":\"error\"", StringComparison.Ordinal) >= 0
                || body.IndexOf("\"status\": \"error\"", StringComparison.Ordinal) >= 0;
        }

        private static bool IsReportFailedInResponse(string body, string hash)
        {
            if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(hash))
                return false;

            var hashNeedle = "\"hash\":\"" + hash + "\"";
            var idx = 0;
            while ((idx = body.IndexOf(hashNeedle, idx, StringComparison.Ordinal)) >= 0)
            {
                var windowEnd = Math.Min(body.Length, idx + 256);
                var slice = body.Substring(idx, windowEnd - idx);
                if (slice.IndexOf("\"status\":\"error\"", StringComparison.Ordinal) >= 0
                    || slice.IndexOf("\"status\": \"error\"", StringComparison.Ordinal) >= 0)
                    return true;

                idx += hashNeedle.Length;
            }

            return false;
        }

        private IEnumerator SendBatch(List<ErrorReport> batch)
        {
            _sendInProgress = true;
            try
            {
                LoggingService.LogVerbose("[ErrorReporter] Sending report...");
                var payload = new ErrorPayload
                {
                    ModVersion = Plugin.VERSION,
                    GameVersion = Application.version,
                    UnityVersion = Application.unityVersion,
                    Reports = batch.ToArray()
                };

                var json = ErrorReportJson.SerializePayload(payload);
                if (json.IndexOf("\"Reports\":[", StringComparison.Ordinal) < 0)
                {
                    LoggingService.LogWarning(
                        $"[ErrorReporter] JSON missing Reports (len={json.Length}); re-queuing batch.");
                    RequeueFailedBatch(batch);
                    yield break;
                }

                var bytes = Encoding.UTF8.GetBytes(json);

                using var request = new UnityWebRequest(WorkerUrl, "POST");
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LoggingService.LogWarning($"Error report HTTP failed: {request.error}");
                    RequeueFailedBatch(batch);
                }
                else
                {
                    var body = request.downloadHandler?.text ?? string.Empty;
                    HandleWorkerResponse(body, batch);
                }
            }
            finally
            {
                _sendInProgress = false;
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

        private static ErrorReport BuildTestCrashReport(
            Exception ex, string message, string stack, string scene)
        {
            return new ErrorReport
            {
                Hash = ComputeHash(stack, message + "|testcrash|" + DateTime.UtcNow.Ticks),
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

        private static SystemInfoData CaptureSystemInfoSafe()
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

        private static DisplayInfoData CaptureDisplayInfoSafe()
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

        private static ConfigData CaptureConfigSafe()
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

        private static GameStateData CreateMinimalGameState(string scene)
        {
            return new GameStateData
            {
                SceneName = scene,
                PlayTimeSeconds = (int)Time.realtimeSinceStartup
            };
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
                PlayerController? player = null;
                try
                {
                    player = FindObjectOfType<PlayerController>();
                }
                catch
                {
                    // ignore player lookup failures
                }

                foreach (var e in enemies)
                {
                    try
                    {
                        if (e.CurrentHealth > 0)
                            alive++;
                        if (player != null)
                        {
                            var dist = Vector3.Distance(e.transform.position, player.transform.position);
                            if (dist < ProximityRange)
                                nearby++;
                        }
                    }
                    catch
                    {
                        // skip enemies with incompatible stubs / mods
                    }
                }

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

        private static ConfigData CaptureConfig()
        {
            return CaptureConfigSafe();
        }

        private static string Truncate(string value, int maxLength)
        {
            return value != null && value.Length > maxLength
                ? value.Substring(0, maxLength)
                : value;
        }
    }
}
