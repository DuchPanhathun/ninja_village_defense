using System;
using System.Text;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;

namespace NinjaVillage.Systems.Crates
{
    /// <summary>One kind of supply crate: price, grade odds, S chance, share of weapons, and its pity.</summary>
    public sealed class CrateKind
    {
        public string Id;
        public string Name;
        public string Blurb;
        public Price Price;
        /// <summary>Weights for Common, Rare, Elite, Epic, Legendary.</summary>
        public float[] GradeWeights;
        /// <summary>Chance per open of an S-class item.</summary>
        public float SChance;
        /// <summary>Share of (non-S) drops that are weapons rather than gear.</summary>
        public float WeaponShare;
        /// <summary>An S item is certain by this many opens without one (0 = no pity).</summary>
        public int Pity;
        /// <summary>One free open a day.</summary>
        public bool DailyFree;
        /// <summary>Its S-class pool also holds the S-class mounts.</summary>
        public bool SMounts;
        public string ClosedIcon, OpenIcon;
    }

    /// <summary>What one roll of a crate decided (the service then picks the actual item).</summary>
    public readonly struct CrateRoll
    {
        public readonly bool Special;
        public readonly bool Weapon;
        public readonly ItemGrade Grade;

        public CrateRoll(bool special, bool weapon, ItemGrade grade)
        {
            Special = special;
            Weapon = weapon;
            Grade = grade;
        }
    }

    /// <summary>
    /// Supply crates as pure functions: a free-daily Wooden Crate (coins), a Silver Crate and the Surprise Box
    /// (gems) with better grades and a small chance of S-class equipment — certain by the 30th Surprise Box
    /// without one. Rolls take a <see cref="Random"/> so tests are exact.
    /// </summary>
    public static class CrateRules
    {
        public static readonly CrateKind[] Crates =
        {
            new()
            {
                Id = "wood", Name = "Wooden Crate", Blurb = "Everyday weapons and gear. One free every day!",
                Price = new Price(CurrencyType.Coins, 400), GradeWeights = new[] { 70f, 25f, 5f, 0f, 0f },
                SChance = 0f, WeaponShare = 0.15f, DailyFree = true, ClosedIcon = "chest_wood_closed", OpenIcon = "chest_wood_open",
            },
            new()
            {
                Id = "silver", Name = "Silver Crate", Blurb = "Mostly Rare and Elite, now and then Epic.",
                Price = new Price(CurrencyType.Gems, 40), GradeWeights = new[] { 15f, 55f, 25f, 5f, 0f },
                SChance = 0.01f, WeaponShare = 0.2f, ClosedIcon = "chest_silver_closed", OpenIcon = "chest_silver_open",
            },
            new()
            {
                Id = "surprise", Name = "Surprise Box", Blurb = "Elite to Legendary gear, and a chance of S-class equipment and mounts!",
                Price = new Price(CurrencyType.Gems, 120), GradeWeights = new[] { 0f, 40f, 40f, 17f, 3f },
                SChance = 0.05f, WeaponShare = 0.25f, Pity = 30, SMounts = true, ClosedIcon = "chest_surprise_closed", OpenIcon = "chest_surprise_open",
            },
        };

        public const int MultiOpen = 10;

        public static CrateKind Get(string id)
        {
            foreach (var crate in Crates)
                if (crate.Id == id) return crate;
            return null;
        }

        /// <summary>
        /// One open: an S item (by chance, or because <paramref name="opensWithoutSpecial"/> reached the pity), else a
        /// weapon or gear at a weighted grade. S items come at their native Elite grade.
        /// </summary>
        public static CrateRoll Roll(CrateKind crate, Random rng, int opensWithoutSpecial)
        {
            bool pity = crate.Pity > 0 && opensWithoutSpecial + 1 >= crate.Pity;
            bool special = crate.SChance > 0f && (pity || rng.NextDouble() < crate.SChance);
            bool weapon = rng.NextDouble() < crate.WeaponShare;
            if (special) return new CrateRoll(true, weapon, ItemGrade.Elite);
            return new CrateRoll(false, weapon, PickGrade(crate.GradeWeights, rng.NextDouble()));
        }

        public static ItemGrade PickGrade(float[] weights, double roll01)
        {
            float total = 0f;
            foreach (var w in weights) total += Math.Max(0f, w);
            double pick = Math.Clamp(roll01, 0.0, 0.999999) * total;
            for (int g = 0; g < weights.Length; g++)
            {
                pick -= Math.Max(0f, weights[g]);
                if (pick < 0) return GradeRules.Clamp(g);
            }
            return ItemGrade.Common;
        }

        /// <summary>"Common 70% · Rare 25% · Elite 5%" (+ the S chance) for the crate card.</summary>
        public static string DescribeOdds(CrateKind crate)
        {
            float total = 0f;
            foreach (var w in crate.GradeWeights) total += w;
            var text = new StringBuilder();
            for (int g = 0; g < crate.GradeWeights.Length; g++)
            {
                if (crate.GradeWeights[g] <= 0f) continue;
                if (text.Length > 0) text.Append(" · ");
                text.Append(GradeColors.Colorize($"{(ItemGrade)g} {crate.GradeWeights[g] / total * 100f:0.#}%", (ItemGrade)g));
            }
            if (crate.SChance > 0f)
                text.Append($"\n<color=#FFD24D>S-class {crate.SChance * 100f:0.#}%</color>" + (crate.Pity > 0 ? $" — certain by the {crate.Pity}th box" : ""));
            return text.ToString();
        }
    }
}
