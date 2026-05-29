using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Dread.Systems
{
    internal static class ErrorReportUploader
    {
        internal const string WorkerUrl = "https://dread-error-reporter.nox-heights.workers.dev/api/report";
        internal const int MaxBatchSize = 50;

        internal static bool TryPostPayloadSync(ErrorPayload payload, out string responseBody, out string error)
        {
            responseBody = string.Empty;
            error = string.Empty;
            try
            {
                var json = ErrorReportJson.SerializePayload(payload);
                if (string.IsNullOrEmpty(json) || json.IndexOf("\"Reports\":[", StringComparison.Ordinal) < 0)
                {
                    error = $"JSON serializer produced invalid payload (length={json?.Length ?? 0})";
                    return false;
                }

                var request = (HttpWebRequest)WebRequest.Create(WorkerUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 15000;
                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(bytes, 0, bytes.Length);

                using var response = (HttpWebResponse)request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null);
                responseBody = reader.ReadToEnd();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errResponse)
                {
                    using var reader = new StreamReader(errResponse.GetResponseStream() ?? Stream.Null);
                    responseBody = reader.ReadToEnd();
                    error = $"HTTP {(int)errResponse.StatusCode}: {responseBody}";
                }
                else
                {
                    error = ex.Message;
                }

                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static List<ErrorReport> CollectFailedReports(string body, List<ErrorReport> batch)
        {
            var failed = new List<ErrorReport>();
            foreach (var report in batch)
            {
                if (IsReportFailedInResponse(body, report.Hash))
                    failed.Add(report);
            }

            return failed;
        }

        internal static bool HasWorkerReportFailures(string body, ErrorReport[] reports)
        {
            if (string.IsNullOrEmpty(body))
                return false;

            foreach (var report in reports)
            {
                if (report != null && IsReportFailedInResponse(body, report.Hash))
                    return true;
            }

            return HasUnmappedWorkerErrors(body);
        }

        internal static bool HasUnmappedWorkerErrors(string body)
        {
            return body.IndexOf("\"status\":\"error\"", StringComparison.Ordinal) >= 0
                || body.IndexOf("\"status\": \"error\"", StringComparison.Ordinal) >= 0;
        }

        internal static bool IsReportFailedInResponse(string body, string hash)
        {
            if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(hash))
                return false;

            var hashNeedle = "\"hash\":\"" + hash + "\"";
            var idx = 0;
            while ((idx = body.IndexOf(hashNeedle, idx, StringComparison.Ordinal)) >= 0)
            {
                var windowEnd = Math.Min(body.Length, idx + 256);
                var slice = body.Substring(idx, windowEnd - idx);
                if (slice.IndexOf("\"status\":\"error\"", StringComparison.Ordinal) >= 0
                    || slice.IndexOf("\"status\": \"error\"", StringComparison.Ordinal) >= 0)
                    return true;

                idx += hashNeedle.Length;
            }

            return false;
        }

        internal static string? ValidateBatchJson(string json)
        {
            if (json.IndexOf("\"Reports\":[", StringComparison.Ordinal) < 0)
                return $"JSON missing Reports (len={json.Length}); re-queuing batch.";
            return null;
        }

        internal static byte[] EncodePayload(ErrorPayload payload, out string json)
        {
            json = ErrorReportJson.SerializePayload(payload);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
