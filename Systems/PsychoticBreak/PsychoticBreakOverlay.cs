using System;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private Component? _accentLeftImage;
        private Component? _accentRightImage;
        private Texture2D? _accentGradientTexture;
        private Color _accentPrimary = PsychoticBreakAccentPalette.GetById(PsychoticBreakAccentId.ScaryRed);
        private Color _accentSecondary = PsychoticBreakAccentPalette.GetById(PsychoticBreakAccentId.DeepCrimson);
        private float _accentAlpha;
        private int _accentFallbackCursor;

        private void CreateOverlay()
        {
            if (_overlayRoot != null) return;

            var rawImageType = ResolveRawImageType();
            if (rawImageType == null)
            {
                LoggingService.LogError("[PsychoticBreak] UnityEngine.UI.RawImage not available");
                return;
            }

            var canvasType = ResolveCanvasType();
            if (canvasType == null)
            {
                LoggingService.LogError("[PsychoticBreak] UnityEngine.Canvas not available");
                return;
            }

            var go = new GameObject("DreadPsychoticBreakOverlay");
            DontDestroyOnLoad(go);
            _overlayRoot = go;

            var canvas = AddRuntimeComponent(go, canvasType);
            ConfigureCanvas(canvas);

            var darkGo = new GameObject("Darkness");
            darkGo.transform.SetParent(go.transform, false);
            _darknessImage = AddRuntimeComponent(darkGo, rawImageType);
            SetRawImageColor(_darknessImage, new Color(0, 0, 0, 0));
            StretchToParent(_darknessImage);

            var vigGo = new GameObject("Vignette");
            vigGo.transform.SetParent(go.transform, false);
            _vignetteImage = AddRuntimeComponent(vigGo, rawImageType);
            SetRawImageColor(_vignetteImage, new Color(0, 0, 0, 0));
            StretchToParent(_vignetteImage);

            _vignetteTexture = OverlayTextureUtil.CreateVignette(256);
            if (_vignetteTexture == null)
                LoggingService.LogWarning("[PsychoticBreak] Vignette texture unavailable; overlay may be incomplete");
            else
                SetRawImageTexture(_vignetteImage, _vignetteTexture);

            _accentGradientTexture = OverlayTextureUtil.CreateEdgeAccent(64, 256);
            if (_accentGradientTexture != null && DreadConfig.PsychoticBreakAccentEnabled.Value)
            {
                _accentLeftImage = CreateAccentStrip(go, rawImageType, "AccentLeft", mirrorX: false);
                _accentRightImage = CreateAccentStrip(go, rawImageType, "AccentRight", mirrorX: true);
            }
        }

        private Component? CreateAccentStrip(GameObject parent, Type rawImageType, string name, bool mirrorX)
        {
            var stripGo = new GameObject(name);
            stripGo.transform.SetParent(parent.transform, false);
            var img = AddRuntimeComponent(stripGo, rawImageType);
            SetRawImageTexture(img, _accentGradientTexture);
            SetRawImageColor(img, new Color(_accentPrimary.r, _accentPrimary.g, _accentPrimary.b, 0f));
            StretchAccentStrip(img, mirrorX);
            return img;
        }

        private static void StretchAccentStrip(Component rawImage, bool mirrorX)
        {
            var rectTransformType = rawImage.GetType().Assembly.GetType("UnityEngine.RectTransform")
                ?? Type.GetType("UnityEngine.RectTransform, UnityEngine.CoreModule");
            if (rectTransformType == null) return;

            var rectTransform = rawImage.GetComponent(rectTransformType);
            if (rectTransform == null) return;

            var anchorMin = rectTransformType.GetProperty("anchorMin");
            var anchorMax = rectTransformType.GetProperty("anchorMax");
            var sizeDelta = rectTransformType.GetProperty("sizeDelta");
            var localScale = rectTransformType.GetProperty("localScale");

            if (mirrorX)
            {
                anchorMin?.SetValue(rectTransform, new Vector2(0.85f, 0f), null);
                anchorMax?.SetValue(rectTransform, new Vector2(1f, 1f), null);
                localScale?.SetValue(rectTransform, new Vector3(-1f, 1f, 1f), null);
            }
            else
            {
                anchorMin?.SetValue(rectTransform, new Vector2(0f, 0f), null);
                anchorMax?.SetValue(rectTransform, new Vector2(0.15f, 1f), null);
            }

            sizeDelta?.SetValue(rectTransform, Vector2.zero, null);
        }

        private void PickEpisodeAccents(System.Random rng)
        {
            int primary = rng.Next(0, PsychoticBreakAccentPalette.AllowlistCount);
            int secondary = rng.Next(0, PsychoticBreakAccentPalette.AllowlistCount);
            while (secondary == primary)
                secondary = rng.Next(0, PsychoticBreakAccentPalette.AllowlistCount);

            _accentPrimary = PsychoticBreakAccentPalette.GetAllowlistColor(primary);
            _accentSecondary = PsychoticBreakAccentPalette.GetAllowlistColor(secondary);
            _accentFallbackCursor = 0;
            ApplyAccentRgb();
        }

        private void ApplyAccentRgb()
        {
            if (_accentLeftImage != null)
            {
                var c = GetRawImageColor(_accentLeftImage);
                SetRawImageColor(_accentLeftImage, new Color(_accentPrimary.r, _accentPrimary.g, _accentPrimary.b, c.a));
            }

            if (_accentRightImage != null)
            {
                var c = GetRawImageColor(_accentRightImage);
                SetRawImageColor(_accentRightImage, new Color(_accentPrimary.r, _accentPrimary.g, _accentPrimary.b, c.a));
            }
        }

        private void SetAccentAlpha(float alpha)
        {
            _accentAlpha = Mathf.Clamp01(alpha);
            if (!DreadConfig.PsychoticBreakAccentEnabled.Value)
                return;

            if (_accentLeftImage != null)
            {
                var c = GetRawImageColor(_accentLeftImage);
                SetRawImageColor(_accentLeftImage, new Color(c.r, c.g, c.b, _accentAlpha));
            }

            if (_accentRightImage != null)
            {
                var c = GetRawImageColor(_accentRightImage);
                SetRawImageColor(_accentRightImage, new Color(c.r, c.g, c.b, _accentAlpha));
            }
        }

        public void PulseAttackAccentFlash()
        {
            if (!DreadConfig.PsychoticBreakAccentEnabled.Value)
                return;

            var pulse = PsychoticBreakAccentPalette.GetAttackPulseColor();
            if (_accentLeftImage != null)
                SetRawImageColor(_accentLeftImage, new Color(pulse.r, pulse.g, pulse.b, Mathf.Min(1f, _accentAlpha + 0.35f)));
            if (_accentRightImage != null)
                SetRawImageColor(_accentRightImage, new Color(pulse.r, pulse.g, pulse.b, Mathf.Min(1f, _accentAlpha + 0.35f)));
        }

        private void UseAccentFallback()
        {
            var color = PsychoticBreakAccentPalette.GetFallback(-1, ref _accentFallbackCursor);
            _accentPrimary = color;
            ApplyAccentRgb();
        }

        private static Color GetRawImageColor(Component rawImage)
        {
            var colorProp = rawImage.GetType().GetProperty("color");
            if (colorProp == null)
                return Color.clear;
            try
            {
                return (Color)colorProp.GetValue(rawImage, null)!;
            }
            catch
            {
                return Color.clear;
            }
        }

        private static Type? ResolveRawImageType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(asm.GetName().Name, "UnityEngine.UI", StringComparison.Ordinal))
                    continue;
                return asm.GetType("UnityEngine.UI.RawImage");
            }

            return Type.GetType("UnityEngine.UI.RawImage, UnityEngine.UI");
        }

        private static Type? ResolveCanvasType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name is not ("UnityEngine.UIModule" or "UnityEngine"))
                    continue;
                var canvas = asm.GetType("UnityEngine.Canvas");
                if (canvas != null)
                    return canvas;
            }

            return Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule")
                ?? Type.GetType("UnityEngine.Canvas, UnityEngine");
        }

        private static void ConfigureCanvas(Component canvas)
        {
            var renderModeProp = canvas.GetType().GetProperty("renderMode");
            renderModeProp?.SetValue(canvas, RenderMode.ScreenSpaceOverlay, null);

            var sortingOrderProp = canvas.GetType().GetProperty("sortingOrder");
            sortingOrderProp?.SetValue(canvas, 999, null);
        }

        private static Component AddRuntimeComponent(GameObject go, Type componentType)
        {
            return go.AddComponent(componentType);
        }

        private static void StretchToParent(Component rawImage)
        {
            var rectTransformType = rawImage.GetType().Assembly.GetType("UnityEngine.RectTransform")
                ?? Type.GetType("UnityEngine.RectTransform, UnityEngine.CoreModule");
            if (rectTransformType == null) return;

            var rectTransform = rawImage.GetComponent(rectTransformType);
            if (rectTransform == null) return;

            var anchorMin = rectTransformType.GetProperty("anchorMin");
            var anchorMax = rectTransformType.GetProperty("anchorMax");
            var sizeDelta = rectTransformType.GetProperty("sizeDelta");
            anchorMin?.SetValue(rectTransform, Vector2.zero, null);
            anchorMax?.SetValue(rectTransform, Vector2.one, null);
            sizeDelta?.SetValue(rectTransform, Vector2.zero, null);
        }

        private static void SetRawImageColor(Component rawImage, Color color)
        {
            var colorProp = rawImage.GetType().GetProperty("color");
            colorProp?.SetValue(rawImage, color, null);
        }

        private static void SetRawImageTexture(Component rawImage, Texture2D texture)
        {
            var textureProp = rawImage.GetType().GetProperty("texture");
            textureProp?.SetValue(rawImage, texture, null);
        }

        private void CleanupOverlay()
        {
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }
            _darknessImage = null;
            _vignetteImage = null;
            _accentLeftImage = null;
            _accentRightImage = null;

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
                _vignetteTexture = null;
            }

            if (_accentGradientTexture != null)
            {
                Destroy(_accentGradientTexture);
                _accentGradientTexture = null;
            }
        }

        private void SetDarknessAlpha(float alpha)
        {
            if (_darknessImage != null)
                SetRawImageColor(_darknessImage, new Color(0, 0, 0, Mathf.Clamp01(alpha)));
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (_vignetteImage != null)
                SetRawImageColor(_vignetteImage, new Color(0, 0, 0, Mathf.Clamp01(alpha)));
        }
    }
}
