using UnityEngine;

namespace Dread.Systems
{
    internal enum PsychoticBreakAccentId
    {
        ScaryRed = 0,
        DeepCrimson = 1,
        SickGreen = 2,
        RotOlive = 3,
        BruiseViolet = 4,
        BlackenedMagenta = 5,
    }

    /// <summary>
    /// Horror-only edge accent colors for psychotic break overlay strips.
    /// </summary>
    internal static class PsychoticBreakAccentPalette
    {
        private static readonly Color[] Allowlist =
        {
            new Color(0.42f, 0.03f, 0.04f, 1f),
            new Color(0.32f, 0.01f, 0.02f, 1f),
            new Color(0.10f, 0.20f, 0.05f, 1f),
            new Color(0.18f, 0.16f, 0.04f, 1f),
            new Color(0.18f, 0.04f, 0.22f, 1f),
            new Color(0.28f, 0.02f, 0.14f, 1f),
        };

        private static readonly PsychoticBreakAccentId[] FallbackChain =
        {
            PsychoticBreakAccentId.ScaryRed,
            PsychoticBreakAccentId.DeepCrimson,
            PsychoticBreakAccentId.BruiseViolet,
            PsychoticBreakAccentId.BlackenedMagenta,
        };

        public static int AllowlistCount => Allowlist.Length;

        public static Color GetAllowlistColor(int index)
        {
            if (index < 0 || index >= Allowlist.Length)
                return Allowlist[0];
            return Allowlist[index];
        }

        public static Color GetById(PsychoticBreakAccentId id) =>
            GetAllowlistColor((int)id);

        public static Color GetFallback(int fallbackStep, ref int cursor)
        {
            int idx = fallbackStep >= 0
                ? fallbackStep % FallbackChain.Length
                : cursor % FallbackChain.Length;
            if (fallbackStep < 0)
                cursor++;
            return GetById(FallbackChain[idx]);
        }

        public static Color GetAttackPulseColor() => GetById(PsychoticBreakAccentId.ScaryRed);

        public static Color GetAttackPulseColorAlternate() => GetById(PsychoticBreakAccentId.DeepCrimson);
    }
}
