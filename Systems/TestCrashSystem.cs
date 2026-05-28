using System;
using System.Collections;
using System.Diagnostics;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public class TestCrashSystem : MonoBehaviour
    {
        private static TestCrashSystem? _instance;
        private bool _pendingCrash;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            DreadConfig.TestCrashButton.SettingChanged += OnTestCrashRequested;
        }

        private void OnDestroy()
        {
            DreadConfig.TestCrashButton.SettingChanged -= OnTestCrashRequested;
            if (_instance == this)
                _instance = null;
        }

        private void OnTestCrashRequested(object? sender, EventArgs e)
        {
            if (!DreadConfig.TestCrashButton.Value)
                return;

            DreadConfig.TestCrashButton.Value = false;
            QueueCrash();
        }

        private void Update()
        {
            if (!_pendingCrash)
                return;

            _pendingCrash = false;
            StartCoroutine(CrashSequence());
        }

        /// <summary>Debug server / MCP entry point for deliberate crash testing.</summary>
        public static void TriggerForDebug()
        {
            if (_instance != null)
            {
                _instance.QueueCrash();
                return;
            }

            throw new InvalidOperationException(
                "[Dread TestCrash] TestCrashSystem not initialized (load into a level first).");
        }

        private void QueueCrash()
        {
            _pendingCrash = true;
        }

        private IEnumerator CrashSequence()
        {
            var send = SendTestCrashReport();
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = send.MoveNext();
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning($"[Dread TestCrash] Report coroutine failed: {ex.Message}");
                    break;
                }

                if (!hasNext)
                    break;

                yield return send.Current;
            }

            ExitProcess();
        }

        private static IEnumerator SendTestCrashReport()
        {
            var ex = new InvalidOperationException(
                "[Dread TestCrash] Game crashed deliberately via the "
                    + "'Crash Game' config button to verify error reporting.");

            UnityEngine.Debug.LogException(ex);

            var reporter = FindObjectOfType<ErrorReporterSystem>();
            if (reporter != null)
                yield return reporter.ReportTestCrashAndWait(ex);
            else
                LoggingService.LogWarning("[Dread TestCrash] ErrorReporterSystem not found; no report sent.");
        }

        private static void ExitProcess()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError("[Dread TestCrash] Editor: process kill skipped.");
#else
            LoggingService.LogInfo("[Dread TestCrash] Exiting game process now.");
            Process.GetCurrentProcess().Kill();
#endif
        }
    }
}
