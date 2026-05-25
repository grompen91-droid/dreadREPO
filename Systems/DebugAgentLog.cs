using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace Dread.Systems
{
    /// <summary>
    /// NDJSON debug session logger (agent instrumentation).
    /// </summary>
    internal static class DebugAgentLog
    {
        private const string SessionId = "77d1e2";
        private static int _writes;
        private static string? _gameLogPath;
        private static string? _workspaceLogPath;

        private static string GameLogPath =>
            _gameLogPath ??= Path.Combine(Paths.BepInExRootPath, "dread-debug-77d1e2.log");

        private static string WorkspaceLogPath
        {
            get
            {
                if (_workspaceLogPath != null)
                    return _workspaceLogPath;

                try
                {
                    var dir = new DirectoryInfo(Paths.BepInExRootPath);
                    for (int i = 0; i < 8 && dir != null; i++)
                    {
                        var candidate = Path.Combine(dir.FullName, ".cursor", "debug-77d1e2.log");
                        if (Directory.Exists(Path.Combine(dir.FullName, ".cursor")))
                        {
                            _workspaceLogPath = candidate;
                            return candidate;
                        }

                        dir = dir.Parent;
                    }
                }
                catch
                {
                    // fall through
                }

                _workspaceLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor", "debug-77d1e2.log");
                return _workspaceLogPath;
            }
        }

        public static void Write(
            string hypothesisId,
            string location,
            string message,
            string runId = "pre-fix",
            params (string key, object value)[] data)
        {
            if (_writes >= 800)
                return;

            try
            {
                var line = BuildLine(hypothesisId, location, message, runId, data);
                File.AppendAllText(GameLogPath, line);
                TryAppendWorkspace(line);
                _writes++;
            }
            catch
            {
                // never break the game for debug logging
            }
        }

        private static string BuildLine(
            string hypothesisId,
            string location,
            string message,
            string runId,
            (string key, object value)[] data)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"sessionId\":\"").Append(SessionId);
            sb.Append("\",\"hypothesisId\":\"").Append(Escape(hypothesisId));
            sb.Append("\",\"runId\":\"").Append(Escape(runId));
            sb.Append("\",\"location\":\"").Append(Escape(location));
            sb.Append("\",\"message\":\"").Append(Escape(message));
            sb.Append("\",\"timestamp\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append(",\"data\":{");
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append('"').Append(Escape(data[i].key)).Append("\":");
                AppendValue(sb, data[i].value);
            }

            sb.Append("}}\n");
            return sb.ToString();
        }

        private static void TryAppendWorkspace(string line)
        {
            try
            {
                var path = WorkspaceLogPath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(path, line);
            }
            catch
            {
                // workspace log is best-effort
            }
        }

        private static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int i:
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                    break;
                case float f:
                    sb.Append(f.ToString(CultureInfo.InvariantCulture));
                    break;
                case double d:
                    sb.Append(d.ToString(CultureInfo.InvariantCulture));
                    break;
                case null:
                    sb.Append("null");
                    break;
                default:
                    sb.Append('"').Append(Escape(value.ToString() ?? "")).Append('"');
                    break;
            }
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
