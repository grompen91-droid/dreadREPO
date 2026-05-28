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
        private const int AcceptPollMs = 50;
        private const int ShutdownJoinMs = 250;

        private Thread? _serverThread;
        private TcpListener? _listener;
        private volatile bool _running;
        private volatile bool _shutdownComplete;

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

            Application.quitting += OnApplicationQuit;

            Plugin.Logger.LogInfo($"[Dread DebugServer] LISTENING 127.0.0.1:{_boundPort}");
        }

        private void OnApplicationQuit() => ShutdownServer();

        private void OnDestroy()
        {
            Application.quitting -= OnApplicationQuit;
            ShutdownServer();
        }

        private void ShutdownServer()
        {
            if (_shutdownComplete)
                return;
            _shutdownComplete = true;

            _running = false;

            if (_logListener != null)
            {
                Logger.Listeners.Remove(_logListener);
                _logListener = null;
            }

            ReleasePendingCommands();

            try
            {
                _listener?.Server?.Close();
            }
            catch { }

            try
            {
                _listener?.Stop();
            }
            catch { }

            _listener = null;

            var thread = _serverThread;
            if (thread != null && thread.IsAlive && Thread.CurrentThread != thread)
                thread.Join(ShutdownJoinMs);

            _serverThread = null;
        }

        private void ReleasePendingCommands()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                {
                    var cmd = _queue.Dequeue();
                    int errId = 0;
                    try { errId = JsonUtility.FromJson<RequestEnvelope>(cmd.RequestJson).id; } catch { }
                    cmd.Response = MakeResponse(errId, false, "server shutting down", -1);
                    cmd.Done.Set();
                }
            }
        }

        private bool WaitForCommandDone(DebugCommand cmd)
        {
            var deadline = Environment.TickCount + CommandTimeoutMs;
            while (_running)
            {
                var remaining = deadline - Environment.TickCount;
                if (remaining <= 0)
                    break;
                var slice = remaining > 100 ? 100 : remaining;
                if (cmd.Done.Wait(slice))
                    return true;
            }

            int errId = 0;
            try { errId = JsonUtility.FromJson<RequestEnvelope>(cmd.RequestJson).id; } catch { }
            cmd.Response = MakeResponse(errId, false, "server shutting down", -1);
            cmd.Done.Set();
            return false;
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
                    if (_listener == null)
                        break;

                    if (!_listener.Pending())
                    {
                        Thread.Sleep(AcceptPollMs);
                        continue;
                    }

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

                        if (!WaitForCommandDone(cmd))
                        {
                            if (!_running)
                                break;
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
                        if (result.error != null)
                            return MakeResponse(req.id, false, result.error, -3);
                        return MakeResponse(req.id, true, result);
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
                    try { _listener?.Server?.Close(); } catch { }
                    try { _listener?.Stop(); } catch { }
                    _listener = null;
                    ReleasePendingCommands();
                    return MakeResponse(req.id, true, new ShutdownResponse());

                case "verify":
                    return MakeResponse(req.id, true, RunVerifyChecks());

                case "trigger_test_crash":
                    return TriggerTestCrash(req.id);

                case "force_psychotic_break":
                    return ForcePsychoticBreak(req.id);

                case "get_runtime_state":
                    return MakeResponse(req.id, true, CaptureRuntimeState());

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
            var flat = new ConfigResponse
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
                compatibilityMode = DreadConfig.CompatibilityMode.Value,
                compatibilitySkipConflictingPatches = DreadConfig.CompatibilitySkipConflictingPatches.Value,
                debugConsoleGuard = DreadConfig.DebugConsoleGuardEnabled.Value,
                psychoticBreak = DreadConfig.PsychoticBreakEnabled.Value,
                psychoticBreakTriggerChance = DreadConfig.PsychoticBreakTriggerChance.Value,
                psychoticBreakDuration = DreadConfig.PsychoticBreakDuration.Value,
                psychoticBreakOncePerMatch = DreadConfig.PsychoticBreakOncePerMatch.Value,
                debugServerEnabled = DreadConfig.DebugServerEnabled.Value,
                debugServerPort = DreadConfig.DebugServerPort.Value,
                debugOverlayEnabled = DreadConfig.DebugOverlayEnabled.Value,
                logLevel = DreadConfig.LogLevelEntry.Value.ToString(),
                sections = BuildConfigSections()
            };
            return flat;
        }

        private static ConfigSectionEntry[] BuildConfigSections()
        {
            return new[]
            {
                Section("1. Audio Dread",
                    Entry("Enabled", "audio.enabled", DreadConfig.AudioEnabled),
                    Entry("Frequency", "audio.frequency", DreadConfig.AudioFrequency),
                    Entry("Volume", "audio.volume", DreadConfig.AudioVolume)),
                Section("2. Monster Overhaul",
                    Entry("AggressionEnabled", "monster.aggression", DreadConfig.MonsterAggressionEnabled),
                    Entry("AudioEnabled", "monster.audio", DreadConfig.MonsterAudioEnabled)),
                Section("3. Tension",
                    Entry("FakeFootstepsEnabled", "tension.fakeFootsteps", DreadConfig.FakeFootstepsEnabled),
                    Entry("AdrenalineEnabled", "tension.adrenaline", DreadConfig.AdrenalineEnabled),
                    Entry("LowStaminaSoundEnabled", "tension.lowStamina", DreadConfig.LowStaminaSoundEnabled),
                    Entry("PanicSprintEnabled", "tension.panicSprint", DreadConfig.PanicSprintEnabled)),
                Section("4. QOL",
                    Entry("CrouchSpeedBoost", "crouch.speed", DreadConfig.CrouchSpeedBoostEnabled)),
                Section("5. Error Reporting",
                    Entry("ErrorReportingEnabled", "errorReporting", DreadConfig.ErrorReportingEnabled)),
                Section("6. Psychotic Break",
                    Entry("PsychoticBreakEnabled", "psychoticBreak.enabled", DreadConfig.PsychoticBreakEnabled),
                    Entry("PsychoticBreakTriggerChance", "psychoticBreak.triggerChance", DreadConfig.PsychoticBreakTriggerChance),
                    Entry("PsychoticBreakDuration", "psychoticBreak.duration", DreadConfig.PsychoticBreakDuration),
                    Entry("PsychoticBreakOncePerMatch", "psychoticBreak.oncePerMatch", DreadConfig.PsychoticBreakOncePerMatch)),
                Section("7. Testing",
                    Entry("Crash Game", "testing.crash", DreadConfig.TestCrashButton)),
                Section("8. Debug Server",
                    Entry("DebugServerEnabled", "debugServer.enabled", DreadConfig.DebugServerEnabled, restartRequired: true),
                    Entry("DebugServerPort", "debugServer.port", DreadConfig.DebugServerPort, restartRequired: true)),
                Section("9. Logging",
                    Entry("LogLevel", "logging.level", DreadConfig.LogLevelEntry)),
                Section("10. Compatibility",
                    Entry("CompatibilityMode", "compatibility.mode", DreadConfig.CompatibilityMode),
                    Entry("SkipConflictingPatches", "compatibility.skipConflictingPatches", DreadConfig.CompatibilitySkipConflictingPatches),
                    Entry("DebugConsoleGuardEnabled", "compatibility.debugConsoleGuard", DreadConfig.DebugConsoleGuardEnabled)),
                Section("11. Debug Overlay",
                    Entry("DebugOverlayEnabled", "overlay.enabled", DreadConfig.DebugOverlayEnabled)),
            };
        }

        private static ConfigSectionEntry Section(string section, params ConfigKeyEntry[] keys)
        {
            return new ConfigSectionEntry { section = section, keys = keys };
        }

        private static ConfigKeyEntry Entry(string key, string debugKey, ConfigEntryBase entry, bool restartRequired = false)
        {
            string type = entry is ConfigEntry<bool> ? "bool"
                : entry is ConfigEntry<float> ? "float"
                : entry is ConfigEntry<int> ? "int"
                : entry is ConfigEntry<LogLevel> ? "LogLevel"
                : "string";

            return new ConfigKeyEntry
            {
                key = key,
                debugKey = debugKey,
                value = ConfigValueToString(entry),
                type = type,
                description = entry.Description.Description ?? "",
                restartRequired = restartRequired
            };
        }

        private static string ConfigValueToString(ConfigEntryBase entry)
        {
            return entry switch
            {
                ConfigEntry<bool> b => b.Value ? "true" : "false",
                ConfigEntry<float> f => f.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ConfigEntry<int> i => i.Value.ToString(),
                ConfigEntry<LogLevel> l => l.Value.ToString(),
                _ => ""
            };
        }

        private static SetConfigResult SetConfigValue(string section, string key, string value)
        {
            var entries = new Dictionary<string, (ConfigEntryBase entry, bool restartRequired)>
            {
                ["audio.enabled"] = (DreadConfig.AudioEnabled, false),
                ["audio.frequency"] = (DreadConfig.AudioFrequency, false),
                ["audio.volume"] = (DreadConfig.AudioVolume, false),
                ["monster.aggression"] = (DreadConfig.MonsterAggressionEnabled, false),
                ["monster.audio"] = (DreadConfig.MonsterAudioEnabled, false),
                ["crouch.speed"] = (DreadConfig.CrouchSpeedBoostEnabled, false),
                ["tension.fakeFootsteps"] = (DreadConfig.FakeFootstepsEnabled, false),
                ["tension.adrenaline"] = (DreadConfig.AdrenalineEnabled, false),
                ["tension.lowStamina"] = (DreadConfig.LowStaminaSoundEnabled, false),
                ["tension.panicSprint"] = (DreadConfig.PanicSprintEnabled, false),
                ["errorReporting"] = (DreadConfig.ErrorReportingEnabled, false),
                ["compatibility.mode"] = (DreadConfig.CompatibilityMode, false),
                ["compatibility.skipConflictingPatches"] = (DreadConfig.CompatibilitySkipConflictingPatches, false),
                ["compatibility.debugConsoleGuard"] = (DreadConfig.DebugConsoleGuardEnabled, false),
                ["psychoticBreak.enabled"] = (DreadConfig.PsychoticBreakEnabled, false),
                ["psychoticBreak.triggerChance"] = (DreadConfig.PsychoticBreakTriggerChance, false),
                ["psychoticBreak.duration"] = (DreadConfig.PsychoticBreakDuration, false),
                ["psychoticBreak.oncePerMatch"] = (DreadConfig.PsychoticBreakOncePerMatch, false),
                ["testing.crash"] = (DreadConfig.TestCrashButton, false),
                ["debugServer.enabled"] = (DreadConfig.DebugServerEnabled, true),
                ["debugServer.port"] = (DreadConfig.DebugServerPort, true),
                ["overlay.enabled"] = (DreadConfig.DebugOverlayEnabled, false),
                ["logging.level"] = (DreadConfig.LogLevelEntry, false),
            };

            var combinedKey = string.IsNullOrEmpty(section)
                ? key
                : string.IsNullOrEmpty(key)
                    ? section
                    : $"{section}.{key}";

            if (!entries.TryGetValue(combinedKey, out var target) || target.entry == null)
                return SetConfigResult.Fail($"Unknown config: {section}/{key} (debug key: {combinedKey})");

            try
            {
                if (target.entry is ConfigEntry<bool> be)
                    be.Value = bool.Parse(value);
                else if (target.entry is ConfigEntry<float> fe)
                    fe.Value = float.Parse(value);
                else if (target.entry is ConfigEntry<int> ie)
                    ie.Value = int.Parse(value);
                else if (target.entry is ConfigEntry<LogLevel> le)
                    le.Value = (LogLevel)Enum.Parse(typeof(LogLevel), value, ignoreCase: true);
                else
                    return SetConfigResult.Fail($"Unsupported config type for {section}/{key}");
            }
            catch (Exception ex)
            {
                return SetConfigResult.Fail($"Failed to set {section}/{key}: {ex.Message}");
            }

            var result = SetConfigResult.Ok(combinedKey, value);
            if (target.restartRequired)
                result.warning = "Restart the game for debug server bind changes to take effect.";
            return result;
        }

        private VerifyResponse RunVerifyChecks()
        {
            var checks = new List<VerifyCheck>
            {
                Check("version", !string.IsNullOrEmpty(Plugin.VERSION), $"version={Plugin.VERSION}"),
                Check("debug_server_listening", _running && _listener != null, $"port={_boundPort}"),
                Check("systems_count", CountActiveSystems() >= 7, $"count={CountActiveSystems()}"),
                Check("audio_clips", DreadRuntimeState.AudioClipCount > 0,
                    $"loaded={DreadRuntimeState.AudioClipCount}/4"),
                Check("psychotic_break_clips", DreadRuntimeState.PsychoticBreakClipsLoaded,
                    DreadRuntimeState.PsychoticBreakClipsLoaded ? "all loaded" : "missing clips"),
                Check("overlay_present", FindObjectOfType<DebugOverlaySystem>() != null, "DebugOverlaySystem host"),
                Check("harmony_patches", GetDreadPatchCount() > 0, $"dreadPatches={GetDreadPatchCount()}"),
            };

            return new VerifyResponse { checks = checks.ToArray() };
        }

        private static int CountActiveSystems()
        {
            int count = 0;
            if (FindObjectOfType<AudioDreadSystem>() != null) count++;
            if (FindObjectOfType<MonsterOverhaulSystem>() != null) count++;
            if (FindObjectOfType<TensionSystem>() != null) count++;
            if (FindObjectOfType<ErrorReporterSystem>() != null) count++;
            if (FindObjectOfType<PsychoticBreakSystem>() != null) count++;
            if (FindObjectOfType<TestCrashSystem>() != null) count++;
            if (FindObjectOfType<DebugServerSystem>() != null) count++;
            if (FindObjectOfType<DebugOverlaySystem>() != null) count++;
            return count;
        }

        private static int GetDreadPatchCount()
        {
            if (DreadRuntimeState.DreadPatchCount > 0)
                return DreadRuntimeState.DreadPatchCount;

            var harmony = Plugin.HarmonyInstance;
            if (harmony == null)
                return 0;

            int count = 0;
            try
            {
                foreach (var method in Harmony.GetAllPatchedMethods())
                {
                    var info = Harmony.GetPatchInfo(method);
                    if (info == null)
                        continue;

                    if (info.Prefixes?.Any(p => p.owner == Plugin.GUID) == true) count++;
                    if (info.Postfixes?.Any(p => p.owner == Plugin.GUID) == true) count++;
                    if (info.Transpilers?.Any(p => p.owner == Plugin.GUID) == true) count++;
                    if (info.Finalizers?.Any(p => p.owner == Plugin.GUID) == true) count++;
                }
            }
            catch
            {
                return -1;
            }

            return count;
        }

        private static VerifyCheck Check(string id, bool ok, string message)
        {
            return new VerifyCheck { id = id, ok = ok, message = message };
        }

        private static string TriggerTestCrash(int id)
        {
            if (!DreadConfig.DebugServerEnabled.Value)
                return MakeResponse(id, false, "Debug server disabled", -3);

            TestCrashSystem.TriggerForDebug();
            return MakeResponse(id, true, new TriggerResponse { triggered = true });
        }

        private static string ForcePsychoticBreak(int id)
        {
            if (!DreadConfig.DebugServerEnabled.Value)
                return MakeResponse(id, false, "Debug server disabled", -3);

            var system = FindObjectOfType<PsychoticBreakSystem>();
            if (system == null)
                return MakeResponse(id, false, "PsychoticBreakSystem not found", -3);

            try
            {
                system.ForceEpisodeForDebug();
            }
            catch (Exception ex)
            {
                return MakeResponse(id, false, ex.Message, -1);
            }

            return MakeResponse(id, true, new TriggerResponse { triggered = true });
        }

        private static RuntimeStateResponse CaptureRuntimeState()
        {
            float nearest = DreadRuntimeState.NearestEnemyDist;
            return new RuntimeStateResponse
            {
                nearestEnemyDist = nearest >= float.MaxValue * 0.5f ? -1f : nearest,
                psychoticBreakEnabled = DreadRuntimeState.PsychoticBreakEnabled,
                psychoticBreakCanTrigger = DreadRuntimeState.PsychoticBreakCanTrigger,
                psychoticBreakBlockReason = DreadRuntimeState.PsychoticBreakBlockReason,
                psychoticBreakEpisodeActive = DreadRuntimeState.PsychoticBreakEpisodeActive,
                psychoticBreakEpisodeTimer = DreadRuntimeState.PsychoticBreakEpisodeTimer,
                psychoticBreakEpisodeDuration = DreadRuntimeState.PsychoticBreakEpisodeDuration,
                psychoticBreakNextCheckIn = DreadRuntimeState.PsychoticBreakNextCheckIn,
                psychoticBreakThreatCount = DreadRuntimeState.PsychoticBreakThreatCount,
                psychoticBreakEnemyCount = DreadRuntimeState.PsychoticBreakEnemyCount,
                psychoticBreakClipsLoaded = DreadRuntimeState.PsychoticBreakClipsLoaded,
                adrenalineActive = DreadRuntimeState.AdrenalineActive,
                panicSprintActive = DreadRuntimeState.PanicSprintActive,
                panicSprintCooldown = DreadRuntimeState.PanicSprintCooldown,
                audioClipCount = DreadRuntimeState.AudioClipCount,
                audioNextPlayIn = DreadRuntimeState.AudioNextPlayIn,
                dreadPatchCount = DreadRuntimeState.DreadPatchCount,
                debugOverlayEnabled = DreadConfig.DebugOverlayEnabled.Value,
                debugOverlayPresent = FindObjectOfType<DebugOverlaySystem>() != null,
            };
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
            public bool compatibilityMode;
            public bool compatibilitySkipConflictingPatches;
            public bool debugConsoleGuard;
            public bool psychoticBreak;
            public float psychoticBreakTriggerChance;
            public float psychoticBreakDuration;
            public bool psychoticBreakOncePerMatch;
            public bool debugServerEnabled;
            public int debugServerPort;
            public bool debugOverlayEnabled;
            public string logLevel = "";
            public ConfigSectionEntry[] sections = null!;
        }

        [Serializable]
        private class ConfigSectionEntry
        {
            public string section = "";
            public ConfigKeyEntry[] keys = null!;
        }

        [Serializable]
        private class ConfigKeyEntry
        {
            public string key = "";
            public string debugKey = "";
            public string value = "";
            public string type = "";
            public string description = "";
            public bool restartRequired;
        }

        [Serializable]
        private class SetConfigResult
        {
            public string status = "ok";
            public string debugKey = "";
            public string value = "";
            public string warning = "";

            public static SetConfigResult Ok(string debugKey, string value)
            {
                return new SetConfigResult { debugKey = debugKey, value = value };
            }

            public static SetConfigResult Fail(string error)
            {
                return new SetConfigResult { status = "error", error = error };
            }

            public string? error;
        }

        [Serializable]
        private class VerifyResponse
        {
            public VerifyCheck[] checks = null!;
        }

        [Serializable]
        private class VerifyCheck
        {
            public string id = "";
            public bool ok;
            public string message = "";
        }

        [Serializable]
        private class TriggerResponse
        {
            public bool triggered;
        }

        [Serializable]
        private class RuntimeStateResponse
        {
            public float nearestEnemyDist = -1;
            public bool psychoticBreakEnabled;
            public bool psychoticBreakCanTrigger;
            public string psychoticBreakBlockReason = "";
            public bool psychoticBreakEpisodeActive;
            public float psychoticBreakEpisodeTimer;
            public float psychoticBreakEpisodeDuration;
            public float psychoticBreakNextCheckIn;
            public int psychoticBreakThreatCount;
            public int psychoticBreakEnemyCount;
            public bool psychoticBreakClipsLoaded;
            public bool adrenalineActive;
            public bool panicSprintActive;
            public float panicSprintCooldown;
            public int audioClipCount;
            public float audioNextPlayIn = -1;
            public int dreadPatchCount;
            public bool debugOverlayEnabled;
            public bool debugOverlayPresent;
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
