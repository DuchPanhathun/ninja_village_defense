using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Core;
using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Pets;
using NinjaVillage.Systems.Talents;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Heroes (EPIC 13), pets + pet gear (EPIC 12) and the talent tree (EPIC 14). Pets are generated
    /// first because the Beast Ninja's wolves reference the Wolf pet.
    /// </summary>
    public static class HeroPetTalentGenerator
    {
        private const string HeroFolder = ContentGen.DataRoot + "/Heroes";
        private const string PetFolder = ContentGen.DataRoot + "/Pets";
        private const string TalentFolder = ContentGen.DataRoot + "/Talents";

        [ContentGenerator("Pets, heroes & talents", 20)]
        public static void Generate()
        {
            GeneratePets();
            GenerateHeroes();
            GenerateTalents();
        }

        // ------------------------------------------------------------------ pets

        private static PetDefinition Pet(string id, string name, string summary, PetAbilityType ability, int sort, Color color,
            CurrencyType currency, int cost, int petHouse, bool premium,
            float power, float growth, float cooldown, float range, float duration, float moveSpeed,
            (string, object)[] ownerBonus = null)
        {
            var values = new List<(string, object)>
            {
                ("id", id), ("displayName", name), ("description", summary), ("abilitySummary", summary),
                ("abilityType", ability), ("sortOrder", sort), ("placeholderColor", color), ("placeholderScale", 0.6f),
                ("followDistance", 1.4f), ("unlockCurrency", currency), ("unlockCost", cost),
                ("requiredPetHouseLevel", petHouse), ("isPremium", premium), ("maxLevel", 20),
                ("upgradeCostBase", 80), ("upgradeCostExponent", 1.55f),
                ("stats.basePower", power), ("stats.powerGrowthPerLevel", growth),
                ("stats.baseCooldown", cooldown), ("stats.cooldownReductionPerLevel", 0.02f),
                ("stats.baseRange", range), ("stats.rangePerLevel", 0.1f),
                ("stats.baseEffectDuration", duration), ("stats.effectDurationPerLevel", 0.05f),
                ("stats.moveSpeed", moveSpeed), ("ownerDamageInheritance", 0.5f),
            };
            if (ownerBonus != null) values.AddRange(ownerBonus);
            return ContentGen.Define<PetDefinition>($"{PetFolder}/Pet_{id}.asset", values.ToArray());
        }

        private static void GeneratePets()
        {
            Pet("fox", "Fox", "Collects XP orbs around you and brings them home.", PetAbilityType.XpCollector, 0,
                new Color(1f, 0.55f, 0.2f), CurrencyType.Coins, 400, 0, false, 6f, 0.06f, 0.5f, 4.5f, 0f, 8f,
                new (string, object)[] { ("ownerBonus.xpGainPercent", 0.05f), ("ownerBonusPerLevel.xpGainPercent", 0.005f),
                                         ("ownerBonus.xpMagnetPercent", 0.1f), ("ownerBonusPerLevel.xpMagnetPercent", 0.01f) });
            Pet("wolf", "Wolf", "Charges at nearby demons and bites them.", PetAbilityType.Bite, 1,
                new Color(0.55f, 0.55f, 0.6f), CurrencyType.Coins, 800, 1, false, 12f, 0.08f, 1.2f, 5f, 0f, 9f);
            Pet("hawk", "Hawk", "Scouts ahead and reveals hidden treasure.", PetAbilityType.TreasureHunter, 2,
                new Color(0.55f, 0.4f, 0.25f), CurrencyType.Coins, 1200, 2, false, 8f, 0.1f, 12f, 6f, 0f, 10f,
                new (string, object)[] { ("ownerBonus.luckyDropChance", 0.02f), ("ownerBonusPerLevel.luckyDropChance", 0.002f) });
            Pet("monkey", "Monkey", "Throws bananas that damage and stun demons.", PetAbilityType.BananaThrow, 3,
                new Color(0.75f, 0.6f, 0.4f), CurrencyType.Coins, 1600, 3, false, 9f, 0.08f, 2.5f, 6f, 1f, 8f);
            Pet("dragon", "Dragon", "Breathes a cone of fire that burns everything in front of it.", PetAbilityType.FireBreath, 4,
                new Color(0.9f, 0.2f, 0.15f), CurrencyType.Gems, 200, 4, true, 14f, 0.1f, 3.5f, 4f, 3f, 7f);

            PetGear("collar", "Spiked Collar", Rarity.Common, 0, CurrencyType.Coins, 300, 0, ("bonus.powerPercent", 0.10f));
            PetGear("anklet", "Swift Anklet", Rarity.Common, 1, CurrencyType.Coins, 250, 0, ("bonus.moveSpeedPercent", 0.20f));
            PetGear("bell", "Temple Bell", Rarity.Rare, 2, CurrencyType.Coins, 500, 1, ("bonus.cooldownReduction", 0.10f));
            PetGear("feather", "Wind Feather", Rarity.Rare, 3, CurrencyType.Coins, 800, 2, ("bonus.rangeFlat", 1f));
            PetGear("ember", "Ember Gem", Rarity.Epic, 4, CurrencyType.Coins, 1200, 3, ("bonus.durationPercent", 0.25f), ("bonus.powerPercent", 0.10f));

            ContentGen.CreateOrLoad<PetCatalog>($"{ContentGen.CatalogRoot}/PetCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<PetDefinition>(ContentGen.DataRoot).Where(p => !string.IsNullOrEmpty(p.Id)).OrderBy(p => p.SortOrder));
            ContentGen.CreateOrLoad<PetEquipmentCatalog>($"{ContentGen.CatalogRoot}/PetEquipmentCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<PetEquipmentDefinition>(ContentGen.DataRoot).Where(p => !string.IsNullOrEmpty(p.Id)).OrderBy(p => p.SortOrder));
        }

        private static void PetGear(string id, string name, Rarity rarity, int sort, CurrencyType currency, int cost, int petHouse,
            params (string, object)[] bonus)
        {
            var values = new List<(string, object)>
            {
                ("id", "petgear_" + id), ("displayName", name), ("rarity", rarity), ("sortOrder", sort),
                ("costCurrency", currency), ("cost", cost), ("requiredPetHouseLevel", petHouse),
            };
            values.AddRange(bonus);
            ContentGen.Define<PetEquipmentDefinition>($"{PetFolder}/Gear/PetGear_{id}.asset", values.ToArray());
        }

        // ------------------------------------------------------------------ heroes

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void Hero(string id, string name, string role, int sort, Color theme, bool starter,
            CurrencyType currency, int cost, int dojo, bool premium, WeaponDefinition weapon, UltimateDefinition ultimate,
            SkillDefinition[] startingSkills, (string, object)[] stats, (string, object)[] extra = null)
        {
            var values = new List<(string, object)>
            {
                ("id", id), ("displayName", name), ("description", role), ("roleSummary", role),
                ("themeColor", theme), ("sortOrder", sort), ("unlockedByDefault", starter),
                ("unlockCurrency", currency), ("unlockCost", cost), ("requiredDojoLevel", dojo), ("isPremium", premium),
                ("maxLevel", 30), ("upgradeCostBase", 120), ("upgradeCostExponent", 1.6f),
                ("signatureWeapon", weapon), ("ultimate", ultimate),
                ("startingSkills", (startingSkills ?? new SkillDefinition[0]).Where(s => s != null).ToList()),
            };
            values.AddRange(stats);
            if (extra != null) values.AddRange(extra);
            ContentGen.Define<HeroDefinition>($"{HeroFolder}/Hero_{id}.asset", values.ToArray());
        }

        private static void GenerateHeroes()
        {
            string w = ContentGen.DataRoot + "/Weapons/";
            string u = BattleContentGenerator.UltimateFolder + "/";
            string s = BattleContentGenerator.SkillFolder + "/";

            Hero("assassin", "Assassin", "Fast attacks and deadly crits, but fragile.", 0, new Color(0.55f, 0.3f, 0.85f), true,
                CurrencyType.Coins, 0, 0, false, Load<WeaponDefinition>(w + "Kunai.asset"),
                Load<UltimateDefinition>(u + "Ultimate_ShadowCloneArmy.asset"), null,
                new (string, object)[]
                {
                    ("baseStats.attackSpeedPercent", 0.25f), ("baseStats.critChance", 0.08f), ("baseStats.maxHealthPercent", -0.2f),
                    ("baseStats.moveSpeedPercent", 0.10f),
                    ("statsPerLevel.attackDamagePercent", 0.02f), ("statsPerLevel.attackSpeedPercent", 0.01f), ("statsPerLevel.critChance", 0.003f),
                });

            Hero("samurai", "Samurai", "A tank with a huge sword. Slow, but nearly unbreakable.", 1, new Color(0.85f, 0.25f, 0.2f), false,
                CurrencyType.Coins, 1500, 1, false, Load<WeaponDefinition>(w + "Katana.asset"),
                Load<UltimateDefinition>(u + "Ultimate_DragonSlash.asset"), null,
                new (string, object)[]
                {
                    ("baseStats.maxHealthPercent", 0.4f), ("baseStats.damageReduction", 0.10f), ("baseStats.attackDamagePercent", 0.15f),
                    ("baseStats.attackSpeedPercent", -0.10f), ("baseStats.moveSpeedPercent", -0.05f),
                    ("statsPerLevel.maxHealthPercent", 0.02f), ("statsPerLevel.attackDamagePercent", 0.02f), ("statsPerLevel.damageReduction", 0.003f),
                });

            Hero("monk", "Monk", "Heals over time and starts every battle shielded.", 2, new Color(1f, 0.7f, 0.25f), false,
                CurrencyType.Coins, 2500, 2, false, Load<WeaponDefinition>(w + "ChainSickle.asset"),
                Load<UltimateDefinition>(u + "Ultimate_HeavenlyStorm.asset"), null,
                new (string, object)[]
                {
                    ("baseStats.healPerSecond", 1f), ("baseStats.maxHealthPercent", 0.1f), ("baseStats.startingShield", 20f),
                    ("baseStats.xpGainPercent", 0.10f),
                    ("statsPerLevel.healPerSecond", 0.08f), ("statsPerLevel.attackDamagePercent", 0.015f), ("statsPerLevel.startingShield", 1f),
                });

            Hero("mage_ninja", "Mage Ninja", "Masters the elements — starts with Fire Blade and Lightning Strike.", 3, new Color(0.3f, 0.6f, 1f), false,
                CurrencyType.Coins, 4000, 3, false, Load<WeaponDefinition>(w + "Shuriken.asset"),
                Load<UltimateDefinition>(u + "Ultimate_HeavenlyStorm.asset"),
                new[] { Load<SkillDefinition>(s + "Skill_FireBlade.asset"), Load<SkillDefinition>(s + "Skill_LightningStrike.asset") },
                new (string, object)[]
                {
                    ("baseStats.attackDamagePercent", 0.10f), ("baseStats.ultimateChargePercent", 0.20f), ("baseStats.maxHealthPercent", -0.05f),
                    ("statsPerLevel.attackDamagePercent", 0.025f), ("statsPerLevel.ultimateChargePercent", 0.01f),
                });

            Hero("beast_ninja", "Beast Ninja", "Fights alongside a pack of wolves that grows with his level.", 4, new Color(0.35f, 0.65f, 0.35f), false,
                CurrencyType.Gems, 300, 0, true, Load<WeaponDefinition>(w + "Bow.asset"),
                Load<UltimateDefinition>(u + "Ultimate_ShadowCloneArmy.asset"), null,
                new (string, object)[]
                {
                    ("baseStats.moveSpeedPercent", 0.05f), ("baseStats.maxHealthPercent", 0.1f),
                    ("statsPerLevel.attackDamagePercent", 0.015f), ("statsPerLevel.maxHealthPercent", 0.01f),
                },
                new (string, object)[]
                {
                    ("companionPet", Load<PetDefinition>(PetFolder + "/Pet_wolf.asset")), ("companionCount", 1),
                    ("extraCompanionEveryLevels", 10), ("maxCompanions", 3), ("companionPowerScale", 0.6f),
                });

            ContentGen.CreateOrLoad<HeroCatalog>($"{ContentGen.CatalogRoot}/HeroCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<HeroDefinition>(ContentGen.DataRoot).Where(h => !string.IsNullOrEmpty(h.Id)).OrderBy(h => h.SortOrder));
        }

        // ------------------------------------------------------------------ talents

        private static TalentDefinition Talent(string id, string name, TalentCategory category, int tier, TalentStat stat,
            float perRank, params TalentDefinition[] prerequisites)
        {
            int baseCost = tier == 0 ? 150 : tier == 1 ? 250 : 400;
            return ContentGen.Define<TalentDefinition>($"{TalentFolder}/Talent_{id}.asset",
                ("id", id), ("displayName", name), ("category", category), ("tier", tier), ("stat", stat),
                ("valuePerRank", perRank), ("maxRank", 5), ("baseCost", baseCost), ("costGrowth", 1.5f),
                ("prerequisites", prerequisites.Where(p => p != null).ToList()));
        }

        private static void GenerateTalents()
        {
            var blades = Talent("sharpened_blades", "Sharpened Blades", TalentCategory.Offense, 0, TalentStat.AttackDamage, 0.03f);
            var hands = Talent("quick_hands", "Quick Hands", TalentCategory.Offense, 1, TalentStat.AttackSpeed, 0.03f, blades);
            var eye = Talent("keen_eye", "Keen Eye", TalentCategory.Offense, 1, TalentStat.CritChance, 0.01f, blades);
            Talent("lethal_focus", "Lethal Focus", TalentCategory.Offense, 2, TalentStat.CritDamage, 0.05f, eye);
            Talent("storm_heart", "Storm Heart", TalentCategory.Offense, 2, TalentStat.UltimateCharge, 0.05f, hands);

            var body = Talent("iron_body", "Iron Body", TalentCategory.Defense, 0, TalentStat.MaxHealth, 0.04f);
            var stone = Talent("stone_skin", "Stone Skin", TalentCategory.Defense, 1, TalentStat.DamageReduction, 0.01f, body);
            var meditation = Talent("meditation", "Meditation", TalentCategory.Defense, 1, TalentStat.HealPerSecond, 0.2f, body);
            Talent("evasion", "Evasion", TalentCategory.Defense, 2, TalentStat.DodgeChance, 0.01f, stone);
            Talent("spirit_ward", "Spirit Ward", TalentCategory.Defense, 2, TalentStat.StartingShield, 10f, meditation);

            var feet = Talent("swift_feet", "Swift Feet", TalentCategory.Utility, 0, TalentStat.MoveSpeed, 0.02f);
            var scholar = Talent("scholar", "Scholar", TalentCategory.Utility, 1, TalentStat.XpGain, 0.03f, feet);
            var merchant = Talent("merchant", "Merchant", TalentCategory.Utility, 1, TalentStat.GoldGain, 0.04f, feet);
            Talent("magnetism", "Magnetism", TalentCategory.Utility, 2, TalentStat.PickupRadius, 0.08f, scholar);
            Talent("lucky_star", "Lucky Star", TalentCategory.Utility, 2, TalentStat.LuckyDrop, 0.01f, merchant);

            ContentGen.CreateOrLoad<TalentCatalog>($"{ContentGen.CatalogRoot}/TalentCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<TalentDefinition>(ContentGen.DataRoot).Where(t => !string.IsNullOrEmpty(t.Id))
                    .OrderBy(t => t.Category).ThenBy(t => t.Tier).ThenBy(t => t.Id));
        }
    }
}
