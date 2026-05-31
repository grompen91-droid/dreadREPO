using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Dread.Systems.Core;

namespace Dread.Systems.AudioAssets
{
    internal static class AudioAssetDownloader
    {
        private const int MaxAttempts = 3;

        internal sealed class DownloadResult
        {
            public bool Success { get; set; }
            public long BytesReceived { get; set; }
            public double ElapsedSeconds { get; set; }
            public string Error { get; set; } = "";
        }

        public static DownloadResult TryDownload(AudioManifest manifest, AudioManifestFile entry, string destPath)
        {
            var url = entry.DownloadUrl(manifest);
            var tmp = destPath + ".tmp";
            AudioCacheValidator.EnsureParentDirectory(destPath);

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    if (File.Exists(tmp))
                        File.Delete(tmp);

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = 60000;
                    request.ReadWriteTimeout = 60000;

                    using var response = (HttpWebResponse)request.GetResponse();
                    using var responseStream = response.GetResponseStream();
                    if (responseStream == null)
                    {
                        return Fail(sw, "Empty response stream");
                    }

                    using (var file = File.Create(tmp))
                    {
                        var buffer = new byte[8192];
                        int read;
                        long total = 0;
                        while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            file.Write(buffer, 0, read);
                            total += read;
                        }

                        sw.Stop();
                        file.Flush();
                        if (File.Exists(destPath))
                            File.Delete(destPath);
                        File.Move(tmp, destPath);

                        if (!AudioCacheValidator.IsValidOnDisk(destPath, entry))
                        {
                            return new DownloadResult
                            {
                                Success = false,
                                BytesReceived = total,
                                ElapsedSeconds = sw.Elapsed.TotalSeconds,
                                Error = "Downloaded file failed validation",
                            };
                        }

                        return new DownloadResult
                        {
                            Success = true,
                            BytesReceived = total,
                            ElapsedSeconds = Math.Max(sw.Elapsed.TotalSeconds, 0.001),
                        };
                    }
                }
                catch (WebException ex)
                {
                    sw.Stop();
                    var msg = ex.Message;
                    if (ex.Response is HttpWebResponse err)
                        msg = $"HTTP {(int)err.StatusCode}: {ex.Message}";
                    if (attempt == MaxAttempts)
                        return Fail(sw, msg);
                    LoggingService.LogVerbose(
                        $"[AudioAssets] Download attempt {attempt} failed for {entry.Path}: {msg}");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    if (attempt == MaxAttempts)
                        return Fail(sw, ex.Message);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tmp))
                            File.Delete(tmp);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            return Fail(Stopwatch.StartNew(), "Exhausted retries");
        }

        private static DownloadResult Fail(Stopwatch sw, string error)
        {
            sw.Stop();
            return new DownloadResult
            {
                Success = false,
                ElapsedSeconds = sw.Elapsed.TotalSeconds,
                Error = error,
            };
        }
    }
}
