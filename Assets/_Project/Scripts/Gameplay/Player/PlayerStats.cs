using UnityEngine;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Central, mutable stat sheet for the player. Weapons and skills read/modify
    /// this instead of touching each other directly — the mechanism that lets
    /// skill upgrades (e.g. "Attack Speed +20%") apply uniformly regardless of
    /// which weapon is equipped.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 5f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float MoveSpeed => baseMoveSpeed * MoveSpeedMultiplier;

        [Header("Combat")]
        public float AttackDamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;
        public float CritChanceBonus { get; private set; } = 0f;
        public float CritMultiplierBonus { get; private set; } = 0f;
        public int ExtraProjectiles { get; private set; } = 0;
        public float ProjectileSizeMultiplier { get; private set; } = 1f;

        [Header("Survival")]
        public float DodgeChance { get; private set; } = 0f;
        public float HealPerSecond { get; private set; } = 0f;
        /// <summary>Fraction of incoming hit damage ignored (0..0.8). Samurai hero, Shrine, talents.</summary>
        public float DamageReduction { get; private set; } = 0f;

        [Header("On-Hit Status Effects")]
        public float BurnOnHitDps { get; private set; } = 0f;
        public float BurnOnHitDuration { get; private set; } = 0f;
        public float PoisonOnHitDps { get; private set; } = 0f;
        public float PoisonOnHitDuration { get; private set; } = 0f;

        [Header("Utility")]
        public float XpMagnetRadiusMultiplier { get; private set; } = 1f;
        public float GoldBonusMultiplier { get; private set; } = 1f;
        public float LuckyDropChanceBonus { get; private set; } = 0f;
        public float XpGainMultiplier { get; private set; } = 1f;
        public float UltimateChargeMultiplier { get; private set; } = 1f;

        public const float MaxDamageReduction = 0.8f;

        public void AddMoveSpeedMultiplier(float delta) => MoveSpeedMultiplier += delta;
        public void AddAttackDamageMultiplier(float delta) => AttackDamageMultiplier += delta;
        public void AddAttackSpeedMultiplier(float delta) => AttackSpeedMultiplier += delta;
        public void AddCritChanceBonus(float delta) => CritChanceBonus += delta;
        public void AddCritMultiplierBonus(float delta) => CritMultiplierBonus += delta;
        public void AddExtraProjectiles(int delta) => ExtraProjectiles += delta;
        public void AddProjectileSizeMultiplier(float delta) => ProjectileSizeMultiplier += delta;
        public void AddDodgeChance(float delta) => DodgeChance = Mathf.Clamp01(DodgeChance + delta);
        public void AddHealPerSecond(float delta) => HealPerSecond += delta;
        public void AddDamageReduction(float delta) => DamageReduction = Mathf.Clamp(DamageReduction + delta, 0f, MaxDamageReduction);
        public void AddBurnOnHit(float dps, float duration)
        {
            BurnOnHitDps += dps;
            BurnOnHitDuration = Mathf.Max(BurnOnHitDuration, duration);
        }
        public void AddPoisonOnHit(float dps, float duration)
        {
            PoisonOnHitDps += dps;
            PoisonOnHitDuration = Mathf.Max(PoisonOnHitDuration, duration);
        }
        public void AddXpMagnetRadiusMultiplier(float delta) => XpMagnetRadiusMultiplier += delta;
        public void AddGoldBonusMultiplier(float delta) => GoldBonusMultiplier += delta;
        public void AddLuckyDropChanceBonus(float delta) => LuckyDropChanceBonus += delta;
        public void AddXpGainMultiplier(float delta) => XpGainMultiplier += delta;
        public void AddUltimateChargeMultiplier(float delta) => UltimateChargeMultiplier += delta;
    }
}
