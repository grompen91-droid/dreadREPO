using System;
using System.Collections.Generic;
using System.Linq;
using Dread.Config;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem
    {
        private readonly List<RowData> _rows = new(16);

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
            BuildRows();

            const float width = 320f;
            const float padX = 10f;
            const float padTop = 6f;
            const float padBottom = 10f;
            const float lineH = 18f;
            const float labelW = 82f;
            const float marginX = 12f;
            const float marginY = 140f; // moved down from the top so it clears the game's top HUD
            float height = padTop + padBottom + _rows.Count * lineH;

            var panel = new Rect(marginX, marginY, width, height);
            GUI.Box(panel, EmptyContent, _boxStyle!);

            // Solid steel accent rail down the left edge (S2 "Slate HUD" look).
            GUI.Box(new Rect(panel.x, panel.y, 3f, height), EmptyContent, _railStyle!);

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
                    GUI.Box(new Rect(x, y + lineH * 0.5f - 0.5f, 9f, 1f), EmptyContent, _railStyle!);
                    GUI.Label(new Rect(x + 15f, y, innerW - 15f, lineH), row.Left, _sectionStyle!);
                }
                else if (row.Kind == RowHeader)
                {
                    GUI.Label(new Rect(x, y, innerW, lineH), row.Left, _headerStyle!);
                    GUI.Label(new Rect(x + innerW - 36f, y + 2f, 36f, lineH), row.Right, _hintStyle!);
                }
                else
                {
                    GUI.Label(new Rect(x, y, labelW, lineH), row.Left, _labelStyle!);
                    GUIStyle valueStyle = _valueStyle;
                    valueStyle.normal.textColor = row.Color;
                    GUI.Label(new Rect(x + labelW, y, innerW - labelW, lineH), row.Right, valueStyle);
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
