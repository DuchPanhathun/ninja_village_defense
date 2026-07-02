using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Gameplay.Enemies
{
    /// <summary>
    /// Data definition for an enemy type (Bandit, Slime, Wolf, ... up to bosses).
    /// A boss is just an EnemyDefinition with <see cref="isBoss"/> set and a
    /// dedicated behavior script layered on top of <see cref="EnemyController"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Ninja Village/Enemy Definition")]
    public class EnemyDefinition : DescriptiveScriptableObject
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Rewards")]
        [SerializeField] private int xpReward = 5;
        [SerializeField] private int coinReward = 1;

        [Header("Classification")]
        [SerializeField] private bool isBoss = false;
        [SerializeField] private bool isElite = false;

        [Header("Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        public GameObject EnemyPrefab => enemyPrefab;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int XpReward => xpReward;
        public int CoinReward => coinReward;
        public bool IsBoss => isBoss;
        public bool IsElite => isElite;
    }
}
