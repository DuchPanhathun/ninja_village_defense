using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>Everything a concrete ultimate needs to execute.</summary>
    public readonly struct UltimateContext
    {
        /// <summary>For starting coroutines (multi-second ultimates like Heavenly Storm).</summary>
        public readonly MonoBehaviour Runner;
        public readonly Transform PlayerTransform;
        public readonly PlayerStats Stats;
        public readonly LayerMask EnemyMask;

        public UltimateContext(MonoBehaviour runner, Transform playerTransform, PlayerStats stats, LayerMask enemyMask)
        {
            Runner = runner;
            PlayerTransform = playerTransform;
            Stats = stats;
            EnemyMask = enemyMask;
        }
    }

    /// <summary>
    /// Base for every ninja's ultimate. Charge is built by kills (see
    /// <see cref="UltimateController"/>); once full and off cooldown the player can
    /// unleash it.
    /// </summary>
    public abstract class UltimateDefinition : DescriptiveScriptableObject
    {
        [Header("Ultimate")]
        [SerializeField] private float cooldownSeconds = 90f;

        public float CooldownSeconds => cooldownSeconds;

        public abstract void Activate(in UltimateContext context);
    }
}
