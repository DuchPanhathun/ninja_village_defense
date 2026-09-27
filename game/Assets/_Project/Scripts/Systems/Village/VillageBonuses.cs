using NinjaVillage.Systems.Meta;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// The village's combined effect on a run (Dojo attack bonus + Shrine blessings), gathered first
    /// and applied in one place. Separating "sum the bonuses" from "push them onto PlayerStats" keeps
    /// the stat math testable and lets UI show the exact totals a run will get.
    /// All values are additive deltas (0.1 = +10%).
    /// </summary>
    public struct VillageBonuses
    {
        public float AttackDamage;
        public float MaxHealth;
        public float CritChance;
        public float Luck;
        public float CoinGain;
        public float XpGain;
        public float DamageReduction;

        public void AddBlessing(BlessingStat stat, float value)
        {
            switch (stat)
            {
                case BlessingStat.MaxHealth: MaxHealth += value; break;
                case BlessingStat.CritChance: CritChance += value; break;
                case BlessingStat.Luck: Luck += value; break;
                case BlessingStat.CoinGain: CoinGain += value; break;
                case BlessingStat.XpGain: XpGain += value; break;
                case BlessingStat.DamageReduction: DamageReduction += value; break;
            }
        }

        /// <summary>Pushes every non-zero bonus onto the run's PlayerStats / max-health math.</summary>
        public void ApplyTo(RunStartContext context)
        {
            if (context == null) return;

            context.MaxHealthMultiplier += MaxHealth;

            var stats = context.Stats;
            if (stats == null) return;

            if (AttackDamage != 0f) stats.AddAttackDamageMultiplier(AttackDamage);
            if (CritChance != 0f) stats.AddCritChanceBonus(CritChance);
            if (Luck != 0f) stats.AddLuckyDropChanceBonus(Luck);
            if (CoinGain != 0f) stats.AddGoldBonusMultiplier(CoinGain);
            if (XpGain != 0f) stats.AddXpGainMultiplier(XpGain);
            if (DamageReduction != 0f) stats.AddDamageReduction(DamageReduction);
        }
    }
}
