using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Heroes;
using UnityEngine;

namespace NinjaVillage.Systems.Heroes
{
    /// <summary>
    /// Every hero the game ships, looked up by id from save data. One asset at
    /// <c>Resources/Catalogs/HeroCatalog</c> (created by Ninja Village → Generate Default Content).
    /// </summary>
    [CreateAssetMenu(fileName = "HeroCatalog", menuName = "Ninja Village/Catalogs/Hero Catalog")]
    public class HeroCatalog : DefinitionCatalog<HeroDefinition>
    {
    }
}
