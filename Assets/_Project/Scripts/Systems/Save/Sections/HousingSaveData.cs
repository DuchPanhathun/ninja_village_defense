using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>The village treasury: coins waiting to be collected, as of <see cref="LastTicks"/> (UTC, GameClock).</summary>
    [Serializable]
    public class TreasurySaveData
    {
        public double Stored;
        /// <summary>When <see cref="Stored"/> was last brought up to date (0 = never; starts empty).</summary>
        public long LastTicks;
    }

    /// <summary>A built house: which plot, which look, how big.</summary>
    [Serializable]
    public class HouseState
    {
        public int Plot;
        public string Style;
        public int Level;
    }

    /// <summary>Houses built on the village's house plots (empty plots have no entry).</summary>
    [Serializable]
    public class HouseSaveData
    {
        public List<HouseState> Houses = new();

        public HouseState Get(int plot)
        {
            foreach (var house in Houses)
                if (house != null && house.Plot == plot) return house;
            return null;
        }

        /// <summary>Sum of every house's level (families living in the village).</summary>
        public int TotalLevels()
        {
            int total = 0;
            foreach (var house in Houses)
                if (house != null && house.Level > 0) total += house.Level;
            return total;
        }
    }
}
