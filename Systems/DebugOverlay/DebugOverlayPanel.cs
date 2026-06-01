using System;
using System.Collections.Generic;
using System.Linq;
using Dread.Config;
using Dread.Systems.UI;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem
    {
        private readonly List<RowData> _rows = new(16);

        // Panel zoom (whole panel scales). Adjusted by the footer +/- buttons.
        private float _zoom = 1f;

        // Panel position (top-left). Draggable by the header in interactive mode.
        private float _panelX = 12f;
        private float _panelY = 140f;
        private bool _dragging;
        private float _dragOffsetX;
        private float _dragOffsetY;

        // Component-kit demo (toggled by the footer "Kit" button) so the loading
        // bar, slider, and toast widgets can be verified in-game.
        private bool _kitDemo;
        private float _kitSlider = 0.5f;

        // Cached empty content. Avoids GUIContent.none, which the build resolves
        // against a stub property getter (get_none) that does not exist in the
        // game's real UnityEngine, throwing MissingMethodException in OnGUI.
        private static readonly GUIContent EmptyContent = new();

        private void OnGUI()
        {
            if (!GuardOverlayEnabled())
                return;

            if (!IsOverlayVisible())
                return;

            EnsureStyles();
            ApplyZoom();
            BuildRows();

            float z = _zoom;
            float width = 320f * z;
            float padX = 10f * z;
            float padTop = 6f * z;
            float padBottom = 10f * z;
            // Row height must clear the largest font's line box (header is 15pt)
            // or glyph descenders get clipped at the bottom of each row.
            float lineH = 22f * z;
            float labelW = 82f * z;
            float btnH = 20f * z;
            float footerH = btnH + 8f * z;
            float height = padTop + padBottom + _rows.Count * lineH + footerH;

            var panel = new Rect(_panelX, _panelY, width, height);
            GUI.Box(panel, EmptyContent, _boxStyle!);

            // Thin steel accent rail down the left edge (S2 "Slate HUD" look).
            GUI.Box(new Rect(panel.x, panel.y, 2f * z, height), EmptyContent, _railStyle!);

            // Solid steel cap across the top edge so the panel reads as framed.
            GUI.Box(new Rect(panel.x, panel.y, width, 2f * z), EmptyContent, _railStyle!);

            float x = panel.x + padX;
            float y = panel.y + padTop;
            float innerW = width - padX * 2f;

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Kind == RowSep)
                {
                    GUI.Box(new Rect(x, y + lineH * 0.5f - 1f, innerW, 1f), EmptyContent, _sepStyle!);
                }
                else if (row.Kind == RowSection)
                {
                    // Short steel tick, then the uppercase section label.
                    GUI.Box(new Rect(x, y + lineH * 0.5f - 0.5f, 9f * z, 1f), EmptyContent, _railStyle!);
                    GUI.Label(new Rect(x + 15f * z, y, innerW - 15f * z, lineH), row.Left, _sectionStyle!);
                }
                else if (row.Kind == RowHeader)
                {
                    GUI.Label(new Rect(x, y, innerW, lineH), row.Left, _headerStyle!);
                    GUI.Label(new Rect(x + innerW - 70f * z, y + 3f * z, 70f * z, lineH), row.Right, _hintStyle!);
                }
                else
                {
                    GUI.Label(new Rect(x, y, labelW, lineH), row.Left, _labelStyle!);
                    GUIStyle valueStyle = _valueStyle!;
                    valueStyle.normal.textColor = row.Color;
                    GUI.Label(new Rect(x + labelW, y, innerW - labelW, lineH), row.Right, valueStyle);
                }

                y += lineH;
            }

            DrawZoomFooter(x, panel.y + height - padBottom - btnH, innerW, btnH, z);

            if (_kitDemo)
                DrawKitDemo(_panelX, panel.y + height + 8f * z, width, z);
        }

        private void DrawZoomFooter(float x, float y, float innerW, float btnH, float z)
        {
            float gap = 4f * z;
            float bw = 22f * z;

            GUI.Label(new Rect(x, y, 36f * z, btnH), "ZOOM", _sectionStyle!);
            float cx = x + 38f * z;

            if (GUI.Button(new Rect(cx, y, bw, btnH), "-", _buttonStyle!))
                SetZoom(_zoom - 0.1f);
            cx += bw + gap;

            if (GUI.Button(new Rect(cx, y, bw, btnH), "+", _buttonStyle!))
                SetZoom(_zoom + 0.1f);
            cx += bw + gap;

            int pct = (int)(_zoom * 100f + 0.5f);
            GUI.Label(new Rect(cx, y, 38f * z, btnH), pct + "%", _labelStyle!);

            float resetW = 50f * z;
            if (GUI.Button(new Rect(x + innerW - resetW, y, resetW, btnH), "Reset", _buttonStyle!))
                SetZoom(1f);

            float kitW = 36f * z;
            if (GUI.Button(new Rect(x + innerW - resetW - kitW - gap, y, kitW, btnH), "Kit", _buttonStyle!))
                _kitDemo = !_kitDemo;
        }

        // Demo block below the panel: exercises the reusable loading bar, slider,
        // and notification toasts so they can be verified in-game.
        private void DrawKitDemo(float x0, float y0, float width, float z)
        {
            float padX = 10f * z;
            float padY = 8f * z;
            float lineH = 22f * z;
            float barH = 6f * z;
            float btnH = 20f * z;
            float gap = 6f * z;
            float rail = 3f * z;

            float totalH = padY * 2f + lineH * 3f + btnH + gap * 3f;
            var box = new Rect(x0, y0, width, totalH);
            GUI.Box(box, EmptyContent, _boxStyle!);
            GUI.Box(new Rect(box.x, box.y, rail, totalH), EmptyContent, _railStyle!);

            float ix = x0 + padX + rail;
            float innerW = width - padX * 2f - rail;
            float labelW = 44f * z;
            float y = y0 + padY;

            GUI.Box(new Rect(ix, y + lineH * 0.5f - 0.5f, 9f * z, 1f), EmptyContent, _railStyle!);
            GUI.Label(new Rect(ix + 15f * z, y, innerW - 15f * z, lineH), "COMPONENT KIT", _sectionStyle!);
            y += lineH + gap;

            // Loading bar (animated sawtooth so it visibly moves).
            GUI.Label(new Rect(ix, y, labelW, lineH), "Load", _labelStyle!);
            float t = Time.realtimeSinceStartup * 0.4f;
            float prog = t - (int)t;
            DreadWidgets.LoadingBar(
                new Rect(ix + labelW, y + (lineH - barH) * 0.5f, innerW - labelW, barH), prog);
            y += lineH + gap;

            // Value slider with a live percent readout.
            GUI.Label(new Rect(ix, y, labelW, lineH), "Vol", _labelStyle!);
            float valW = 40f * z;
            _kitSlider = DreadWidgets.Slider(
                new Rect(ix + labelW, y, innerW - labelW - valW, lineH), _kitSlider, 0f, 1f);
            int sp = (int)(_kitSlider * 100f + 0.5f);
            GUI.Label(new Rect(ix + innerW - valW, y, valW, lineH), sp + "%", _valueStyle!);
            y += lineH + gap;

            // Toast triggers, one per severity.
            float bw = (innerW - gap * 2f) / 3f;
            if (GUI.Button(new Rect(ix, y, bw, btnH), "Info", _buttonStyle!))
                DreadNotificationSystem.Info("Test toast", "Informational notification from the kit demo.");
            if (GUI.Button(new Rect(ix + bw + gap, y, bw, btnH), "Warn", _buttonStyle!))
                DreadNotificationSystem.Warn("Test toast", "Warning notification from the kit demo.");
            if (GUI.Button(new Rect(ix + (bw + gap) * 2f, y, bw, btnH), "Bad", _buttonStyle!))
                DreadNotificationSystem.Bad("Test toast", "Error notification from the kit demo.");
        }

        private void SetZoom(float value)
        {
            _zoom = Mathf.Clamp(value, 0.6f, 1.6f);
            SaveLayout();
        }

        // Restore the panel's saved zoom and position so they survive a relaunch.
        // Called from Awake, after DreadConfig is initialized.
        private void LoadLayout()
        {
            _zoom = Mathf.Clamp(DreadConfig.DebugOverlayZoom.Value, 0.6f, 1.6f);
            _panelX = DreadConfig.DebugOverlayPanelX.Value;
            _panelY = DreadConfig.DebugOverlayPanelY.Value;
        }

        // Persist the current zoom and position. Called when the user changes zoom
        // (footer buttons) or finishes a drag, not every frame, to avoid disk churn.
        private void SaveLayout()
        {
            DreadConfig.DebugOverlayZoom.Value = _zoom;
            DreadConfig.DebugOverlayPanelX.Value = _panelX;
            DreadConfig.DebugOverlayPanelY.Value = _panelY;
            DreadConfig.SaveToDisk();
        }

        // Drag the panel by its header strip. Runs only in interactive (F9) mode.
        // Input.mousePosition has a bottom-left origin; GUI coordinates are
        // top-left, so the Y axis is flipped.
        private void HandleDrag()
        {
            float z = _zoom;
            float width = 320f * z;
            float headerH = (6f + 22f) * z; // padTop + header row = the grab strip

            float mx = Input.mousePosition.x;
            float my = Screen.height - Input.mousePosition.y;

            if (!_dragging
                && Input.GetMouseButtonDown(0)
                && mx >= _panelX && mx <= _panelX + width
                && my >= _panelY && my <= _panelY + headerH)
            {
                _dragging = true;
                _dragOffsetX = mx - _panelX;
                _dragOffsetY = my - _panelY;
            }

            if (!_dragging)
                return;

            if (Input.GetMouseButton(0))
            {
                _panelX = mx - _dragOffsetX;
                _panelY = my - _dragOffsetY;
                ClampPanel(width);
            }
            else
            {
                _dragging = false;
                SaveLayout();
            }
        }

        // Keep enough of the panel on screen that the header stays grabbable.
        private void ClampPanel(float width)
        {
            float minX = -(width - 60f);
            float maxX = Screen.width - 60f;
            float maxY = Screen.height - 30f;

            if (_panelX < minX) _panelX = minX;
            if (_panelX > maxX) _panelX = maxX;
            if (_panelY < 0f) _panelY = 0f;
            if (_panelY > maxY) _panelY = maxY;
        }

        private void ApplyZoom()
        {
            _headerStyle!.fontSize = Scaled(15);
            _hintStyle!.fontSize = Scaled(11);
            _labelStyle!.fontSize = Scaled(13);
            _valueStyle!.fontSize = Scaled(13);
            _sectionStyle!.fontSize = Scaled(11);
            _buttonStyle!.fontSize = Scaled(11);
        }

        private int Scaled(int baseSize)
        {
            int v = (int)(baseSize * _zoom + 0.5f);
            return v < 1 ? 1 : v;
        }

        private void BuildRows()
        {
            _rows.Clear();

            AddHeader("DREAD " + Dread.Plugin.VERSION, _interactive ? "mouse on" : "F9 mouse");
            AddSep();

            // Performance
            AddSection("Performance");
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            float ms = _smoothedDelta * 1000f;
            Color fpsCol = fps >= 60f ? ColGood : fps >= 30f ? ColWarn : ColBad;
            AddRow("FPS", $"{fps:F0}", fpsCol);
            AddRow("Frame", $"{ms:F1} ms   min {_minFps:F0} fps", ColValue);
            AddRow("Memory", $"{_memMB:F0} MB", ColValue);
            AddRow("GC", $"g0 {GC.CollectionCount(0)}  g1 {GC.CollectionCount(1)}  g2 {GC.CollectionCount(2)}", ColDim);
            AddRow("Screen", $"{Screen.width}x{Screen.height}", ColDim);
            AddRow("Frames", Time.frameCount.ToString(), ColDim);

            // Mod state
            AddSection("Mod State");
            AddRow("Phase", PhaseSummary(), PhaseColor());
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
                + $"enemies {DreadRuntimeState.PsychoticBreakEnemyCount}   "
                + $"threat {DreadRuntimeState.PsychoticBreakThreatCount}s   "
                + $"next {DreadRuntimeState.PsychoticBreakNextCheckIn:F0}s",
                ColDim);

            AddRow("Audio", AudioSummary(), ColValue);
            AddRow("Lure", LureSummary(), LureColor());
            AddRow("Snitch", SnitchSummary(), SnitchColor());

            AddSection("System");
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

        private void AddSection(string label)
            => _rows.Add(new RowData { Left = label.ToUpperInvariant(), Right = string.Empty, Kind = RowSection });

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
                + $"threat {DreadRuntimeState.PsychoticBreakThreatCount}s";
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

        private static string PhaseSummary()
        {
            var phase = DreadRuntimeState.GameplayPhase;
            return string.IsNullOrEmpty(phase) ? "unknown" : phase;
        }

        private static Color PhaseColor()
        {
            return DreadRuntimeState.GameplayPhase switch
            {
                "run" => ColGood,
                "menu" => ColDim,
                "truck/shop" => ColWarn,
                _ => ColDim,
            };
        }

        private static string LureSummary()
        {
            if (!string.IsNullOrEmpty(DreadRuntimeState.LureBlockReason))
                return $"blocked: {DreadRuntimeState.LureBlockReason}";

            if (DreadRuntimeState.LureTarget.Length == 0)
            {
                if (DreadRuntimeState.LureCooldownRemaining > 0f)
                    return $"cooldown {DreadRuntimeState.LureCooldownRemaining:F0}s";
                return "idle";
            }

            var cd = DreadRuntimeState.LureCooldownRemaining > 0f
                ? $"  cd {DreadRuntimeState.LureCooldownRemaining:F0}s"
                : "";
            return $"{DreadRuntimeState.LureTarget}  step {DreadRuntimeState.LurePullStep}  "
                + $"camp {DreadRuntimeState.LureCampTimer:F0}s{cd}";
        }

        private static Color LureColor()
        {
            if (!string.IsNullOrEmpty(DreadRuntimeState.LureBlockReason))
                return ColDim;
            return DreadRuntimeState.LureTarget.Length == 0 ? ColDim : ColWarn;
        }

        private static string SnitchSummary()
        {
            if (!DreadRuntimeState.SnitchEnabled)
                return "off";

            if (DreadRuntimeState.SnitchState == "triggered")
                return $"triggered  POI {DreadRuntimeState.SnitchPoiRemaining:F0}s";

            if (DreadRuntimeState.SnitchState == "armed")
            {
                if (DreadRuntimeState.SnitchItemDistance >= 0f)
                    return $"armed  {DreadRuntimeState.SnitchItemDistance:F1}m";
                return "armed";
            }

            if (DreadRuntimeState.SnitchState == "failed")
                return "failed (no items)";

            if (!string.IsNullOrEmpty(DreadRuntimeState.SnitchBlockReason))
                return $"blocked: {DreadRuntimeState.SnitchBlockReason}";

            if (DreadRuntimeState.SnitchNextCheckIn > 0f)
                return $"arming  {DreadRuntimeState.SnitchNextCheckIn:F1}s";

            return DreadRuntimeState.SnitchState;
        }

        private static Color SnitchColor()
        {
            if (!DreadRuntimeState.SnitchEnabled)
                return ColDim;
            if (DreadRuntimeState.SnitchState == "triggered")
                return ColBad;
            if (DreadRuntimeState.SnitchState == "armed")
                return ColWarn;
            if (!string.IsNullOrEmpty(DreadRuntimeState.SnitchBlockReason))
                return ColWarn;
            return ColGood;
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
