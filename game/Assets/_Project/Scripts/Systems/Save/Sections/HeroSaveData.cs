using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Unlocked heroes and their levels (EPIC 13). Owned by the Hero system.</summary>
    [Serializable]
    public class HeroSaveData
    {
        /// <summary>Hero definition id → hero level. Presence = unlocked.</summary>
        public List<IdLevelEntry> Owned = new();
        public string SelectedHeroId;

        public bool IsUnlocked(string heroId) => Owned.ContainsId(heroId);
        public int GetLevel(string heroId) => Owned.GetLevel(heroId);

        /// <summary>Repairs lists a hand-edited / cloud-merged save may have nulled.</summary>
        public void EnsureInitialized()
        {
            Owned ??= new List<IdLevelEntry>();
        }
    }
}
