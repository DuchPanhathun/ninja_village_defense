using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Farm
{
    /// <summary>Every crop, in unlock order. One asset at <c>Resources/Catalogs/CropCatalog</c>.</summary>
    [CreateAssetMenu(fileName = "CropCatalog", menuName = "Ninja Village/Catalogs/Crop Catalog")]
    public class CropCatalog : DefinitionCatalog<CropDefinition>
    {
    }
}
