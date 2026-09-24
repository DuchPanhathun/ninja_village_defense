using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Requests
{
    /// <summary>
    /// Villager request rules as pure functions (EPIC 24 Phase 3), free of assets and the save so EditMode
    /// tests pin them down: who the villagers are, which requests can come up, how rewards grow with the
    /// castle, when a request is done, and who asks for what.
    /// </summary>
    public static class RequestRules
    {
        /// <summary>Requests posted each day.</summary>
        public const int ActiveCount = 3;
        /// <summary>Seed salt so requests don't roll in step with the daily (1) and weekly (2) quests.</summary>
        public const int Salt = 3;
        /// <summary>Coin rewards grow by this share per castle level above 1.</summary>
        public const float CoinGrowthPerCastle = 0.2f;

        /// <summary>The townsfolk who ask for favours: sprite key → name.</summary>
        public static readonly (string key, string name)[] Villagers =
        {
            ("npc_villager", "Monk Daigo"), ("npc_woman", "Aiko"), ("npc_oldman", "Elder Genji"),
            ("npc_boy", "Little Kenta"), ("npc_villager2", "Hunter Jiro"), ("npc_oldwoman", "Granny Ume"),
            ("npc_villager3", "Farmer Taro"), ("npc_master", "Master Ryu"), ("npc_villager4", "Merchant Jin"),
        };

        public static string VillagerName(string key)
        {
            foreach (var (k, name) in Villagers)
                if (k == key) return name;
            return "A villager";
        }

        /// <summary>Whether a request can come up: the village can do it, and a chapter request needs a chapter left to clear.</summary>
        public static bool IsEligible(RequestKind kind, int requiredCastle, int requiredKitchen, int castleLevel, int kitchenLevel, int nextChapter) =>
            castleLevel >= requiredCastle && kitchenLevel >= requiredKitchen && (kind != RequestKind.Chapter || nextChapter > 0);

        /// <summary>Base coins grown by <see cref="CoinGrowthPerCastle"/> per castle level, rounded to 5.</summary>
        public static int ScaledCoins(int baseCoins, int castleLevel)
        {
            if (baseCoins <= 0) return 0;
            float scaled = baseCoins * (1f + CoinGrowthPerCastle * (Mathf.Max(1, castleLevel) - 1));
            return Mathf.Max(5, Mathf.RoundToInt(scaled / 5f) * 5);
        }

        /// <summary>How far along a request is, 0..target (a chapter request counts 0 or 1).</summary>
        public static int Progress(RequestKind kind, int target, int counted, int stored, int highestCleared, int chapter) => kind switch
        {
            RequestKind.Deliver => Mathf.Clamp(stored, 0, target),
            RequestKind.Stat => Mathf.Clamp(counted, 0, target),
            RequestKind.Chapter => highestCleared >= chapter ? 1 : 0,
            _ => 0,
        };

        public static int ShownTarget(RequestKind kind, int target) => kind == RequestKind.Chapter ? 1 : target;

        public static bool IsComplete(RequestKind kind, int target, int counted, int stored, int highestCleared, int chapter) =>
            Progress(kind, target, counted, stored, highestCleared, chapter) >= ShownTarget(kind, target);

        /// <summary>
        /// Who asks: one of the request's favourite villagers if any is free, else any free villager (never the
        /// same villager twice in a day), picked with <paramref name="rng"/>.
        /// </summary>
        public static string PickVillager(IReadOnlyList<string> preferred, IReadOnlyList<string> everyone, ICollection<string> taken, System.Random rng)
        {
            var free = new List<string>();
            if (preferred != null)
                foreach (var key in preferred)
                    if (!string.IsNullOrEmpty(key) && !taken.Contains(key) && Contains(everyone, key)) free.Add(key);
            if (free.Count == 0)
                foreach (var key in everyone)
                    if (!taken.Contains(key)) free.Add(key);
            if (free.Count == 0) return everyone.Count > 0 ? everyone[rng.Next(everyone.Count)] : null;
            return free[rng.Next(free.Count)];
        }

        private static bool Contains(IReadOnlyList<string> list, string key)
        {
            foreach (var k in list)
                if (k == key) return true;
            return false;
        }

        /// <summary>The task line, e.g. "Bring 5 Carrots" from "Bring {0} {1}".</summary>
        public static string Title(string format, int amount, string goodsName)
        {
            if (string.IsNullOrEmpty(format)) return string.Empty;
            try { return string.Format(format, amount, Plural(goodsName ?? "", amount)); }
            catch (FormatException) { return format; }
        }

        private static readonly HashSet<string> SameInPlural = new(StringComparer.OrdinalIgnoreCase)
        {
            "Rice", "Onigiri", "Yakitori", "Sushi", "Fish", "Shrimp", "Calamari", "Honey", "Meat",
        };

        /// <summary>"Carrot" → "Carrots", "Radish" → "Radishes"; words ending in s and dishes like Onigiri stay as they are.</summary>
        public static string Plural(string name, int amount)
        {
            if (amount == 1 || string.IsNullOrEmpty(name) || SameInPlural.Contains(name) || name.EndsWith("s")) return name;
            return name.EndsWith("sh") || name.EndsWith("ch") || name.EndsWith("x") ? name + "es" : name + "s";
        }
    }
}
