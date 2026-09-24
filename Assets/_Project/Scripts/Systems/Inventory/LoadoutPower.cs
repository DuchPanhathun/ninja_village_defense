using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>
    /// The headline numbers of the current loadout for the Equipment screen: damage per hit and max health,
    /// adding up the same sources the run-start modifiers apply in battle — the equipped weapon (and its forge
    /// tier), the selected hero, equipped gear, talents and village bonuses. (Skills picked during a run
    /// come on top.)
    /// </summary>
    public static class LoadoutPower
    {
        /// <summary>The player's max health before any bonus (the Battle scene's player).</summary>
        public const float BasePlayerHealth = 100f;
        private const float FallbackDamage = 10f;

        public readonly struct Totals
        {
            public readonly int Attack;
            public readonly int Health;
            public Totals(int attack, int health)
            {
                Attack = attack;
                Health = health;
            }
        }

        public static Totals Compute(SaveData save)
        {
            if (save == null) return new Totals((int)FallbackDamage, (int)BasePlayerHealth);

            var hero = HeroService.Get(save.Heroes.SelectedHeroId) ?? HeroService.GetSelected();
            var heroStats = hero != null ? hero.GetStatsAtLevel(Mathf.Clamp(save.Heroes.GetLevel(hero.Id), 1, hero.MaxLevel)) : default;

            // Equipped weapon wins over the hero's signature weapon (InventoryRunModifier runs after HeroRunModifier).
            var weapon = InventoryService.GetWeapon(save.Inventory.EquippedWeaponId);
            if (weapon == null && hero != null) weapon = hero.SignatureWeapon;
            float damage = weapon != null ? weapon.GetDamage(Mathf.Max(1, save.Inventory.GetWeaponLevel(weapon.Id))) : FallbackDamage;

            float attack = heroStats.attackDamagePercent;
            float health = heroStats.maxHealthPercent;
            if (weapon != null) attack += ForgeService.TierAttackBonus(weapon.Id);
            foreach (var id in save.Inventory.EquippedEquipmentIds)
            {
                var item = InventoryService.GetEquipment(id);
                if (item != null) attack += item.AttackDamageBonus;
            }

            var talents = TalentService.Catalog;
            if (talents != null)
                foreach (var talent in talents.All)
                {
                    if (talent == null) continue;
                    int rank = save.Talents.Nodes.GetLevel(talent.Id);
                    if (rank <= 0) continue;
                    if (talent.Stat == TalentStat.AttackDamage) attack += talent.ValueAt(rank);
                    else if (talent.Stat == TalentStat.MaxHealth) health += talent.ValueAt(rank);
                }

            var village = VillageService.ComputeBonuses(save);
            attack += village.AttackDamage;
            health += village.MaxHealth;

            return new Totals(
                Mathf.RoundToInt(damage * Mathf.Max(0.1f, 1f + attack)),
                Mathf.RoundToInt((BasePlayerHealth + heroStats.maxHealthFlat) * Mathf.Max(0.1f, 1f + health)));
        }
    }
}
