using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Core;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Village content (EPIC 11): the six buildings with map placement, Castle gating, cost curves and
    /// per-level effects; the Shrine's blessings; and the Market's offer pool.
    ///
    /// Pacing (EPIC 15 "Upgrade costs"): early upgrades cost roughly one or two runs of coins (a
    /// wave-5 run earns ~150-250), growing 30-60% per level so later levels are long-term goals. The
    /// Castle gates everything else and also needs a best wave (3 per level), so village growth tracks
    /// battle progress instead of pure grinding.
    /// </summary>
    public static class VillageGenerator
    {
        private const string BuildingFolder = ContentGen.DataRoot + "/Village/Buildings";
        private const string BlessingFolder = ContentGen.DataRoot + "/Village/Blessings";
        private const string OfferFolder = ContentGen.DataRoot + "/Village/MarketOffers";

        [ContentGenerator("Village buildings, blessings & market", 10)]
        public static void Generate()
        {
            GenerateBuildings();
            GenerateBlessings();
            GenerateMarketOffers();
        }

        private static (string, object)[] Cost(int baseCost, float growth, CurrencyType currency = CurrencyType.Coins) => new (string, object)[]
        {
            ("upgradeCost.type", CostCurveType.Exponential), ("upgradeCost.currency", currency),
            ("upgradeCost.baseCost", baseCost), ("upgradeCost.growth", growth), ("upgradeCost.roundTo", 5),
        };

        private static void Building(string id, string name, string description, int maxLevel, int startLevel, int requiredCastle,
            int levelsPerCastle, int wavePerLevel, (string, object)[] cost, EffectDisplay display, string effectLabel,
            float effectFirst, float effectMax, Vector2 plot, Vector2 footprint, Color color, Color roof, string[] stages)
        {
            var values = new List<(string, object)>
            {
                ("id", id), ("displayName", name), ("description", description),
                ("maxLevel", maxLevel), ("startLevel", startLevel), ("requiredCastleLevel", requiredCastle),
                ("levelsPerCastleLevel", levelsPerCastle), ("waveRequirementPerLevel", wavePerLevel),
                ("effectDisplay", display), ("effectLabel", effectLabel), ("effectAtFirstLevel", effectFirst), ("effectAtMaxLevel", effectMax),
                ("plotPosition", plot), ("footprint", footprint), ("color", color), ("roofColor", roof), ("stageNames", stages),
            };
            values.AddRange(cost);
            ContentGen.Define<BuildingDefinition>($"{BuildingFolder}/Building_{id}.asset", values.ToArray());
        }

        private static void GenerateBuildings()
        {
            Building(BuildingIds.Castle, "Castle", "The heart of the village. Its level caps every other building and adds equipment slots.",
                10, 1, 0, 0, 3, Cost(400, 1.6f), EffectDisplay.Integer, "Equipment slots", 2f, 5f,
                new Vector2(0f, 6.5f), new Vector2(4f, 3.2f), new Color(0.78f, 0.74f, 0.66f), new Color(0.35f, 0.22f, 0.45f),
                new[] { "Hut", "House", "Manor", "Keep", "Fortress", "Castle" });

            Building(BuildingIds.Dojo, "Dojo", "Train your ninja. Every level adds attack power to every hero and raises the hero level cap.",
                20, 0, 1, 2, 0, Cost(150, 1.28f), EffectDisplay.Percent, "Attack", 0.02f, 0.80f,
                new Vector2(-7.5f, 2f), new Vector2(3f, 2.4f), new Color(0.72f, 0.56f, 0.40f), new Color(0.62f, 0.20f, 0.18f),
                new[] { "Training Mat", "Dojo", "Grand Dojo" });

            Building(BuildingIds.Forge, "Forge", "Craft new weapons, level them up and reforge them into legendary tiers.",
                10, 0, 1, 1, 0, Cost(200, 1.45f), EffectDisplay.Integer, "Max weapon level", 3f, 10f,
                new Vector2(7.5f, 2f), new Vector2(3f, 2.4f), new Color(0.45f, 0.42f, 0.42f), new Color(0.25f, 0.22f, 0.22f),
                new[] { "Anvil", "Forge", "Great Forge" });

            Building(BuildingIds.Shrine, "Shrine", "Receive permanent blessings: health, critical strikes, luck and fortune.",
                10, 0, 2, 1, 0, Cost(300, 1.45f), EffectDisplay.Integer, "Blessing rank cap", 2f, 10f,
                new Vector2(-8f, -5.5f), new Vector2(2.6f, 2.4f), new Color(0.85f, 0.25f, 0.2f), new Color(0.2f, 0.2f, 0.22f),
                new[] { "Stone Altar", "Shrine", "Temple" });

            Building(BuildingIds.PetHouse, "Pet House", "Home for your companions. Holds more pets and lets them grow stronger.",
                10, 0, 2, 1, 0, Cost(250, 1.45f), EffectDisplay.Integer, "Pet slots", 2f, 11f,
                new Vector2(8f, -5.5f), new Vector2(2.8f, 2.2f), new Color(0.62f, 0.48f, 0.30f), new Color(0.30f, 0.50f, 0.25f),
                new[] { "Kennel", "Pet House", "Beast Lodge" });

            Building(BuildingIds.Market, "Market", "A daily shop with equipment, crafting crates and currency deals.",
                5, 0, 3, 1, 0, Cost(400, 1.6f), EffectDisplay.Integer, "Offers per day", 3f, 6f,
                new Vector2(0f, -7.5f), new Vector2(3.4f, 2f), new Color(0.90f, 0.78f, 0.45f), new Color(0.80f, 0.35f, 0.25f),
                new[] { "Stall", "Market", "Bazaar" });

            Building(BuildingIds.Kitchen, "Kitchen", "Cook your harvest into meals. Pick up to 2 meals before a battle — each one powers you up for that whole run.",
                5, 0, 2, 1, 0, Cost(300, 1.5f), EffectDisplay.Integer, "Cooking slots", 1f, 3f,
                new Vector2(-18f, 1.7f), new Vector2(3.6f, 2.4f), new Color(0.42f, 0.52f, 0.46f), new Color(0.36f, 0.46f, 0.40f),
                new[] { "Cook Stall", "Kitchen", "Tea House" });

            var catalog = ContentGen.CreateOrLoad<BuildingCatalog>($"{ContentGen.CatalogRoot}/BuildingCatalog.asset");
            catalog.EditorSetItems(ContentGen.FindAll<BuildingDefinition>(ContentGen.DataRoot).Where(b => !string.IsNullOrEmpty(b.Id)).OrderBy(b => b.Id));
        }

        private static void Blessing(string id, string name, string description, BlessingStat stat, float perRank, int maxRank,
            int requiredShrine, int baseCost, Color color)
        {
            ContentGen.Define<BlessingDefinition>($"{BlessingFolder}/Blessing_{id}.asset",
                ("id", id), ("displayName", name), ("description", description), ("stat", stat),
                ("valuePerRank", perRank), ("maxRank", maxRank), ("requiredShrineLevel", requiredShrine),
                ("rankCost.type", CostCurveType.Exponential), ("rankCost.currency", CurrencyType.Coins),
                ("rankCost.baseCost", baseCost), ("rankCost.growth", 1.4f), ("rankCost.roundTo", 5), ("color", color));
        }

        private static void GenerateBlessings()
        {
            Blessing("vitality", "Blessing of Vitality", "+Health for every battle.", BlessingStat.MaxHealth, 0.04f, 10, 1, 120, new Color(0.45f, 0.9f, 0.5f));
            Blessing("precision", "Blessing of Precision", "+Critical chance.", BlessingStat.CritChance, 0.015f, 10, 1, 140, new Color(1f, 0.55f, 0.3f));
            Blessing("fortune", "Blessing of Fortune", "+Luck: more double coin drops and better loot.", BlessingStat.Luck, 0.02f, 10, 2, 160, new Color(0.4f, 0.9f, 0.9f));
            Blessing("prosperity", "Blessing of Prosperity", "+Coins from every source.", BlessingStat.CoinGain, 0.05f, 10, 2, 160, new Color(1f, 0.85f, 0.3f));
            Blessing("wisdom", "Blessing of Wisdom", "+XP gained in battle — level up faster.", BlessingStat.XpGain, 0.04f, 10, 3, 200, new Color(0.6f, 0.6f, 1f));
            Blessing("iron_skin", "Blessing of Iron Skin", "Take less damage from every hit.", BlessingStat.DamageReduction, 0.015f, 10, 4, 260, new Color(0.7f, 0.7f, 0.75f));

            var catalog = ContentGen.CreateOrLoad<BlessingCatalog>($"{ContentGen.CatalogRoot}/BlessingCatalog.asset");
            catalog.EditorSetItems(ContentGen.FindAll<BlessingDefinition>(ContentGen.DataRoot).Where(b => !string.IsNullOrEmpty(b.Id)).OrderBy(b => b.RequiredShrineLevel).ThenBy(b => b.Id));
        }

        private static void Offer(string id, string name, MarketRewardType type, int amount, EquipmentDefinition equipment, Rarity randomRarity,
            CurrencyType priceCurrency, int price, float weight, int requiredMarket)
        {
            ContentGen.Define<MarketOfferDefinition>($"{OfferFolder}/Offer_{id}.asset",
                ("id", id), ("displayName", name), ("rewardType", type), ("rewardAmount", amount), ("equipment", equipment),
                ("randomRarity", randomRarity), ("priceCurrency", priceCurrency), ("priceAmount", price),
                ("weight", weight), ("requiredMarketLevel", requiredMarket));
        }

        private static void GenerateMarketOffers()
        {
            EquipmentDefinition Eq(string id) => ContentGen.FindAll<EquipmentDefinition>(ContentGen.DataRoot).FirstOrDefault(e => e.Id == id);

            Offer("coin_pouch", "Coin Pouch", MarketRewardType.Coins, 500, null, Rarity.Common, CurrencyType.Gems, 5, 1.2f, 1);
            Offer("gem_trade", "Gem Trade", MarketRewardType.Gems, 5, null, Rarity.Common, CurrencyType.Coins, 900, 1f, 1);
            Offer("common_crate", "Common Gear Crate", MarketRewardType.RandomEquipment, 2, null, Rarity.Common, CurrencyType.Coins, 250, 1.5f, 1);
            Offer("iron_ring_deal", "Iron Ring", MarketRewardType.Equipment, 1, Eq("iron_ring"), Rarity.Common, CurrencyType.Coins, 200, 1f, 1);
            Offer("headband_deal", "Ninja Headband", MarketRewardType.Equipment, 1, Eq("ninja_headband"), Rarity.Common, CurrencyType.Coins, 200, 1f, 1);
            Offer("rare_crate", "Rare Gear Crate", MarketRewardType.RandomEquipment, 1, null, Rarity.Rare, CurrencyType.Coins, 600, 1f, 2);
            Offer("jade_charm_deal", "Jade Charm", MarketRewardType.Equipment, 1, Eq("jade_charm"), Rarity.Rare, CurrencyType.Coins, 450, 0.8f, 2);
            Offer("coin_chest", "Coin Chest", MarketRewardType.Coins, 2500, null, Rarity.Common, CurrencyType.Gems, 20, 0.7f, 3);
            Offer("epic_crate", "Epic Gear Crate", MarketRewardType.RandomEquipment, 1, null, Rarity.Epic, CurrencyType.Gems, 30, 0.6f, 3);
            Offer("dragon_scale_deal", "Dragon Scale", MarketRewardType.Equipment, 1, Eq("dragon_scale"), Rarity.Legendary, CurrencyType.Gems, 150, 0.25f, 5);

            var catalog = ContentGen.CreateOrLoad<MarketOfferCatalog>($"{ContentGen.CatalogRoot}/MarketOfferCatalog.asset");
            catalog.EditorSetItems(ContentGen.FindAll<MarketOfferDefinition>(ContentGen.DataRoot).Where(o => !string.IsNullOrEmpty(o.Id)).OrderBy(o => o.RequiredMarketLevel).ThenBy(o => o.Id));
        }
    }
}
