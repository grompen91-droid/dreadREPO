using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dread;

namespace Dread.Systems.AudioAssets
{
    internal static class AudioCachePaths
    {
        public static string PluginDirectory { get; } =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        public static string CacheRoot => Path.Combine(PluginDirectory, "audio-cache");

        public static string VersionCacheDirectory => Path.Combine(CacheRoot, "v" + Plugin.VERSION);

        public static string FilePathForManifestEntry(string manifestPath)
        {
            var normalized = manifestPath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(VersionCacheDirectory, normalized);
        }

        public static void EnsureVersionDirectory()
        {
            Directory.CreateDirectory(VersionCacheDirectory);
        }

        public static IEnumerable<string> ListOtherVersionDirectories()
        {
            if (!Directory.Exists(CacheRoot))
                yield break;

            var current = Path.GetFileName(VersionCacheDirectory);
            foreach (var dir in Directory.GetDirectories(CacheRoot))
            {
                var name = Path.GetFileName(dir);
                if (string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    yield return dir;
            }
        }

        public static int CompareVersionFolderNames(string dirPathA, string dirPathB)
        {
            var a = ParseVersionFolder(Path.GetFileName(dirPathA));
            var b = ParseVersionFolder(Path.GetFileName(dirPathB));
            return a.CompareTo(b);
        }

        private static Version ParseVersionFolder(string folderName)
        {
            var s = folderName;
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1);
            return Version.TryParse(s, out var v) ? v : new Version(0, 0, 0);
        }
    }
}
