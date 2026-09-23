using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>All Shrine blessings (<c>Resources/Catalogs/BlessingCatalog.asset</c>) — the Shrine screen lists them and the run modifier applies their saved ranks.</summary>
    [CreateAssetMenu(fileName = "BlessingCatalog", menuName = "Ninja Village/Catalogs/Blessing Catalog")]
    public class BlessingCatalog : DefinitionCatalog<BlessingDefinition> { }
}
