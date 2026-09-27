namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// An amount of one currency — what an upgrade, craft or shop offer costs. Exists so cost
    /// formulas (<see cref="CostCurve"/>) and spending (<see cref="CurrencyService.TrySpend"/>)
    /// pass one value around instead of a loose (type, amount) pair that is easy to mismatch.
    /// </summary>
    public readonly struct Price
    {
        public readonly CurrencyType Currency;
        public readonly int Amount;

        public Price(CurrencyType currency, int amount)
        {
            Currency = currency;
            Amount = amount < 0 ? 0 : amount;
        }

        public bool IsFree => Amount <= 0;

        public static Price Coins(int amount) => new(CurrencyType.Coins, amount);
        public static Price Gems(int amount) => new(CurrencyType.Gems, amount);

        public override string ToString() => $"{Amount} {(Currency == CurrencyType.Coins ? "Coins" : "Gems")}";
    }
}
