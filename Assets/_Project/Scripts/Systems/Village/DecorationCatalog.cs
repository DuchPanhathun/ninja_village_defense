using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Every decoration the village shop sells. One asset at <c>Resources/Catalogs/DecorationCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "DecorationCatalog", menuName = "Ninja Village/Catalogs/Decoration Catalog")]
    public class DecorationCatalog : DefinitionCatalog<DecorationDefinition>
    {
    }
}
