using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Talents
{
    /// <summary>Every talent node (<c>Resources/Catalogs/TalentCatalog.asset</c>) — the whole tree.</summary>
    [CreateAssetMenu(fileName = "TalentCatalog", menuName = "Ninja Village/Catalogs/Talent Catalog")]
    public class TalentCatalog : DefinitionCatalog<TalentDefinition> { }
}
