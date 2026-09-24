using System;
using UnityEngine;

namespace NinjaVillage.Systems.Farm
{
    public enum CropStage
    {
        Empty,
        Seed,
        Growing,
        Ripe,
    }

    public enum FarmResult
    {
        Success,
        PlotLocked,
        PlotBusy,
        PlotEmpty,
        CropLocked,
        NotEnoughCoins,
        NotRipe,
        AlreadyWatered,
        UnknownCrop,
    }

    /// <summary>
    /// Pure farm rules (unit-testable, no clock or save access): how many plots the castle level opens, a
    /// crop's stage at a given time, and what watering does to the time left.
    /// </summary>
    public static class FarmRules
    {
        public const int MaxPlots = 6;
        public const int StartingPlots = 2;
        /// <summary>Watering once cuts the time still left by this share.</summary>
        public const float WaterSpeedUp = 0.3f;
        /// <summary>Share of the growing time spent as a seed before it sprouts.</summary>
        public const float SeedShare = 0.3f;

        /// <summary>Two plots at Castle Lv 1, one more per castle level, up to <see cref="MaxPlots"/>.</summary>
        public static int UnlockedPlots(int castleLevel) => Mathf.Clamp(StartingPlots + Mathf.Max(0, castleLevel - 1), StartingPlots, MaxPlots);

        /// <summary>Castle level that opens plot <paramref name="plot"/> (0-based).</summary>
        public static int CastleLevelForPlot(int plot) => Mathf.Max(1, plot - StartingPlots + 2);

        public static CropStage Stage(DateTime now, long plantedTicks, long readyTicks)
        {
            if (readyTicks <= 0) return CropStage.Empty;
            if (now.Ticks >= readyTicks) return CropStage.Ripe;
            return Progress(now, plantedTicks, readyTicks) < SeedShare ? CropStage.Seed : CropStage.Growing;
        }

        /// <summary>0 when planted → 1 when ripe.</summary>
        public static float Progress(DateTime now, long plantedTicks, long readyTicks)
        {
            if (readyTicks <= plantedTicks) return 1f;
            return Mathf.Clamp01((float)((double)(now.Ticks - plantedTicks) / (readyTicks - plantedTicks)));
        }

        public static TimeSpan TimeLeft(DateTime now, long readyTicks) =>
            readyTicks > now.Ticks ? TimeSpan.FromTicks(readyTicks - now.Ticks) : TimeSpan.Zero;

        /// <summary>The new ripe time after watering at <paramref name="now"/>.</summary>
        public static long WaterReadyTicks(DateTime now, long readyTicks)
        {
            if (readyTicks <= now.Ticks) return readyTicks;
            long left = readyTicks - now.Ticks;
            return now.Ticks + (long)(left * (1.0 - WaterSpeedUp));
        }

        /// <summary>"4h 20m", "12m", "45s" — short enough for a label over a plot.</summary>
        public static string Format(TimeSpan time)
        {
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes:00}m";
            if (time.TotalMinutes >= 1) return $"{(int)time.TotalMinutes}m {time.Seconds:00}s";
            return $"{Math.Max(0, (int)Math.Ceiling(time.TotalSeconds))}s";
        }
    }
}
