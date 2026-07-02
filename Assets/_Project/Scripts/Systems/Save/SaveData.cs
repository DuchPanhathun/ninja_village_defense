using System;
using NinjaVillage.Systems.Economy;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Everything persisted between sessions. Grows as later EPICs add heroes,
    /// weapon levels, pets, village buildings, talents, etc. — keep every new
    /// field JsonUtility-friendly (no Dictionary; use arrays/lists of small
    /// serializable structs instead).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public CurrencyWallet Wallet = new();
        public int HighestWaveReached;
        public int TotalRunsCompleted;
    }
}
