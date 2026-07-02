using System;
using System.Collections.Generic;
using NinjaVillage.Gameplay.Enemies;
using UnityEngine;

namespace NinjaVillage.Gameplay.Waves
{
    [Serializable]
    public struct SpawnEntry
    {
        public EnemyDefinition EnemyDefinition;
        public int Count;
    }

    /// <summary>Data definition for a single wave — what spawns, how fast, and whether it ends in a boss.</summary>
    [CreateAssetMenu(fileName = "NewWave", menuName = "Ninja Village/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        [SerializeField] private List<SpawnEntry> spawns = new();
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private bool isBossWave;
        [SerializeField] private EnemyDefinition bossDefinition;

        public IReadOnlyList<SpawnEntry> Spawns => spawns;
        public float SpawnInterval => spawnInterval;
        public bool IsBossWave => isBossWave;
        public EnemyDefinition BossDefinition => bossDefinition;
    }
}
