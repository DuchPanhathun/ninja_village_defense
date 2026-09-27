using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Kitchen
{
    /// <summary>Every Kitchen recipe. One asset at <c>Resources/Catalogs/RecipeCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "RecipeCatalog", menuName = "Ninja Village/Catalogs/Recipe Catalog")]
    public class RecipeCatalog : DefinitionCatalog<RecipeDefinition>
    {
    }
}
