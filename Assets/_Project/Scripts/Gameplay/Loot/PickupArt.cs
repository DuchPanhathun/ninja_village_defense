using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// Sprites for pickups that are built in code rather than from a prefab (the boss chest).
    /// Lives in <c>Resources/Catalogs/PickupArt</c>; filled by the art hookup generator.
    /// Without it the chest falls back to generated shapes.
    /// </summary>
    public class PickupArt : ScriptableObject
    {
        [SerializeField] private Sprite chestClosed;
        [SerializeField] private Sprite chestOpen;

        public Sprite ChestClosed => chestClosed;
        public Sprite ChestOpen => chestOpen;
    }
}
