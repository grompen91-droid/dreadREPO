using System;
using System.Collections.Generic;
using System.Linq;
using Dread.Config;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    public class DebugOverlaySystem : MonoBehaviour
    {
        private const byte RowNormal = 0;
        private const byte RowHeader = 1;
        private const byte RowSep = 2;

        private bool _visible;
        private bool _loggedDisabledWhileRunning;

        // Performance sampling.
        private float _smoothedDelta;
        private float _minFps;
        private float _minFpsResetAt;
        private float _memMB;
        private float _nextStatRefresh;

        private Texture2D? _bgTex;
        private Texture2D? _sepTex;
        private GUIStyle? _boxStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _hintStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _valueStyle;
        private GUIStyle? _sepStyle;
        private readonly List<RowData> _rows = new(16);

        // Cached empty content. Avoids GUIContent.none, which the build resolves
        // against a stub property getter (get_none) that does not exist in the
        // game's real UnityEngine, throwing MissingMethodException in OnGUI.
        private static readonly GUIContent EmptyContent = new();

        private static readonly Color ColAccent = new(0.96f, 0.55f, 0.38f);
        private static readonly Color ColDim = new(0.62f, 0.64f, 0.70f);
        private static readonly Color ColValue = new(0.92f, 0.93f, 0.96f);
        private static readonly Color ColGood = new(0.48f, 0.90f, 0.55f);
        private static readonly Color ColWarn = new(0.97f, 0.84f, 0.42f);
        private static readonly Color ColBad = new(0.96f, 0.46f, 0.46f);

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

            SampleFrameStats();

            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;

            if (!IsOverlayVisible())
                return;

            if (Time.realtimeSinceStartup >= _nextStatRefresh)
            {
                _nextStatRefresh = Time.realtimeSinceStartup + 0.5f;
                DreadRuntimeState.DreadPatchCount = CountDreadPatches();
                _memMB = GC.GetTotalMemory(false) / (1024f * 1024f);
            }
        }

        // Runs every frame (even while toggled off) so FPS is accurate the instant the HUD is shown.
        private void SampleFrameStats()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            _smoothedDelta = _smoothedDelta <= 0f ? dt : _smoothedDelta + (dt - _smoothedDelta) * 0.1f;

            float fps = 1f / dt;
            float now = Time.realtimeSinceStartup;
            if (now >= _minFpsResetAt)
            {
                _minFps = fps;
                _minFpsResetAt = now + 2f;
            }
            else if (fps < _minFps)
            {
                _minFps = fps;
            }
        }

        private void OnGUI()
        {
            if (!GuardOverlayEnabled())
                return;

            if (!IsOverlayVisible())
                return;

            EnsureStyles();
            BuildRows();

            const float width = 320f;
            const float pad = 10f;
            const float lineH = 18f;
            const float labelW = 82f;
            const float marginX = 12f;
            const float marginY = 140f; // moved down from the top so it clears the game's top HUD
            float height = pad * 2f + _rows.Count * lineH;

            var panel = new Rect(marginX, marginY, width, height);
            GUI.Box(panel, EmptyContent, _boxStyle!);

            float x = panel.x + pad;
            float y = panel.y + pad;
            float innerW = width - pad * 2f;

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Kind == RowSep)
                {
                    GUI.Box(new Rect(x, y + lineH * 0.5f - 1f, innerW, 1f), EmptyContent, _sepStyle!);
                }
                else if (row.Kind == RowHeader)
                {
                    GUI.Label(new Rect(x, y, innerW, lineH), row.Left, _headerStyle!);
                    GUI.Label(new Rect(x + innerW - 36f, y + 2f, 36f, lineH), row.Right, _hintStyle!);
                }
                else
                {
                    GUI.Label(new Rect(x, y, labelW, lineH), row.Left, _labelStyle!);
                    _valueStyle!.normal.textColor = row.Color;
                    GUI.Label(new Rect(x + labelW, y, innerW - labelW, lineH), row.Right, _valueStyle!);
                }

                y += lineH;
            }
        }

        private void BuildRows()
        {
            _rows.Clear();

            AddHeader("DREAD " + Dread.Plugin.VERSION, "F10");
            AddSep();

            // Performance
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            float ms = _smoothedDelta * 1000f;
            Color fpsCol = fps >= 60f ? ColGood : fps >= 30f ? ColWarn : ColBad;
            AddRow("FPS", $"{fps:F0}", fpsCol);
            AddRow("Frame", $"{ms:F1} ms   min {_minFps:F0} fps", ColValue);
            AddRow("Memory", $"{_memMB:F0} MB", ColValue);
            AddRow("GC", $"g0 {GC.CollectionCount(0)}  g1 {GC.CollectionCount(1)}  g2 {GC.CollectionCount(2)}", ColDim);
            AddRow("Screen", $"{Screen.width}x{Screen.height}", ColDim);
            AddRow("Frames", Time.frameCount.ToString(), ColDim);
            AddSep();

            // Mod state
            float nearest = DreadRuntimeState.NearestEnemyDist;
            string enemy = nearest >= float.MaxValue * 0.5f ? "none" : $"{nearest:F1} m  (range 15m)";
            AddRow("Enemy", enemy, ColValue);

            AddRow("Tension", $"adrenaline {OnOff(DreadRuntimeState.AdrenalineActive)}", ColValue);
            AddRow("Sprint",
                $"panic {OnOff(DreadRuntimeState.PanicSprintActive)}   cd {DreadRuntimeState.PanicSprintCooldown:F0}s",
                ColValue);

            AddRow("Break", BreakSummary(), BreakColor());
            AddRow("Break+",
                $"clips {OnOff(DreadRuntimeState.PsychoticBreakClipsLoaded)}   "
                + $"threat {DreadRuntimeState.PsychoticBreakThreatCount}   "
                + $"next {DreadRuntimeState.PsychoticBreakNextCheckIn:F0}s",
                ColDim);

            AddRow("Audio", AudioSummary(), ColValue);

            AddSep();
            AddRow("Config",
                $"compat{PlusMinus(DreadConfig.CompatibilityMode.Value)} "
                + $"aggr{PlusMinus(DreadConfig.MonsterAggressionEnabled.Value)} "
                + $"maud{PlusMinus(DreadConfig.MonsterAudioEnabled.Value)} "
                + $"aud{PlusMinus(DreadConfig.AudioEnabled.Value)} "
                + $"srv{PlusMinus(DreadConfig.DebugServerEnabled.Value)}",
                ColDim);

            AddRow("Patches", DreadRuntimeState.DreadPatchCount.ToString(), ColValue);
        }

        private void AddRow(string left, string right, Color color)
            => _rows.Add(new RowData { Left = left, Right = right, Color = color, Kind = RowNormal });

        private void AddHeader(string left, string right)
            => _rows.Add(new RowData { Left = left, Right = right, Kind = RowHeader });

        private void AddSep()
            => _rows.Add(new RowData { Left = string.Empty, Right = string.Empty, Kind = RowSep });

        private static string BreakSummary()
        {
            if (!DreadRuntimeState.PsychoticBreakEnabled)
                return "off";

            if (DreadRuntimeState.PsychoticBreakEpisodeActive)
            {
                float remaining = DreadRuntimeState.PsychoticBreakEpisodeDuration
                    - DreadRuntimeState.PsychoticBreakEpisodeTimer;
                return $"ACTIVE {remaining:F1}s";
            }

            if (!DreadRuntimeState.PsychoticBreakCanTrigger
                && !string.IsNullOrEmpty(DreadRuntimeState.PsychoticBreakBlockReason))
            {
                return $"blocked: {DreadRuntimeState.PsychoticBreakBlockReason}";
            }

            return $"ready  next {DreadRuntimeState.PsychoticBreakNextCheckIn:F0}s  "
                + $"threat {DreadRuntimeState.PsychoticBreakThreatCount}";
        }

        private static Color BreakColor()
        {
            if (!DreadRuntimeState.PsychoticBreakEnabled)
                return ColDim;
            if (DreadRuntimeState.PsychoticBreakEpisodeActive)
                return ColBad;
            if (!DreadRuntimeState.PsychoticBreakCanTrigger)
                return ColWarn;
            return ColGood;
        }

        private static string AudioSummary()
        {
            string next = DreadRuntimeState.AudioNextPlayIn >= 0f
                ? $"next {DreadRuntimeState.AudioNextPlayIn:F0}s"
                : "next n/a";
            return $"{DreadRuntimeState.AudioClipCount}/4  {next}";
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

            _bgTex = MakeTexture(new Color(0.05f, 0.05f, 0.07f, 0.88f));
            _sepTex = MakeTexture(new Color(0.45f, 0.47f, 0.52f, 0.5f));

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _bgTex;

            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = false };
            _headerStyle.normal.textColor = ColAccent;

            _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false };
            _hintStyle.normal.textColor = ColDim;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            _labelStyle.normal.textColor = ColDim;

            _valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            _valueStyle.normal.textColor = ColValue;

            _sepStyle = new GUIStyle(GUI.skin.box);
            _sepStyle.normal.background = _sepTex;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static string OnOff(bool value) => value ? "ON" : "off";

        private static string PlusMinus(bool value) => value ? "+" : "-";

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

        private struct RowData
        {
            public string Left;
            public string Right;
            public Color Color;
            public byte Kind;
        }
    }
}
