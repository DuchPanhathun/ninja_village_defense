using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Mounts owned (id → level) and the one your hero rides.</summary>
    [Serializable]
    public class MountSaveData
    {
        public List<IdLevelEntry> Owned = new();
        public string ActiveMountId;
    }
}
