using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Every village building, looked up by <see cref="NinjaVillage.Systems.Save.BuildingIds"/>. Lives at
    /// <c>Resources/Catalogs/BuildingCatalog.asset</c> so the village map, building screens and the run
    /// modifier can resolve saved building levels back into definitions in any scene.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCatalog", menuName = "Ninja Village/Catalogs/Building Catalog")]
    public class BuildingCatalog : DefinitionCatalog<BuildingDefinition> { }
}
