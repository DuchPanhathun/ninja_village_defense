using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>Recipe satisfaction as a pure function (EditMode-tested; the manager feeds it live state).</summary>
    public static class EvolutionRules
    {
        /// <param name="requiredSkillIds">Skills that must be owned at level ≥ 1 (null entries ignored).</param>
        /// <param name="skillLevel">Current level of a skill id (0 = not owned).</param>
        /// <param name="requiredWeaponId">Null/empty = any weapon.</param>
        public static bool IsSatisfied(IEnumerable<string> requiredSkillIds, Func<string, int> skillLevel,
            string requiredWeaponId, string equippedWeaponId)
        {
            if (requiredSkillIds != null)
            {
                foreach (var id in requiredSkillIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (skillLevel == null || skillLevel(id) < 1) return false;
                }
            }
            return string.IsNullOrEmpty(requiredWeaponId) || requiredWeaponId == equippedWeaponId;
        }
    }
}
