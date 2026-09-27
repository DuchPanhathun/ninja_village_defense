using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Talents
{
    /// <summary>Raised after a talent rank changes or the tree is reset (TalentId null on reset).</summary>
    public readonly struct TalentsChangedEvent : IGameEvent
    {
        public readonly string TalentId;
        public readonly int NewRank;
        public TalentsChangedEvent(string talentId, int newRank)
        {
            TalentId = talentId;
            NewRank = newRank;
        }
    }

    /// <summary>
    /// The permanent talent tree (EPIC 14 "Talent unlock", "Talent reset"): ranking up nodes with
    /// coins once their prerequisites are learned, and resetting the tree for a full refund.
    /// Rules are in <see cref="TalentRules"/>; bonuses reach battles through <see cref="TalentRunModifier"/>.
    /// </summary>
    public static class TalentService
    {
        public static TalentCatalog Catalog => CatalogLoader.Load<TalentCatalog>();

        private static TalentSaveData Data
        {
            get
            {
                var talents = SaveService.Data.Talents;
                talents.Nodes ??= new List<IdLevelEntry>();
                return talents;
            }
        }

        public static int GetRank(TalentDefinition talent) => talent != null ? Data.Nodes.GetLevel(talent.Id) : 0;

        public static int TotalRanks
        {
            get
            {
                int total = 0;
                foreach (var node in Data.Nodes)
                    if (node != null) total += node.Level;
                return total;
            }
        }

        public static int TimesReset => Data.TimesReset;
        public static int CoinsSpent => Data.CoinsSpent;
        public static int ResetGemCost => TalentRules.ResetGemCost(Data.TimesReset);

        /// <summary>Talents of one branch ordered by tier, then name — the order the tree UI draws them.</summary>
        public static List<TalentDefinition> GetCategory(TalentCategory category)
        {
            var list = new List<TalentDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var talent in catalog.All)
                if (talent != null && talent.Category == category) list.Add(talent);
            list.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : string.CompareOrdinal(a.NameOrId, b.NameOrId));
            return list;
        }

        public static bool PrerequisitesMet(TalentDefinition talent)
        {
            if (talent == null) return false;
            foreach (var prerequisite in talent.Prerequisites)
                if (prerequisite != null && GetRank(prerequisite) < 1) return false;
            return true;
        }

        public static TalentResult CheckRankUp(TalentDefinition talent)
        {
            if (talent == null) return TalentResult.InvalidTalent;
            int rank = GetRank(talent);
            return TalentRules.CheckRankUp(rank, talent.MaxRank, PrerequisitesMet(talent),
                CurrencyService.Balance(CurrencyType.Coins), talent.CostForNextRank(rank));
        }

        public static TalentResult TryRankUp(TalentDefinition talent)
        {
            var result = CheckRankUp(talent);
            if (result != TalentResult.Success) return result;

            int rank = GetRank(talent);
            int cost = talent.CostForNextRank(rank);
            if (!CurrencyService.TrySpend(Price.Coins(cost), talent.Id)) return TalentResult.NotEnoughCoins;

            Data.Nodes.SetLevel(talent.Id, rank + 1);
            Data.CoinsSpent += cost;
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.TalentUnlocked, 1, talent.Id);
            EventBus<TalentsChangedEvent>.Raise(new TalentsChangedEvent(talent.Id, rank + 1));
            Sfx.Play(AudioCueIds.UiUpgrade);
            return TalentResult.Success;
        }

        public static TalentResult CheckReset() =>
            TalentRules.CheckReset(TotalRanks, Data.TimesReset, CurrencyService.Balance(CurrencyType.Gems));

        /// <summary>Clears every rank, refunds all coins spent, charges <see cref="ResetGemCost"/> gems.</summary>
        public static TalentResult TryReset()
        {
            var result = CheckReset();
            if (result != TalentResult.Success) return result;

            int gemCost = ResetGemCost;
            if (gemCost > 0 && !CurrencyService.TrySpend(Price.Gems(gemCost), "talent_reset"))
                return TalentResult.NotEnoughGems;

            int refund = Data.CoinsSpent;
            Data.Nodes.Clear();
            Data.CoinsSpent = 0;
            Data.TimesReset++;
            if (refund > 0) CurrencyService.Grant(CurrencyType.Coins, refund, "talent_reset");
            SaveService.SaveNow();

            EventBus<TalentsChangedEvent>.Raise(new TalentsChangedEvent(null, 0));
            return TalentResult.Success;
        }

        public static string Describe(TalentResult result, TalentDefinition talent = null)
        {
            switch (result)
            {
                case TalentResult.Success: return string.Empty;
                case TalentResult.InvalidTalent: return "Unknown talent";
                case TalentResult.MaxRank: return "Max rank";
                case TalentResult.PrerequisitesMissing:
                    if (talent == null) return "Learn the previous talents first";
                    var names = new List<string>();
                    foreach (var p in talent.Prerequisites)
                        if (p != null && GetRank(p) < 1) names.Add(p.NameOrId);
                    return "Requires " + string.Join(", ", names);
                case TalentResult.NotEnoughCoins: return "Not enough coins";
                case TalentResult.NothingToReset: return "No talents learned yet";
                case TalentResult.NotEnoughGems: return $"Reset costs {ResetGemCost} gems";
                default: return "Unavailable";
            }
        }

        /// <summary>"+3% attack damage" style label for a stat value.</summary>
        public static string FormatValue(TalentStat stat, float value)
        {
            switch (stat)
            {
                case TalentStat.HealPerSecond: return $"+{value:0.#} HP/s";
                case TalentStat.StartingShield: return $"+{value:0} starting shield";
                default: return $"+{value * 100f:0.#}% {Label(stat)}";
            }
        }

        public static string Label(TalentStat stat)
        {
            switch (stat)
            {
                case TalentStat.AttackDamage: return "attack damage";
                case TalentStat.AttackSpeed: return "attack speed";
                case TalentStat.CritChance: return "crit chance";
                case TalentStat.CritDamage: return "crit damage";
                case TalentStat.MaxHealth: return "max health";
                case TalentStat.DamageReduction: return "damage reduction";
                case TalentStat.HealPerSecond: return "healing";
                case TalentStat.DodgeChance: return "dodge chance";
                case TalentStat.StartingShield: return "starting shield";
                case TalentStat.MoveSpeed: return "move speed";
                case TalentStat.XpGain: return "XP gain";
                case TalentStat.GoldGain: return "gold gain";
                case TalentStat.PickupRadius: return "pickup radius";
                case TalentStat.LuckyDrop: return "lucky drop chance";
                case TalentStat.UltimateCharge: return "ultimate charge";
                default: return stat.ToString();
            }
        }
    }
}
