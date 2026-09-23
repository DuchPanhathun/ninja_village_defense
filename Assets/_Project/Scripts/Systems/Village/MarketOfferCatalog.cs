using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>The Market's full offer pool (<c>Resources/Catalogs/MarketOfferCatalog.asset</c>); the daily stock is rolled from it and saved as offer ids.</summary>
    [CreateAssetMenu(fileName = "MarketOfferCatalog", menuName = "Ninja Village/Catalogs/Market Offer Catalog")]
    public class MarketOfferCatalog : DefinitionCatalog<MarketOfferDefinition> { }
}
