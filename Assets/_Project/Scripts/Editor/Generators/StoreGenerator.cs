using System.Linq;
using NinjaVillage.Systems.Monetization;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Cosmetic skins (EPIC 21): gem skins sold in the Shop plus the battle pass reward skins (not for sale).
    /// Color tints until real skin sprites exist.
    /// </summary>
    public static class StoreGenerator
    {
        private const string Folder = ContentGen.DataRoot + "/Store/Skins";

        [ContentGenerator("Store skins", 60)]
        public static void Generate()
        {
            Skin("skin_assassin_crimson", "Crimson Assassin", "assassin", new Color(1f, 0.45f, 0.45f), 250, true, 0);
            Skin("skin_assassin_sakura", "Sakura Assassin", "assassin", new Color(1f, 0.72f, 0.85f), 0, false, 1, "Season 1 battle pass reward.");
            Skin("skin_samurai_gold", "Golden Samurai", "samurai", new Color(1f, 0.85f, 0.35f), 400, true, 2);
            Skin("skin_samurai_oni", "Oni Samurai", "samurai", new Color(0.85f, 0.25f, 0.25f), 0, false, 3, "Season 2 battle pass reward.");
            Skin("skin_monk_jade", "Jade Monk", "monk", new Color(0.45f, 0.9f, 0.6f), 300, true, 4);
            Skin("skin_mage_frost", "Frost Mage", "mage_ninja", new Color(0.6f, 0.85f, 1f), 350, true, 5);
            Skin("skin_beast_shadow", "Shadow Beastmaster", "beast_ninja", new Color(0.45f, 0.35f, 0.6f), 350, true, 6);

            ContentGen.CreateOrLoad<SkinCatalog>($"{ContentGen.CatalogRoot}/SkinCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<SkinDefinition>(ContentGen.DataRoot).Where(s => !string.IsNullOrEmpty(s.Id)).OrderBy(s => s.SortOrder));
        }

        private static void Skin(string id, string name, string heroId, Color tint, int gems, bool soldInShop, int sort, string description = null)
        {
            ContentGen.Define<SkinDefinition>($"{Folder}/Skin_{id}.asset",
                ("id", id), ("displayName", name), ("description", description ?? "A cosmetic look — no stat changes."),
                ("heroId", heroId), ("tint", tint), ("gemPrice", gems), ("soldInShop", soldInShop), ("sortOrder", sort));
        }
    }
}
