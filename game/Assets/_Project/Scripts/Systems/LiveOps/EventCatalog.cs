using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>All EventDefinitions (<c>Resources/Catalogs/EventCatalog.asset</c>).</summary>
    [CreateAssetMenu(fileName = "EventCatalog", menuName = "Ninja Village/Catalogs/Event Catalog")]
    public class EventCatalog : DefinitionCatalog<EventDefinition> { }
}
