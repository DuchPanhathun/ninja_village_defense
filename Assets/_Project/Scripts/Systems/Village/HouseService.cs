using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised when a house is built, grows or is repainted.</summary>
    public readonly struct HousesChangedEvent : IGameEvent
    {
        public readonly int Plot;
        public HousesChangedEvent(int plot) => Plot = plot;
    }

    public enum HouseResult
    {
        Success,
        PlotLocked,
        PlotTaken,
        NoHouse,
        StyleLocked,
        MaxLevel,
        CastleLevel,
        NotEnoughCoins,
    }

    /// <summary>
    /// Houses on the village's house plots (EPIC 24 Phase 4): build one in any unlocked style, grow it to more
    /// levels (each level is a family: more townsfolk, more treasury income), repaint it for free. State in
    /// <c>SaveService.Data.Housing</c>; rules in <see cref="HousingRules"/>.
    /// </summary>
    public static class HouseService
    {
        private static HouseSaveData Data => SaveService.Data.Housing;

        public static int CastleLevel => VillageService.CastleLevel;
        public static int UnlockedPlots => HousingRules.UnlockedPlots(CastleLevel);
        public static bool IsUnlocked(int plot) => plot >= 0 && plot < UnlockedPlots;
        public static int TotalLevels => Data.TotalLevels();
        public static int Built => Data.Houses.Count;
        public static IReadOnlyList<HouseState> Houses => Data.Houses;

        public static HouseState Get(int plot) => Data.Get(plot);

        public static bool IsStyleUnlocked(HouseStyle style) => CastleLevel >= style.RequiredCastleLevel;

        public static Price BuildPrice => new(CurrencyType.Coins, HousingRules.BuildCost(Built));

        public static Price UpgradePrice(HouseState house) =>
            new(CurrencyType.Coins, HousingRules.UpgradeCost(house != null ? house.Level : 1));

        public static HouseResult CheckBuild(int plot, string styleId)
        {
            if (!IsUnlocked(plot)) return HouseResult.PlotLocked;
            if (Data.Get(plot) != null) return HouseResult.PlotTaken;
            var style = HousingRules.Style(styleId);
            if (style == null || !IsStyleUnlocked(style.Value)) return HouseResult.StyleLocked;
            if (!CurrencyService.CanAfford(BuildPrice)) return HouseResult.NotEnoughCoins;
            return HouseResult.Success;
        }

        public static HouseResult Build(int plot, string styleId)
        {
            var check = CheckBuild(plot, styleId);
            if (check != HouseResult.Success) return check;
            var price = BuildPrice;
            if (!CurrencyService.TrySpend(price, $"house_{plot}")) return HouseResult.NotEnoughCoins;
            TreasuryService.Accrue(); // a new family changes the treasury's rate from now on
            Data.Houses.Add(new HouseState { Plot = plot, Style = styleId, Level = 1 });
            Progress.Report(ProgressStatIds.HouseBuilt, 1, styleId);
            Changed(plot);
            return HouseResult.Success;
        }

        public static HouseResult CheckUpgrade(int plot)
        {
            var house = Data.Get(plot);
            if (house == null) return HouseResult.NoHouse;
            if (house.Level >= HousingRules.MaxLevel) return HouseResult.MaxLevel;
            if (house.Level >= HousingRules.LevelCap(CastleLevel)) return HouseResult.CastleLevel;
            if (!CurrencyService.CanAfford(UpgradePrice(house))) return HouseResult.NotEnoughCoins;
            return HouseResult.Success;
        }

        public static HouseResult Upgrade(int plot)
        {
            var check = CheckUpgrade(plot);
            if (check != HouseResult.Success) return check;
            var house = Data.Get(plot);
            if (!CurrencyService.TrySpend(UpgradePrice(house), $"house_{plot}_lv{house.Level + 1}")) return HouseResult.NotEnoughCoins;
            TreasuryService.Accrue();
            house.Level++;
            Progress.Report(ProgressStatIds.HouseBuilt, 1, house.Style);
            Changed(plot);
            return HouseResult.Success;
        }

        /// <summary>Repaints a house in another unlocked style (free).</summary>
        public static HouseResult Restyle(int plot, string styleId)
        {
            var house = Data.Get(plot);
            if (house == null) return HouseResult.NoHouse;
            var style = HousingRules.Style(styleId);
            if (style == null || !IsStyleUnlocked(style.Value)) return HouseResult.StyleLocked;
            if (house.Style == styleId) return HouseResult.Success;
            house.Style = styleId;
            Changed(plot);
            return HouseResult.Success;
        }

        public static string Describe(HouseResult result, int plot = -1) => result switch
        {
            HouseResult.PlotLocked => $"This plot opens at Castle Lv {HousingRules.CastleLevelForPlot(plot)}.",
            HouseResult.PlotTaken => "There's already a house here.",
            HouseResult.NoHouse => "Build a house here first.",
            HouseResult.StyleLocked => "That style needs a bigger castle.",
            HouseResult.MaxLevel => "This house is as big as it gets.",
            HouseResult.CastleLevel => $"Upgrade the Castle to Lv {HousingRules.CastleLevelForHouseLevel((Get(plot)?.Level ?? 1) + 1)} to grow this house.",
            HouseResult.NotEnoughCoins => "Not enough coins.",
            _ => string.Empty,
        };

        private static void Changed(int plot)
        {
            SaveService.MarkDirty();
            EventBus<HousesChangedEvent>.Raise(new HousesChangedEvent(plot));
        }
    }
}
