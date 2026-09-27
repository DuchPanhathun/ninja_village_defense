using System.Linq;
using NinjaVillage.Core;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using Weapons = NinjaVillage.Gameplay.Weapons;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Battle definitions that had code but no assets yet: the three ultimates (EPIC 8) and the
    /// Giant Shuriken / Smoke Bomb skills (EPIC 6). Runs before the hero generator, which assigns
    /// ultimates to heroes.
    /// </summary>
    public static class BattleContentGenerator
    {
        public const string UltimateFolder = ContentGen.DataRoot + "/Ultimates";
        public const string SkillFolder = ContentGen.DataRoot + "/Skills";

        [ContentGenerator("Ultimates & missing skills", 5)]
        public static void Generate()
        {
            ContentGen.Define<ShadowCloneArmyUltimate>($"{UltimateFolder}/Ultimate_ShadowCloneArmy.asset",
                ("id", "shadow_clone_army"), ("displayName", "Shadow Clone Army"),
                ("description", "Summon 8 shadow clones that fight beside you for 15 seconds."),
                ("cooldownSeconds", 90f), ("cloneCount", 8), ("cloneDuration", 15f), ("cloneOrbitRadius", 2f), ("damageFraction", 0.5f));

            ContentGen.Define<DragonSlashUltimate>($"{UltimateFolder}/Ultimate_DragonSlash.asset",
                ("id", "dragon_slash"), ("displayName", "Dragon Slash"),
                ("description", "A huge dragon crosses the battlefield, cutting down everything."),
                ("cooldownSeconds", 90f), ("damage", 500f), ("radius", 20f));

            ContentGen.Define<HeavenlyStormUltimate>($"{UltimateFolder}/Ultimate_HeavenlyStorm.asset",
                ("id", "heavenly_storm"), ("displayName", "Heavenly Storm"),
                ("description", "A rain of shuriken strikes all around you for 5 seconds."),
                ("cooldownSeconds", 90f), ("burstCount", 50), ("damagePerBurst", 20f), ("burstRadius", 1.2f), ("stormRadius", 8f), ("duration", 5f));

            ContentGen.Define<GiantShurikenSkill>($"{SkillFolder}/Skill_GiantShuriken.asset",
                ("id", "Skill_GiantShuriken"), ("displayName", "Giant Shuriken"),
                ("description", "Bigger projectiles that hit harder."),
                ("category", SkillCategory.Offensive), ("rarity", Rarity.Rare), ("maxLevel", 5),
                ("sizePerLevel", 0.35f), ("damagePerLevel", 0.25f));

            ContentGen.Define<SmokeBombSkill>($"{SkillFolder}/Skill_SmokeBomb.asset",
                ("id", "Skill_SmokeBomb"), ("displayName", "Smoke Bomb"),
                ("description", "Periodically vanish in smoke — enemies lose track of you."),
                ("category", SkillCategory.Defensive), ("rarity", Rarity.Rare), ("maxLevel", 5), ("element", Element.Shadow),
                ("activeDurationPerLevel", 1.5f), ("cycleDuration", 6f));

            GenerateEvolutionContent(); // after Smoke Bomb: the Invisible Assassin recipe needs it
        }

        /// <summary>
        /// EPIC 7: the missing ingredient skills (Wind, Clone, Teleport), the four evolution results
        /// (flagged isEvolution so they never appear in level-up rolls) and their recipes + catalog.
        /// </summary>
        private static void GenerateEvolutionContent()
        {
            var wind = ContentGen.Define<WindSkill>($"{SkillFolder}/Skill_Wind.asset",
                ("id", "Skill_Wind"), ("displayName", "Gale Step"), ("description", "Ride the wind: move and attack faster."),
                ("category", SkillCategory.Utility), ("rarity", Rarity.Common), ("maxLevel", 5), ("element", Element.Wind));
            var clone = ContentGen.Define<CloneSkill>($"{SkillFolder}/Skill_Clone.asset",
                ("id", "Skill_Clone"), ("displayName", "Shadow Clone"), ("description", "A shadow clone fights beside you. +1 clone per level."),
                ("category", SkillCategory.Offensive), ("rarity", Rarity.Rare), ("maxLevel", 3), ("element", Element.Shadow));
            var teleport = ContentGen.Define<TeleportSkill>($"{SkillFolder}/Skill_Teleport.asset",
                ("id", "Skill_Teleport"), ("displayName", "Blink"), ("description", "When hit, vanish and reappear away from danger."),
                ("category", SkillCategory.Defensive), ("rarity", Rarity.Rare), ("maxLevel", 5), ("element", Element.Wind));

            var firestorm = ContentGen.Define<FirestormSkill>($"{SkillFolder}/Evolutions/Skill_Firestorm.asset",
                ("id", "Skill_Firestorm"), ("displayName", "Firestorm"), ("description", "A ring of wind-fed fire erupts around you, burning every demon near."),
                ("category", SkillCategory.Offensive), ("rarity", Rarity.Legendary), ("maxLevel", 1), ("element", Element.Fire), ("isEvolution", true));
            var thunder = ContentGen.Define<ThunderKunaiSkill>($"{SkillFolder}/Evolutions/Skill_ThunderKunai.asset",
                ("id", "Skill_ThunderKunai"), ("displayName", "Thunder Kunai"), ("description", "Faster throws, and chain lightning that leaps between demons."),
                ("category", SkillCategory.Offensive), ("rarity", Rarity.Legendary), ("maxLevel", 1), ("element", Element.Lightning), ("isEvolution", true));
            var army = ContentGen.Define<ShadowArmySkill>($"{SkillFolder}/Evolutions/Skill_ShadowArmy.asset",
                ("id", "Skill_ShadowArmy"), ("displayName", "Shadow Army"), ("description", "Shadow samurai circle you and cut down anything close."),
                ("category", SkillCategory.Offensive), ("rarity", Rarity.Legendary), ("maxLevel", 1), ("element", Element.Shadow), ("isEvolution", true));
            var assassin = ContentGen.Define<InvisibleAssassinSkill>($"{SkillFolder}/Evolutions/Skill_InvisibleAssassin.asset",
                ("id", "Skill_InvisibleAssassin"), ("displayName", "Invisible Assassin"), ("description", "Stay unseen most of the fight; strikes from the shadows crit far more."),
                ("category", SkillCategory.Defensive), ("rarity", Rarity.Legendary), ("maxLevel", 1), ("element", Element.Shadow), ("isEvolution", true));

            var fireBlade = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillDefinition>($"{SkillFolder}/Skill_FireBlade.asset");
            var lightning = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillDefinition>($"{SkillFolder}/Skill_LightningStrike.asset");
            var smoke = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillDefinition>($"{SkillFolder}/Skill_SmokeBomb.asset");
            if (smoke == null)
                smoke = ContentGen.Define<SmokeBombSkill>($"{SkillFolder}/Skill_SmokeBomb.asset", ("id", "Skill_SmokeBomb"), ("displayName", "Smoke Bomb"));
            var kunai = UnityEditor.AssetDatabase.LoadAssetAtPath<Weapons.WeaponDefinition>(ContentGen.DataRoot + "/Weapons/Kunai.asset");
            var katana = UnityEditor.AssetDatabase.LoadAssetAtPath<Weapons.WeaponDefinition>(ContentGen.DataRoot + "/Weapons/Katana.asset");

            string folder = ContentGen.DataRoot + "/Evolutions";
            Recipe(folder, "firestorm", "Fire + Wind", new[] { fireBlade, wind }, null, firestorm, "Flames that ride the wind...");
            Recipe(folder, "thunder_kunai", "Lightning + Kunai", new[] { lightning }, kunai, thunder, "A blade that remembers the storm...");
            Recipe(folder, "shadow_army", "Clone + Katana", new SkillDefinition[] { clone }, katana, army, "Many shadows, one sword...");
            Recipe(folder, "invisible_assassin", "Smoke + Teleport", new[] { smoke, teleport }, null, assassin, "Vanish, then vanish again...");

            ContentGen.CreateOrLoad<NinjaVillage.Systems.Evolution.EvolutionCatalog>($"{ContentGen.CatalogRoot}/EvolutionCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<NinjaVillage.Systems.Evolution.EvolutionRecipe>(ContentGen.DataRoot)
                    .Where(r => !string.IsNullOrEmpty(r.Id)).OrderBy(r => r.Id));
        }

        private static void Recipe(string folder, string id, string name, SkillDefinition[] skills, Weapons.WeaponDefinition weapon,
            SkillDefinition result, string hint)
        {
            ContentGen.Define<NinjaVillage.Systems.Evolution.EvolutionRecipe>($"{folder}/Evolution_{id}.asset",
                ("id", id), ("displayName", name), ("requiredSkills", skills.Where(x => x != null).ToList()),
                ("requiredWeapon", weapon), ("resultSkill", result), ("hint", hint));
        }
    }
}
