using System;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>JSON DTOs for error reporter worker (serialized via ErrorReportJson).</summary>
    [Serializable]
    internal class ErrorPayload
    {
        public string ModVersion = string.Empty;
        public string GameVersion = string.Empty;
        public string UnityVersion = string.Empty;
        public ErrorReport[] Reports = Array.Empty<ErrorReport>();
    }

    [Serializable]
    internal class ErrorReport
    {
        public string Hash = string.Empty;
        public string Timestamp = string.Empty;
        public string Type = string.Empty;
        public string ExceptionType = string.Empty;
        public string Message = string.Empty;
        public string StackTrace = string.Empty;
        public string Scene = string.Empty;
        public GameStateData GameState = new GameStateData();
        public SystemInfoData SystemInfo = new SystemInfoData();
        public DisplayInfoData Display = new DisplayInfoData();
        public ConfigData Config = new ConfigData();
    }

    [Serializable]
    internal class GameStateData
    {
        public string SceneName = string.Empty;
        public int EnemiesAlive;
        public int EnemiesTotal;
        public int EnemiesNearby;
        public int PlayerHp;
        public int PlayerMaxHp;
        public int PlayerStamina;
        public Vector3 PlayerPosition;
        public int PlayTimeSeconds;
    }

    [Serializable]
    internal class SystemInfoData
    {
        public string Os = string.Empty;
        public string OsFamily = string.Empty;
        public string Cpu = string.Empty;
        public int CpuCores;
        public int CpuFrequencyMHz;
        public int MemoryMB;
        public string Gpu = string.Empty;
        public string GpuVendor = string.Empty;
        public string GpuDriverVersion = string.Empty;
        public int GpuShaderLevel;
        public int VramMB;
        public string DeviceType = string.Empty;
        public string DeviceModel = string.Empty;
    }

    [Serializable]
    internal class DisplayInfoData
    {
        public int Width;
        public int Height;
        public int RefreshRate;
        public float Dpi;
        public string FullScreenMode = string.Empty;
    }

    [Serializable]
    internal class ConfigData
    {
        public bool AudioEnabled;
        public float AudioFrequency;
        public float AudioVolume;
        public bool AggressionEnabled;
        public bool AggressionAudioEnabled;
        public bool FakeFootsteps;
        public bool Adrenaline;
        public bool LowStaminaSound;
        public bool PanicSprint;
        public bool CrouchSpeedBoost;
        public bool ErrorReportingEnabled;
    }
}
