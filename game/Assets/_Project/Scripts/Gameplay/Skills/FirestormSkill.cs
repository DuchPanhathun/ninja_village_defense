using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Evolution: Fire + Wind = <b>Firestorm</b>. A roaring ring of fire bursts around you every few
    /// seconds, damaging and setting every nearby demon ablaze.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_Firestorm", menuName = "Ninja Village/Skills/Evolutions/Firestorm")]
    public class FirestormSkill : SkillDefinition
    {
        [SerializeField] private float damage = 35f;
        [SerializeField] private float interval = 2f;
        [SerializeField] private float radius = 3.8f;
        [SerializeField] private float burnDps = 12f;
        [SerializeField] private float burnDuration = 3f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            if (!stats.TryGetComponent<FirestormBehavior>(out var behavior))
                behavior = stats.gameObject.AddComponent<FirestormBehavior>();
            behavior.Configure(damage * newLevel, interval, radius, burnDps, burnDuration);
        }
    }
}
