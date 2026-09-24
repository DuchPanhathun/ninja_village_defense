using System;
using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    public enum LiveOpsRewardType
    {
        Coins,
        Gems,
        Equipment,
        Hero,
        Skin,
        BattlePassXp
    }

    /// <summary>One reward from a battle pass tier, event mission or limited offer.</summary>
    [Serializable]
    public struct LiveOpsReward
    {
        public LiveOpsRewardType type;
        [Tooltip("Equipment / hero / skin id (unused for currencies and XP).")]
        public string id;
        [Min(0)] public int amount;

        public LiveOpsReward(LiveOpsRewardType type, int amount, string id = null)
        {
            this.type = type;
            this.amount = amount;
            this.id = id;
        }

        public override string ToString()
        {
            switch (type)
            {
                case LiveOpsRewardType.Coins: return $"{amount} Coins";
                case LiveOpsRewardType.Gems: return $"{amount} Gems";
                case LiveOpsRewardType.Equipment: return amount > 1 ? $"{DefinitionNames.Prettify(id)} ×{amount}" : DefinitionNames.Prettify(id);
                case LiveOpsRewardType.Hero: return $"Hero: {DefinitionNames.Prettify(id)}";
                case LiveOpsRewardType.Skin: return $"Skin: {DefinitionNames.Prettify(id)}";
                case LiveOpsRewardType.BattlePassXp: return $"{amount} Pass XP";
                default: return "?";
            }
        }
    }

    /// <summary>Applies live-ops rewards to the save (currencies via CurrencyService so they're reported once).</summary>
    public static class LiveOpsRewards
    {
        public static void Grant(LiveOpsReward reward, string subject)
        {
            switch (reward.type)
            {
                case LiveOpsRewardType.Coins: CurrencyService.Grant(CurrencyType.Coins, reward.amount, subject); break;
                case LiveOpsRewardType.Gems: CurrencyService.Grant(CurrencyType.Gems, reward.amount, subject); break;
                case LiveOpsRewardType.Equipment: InventoryService.AddEquipment(reward.id, Mathf.Max(1, reward.amount)); break;
                case LiveOpsRewardType.Hero:
                    // Already owned → compensate with gems so a duplicate is never a wasted reward.
                    if (!HeroService.Grant(reward.id)) CurrencyService.Grant(CurrencyType.Gems, 100, subject);
                    break;
                case LiveOpsRewardType.Skin:
                    var skins = SaveService.Data.Store.OwnedSkinIds;
                    if (!string.IsNullOrEmpty(reward.id) && !skins.Contains(reward.id)) skins.Add(reward.id);
                    break;
                case LiveOpsRewardType.BattlePassXp: BattlePassService.AddXp(reward.amount, subject); break;
            }
        }

        public static void GrantAll(IEnumerable<LiveOpsReward> rewards, string subject)
        {
            if (rewards == null) return;
            foreach (var reward in rewards) Grant(reward, subject);
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.RewardClaim);
        }

        public static string Describe(IReadOnlyList<LiveOpsReward> rewards)
        {
            if (rewards == null || rewards.Count == 0) return "—";
            var parts = new List<string>();
            foreach (var r in rewards) parts.Add(r.ToString());
            return string.Join(", ", parts);
        }
    }
}
