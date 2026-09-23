using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Weapons;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>
    /// Every weapon the player can own (<c>Resources/Catalogs/WeaponCatalog.asset</c>). The save stores
    /// weapon ids (InventorySaveData.Weapons / EquippedWeaponId); this turns them back into
    /// <see cref="WeaponDefinition"/>s for the Forge, the Inventory screen and the run start.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Ninja Village/Catalogs/Weapon Catalog")]
    public class WeaponCatalog : DefinitionCatalog<WeaponDefinition> { }
}
