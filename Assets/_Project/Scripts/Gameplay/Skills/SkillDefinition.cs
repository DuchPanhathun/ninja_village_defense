using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Base for every level-up upgrade. Concrete skills override <see cref="ApplyLevel"/>
    /// to push their *incremental* effect for the level just reached onto
    /// <see cref="PlayerStats"/> — called once per pick/upgrade, never cumulatively
    /// recomputed, so it composes naturally with everything else touching PlayerStats.
    /// </summary>
    public abstract class SkillDefinition : DescriptiveScriptableObject
    {
        [Header("Classification")]
        [SerializeField] private SkillCategory category = SkillCategory.Offensive;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private Element element = Element.None;
        [Tooltip("Evolution results are granted by an EvolutionRecipe, never offered in random level-up rolls.")]
        [SerializeField] private bool isEvolution;

        public SkillCategory Category => category;
        public Rarity Rarity => rarity;
        public int MaxLevel => maxLevel;
        public Element Element => element;
        public bool IsEvolution => isEvolution;

        /// <summary>Applies this skill's effect for having just reached <paramref name="newLevel"/>.</summary>
        public abstract void ApplyLevel(PlayerStats stats, int newLevel);
    }
}
