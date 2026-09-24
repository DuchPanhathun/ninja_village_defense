using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Farm
{
    /// <summary>Every kind of village goods. One asset at <c>Resources/Catalogs/GoodsCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "GoodsCatalog", menuName = "Ninja Village/Catalogs/Goods Catalog")]
    public class GoodsCatalog : DefinitionCatalog<GoodsDefinition>
    {
    }
}
