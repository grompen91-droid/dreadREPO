using System;
using System.Reflection;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Toggle key polling compatible with legacy Input and Unity Input System (REPO default).
    /// </summary>
    internal static class DebugOverlayInput
    {
        private static bool _legacyInputUnavailable;
        private static bool _inputSystemResolved;
        private static bool _inputSystemAvailable;
        private static Type? _keyboardType;
        private static Type? _keyEnumType;
        private static MethodInfo? _inputSystemUpdate;
        private static PropertyInfo? _keyboardCurrent;
        private static readonly System.Collections.Generic.Dictionary<string, MemberInfo?> KeyMembers = new(StringComparer.OrdinalIgnoreCase);

        public static bool WasTogglePressedThisFrame(KeyCode keyCode, string keyName)
        {
            if (WasLegacyKeyDown(keyCode))
            {
                LogKeyHit("legacy", keyName);
                return true;
            }

            if (WasNewInputSystemKeyPressed(keyName))
            {
                LogKeyHit("input_system", keyName);
                return true;
            }

            if (WasImGuiKeyDown(keyCode))
            {
                LogKeyHit("imgui", keyName);
                return true;
            }

            return false;
        }

        public static bool WasImGuiKeyDown(KeyCode keyCode)
        {
            try
            {
                var e = Event.current;
                return e != null && e.type == EventType.KeyDown && e.keyCode == keyCode;
            }
            catch
            {
                return false;
            }
        }

        public static KeyCode ParseKeyCode(string name, KeyCode fallback = KeyCode.F10)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            try
            {
                return (KeyCode)Enum.Parse(typeof(KeyCode), name.Trim(), ignoreCase: true);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool WasLegacyKeyDown(KeyCode keyCode)
        {
            if (_legacyInputUnavailable)
                return false;

            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch (InvalidOperationException)
            {
                _legacyInputUnavailable = true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool WasNewInputSystemKeyPressed(string keyName)
        {
            try
            {
                EnsureInputSystemResolved();
                if (!_inputSystemAvailable || _keyboardType == null || _keyboardCurrent == null)
                    return false;

                _inputSystemUpdate?.Invoke(null, null);

                var keyboard = _keyboardCurrent.GetValue(null);
                if (keyboard == null)
                    return false;

                var memberName = ToInputSystemKeyMember(keyName);
                if (memberName == null)
                    return false;

                if (!KeyMembers.TryGetValue(memberName, out var member))
                {
                    member = (MemberInfo?)_keyboardType.GetProperty(memberName)
                        ?? _keyboardType.GetField(memberName);
                    KeyMembers[memberName] = member;
                }

                if (member == null)
                    return TryKeyboardIndexer(keyboard, keyName);

                object? keyControl = member switch
                {
                    PropertyInfo prop => prop.GetValue(keyboard),
                    FieldInfo field => field.GetValue(keyboard),
                    _ => null
                };

                if (keyControl == null)
                    return TryKeyboardIndexer(keyboard, keyName);

                var pressedProp = keyControl.GetType().GetProperty("wasPressedThisFrame");
                if (pressedProp == null)
                    return false;

                return pressedProp.GetValue(keyControl) is bool pressed && pressed;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryKeyboardIndexer(object keyboard, string keyName)
        {
            if (_keyEnumType == null || _keyboardType == null)
                return false;

            try
            {
                object keyValue;
                try
                {
                    keyValue = Enum.Parse(_keyEnumType, keyName.Trim(), ignoreCase: true);
                }
                catch
                {
                    return false;
                }

                var indexer = _keyboardType.GetProperty("Item", new[] { _keyEnumType });
                if (indexer == null)
                    return false;

                var keyControl = indexer.GetValue(keyboard, new object[] { keyValue });
                if (keyControl == null)
                    return false;

                var pressedProp = keyControl.GetType().GetProperty("wasPressedThisFrame");
                return pressedProp?.GetValue(keyControl) is bool pressed && pressed;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureInputSystemResolved()
        {
            if (_inputSystemResolved)
                return;

            _inputSystemResolved = true;

            try
            {
                _keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, UnityEngine.InputSystem");
                _keyEnumType = Type.GetType("UnityEngine.InputSystem.Key, UnityEngine.InputSystem");
                if (_keyboardType == null)
                    return;

                _keyboardCurrent = _keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                var inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, UnityEngine.InputSystem");
                _inputSystemUpdate = inputSystemType?.GetMethod(
                    "Update",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);

                _inputSystemAvailable = _keyboardCurrent != null;
            }
            catch
            {
                _inputSystemAvailable = false;
            }
        }

        private static string? ToInputSystemKeyMember(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                return null;

            var normalized = keyName.Trim();
            if (normalized.Length >= 2
                && normalized[0] == 'F'
                && int.TryParse(normalized.Substring(1), out var fn)
                && fn >= 1
                && fn <= 12)
            {
                return $"f{fn}Key";
            }

            if (normalized.Length == 1)
                return $"{char.ToLowerInvariant(normalized[0])}Key";

            return $"{char.ToLowerInvariant(normalized[0])}{normalized.Substring(1)}Key";
        }

        private static void LogKeyHit(string source, string keyName)
        {
            // #region agent log
            DebugAgentLog.Write(
                "E",
                "DebugOverlayInput.cs:WasTogglePressedThisFrame",
                "key_down",
                "post-fix",
                ("source", source),
                ("keyName", keyName),
                ("frame", Time.frameCount));
            // #endregion
        }
    }
}
