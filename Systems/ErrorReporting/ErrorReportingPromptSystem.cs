using System;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    /// <summary>
    /// One-time IMGUI disclosure before error reports may be sent (ERR-2, issue #172).
    /// </summary>
    public class ErrorReportingPromptSystem : MonoBehaviour
    {
        private enum PromptState : byte
        {
            Pending,
            Visible,
            Dismissed,
        }

        private const int GuiDepth = 10000;
        private const float WindowWidth = 580f;
        private const float MinWindowHeight = 500f;
        private const float Pad = 18f;
        private const float ButtonHeight = 36f;
        private const float ButtonGap = 10f;
        private const float BulletGap = 6f;
        private const float MinBulletRowHeight = 22f;

        private const string ModBrandTitle = "DREAD";
        private const string ModBrandSubtitle = "elytraking-Dread · atmospheric horror mod for R.E.P.O.";
        private const string ModBrandAsk =
            "This mod (not the base game) is asking permission before it may send anonymous error reports.";

        private static readonly GUIContent EmptyContent = new();
        private static readonly Color ColAccent = new(0.96f, 0.55f, 0.38f);
        private static readonly Color ColDim = new(0.62f, 0.64f, 0.70f);
        private static readonly Color ColBody = new(0.92f, 0.93f, 0.96f);
        private static readonly Color ColOverlay = new(0.02f, 0.02f, 0.04f, 0.72f);
        private static readonly Color ColPanel = new(0.08f, 0.08f, 0.11f, 0.96f);
        private static readonly Color ColButton = new(0.14f, 0.14f, 0.18f, 1f);
        private static readonly Color ColButtonHover = new(0.22f, 0.22f, 0.28f, 1f);
        private static readonly Color ColButtonPrimary = new(0.28f, 0.16f, 0.12f, 1f);
        private static readonly Color ColButtonPrimaryHover = new(0.38f, 0.22f, 0.16f, 1f);

        private PromptState _state = PromptState.Pending;
        private Vector2 _scrollPosition;
        private bool _cursorCaptured;
        private CursorLockMode _savedLockState;
        private bool _savedCursorVisible;
        private bool _inputLocked;
        private bool _layoutReady;
        private float _windowHeight = MinWindowHeight;
        private float _summaryHeight;
        private float _hintHeight;
        private float _scrollContentHeight;
        private float _scrollViewHeight;
        private float[]? _bulletHeights;

        private Texture2D? _overlayTex;
        private Texture2D? _panelTex;
        private Texture2D? _buttonTex;
        private Texture2D? _buttonHoverTex;
        private Texture2D? _buttonPrimaryTex;
        private Texture2D? _buttonPrimaryHoverTex;
        private GUIStyle? _overlayStyle;
        private GUIStyle? _panelStyle;
        private GUIStyle? _brandStyle;
        private GUIStyle? _subtitleStyle;
        private GUIStyle? _askStyle;
        private GUIStyle? _sectionStyle;
        private GUIStyle? _bodyStyle;
        private GUIStyle? _hintStyle;
        private GUIStyle? _buttonStyle;
        private GUIStyle? _buttonPrimaryStyle;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            DreadConfig.ErrorReportingPromptShown.SettingChanged += OnPromptShownConfigChanged;
            TryActivatePrompt(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DreadConfig.ErrorReportingPromptShown.SettingChanged -= OnPromptShownConfigChanged;
            ReleasePromptCapture();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryActivatePrompt(scene);
        }

        private void OnPromptShownConfigChanged(object? sender, EventArgs e)
        {
            SyncPromptStateFromConfig();
            TryActivatePrompt(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Keeps in-memory prompt state aligned with cfg (resetting ErrorReportingPromptShown
        /// via cfg or REPOConfig works without restart).
        /// </summary>
        private void SyncPromptStateFromConfig()
        {
            if (DreadConfig.ErrorReportingPromptShown.Value)
            {
                if (_state == PromptState.Visible)
                    ReleasePromptCapture();
                _state = PromptState.Dismissed;
                return;
            }

            if (_state != PromptState.Dismissed)
                return;

            ReleasePromptCapture();
            _state = PromptState.Pending;
            _layoutReady = false;
        }

        private void TryActivatePrompt(Scene scene)
        {
            SyncPromptStateFromConfig();

            if (_state == PromptState.Dismissed)
                return;

            if (GameplayContext.IsMenuLevel())
            {
                if (_state == PromptState.Visible)
                {
                    ReleasePromptCapture();
                    _state = PromptState.Pending;
                }

                return;
            }

            if (_state == PromptState.Pending)
                _state = PromptState.Visible;
        }

        private void Update()
        {
            if (_state != PromptState.Visible)
                return;

            MaintainCursorForPrompt();

            if (!_inputLocked)
            {
                LockLocalPlayerInput();
                _inputLocked = true;
            }
        }

        private void OnGUI()
        {
            if (_state != PromptState.Visible)
                return;

            EnsureStyles();

            var screenH = Screen.height > 100 ? Screen.height : 1080f;
            var innerW = WindowWidth - Pad * 2f;
            EnsureLayout(innerW, screenH);

            MaintainCursorForPrompt();

            var prevDepth = GUI.depth;
            GUI.depth = GuiDepth;

            var screenRect = new Rect(0f, 0f, Screen.width, screenH);
            GUI.Box(screenRect, EmptyContent, _overlayStyle!);

            var centerX = (Screen.width - WindowWidth) * 0.5f;
            var centerY = (screenH - _windowHeight) * 0.5f;
            var windowRect = new Rect(centerX, centerY, WindowWidth, _windowHeight);

            GUI.Box(windowRect, EmptyContent, _panelStyle!);

            var innerX = windowRect.x + Pad;
            var y = windowRect.y + Pad;

            GUI.Label(new Rect(innerX, y, innerW, 30f), ModBrandTitle, _brandStyle!);
            y += 32f;
            GUI.Label(new Rect(innerX, y, innerW, 20f), ModBrandSubtitle, _subtitleStyle!);
            y += 22f;
            GUI.Label(new Rect(innerX, y, innerW, 36f), ModBrandAsk, _askStyle!);
            y += 40f;
            GUI.Label(new Rect(innerX, y, innerW, 22f), "Error reporting", _sectionStyle!);
            y += 26f;

            GUI.Label(new Rect(innerX, y, innerW, _summaryHeight), ErrorReportingPrivacyCopy.ShortSummary, _bodyStyle!);
            y += _summaryHeight + 10f;

            var scrollRect = new Rect(innerX, y, innerW, _scrollViewHeight);
            var viewRect = new Rect(0f, 0f, innerW - 22f, _scrollContentHeight);
            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, viewRect);

            var bulletY = 0f;
            var bullets = ErrorReportingPrivacyCopy.DataBullets;
            for (var i = 0; i < bullets.Length; i++)
            {
                var rowH = _bulletHeights![i];
                GUI.Label(new Rect(0f, bulletY, viewRect.width, rowH - BulletGap), "\u2022 " + bullets[i], _bodyStyle!);
                bulletY += rowH;
            }

            GUI.EndScrollView();
            y += _scrollViewHeight + 10f;

            GUI.Label(
                new Rect(innerX, y, innerW, _hintHeight),
                ErrorReportingPrivacyCopy.DisableInstructions,
                _hintStyle!);
            y += _hintHeight + 12f;

            var buttonW = (innerW - ButtonGap) * 0.5f;
            if (GUI.Button(new Rect(innerX, y, buttonW, ButtonHeight), "Keep reporting on", _buttonPrimaryStyle!))
                OnPromptChoice(keepReporting: true);

            if (GUI.Button(
                    new Rect(innerX + buttonW + ButtonGap, y, buttonW, ButtonHeight),
                    "Turn off reporting",
                    _buttonStyle!))
                OnPromptChoice(keepReporting: false);

            GUI.depth = prevDepth;
        }

        private void EnsureLayout(float innerW, float screenH)
        {
            if (_layoutReady)
                return;

            _summaryHeight = _bodyStyle!.CalcHeight(
                new GUIContent(ErrorReportingPrivacyCopy.ShortSummary),
                innerW);
            _hintHeight = _hintStyle!.CalcHeight(
                new GUIContent(ErrorReportingPrivacyCopy.DisableInstructions),
                innerW);

            var bulletW = innerW - 22f;
            var bullets = ErrorReportingPrivacyCopy.DataBullets;
            _bulletHeights = new float[bullets.Length];
            _scrollContentHeight = 4f;
            for (var i = 0; i < bullets.Length; i++)
            {
                var h = _bodyStyle.CalcHeight(new GUIContent("\u2022 " + bullets[i]), bulletW);
                _bulletHeights[i] = Mathf.Max(h, MinBulletRowHeight) + BulletGap;
                _scrollContentHeight += _bulletHeights[i];
            }

            const float brandBlock = 32f + 22f + 40f + 26f;
            const float footerBlock = 12f + ButtonHeight + Pad;
            var maxScroll = screenH * 0.38f;
            _scrollViewHeight = Mathf.Min(_scrollContentHeight, maxScroll);
            _windowHeight = Mathf.Min(
                screenH * 0.92f,
                Pad * 2f + brandBlock + _summaryHeight + 10f + _scrollViewHeight + 10f + _hintHeight + footerBlock);
            _windowHeight = Mathf.Max(_windowHeight, MinWindowHeight);
            _layoutReady = true;
        }

        private void MaintainCursorForPrompt()
        {
            if (!_cursorCaptured)
            {
                _savedLockState = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _cursorCaptured = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReleasePromptCapture()
        {
            if (_cursorCaptured)
            {
                Cursor.lockState = _savedLockState;
                Cursor.visible = _savedCursorVisible;
                _cursorCaptured = false;
            }

            if (_inputLocked)
            {
                UnlockLocalPlayerInput();
                _inputLocked = false;
            }
        }

        private static void LockLocalPlayerInput()
        {
            var pc = PlayerController.instance;
            if ((object)pc == null)
                return;

            PlayerInputLockCompat.SetLocked(pc, locked: true);
        }

        private static void UnlockLocalPlayerInput()
        {
            var pc = PlayerController.instance;
            if ((object)pc == null)
                return;

            PlayerInputLockCompat.SetLocked(pc, locked: false);
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
                return;

            _overlayTex = MakeTexture(ColOverlay);
            _panelTex = MakeTexture(ColPanel);
            _buttonTex = MakeTexture(ColButton);
            _buttonHoverTex = MakeTexture(ColButtonHover);
            _buttonPrimaryTex = MakeTexture(ColButtonPrimary);
            _buttonPrimaryHoverTex = MakeTexture(ColButtonPrimaryHover);

            _overlayStyle = new GUIStyle(GUI.skin.box);
            _overlayStyle.normal.background = _overlayTex;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = _panelTex;

            _brandStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = false };
            _brandStyle.normal.textColor = ColAccent;

            _subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            _subtitleStyle.normal.textColor = ColDim;

            _askStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            _askStyle.normal.textColor = ColBody;

            _sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = false };
            _sectionStyle.normal.textColor = ColAccent;

            _bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _bodyStyle.normal.textColor = ColBody;

            _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            _hintStyle.normal.textColor = ColDim;

            _buttonStyle = BuildButtonStyle(_buttonTex!, _buttonHoverTex!, ColBody);
            _buttonPrimaryStyle = BuildButtonStyle(_buttonPrimaryTex!, _buttonPrimaryHoverTex!, ColAccent);
        }

        private static GUIStyle BuildButtonStyle(Texture2D normal, Texture2D hover, Color textColor)
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = hover;
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            return style;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void OnPromptChoice(bool keepReporting)
        {
            DreadConfig.ErrorReportingEnabled.Value = keepReporting;
            DreadConfig.ErrorReportingPromptShown.Value = true;
            DreadConfig.SaveToDisk();
            _state = PromptState.Dismissed;
            ReleasePromptCapture();

            if (keepReporting)
            {
                LoggingService.LogInfo(
                    "[Dread] Error reporting enabled. "
                        + ErrorReportingPrivacyCopy.ShortSummary
                        + " "
                        + ErrorReportingPrivacyCopy.DisableInstructions);
            }
            else
            {
                LoggingService.LogInfo("[Dread] Anonymous error reporting turned off.");
            }
        }
    }
}
