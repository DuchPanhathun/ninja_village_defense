using System;
using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;

namespace NinjaVillage.Systems.Farm
{
    /// <summary>Raised whenever a plot changes (planted, watered, harvested, cleared).</summary>
    public readonly struct FarmChangedEvent : IGameEvent
    {
        public readonly int Plot;
        public FarmChangedEvent(int plot) => Plot = plot;
    }

    /// <summary>
    /// The farm (EPIC 24 Phase 1): plant a crop on an open plot (paying the seed), water it once to speed
    /// it up, harvest it when ripe into the storehouse (<see cref="GoodsService"/>). Crops grow in real time
    /// on <see cref="GameClock"/> — server time when online — so they keep growing while the app is closed
    /// and changing the phone's clock can't rush them. State in <c>SaveService.Data.Farm</c>; rules in
    /// <see cref="FarmRules"/>.
    /// </summary>
    public static class FarmService
    {
        public static CropCatalog Catalog => CatalogLoader.Load<CropCatalog>();

        private static FarmSaveData Data => SaveService.Data.Farm;

        public static int CastleLevel => VillageService.CastleLevel;
        public static int UnlockedPlots => FarmRules.UnlockedPlots(CastleLevel);
        public static bool IsUnlocked(int plot) => plot >= 0 && plot < UnlockedPlots;

        public static CropDefinition GetCrop(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        /// <summary>Crops in unlock order (a new list per call, for the seed picker).</summary>
        public static List<CropDefinition> GetCrops()
        {
            var list = new List<CropDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var crop in catalog.All)
                if (crop != null) list.Add(crop);
            list.Sort((a, b) => a.RequiredCastleLevel != b.RequiredCastleLevel
                ? a.RequiredCastleLevel.CompareTo(b.RequiredCastleLevel)
                : a.GrowSeconds.CompareTo(b.GrowSeconds));
            return list;
        }

        public static bool IsCropUnlocked(CropDefinition crop) => crop != null && CastleLevel >= crop.RequiredCastleLevel;

        public static FarmPlotState GetPlot(int plot) => Data.Get(plot);

        public static CropStage Stage(int plot)
        {
            var state = Data.Get(plot);
            return state == null ? CropStage.Empty : FarmRules.Stage(GameClock.UtcNow, state.PlantedTicks, state.ReadyTicks);
        }

        public static TimeSpan TimeLeft(int plot)
        {
            var state = Data.Get(plot);
            return state == null ? TimeSpan.Zero : FarmRules.TimeLeft(GameClock.UtcNow, state.ReadyTicks);
        }

        /// <summary>How many open plots hold a ripe crop (for badges and the HUD's Farm button).</summary>
        public static int RipeCount()
        {
            int ripe = 0;
            for (int plot = 0; plot < UnlockedPlots; plot++)
                if (Stage(plot) == CropStage.Ripe) ripe++;
            return ripe;
        }

        public static FarmResult CheckPlant(int plot, CropDefinition crop)
        {
            if (crop == null) return FarmResult.UnknownCrop;
            if (!IsUnlocked(plot)) return FarmResult.PlotLocked;
            if (Data.Get(plot) != null) return FarmResult.PlotBusy;
            if (!IsCropUnlocked(crop)) return FarmResult.CropLocked;
            if (!CurrencyService.CanAfford(new Price(CurrencyType.Coins, crop.SeedCost))) return FarmResult.NotEnoughCoins;
            return FarmResult.Success;
        }

        public static FarmResult Plant(int plot, CropDefinition crop)
        {
            var check = CheckPlant(plot, crop);
            if (check != FarmResult.Success) return check;
            if (!CurrencyService.TrySpend(new Price(CurrencyType.Coins, crop.SeedCost), $"seed_{crop.Id}")) return FarmResult.NotEnoughCoins;

            var now = GameClock.UtcNow;
            Data.Plots.Add(new FarmPlotState
            {
                Plot = plot, CropId = crop.Id, PlantedTicks = now.Ticks,
                ReadyTicks = now.Ticks + TimeSpan.FromSeconds(crop.GrowSeconds).Ticks,
            });
            Changed(plot);
            return FarmResult.Success;
        }

        public static FarmResult Water(int plot)
        {
            var state = Data.Get(plot);
            if (state == null) return FarmResult.PlotEmpty;
            if (state.Watered) return FarmResult.AlreadyWatered;
            if (Stage(plot) == CropStage.Ripe) return FarmResult.AlreadyWatered;
            state.ReadyTicks = FarmRules.WaterReadyTicks(GameClock.UtcNow, state.ReadyTicks);
            state.Watered = true;
            Changed(plot);
            return FarmResult.Success;
        }

        /// <summary>Rain on the village: every growing crop that isn't watered yet gets watered. Returns how many.</summary>
        public static int WaterAllByRain()
        {
            int watered = 0;
            for (int plot = 0; plot < UnlockedPlots; plot++)
            {
                var state = Data.Get(plot);
                if (state == null || state.Watered || Stage(plot) == CropStage.Ripe) continue;
                if (Water(plot) == FarmResult.Success) watered++;
            }
            return watered;
        }

        /// <summary>Harvests a ripe plot into the storehouse; <paramref name="harvest"/>/<paramref name="amount"/> say what came out.</summary>
        public static FarmResult Harvest(int plot, out GoodsDefinition harvest, out int amount)
        {
            harvest = null;
            amount = 0;
            var state = Data.Get(plot);
            if (state == null) return FarmResult.PlotEmpty;
            if (Stage(plot) != CropStage.Ripe) return FarmResult.NotRipe;
            var crop = GetCrop(state.CropId);
            Data.Plots.Remove(state);
            if (crop != null && crop.Harvest != null)
            {
                harvest = crop.Harvest;
                amount = crop.HarvestAmount;
                GoodsService.Add(harvest.Id, amount);
            }
            Progress.Report(ProgressStatIds.CropsHarvested, amount);
            Changed(plot);
            return FarmResult.Success;
        }

        /// <summary>Digs up whatever grows on the plot (no refund) so something else can be planted.</summary>
        public static bool Clear(int plot)
        {
            var state = Data.Get(plot);
            if (state == null) return false;
            Data.Plots.Remove(state);
            Changed(plot);
            return true;
        }

        public static string Describe(FarmResult result, CropDefinition crop = null) => result switch
        {
            FarmResult.PlotLocked => "Upgrade the Castle to open this plot.",
            FarmResult.PlotBusy => "Something is already growing here.",
            FarmResult.PlotEmpty => "Nothing is planted here.",
            FarmResult.CropLocked => crop != null ? $"{crop.NameOrId} unlocks at Castle Lv {crop.RequiredCastleLevel}." : "Not unlocked yet.",
            FarmResult.NotEnoughCoins => "Not enough coins for the seeds.",
            FarmResult.NotRipe => "Not ripe yet.",
            FarmResult.AlreadyWatered => "Already watered.",
            FarmResult.UnknownCrop => "Unknown crop.",
            _ => string.Empty,
        };

        private static void Changed(int plot)
        {
            SaveService.MarkDirty();
            EventBus<FarmChangedEvent>.Raise(new FarmChangedEvent(plot));
        }
    }
}
