using NinjaVillage.Core;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>How good one copy of a weapon or gear piece is. Merging three of the same grade makes one of the next.</summary>
    public enum ItemGrade
    {
        Common,
        Rare,
        Elite,
        Epic,
        Legendary,
    }

    /// <summary>
    /// Weapon and gear grades as pure functions (Survivor.io-style merging): five grades, Common → Rare → Elite →
    /// Epic → Legendary; three copies of the same item at the same grade merge into one of the next grade (one
    /// missing copy can be made up with metal bars from the mine). Every item has a native grade — the one it drops
    /// at, from its rarity — where its listed stats apply; other grades scale them. A weapon's grade adds attack.
    /// </summary>
    public static class GradeRules
    {
        public const int MergeCount = 3;
        public const ItemGrade Max = ItemGrade.Legendary;

        /// <summary>Stat strength of each grade, relative to Common.</summary>
        private static readonly float[] Strength = { 1f, 1.5f, 2.2f, 3.2f, 4.5f };

        /// <summary>Attack added by the equipped weapon's grade.</summary>
        private static readonly float[] WeaponAttack = { 0f, 0.15f, 0.25f, 0.4f, 0.6f };

        public static string Name(ItemGrade grade) => grade.ToString();

        /// <summary>
        /// The grade an item drops at: Common, Rare, Epic or Legendary from its rarity; S-class items (Surprise Boxes
        /// only) drop at Elite. Elite is otherwise only reached by merging.
        /// </summary>
        public static ItemGrade Native(Rarity rarity, bool special = false) => special ? ItemGrade.Elite : rarity switch
        {
            Rarity.Rare => ItemGrade.Rare,
            Rarity.Epic => ItemGrade.Epic,
            Rarity.Legendary => ItemGrade.Legendary,
            _ => ItemGrade.Common,
        };

        public static ItemGrade Clamp(int grade) => (ItemGrade)Mathf.Clamp(grade, 0, (int)Max);

        /// <summary>Multiplier on an item's listed (native-grade) stats at <paramref name="grade"/>.</summary>
        public static float StatScale(ItemGrade native, ItemGrade grade) => Strength[(int)grade] / Strength[(int)native];

        public static float WeaponAttackBonus(ItemGrade grade) => WeaponAttack[(int)grade];

        public static bool IsMax(ItemGrade grade) => grade >= Max;
        public static ItemGrade Next(ItemGrade grade) => IsMax(grade) ? grade : grade + 1;

        /// <summary>
        /// The metal bar that can stand in for one missing copy when merging up to <paramref name="target"/>, and
        /// how many: iron for Rare and Elite, gold for Epic, mithril for Legendary.
        /// </summary>
        public static (string bar, int amount) BarsFor(ItemGrade target) => target switch
        {
            ItemGrade.Rare => ("iron_bar", 4),
            ItemGrade.Elite => ("iron_bar", 8),
            ItemGrade.Epic => ("gold_bar", 8),
            ItemGrade.Legendary => ("mithril_bar", 10),
            _ => (null, 0),
        };

        /// <summary>Whether <paramref name="copies"/> at <paramref name="grade"/> (plus bars for at most one missing copy) can merge.</summary>
        public static bool CanMerge(ItemGrade grade, int copies, int barsOwned)
        {
            if (IsMax(grade)) return false;
            if (copies >= MergeCount) return true;
            var (bar, amount) = BarsFor(Next(grade));
            return copies == MergeCount - 1 && bar != null && barsOwned >= amount;
        }
    }

    /// <summary>The five grade colours: grey, blue, purple, gold, red.</summary>
    public static class GradeColors
    {
        private static readonly Color[] Colors =
        {
            new(0.82f, 0.82f, 0.84f), new(0.35f, 0.62f, 1f), new(0.74f, 0.42f, 1f), new(1f, 0.8f, 0.22f), new(1f, 0.38f, 0.26f),
        };

        public static Color For(ItemGrade grade) => Colors[(int)GradeRules.Clamp((int)grade)];
        public static string Hex(ItemGrade grade) => "#" + ColorUtility.ToHtmlStringRGB(For(grade));
        public static string Colorize(string text, ItemGrade grade) => $"<color={Hex(grade)}>{text}</color>";
    }
}
