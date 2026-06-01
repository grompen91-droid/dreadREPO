using System;
using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class ErrorReporterSystem : MonoBehaviour
    {
        private readonly List<ErrorReport> _buffer = new List<ErrorReport>();
        private float _lastFlushTime;
        private volatile bool _shouldFlush;
        private volatile bool _urgentFlush;
        private bool _sendInProgress;
        private const float FlushInterval = 300f;

        private void OnEnable()
        {
            LoggingService.LogVerbose("[ErrorReporter] Awake starting...");
            Application.logMessageReceived += OnLogMessageReceived;
            Application.quitting += OnApplicationQuitting;
            SceneManager.sceneLoaded += OnSceneLoaded;
            DreadConfig.ErrorReportingEnabled.SettingChanged += OnErrorReportingSettingChanged;
            _lastFlushTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.quitting -= OnApplicationQuitting;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DreadConfig.ErrorReportingEnabled.SettingChanged -= OnErrorReportingSettingChanged;
            FlushNowSync();
        }

        private void OnApplicationQuitting()
        {
            FlushNowSync();
        }

        private static void OnErrorReportingSettingChanged(object sender, EventArgs e)
        {
            if (!ErrorReportingConsent.IsReportingAllowed())
                return;

            LoggingService.LogInfo(
                "[Dread] Error reporting enabled. "
                    + ErrorReportingPrivacyCopy.ShortSummary
                    + " "
                    + ErrorReportingPrivacyCopy.DisableInstructions);
        }

        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (ShouldIgnoreUnityLog(logString, stackTrace))
                return;

            EnqueueLog(logString, stackTrace, type);
        }

        private static bool ShouldIgnoreUnityLog(string logString, string stackTrace)
        {
            if (!logString.Contains("BadImageFormatException"))
                return false;

            return logString.IndexOf("zero rva", StringComparison.OrdinalIgnoreCase) >= 0
                || stackTrace.IndexOf("UnityEngine.Networking", StringComparison.Ordinal) >= 0;
        }

        internal static void EnqueueLog(string logString, string stackTrace, LogType type)
        {
            ErrorReportLogQueue.EnqueueLog(logString, stackTrace, type);
        }

        private void Update()
        {
            ProcessPendingLogs();

            if (_shouldFlush || Time.realtimeSinceStartup - _lastFlushTime >= FlushInterval)
            {
                var sync = _urgentFlush;
                _urgentFlush = false;
                FlushNow(sync);
            }
        }

        /// <summary>TestCrash: synchronous POST so report completes before Process.Kill().</summary>
        internal IEnumerator ReportTestCrashAndWait(Exception ex)
        {
            if (!ErrorReportingConsent.IsReportingAllowed())
            {
                LoggingService.LogWarning(
                    "[ErrorReporter] Error reporting is not allowed yet (disabled or first-run prompt pending).");
                yield break;
            }

            LoggingService.LogInfo("[ErrorReporter] Sending test crash report (sync POST)...");

            try
            {
                var message = $"{ex.GetType().Name}: {ex.Message}";
                var stack = ex.StackTrace ?? string.Empty;
                var scene = SceneManager.GetActiveScene().name ?? "unknown";
                var report = ErrorReportPayloadCapture.BuildTestCrashReport(ex, message, stack, scene);

                var payload = new ErrorPayload
                {
                    ModVersion = Plugin.VERSION,
                    GameVersion = Application.version,
                    UnityVersion = Application.unityVersion,
                    Reports = new[] { report }
                };

                if (ErrorReportUploader.TryPostPayloadSync(payload, out var responseBody, out var postError))
                {
                    if (ErrorReportUploader.HasWorkerReportFailures(responseBody, payload.Reports))
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
            if (!ErrorReportingConsent.IsReportingAllowed())
                return;

            if (!ErrorReportLogQueue.TryDequeueBatch(out var batch))
                return;

            GameStateData gameState;
            SystemInfoData systemInfo;
            DisplayInfoData display;
            ConfigData config;
            var scene = SceneManager.GetActiveScene().name ?? "unknown";

            try
            {
                gameState = ErrorReportPayloadCapture.CaptureGameState();
            }
            catch (Exception e)
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Game state capture failed: {e.Message}");
                gameState = ErrorReportPayloadCapture.CreateMinimalGameState(scene);
            }

            try
            {
                systemInfo = ErrorReportPayloadCapture.CaptureSystemInfoSafe();
            }
            catch
            {
                systemInfo = new SystemInfoData();
            }

            try
            {
                display = ErrorReportPayloadCapture.CaptureDisplayInfoSafe();
            }
            catch
            {
                display = new DisplayInfoData();
            }

            try
            {
                config = ErrorReportPayloadCapture.CaptureConfig();
            }
            catch
            {
                config = new ConfigData();
            }

            var scheduleUrgentFlush = false;

            foreach (var raw in batch)
            {
                if (raw.Type == LogType.Exception || raw.Type == LogType.Error)
                    scheduleUrgentFlush = true;

                var report = new ErrorReport
                {
                    Hash = ErrorReportLogQueue.ComputeHash(raw.StackTrace, raw.Message),
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Type = raw.Type == LogType.Exception ? "exception" : "error",
                    ExceptionType = ErrorReportPayloadCapture.ParseExceptionType(raw.Message),
                    Message = ErrorReportPayloadCapture.Truncate(raw.Message, ErrorReportPayloadCapture.MaxMessageLength),
                    StackTrace = ErrorReportPayloadCapture.Truncate(raw.StackTrace, ErrorReportPayloadCapture.MaxStackTraceLength),
                    Scene = scene,
                    GameState = gameState,
                    SystemInfo = systemInfo,
                    Display = display,
                    Config = config
                };

                lock (_buffer)
                {
                    _buffer.Add(report);
                    if (_buffer.Count >= ErrorReportUploader.MaxBatchSize)
                        _shouldFlush = true;
                }
            }

            if (scheduleUrgentFlush)
            {
                _shouldFlush = true;
                _urgentFlush = true;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FlushNow();
        }

        private void FlushNow(bool sync = false)
        {
            if (_sendInProgress && !sync)
                return;

            if (!ErrorReportingConsent.IsReportingAllowed())
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

            if (sync)
                SendBatchSync(batch);
            else
                StartCoroutine(SendBatch(batch));
        }

        private void FlushNowSync()
        {
            if (!ErrorReportingConsent.IsReportingAllowed())
                return;

            try
            {
                while (ErrorReportLogQueue.HasPending())
                    ProcessPendingLogsCore();
            }
            catch (Exception e)
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Failed to drain pending logs on shutdown: {e.Message}");
            }

            FlushNow(sync: true);
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
            var failed = ErrorReportUploader.CollectFailedReports(body, batch);
            if (failed.Count > 0)
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Worker reported {failed.Count} GitHub failure(s). Response: {body}");
                RequeueFailedBatch(failed);
                return;
            }

            if (ErrorReportUploader.HasUnmappedWorkerErrors(body))
            {
                LoggingService.LogWarning(
                    $"[ErrorReporter] Worker returned errors; re-queuing full batch. Response: {body}");
                RequeueFailedBatch(batch);
                return;
            }

            LoggingService.LogInfo($"Sent {batch.Count} error report(s). Response: {body}");
        }

        private void SendBatchSync(List<ErrorReport> batch)
        {
            if (batch.Count == 0)
                return;

            _sendInProgress = true;
            try
            {
                SendBatchCore(batch);
            }
            finally
            {
                _sendInProgress = false;
            }
        }

        private IEnumerator SendBatch(List<ErrorReport> batch)
        {
            _sendInProgress = true;
            try
            {
                LoggingService.LogVerbose("[ErrorReporter] Sending report...");
                ErrorReportUploader.EncodePayload(BuildPayload(batch), out var json);
                var validationError = ErrorReportUploader.ValidateBatchJson(json);
                if (validationError != null)
                {
                    LoggingService.LogWarning($"[ErrorReporter] {validationError}");
                    RequeueFailedBatch(batch);
                    yield break;
                }

                yield return null;
                SendBatchCore(batch);
            }
            finally
            {
                _sendInProgress = false;
            }
        }

        private static ErrorPayload BuildPayload(List<ErrorReport> batch)
        {
            return new ErrorPayload
            {
                ModVersion = Plugin.VERSION,
                GameVersion = Application.version,
                UnityVersion = Application.unityVersion,
                Reports = batch.ToArray()
            };
        }

        private void SendBatchCore(List<ErrorReport> batch)
        {
            var payload = BuildPayload(batch);
            ErrorReportUploader.EncodePayload(payload, out var json);
            var validationError = ErrorReportUploader.ValidateBatchJson(json);
            if (validationError != null)
            {
                LoggingService.LogWarning($"[ErrorReporter] {validationError}");
                RequeueFailedBatch(batch);
                return;
            }

            if (!ErrorReportUploader.TryPostPayloadSync(payload, out var body, out var postError))
            {
                LoggingService.LogWarning($"Error report HTTP failed: {postError}");
                RequeueFailedBatch(batch);
            }
            else
            {
                HandleWorkerResponse(body, batch);
            }
        }
    }
}
