using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dread;

namespace Dread.Systems.AudioAssets
{
    internal static class AudioCachePaths
    {
        public static string PluginDirectory { get; } = GetPluginDirectory();

        private static string GetPluginDirectory()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(location))
                return "";
            return Path.GetDirectoryName(location) ?? "";
        }

        public static string CacheRoot => Path.Combine(PluginDirectory, "audio-cache");

        public static string VersionCacheDirectory => Path.Combine(CacheRoot, "v" + Plugin.VERSION);

        public static bool IsSafeManifestRelativePath(string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
                return false;
            if (Path.IsPathRooted(manifestPath))
                return false;

            foreach (var part in manifestPath.Split('/', '\\'))
            {
                if (part == "..")
                    return false;
            }

            return true;
        }

        public static string FilePathForManifestEntry(string manifestPath)
        {
            if (!TryFilePathForManifestEntry(manifestPath, out var fullPath))
                throw new ArgumentException($"Unsafe manifest path: {manifestPath}", nameof(manifestPath));
            return fullPath;
        }

        public static bool TryFilePathForManifestEntry(string manifestPath, out string fullPath)
        {
            fullPath = "";
            if (!IsSafeManifestRelativePath(manifestPath))
                return false;

            var relative = manifestPath.Replace('/', Path.DirectorySeparatorChar);
            var combined = Path.Combine(VersionCacheDirectory, relative);
            var rooted = Path.GetFullPath(combined);
            var cacheRoot = Path.GetFullPath(
                VersionCacheDirectory + Path.DirectorySeparatorChar);
            if (!rooted.StartsWith(cacheRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            fullPath = rooted;
            return true;
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
