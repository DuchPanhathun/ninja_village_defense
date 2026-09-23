using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// The one place meta systems spend and grant currency. Wraps <see cref="EconomyManager"/>
    /// (which persists and broadcasts <see cref="CurrencyChangedEvent"/>) and additionally reports
    /// <see cref="ProgressStatIds.CoinsSpent"/> / <see cref="ProgressStatIds.GemsSpent"/> /
    /// <see cref="ProgressStatIds.CoinsEarned"/> so quests, achievements and analytics count every
    /// transaction exactly once without each feature remembering to do it.
    ///
    /// Falls back to the save's wallet directly if the EconomyManager doesn't exist yet
    /// (e.g. edit-mode tools), so callers never need a null check.
    /// </summary>
    public static class CurrencyService
    {
        public static int Balance(CurrencyType type) =>
            EconomyManager.Instance != null ? EconomyManager.Instance.Get(type) : SaveService.Data.Wallet.Get(type);

        public static bool CanAfford(Price price) => price.IsFree || Balance(price.Currency) >= price.Amount;

        /// <summary>Spends <paramref name="price"/> if affordable (saved immediately). <paramref name="subject"/> tags the progress report (e.g. building id).</summary>
        public static bool TrySpend(Price price, string subject = null)
        {
            if (price.IsFree) return true;

            bool success;
            if (EconomyManager.Instance != null)
            {
                success = EconomyManager.Instance.TrySpend(price.Currency, price.Amount);
            }
            else
            {
                var wallet = SaveService.Data.Wallet;
                success = wallet.TrySpend(price.Currency, price.Amount);
                if (success)
                {
                    SaveService.SaveNow();
                    EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(price.Currency, wallet.Get(price.Currency)));
                }
            }

            if (success)
            {
                string stat = price.Currency == CurrencyType.Coins ? ProgressStatIds.CoinsSpent : ProgressStatIds.GemsSpent;
                Progress.Report(stat, price.Amount, subject);
            }
            return success;
        }

        /// <summary>Adds currency (batched save) and reports coins earned.</summary>
        public static void Grant(CurrencyType type, int amount, string subject = null)
        {
            if (amount <= 0) return;

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Add(type, amount);
            }
            else
            {
                var wallet = SaveService.Data.Wallet;
                wallet.Add(type, amount);
                SaveService.MarkDirty();
                EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, wallet.Get(type)));
            }

            if (type == CurrencyType.Coins)
                Progress.Report(ProgressStatIds.CoinsEarned, amount, subject);
        }

        public static void Grant(Price reward, string subject = null) => Grant(reward.Currency, reward.Amount, subject);
    }
}
