using System;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Ring buffer of Unity console output for error report context (ADR-0010 adjunct).
    /// Fed from <see cref="Application.logMessageReceived"/> on the main thread.
    /// </summary>
    internal static class ErrorReportConsoleLogBuffer
    {
        internal const int MaxInMemoryChars = 96_000;
        internal const int MaxReportChars = 48_000;
        internal const int MaxBepInExTailBytes = 24_000;

        private static readonly object BufferLock = new object();
        private static readonly StringBuilder Buffer = new StringBuilder(4096);

        internal static void Record(string logString, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(logString))
                return;

            try
            {
                var line = FormatLine(logString, stackTrace, type);
                lock (BufferLock)
                {
                    if (Buffer.Length > 0)
                        Buffer.Append('\n');
                    Buffer.Append(line);
                    TrimToMaxChars(Buffer, MaxInMemoryChars);
                }
            }
            catch
            {
                // Never break logging pipeline
            }
        }

        internal static string CaptureForReport()
        {
            string session;
            lock (BufferLock)
            {
                session = Buffer.Length == 0 ? string.Empty : Buffer.ToString();
            }

            var bepinexTail = TryReadBepInExLogTail();
            var combined = CombineSections(session, bepinexTail);
            return TruncateWithNotice(combined, MaxReportChars);
        }

        private static string FormatLine(string logString, string stackTrace, LogType type)
        {
            var t = Time.realtimeSinceStartup;
            var sb = new StringBuilder(logString.Length + 32);
            sb.Append('[').Append(t.ToString("F1")).Append("s] ");
            sb.Append('[').Append(type).Append("] ");
            sb.Append(logString);

            if (!string.IsNullOrEmpty(stackTrace)
                && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            {
                sb.Append('\n').Append(stackTrace);
            }

            return sb.ToString();
        }

        private static string CombineSections(string sessionLog, string bepinexTail)
        {
            if (string.IsNullOrEmpty(sessionLog) && string.IsNullOrEmpty(bepinexTail))
                return string.Empty;

            if (string.IsNullOrEmpty(bepinexTail))
                return sessionLog;

            if (string.IsNullOrEmpty(sessionLog))
            {
                return "--- BepInEx LogOutput.log (tail) ---\n" + bepinexTail;
            }

            var sb = new StringBuilder(sessionLog.Length + bepinexTail.Length + 64);
            sb.Append(sessionLog);
            sb.Append("\n\n--- BepInEx LogOutput.log (tail) ---\n");
            sb.Append(bepinexTail);
            return sb.ToString();
        }

        private static string TryReadBepInExLogTail()
        {
            try
            {
                var root = Paths.BepInExRootPath;
                if (string.IsNullOrEmpty(root))
                    return string.Empty;

                var path = Path.Combine(root, "LogOutput.log");
                if (!File.Exists(path))
                    return string.Empty;

                using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length == 0)
                    return string.Empty;

                var readLen = (int)Math.Min(MaxBepInExTailBytes, stream.Length);
                stream.Seek(-readLen, SeekOrigin.End);
                var bytes = new byte[readLen];
                var got = stream.Read(bytes, 0, readLen);
                if (got <= 0)
                    return string.Empty;

                var text = Encoding.UTF8.GetString(bytes, 0, got);
                if (got < stream.Length)
                    text = "...[log truncated to last " + readLen + " bytes]\n" + text;

                return text;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TruncateWithNotice(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value ?? string.Empty;

            var notice = $"...[console log truncated; showing last {maxLength} characters]\n";
            var keep = Math.Max(0, maxLength - notice.Length);
            return notice + value.Substring(value.Length - keep, keep);
        }

        private static void TrimToMaxChars(StringBuilder sb, int maxChars)
        {
            if (sb.Length <= maxChars)
                return;

            var drop = sb.Length - maxChars;
            sb.Remove(0, drop);
            sb.Insert(0, "...[earlier log lines dropped]\n");
        }
    }
}
