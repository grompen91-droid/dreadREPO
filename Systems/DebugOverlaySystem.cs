using System.Collections.Generic;
using System.Linq;
using Dread.Config;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    public class DebugOverlaySystem : MonoBehaviour
    {
        private bool _visible;
        private float _nextPatchRefresh;
        private bool _loggedDisabledWhileRunning;
        private GUIStyle? _boxStyle;
        private GUIStyle? _labelStyle;
        private readonly List<string> _lines = new(24);

        private void Awake()
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }

        private void OnDestroy()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
        }

        private void OnOverlayConfigChanged(object? sender, System.EventArgs e)
        {
            _visible = DreadConfig.DebugOverlayEnabled.Value;
            enabled = DreadConfig.DebugOverlayEnabled.Value;
        }

        private void Update()
        {
            if (!GuardOverlayEnabled())
                return;

            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;

            if (!IsOverlayVisible())
                return;

            if (Time.time >= _nextPatchRefresh)
            {
                _nextPatchRefresh = Time.time + 0.5f;
                DreadRuntimeState.DreadPatchCount = CountDreadPatches();
            }
        }

        private void OnGUI()
        {
            if (!GuardOverlayEnabled())
                return;

            if (!IsOverlayVisible())
                return;

            EnsureStyles();
            BuildLines();

            const float width = 420f;
            const float lineHeight = 18f;
            const float padding = 8f;
            float height = padding * 2f + _lines.Count * lineHeight;

            var rect = new Rect(10f, 10f, width, height);
            GUI.Box(rect, GUIContent.none, _boxStyle!);

            var y = rect.y + padding;
            for (int i = 0; i < _lines.Count; i++)
            {
                GUI.Label(new Rect(rect.x + padding, y, width - padding * 2f, lineHeight), _lines[i], _labelStyle!);
                y += lineHeight;
            }
        }

        private bool IsOverlayVisible() => _visible && !SemiFunc.MenuLevel();

        private bool GuardOverlayEnabled()
        {
            if (DreadConfig.DebugOverlayEnabled.Value)
                return true;

            if (!_loggedDisabledWhileRunning)
            {
                _loggedDisabledWhileRunning = true;
                LoggingService.LogError(
                    "DebugOverlaySystem ran while DebugOverlayEnabled is false: enable/disable wiring regressed (PERF-2).");
            }

            return false;
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
                return;

            _boxStyle = new GUIStyle(GUI.skin.box);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white },
                wordWrap = false
            };
        }

        private void BuildLines()
        {
            _lines.Clear();

            _lines.Add($"Dread {Dread.Plugin.VERSION}  |  F10 toggle  |  refresh 0.5s");
            _lines.Add("");

            float nearest = DreadRuntimeState.NearestEnemyDist;
            string nearestText = nearest >= float.MaxValue * 0.5f ? "none" : $"{nearest:F1}m";
            _lines.Add($"Nearest enemy: {nearestText}");

            _lines.Add("");
            _lines.Add("Tension");
            _lines.Add($"  nearest: {nearestText}  (range 15m)");
            _lines.Add($"  adrenaline: {OnOff(DreadRuntimeState.AdrenalineActive)}");
            _lines.Add($"  panic sprint: {OnOff(DreadRuntimeState.PanicSprintActive)}"
                + $"  cd: {DreadRuntimeState.PanicSprintCooldown:F0}s");

            _lines.Add("");
            _lines.Add("PsychoticBreak");
            _lines.Add($"  enabled: {OnOff(DreadRuntimeState.PsychoticBreakEnabled)}");
            if (DreadRuntimeState.PsychoticBreakEpisodeActive)
            {
                float remaining = DreadRuntimeState.PsychoticBreakEpisodeDuration
                    - DreadRuntimeState.PsychoticBreakEpisodeTimer;
                _lines.Add($"  episode: ACTIVE  {remaining:F1}s left");
            }
            else
            {
                _lines.Add($"  canTrigger: {OnOff(DreadRuntimeState.PsychoticBreakCanTrigger)}");
                if (!DreadRuntimeState.PsychoticBreakCanTrigger
                    && !string.IsNullOrEmpty(DreadRuntimeState.PsychoticBreakBlockReason))
                {
                    _lines.Add($"  reason: {DreadRuntimeState.PsychoticBreakBlockReason}");
                }

                _lines.Add($"  next check: {DreadRuntimeState.PsychoticBreakNextCheckIn:F1}s");
                _lines.Add($"  threat memory: {DreadRuntimeState.PsychoticBreakThreatCount}");
            }

            _lines.Add($"  clips loaded: {OnOff(DreadRuntimeState.PsychoticBreakClipsLoaded)}");

            _lines.Add("");
            _lines.Add("AudioDread");
            _lines.Add($"  clips: {DreadRuntimeState.AudioClipCount}/4");
            if (DreadRuntimeState.AudioNextPlayIn >= 0f)
                _lines.Add($"  next play: {DreadRuntimeState.AudioNextPlayIn:F0}s");
            else
                _lines.Add("  next play: n/a");

            _lines.Add("");
            _lines.Add("Config");
            _lines.Add($"  CompatibilityMode: {OnOff(DreadConfig.CompatibilityMode.Value)}");
            _lines.Add($"  MonsterAggression: {OnOff(DreadConfig.MonsterAggressionEnabled.Value)}");
            _lines.Add($"  MonsterAudio: {OnOff(DreadConfig.MonsterAudioEnabled.Value)}");
            _lines.Add($"  AudioEnabled: {OnOff(DreadConfig.AudioEnabled.Value)}");
            _lines.Add($"  DebugServer: {OnOff(DreadConfig.DebugServerEnabled.Value)}");

            _lines.Add("");
            _lines.Add($"Harmony patches (Dread): {DreadRuntimeState.DreadPatchCount}");
        }

        private static string OnOff(bool value) => value ? "ON" : "off";

        private static int CountDreadPatches()
        {
            var harmony = Dread.Plugin.HarmonyInstance;
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

                    if (info.Prefixes?.Any(p => p.owner == Dread.Plugin.GUID) == true) count++;
                    if (info.Postfixes?.Any(p => p.owner == Dread.Plugin.GUID) == true) count++;
                    if (info.Transpilers?.Any(p => p.owner == Dread.Plugin.GUID) == true) count++;
                    if (info.Finalizers?.Any(p => p.owner == Dread.Plugin.GUID) == true) count++;
                }
            }
            catch
            {
                return -1;
            }

            return count;
        }
    }
}
