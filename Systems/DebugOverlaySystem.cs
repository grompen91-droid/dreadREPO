using System;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Toggle + state for the debug HUD. IMGUI drawing runs only while visible (see <see cref="DebugOverlayGuiRenderer"/>).
    /// </summary>
    public class DebugOverlaySystem : MonoBehaviour
    {
        private const float ScreenMargin = 12f;
        private const float PanelPadding = 10f;
        private const float HeaderHeight = 26f;

        internal bool Visible => _visible;
        internal bool DrawFailed => _drawFailed;

        private bool _visible;
        private bool _drawFailed;
        private bool _drawFailureLogged;
        private bool _drawSuccessLogged;
        private KeyCode _toggleKey = KeyCode.F10;
        private int _lastToggleFrame = -1;
        private DebugOverlayGuiRenderer? _guiRenderer;
        private OverlayStyleCache? _styles;
        private int _styleSignature;
        private readonly List<OverlayLine> _lines = new(32);
        private float _nextLinesRefresh;
        private const float ContentLineHeight = 22f;
        private const float ContentLineSpacing = 3f;

        private void Start()
        {
            RefreshToggleKey();
            ApplyConfigEnabled();
            SubscribeConfig();
            this.enabled = false;
            DebugOverlayTogglePoll.Register(this);

            // #region agent log
            DebugAgentLog.Write(
                "J",
                "DebugOverlaySystem.cs:Start",
                "overlay_start",
                "post-fix",
                ("configEnabled", DreadConfig.DebugOverlayEnabled.Value),
                ("behaviourEnabled", enabled),
                ("visible", _visible),
                ("hasGuiRenderer", _guiRenderer != null),
                ("drawFailed", _drawFailed),
                ("toggleKey", DreadConfig.DebugOverlayToggleKey.Value));
            // #endregion
        }

        private void OnDestroy()
        {
            DebugOverlayTogglePoll.Unregister(this);
            UnsubscribeConfig();
            DestroyGuiRenderer();
        }

        private void SubscribeConfig()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged += OnOverlayConfigChanged;
            DreadConfig.DebugOverlayScreenAnchor.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayOffsetX.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayOffsetY.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayPanelWidth.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayFontSize.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayBackgroundAlpha.SettingChanged += OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayToggleKey.SettingChanged += OnToggleKeyChanged;
        }

        private void UnsubscribeConfig()
        {
            DreadConfig.DebugOverlayEnabled.SettingChanged -= OnOverlayConfigChanged;
            DreadConfig.DebugOverlayScreenAnchor.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayOffsetX.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayOffsetY.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayPanelWidth.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayFontSize.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayBackgroundAlpha.SettingChanged -= OnOverlayAppearanceChanged;
            DreadConfig.DebugOverlayToggleKey.SettingChanged -= OnToggleKeyChanged;
        }

        private void OnOverlayConfigChanged(object? sender, EventArgs e)
        {
            ApplyConfigEnabled();
            if (!DreadConfig.DebugOverlayEnabled.Value)
                HideOverlay();
        }

        private void OnToggleKeyChanged(object? sender, EventArgs e) => RefreshToggleKey();

        private void RefreshToggleKey()
        {
            _toggleKey = DebugOverlayInput.ParseKeyCode(
                DreadConfig.DebugOverlayToggleKey.Value,
                KeyCode.F10);
        }

        private void HideOverlay()
        {
            _visible = false;
            this.enabled = false;
            DestroyGuiRenderer();
        }

        private void ShowOverlay()
        {
            if (!DreadConfig.DebugOverlayEnabled.Value)
                return;

            _visible = true;
            _drawFailed = false;
            _drawSuccessLogged = false;
            EnsureGuiRenderer();
            this.enabled = true;
        }

        private void EnsureGuiRenderer()
        {
            if (_guiRenderer != null)
                return;

            _guiRenderer = gameObject.AddComponent<DebugOverlayGuiRenderer>();
            _guiRenderer.Bind(this);
        }

        private void DestroyGuiRenderer()
        {
            if (_guiRenderer != null)
            {
                Destroy(_guiRenderer);
                _guiRenderer = null;
            }

            _styles?.Dispose();
            _styles = null;
            _styleSignature = 0;
        }

        private void OnOverlayAppearanceChanged(object? sender, EventArgs e)
        {
            _styles?.Dispose();
            _styles = null;
            _styleSignature = 0;
        }

        private void ApplyConfigEnabled()
        {
            if (!DreadConfig.DebugOverlayEnabled.Value)
                HideOverlay();
        }

        internal void ForceHide() => HideOverlay();

        internal void PollToggleFromExternal() => TryToggleVisibility();

        private void Update()
        {
            if (!DreadConfig.DebugOverlayEnabled.Value)
            {
                HideOverlay();
                return;
            }

            if (!_visible)
                this.enabled = false;
        }

        internal void DrawOverlayGui()
        {
            if (!DreadConfig.DebugOverlayEnabled.Value || SemiFunc.MenuLevel() || !_visible || _drawFailed)
                return;

            var evt = Event.current;
            // Repaint = 7 in Unity EventType (avoid stub/build mismatch on enum name).
            if (evt != null && (int)evt.type != 7)
                return;

            if (ConfigUiDetector.IsConfigurationManagerOpen())
                return;

            var prevColor = GUI.color;
            try
            {
                var styles = EnsureStyles();
                if (Time.time >= _nextLinesRefresh)
                {
                    BuildLines();
                    _nextLinesRefresh = Time.time + 0.5f;
                }

                float contentHeight = MeasureContentHeight(styles);
                float panelWidth = DreadConfig.DebugOverlayPanelWidth.Value;
                float panelHeight = HeaderHeight + PanelPadding * 2f + contentHeight;
                ComputePanelPosition(panelWidth, panelHeight, out float px, out float py);

                DrawPanelBackground(px, py, panelWidth, panelHeight, styles);
                DrawHeader(px, py, panelWidth, styles);
                DrawContent(px, py, panelWidth, styles);

                if (!_drawSuccessLogged)
                {
                    _drawSuccessLogged = true;
                    // #region agent log
                    DebugAgentLog.Write(
                        "C",
                        "DebugOverlaySystem.cs:DrawOverlayGui",
                        "draw_ok",
                        "post-fix",
                        ("lineCount", _lines.Count),
                        ("frame", Time.frameCount));
                    // #endregion
                }
            }
            catch (Exception ex)
            {
                _drawFailed = true;
                HideOverlay();
                if (!_drawFailureLogged)
                {
                    _drawFailureLogged = true;
                    // #region agent log
                    DebugAgentLog.Write(
                        "C",
                        "DebugOverlaySystem.cs:DrawOverlayGui",
                        "draw_failed",
                        "post-fix",
                        ("error", ex.GetType().Name),
                        ("message", ex.Message));
                    // #endregion
                    LoggingService.LogError(
                        $"[DebugOverlay] Draw failed: {ex.Message}");
                }
            }
            finally
            {
                GUI.color = prevColor;
            }
        }

        private void TryToggleVisibility()
        {
            if (Time.frameCount == _lastToggleFrame)
                return;

            var keyName = DreadConfig.DebugOverlayToggleKey.Value;
            if (!DebugOverlayInput.WasTogglePressedThisFrame(_toggleKey, keyName))
                return;

            _lastToggleFrame = Time.frameCount;
            if (_visible)
                HideOverlay();
            else
                ShowOverlay();

            // #region agent log
            DebugAgentLog.Write(
                "E",
                "DebugOverlaySystem.cs:TryToggleVisibility",
                "toggle",
                "post-fix",
                ("visible", _visible),
                ("keyName", keyName),
                ("behaviourEnabled", enabled),
                ("hasGuiRenderer", _guiRenderer != null),
                ("frame", Time.frameCount));
            // #endregion
            LoggingService.LogInfo($"[DebugOverlay] {(_visible ? "Shown" : "Hidden")} (toggle {keyName})");
        }

        private OverlayStyleCache EnsureStyles()
        {
            int sig = unchecked(
                ((int)DreadConfig.DebugOverlayPanelWidth.Value * 397)
                ^ (DreadConfig.DebugOverlayFontSize.Value * 397)
                ^ (int)(DreadConfig.DebugOverlayBackgroundAlpha.Value * 1000f));

            if (_styles != null && _styleSignature == sig)
                return _styles;

            _styles?.Dispose();
            _styles = OverlayStyleCache.Create(
                DreadConfig.DebugOverlayBackgroundAlpha.Value,
                DreadConfig.DebugOverlayFontSize.Value);
            _styleSignature = sig;
            return _styles;
        }

        private static void ComputePanelPosition(float width, float height, out float x, out float y)
        {
            x = ScreenMargin + DreadConfig.DebugOverlayOffsetX.Value;
            y = ScreenMargin + DreadConfig.DebugOverlayOffsetY.Value;

            switch (DreadConfig.DebugOverlayScreenAnchor.Value?.Trim())
            {
                case "TopRight":
                    x = Screen.width - width - ScreenMargin + DreadConfig.DebugOverlayOffsetX.Value;
                    break;
                case "BottomLeft":
                    y = Screen.height - height - ScreenMargin + DreadConfig.DebugOverlayOffsetY.Value;
                    break;
                case "BottomRight":
                    x = Screen.width - width - ScreenMargin + DreadConfig.DebugOverlayOffsetX.Value;
                    y = Screen.height - height - ScreenMargin + DreadConfig.DebugOverlayOffsetY.Value;
                    break;
            }
        }

        private static void DrawPanelBackground(float x, float y, float w, float h, OverlayStyleCache styles)
        {
            GUI.Box(new Rect(x, y, w, h), ImGuiContentCache.Empty, styles.Panel);
        }

        private void DrawHeader(float panelX, float panelY, float panelW, OverlayStyleCache styles)
        {
            float headerX = panelX + 1f;
            float headerY = panelY + 1f;
            float headerW = panelW - 2f;
            GUI.Box(new Rect(headerX, headerY, headerW, HeaderHeight), ImGuiContentCache.Empty, styles.HeaderBar);

            float titleX = headerX + PanelPadding;
            float titleY = headerY + 4f;
            float titleW = headerW - PanelPadding * 2f;
            GUI.Label(new Rect(titleX, titleY, titleW, 22f), $"DREAD  v{Dread.Plugin.VERSION}", styles.Title);

            var hint = $"{DreadConfig.DebugOverlayToggleKey.Value} hide";
            var hintSize = styles.Muted.CalcSize(new GUIContent(hint));
            float hintX = headerX + headerW - hintSize.x - PanelPadding;
            GUI.Label(new Rect(hintX, titleY, hintSize.x, 22f), hint, styles.Muted);
        }

        private float MeasureContentHeight(OverlayStyleCache styles)
        {
            _ = styles;
            if (_lines.Count == 0)
                return ContentLineHeight;

            return _lines.Count * (ContentLineHeight + ContentLineSpacing);
        }

        private void DrawContent(float panelX, float panelY, float panelW, OverlayStyleCache styles)
        {
            float x = panelX + PanelPadding;
            float y = panelY + HeaderHeight + PanelPadding;
            float w = panelW - PanelPadding * 2f;

            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                var style = styles.ForKind(line.Kind);
                GUI.Label(new Rect(x, y, w, ContentLineHeight), line.Text, style);
                y += ContentLineHeight + ContentLineSpacing;
            }
        }

        private void BuildLines()
        {
            _lines.Clear();

            float nearest = DreadRuntimeState.NearestEnemyDist;
            string nearestText = nearest >= float.MaxValue * 0.5f ? "none" : $"{nearest:F1} m";

            Add(OverlayLineKind.Muted, "");
            Add(OverlayLineKind.Section, "Threat");
            Add(OverlayLineKind.Body, $"Nearest enemy     {nearestText}");

            Add(OverlayLineKind.Muted, "");
            Add(OverlayLineKind.Section, "Tension");
            Add(OverlayLineKind.Body, $"Adrenaline        {Status(DreadRuntimeState.AdrenalineActive)}");
            Add(OverlayLineKind.Body, $"Panic sprint      {Status(DreadRuntimeState.PanicSprintActive)}"
                + $"   cd {DreadRuntimeState.PanicSprintCooldown:F0}s");

            Add(OverlayLineKind.Muted, "");
            Add(OverlayLineKind.Section, "Psychotic break");
            Add(OverlayLineKind.Body, $"Enabled           {Status(DreadRuntimeState.PsychoticBreakEnabled)}");

            if (DreadRuntimeState.PsychoticBreakEpisodeActive)
            {
                float remaining = DreadRuntimeState.PsychoticBreakEpisodeDuration
                    - DreadRuntimeState.PsychoticBreakEpisodeTimer;
                Add(OverlayLineKind.Alert, $"Episode           ACTIVE  {remaining:F1}s left");
            }
            else
            {
                Add(OverlayLineKind.Body, $"Can trigger       {Status(DreadRuntimeState.PsychoticBreakCanTrigger)}");
                if (!DreadRuntimeState.PsychoticBreakCanTrigger
                    && !string.IsNullOrEmpty(DreadRuntimeState.PsychoticBreakBlockReason))
                {
                    Add(OverlayLineKind.Warn, $"Blocked           {DreadRuntimeState.PsychoticBreakBlockReason}");
                }

                Add(OverlayLineKind.Body, $"Next check        {DreadRuntimeState.PsychoticBreakNextCheckIn:F1}s");
                Add(OverlayLineKind.Body, $"Threat memory     {DreadRuntimeState.PsychoticBreakThreatCount}");
            }

            Add(OverlayLineKind.Body, $"Clips loaded      {Status(DreadRuntimeState.PsychoticBreakClipsLoaded)}");

            Add(OverlayLineKind.Muted, "");
            Add(OverlayLineKind.Section, "Audio");
            Add(OverlayLineKind.Body, $"Clips             {DreadRuntimeState.AudioClipCount}/4");
            if (DreadRuntimeState.AudioNextPlayIn >= 0f)
                Add(OverlayLineKind.Body, $"Next play         {DreadRuntimeState.AudioNextPlayIn:F0}s");
            else
                Add(OverlayLineKind.Body, "Next play         n/a");

            Add(OverlayLineKind.Muted, "");
            Add(OverlayLineKind.Section, "Config");
            Add(OverlayLineKind.Body, $"Compatibility     {Status(DreadConfig.CompatibilityMode.Value)}");
            Add(OverlayLineKind.Body, $"Monster aggro     {Status(DreadConfig.MonsterAggressionEnabled.Value)}");
            Add(OverlayLineKind.Body, $"Monster audio     {Status(DreadConfig.MonsterAudioEnabled.Value)}");
            Add(OverlayLineKind.Body, $"Ambient audio     {Status(DreadConfig.AudioEnabled.Value)}");
            Add(OverlayLineKind.Body, $"Debug server      {Status(DreadConfig.DebugServerEnabled.Value)}");
        }

        private void Add(OverlayLineKind kind, string text) =>
            _lines.Add(new OverlayLine { Kind = kind, Text = text });

        private static string Status(bool on) => on ? "ON" : "off";

        private enum OverlayLineKind
        {
            Section,
            Body,
            Muted,
            Alert,
            Warn,
            Good
        }

        private struct OverlayLine
        {
            public OverlayLineKind Kind;
            public string Text;
        }

        private sealed class DebugOverlayGuiRenderer : MonoBehaviour
        {
            private DebugOverlaySystem? _owner;
            private int _drawFrames;

            public void Bind(DebugOverlaySystem owner) => _owner = owner;

            private void OnGUI()
            {
                if (_owner == null || !_owner.Visible || !DreadConfig.DebugOverlayEnabled.Value)
                    return;

                _drawFrames++;
                if (_drawFrames == 1 || _drawFrames % 120 == 0)
                {
                    // #region agent log
                    DebugAgentLog.Write(
                        "A",
                        "DebugOverlayGuiRenderer.cs:OnGUI",
                        "on_gui_sample",
                        "post-fix",
                        ("drawFrames", _drawFrames),
                        ("rendererEnabled", enabled),
                        ("visible", _owner.Visible),
                        ("frame", Time.frameCount));
                    // #endregion
                }

                _owner.DrawOverlayGui();
            }
        }

        private sealed class OverlayStyleCache : IDisposable
        {
            private Texture2D? _panelBg;
            private Texture2D? _headerBg;

            public GUIStyle Panel { get; }
            public GUIStyle HeaderBar { get; }
            public GUIStyle Title { get; }
            public GUIStyle Section { get; }
            public GUIStyle Body { get; }
            public GUIStyle Muted { get; }
            public GUIStyle Alert { get; }
            public GUIStyle Warn { get; }
            public GUIStyle Good { get; }
            public float LineHeight { get; }
            public float LineSpacing { get; }
            public float MinLineHeight { get; }

            private OverlayStyleCache(
                GUIStyle panel,
                GUIStyle headerBar,
                GUIStyle title,
                GUIStyle section,
                GUIStyle body,
                GUIStyle muted,
                GUIStyle alert,
                GUIStyle warn,
                GUIStyle good,
                float lineHeight,
                float lineSpacing,
                float minLineHeight,
                Texture2D? panelBg,
                Texture2D? headerBg)
            {
                Panel = panel;
                HeaderBar = headerBar;
                Title = title;
                Section = section;
                Body = body;
                Muted = muted;
                Alert = alert;
                Warn = warn;
                Good = good;
                LineHeight = lineHeight;
                LineSpacing = lineSpacing;
                MinLineHeight = minLineHeight;
                _panelBg = panelBg;
                _headerBg = headerBg;
            }

            public static OverlayStyleCache Create(float bgAlpha, int fontSize)
            {
                var font = ResolveFont();
                fontSize = Math.Max(8, Math.Min(32, fontSize));
                const float lineSpacing = 3f;
                float minLineHeight = fontSize + 8f;

                var panelBg = MakeTexture(new Color(0.06f, 0.07f, 0.1f, bgAlpha));
                var headerBg = MakeTexture(new Color(0.45f, 0.12f, 0.14f, Clamp01(bgAlpha + 0.05f)));

                var panel = new GUIStyle(GUI.skin.box)
                {
                    border = new RectOffset(6, 6, 6, 6),
                    padding = new RectOffset(0, 0, 0, 0)
                };
                if (panelBg != null)
                    panel.normal.background = panelBg;

                var headerBar = new GUIStyle(GUI.skin.box)
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };
                if (headerBg != null)
                    headerBar.normal.background = headerBg;

                var title = BaseLabel(font, fontSize + 1, FontStyle.Bold, new Color(0.98f, 0.92f, 0.88f));
                var section = BaseLabel(font, fontSize, FontStyle.Bold, new Color(0.75f, 0.68f, 0.45f));
                var body = BaseLabel(font, fontSize, FontStyle.Normal, new Color(0.9f, 0.9f, 0.92f));
                var muted = BaseLabel(font, fontSize - 1, FontStyle.Italic, new Color(0.55f, 0.58f, 0.62f));
                var alert = BaseLabel(font, fontSize, FontStyle.Bold, new Color(1f, 0.45f, 0.35f));
                var warn = BaseLabel(font, fontSize, FontStyle.Normal, new Color(1f, 0.78f, 0.4f));
                var good = BaseLabel(font, fontSize, FontStyle.Bold, new Color(0.45f, 0.95f, 0.55f));

                return new OverlayStyleCache(
                    panel, headerBar, title, section, body, muted, alert, warn, good,
                    minLineHeight, lineSpacing, minLineHeight, panelBg, headerBg);
            }

            public GUIStyle ForKind(OverlayLineKind kind) =>
                kind switch
                {
                    OverlayLineKind.Section => Section,
                    OverlayLineKind.Muted => Muted,
                    OverlayLineKind.Alert => Alert,
                    OverlayLineKind.Warn => Warn,
                    OverlayLineKind.Good => Good,
                    _ => Body
                };

            public void Dispose()
            {
                if (_panelBg != null)
                    UnityEngine.Object.Destroy(_panelBg);
                if (_headerBg != null)
                    UnityEngine.Object.Destroy(_headerBg);
                _panelBg = null;
                _headerBg = null;
            }

            private static Font ResolveFont()
            {
                if (GUI.skin?.font != null)
                    return GUI.skin.font;

                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            private static GUIStyle BaseLabel(Font font, int size, FontStyle style, Color color)
            {
                return new GUIStyle(GUI.skin.label)
                {
                    font = font,
                    fontSize = Math.Max(8, size),
                    fontStyle = style,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false,
                    richText = false,
                    padding = new RectOffset(0, 0, 2, 6),
                    normal = { textColor = color }
                };
            }

            private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

            private static Texture2D? MakeTexture(Color color) =>
                OverlayTextureUtil.CreateSolid(color);
        }
    }
}
