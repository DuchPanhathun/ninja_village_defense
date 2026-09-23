using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Discovered evolution recipes (EPIC 7 "Hidden Discovery"). Survives across runs.</summary>
    [Serializable]
    public class EvolutionSaveData
    {
        public List<string> DiscoveredRecipeIds = new();
        /// <summary>Ids the player hasn't opened in the Collection UI yet (drives a "new" badge).</summary>
        public List<string> UnseenRecipeIds = new();
    }
}
