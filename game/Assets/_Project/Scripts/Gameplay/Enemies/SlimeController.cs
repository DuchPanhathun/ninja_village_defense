using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Enemies
{
    /// <summary>
    /// Slime's signature twist: splits into smaller slimes on death. The mini-slime
    /// prefab should reuse this same script with <see cref="canSplit"/> unchecked so
    /// it doesn't split again, and reuses the parent's own EnemyDefinition (just
    /// scaled down), so no extra data asset is needed beyond a smaller prefab.
    /// </summary>
    public class SlimeController : EnemyController
    {
        [Header("Split On Death")]
        [SerializeField] private GameObject miniSlimePrefab;
        [SerializeField] private int splitCount = 2;
        [SerializeField] private float childStatMultiplier = 0.4f;
        [SerializeField] private bool canSplit = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            HealthComponent.OnDeath += HandleSplitOnDeath;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HealthComponent.OnDeath -= HandleSplitOnDeath;
        }

        private void HandleSplitOnDeath(Health health)
        {
            if (!canSplit || miniSlimePrefab == null || Definition == null) return;

            for (int i = 0; i < splitCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                var instance = Instantiate(miniSlimePrefab, (Vector2)transform.position + offset, Quaternion.identity);
                instance.GetComponent<EnemyController>().Initialize(Definition, childStatMultiplier);
            }
        }
    }
}
