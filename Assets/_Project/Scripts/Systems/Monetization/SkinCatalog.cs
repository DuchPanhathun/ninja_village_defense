using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>All skins (<c>Resources/Catalogs/SkinCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "SkinCatalog", menuName = "Ninja Village/Catalogs/Skin Catalog")]
    public class SkinCatalog : DefinitionCatalog<SkinDefinition> { }
}
