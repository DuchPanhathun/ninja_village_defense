using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>All LimitedOfferDefinitions (<c>Resources/Catalogs/LimitedOfferCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "LimitedOfferCatalog", menuName = "Ninja Village/Catalogs/LimitedOffer Catalog")]
    public class LimitedOfferCatalog : DefinitionCatalog<LimitedOfferDefinition> { }
}
