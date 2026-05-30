using System;
using System.Collections.Generic;

namespace Dread.Systems
{
    internal enum SystemOrderGroup
    {
        Core,
        Debug,
    }

    internal sealed class SystemRegistration
    {
        public SystemRegistration(
            string id,
            Type systemType,
            string hostName,
            SystemOrderGroup orderGroup,
            Func<bool>? isEnabled = null)
        {
            Id = id;
            SystemType = systemType;
            HostName = hostName;
            OrderGroup = orderGroup;
            IsEnabled = isEnabled;
        }

        public string Id { get; }
        public Type SystemType { get; }
        public string HostName { get; }
        public SystemOrderGroup OrderGroup { get; }
        public Func<bool>? IsEnabled { get; }
    }

    /// <summary>
    /// Explicit registration list for runtime systems.
    /// See specs/002-arch-3-extensible-core/contracts/extension-registry.md.
    /// </summary>
    internal static class DreadSystemRegistry
    {
        public static IReadOnlyList<SystemRegistration> Registrations { get; } =
        [
            new SystemRegistration(
                "audio-dread",
                typeof(AudioDreadSystem),
                "DreadAudioHost",
                SystemOrderGroup.Core),
            new SystemRegistration(
                "monster-overhaul",
                typeof(MonsterOverhaulSystem),
                "DreadMonsterHost",
                SystemOrderGroup.Core),
            new SystemRegistration(
                "tension",
                typeof(TensionSystem),
                "DreadTensionHost",
                SystemOrderGroup.Core),
            new SystemRegistration(
                "error-reporter",
                typeof(ErrorReporterSystem),
                "DreadErrorHost",
                SystemOrderGroup.Core),
            new SystemRegistration(
                "psychotic-break",
                typeof(PsychoticBreakSystem),
                "DreadPsychoticBreakHost",
                SystemOrderGroup.Core),
            new SystemRegistration(
                "test-crash",
                typeof(TestCrashSystem),
                "DreadTestCrashHost",
                SystemOrderGroup.Debug),
            new SystemRegistration(
                "debug-server",
                typeof(DebugServerSystem),
                "DreadDebugHost",
                SystemOrderGroup.Debug),
            new SystemRegistration(
                "debug-overlay",
                typeof(DebugOverlaySystem),
                "DreadDebugOverlayHost",
                SystemOrderGroup.Debug),
        ];
    }
}
