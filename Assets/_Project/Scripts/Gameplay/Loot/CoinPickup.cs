using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Core.Utilities;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// A coin drop. Same magnet-then-fly behavior as the XP orb; grants coins
    /// through the EconomyManager on contact.
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField] private float baseMagnetRadius = 2f;
        [SerializeField] private float pickupDistance = 0.3f;
        [SerializeField] private float flySpeed = 9f;

        private int _amount;
        private PlayerStats _playerStats;

        public void Initialize(int amount) => _amount = amount;

        private void Start()
        {
            if (PlayerReference.Instance != null)
                _playerStats = PlayerReference.Instance.GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (PlayerReference.Instance == null) return;

            Transform player = PlayerReference.Instance.PlayerTransform;
            float magnetRadius = baseMagnetRadius * (_playerStats != null ? _playerStats.XpMagnetRadiusMultiplier : 1f);
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= pickupDistance)
            {
                Collect();
                return;
            }

            if (distance <= magnetRadius)
            {
                Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(direction * flySpeed * Time.deltaTime);
            }
        }

        private void Collect()
        {
            // Through CurrencyService so quests/achievements/analytics count coins earned.
            CurrencyService.Grant(CurrencyType.Coins, _amount, "coin_pickup");
            PrefabPool.Release(gameObject);
        }
    }
}
