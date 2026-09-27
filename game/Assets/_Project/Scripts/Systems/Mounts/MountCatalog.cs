using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Mounts
{
    /// <summary>Every mount. One asset at <c>Resources/Catalogs/MountCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "MountCatalog", menuName = "Ninja Village/Catalogs/Mount Catalog")]
    public class MountCatalog : DefinitionCatalog<MountDefinition>
    {
    }
}
