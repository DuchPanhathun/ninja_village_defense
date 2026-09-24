using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>A meal on the stove: which recipe, in which cooking slot, and when it's done (UTC ticks, from GameClock).</summary>
    [Serializable]
    public class CookingJob
    {
        public int Slot;
        public string RecipeId;
        public long StartTicks;
        public long ReadyTicks;
    }

    /// <summary>The Kitchen (EPIC 24 Phase 2): what's cooking, and the meals picked for the next battle.</summary>
    [Serializable]
    public class KitchenSaveData
    {
        public List<CookingJob> Jobs = new();

        /// <summary>Goods ids of the meals to eat when the next battle starts (at most two, no repeats).</summary>
        public List<string> SelectedMeals = new();

        public CookingJob Get(int slot)
        {
            foreach (var job in Jobs)
                if (job != null && job.Slot == slot) return job;
            return null;
        }
    }
}
