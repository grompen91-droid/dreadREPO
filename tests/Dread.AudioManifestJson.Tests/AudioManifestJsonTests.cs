using System;
using System.IO;
using Dread.Systems.AudioAssets;
using Xunit;

namespace Dread.Systems.AudioAssets.Tests
{
    public class AudioManifestJsonTests
    {
        [Fact]
        public void TryParse_repo_manifest_has_eleven_files()
        {
            var repoRoot = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
            var path = Path.Combine(repoRoot, "audio", "audio-manifest.json");
            Assert.True(File.Exists(path), $"Missing {path}");

            var json = File.ReadAllText(path);
            var ok = AudioManifestJson.TryParse(json, out var dto, out var error);

            Assert.True(ok, error);
            Assert.NotNull(dto);
            Assert.Equal(11, dto!.files.Length);
            Assert.Contains(dto.files, f => f.path == "shared/footsteps.ogg");
            Assert.Contains(dto.files, f => f.path == "ambient_dread/scraping.ogg");
        }

        [Fact]
        public void TryParse_unsupported_schema_fails()
        {
            const string json = "{\"schema\":2,\"modVersion\":\"1.0.0\",\"files\":[{\"path\":\"a.ogg\"}]}";
            var ok = AudioManifestJson.TryParse(json, out _, out var error);

            Assert.False(ok);
            Assert.Contains("schema", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryParse_empty_files_array_fails()
        {
            const string json = "{\"schema\":1,\"modVersion\":\"1.0.0\",\"files\":[]}";
            var ok = AudioManifestJson.TryParse(json, out _, out var error);

            Assert.False(ok);
            Assert.Contains("no files", error);
        }
    }
}
