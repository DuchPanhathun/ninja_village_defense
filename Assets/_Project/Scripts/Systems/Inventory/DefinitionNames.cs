using System.Text;
using NinjaVillage.Core.ScriptableObjects;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>
    /// Player-facing name for any definition: its display name, or a prettified id when the asset
    /// has none (several early assets, e.g. Kunai, were created with an empty display name).
    /// </summary>
    public static class DefinitionNames
    {
        public static string Of(DescriptiveScriptableObject definition)
        {
            if (definition == null) return string.Empty;
            return string.IsNullOrEmpty(definition.DisplayName) ? Prettify(definition.Id) : definition.DisplayName;
        }

        /// <summary>"iron_ring" → "Iron Ring", "ChainSickle" → "Chain Sickle".</summary>
        public static string Prettify(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            var sb = new StringBuilder(id.Length + 4);
            bool newWord = true;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == '_' || c == '-' || c == ' ')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
                    newWord = true;
                    continue;
                }
                if (char.IsUpper(c) && i > 0 && char.IsLower(id[i - 1]) && sb.Length > 0 && sb[sb.Length - 1] != ' ')
                {
                    sb.Append(' ');
                    newWord = true;
                }
                sb.Append(newWord ? char.ToUpperInvariant(c) : c);
                newWord = false;
            }
            return sb.ToString().Trim();
        }
    }
}
