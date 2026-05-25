using System;
using System.Reflection;

namespace Dread.Systems
{
    /// <summary>
    /// Detects when BepInEx ConfigurationManager is open so IMGUI overlays can yield.
    /// Does not reference MenuLib or REPOConfig (no hard dependency, no TypeByName spam).
    /// </summary>
    internal static class ConfigUiDetector
    {
        private const int Unresolved = 0;
        private const int Resolved = 1;
        private const int Unavailable = 2;

        private static int _resolveState;
        private static PropertyInfo?[] _openProps = Array.Empty<PropertyInfo?>();

        public static bool IsConfigurationManagerOpen()
        {
            EnsureResolved();
            if (_resolveState != Resolved || _openProps.Length == 0)
                return false;

            try
            {
                foreach (var prop in _openProps)
                {
                    if (prop?.GetValue(null, null) is bool open && open)
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static void EnsureResolved()
        {
            if (_resolveState != Unresolved)
                return;

            var type = FindLoadedType("ConfigurationManager.ConfigurationManager");
            if (type == null)
            {
                _resolveState = Unavailable;
                return;
            }

            var names = new[] { "DisplayingWindow", "DisplayingSearch", "Shown" };
            var props = new PropertyInfo?[names.Length];
            int found = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var prop = type.GetProperty(
                    names[i],
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                props[i] = prop;
                if (prop != null)
                    found++;
            }

            _openProps = props;
            _resolveState = found > 0 ? Resolved : Unavailable;
        }

        private static Type? FindLoadedType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, throwOnError: false);
                    if (t != null)
                        return t;
                }
                catch
                {
                    // ignore broken assemblies
                }
            }

            return null;
        }
    }
}
