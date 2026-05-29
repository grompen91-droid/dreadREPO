using System;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
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
            {
                LoggingService.LogWarning("[PsychoticBreak] Vignette texture unavailable; overlay may be incomplete");
                return;
            }

            SetRawImageTexture(_vignetteImage, _vignetteTexture);
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

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
                _vignetteTexture = null;
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
