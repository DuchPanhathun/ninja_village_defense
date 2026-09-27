using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>Every achievement (<c>Resources/Catalogs/AchievementCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Ninja Village/Catalogs/Achievement Catalog")]
    public class AchievementCatalog : DefinitionCatalog<AchievementDefinition> { }
}
