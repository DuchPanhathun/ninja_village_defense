using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Base for a pet's active behaviour (Fox pulls XP, Wolf bites...). <see cref="PetFactory"/>
    /// adds the right subclass for <see cref="PetDefinition.AbilityType"/> (or keeps one already on
    /// a custom prefab) and calls <see cref="Initialize"/> with the level-scaled stats — so an
    /// ability never reads save data or definitions' growth curves itself.
    /// </summary>
    [RequireComponent(typeof(PetController))]
    public abstract class PetAbility : MonoBehaviour
    {
        protected PetController Controller { get; private set; }
        protected PetDefinition Definition { get; private set; }
        protected PetRuntimeStats Stats { get; private set; }
        protected PlayerStats OwnerStats { get; private set; }
        protected LayerMask EnemyMask { get; private set; }
        protected bool IsReady { get; private set; }

        /// <summary>Owner transform (the player); null if the player is gone.</summary>
        protected Transform Owner => Controller != null ? Controller.Owner : null;

        public virtual void Initialize(PetDefinition definition, PetRuntimeStats stats, PlayerStats ownerStats, LayerMask enemyMask)
        {
            Controller = GetComponent<PetController>();
            Definition = definition;
            Stats = stats;
            OwnerStats = ownerStats;
            EnemyMask = PetCombat.ResolveEnemyMask(enemyMask);
            IsReady = true;
            OnInitialized();
        }

        /// <summary>Hook for subclasses to reset timers once stats are known.</summary>
        protected virtual void OnInitialized() { }

        /// <summary>Pet damage including the share of the owner's bonus damage it inherits.</summary>
        protected float ScaledDamage(float baseDamage)
        {
            float ownerMultiplier = OwnerStats != null ? OwnerStats.AttackDamageMultiplier : 1f;
            float inheritance = Definition != null ? Definition.OwnerDamageInheritance : 0.5f;
            return PetStatMath.InheritOwnerDamage(baseDamage, ownerMultiplier, inheritance);
        }
    }
}
