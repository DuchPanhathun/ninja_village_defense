using NinjaVillage.Core;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>One shared rarity palette so the Inventory, Forge, Market and world-space loot drops agree on colors.</summary>
    public static class RarityColors
    {
        public static readonly Color Common = new(0.82f, 0.82f, 0.84f, 1f);
        public static readonly Color Rare = new(0.35f, 0.62f, 1f, 1f);
        public static readonly Color Epic = new(0.74f, 0.42f, 1f, 1f);
        public static readonly Color Legendary = new(1f, 0.62f, 0.15f, 1f);

        public static Color For(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return Rare;
                case Rarity.Epic: return Epic;
                case Rarity.Legendary: return Legendary;
                default: return Common;
            }
        }

        /// <summary>For TextMeshPro rich text: <c>&lt;color=#RRGGBB&gt;</c>.</summary>
        public static string Hex(Rarity rarity) => "#" + ColorUtility.ToHtmlStringRGB(For(rarity));

        public static string Colorize(string text, Rarity rarity) => $"<color={Hex(rarity)}>{text}</color>";
    }
}
