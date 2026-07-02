using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>Raised whenever a currency balance changes — the wallet/HUD UI listens for this.</summary>
    public readonly struct CurrencyChangedEvent : IGameEvent
    {
        public readonly CurrencyType Type;
        public readonly int NewBalance;
        public CurrencyChangedEvent(CurrencyType type, int newBalance)
        {
            Type = type;
            NewBalance = newBalance;
        }
    }
}
