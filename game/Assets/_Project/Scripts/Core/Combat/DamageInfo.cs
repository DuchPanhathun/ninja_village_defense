using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>
    /// A fully-resolved damage packet. The attacker computes crit/final amount
    /// *before* creating this — <see cref="Health"/> just applies it.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly bool IsCritical;
        public readonly Vector2 KnockbackDirection;
        public readonly float KnockbackForce;
        public readonly GameObject Source;

        public DamageInfo(float amount, bool isCritical, Vector2 knockbackDirection, float knockbackForce, GameObject source)
        {
            Amount = amount;
            IsCritical = isCritical;
            KnockbackDirection = knockbackDirection.normalized;
            KnockbackForce = knockbackForce;
            Source = source;
        }
    }
}
