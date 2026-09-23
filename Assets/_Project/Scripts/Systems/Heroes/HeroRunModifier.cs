using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Heroes
{
    /// <summary>
    /// Brings the selected hero into the battle (runs first, <see cref="RunModifierOrder.Hero"/>):
    /// pushes the hero's level-scaled stats onto the player, picks its signature weapon (the Forge /
    /// inventory modifier runs later and may level or replace it), queues starting skills, swaps
    /// the ultimate, records the hero id and spawns companions such as the Beast Ninja's wolves.
    /// </summary>
    public sealed class HeroRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Hero;

        public void Apply(RunStartContext context)
        {
            if (context == null) return;

            var hero = HeroService.GetSelected();
            if (hero == null) return;

            var save = context.Save ?? SaveService.Data;
            int level = Mathf.Clamp(save.Heroes.GetLevel(hero.Id), 1, hero.MaxLevel);

            ApplyStats(context, hero.GetStatsAtLevel(level));

            if (hero.SignatureWeapon != null)
            {
                context.Weapon = hero.SignatureWeapon;
                context.WeaponLevel = Mathf.Max(1, save.Inventory.GetWeaponLevel(hero.SignatureWeapon.Id));
            }

            foreach (var skill in hero.StartingSkills)
                if (skill != null && !context.StartingSkills.Contains(skill))
                    context.StartingSkills.Add(skill);

            if (hero.Ultimate != null)
                context.UltimateOverride = hero.Ultimate;

            context.HeroId = hero.Id;

            SpawnCompanions(context, hero, level);
        }

        /// <summary>Applies a hero stat block to the run (also usable by other systems for hero-like bonuses).</summary>
        public static void ApplyStats(RunStartContext context, HeroStatBlock stats)
        {
            context.MaxHealthMultiplier += stats.maxHealthPercent;
            context.MaxHealthFlatBonus += stats.maxHealthFlat;
            context.StartingShield += stats.startingShield;

            PlayerStats playerStats = context.Stats;
            if (playerStats == null) return;

            if (stats.attackDamagePercent != 0f) playerStats.AddAttackDamageMultiplier(stats.attackDamagePercent);
            if (stats.attackSpeedPercent != 0f) playerStats.AddAttackSpeedMultiplier(stats.attackSpeedPercent);
            if (stats.moveSpeedPercent != 0f) playerStats.AddMoveSpeedMultiplier(stats.moveSpeedPercent);
            if (stats.critChance != 0f) playerStats.AddCritChanceBonus(stats.critChance);
            if (stats.critDamage != 0f) playerStats.AddCritMultiplierBonus(stats.critDamage);
            if (stats.damageReduction != 0f) playerStats.AddDamageReduction(stats.damageReduction);
            if (stats.healPerSecond != 0f) playerStats.AddHealPerSecond(stats.healPerSecond);
            if (stats.dodgeChance != 0f) playerStats.AddDodgeChance(stats.dodgeChance);
            if (stats.xpGainPercent != 0f) playerStats.AddXpGainMultiplier(stats.xpGainPercent);
            if (stats.goldBonusPercent != 0f) playerStats.AddGoldBonusMultiplier(stats.goldBonusPercent);
            if (stats.ultimateChargePercent != 0f) playerStats.AddUltimateChargeMultiplier(stats.ultimateChargePercent);
        }

        private static void SpawnCompanions(RunStartContext context, HeroDefinition hero, int heroLevel)
        {
            var pet = hero.CompanionPet;
            if (pet == null || context.Player == null) return;

            int count = HeroRules.GetCompanionCount(hero.CompanionCount, hero.ExtraCompanionEveryLevels, heroLevel, hero.MaxCompanions);
            if (count <= 0) return;

            int petLevel = HeroRules.GetCompanionLevel(heroLevel, pet.MaxLevel);
            Transform owner = context.Player.transform;
            LayerMask enemyMask = context.AutoAttack != null ? context.AutoAttack.EnemyMask : default;

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                Vector2 position = (Vector2)owner.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f;
                var controller = PetFactory.Spawn(pet, petLevel, default, hero.CompanionPowerScale, owner, context.Stats, enemyMask, position);
                if (controller != null) controller.name = $"{hero.Id}_Companion_{i + 1}";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new HeroRunModifier());
    }
}
