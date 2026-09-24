using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// Brings the active pet into the battle: applies its passive owner bonus (XP, luck, gold, pickup
    /// radius, speed) to the player and spawns it next to them with its level and equipped gear.
    /// Pets without a prefab are built from placeholders by <see cref="PetFactory"/>, so this works
    /// before any pet art exists.
    /// </summary>
    public sealed class PetRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Pets;

        public void Apply(RunStartContext context)
        {
            var save = context.Save ?? SaveService.Data;
            var pet = PetService.Get(save.Pets.ActivePetId);
            if (pet == null || !save.Pets.IsUnlocked(pet.Id)) return;

            int level = Mathf.Clamp(save.Pets.GetLevel(pet.Id), 1, pet.MaxLevel);
            ApplyOwnerBonus(context, pet.GetOwnerBonus(level));

            if (context.Player == null) return;
            Transform owner = context.Player.transform;
            LayerMask enemyMask = context.AutoAttack != null ? context.AutoAttack.EnemyMask : default;
            Vector2 position = (Vector2)owner.position + new Vector2(-1f, 0.5f);

            var controller = PetFactory.Spawn(pet, level, PetService.GetGear(pet.Id), 1f, owner, context.Stats, enemyMask, position);
            if (controller != null) controller.name = $"Pet_{pet.Id}";
        }

        public static void ApplyOwnerBonus(RunStartContext context, PetOwnerBonus bonus)
        {
            var stats = context.Stats;
            if (stats == null || bonus.IsEmpty) return;
            if (bonus.xpGainPercent != 0f) stats.AddXpGainMultiplier(bonus.xpGainPercent);
            if (bonus.luckyDropChance != 0f) stats.AddLuckyDropChanceBonus(bonus.luckyDropChance);
            if (bonus.goldBonusPercent != 0f) stats.AddGoldBonusMultiplier(bonus.goldBonusPercent);
            if (bonus.xpMagnetPercent != 0f) stats.AddXpMagnetRadiusMultiplier(bonus.xpMagnetPercent);
            if (bonus.moveSpeedPercent != 0f) stats.AddMoveSpeedMultiplier(bonus.moveSpeedPercent);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new PetRunModifier());
    }
}
