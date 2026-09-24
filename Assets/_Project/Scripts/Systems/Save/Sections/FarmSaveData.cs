using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>One farm plot: what's growing there and when it will be ripe (UTC ticks, from GameClock).</summary>
    [Serializable]
    public class FarmPlotState
    {
        public int Plot;
        public string CropId;
        public long PlantedTicks;
        public long ReadyTicks;
        public bool Watered;
    }

    /// <summary>Farm plots in use (empty plots have no entry).</summary>
    [Serializable]
    public class FarmSaveData
    {
        public List<FarmPlotState> Plots = new();

        public FarmPlotState Get(int plot)
        {
            foreach (var state in Plots)
                if (state != null && state.Plot == plot) return state;
            return null;
        }
    }

    /// <summary>The village storehouse: goods id → count (crops now; meals, fish, ore later).</summary>
    [Serializable]
    public class GoodsSaveData
    {
        public List<IdLevelEntry> Items = new();
    }
}
