using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Dread.Systems
{
    internal static class ErrorReportLogQueue
    {
        internal const int MaxPendingLogs = 100;
        internal const int MaxProcessPerFrame = 3;
        private const float DedupeCooldownSeconds = 60f;
        private const int HashPrefixLength = 16;

        private static readonly Queue<RawLogEntry> PendingLogs = new Queue<RawLogEntry>(32);
        private static readonly object LogsLock = new object();
        private static readonly Dictionary<string, float> RecentHashes = new Dictionary<string, float>();

        internal sealed class RawLogEntry
        {
            public string Message = string.Empty;
            public string StackTrace = string.Empty;
            public LogType Type;
        }

        internal static void EnqueueLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
                return;

            if (!ErrorReportingConsent.IsReportingAllowed())
                return;

            if (IsIgnoredSpam(logString, stackTrace))
                return;

            var hash = ComputeHash(stackTrace, logString);
            var now = Time.realtimeSinceStartup;
            lock (LogsLock)
            {
                if (RecentHashes.TryGetValue(hash, out var last) && now - last < DedupeCooldownSeconds)
                    return;

                RecentHashes[hash] = now;
                if (PendingLogs.Count >= MaxPendingLogs)
                    return;

                PendingLogs.Enqueue(new RawLogEntry
                {
                    Message = logString,
                    StackTrace = stackTrace,
                    Type = type
                });
            }
        }

        internal static bool TryDequeueBatch(out RawLogEntry[] batch)
        {
            lock (LogsLock)
            {
                if (PendingLogs.Count == 0)
                {
                    batch = Array.Empty<RawLogEntry>();
                    return false;
                }

                var count = Math.Min(MaxProcessPerFrame, PendingLogs.Count);
                batch = new RawLogEntry[count];
                for (var i = 0; i < count; i++)
                    batch[i] = PendingLogs.Dequeue();
                return true;
            }
        }

        internal static bool IsIgnoredSpam(string message, string stackTrace)
        {
            if (message.IndexOf("[Dread TestCrash]", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("TestCrashSystem", StringComparison.Ordinal) >= 0)
                return true;
            if (message.IndexOf("DebugConsoleUI", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("DebugConsoleUI", StringComparison.Ordinal) >= 0)
                return true;
            if (message.IndexOf("DebugTester", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("SemiFunc.DebugTester", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("DebugTester", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }

        internal static string ComputeHash(string stackTrace, string message)
        {
            using var sha = SHA256.Create();
            var input = $"{stackTrace}\n{message}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString().Substring(0, HashPrefixLength);
        }
    }
}
