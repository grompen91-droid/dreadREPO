using System;
using System.Collections.Generic;

namespace Dread.Systems.AudioAssets
{
    /// <summary>Maps feature category + file name to canonical manifest paths.</summary>
    internal static class AudioAssetPathResolver
    {
        private static readonly Dictionary<(string Category, string FileName), string> Map =
            new(StringTupleComparer.Instance)
            {
                { ("ambient_dread", "scraping.ogg"), "ambient_dread/scraping.ogg" },
                { ("ambient_dread", "whisper.ogg"), "ambient_dread/whisper.ogg" },
                { ("ambient_dread", "breathing.ogg"), "shared/breathing.ogg" },
                { ("ambient_dread", "footsteps.ogg"), "shared/footsteps.ogg" },
                { ("tension", "breathing.ogg"), "shared/breathing.ogg" },
                { ("tension", "breath2.ogg"), "tension/breath2.ogg" },
                { ("tension", "breath3.ogg"), "tension/breath3.ogg" },
                { ("tension", "footsteps.ogg"), "shared/footsteps.ogg" },
                { ("psychotic_break", "scream_peak.ogg"), "psychotic_break/scream_peak.ogg" },
                { ("psychotic_break", "scream_distant.ogg"), "psychotic_break/scream_distant.ogg" },
                { ("psychotic_break", "scream_threat.ogg"), "psychotic_break/scream_threat.ogg" },
                { ("psychotic_break", "footsteps.ogg"), "shared/footsteps.ogg" },
            };

        public static IEnumerable<string> CategoriesForManifestPath(string manifestPath)
        {
            foreach (var kv in Map)
            {
                if (string.Equals(kv.Value, manifestPath, StringComparison.OrdinalIgnoreCase))
                    yield return kv.Key.Category;
            }

            var slash = manifestPath.IndexOf('/');
            if (slash > 0)
            {
                var cat = manifestPath.Substring(0, slash);
                yield return cat;
            }
        }

        public static bool TryResolve(string category, string fileName, out string manifestPath)
        {
            manifestPath = "";
            if (Map.TryGetValue((category, fileName), out var path))
            {
                manifestPath = path;
                return true;
            }

            if (fileName.Contains("/") || fileName.Contains("\\"))
            {
                manifestPath = fileName.Replace('\\', '/');
                return true;
            }

            var combined = category.TrimEnd('/') + "/" + fileName;
            manifestPath = combined;
            return true;
        }

        private sealed class StringTupleComparer : IEqualityComparer<(string Category, string FileName)>
        {
            public static readonly StringTupleComparer Instance = new();

            public bool Equals((string Category, string FileName) x, (string Category, string FileName) y)
                => string.Equals(x.Category, y.Category, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string Category, string FileName) obj)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Category)
                       ^ (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FileName) << 1);
            }
        }
    }
}
