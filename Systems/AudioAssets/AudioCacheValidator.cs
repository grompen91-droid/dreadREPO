using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dread.Systems.AudioAssets
{
    internal static class AudioCacheValidator
    {
        public static bool IsValidOnDisk(string filePath, AudioManifestFile entry)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                var info = new FileInfo(filePath);
                if (entry.SizeBytes > 0 && info.Length != entry.SizeBytes)
                    return false;

                if (string.IsNullOrEmpty(entry.Sha256))
                    return entry.SizeBytes <= 0 || info.Length == entry.SizeBytes;

                return string.Equals(ComputeSha256Hex(filePath), entry.Sha256, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string ComputeSha256Hex(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static void EnsureParentDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
