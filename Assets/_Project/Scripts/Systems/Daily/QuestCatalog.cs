using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>Daily + weekly quest pool (<c>Resources/Catalogs/QuestCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Ninja Village/Catalogs/Quest Catalog")]
    public class QuestCatalog : DefinitionCatalog<QuestDefinition> { }
}
