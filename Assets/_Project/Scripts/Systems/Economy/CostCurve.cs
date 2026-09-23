using System;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    public enum CostCurveType
    {
        /// <summary>baseCost × growth^step — the default: every level costs a fixed % more.</summary>
        Exponential,
        /// <summary>baseCost + linearStep × step — gentle ramps (cheap consumables, early tiers).</summary>
        Linear,
        /// <summary>Hand-authored costs per step; steps past the end repeat the last entry.</summary>
        Table
    }

    /// <summary>
    /// Reusable "how much does the next level cost" formula (EPIC 15 "Upgrade costs"). Buildings,
    /// Shrine blessings and the Forge embed one of these instead of hard-coding cost math, so all
    /// meta-progression pacing is tuned in data and shares the same rounding rules. Other systems
    /// (heroes, pets, talents) can embed it the same way — it's a plain [Serializable] class that
    /// shows up as a nested block in the Inspector.
    ///
    /// <b>Step</b> is "how many upgrades were already bought": step 0 is the first purchase (build a
    /// building from level 0, or level 1 → 2 for things that start at level 1).
    /// </summary>
    [Serializable]
    public class CostCurve
    {
        [SerializeField] private CostCurveType type = CostCurveType.Exponential;
        [SerializeField] private CurrencyType currency = CurrencyType.Coins;
        [Tooltip("Cost of the first purchase (step 0).")]
        [SerializeField] private int baseCost = 100;
        [Tooltip("Exponential only: multiplier per step. 1.35 = each level costs 35% more than the previous one.")]
        [SerializeField] private float growth = 1.35f;
        [Tooltip("Linear only: added per step.")]
        [SerializeField] private int linearStep = 50;
        [Tooltip("Table only: cost per step. Steps past the end use the last value.")]
        [SerializeField] private int[] table = System.Array.Empty<int>();
        [Tooltip("Round every cost to a multiple of this (5 → 127 becomes 125). 1 = no rounding.")]
        [SerializeField] private int roundTo = 5;

        public CostCurve() { }

        public CostCurve(CostCurveType type, CurrencyType currency, int baseCost, float growth = 1.35f,
            int linearStep = 0, int roundTo = 5, int[] table = null)
        {
            this.type = type;
            this.currency = currency;
            this.baseCost = baseCost;
            this.growth = growth;
            this.linearStep = linearStep;
            this.roundTo = roundTo;
            this.table = table ?? System.Array.Empty<int>();
        }

        public CostCurveType Type => type;
        public CurrencyType Currency => currency;
        public int BaseCost => baseCost;
        public float Growth => growth;

        /// <summary>Cost of purchase number <paramref name="step"/> (0-based).</summary>
        public int Evaluate(int step)
        {
            switch (type)
            {
                case CostCurveType.Linear: return Linear(baseCost, linearStep, step, roundTo);
                case CostCurveType.Table: return FromTable(table, step, baseCost);
                default: return Exponential(baseCost, growth, step, roundTo);
            }
        }

        public Price PriceAt(int step) => new(currency, Evaluate(step));

        /// <summary>Sum of steps [<paramref name="fromStep"/>, <paramref name="toStepExclusive"/>) — for refunds and "total invested" UI.</summary>
        public long TotalCost(int fromStep, int toStepExclusive)
        {
            long total = 0;
            for (int s = Math.Max(0, fromStep); s < toStepExclusive; s++)
                total += Evaluate(s);
            return total;
        }

        // ---- Pure formulas (also used directly by tests and other systems) ----

        public static int Exponential(int baseCost, float growth, int step, int roundTo = 1)
        {
            if (baseCost <= 0) return 0;
            step = Math.Max(0, step);
            double value = baseCost * Math.Pow(Math.Max(0.0, growth), step);
            return Round(value, roundTo);
        }

        public static int Linear(int baseCost, int stepAmount, int step, int roundTo = 1)
        {
            step = Math.Max(0, step);
            double value = baseCost + (double)stepAmount * step;
            return value <= 0 ? 0 : Round(value, roundTo);
        }

        public static int FromTable(int[] costs, int step, int fallback = 0)
        {
            if (costs == null || costs.Length == 0) return Math.Max(0, fallback);
            step = Math.Clamp(step, 0, costs.Length - 1);
            return Math.Max(0, costs[step]);
        }

        /// <summary>Rounds to the nearest multiple of <paramref name="roundTo"/> (never below one multiple), clamped to int range.</summary>
        public static int Round(double value, int roundTo)
        {
            if (double.IsNaN(value) || value <= 0) return 0;
            if (value >= int.MaxValue) return int.MaxValue;
            roundTo = Math.Max(1, roundTo);
            double rounded = Math.Round(value / roundTo, MidpointRounding.AwayFromZero) * roundTo;
            if (rounded < roundTo) rounded = roundTo;
            return rounded >= int.MaxValue ? int.MaxValue : (int)rounded;
        }
    }
}
