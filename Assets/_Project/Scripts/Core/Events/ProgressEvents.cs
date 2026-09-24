namespace NinjaVillage.Core.Events
{
    /// <summary>
    /// Generic "something countable happened" signal for meta systems that must not depend on
    /// every feature's own event types — daily/weekly quests, achievements, battle-pass XP and
    /// analytics all listen to this one channel. Features report with <see cref="Progress.Report"/>.
    /// Combat-level signals (kills, bosses, level-ups) keep their own dedicated events.
    /// </summary>
    public readonly struct ProgressStatEvent : IGameEvent
    {
        public readonly string StatId;
        public readonly int Amount;
        /// <summary>Optional detail, e.g. which building/hero/product (ids).</summary>
        public readonly string Subject;

        public ProgressStatEvent(string statId, int amount, string subject = null)
        {
            StatId = statId;
            Amount = amount;
            Subject = subject;
        }
    }

    public static class Progress
    {
        public static void Report(string statId, int amount = 1, string subject = null) =>
            EventBus<ProgressStatEvent>.Raise(new ProgressStatEvent(statId, amount, subject));
    }

    /// <summary>Stable stat ids. Quest/achievement definitions reference these strings.</summary>
    public static class ProgressStatIds
    {
        // Battle (reported by the battle systems in addition to their dedicated events)
        public const string RunCompleted = "run_completed";
        public const string RunVictory = "run_victory";
        public const string WaveReached = "wave_reached";          // Amount = wave number
        public const string SurvivedSeconds = "survived_seconds";  // Amount = seconds in the run
        public const string EnemiesKilled = "enemies_killed";
        public const string BossesKilled = "bosses_killed";
        public const string UltimateUsed = "ultimate_used";
        public const string SkillPicked = "skill_picked";
        public const string EvolutionDiscovered = "evolution_discovered";
        public const string ChestOpened = "chest_opened";
        public const string EquipmentCollected = "equipment_collected";

        // Economy
        public const string CoinsEarned = "coins_earned";
        public const string CoinsSpent = "coins_spent";
        public const string GemsSpent = "gems_spent";

        // Meta progression
        public const string BuildingUpgraded = "building_upgraded";
        public const string WeaponUpgraded = "weapon_upgraded";
        public const string WeaponCrafted = "weapon_crafted";
        public const string HeroUnlocked = "hero_unlocked";
        public const string HeroUpgraded = "hero_upgraded";
        public const string PetUnlocked = "pet_unlocked";
        public const string PetUpgraded = "pet_upgraded";
        public const string TalentUnlocked = "talent_unlocked";
        public const string BlessingUnlocked = "blessing_unlocked";
        public const string MarketPurchase = "market_purchase";

        // Village activities (EPIC 24)
        public const string CropsHarvested = "crops_harvested";    // Amount = goods harvested
        public const string MealsCooked = "meals_cooked";          // Amount = meals collected from the Kitchen
        public const string RequestDelivered = "request_delivered"; // a villager request was completed; Subject = request id
        public const string TreasuryCollected = "treasury_collected"; // Amount = coins collected from the treasury
        public const string HouseBuilt = "house_built";              // a house was built or grew a level
        public const string FishCaught = "fish_caught";              // Amount = fish landed
        public const string BarsMined = "bars_mined";                // Amount = metal bars collected from the mine
        public const string PetCared = "pet_cared";                  // a pet was petted or fed; Subject = pet id

        // Engagement / monetization
        public const string DailyLoginClaimed = "daily_login_claimed";
        public const string QuestCompleted = "quest_completed";
        public const string AdWatched = "ad_watched";
        public const string Purchase = "purchase";
    }
}
