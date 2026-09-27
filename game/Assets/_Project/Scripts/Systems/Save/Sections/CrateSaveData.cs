using System;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Supply crates: the day the free Wooden Crate was last opened, and Surprise Boxes opened since the last S item.</summary>
    [Serializable]
    public class CrateSaveData
    {
        public int FreeDay = -1;
        public int SurprisePity;
        public int Opened;
        public int SpecialsFound;
    }
}
