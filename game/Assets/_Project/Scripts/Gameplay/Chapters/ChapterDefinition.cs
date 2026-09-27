using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Enemies;
using NinjaVillage.Gameplay.Waves;
using UnityEngine;

namespace NinjaVillage.Gameplay.Chapters
{
    /// <summary>
    /// One battle "chapter": a finite wave plan that ends with a final boss, its own map (ground,
    /// scattered props, sky colour) and enemy roster, and a first-clear reward. Clearing chapter N
    /// unlocks chapter N+1. <see cref="DescriptiveScriptableObject.Icon"/> is the final boss's sprite.
    /// </summary>
    [CreateAssetMenu(fileName = "Chapter", menuName = "Ninja Village/Chapter")]
    public class ChapterDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private int number = 1;
        [SerializeField] private WaveDefinition[] waves = System.Array.Empty<WaveDefinition>();
        [Tooltip("Multiplies enemy health and damage on top of the per-wave ramp.")]
        [SerializeField] private float difficulty = 1f;

        [Header("Map")]
        [SerializeField] private Sprite ground;
        [SerializeField] private Color groundTint = Color.white;
        [SerializeField] private Color skyColor = new(0.29f, 0.45f, 0.2f);
        [SerializeField] private Sprite[] props = System.Array.Empty<Sprite>();
        [Tooltip("Chance that a scatter cell holds a prop (0..1).")]
        [SerializeField, Range(0f, 1f)] private float propDensity = 0.35f;
        [Tooltip("Colour of the chapter on maps and its UI accents.")]
        [SerializeField] private Color themeColor = new(0.55f, 0.75f, 0.35f);

        [Header("First clear reward")]
        [SerializeField] private int clearCoins = 500;
        [SerializeField] private int clearGems = 20;

        public int Number => number;
        public IReadOnlyList<WaveDefinition> Waves => waves;
        public int WaveCount => waves.Length;
        public float Difficulty => difficulty;
        public Sprite Ground => ground;
        public Color GroundTint => groundTint;
        public Color SkyColor => skyColor;
        public IReadOnlyList<Sprite> Props => props;
        public float PropDensity => propDensity;
        public Color ThemeColor => themeColor;
        public int ClearCoins => clearCoins;
        public int ClearGems => clearGems;

        /// <summary>The boss of the last boss wave (the chapter's final boss), or null.</summary>
        public EnemyDefinition FinalBoss
        {
            get
            {
                for (int i = waves.Length - 1; i >= 0; i--)
                    if (waves[i] != null && waves[i].IsBossWave && waves[i].BossDefinition != null)
                        return waves[i].BossDefinition;
                return null;
            }
        }

        /// <summary>Distinct non-boss enemies across the waves, in first-appearance order.</summary>
        public List<EnemyDefinition> Enemies()
        {
            var list = new List<EnemyDefinition>();
            foreach (var wave in waves)
            {
                if (wave == null) continue;
                foreach (var entry in wave.Spawns)
                    if (entry.EnemyDefinition != null && !list.Contains(entry.EnemyDefinition))
                        list.Add(entry.EnemyDefinition);
            }
            return list;
        }

        /// <summary>Distinct bosses in the order they appear (mid-bosses first, the final boss last).</summary>
        public List<EnemyDefinition> Bosses()
        {
            var list = new List<EnemyDefinition>();
            foreach (var wave in waves)
                if (wave != null && wave.IsBossWave && wave.BossDefinition != null && !list.Contains(wave.BossDefinition))
                    list.Add(wave.BossDefinition);
            return list;
        }
    }
}
