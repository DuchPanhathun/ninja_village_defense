using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    public enum FishRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    /// <summary>Something that can bite at the pond.</summary>
    public readonly struct FishKind
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>Storehouse goods it becomes (null for the Golden Koi, which becomes a decoration).</summary>
        public readonly string GoodsId;
        public readonly FishRarity Rarity;
        public readonly float Weight;
        public readonly int RequiredCastleLevel;
        /// <summary>0 = easy (wide zone, slow marker) .. 1 = very hard.</summary>
        public readonly float Difficulty;

        public FishKind(string id, string name, string goodsId, FishRarity rarity, float weight, int castle, float difficulty)
        {
            Id = id;
            Name = name;
            GoodsId = goodsId;
            Rarity = rarity;
            Weight = weight;
            RequiredCastleLevel = castle;
            Difficulty = difficulty;
        }
    }

    public enum CatchResult
    {
        Miss,
        Catch,
        /// <summary>Right in the middle of the zone: two for one.</summary>
        Perfect,
    }

    /// <summary>What one trip to the mine brought up.</summary>
    public readonly struct MineHaul
    {
        public readonly int Iron, Gold, Mithril, Gems;

        public MineHaul(int iron, int gold, int mithril, int gems)
        {
            Iron = iron;
            Gold = gold;
            Mithril = mithril;
            Gems = gems;
        }

        public int Bars => Iron + Gold + Mithril;

        public override string ToString()
        {
            var parts = new List<string>();
            if (Iron > 0) parts.Add($"{Iron} iron");
            if (Gold > 0) parts.Add($"{Gold} gold");
            if (Mithril > 0) parts.Add($"{Mithril} mithril");
            if (Gems > 0) parts.Add($"{Gems} gem{(Gems == 1 ? "" : "s")}");
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Fishing pond and mine rules as pure functions (EPIC 24 Phase 5), kept free of assets and the save so EditMode
    /// tests pin them down. Fishing: casts refill over real time; a fish bites (rarer ones with a bigger castle),
    /// and a timing bar decides the catch — rarer fish have a narrower zone and a faster marker. Mine: bars pile up
    /// over time (the Mine building's level sets the rate), gold from Mine Lv 3 and mithril from Lv 5 by a fixed
    /// share, with gems now and then; bars can stand in for a missing copy when merging (GradeRules.BarsFor).
    /// </summary>
    public static class PondMineRules
    {
        // ------------------------------------------------------------------ fishing

        public const int MaxCasts = 5;
        public const float CastRegenMinutes = 20f;
        public const string KoiDecorationId = "golden_koi_pond";

        public static readonly FishKind[] Fish =
        {
            new("fish", "River Fish", "fish", FishRarity.Common, 50f, 1, 0.1f),
            new("shrimp", "Shrimp", "shrimp", FishRarity.Common, 30f, 1, 0.2f),
            new("calamari", "Squid", "calamari", FishRarity.Uncommon, 14f, 2, 0.45f),
            new("octopus", "Octopus", "octopus", FishRarity.Rare, 5f, 3, 0.7f),
            new("golden_koi", "Golden Koi", null, FishRarity.Legendary, 1.2f, 3, 0.9f),
        };

        /// <summary>Casts available at <paramref name="now"/> and the new refill start, refilling one per <see cref="CastRegenMinutes"/>.</summary>
        public static (int casts, long regenTicks) Casts(int stored, long regenTicks, DateTime now)
        {
            if (stored < 0 || stored >= MaxCasts) return (MaxCasts, now.Ticks); // never fished / full: the refill clock waits
            if (regenTicks <= 0) return (stored, now.Ticks);
            if (now.Ticks <= regenTicks) return (stored, regenTicks);           // a clock turned back earns nothing
            long step = TimeSpan.FromMinutes(CastRegenMinutes).Ticks;
            long earned = (now.Ticks - regenTicks) / step;
            int casts = (int)Math.Min(MaxCasts, stored + earned);
            return (casts, casts >= MaxCasts ? now.Ticks : regenTicks + earned * step);
        }

        public static TimeSpan UntilNextCast(int casts, long regenTicks, DateTime now)
        {
            if (casts >= MaxCasts) return TimeSpan.Zero;
            var next = new DateTime(regenTicks, DateTimeKind.Utc) + TimeSpan.FromMinutes(CastRegenMinutes);
            return next > now ? next - now : TimeSpan.Zero;
        }

        /// <summary>Which fish bites for a random <paramref name="roll01"/>, among those the castle lets in.</summary>
        public static FishKind PickFish(double roll01, int castleLevel)
        {
            float total = 0f;
            foreach (var fish in Fish)
                if (castleLevel >= fish.RequiredCastleLevel) total += fish.Weight;
            double pick = Math.Clamp(roll01, 0.0, 0.999999) * total;
            foreach (var fish in Fish)
            {
                if (castleLevel < fish.RequiredCastleLevel) continue;
                pick -= fish.Weight;
                if (pick < 0) return fish;
            }
            return Fish[0];
        }

        public static FishKind? GetFish(string id)
        {
            foreach (var fish in Fish)
                if (fish.Id == id) return fish;
            return null;
        }

        /// <summary>Width of the catch zone as a share of the bar.</summary>
        public static float ZoneWidth(float difficulty) => Mathf.Lerp(0.34f, 0.12f, Mathf.Clamp01(difficulty));

        /// <summary>Marker speed in bar-widths per second.</summary>
        public static float MarkerSpeed(float difficulty) => Mathf.Lerp(0.8f, 2.1f, Mathf.Clamp01(difficulty));

        /// <summary>Marker position 0..1 bouncing end to end at <paramref name="speed"/> after <paramref name="seconds"/>.</summary>
        public static float MarkerPosition(float seconds, float speed) => Mathf.PingPong(Mathf.Max(0f, seconds) * speed, 1f);

        public static CatchResult Judge(float marker, float zoneCenter, float zoneWidth)
        {
            float off = Mathf.Abs(marker - zoneCenter);
            if (off <= zoneWidth * 0.15f) return CatchResult.Perfect;
            return off <= zoneWidth * 0.5f ? CatchResult.Catch : CatchResult.Miss;
        }

        // ------------------------------------------------------------------ mine

        public const float MineCapHours = 8f;
        public const double GemsPerBar = 0.04;

        // Doubles, not floats: 100 × 0.08f is 7.9999998 and would round a mithril bar away.
        public static double GoldShare(int mineLevel) => mineLevel >= 3 ? 0.2 : 0.0;
        public static double MithrilShare(int mineLevel) => mineLevel >= 5 ? 0.08 : 0.0;

        public static int Capacity(float barsPerHour) => Mathf.RoundToInt(Mathf.Max(0f, barsPerHour) * MineCapHours);

        /// <summary>
        /// Splits <paramref name="bars"/> into iron / gold / mithril and finds gems, carrying the fractions over so
        /// the shares come out exact over time (and nothing is random).
        /// </summary>
        public static MineHaul Split(int bars, int mineLevel, ref double goldCarry, ref double mithrilCarry, ref double gemCarry)
        {
            if (bars <= 0) return default;
            goldCarry += bars * GoldShare(mineLevel);
            mithrilCarry += bars * MithrilShare(mineLevel);
            gemCarry += bars * GemsPerBar;
            int mithril = Math.Min(bars, (int)Math.Floor(mithrilCarry + 1e-6));
            mithrilCarry = Math.Max(0.0, mithrilCarry - mithril);
            int gold = Math.Min(bars - mithril, (int)Math.Floor(goldCarry + 1e-6));
            goldCarry = Math.Max(0.0, goldCarry - gold);
            int gems = (int)Math.Floor(gemCarry + 1e-6);
            gemCarry = Math.Max(0.0, gemCarry - gems);
            return new MineHaul(bars - gold - mithril, gold, mithril, gems);
        }
    }
}
