using System;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Fishing (EPIC 24 Phase 5): casts left and when the last one started refilling (UTC ticks, GameClock).</summary>
    [Serializable]
    public class FishingSaveData
    {
        /// <summary>-1 = never fished (starts with a full set of casts).</summary>
        public int Casts = -1;
        public long RegenTicks;
        public int TotalCaught;
    }

    /// <summary>The mine: bars dug up and waiting, as of <see cref="LastTicks"/>, plus the share carried towards the next gold / mithril bar and gem.</summary>
    [Serializable]
    public class MineSaveData
    {
        public double Stored;
        public long LastTicks;
        public double GoldCarry;
        public double MithrilCarry;
        public double GemCarry;
    }
}
