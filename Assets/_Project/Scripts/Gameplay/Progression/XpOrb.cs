using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Progression
{
    /// <summary>
    /// A pickup dropped by dead enemies. Sits still until the player enters the
    /// magnet radius (boosted by the "XP Magnet" utility skill via PlayerStats),
    /// then flies toward them and grants XP on contact.
    /// </summary>
    public class XpOrb : MonoBehaviour
    {
        [SerializeField] private float baseMagnetRadius = 2.5f;
        [SerializeField] private float pickupDistance = 0.3f;
        [SerializeField] private float flySpeed = 8f;

        private int _xpAmount;
        private PlayerStats _playerStats;

        public void Initialize(int xpAmount)
        {
            _xpAmount = xpAmount;
        }

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
            EventBus<XpGainedEvent>.Raise(new XpGainedEvent(_xpAmount));
            Destroy(gameObject);
        }
    }
}
