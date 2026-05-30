using System;
using System.Reflection;
using Dread.Systems;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// REPOConfig-only MenuLib compat. Without REPOConfig, Dread uses BepInEx cfg / ConfigurationManager (no patch).
    /// When REPOConfig passes an empty slider description, restore the setting name and keep the compact row.
    /// </summary>
    internal static class RepoConfigSliderLabelCompat
    {
        /// <summary>Left name column: after MenuLib Awake (~44.7), place label before bar (~122).</summary>
        private const float LabelColumnLocalX = 100f;

        private static bool _applied;

        internal static void TryApply(Harmony harmony)
        {
            if (_applied)
                return;

            if (!IsRepoConfigLoaded())
            {
                LoggingService.LogVerbose(
                    "[Dread] REPOConfig not loaded; slider label compat skipped (use elytraking.dread.cfg)");
                return;
            }

            var menuApi = FindMenuApiType();
            if (menuApi == null)
                return;

            var postfix = new HarmonyMethod(typeof(RepoConfigSliderLabelCompat), nameof(AfterCreateSlider));
            var patched = 0;

            foreach (var method in AccessTools.GetDeclaredMethods(menuApi))
            {
                if (!string.Equals(method.Name, "CreateREPOSlider", StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length < 2
                    || parameters[0].ParameterType != typeof(string)
                    || parameters[1].ParameterType != typeof(string))
                {
                    continue;
                }

                harmony.Patch(method, postfix: postfix);
                patched++;
            }

            var repoSliderType = FindRepoSliderType();
            if (repoSliderType != null)
            {
                var handleDescription = AccessTools.Method(repoSliderType, "HandleDescription");
                if (handleDescription != null)
                {
                    harmony.Patch(
                        handleDescription,
                        postfix: new HarmonyMethod(
                            typeof(RepoConfigSliderLabelCompat),
                            nameof(AfterHandleDescription)));
                }
            }

            if (patched == 0)
                return;

            _applied = true;
            LoggingService.LogInfo($"[Dread] REPOConfig slider label compat active ({patched} hooks)");
        }

        internal static bool IsRepoConfigLoaded()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, "REPOConfig", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void AfterCreateSlider(object __result, string text, string description)
        {
            if (__result == null || string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(description))
                return;

            var sliderType = __result.GetType();
            var labelField = sliderType.GetField("labelTMP", BindingFlags.Instance | BindingFlags.Public);
            var descriptionField = sliderType.GetField("descriptionTMP", BindingFlags.Instance | BindingFlags.Public);
            var labelTmp = labelField?.GetValue(__result);
            var descriptionTmp = descriptionField?.GetValue(__result);

            SetTmpText(labelTmp, text);
            SetTmpText(descriptionTmp, string.Empty);
            SetGameObjectActive(descriptionTmp, false);
            SetLocalX(labelTmp, LabelColumnLocalX);
            ConfigureLabelForLeftColumn(labelTmp);
            ForceCompactSliderRow(__result);
        }

        private static void AfterHandleDescription(object __instance)
        {
            if (__instance == null)
                return;

            var sliderType = __instance.GetType();
            var descriptionField = sliderType.GetField("descriptionTMP", BindingFlags.Instance | BindingFlags.Public);
            var descriptionTmp = descriptionField?.GetValue(__instance);
            var labelField = sliderType.GetField("labelTMP", BindingFlags.Instance | BindingFlags.Public);
            var labelTmp = labelField?.GetValue(__instance);

            if (string.IsNullOrEmpty(GetTmpText(labelTmp)))
                return;

            if (string.IsNullOrEmpty(GetTmpText(descriptionTmp)))
            {
                SetTmpText(descriptionTmp, string.Empty);
                SetGameObjectActive(descriptionTmp, false);
                SetLocalX(labelTmp, LabelColumnLocalX);
                ConfigureLabelForLeftColumn(labelTmp);
                ForceCompactSliderRow(__instance);
            }
        }

        private static void ForceCompactSliderRow(object slider)
        {
            try
            {
                var sliderType = slider.GetType();
                var fillField = sliderType.GetField(
                    "backgroundFillRectTransform",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var outlineField = sliderType.GetField(
                    "backgroundOutlineRectTransform",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                SetSizeDelta(fillField?.GetValue(slider), 109.8f, 15f);
                SetSizeDelta(outlineField?.GetValue(slider), 108f, 15f);

                var scrollElementField = sliderType.GetField(
                    "repoScrollViewElement",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var scrollElement = scrollElementField?.GetValue(slider);
                if (scrollElement != null)
                {
                    var paddingField = scrollElement.GetType().GetField(
                        "bottomPadding",
                        BindingFlags.Instance | BindingFlags.Public);
                    paddingField?.SetValue(scrollElement, 1f);
                }
            }
            catch
            {
                // ignore layout reflection failures
            }
        }

        private static void SetSizeDelta(object? rectTransform, float width, float height)
        {
            if (rectTransform == null)
                return;

            var sizeDeltaProp = rectTransform.GetType().GetProperty(
                "sizeDelta",
                BindingFlags.Instance | BindingFlags.Public);
            if (sizeDeltaProp?.GetValue(rectTransform) is Vector2 size)
            {
                size.x = width;
                size.y = height;
                sizeDeltaProp.SetValue(rectTransform, size);
            }
        }

        private static void SetLocalX(object? target, float x)
        {
            if (target is not Component component)
                return;

            var pos = component.transform.localPosition;
            pos.x = x;
            component.transform.localPosition = pos;
        }

        private static void ConfigureLabelForLeftColumn(object? labelTmp)
        {
            if (labelTmp == null)
                return;

            try
            {
                var tmpType = labelTmp.GetType();
                var alignmentProp = tmpType.GetProperty("alignment", BindingFlags.Instance | BindingFlags.Public);
                if (alignmentProp != null && alignmentProp.PropertyType.IsEnum)
                {
                    alignmentProp.SetValue(
                        labelTmp,
                        Enum.Parse(alignmentProp.PropertyType, "Left"));
                }

                var overflowProp = tmpType.GetProperty("overflowMode", BindingFlags.Instance | BindingFlags.Public);
                if (overflowProp != null && overflowProp.PropertyType.IsEnum)
                {
                    foreach (var name in new[] { "Overflow", "Ellipsis", "Visible" })
                    {
                        try
                        {
                            overflowProp.SetValue(labelTmp, Enum.Parse(overflowProp.PropertyType, name));
                            break;
                        }
                        catch
                        {
                            // try next
                        }
                    }
                }

                if (labelTmp is Component component)
                {
                    var rect = component.transform;
                    var sizeDeltaProp = rect.GetType().GetProperty(
                        "sizeDelta",
                        BindingFlags.Instance | BindingFlags.Public);
                    if (sizeDeltaProp?.GetValue(rect) is Vector2 size)
                    {
                        size.x = Math.Max(size.x, 180f);
                        sizeDeltaProp.SetValue(rect, size);
                    }
                }
            }
            catch
            {
                // ignore layout tweaks
            }
        }

        private static void SetGameObjectActive(object? tmp, bool active)
        {
            if (tmp == null)
                return;

            try
            {
                object? go = tmp is Component component ? component.gameObject : null;
                if (go == null)
                    return;

                var setActive = go.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                setActive?.Invoke(go, new object[] { active });
            }
            catch
            {
                // ignore
            }
        }

        internal static Type? FindMenuApiType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, "MenuLib", StringComparison.Ordinal))
                    return assembly.GetType("MenuLib.MenuAPI");
            }

            return null;
        }

        private static Type? FindRepoSliderType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, "MenuLib", StringComparison.Ordinal))
                    return assembly.GetType("MenuLib.MonoBehaviors.REPOSlider");
            }

            return null;
        }

        private static string GetTmpText(object? tmp)
        {
            if (tmp == null)
                return "";

            var textProp = tmp.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            return textProp?.GetValue(tmp)?.ToString() ?? "";
        }

        private static void SetTmpText(object? tmp, string value)
        {
            if (tmp == null)
                return;

            var textProp = tmp.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            textProp?.SetValue(tmp, value);
        }
    }
}
