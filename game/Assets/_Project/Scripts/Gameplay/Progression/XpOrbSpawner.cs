using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using UnityEngine;

namespace NinjaVillage.Gameplay.Progression
{
    /// <summary>Listens for enemy deaths and drops an XP orb at the death position — bridges Combat and Progression without either referencing the other.</summary>
    public class XpOrbSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject xpOrbPrefab;

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (xpOrbPrefab == null || evt.XpReward <= 0) return;

            var instance = PrefabPool.Get(xpOrbPrefab, evt.Position, Quaternion.identity);
            instance.GetComponent<XpOrb>().Initialize(evt.XpReward);
        }
    }
}
