using System;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>A currency reward for a login day, quest or achievement tier.</summary>
    [Serializable]
    public struct DailyReward
    {
        public CurrencyType currency;
        [Min(0)] public int amount;

        public DailyReward(CurrencyType currency, int amount)
        {
            this.currency = currency;
            this.amount = amount;
        }

        public static DailyReward Coins(int amount) => new(CurrencyType.Coins, amount);
        public static DailyReward Gems(int amount) => new(CurrencyType.Gems, amount);

        public bool IsEmpty => amount <= 0;

        public override string ToString() => $"{amount} {(currency == CurrencyType.Coins ? "Coins" : "Gems")}";

        /// <summary>Pays the reward (reports CoinsEarned for coins via CurrencyService).</summary>
        public void Grant(string subject)
        {
            if (!IsEmpty) CurrencyService.Grant(currency, amount, subject);
        }
    }
}
