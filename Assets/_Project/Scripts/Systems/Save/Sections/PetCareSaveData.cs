using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Pet care (EPIC 24 Phase 7): the GameClock day each pet was last petted / fed (pet id → day).</summary>
    [Serializable]
    public class PetCareSaveData
    {
        public List<IdLevelEntry> PettedDay = new();
        public List<IdLevelEntry> FedDay = new();
    }
}
