using System;
using UnityEngine.Networking;

namespace Dread.Systems
{
    /// <summary>
    /// Stub-built Dread.dll can reference empty UnityWebRequest bodies (zero RVA). Probe once and skip those paths.
    /// </summary>
    internal static class UnityWebRequestCompat
    {
        private static bool _probed;
        private static bool _usable;

        internal static bool IsUsable
        {
            get
            {
                if (!_probed)
                    Probe();
                return _usable;
            }
        }

        private static void Probe()
        {
            _probed = true;
            try
            {
                using var req = new UnityWebRequest("http://127.0.0.1/", "HEAD");
                _ = req.method;
                _usable = true;
            }
            catch (BadImageFormatException)
            {
                _usable = false;
                LoggingService.LogWarning(
                    "[Dread] UnityWebRequest unavailable (stub/zero-RVA build); using HTTP/NVorbis-only paths.");
            }
            catch (Exception ex)
            {
                _usable = false;
                LoggingService.LogVerbose($"[Dread] UnityWebRequest probe failed: {ex.Message}");
            }
        }
    }
}
