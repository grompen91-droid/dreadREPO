using UnityEngine;

namespace Dread.Systems
{
    public partial class DebugOverlaySystem
    {
        private const byte RowNormal = 0;
        private const byte RowHeader = 1;
        private const byte RowSep = 2;
        private const byte RowSection = 3;

        // Slate HUD "S2" monochrome palette. Pulled from the R.E.P.O. icon:
        // void black, brushed-steel grays, soft white. No hue, no gradients.
        private static readonly Color ColAccent = new(0.80f, 0.81f, 0.83f);  // header text (steel highlight)
        private static readonly Color ColRail = new(0.74f, 0.75f, 0.77f);    // left accent rail + section ticks
        private static readonly Color ColSection = new(0.60f, 0.61f, 0.64f); // section labels (mid steel)
        private static readonly Color ColDim = new(0.44f, 0.45f, 0.49f);     // keys and muted values (steel low)
        private static readonly Color ColValue = new(0.91f, 0.92f, 0.93f);   // primary values (soft white)
        private static readonly Color ColGood = new(0.79f, 0.81f, 0.79f);    // status ok (light neutral)
        private static readonly Color ColWarn = new(0.85f, 0.79f, 0.65f);    // status warn (warm gray)
        private static readonly Color ColBad = new(0.84f, 0.70f, 0.68f);     // status bad (rosy gray)
        private static readonly Color ColButton = new(0.11f, 0.12f, 0.14f, 0.92f);      // button rest
        private static readonly Color ColButtonHover = new(0.17f, 0.18f, 0.21f, 0.96f); // button hover

        private Texture2D? _bgTex;
        private Texture2D? _sepTex;
        private Texture2D? _railTex;
        private Texture2D? _buttonTex;
        private Texture2D? _buttonHoverTex;
        private GUIStyle? _boxStyle;
        private GUIStyle? _railStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _hintStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _valueStyle;
        private GUIStyle? _sectionStyle;
        private GUIStyle? _sectionBtnStyle;
        private GUIStyle? _caretStyle;
        private GUIStyle? _sepStyle;
        private GUIStyle? _buttonStyle;
        private GUIStyle? _midStyle;

        private void EnsureStyles()
        {
            if (_boxStyle != null)
                return;

            _bgTex = MakeTexture(new Color(0.055f, 0.058f, 0.070f, 0.90f));
            _sepTex = MakeTexture(new Color(0.85f, 0.86f, 0.88f, 0.18f));
            _railTex = MakeTexture(ColRail);

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _bgTex;

            _railStyle = new GUIStyle(GUI.skin.box);
            _railStyle.normal.background = _railTex;

            // MiddleLeft vertically centers each label in its row box so glyphs
            // line up with the steel ticks and separators drawn at mid-line.
            _headerStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 15, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            _headerStyle.normal.textColor = ColAccent;

            _hintStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = false, alignment = TextAnchor.MiddleRight };
            _hintStyle.normal.textColor = ColDim;

            _labelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 13, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            _labelStyle.normal.textColor = ColDim;

            _valueStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 13, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            _valueStyle.normal.textColor = ColValue;

            _sectionStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            _sectionStyle.normal.textColor = ColSection;

            // Transparent button over the section label so the row folds on click.
            // No background = invisible chrome; it reads as the section label itself.
            _sectionBtnStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            _sectionBtnStyle.normal.textColor = ColSection;
            _sectionBtnStyle.hover.textColor = ColAccent;
            _sectionBtnStyle.active.textColor = ColAccent;

            // Fold caret, right-aligned in the section row.
            _caretStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = false, alignment = TextAnchor.MiddleRight };
            _caretStyle.normal.textColor = ColDim;

            // Centered numeric readout (zoom %, slider %) so the value sits under
            // its control instead of hugging the left edge.
            _midStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, wordWrap = false, alignment = TextAnchor.MiddleCenter };
            _midStyle.normal.textColor = ColValue;

            _sepStyle = new GUIStyle(GUI.skin.box);
            _sepStyle.normal.background = _sepTex;

            _buttonTex = MakeTexture(ColButton);
            _buttonHoverTex = MakeTexture(ColButtonHover);
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, wordWrap = false };
            _buttonStyle.normal.background = _buttonTex;
            _buttonStyle.normal.textColor = ColValue;
            _buttonStyle.hover.background = _buttonHoverTex;
            _buttonStyle.hover.textColor = ColAccent;
            _buttonStyle.active.background = _buttonHoverTex;
            _buttonStyle.active.textColor = ColAccent;
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
