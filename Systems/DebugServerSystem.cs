using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx.Configuration;
using BepInEx.Logging;
using Dread.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class DebugServerSystem : MonoBehaviour
    {
        private const int MaxMessageBytes = 4096;
        private const int ReadTimeoutMs = 5000;
        private const int CommandTimeoutMs = 10000;
        private const int MaxLogEntries = 200;
        private const int MaxQueueDepth = 256;

        private Thread? _serverThread;
        private TcpListener? _listener;
        private volatile bool _running;

        private readonly Queue<DebugCommand> _queue = new(32);
        private readonly List<LogEntry> _logBuffer = new(MaxLogEntries);
        private readonly object _logLock = new();

        private int _boundPort;

        private class DebugCommand
        {
            public string RequestJson;
            public string? Response;
            public ManualResetEventSlim Done = new(false);
        }

        private class LogEntry
        {
            public string Level;
            public string Message;
            public string Timestamp;
        }

        private class DebugLogListener : ILogListener
        {
            private readonly DebugServerSystem _owner;
            public BepInEx.Logging.LogLevel LogLevelFilter => BepInEx.Logging.LogLevel.All;
            public DebugLogListener(DebugServerSystem owner) => _owner = owner;

            public void LogEvent(object sender, LogEventArgs e)
            {
                _owner.AddLogEntry($"[{e.Level}]", e.Data?.ToString() ?? "");
            }

            public void Dispose() { }
        }

        private DebugLogListener? _logListener;

        private void Start()
        {
            if (!DreadConfig.DebugServerEnabled.Value)
            {
                enabled = false;
                return;
            }

            int port = DreadConfig.DebugServerPort.Value;
            _listener = new TcpListener(IPAddress.Loopback, port);

            try
            {
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                _boundPort = port;
            }
            catch (SocketException)
            {
                port = port + 1;
                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, port);
                    _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _listener.Start();
                    _boundPort = port;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[Dread DebugServer] Failed to bind on {port - 1} or {port}: {ex.Message}");
                    _listener = null;
                    enabled = false;
                    return;
                }
            }

            _logListener = new DebugLogListener(this);
            Logger.Listeners.Add(_logListener);

            _running = true;
            _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "DreadDebugServer" };
            _serverThread.Start();

            Plugin.Logger.LogInfo($"[Dread DebugServer] LISTENING 127.0.0.1:{_boundPort}");
        }

        private void OnDestroy()
        {
            if (_logListener != null)
                Logger.Listeners.Remove(_logListener);

            _running = false;
            _listener?.Stop();
            _serverThread?.Join(1000);
        }

        private void AddLogEntry(string level, string message)
        {
            lock (_logLock)
            {
                _logBuffer.Add(new LogEntry
                {
                    Level = level,
                    Message = message,
                    Timestamp = DateTime.UtcNow.ToString("O")
                });
                if (_logBuffer.Count > MaxLogEntries)
                    _logBuffer.RemoveAt(0);
            }
        }

        private void ServerLoop()
        {
            while (_running)
            {
                try
                {
                    if (_listener == null) break;
                    using var client = _listener.AcceptTcpClient();
                    client.ReceiveTimeout = ReadTimeoutMs;
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    while (_running)
                    {
                        string? line;
                        try
                        {
                            line = reader.ReadLine();
                        }
                        catch (IOException)
                        {
                            break;
                        }

                        if (line == null)
                            break;

                        line = line.Trim();
                        if (line.Length == 0)
                            continue;

                        var cmd = new DebugCommand { RequestJson = line };
                        lock (_queue)
                        {
                            if (_queue.Count >= MaxQueueDepth)
                            {
                                RequestEnvelope envelope;
                                try { envelope = JsonUtility.FromJson<RequestEnvelope>(line); }
                                catch { envelope = new RequestEnvelope(); }
                                var reject = $"{{\"id\":{envelope.id},\"ok\":false,"
                                    + "\"error\":\"queue full\",\"code\":-1}}\n";
                                var rejectBytes = Encoding.UTF8.GetBytes(reject);
                                try { stream.Write(rejectBytes, 0, rejectBytes.Length); }
                                catch (IOException) { }
                                break;
                            }
                            _queue.Enqueue(cmd);
                        }

                        if (!cmd.Done.Wait(CommandTimeoutMs))
                        {
                            Plugin.Logger.LogWarning("[Dread DebugServer] Command timed out");
                            break;
                        }

                        var responseBytes = Encoding.UTF8.GetBytes((cmd.Response ?? "{}") + "\n");
                        try { stream.Write(responseBytes, 0, responseBytes.Length); }
                        catch (IOException) { break; }
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex) when (_running)
                {
                    Plugin.Logger.LogWarning($"[Dread DebugServer] Connection error: {ex.Message}");
                    Thread.Sleep(500);
                }
            }
        }

        private void Update()
        {
            DebugCommand? cmd;
            while (true)
            {
                lock (_queue)
                {
                    if (_queue.Count == 0)
                        break;
                    cmd = _queue.Dequeue();
                }

                try
                {
                    cmd.Response = ExecuteCommand(cmd.RequestJson);
                }
                catch (Exception ex)
                {
                    int errId = 0;
                    try { errId = JsonUtility.FromJson<RequestEnvelope>(cmd.RequestJson).id; } catch { }
                    cmd.Response = MakeResponse(errId, false, Sanitize(ex.Message), -1);
                    Plugin.Logger.LogError($"[Dread DebugServer] Command error: {ex}");
                }
                finally
                {
                    cmd.Done.Set();
                }
            }
        }

        private string ExecuteCommand(string rawJson)
        {
            RequestEnvelope req;
            try { req = JsonUtility.FromJson<RequestEnvelope>(rawJson); }
            catch { return MakeResponse(0, false, "invalid JSON", -3); }

            if (req.cmd == null)
                return MakeResponse(req.id, false, "Missing cmd field", -3);

            switch (req.cmd)
            {
                case "ping":
                    return MakeResponse(req.id, true, new PingResponse { version = Plugin.VERSION, port = _boundPort });

                case "get_state":
                    return MakeResponse(req.id, true, CaptureState());

                case "get_config":
                    return MakeResponse(req.id, true, CaptureConfig());

                case "set_config":
                    {
                        SetConfigRequest setReq;
                        try { setReq = JsonUtility.FromJson<SetConfigRequest>(rawJson); }
                        catch { return MakeResponse(req.id, false, "invalid JSON", -3); }

                        if (setReq.data?.section == null || setReq.data?.key == null || setReq.data?.value == null)
                            return MakeResponse(req.id, false, "Missing section, key, or value", -3);

                        var result = SetConfigValue(setReq.data.section, setReq.data.key, setReq.data.value);
                        if (result != null)
                            return MakeResponse(req.id, false, result, -3);
                        return MakeResponse(req.id, true, "ok");
                    }

                case "get_patches":
                    return MakeResponse(req.id, true, GetHarmonyPatches());

                case "get_logs":
                    {
                        lock (_logLock)
                        {
                            return MakeResponse(req.id, true, new LogsResponse { logs = _logBuffer.ToArray() });
                        }
                    }

                case "shutdown":
                    Plugin.Logger.LogInfo("[Dread DebugServer] Shutdown requested via debug command");
                    _running = false;
                    _listener?.Stop();
                    return MakeResponse(req.id, true, new ShutdownResponse());

                default:
                    return MakeResponse(req.id, false, $"Unknown command: {req.cmd}", -2);
            }
        }

        private StateResponse CaptureState()
        {
            string scene = "unknown";
            int enemyCount = 0;
            float nearestEnemyDist = -1;
            float playerHp = -1;
            float playerStamina = -1;

            try
            {
                scene = SceneManager.GetActiveScene().name ?? "unknown";
                var enemies = FindObjectsOfType<EnemyHealth>();
                enemyCount = enemies.Length;
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    playerHp = ReadPlayerFloat(player, "Health", "health", "playerHealth");
                    playerStamina = ReadPlayerFloat(player, "stamina", "Stamina", "energy");
                    foreach (var e in enemies)
                    {
                        if (e.CurrentHealth > 0)
                        {
                            var dist = Vector3.Distance(e.transform.position, player.transform.position);
                            if (nearestEnemyDist < 0 || dist < nearestEnemyDist)
                                nearestEnemyDist = dist;
                        }
                    }
                }
            }
            catch
            {
                // not in a level, safe defaults returned
            }

            return new StateResponse
            {
                version = Plugin.VERSION,
                scene = scene,
                enemyCount = enemyCount,
                nearestEnemyDist = nearestEnemyDist,
                playerHp = playerHp,
                playerStamina = playerStamina,
                debugServerPort = _boundPort,
                isEnabled = DreadConfig.DebugServerEnabled.Value
            };
        }

        private static ConfigResponse CaptureConfig()
        {
            return new ConfigResponse
            {
                audioEnabled = DreadConfig.AudioEnabled.Value,
                audioFrequency = DreadConfig.AudioFrequency.Value,
                audioVolume = DreadConfig.AudioVolume.Value,
                monsterAggression = DreadConfig.MonsterAggressionEnabled.Value,
                monsterAudio = DreadConfig.MonsterAudioEnabled.Value,
                crouchSpeedBoost = DreadConfig.CrouchSpeedBoostEnabled.Value,
                fakeFootsteps = DreadConfig.FakeFootstepsEnabled.Value,
                adrenaline = DreadConfig.AdrenalineEnabled.Value,
                lowStaminaSound = DreadConfig.LowStaminaSoundEnabled.Value,
                panicSprint = DreadConfig.PanicSprintEnabled.Value,
                errorReporting = DreadConfig.ErrorReportingEnabled.Value,
                psychoticBreak = DreadConfig.PsychoticBreakEnabled.Value,
                psychoticBreakTriggerChance = DreadConfig.PsychoticBreakTriggerChance.Value,
                psychoticBreakDuration = DreadConfig.PsychoticBreakDuration.Value,
                psychoticBreakOncePerMatch = DreadConfig.PsychoticBreakOncePerMatch.Value,
                debugServerEnabled = DreadConfig.DebugServerEnabled.Value,
                debugServerPort = DreadConfig.DebugServerPort.Value
            };
        }

        private static string? SetConfigValue(string section, string key, string value)
        {
            var entries = new Dictionary<string, ConfigEntryBase>
            {
                ["audio.enabled"] = DreadConfig.AudioEnabled,
                ["audio.frequency"] = DreadConfig.AudioFrequency,
                ["audio.volume"] = DreadConfig.AudioVolume,
                ["monster.aggression"] = DreadConfig.MonsterAggressionEnabled,
                ["monster.audio"] = DreadConfig.MonsterAudioEnabled,
                ["crouch.speed"] = DreadConfig.CrouchSpeedBoostEnabled,
                ["tension.fakeFootsteps"] = DreadConfig.FakeFootstepsEnabled,
                ["tension.adrenaline"] = DreadConfig.AdrenalineEnabled,
                ["tension.lowStamina"] = DreadConfig.LowStaminaSoundEnabled,
                ["tension.panicSprint"] = DreadConfig.PanicSprintEnabled,
                ["errorReporting"] = DreadConfig.ErrorReportingEnabled,
                ["psychoticBreak.enabled"] = DreadConfig.PsychoticBreakEnabled,
                ["psychoticBreak.triggerChance"] = DreadConfig.PsychoticBreakTriggerChance,
                ["psychoticBreak.duration"] = DreadConfig.PsychoticBreakDuration,
                ["psychoticBreak.oncePerMatch"] = DreadConfig.PsychoticBreakOncePerMatch,
            };

            var combinedKey = $"{section}.{key}";
            if (!entries.TryGetValue(combinedKey, out var entry) || entry == null)
                return $"Unknown config: {section}/{key}";

            try
            {
                if (entry is ConfigEntry<bool> be)
                    be.Value = bool.Parse(value);
                else if (entry is ConfigEntry<float> fe)
                    fe.Value = float.Parse(value);
                else if (entry is ConfigEntry<int> ie)
                    ie.Value = int.Parse(value);
                else
                    return $"Unsupported config type for {section}/{key}";
            }
            catch (Exception ex)
            {
                return $"Failed to set {section}/{key}: {ex.Message}";
            }

            return null;
        }

        private PatchesResponse GetHarmonyPatches()
        {
            var harmony = Plugin.HarmonyInstance;
            if (harmony == null)
                return new PatchesResponse { patches = Array.Empty<PatchEntry>() };

            var patchList = new List<PatchEntry>();
            try
            {
                var methods = Harmony.GetAllPatchedMethods();
                foreach (var method in methods)
                {
                    var info = Harmony.GetPatchInfo(method);
                    patchList.Add(new PatchEntry
                    {
                        method = method.FullDescription(),
                        prefixes = info?.Prefixes?.Count ?? 0,
                        postfixes = info?.Postfixes?.Count ?? 0,
                        transpilers = info?.Transpilers?.Count ?? 0,
                        finalizers = info?.Finalizers?.Count ?? 0,
                        owners = info?.Prefixes?.Select(p => p.owner)
                            .Concat(info?.Postfixes?.Select(p => p.owner) ?? Enumerable.Empty<string>())
                            .Distinct()
                            .ToArray() ?? Array.Empty<string>()
                    });
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[Dread DebugServer] get_patches error: {ex.Message}");
            }

            return new PatchesResponse { patches = patchList.ToArray() };
        }

        private static string MakeResponse(int id, bool ok, object data)
        {
            var payload = JsonUtility.ToJson(data);
            return $"{{\"id\":{id},\"ok\":{(ok ? "true" : "false")},\"data\":{payload}}}";
        }

        private static string MakeResponse(int id, bool ok, string error, int code)
        {
            var safeError = Sanitize(error);
            return $"{{\"id\":{id},\"ok\":{(ok ? "true" : "false")},\"error\":\"{safeError}\",\"code\":{code}}}";
        }

        private static float ReadPlayerFloat(PlayerController player, params string[] names)
        {
            foreach (var name in names)
            {
                try
                {
                    return Traverse.Create(player).Property<float>(name).Value;
                }
                catch { }

                try
                {
                    return Traverse.Create(player).Field<float>(name).Value;
                }
                catch { }
            }

            return -1f;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 0x20) sb.Append($"\\u{(int)c:x4}");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        [Serializable]
        private class RequestEnvelope { public int id; public string? cmd; }

        [Serializable]
        private class PingResponse
        {
            public bool pong = true;
            public string? version;
            public int port;
        }

        [Serializable]
        private class StateResponse
        {
            public string version = "";
            public string scene = "";
            public int enemyCount;
            public float nearestEnemyDist = -1;
            public float playerHp = -1;
            public float playerStamina = -1;
            public int debugServerPort;
            public bool isEnabled;
        }

        [Serializable]
        private class ConfigResponse
        {
            public bool audioEnabled;
            public float audioFrequency;
            public float audioVolume;
            public bool monsterAggression;
            public bool monsterAudio;
            public bool crouchSpeedBoost;
            public bool fakeFootsteps;
            public bool adrenaline;
            public bool lowStaminaSound;
            public bool panicSprint;
            public bool errorReporting;
            public bool psychoticBreak;
            public float psychoticBreakTriggerChance;
            public float psychoticBreakDuration;
            public bool psychoticBreakOncePerMatch;
            public bool debugServerEnabled;
            public int debugServerPort;
        }

        [Serializable]
        private class ShutdownResponse { public string status = "shutting down"; }

        [Serializable]
        private class LogsResponse { public LogEntry[] logs = null!; }

        [Serializable]
        private class PatchesResponse { public PatchEntry[] patches = null!; }

        [Serializable]
        private class PatchEntry
        {
            public string method = "";
            public int prefixes;
            public int postfixes;
            public int transpilers;
            public int finalizers;
            public string[] owners = null!;
        }

        [Serializable]
        private class SetConfigRequest
        {
            public string? cmd;
            public SetConfigData? data;
        }

        [Serializable]
        private class SetConfigData
        {
            public string? section;
            public string? key;
            public string? value;
        }
    }
}
