using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Chapters;
using UnityEngine;

namespace NinjaVillage.Systems.Chapters
{
    /// <summary>
    /// Every chapter the game ships, in order. One asset at <c>Resources/Catalogs/ChapterCatalog</c>
    /// (created by Ninja Village → Generate Default Content).
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterCatalog", menuName = "Ninja Village/Catalogs/Chapter Catalog")]
    public class ChapterCatalog : DefinitionCatalog<ChapterDefinition>
    {
    }
}
