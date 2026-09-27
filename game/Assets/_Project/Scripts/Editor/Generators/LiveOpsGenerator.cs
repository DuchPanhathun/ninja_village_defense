using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.LiveOps;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Live-ops content (EPIC 20): two battle pass seasons, three seasonal events with missions and
    /// battle bonuses, and limited-time offers (event-bound and standalone). Dates are UTC; update them
    /// (or add new assets) as the live calendar moves on.
    /// </summary>
    public static class LiveOpsGenerator
    {
        private const string Folder = ContentGen.DataRoot + "/LiveOps";

        [ContentGenerator("Live ops: battle pass, events, limited offers", 50)]
        public static void Generate()
        {
            Season("season_1", "Season 1: Blossom Blades", "2026-09-01", 60, "skin_assassin_sakura", "samurai");
            Season("season_2", "Season 2: Oni Moon", "2026-10-31", 60, "skin_samurai_oni", "monk");
            ContentGen.CreateOrLoad<SeasonCatalog>($"{ContentGen.CatalogRoot}/SeasonCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<SeasonDefinition>(ContentGen.DataRoot).Where(s => !string.IsNullOrEmpty(s.Id)).OrderBy(s => s.Id));

            GenerateEvents();
            ContentGen.CreateOrLoad<EventCatalog>($"{ContentGen.CatalogRoot}/EventCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<EventDefinition>(ContentGen.DataRoot).Where(e => !string.IsNullOrEmpty(e.Id)).OrderBy(e => e.Id));

            GenerateOffers();
            ContentGen.CreateOrLoad<LimitedOfferCatalog>($"{ContentGen.CatalogRoot}/LimitedOfferCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<LimitedOfferDefinition>(ContentGen.DataRoot).Where(o => !string.IsNullOrEmpty(o.Id)).OrderBy(o => o.Id));
        }

        private static LiveOpsReward Coins(int n) => new(LiveOpsRewardType.Coins, n);
        private static LiveOpsReward Gems(int n) => new(LiveOpsRewardType.Gems, n);
        private static LiveOpsReward Gear(string id, int n = 1) => new(LiveOpsRewardType.Equipment, n, id);
        private static LiveOpsReward PassXp(int n) => new(LiveOpsRewardType.BattlePassXp, n);

        /// <summary>30 tiers: steady coins on the free track with gem/gear spikes; premium doubles up and adds a hero (tier 15) and a skin (tier 30).</summary>
        private static void Season(string id, string name, string start, int days, string finalSkinId, string premiumHeroId)
        {
            var season = ContentGen.CreateOrLoad<SeasonDefinition>($"{Folder}/Seasons/Season_{id}.asset", out bool created);
            if (!created) return;
            ContentGen.Set(season, ("id", id), ("displayName", name), ("description", "Earn XP in battle to climb 30 tiers of rewards."));

            var free = new List<LiveOpsReward>();
            var premium = new List<LiveOpsReward>();
            for (int tier = 1; tier <= 30; tier++)
            {
                free.Add(tier % 10 == 0 ? Gear(tier == 30 ? "blood_talisman" : "jade_charm")
                       : tier % 5 == 0 ? Gems(10)
                       : Coins(150 + tier * 10));
                premium.Add(tier == 30 ? new LiveOpsReward(LiveOpsRewardType.Skin, 1, finalSkinId)
                          : tier == 15 ? new LiveOpsReward(LiveOpsRewardType.Hero, 1, premiumHeroId)
                          : tier % 10 == 0 ? Gear("dragon_scale")
                          : tier % 3 == 0 ? Gems(20)
                          : Coins(400 + tier * 20));
            }
            season.EditorSetTrack(start, days, 300, free, premium);
        }

        private static EventMission Mission(string id, string text, string stat, int target, params LiveOpsReward[] rewards) =>
            new() { id = id, description = text, statId = stat, target = target, rewards = rewards.ToList() };

        private static void Event(string id, string name, string description, string start, int days, float coinBonus, float xpBonus,
            UnityEngine.Color color, params EventMission[] missions)
        {
            var evt = ContentGen.CreateOrLoad<EventDefinition>($"{Folder}/Events/Event_{id}.asset", out bool created);
            if (!created) return;
            ContentGen.Set(evt, ("id", id), ("displayName", name), ("description", description), ("startDate", start),
                ("durationDays", days), ("coinBonus", coinBonus), ("xpBonus", xpBonus), ("themeColor", color));
            evt.EditorSetMissions(missions);
        }

        private static void GenerateEvents()
        {
            Event("cherry_blossom", "Cherry Blossom Festival", "Petals fall on the village — demons drop extra coins!",
                "2026-09-20", 14, 0.25f, 0f, new UnityEngine.Color(1f, 0.7f, 0.82f),
                Mission("cb_kill", "Defeat 500 demons", ProgressStatIds.EnemiesKilled, 500, Gems(20)),
                Mission("cb_wave", "Reach wave 8", ProgressStatIds.WaveReached, 8, Gear("jade_charm")),
                Mission("cb_wins", "Win 2 battles", ProgressStatIds.RunVictory, 2, Gems(30), PassXp(300)),
                Mission("cb_chests", "Open 3 treasure chests", ProgressStatIds.ChestOpened, 3, Coins(1500)),
                Mission("cb_build", "Upgrade buildings 3 times", ProgressStatIds.BuildingUpgraded, 3, Gems(15)));

            Event("oni_moon", "Oni Moon Night", "Under the red moon, every battle teaches more — bonus XP!",
                "2026-10-25", 7, 0f, 0.3f, new UnityEngine.Color(0.9f, 0.25f, 0.2f),
                Mission("om_bosses", "Defeat 5 bosses", ProgressStatIds.BossesKilled, 5, Gems(40)),
                Mission("om_kill", "Defeat 1,000 demons", ProgressStatIds.EnemiesKilled, 1000, Gear("blood_talisman")),
                Mission("om_ult", "Use your ultimate 10 times", ProgressStatIds.UltimateUsed, 10, Coins(2000)),
                Mission("om_quests", "Complete 6 quests", ProgressStatIds.QuestCompleted, 6, PassXp(500)));

            Event("winter_lantern", "Winter Lantern Festival", "Lanterns light the snow — extra coins and XP for every battle.",
                "2026-12-18", 14, 0.2f, 0.2f, new UnityEngine.Color(0.6f, 0.85f, 1f),
                Mission("wl_runs", "Complete 10 battles", ProgressStatIds.RunCompleted, 10, Gems(30)),
                Mission("wl_wave", "Reach wave 15", ProgressStatIds.WaveReached, 15, Gear("dragon_scale")),
                Mission("wl_spend", "Spend 5,000 coins", ProgressStatIds.CoinsSpent, 5000, Gems(25)),
                Mission("wl_login", "Claim 5 daily rewards", ProgressStatIds.DailyLoginClaimed, 5, Coins(2500)));
        }

        private static void Offer(string id, string name, string eventId, string start, int days, CurrencyType currency, int price,
            int limit, string badge, params LiveOpsReward[] rewards)
        {
            var offer = ContentGen.CreateOrLoad<LimitedOfferDefinition>($"{Folder}/Offers/Offer_{id}.asset", out bool created);
            if (!created) return;
            ContentGen.Set(offer, ("id", id), ("displayName", name), ("eventId", eventId ?? string.Empty), ("startDate", start ?? string.Empty),
                ("durationDays", days), ("priceCurrency", currency), ("priceAmount", price), ("purchaseLimit", limit), ("badge", badge));
            offer.EditorSetRewards(rewards);
        }

        private static void GenerateOffers()
        {
            Offer("sakura_bundle", "Sakura Bundle", "cherry_blossom", null, 0, CurrencyType.Gems, 80, 1, "EVENT", Coins(5000), Gear("blood_talisman"));
            Offer("blossom_coins", "Blossom Coin Pouch", "cherry_blossom", null, 0, CurrencyType.Gems, 20, 3, "EVENT", Coins(1500));
            Offer("oni_kit", "Oni Slayer Kit", "oni_moon", null, 0, CurrencyType.Gems, 120, 1, "EVENT", Gear("dragon_scale"), PassXp(500));
            Offer("lantern_pack", "Lantern Pack", "winter_lantern", null, 0, CurrencyType.Gems, 60, 2, "EVENT", Coins(3000), Gems(10), PassXp(300));
            Offer("autumn_training", "Autumn Training Crate", null, "2026-09-15", 30, CurrencyType.Coins, 2500, 2, "LIMITED",
                Gear("ninja_headband"), Gear("iron_ring"), PassXp(200));
        }
    }
}
