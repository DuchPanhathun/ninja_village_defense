using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>Every evolution recipe (<c>Resources/Catalogs/EvolutionCatalog.asset</c>) — used by the
    /// EvolutionManager when its own list is empty and by the Collection screen.</summary>
    [CreateAssetMenu(fileName = "EvolutionCatalog", menuName = "Ninja Village/Catalogs/Evolution Catalog")]
    public class EvolutionCatalog : DefinitionCatalog<EvolutionRecipe> { }
}
