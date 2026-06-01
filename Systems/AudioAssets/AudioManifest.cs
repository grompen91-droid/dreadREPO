using System;
using System.IO;
using System.Reflection;
using Dread;
using Dread.Systems.Core;

namespace Dread.Systems.AudioAssets
{
    internal sealed class AudioManifestFile
    {
        public string Path { get; }
        public string AssetName { get; }
        public int SizeBytes { get; }
        public int Priority { get; }
        public string Sha256 { get; }

        public AudioManifestFile(AudioManifestFileDto dto)
        {
            Path = dto.path ?? "";
            AssetName = string.IsNullOrEmpty(dto.assetName)
                ? Path.Replace('/', '_').Replace('\\', '_')
                : dto.assetName;
            SizeBytes = dto.sizeBytes;
            Priority = dto.priority;
            Sha256 = (dto.sha256 ?? "").Trim().ToLowerInvariant();
        }

        public string DownloadUrl(AudioManifest manifest)
            => manifest.BaseUrl.TrimEnd('/') + "/" + AssetName;
    }

    internal sealed class AudioManifest
    {
        public int Schema { get; }
        public string ModVersion { get; }
        public string ReleaseTag { get; }
        public string BaseUrl { get; }
        public AudioManifestFile[] Files { get; }

        private AudioManifest(AudioManifestDto dto)
        {
            Schema = dto.schema;
            ModVersion = dto.modVersion ?? "";
            ReleaseTag = dto.releaseTag ?? "";
            BaseUrl = dto.baseUrl ?? "";
            var raw = dto.files ?? Array.Empty<AudioManifestFileDto>();
            Files = new AudioManifestFile[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                Files[i] = new AudioManifestFile(raw[i]);
        }

        public static bool TryLoad(out AudioManifest? manifest, out string error)
        {
            manifest = null;
            error = "";
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                const string resourceName = "Dread.audio.audio-manifest.json";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    error = $"Embedded resource not found: {resourceName}";
                    return false;
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                if (!AudioManifestJson.TryParse(json, out var dto, out error) || dto == null)
                    return false;

                if (dto.modVersion != Plugin.VERSION)
                {
                    LoggingService.LogWarning(
                        $"[AudioAssets] Manifest modVersion {dto.modVersion} != Plugin.VERSION {Plugin.VERSION}");
                }

                manifest = new AudioManifest(dto);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public AudioManifestFile? FindByPath(string path)
        {
            foreach (var f in Files)
            {
                if (string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase))
                    return f;
            }

            return null;
        }
    }
}
