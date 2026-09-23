using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Pets;
using UnityEngine;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// All pet gear sold in the Pet screen, looked up by id from <c>PetSaveData</c>. One asset at
    /// <c>Resources/Catalogs/PetEquipmentCatalog</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "PetEquipmentCatalog", menuName = "Ninja Village/Catalogs/Pet Equipment Catalog")]
    public class PetEquipmentCatalog : DefinitionCatalog<PetEquipmentDefinition>
    {
    }
}
