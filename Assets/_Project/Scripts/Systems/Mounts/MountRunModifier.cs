using NinjaVillage.Gameplay.Mounts;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;
using UnityEngine;

namespace NinjaVillage.Systems.Mounts
{
    /// <summary>
    /// Rides the active mount into battle (<see cref="RunModifierOrder.Mounts"/>): applies its bonuses at its level
    /// (like talent stats) and seats the player on it (<see cref="MountVisual"/>).
    /// </summary>
    public sealed class MountRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Mounts;

        public void Apply(RunStartContext context)
        {
            var save = context.Save ?? SaveService.Data;
            var mount = MountService.Get(save.Mounts.ActiveMountId);
            if (mount == null || !save.Mounts.Owned.ContainsId(mount.Id)) return;

            float scale = MountRules.LevelScale(save.Mounts.Owned.GetLevel(mount.Id));
            foreach (var bonus in mount.Bonuses)
                TalentRunModifier.ApplyStat(context, bonus.Stat, bonus.Value * scale);

            if (context.Player == null) return;
            var visual = context.Player.GetComponent<MountVisual>();
            if (visual == null) visual = context.Player.AddComponent<MountVisual>();
            visual.Ride(mount);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new MountRunModifier());
    }
}
