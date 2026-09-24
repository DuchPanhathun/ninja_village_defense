using System;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised when coins are collected from the treasury.</summary>
    public readonly struct TreasuryCollectedEvent : IGameEvent
    {
        public readonly int Coins;
        public TreasuryCollectedEvent(int coins) => Coins = coins;
    }

    /// <summary>
    /// The village treasury (EPIC 24 Phase 4): coins trickle in over real time on <see cref="GameClock"/> —
    /// faster with a bigger castle and more families in houses — up to a cap, and are collected with a tap.
    /// Anything that changes the rate calls <see cref="Accrue"/> first so the old rate counts up to that moment.
    /// State in <c>SaveService.Data.Treasury</c>; rules in <see cref="HousingRules"/>.
    /// </summary>
    public static class TreasuryService
    {
        private static TreasurySaveData Data => SaveService.Data.Treasury;

        public static float CoinsPerHour => HousingRules.CoinsPerHour(VillageService.CastleLevel, HouseService.TotalLevels);
        public static int Capacity => HousingRules.Capacity(VillageService.CastleLevel, HouseService.TotalLevels);

        /// <summary>Whole coins ready to collect right now.</summary>
        public static int Available => (int)Math.Floor(Current());

        public static bool IsFull => Available >= Capacity;

        public static float Fill => Capacity <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)(Current() / Capacity));

        public static TimeSpan UntilFull => HousingRules.UntilFull(Current(), CoinsPerHour, Capacity);

        private static double Current()
        {
            var data = Data;
            if (data.LastTicks <= 0) Accrue(); // first look: start the clock (empty)
            return HousingRules.Accrued(data.Stored, data.LastTicks, GameClock.UtcNow, CoinsPerHour, Capacity);
        }

        /// <summary>Banks what has flowed in so far at the current rate. Call before the rate or cap changes.</summary>
        public static void Accrue()
        {
            var data = Data;
            var now = GameClock.UtcNow;
            data.Stored = HousingRules.Accrued(data.Stored, data.LastTicks, now, CoinsPerHour, Capacity);
            data.LastTicks = now.Ticks;
            SaveService.MarkDirty();
        }

        /// <summary>Pays out every whole coin in the treasury; returns how many.</summary>
        public static int Collect()
        {
            Accrue();
            var data = Data;
            int coins = (int)Math.Floor(data.Stored);
            if (coins <= 0) return 0;
            data.Stored -= coins;
            CurrencyService.Grant(CurrencyType.Coins, coins, "treasury");
            Progress.Report(ProgressStatIds.TreasuryCollected, coins);
            SaveService.MarkDirty();
            EventBus<TreasuryCollectedEvent>.Raise(new TreasuryCollectedEvent(coins));
            return coins;
        }
    }
}
