using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Requests
{
    /// <summary>Every villager request. One asset at <c>Resources/Catalogs/VillagerRequestCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "VillagerRequestCatalog", menuName = "Ninja Village/Catalogs/Villager Request Catalog")]
    public class VillagerRequestCatalog : DefinitionCatalog<VillagerRequestDefinition>
    {
    }
}
