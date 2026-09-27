using System;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised when bars are collected from the mine.</summary>
    public readonly struct MineCollectedEvent : IGameEvent
    {
        public readonly MineHaul Haul;
        public MineCollectedEvent(MineHaul haul) => Haul = haul;
    }

    /// <summary>
    /// The mine (EPIC 24 Phase 5): once built (Castle Lv 3), it digs up metal bars over real time — its level sets
    /// the rate ("Bars per hour") — up to <see cref="PondMineRules.MineCapHours"/> hours' worth. Collecting splits
    /// them into iron / gold / mithril by the mine's level and turns up a gem now and then. The Forge takes bars in
    /// place of spare gear. State in <c>SaveService.Data.Mine</c>; rules in <see cref="PondMineRules"/>.
    /// </summary>
    public static class MineService
    {
        private static MineSaveData Data => SaveService.Data.Mine;

        public static int Level => VillageService.GetLevel(BuildingIds.Mine);
        public static bool IsBuilt => Level > 0;
        public static float BarsPerHour => IsBuilt ? VillageService.GetEffect(BuildingIds.Mine) : 0f;
        public static int Capacity => PondMineRules.Capacity(BarsPerHour);

        public static int Available => (int)Math.Floor(Current());
        public static bool IsFull => IsBuilt && Available >= Capacity;
        public static float Fill => Capacity <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)(Current() / Capacity));

        private static double Current()
        {
            if (!IsBuilt) return 0;
            var data = Data;
            if (data.LastTicks <= 0) Accrue(); // just built: start digging now
            return HousingRules.Accrued(data.Stored, data.LastTicks, GameClock.UtcNow, BarsPerHour, Capacity);
        }

        /// <summary>Banks what was dug so far at the current rate. Call before the mine's level changes.</summary>
        public static void Accrue()
        {
            var data = Data;
            var now = GameClock.UtcNow;
            data.Stored = IsBuilt ? HousingRules.Accrued(data.Stored, data.LastTicks, now, BarsPerHour, Capacity) : 0;
            data.LastTicks = now.Ticks;
            SaveService.MarkDirty();
        }

        /// <summary>Takes every whole bar out of the mine into the storehouse (and any gems into the wallet).</summary>
        public static MineHaul Collect()
        {
            if (!IsBuilt) return default;
            Accrue();
            var data = Data;
            int bars = (int)Math.Floor(data.Stored);
            if (bars <= 0) return default;
            data.Stored -= bars;
            var haul = PondMineRules.Split(bars, Level, ref data.GoldCarry, ref data.MithrilCarry, ref data.GemCarry);
            GoodsService.Add("iron_bar", haul.Iron);
            GoodsService.Add("gold_bar", haul.Gold);
            GoodsService.Add("mithril_bar", haul.Mithril);
            if (haul.Gems > 0) CurrencyService.Grant(CurrencyType.Gems, haul.Gems, "mine");
            Progress.Report(ProgressStatIds.BarsMined, haul.Bars);
            SaveService.MarkDirty();
            EventBus<MineCollectedEvent>.Raise(new MineCollectedEvent(haul));
            return haul;
        }
    }
}
