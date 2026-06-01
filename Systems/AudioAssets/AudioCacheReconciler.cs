using System;
using System.Collections.Generic;
using System.IO;
using Dread.Config;
using Dread.Systems.Core;

namespace Dread.Systems.AudioAssets
{
    internal sealed class AudioCacheReconcileResult
    {
        public int AlreadyValid { get; set; }
        public int Imported { get; set; }
        public List<AudioManifestFile> NeedDownload { get; } = new();
    }

    internal static class AudioCacheReconciler
    {
        public static AudioCacheReconcileResult Reconcile(AudioManifest manifest)
        {
            var result = new AudioCacheReconcileResult();
            AudioCachePaths.EnsureVersionDirectory();

            var missing = new List<AudioManifestFile>();
            foreach (var entry in manifest.Files)
            {
                if (!AudioCachePaths.TryFilePathForManifestEntry(entry.Path, out var dest))
                    continue;
                if (AudioCacheValidator.IsValidOnDisk(dest, entry))
                {
                    result.AlreadyValid++;
                    continue;
                }

                missing.Add(entry);
            }

            if (missing.Count > 0)
            {
                TryImportFromDonors(manifest, missing, result);
#if DREAD_DEBUG
                TryImportFromBundledPluginAudio(missing, result);
#endif
            }

            foreach (var entry in missing)
            {
                if (!AudioCachePaths.TryFilePathForManifestEntry(entry.Path, out var dest))
                    continue;
                if (!AudioCacheValidator.IsValidOnDisk(dest, entry))
                    result.NeedDownload.Add(entry);
            }

            return result;
        }

        private static void TryImportFromDonors(
            AudioManifest manifest,
            List<AudioManifestFile> missing,
            AudioCacheReconcileResult result)
        {
            var donors = new List<string>();
            foreach (var dir in AudioCachePaths.ListOtherVersionDirectories())
                donors.Add(dir);

            donors.Sort((a, b) => AudioCachePaths.CompareVersionFolderNames(b, a));

            var stillMissing = new List<AudioManifestFile>();
            foreach (var entry in missing)
            {
                if (!AudioCachePaths.TryFilePathForManifestEntry(entry.Path, out var dest))
                    continue;
                if (TryCopyFromDonors(entry, dest, donors))
                {
                    result.Imported++;
                    continue;
                }

                stillMissing.Add(entry);
            }

            missing.Clear();
            missing.AddRange(stillMissing);
        }

#if DREAD_DEBUG
        private static void TryImportFromBundledPluginAudio(
            List<AudioManifestFile> missing,
            AudioCacheReconcileResult result)
        {
            var bundledRoot = Path.Combine(AudioCachePaths.PluginDirectory, "audio");
            if (!Directory.Exists(bundledRoot))
                return;

            var stillMissing = new List<AudioManifestFile>();
            foreach (var entry in missing)
            {
                if (!AudioCachePaths.IsSafeManifestRelativePath(entry.Path))
                {
                    stillMissing.Add(entry);
                    continue;
                }

                if (!AudioCachePaths.TryFilePathForManifestEntry(entry.Path, out var dest))
                {
                    stillMissing.Add(entry);
                    continue;
                }

                if (AudioCacheValidator.IsValidOnDisk(dest, entry))
                    continue;

                var donorPath = Path.Combine(
                    bundledRoot,
                    entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(donorPath))
                {
                    stillMissing.Add(entry);
                    continue;
                }

                if (!AudioCacheValidator.IsValidOnDisk(donorPath, entry))
                {
                    stillMissing.Add(entry);
                    continue;
                }

                try
                {
                    AudioCacheValidator.EnsureParentDirectory(dest);
                    File.Copy(donorPath, dest, overwrite: true);
                    if (AudioCacheValidator.IsValidOnDisk(dest, entry))
                    {
                        result.Imported++;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogVerbose(
                        $"[AudioAssets] Bundled import failed {entry.Path}: {ex.Message}");
                }

                stillMissing.Add(entry);
            }

            missing.Clear();
            missing.AddRange(stillMissing);
        }
#endif

        private static bool TryCopyFromDonors(
            AudioManifestFile entry,
            string destPath,
            List<string> donorDirs)
        {
            foreach (var donorRoot in donorDirs)
            {
                var donorPath = Path.Combine(
                    donorRoot,
                    entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(donorPath))
                    continue;

                if (!AudioCacheValidator.IsValidOnDisk(donorPath, entry))
                    continue;

                try
                {
                    AudioCacheValidator.EnsureParentDirectory(destPath);
                    File.Copy(donorPath, destPath, overwrite: true);
                    if (AudioCacheValidator.IsValidOnDisk(destPath, entry))
                        return true;
                }
                catch (Exception ex)
                {
                    LoggingService.LogVerbose($"[AudioAssets] Donor copy failed {entry.Path}: {ex.Message}");
                }
            }

            return false;
        }

        public static int PruneOtherVersionCaches()
        {
            if (DreadConfig.AudioAssetsKeepOtherCaches?.Value == true)
                return 0;

            var pruned = 0;
            foreach (var dir in AudioCachePaths.ListOtherVersionDirectories())
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    pruned++;
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning($"[AudioAssets] Failed to prune {dir}: {ex.Message}");
                }
            }

            if (pruned > 0)
                LoggingService.LogVerbose($"[AudioAssets] Pruned {pruned} old cache folder(s)");

            return pruned;
        }
    }
}
