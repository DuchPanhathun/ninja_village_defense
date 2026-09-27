using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>All SeasonDefinitions (<c>Resources/Catalogs/SeasonCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "SeasonCatalog", menuName = "Ninja Village/Catalogs/Season Catalog")]
    public class SeasonCatalog : DefinitionCatalog<SeasonDefinition> { }
}
