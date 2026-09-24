using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Talents
{
    /// <summary>Applies every learned talent rank to the player at run start (<see cref="RunModifierOrder.Talents"/>).</summary>
    public sealed class TalentRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Talents;

        public void Apply(RunStartContext context)
        {
            var catalog = TalentService.Catalog;
            if (catalog == null) return;
            var save = context.Save ?? SaveService.Data;

            foreach (var talent in catalog.All)
            {
                if (talent == null) continue;
                int rank = save.Talents.Nodes.GetLevel(talent.Id);
                if (rank > 0) ApplyStat(context, talent.Stat, talent.ValueAt(rank));
            }
        }

        public static void ApplyStat(RunStartContext context, TalentStat stat, float value)
        {
            var s = context.Stats;
            switch (stat)
            {
                case TalentStat.MaxHealth: context.MaxHealthMultiplier += value; return;
                case TalentStat.StartingShield: context.StartingShield += value; return;
            }
            if (s == null) return;
            switch (stat)
            {
                case TalentStat.AttackDamage: s.AddAttackDamageMultiplier(value); break;
                case TalentStat.AttackSpeed: s.AddAttackSpeedMultiplier(value); break;
                case TalentStat.CritChance: s.AddCritChanceBonus(value); break;
                case TalentStat.CritDamage: s.AddCritMultiplierBonus(value); break;
                case TalentStat.DamageReduction: s.AddDamageReduction(value); break;
                case TalentStat.HealPerSecond: s.AddHealPerSecond(value); break;
                case TalentStat.DodgeChance: s.AddDodgeChance(value); break;
                case TalentStat.MoveSpeed: s.AddMoveSpeedMultiplier(value); break;
                case TalentStat.XpGain: s.AddXpGainMultiplier(value); break;
                case TalentStat.GoldGain: s.AddGoldBonusMultiplier(value); break;
                case TalentStat.PickupRadius: s.AddXpMagnetRadiusMultiplier(value); break;
                case TalentStat.LuckyDrop: s.AddLuckyDropChanceBonus(value); break;
                case TalentStat.UltimateCharge: s.AddUltimateChargeMultiplier(value); break;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new TalentRunModifier());
    }
}
