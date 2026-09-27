using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Meta
{
    /// <summary>
    /// Put on the Player in Battle.unity. In Start (after every Awake, so the scene's default
    /// weapon is already equipped) it runs all registered <see cref="IRunStartModifier"/>s and
    /// applies their combined result — this is where permanent progression meets the run.
    /// </summary>
    [RequireComponent(typeof(PlayerStats), typeof(Health))]
    [DefaultExecutionOrder(-50)]
    public class RunBootstrapper : MonoBehaviour
    {
        [Tooltip("Where to spawn the pet relative to the player.")]
        [SerializeField] private Vector2 petSpawnOffset = new(-1f, 0.5f);

        public RunStartContext Context { get; private set; }

        private void Start()
        {
            var context = new RunStartContext
            {
                Save = SaveService.Data,
                Player = gameObject,
                Stats = GetComponent<PlayerStats>(),
                Health = GetComponent<Health>(),
                AutoAttack = GetComponent<AutoAttackController>(),
                Ultimate = GetComponent<UltimateController>(),
                Skills = GetComponent<SkillManager>(),
            };
            context.BaseMaxHealth = context.Health.MaxHealth;
            if (context.AutoAttack != null && context.AutoAttack.Weapon != null)
            {
                context.Weapon = context.AutoAttack.Weapon.Definition;
                context.WeaponLevel = context.AutoAttack.Weapon.Level;
            }

            RunStartModifiers.ApplyAll(context);
            ApplyResult(context);
            Context = context;

            string weaponId = context.Weapon != null ? context.Weapon.Id : null;
            EventBus<RunStartedEvent>.Raise(new RunStartedEvent(context.HeroId, weaponId));
        }

        private void ApplyResult(RunStartContext context)
        {
            if (context.AutoAttack != null && context.Weapon != null)
                context.AutoAttack.EquipWeapon(context.Weapon, Mathf.Max(1, context.WeaponLevel));

            context.Health.ResetHealth(context.FinalMaxHealth);
            if (context.StartingShield > 0f)
                context.Health.GainShield(context.StartingShield, context.StartingShield);

            if (context.Ultimate != null && context.UltimateOverride != null)
                context.Ultimate.Equip(context.UltimateOverride);

            if (context.Skills != null)
            {
                foreach (var skill in context.StartingSkills)
                    if (skill != null) context.Skills.SelectSkill(skill);
            }

            if (context.PetPrefab != null)
            {
                var pet = Instantiate(context.PetPrefab, (Vector2)transform.position + petSpawnOffset, Quaternion.identity);
                context.OnPetSpawned?.Invoke(pet);
            }
        }
    }
}
