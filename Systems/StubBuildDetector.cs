using System;
using System.IO;

namespace Dread.Systems
{
    /// <summary>
    /// CI/Linux builds use compile stubs; some Dread IL (notably ErrorReporter) can throw BadImageFormatException at runtime.
    /// </summary>
    internal static class StubBuildDetector
    {
        public static bool IsStubBuild { get; private set; }

        public static void Initialize()
        {
            try
            {
                var dir = Path.GetDirectoryName(typeof(StubBuildDetector).Assembly.Location);
                if (string.IsNullOrEmpty(dir))
                    return;

                IsStubBuild = File.Exists(Path.Combine(dir, "stub-build.marker"));
            }
            catch
            {
                IsStubBuild = false;
            }
        }
    }
}
