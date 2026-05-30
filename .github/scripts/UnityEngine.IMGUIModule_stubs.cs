// Stub for UnityEngine.IMGUIModule. In real Unity the IMGUI types (GUI, GUIStyle,
// GUIContent, GUISkin, GUIStyleState) live in UnityEngine.IMGUIModule.dll, forwarded
// from UnityEngine.dll. They must be referenced from that assembly so compiled IL
// binds to the correct types at runtime. Rect, Color, and Texture2D remain in the
// UnityEngine stub and are resolved via the UnityEngine reference.
namespace UnityEngine
{
    public class GUIContent
    {
        public static GUIContent none { get; } = new GUIContent();
        public string text { get; set; } = string.Empty;
        public GUIContent() { }
        public GUIContent(string text) => this.text = text;
    }

    public class GUIStyleState
    {
        public Texture2D? background { get; set; }
        public Color textColor { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyleState normal { get; } = new GUIStyleState();
        public GUIStyleState hover { get; } = new GUIStyleState();
        public GUIStyleState active { get; } = new GUIStyleState();
        public int fontSize { get; set; }
        public bool wordWrap { get; set; }
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }

        public float CalcHeight(GUIContent content, float width)
        {
            var text = content?.text ?? string.Empty;
            if (width <= 1f || text.Length == 0)
                return fontSize + 8f;

            var linePx = fontSize + 7f;
            var charsPerLine = System.Math.Max(8, (int)(width / (fontSize * 0.55f)));
            var lines = (text.Length + charsPerLine - 1) / charsPerLine;
            return lines * linePx + 4f;
        }
    }

    public class GUISkin
    {
        public GUIStyle box { get; } = new GUIStyle();
        public GUIStyle label { get; } = new GUIStyle();
        public GUIStyle button { get; } = new GUIStyle();
    }

    public static class GUI
    {
        public static int depth { get; set; }
        public static GUISkin skin { get; } = new GUISkin();
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static bool Button(Rect position, string text, GUIStyle style) => false;
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect) => scrollPosition;
        public static void EndScrollView() { }
    }
}
