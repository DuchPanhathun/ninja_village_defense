using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Pets;
using UnityEngine;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// Every pet the game ships, looked up by id from save data. One asset at
    /// <c>Resources/Catalogs/PetCatalog</c> (created by Ninja Village → Generate Default Content).
    /// </summary>
    [CreateAssetMenu(fileName = "PetCatalog", menuName = "Ninja Village/Catalogs/Pet Catalog")]
    public class PetCatalog : DefinitionCatalog<PetDefinition>
    {
    }
}
