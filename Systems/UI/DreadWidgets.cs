using UnityEngine;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Reusable Slate HUD "S2" monochrome widgets for IMGUI: a determinate
    /// loading bar, an indeterminate marquee, and a value slider. Monochrome,
    /// no gradients. Styles are created lazily on first use.
    ///
    /// All draw calls must run inside an OnGUI pass.
    /// </summary>
    public static class DreadWidgets
    {
        private static readonly Color ColTrack = new(0.20f, 0.21f, 0.24f, 0.85f);
        private static readonly Color ColSteel = new(0.74f, 0.75f, 0.77f);

        private static readonly GUIContent EmptyContent = new();

        private static Texture2D? _trackTex;
        private static Texture2D? _steelTex;
        private static GUIStyle? _trackStyle;
        private static GUIStyle? _fillStyle;
        private static GUIStyle? _sliderStyle;
        private static GUIStyle? _thumbStyle;

        /// <summary>Determinate progress bar. <paramref name="progress"/> is clamped to [0, 1].</summary>
        public static void LoadingBar(Rect rect, float progress)
        {
            Ensure();
            float p = Mathf.Clamp01(progress);
            GUI.Box(rect, EmptyContent, _trackStyle!);
            if (p > 0f)
                GUI.Box(new Rect(rect.x, rect.y, rect.width * p, rect.height), EmptyContent, _fillStyle!);
        }

        /// <summary>Indeterminate marquee: a steel chunk sweeping left to right.</summary>
        public static void LoadingMarquee(Rect rect)
        {
            Ensure();
            GUI.Box(rect, EmptyContent, _trackStyle!);

            float chunk = rect.width * 0.32f;
            float span = rect.width + chunk;
            float t = Time.realtimeSinceStartup * 0.9f;
            float phase = t - (int)t;                  // 0..1 sawtooth
            float left = rect.x - chunk + span * phase;

            float clampedLeft = Mathf.Max(left, rect.x);
            float right = Mathf.Min(left + chunk, rect.x + rect.width);
            float w = right - clampedLeft;
            if (w > 0f)
                GUI.Box(new Rect(clampedLeft, rect.y, w, rect.height), EmptyContent, _fillStyle!);
        }

        /// <summary>Value slider. Returns the new value after any drag.</summary>
        public static float Slider(Rect rect, float value, float min, float max)
        {
            Ensure();
            return GUI.HorizontalSlider(rect, value, min, max, _sliderStyle!, _thumbStyle!);
        }

        private static void Ensure()
        {
            if (_trackStyle != null)
                return;

            _trackTex = MakeTexture(ColTrack);
            _steelTex = MakeTexture(ColSteel);

            _trackStyle = new GUIStyle(GUI.skin.box);
            _trackStyle.normal.background = _trackTex;

            _fillStyle = new GUIStyle(GUI.skin.box);
            _fillStyle.normal.background = _steelTex;

            _sliderStyle = new GUIStyle(GUI.skin.box) { fixedHeight = 5f };
            _sliderStyle.normal.background = _trackTex;

            _thumbStyle = new GUIStyle(GUI.skin.box) { fixedWidth = 11f, fixedHeight = 11f };
            _thumbStyle.normal.background = _steelTex;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
