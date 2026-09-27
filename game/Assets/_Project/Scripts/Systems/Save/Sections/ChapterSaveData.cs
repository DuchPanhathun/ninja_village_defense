using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Chapter progress: which chapter START plays, how far the player got, and what's cleared.</summary>
    [Serializable]
    public class ChapterSaveData
    {
        /// <summary>Chapter START launches (repaired to an unlocked one if it isn't).</summary>
        public string SelectedId;
        /// <summary>Number of the highest chapter cleared; 0 = none. Chapter N is unlocked when N &lt;= this + 1.</summary>
        public int HighestCleared;
        /// <summary>Best wave reached per chapter id.</summary>
        public List<IdLevelEntry> BestWaves = new();
    }
}
