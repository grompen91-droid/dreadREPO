using System.Globalization;
using System.Text;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>Manual JSON for error reporter (Unity JsonUtility omits Reports[] on nested DTOs).</summary>
    internal static class ErrorReportJson
    {
        public static string SerializePayload(ErrorPayload payload)
        {
            var sb = new StringBuilder(2048);
            sb.Append('{');
            AppendStringField(sb, "ModVersion", payload.ModVersion, first: true);
            AppendStringField(sb, "GameVersion", payload.GameVersion);
            AppendStringField(sb, "UnityVersion", payload.UnityVersion);
            sb.Append(",\"Reports\":[");
            var reports = payload.Reports;
            if (reports != null)
            {
                for (var i = 0; i < reports.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    AppendReport(sb, reports[i]);
                }
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendReport(StringBuilder sb, ErrorReport? report)
        {
            report ??= new ErrorReport();
            sb.Append('{');
            AppendStringField(sb, "Hash", report.Hash, first: true);
            AppendStringField(sb, "Timestamp", report.Timestamp);
            AppendStringField(sb, "Type", report.Type);
            AppendStringField(sb, "ExceptionType", report.ExceptionType);
            AppendStringField(sb, "Message", report.Message);
            AppendStringField(sb, "StackTrace", report.StackTrace);
            AppendStringField(sb, "Scene", report.Scene);
            sb.Append(",\"GameState\":");
            AppendGameState(sb, report.GameState);
            sb.Append(",\"SystemInfo\":");
            AppendSystemInfo(sb, report.SystemInfo);
            sb.Append(",\"Display\":");
            AppendDisplay(sb, report.Display);
            sb.Append(",\"Config\":");
            AppendConfig(sb, report.Config);
            sb.Append('}');
        }

        private static void AppendGameState(StringBuilder sb, GameStateData state)
        {
            state ??= new GameStateData();
            sb.Append('{');
            AppendStringField(sb, "SceneName", state.SceneName, first: true);
            AppendIntField(sb, "EnemiesAlive", state.EnemiesAlive);
            AppendIntField(sb, "EnemiesTotal", state.EnemiesTotal);
            AppendIntField(sb, "EnemiesNearby", state.EnemiesNearby);
            AppendIntField(sb, "PlayerHp", state.PlayerHp);
            AppendIntField(sb, "PlayerMaxHp", state.PlayerMaxHp);
            AppendIntField(sb, "PlayerStamina", state.PlayerStamina);
            sb.Append(",\"PlayerPosition\":");
            AppendVector3(sb, state.PlayerPosition);
            AppendIntField(sb, "PlayTimeSeconds", state.PlayTimeSeconds);
            sb.Append('}');
        }

        private static void AppendSystemInfo(StringBuilder sb, SystemInfoData info)
        {
            info ??= new SystemInfoData();
            sb.Append('{');
            AppendStringField(sb, "Os", info.Os, first: true);
            AppendStringField(sb, "OsFamily", info.OsFamily);
            AppendStringField(sb, "Cpu", info.Cpu);
            AppendIntField(sb, "CpuCores", info.CpuCores);
            AppendIntField(sb, "CpuFrequencyMHz", info.CpuFrequencyMHz);
            AppendIntField(sb, "MemoryMB", info.MemoryMB);
            AppendStringField(sb, "Gpu", info.Gpu);
            AppendStringField(sb, "GpuVendor", info.GpuVendor);
            AppendStringField(sb, "GpuDriverVersion", info.GpuDriverVersion);
            AppendIntField(sb, "GpuShaderLevel", info.GpuShaderLevel);
            AppendIntField(sb, "VramMB", info.VramMB);
            AppendStringField(sb, "DeviceType", info.DeviceType);
            AppendStringField(sb, "DeviceModel", info.DeviceModel);
            sb.Append('}');
        }

        private static void AppendDisplay(StringBuilder sb, DisplayInfoData display)
        {
            display ??= new DisplayInfoData();
            sb.Append('{');
            AppendIntField(sb, "Width", display.Width, first: true);
            AppendIntField(sb, "Height", display.Height);
            AppendIntField(sb, "RefreshRate", display.RefreshRate);
            AppendFloatField(sb, "Dpi", display.Dpi);
            AppendStringField(sb, "FullScreenMode", display.FullScreenMode);
            sb.Append('}');
        }

        private static void AppendConfig(StringBuilder sb, ConfigData config)
        {
            config ??= new ConfigData();
            sb.Append('{');
            AppendBoolField(sb, "AudioEnabled", config.AudioEnabled, first: true);
            AppendFloatField(sb, "AudioFrequency", config.AudioFrequency);
            AppendFloatField(sb, "AudioVolume", config.AudioVolume);
            AppendBoolField(sb, "AggressionEnabled", config.AggressionEnabled);
            AppendBoolField(sb, "AggressionAudioEnabled", config.AggressionAudioEnabled);
            AppendBoolField(sb, "FakeFootsteps", config.FakeFootsteps);
            AppendBoolField(sb, "Adrenaline", config.Adrenaline);
            AppendBoolField(sb, "LowStaminaSound", config.LowStaminaSound);
            AppendBoolField(sb, "PanicSprint", config.PanicSprint);
            AppendBoolField(sb, "CrouchSpeedBoost", config.CrouchSpeedBoost);
            AppendBoolField(sb, "ErrorReportingEnabled", config.ErrorReportingEnabled);
            sb.Append('}');
        }

        private static void AppendVector3(StringBuilder sb, Vector3 v)
        {
            sb.Append('{');
            AppendFloatField(sb, "x", v.x, first: true);
            AppendFloatField(sb, "y", v.y);
            AppendFloatField(sb, "z", v.z);
            sb.Append('}');
        }

        private static void AppendStringField(
            StringBuilder sb, string name, string? value, bool first = false)
        {
            if (!first)
                sb.Append(',');
            sb.Append('"').Append(name).Append("\":");
            AppendEscapedString(sb, value ?? string.Empty);
        }

        private static void AppendIntField(StringBuilder sb, string name, int value, bool first = false)
        {
            if (!first)
                sb.Append(',');
            sb.Append('"').Append(name).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendFloatField(StringBuilder sb, string name, float value, bool first = false)
        {
            if (!first)
                sb.Append(',');
            sb.Append('"').Append(name).Append("\":")
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendBoolField(StringBuilder sb, string name, bool value, bool first = false)
        {
            if (!first)
                sb.Append(',');
            sb.Append('"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendEscapedString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
        }
    }
}
