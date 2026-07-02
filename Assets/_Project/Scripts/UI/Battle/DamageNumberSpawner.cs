using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>Spawns a floating damage number wherever anything takes a direct hit.</summary>
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private DamageNumber damageNumberPrefab;
        [SerializeField] private Vector2 randomOffset = new(0.3f, 0.3f);

        private void OnEnable() => EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        private void OnDisable() => EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);

        private void OnEntityDamaged(EntityDamagedEvent evt)
        {
            if (damageNumberPrefab == null) return;

            Vector2 position = evt.Position + new Vector2(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(0f, randomOffset.y));

            var instance = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            instance.Show(evt.Amount, evt.IsCritical);
        }
    }
}
