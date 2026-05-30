using Dread.Config;
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

        private const float WindowWidth = 520f;
        private const float WindowHeight = 420f;
        private const float Pad = 14f;
        private const float LineHeight = 18f;
        private const float ButtonHeight = 32f;
        private const float ButtonGap = 8f;

        private static readonly GUIContent EmptyContent = new();

        private PromptState _state = PromptState.Pending;
        private Vector2 _scrollPosition;
        private GUIStyle? _boxStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _buttonStyle;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryActivatePrompt(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryActivatePrompt(scene);
        }

        private void TryActivatePrompt(Scene scene)
        {
            if (_state == PromptState.Dismissed)
                return;

            if (DreadConfig.ErrorReportingPromptShown.Value)
            {
                _state = PromptState.Dismissed;
                return;
            }

            if (SemiFunc.MenuLevel())
                return;

            if (_state == PromptState.Pending)
                _state = PromptState.Visible;
        }

        private void OnGUI()
        {
            if (_state != PromptState.Visible)
                return;

            EnsureStyles();

            var centerX = (Screen.width - WindowWidth) * 0.5f;
            var centerY = (Screen.height - WindowHeight) * 0.5f;
            var windowRect = new Rect(centerX, centerY, WindowWidth, WindowHeight);

            GUI.Box(windowRect, EmptyContent, _boxStyle!);

            var innerX = windowRect.x + Pad;
            var innerY = windowRect.y + Pad;
            var innerW = windowRect.width - Pad * 2f;
            var y = innerY;

            GUI.Label(new Rect(innerX, y, innerW, LineHeight * 3f), ErrorReportingPrivacyCopy.ShortSummary, _labelStyle!);
            y += LineHeight * 3f + 6f;

            var scrollHeight = WindowHeight - Pad * 2f - LineHeight * 3f - LineHeight * 4f - ButtonHeight * 2f - ButtonGap - 24f;
            var scrollRect = new Rect(innerX, y, innerW, scrollHeight);
            var contentHeight = ErrorReportingPrivacyCopy.DataBullets.Length * LineHeight + 8f;
            var viewRect = new Rect(0f, 0f, innerW - 20f, contentHeight);
            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, viewRect);

            var bulletY = 0f;
            foreach (var bullet in ErrorReportingPrivacyCopy.DataBullets)
            {
                GUI.Label(new Rect(0f, bulletY, viewRect.width, LineHeight), "\u2022 " + bullet, _labelStyle!);
                bulletY += LineHeight;
            }

            GUI.EndScrollView();
            y += scrollHeight + 6f;

            GUI.Label(
                new Rect(innerX, y, innerW, LineHeight * 3f),
                ErrorReportingPrivacyCopy.DisableInstructions,
                _labelStyle!);
            y += LineHeight * 3f + 8f;

            var buttonW = (innerW - ButtonGap) * 0.5f;
            if (GUI.Button(new Rect(innerX, y, buttonW, ButtonHeight), "Keep reporting on", _buttonStyle!))
                OnPromptChoice(keepReporting: true);

            if (GUI.Button(new Rect(innerX + buttonW + ButtonGap, y, buttonW, ButtonHeight), "Turn off reporting", _buttonStyle!))
                OnPromptChoice(keepReporting: false);
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
                return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button);
        }

        private void OnPromptChoice(bool keepReporting)
        {
            DreadConfig.ErrorReportingEnabled.Value = keepReporting;
            DreadConfig.ErrorReportingPromptShown.Value = true;
            DreadConfig.SaveToDisk();
            _state = PromptState.Dismissed;

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
