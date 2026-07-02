using System;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Plain data holder for currency balances — deliberately not a MonoBehaviour so
    /// it can be embedded directly in <c>SaveData</c> and serialized with JsonUtility.
    /// </summary>
    [Serializable]
    public class CurrencyWallet
    {
        public int Coins;
        public int Gems;

        public int Get(CurrencyType type) => type == CurrencyType.Coins ? Coins : Gems;

        public void Add(CurrencyType type, int amount)
        {
            if (amount <= 0) return;
            if (type == CurrencyType.Coins) Coins += amount;
            else Gems += amount;
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            if (amount <= 0) return true;
            if (Get(type) < amount) return false;

            if (type == CurrencyType.Coins) Coins -= amount;
            else Gems -= amount;
            return true;
        }
    }
}
