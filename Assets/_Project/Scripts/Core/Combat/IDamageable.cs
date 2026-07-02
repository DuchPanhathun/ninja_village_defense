using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>Anything that can receive a <see cref="DamageInfo"/> — player, enemy, boss, breakable prop.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        Transform Transform { get; }
        void TakeDamage(DamageInfo damage);
    }
}
