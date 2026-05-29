using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem
    {
        private const byte RowNormal = 0;
        private const byte RowHeader = 1;
        private const byte RowSep = 2;

        private static readonly Color ColAccent = new(0.96f, 0.55f, 0.38f);
        private static readonly Color ColDim = new(0.62f, 0.64f, 0.70f);
        private static readonly Color ColValue = new(0.92f, 0.93f, 0.96f);
        private static readonly Color ColGood = new(0.48f, 0.90f, 0.55f);
        private static readonly Color ColWarn = new(0.97f, 0.84f, 0.42f);
        private static readonly Color ColBad = new(0.96f, 0.46f, 0.46f);

        private Texture2D? _bgTex;
        private Texture2D? _sepTex;
        private GUIStyle? _boxStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _hintStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _valueStyle;
        private GUIStyle? _sepStyle;

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
    }
}
