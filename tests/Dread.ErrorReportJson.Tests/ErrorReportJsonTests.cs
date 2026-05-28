using System.Globalization;
using Dread.Systems;
using UnityEngine;
using Xunit;

namespace Dread.Tests.ErrorReportJson
{
    public class ErrorReportJsonTests
    {
        [Fact]
        public void SerializePayload_includes_reports_array_and_nested_fields()
        {
            var payload = new ErrorPayload
            {
                ModVersion = "1.5.2",
                GameVersion = "0.1",
                UnityVersion = "2022.3.67f2",
                Reports = new[]
                {
                    new ErrorReport
                    {
                        Hash = "abc123def4567890",
                        Timestamp = "2026-05-28T12:00:00.0000000Z",
                        Type = "exception",
                        ExceptionType = "InvalidOperationException",
                        Message = "[Dread TestCrash] deliberate test",
                        StackTrace = "TestCrashSystem.CrashSequence()",
                        Scene = "Warehouse",
                        GameState = new GameStateData
                        {
                            SceneName = "Warehouse",
                            PlayerHp = 80,
                            PlayerPosition = new Vector3(10.5f, 0f, -3.2f),
                            PlayTimeSeconds = 420
                        },
                        SystemInfo = new SystemInfoData { Os = "Linux", OsFamily = "Linux" },
                        Display = new DisplayInfoData { Width = 1920, Height = 1080 },
                        Config = new ConfigData { AudioEnabled = true, ErrorReportingEnabled = true }
                    }
                }
            };

            var json = Systems.ErrorReportJson.SerializePayload(payload);

            Assert.Contains("\"Reports\":[", json);
            Assert.Contains("\"Hash\":\"abc123def4567890\"", json);
            Assert.Contains("\"Scene\":\"Warehouse\"", json);
            Assert.Contains(
                "\"x\":" + 10.5f.ToString(CultureInfo.InvariantCulture),
                json);
            Assert.Contains("\"ErrorReportingEnabled\":true", json);
        }

        [Fact]
        public void SerializePayload_empty_reports_emits_empty_array()
        {
            var json = Systems.ErrorReportJson.SerializePayload(new ErrorPayload
            {
                ModVersion = "0.0.0",
                Reports = System.Array.Empty<ErrorReport>()
            });

            Assert.Contains("\"Reports\":[]", json);
        }

        [Fact]
        public void SerializePayload_escapes_special_characters_in_strings()
        {
            var json = Systems.ErrorReportJson.SerializePayload(new ErrorPayload
            {
                ModVersion = "1.0.0",
                Reports = new[]
                {
                    new ErrorReport
                    {
                        Hash = "hash\"1",
                        Message = "line1\nline2\t\"quoted\"",
                        StackTrace = "at Foo\\Bar()",
                        Scene = "Test\\Scene"
                    }
                }
            });

            Assert.Contains("\"Hash\":\"hash\\\"1\"", json);
            Assert.Contains("\"Message\":\"line1\\nline2\\t\\\"quoted\\\"\"", json);
            Assert.Contains("\"StackTrace\":\"at Foo\\\\Bar()\"", json);
            Assert.Contains("\"Scene\":\"Test\\\\Scene\"", json);
        }

        [Fact]
        public void SerializePayload_null_report_entry_does_not_throw()
        {
            var json = Systems.ErrorReportJson.SerializePayload(new ErrorPayload
            {
                ModVersion = "0.0.0",
                Reports = new ErrorReport[] { null! }
            });

            Assert.Contains("\"Reports\":[", json);
            Assert.Contains("\"Hash\":\"\"", json);
        }
    }
}
