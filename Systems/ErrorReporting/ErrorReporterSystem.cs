using System;
using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class ErrorReporterSystem : MonoBehaviour
    {
        private readonly List<ErrorReport> _buffer = new List<ErrorReport>();
        private float _lastFlushTime;
        private volatile bool _shouldFlush;
        private bool _sendInProgress;
        private const float FlushInterval = 300f;

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
            ErrorReportLogQueue.EnqueueLog(logString, stackTrace, type);
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
            if (!ErrorReportLogQueue.TryDequeueBatch(out var batch))
                return;

            var gameState = ErrorReportPayloadCapture.CaptureGameState();
            var systemInfo = ErrorReportPayloadCapture.CaptureSystemInfoSafe();
            var display = ErrorReportPayloadCapture.CaptureDisplayInfoSafe();
            var config = ErrorReportPayloadCapture.CaptureConfig();
            var scene = SceneManager.GetActiveScene().name ?? "unknown";

            foreach (var raw in batch)
            {
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

                var bytes = ErrorReportUploader.EncodePayload(payload, out var json);
                var validationError = ErrorReportUploader.ValidateBatchJson(json);
                if (validationError != null)
                {
                    LoggingService.LogWarning($"[ErrorReporter] {validationError}");
                    RequeueFailedBatch(batch);
                    yield break;
                }

                using var request = new UnityWebRequest(ErrorReportUploader.WorkerUrl, "POST");
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
    }
}
