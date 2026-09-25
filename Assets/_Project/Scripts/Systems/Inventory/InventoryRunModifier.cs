using NinjaVillage.Systems.Meta;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>
    /// Where the persistent inventory meets the battle: equips the weapon chosen in the Inventory at
    /// its Forge level, applies its grade's attack bonus, and applies every equipped equipment piece at the best
    /// grade owned.
    /// Runs after the hero (<see cref="RunModifierOrder.Inventory"/>), so the player's own weapon choice
    /// wins over a hero's signature weapon.
    /// </summary>
    public sealed class InventoryRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Inventory;

        public void Apply(RunStartContext context)
        {
            var inv = InventoryService.Data;

            var weapon = InventoryService.GetWeapon(inv.EquippedWeaponId);
            if (weapon != null)
            {
                context.Weapon = weapon;
                context.WeaponLevel = Mathf.Max(1, inv.GetWeaponLevel(weapon.Id));

                float gradeBonus = GradeRules.WeaponAttackBonus(InventoryService.WeaponGrade(weapon.Id));
                if (gradeBonus != 0f && context.Stats != null)
                    context.Stats.AddAttackDamageMultiplier(gradeBonus);
            }

            if (context.Stats == null) return;
            foreach (var equipmentId in inv.EquippedEquipmentIds)
            {
                var equipment = InventoryService.GetEquipment(equipmentId);
                if (equipment != null)
                    equipment.Apply(context.Stats, GradeRules.StatScale(InventoryService.NativeGrade(equipment), InventoryService.GearGrade(equipment)));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new InventoryRunModifier());
    }
}
