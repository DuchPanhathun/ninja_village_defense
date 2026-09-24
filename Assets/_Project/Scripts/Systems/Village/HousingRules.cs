using System;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>A look a house can be built (or repainted) in.</summary>
    public readonly struct HouseStyle
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>Environment/Village sprite name (building_...).</summary>
        public readonly string Sprite;
        public readonly int RequiredCastleLevel;

        public HouseStyle(string id, string name, string sprite, int requiredCastleLevel)
        {
            Id = id;
            Name = name;
            Sprite = sprite;
            RequiredCastleLevel = requiredCastleLevel;
        }
    }

    /// <summary>
    /// Treasury and house rules as pure functions (EPIC 24 Phase 4), kept free of assets and the save so
    /// EditMode tests pin them down. The treasury fills with coins over real time — faster with a bigger castle
    /// and more families — up to a few hours' worth. Each castle level opens one more house plot; houses grow to
    /// Lv 3 (more with a bigger castle), and every level houses a family that pays into the treasury.
    /// </summary>
    public static class HousingRules
    {
        // ------------------------------------------------------------------ treasury

        public const float BaseCoinsPerHour = 30f;
        public const float CoinsPerHourPerCastleLevel = 15f;
        public const float CoinsPerHourPerHouseLevel = 15f;
        public const float BaseCapHours = 6f;
        public const float CapHoursPerCastleLevel = 2f;

        public static float CoinsPerHour(int castleLevel, int houseLevels) =>
            BaseCoinsPerHour + CoinsPerHourPerCastleLevel * (Mathf.Max(1, castleLevel) - 1) + CoinsPerHourPerHouseLevel * Mathf.Max(0, houseLevels);

        public static float CapHours(int castleLevel) => BaseCapHours + CapHoursPerCastleLevel * (Mathf.Max(1, castleLevel) - 1);

        /// <summary>The most the treasury holds before it stops filling.</summary>
        public static int Capacity(int castleLevel, int houseLevels) =>
            Mathf.RoundToInt(CoinsPerHour(castleLevel, houseLevels) * CapHours(castleLevel));

        /// <summary>
        /// Coins held at <paramref name="now"/>: what was stored plus what flowed in since <paramref name="lastTicks"/>,
        /// never over <paramref name="capacity"/>. A clock that went backwards adds nothing.
        /// </summary>
        public static double Accrued(double stored, long lastTicks, DateTime now, float coinsPerHour, int capacity)
        {
            if (lastTicks <= 0) return Math.Min(stored, capacity);
            double hours = Math.Max(0.0, (now.Ticks - lastTicks) / (double)TimeSpan.TicksPerHour);
            return Math.Min(capacity, Math.Max(0.0, stored) + hours * coinsPerHour);
        }

        /// <summary>How long until the treasury is full (zero when it already is).</summary>
        public static TimeSpan UntilFull(double accrued, float coinsPerHour, int capacity)
        {
            if (accrued >= capacity || coinsPerHour <= 0f) return TimeSpan.Zero;
            return TimeSpan.FromHours((capacity - accrued) / coinsPerHour);
        }

        // ------------------------------------------------------------------ houses

        public const int MaxPlots = 6;
        public const int MaxLevel = 3;

        public static readonly HouseStyle[] Styles =
        {
            new("tan", "Tan Cottage", "building_house_tan", 1),
            new("clay", "Clay House", "building_house_adobe", 1),
            new("orange", "Orange Cottage", "building_house_orange_b", 1),
            new("straw", "Straw Hut", "building_hut_straw", 1),
            new("timber", "Timber House", "building_house_timber", 2),
            new("tall", "Tall House", "building_house_tall", 3),
            new("igloo", "Snow Igloo", "building_igloo", 4),
        };

        public static HouseStyle? Style(string id)
        {
            foreach (var style in Styles)
                if (style.Id == id) return style;
            return null;
        }

        /// <summary>One plot per castle level, up to <see cref="MaxPlots"/>.</summary>
        public static int UnlockedPlots(int castleLevel) => Mathf.Clamp(castleLevel, 1, MaxPlots);

        public static int CastleLevelForPlot(int plot) => Mathf.Max(1, plot + 1);

        /// <summary>Highest house level the castle allows: Lv 1, Lv 2 from Castle 3, Lv 3 from Castle 5.</summary>
        public static int LevelCap(int castleLevel) => Mathf.Clamp(1 + (Mathf.Max(1, castleLevel) - 1) / 2, 1, MaxLevel);

        public static int CastleLevelForHouseLevel(int level) => Mathf.Max(1, 2 * (level - 1) + 1);

        private static readonly int[] BuildCosts = { 300, 600, 1000, 1600, 2400, 3500 };

        /// <summary>Price of the next house, by how many are built already.</summary>
        public static int BuildCost(int housesBuilt) => BuildCosts[Mathf.Clamp(housesBuilt, 0, BuildCosts.Length - 1)];

        /// <summary>Price to grow a house from <paramref name="level"/> to the next.</summary>
        public static int UpgradeCost(int level) => level <= 1 ? 800 : 2000;
    }
}
