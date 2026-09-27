using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Core.Utilities;
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

        [Header("Look by value (optional)")]
        [Tooltip("Gem sprites from small to large; the orb shows the first whose threshold its XP doesn't exceed.")]
        [SerializeField] private Sprite[] tierSprites = System.Array.Empty<Sprite>();
        [SerializeField] private int[] tierMaxXp = { 5, 15 };

        private int _xpAmount;
        private PlayerStats _playerStats;

        public void Initialize(int xpAmount)
        {
            _xpAmount = xpAmount;
            if (tierSprites.Length == 0 || !TryGetComponent<SpriteRenderer>(out var spriteRenderer)) return;
            int tier = 0;
            while (tier < tierMaxXp.Length && tier < tierSprites.Length - 1 && xpAmount > tierMaxXp[tier]) tier++;
            if (tierSprites[tier] != null) spriteRenderer.sprite = tierSprites[tier];
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
            PrefabPool.Release(gameObject);
        }
    }
}
