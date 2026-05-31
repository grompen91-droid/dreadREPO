using System;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Derives psychotic break poll interval, LoS-lost delay, and per-roll probability from
    /// <see cref="Dread.Config.DreadConfig.PsychoticBreakChancePercent"/> (target window chance).
    /// </summary>
    internal readonly struct PsychoticBreakTriggerTuning
    {
        internal const float ThreatWindowSeconds = 30f;
        internal const float HidingMinSeconds = 2f;

        private const float CalibratedLosDelay = 3f;
        private const float CalibratedCheckInterval = 8f;
        private const float CalibratedPerRoll = 0.003f;
        private const float CalibratedWindowChance = 0.00897f;

        private const float LosMin = 1.5f;
        private const float LosMax = 10f;
        private const float CheckMin = 4f;
        private const float CheckMax = 14f;
        private const float PerRollMin = 0.0003f;
        private const float PerRollMax = 0.15f;

        public float LosLostDelaySeconds { get; }
        public float CheckIntervalSeconds { get; }
        public float PerRollProbability { get; }
        public float TargetWindowChance { get; }
        public float EstimatedWindowChance { get; }
        public int RollsPerFullWindow { get; }

        public static PsychoticBreakTriggerTuning Compute(float chancePercent)
        {
            float target = Mathf.Clamp(chancePercent / 100f, 0.001f, 0.25f);
            float r = Mathf.Clamp(target / CalibratedWindowChance, 0.1f, 40f);

            float los = Mathf.Clamp(CalibratedLosDelay / Mathf.Pow(r, 0.25f), LosMin, LosMax);
            float check = Mathf.Clamp(CalibratedCheckInterval / Mathf.Pow(r, 0.35f), CheckMin, CheckMax);
            int n = Math.Max(1, (int)Math.Floor((ThreatWindowSeconds - los - HidingMinSeconds) / check));

            float p = 1f - Mathf.Pow(1f - target, 1f / n);
            p = Mathf.Clamp(p, PerRollMin, PerRollMax);

            float estimated = 1f - Mathf.Pow(1f - p, n);
            if (estimated < target - 0.001f && p >= PerRollMax - 0.0001f && check > CheckMin)
            {
                check = Mathf.Max(CheckMin, check - 1f);
                n = Math.Max(1, (int)Math.Floor((ThreatWindowSeconds - los - HidingMinSeconds) / check));
                p = 1f - Mathf.Pow(1f - target, 1f / n);
                p = Mathf.Clamp(p, PerRollMin, PerRollMax);
                estimated = 1f - Mathf.Pow(1f - p, n);
            }

            return new PsychoticBreakTriggerTuning(los, check, p, target, estimated, n);
        }

        private PsychoticBreakTriggerTuning(
            float los,
            float check,
            float perRoll,
            float target,
            float estimated,
            int rolls)
        {
            LosLostDelaySeconds = los;
            CheckIntervalSeconds = check;
            PerRollProbability = perRoll;
            TargetWindowChance = target;
            EstimatedWindowChance = estimated;
            RollsPerFullWindow = rolls;
        }
    }
}
