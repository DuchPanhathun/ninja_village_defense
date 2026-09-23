using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// Every equipment piece (<c>Resources/Catalogs/EquipmentCatalog.asset</c>). Persistent inventory
    /// stores equipment ids; this resolves them for the Inventory screen, the run-start modifier, the
    /// Market, and as LootSpawner/ChestPickup's default drop pool when none is assigned in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentCatalog", menuName = "Ninja Village/Catalogs/Equipment Catalog")]
    public class EquipmentCatalog : DefinitionCatalog<EquipmentDefinition> { }
}
