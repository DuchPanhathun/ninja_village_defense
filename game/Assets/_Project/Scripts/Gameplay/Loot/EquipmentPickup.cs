using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Inventory;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// A world-space equipment drop. Flies toward the player when in magnet range; on contact it
    /// applies its EquipmentDefinition's stat bonuses for the rest of the run AND adds the piece to the
    /// persistent inventory (EPIC 16 "Inventory save") so it can be equipped or used as Forge material
    /// in later runs. Works without a prefab: <see cref="Create"/> builds a rarity-colored placeholder.
    /// </summary>
    public class EquipmentPickup : MonoBehaviour
    {
        [SerializeField] private float baseMagnetRadius = 2.5f;
        [SerializeField] private float pickupDistance = 0.35f;
        [SerializeField] private float flySpeed = 8f;

        private EquipmentDefinition _definition;
        private PlayerStats _playerStats;
        private Transform _visual;
        private float _spin;

        public void Initialize(EquipmentDefinition definition)
        {
            _definition = definition;
            EnsureVisual();
            NinjaVillage.Gameplay.World.MinimapMarker.Add(gameObject, NinjaVillage.Gameplay.World.MinimapMarkerKind.Item);
        }

        /// <summary>Spawns a prefab-less pickup for <paramref name="definition"/>.</summary>
        public static EquipmentPickup Create(EquipmentDefinition definition, Vector2 position)
        {
            var go = new GameObject($"EquipmentPickup_{(definition != null ? definition.Id : "none")}");
            go.transform.position = position;
            var pickup = go.AddComponent<EquipmentPickup>();
            pickup.Initialize(definition);
            return pickup;
        }

        private void EnsureVisual()
        {
            if (_visual != null || _definition == null) return;
            if (TryGetComponent<SpriteRenderer>(out var existing) && existing.sprite != null) return;

            Color color = RarityColors.For(_definition.Rarity);
            var glow = GeneratedSprites.CreateRenderer(transform, "Glow", GeneratedSprites.Glow, new Color(color.r, color.g, color.b, 0.6f), 40, Vector2.zero, new Vector2(1.1f, 1.1f));
            _visual = GeneratedSprites.CreateRenderer(transform, "Gem", GeneratedSprites.Square, color, 41, Vector2.zero, new Vector2(0.35f, 0.35f)).transform;
            _visual.localRotation = Quaternion.Euler(0f, 0f, 45f);
            glow.transform.SetAsFirstSibling();
        }

        private void Start()
        {
            if (PlayerReference.Instance != null)
                _playerStats = PlayerReference.Instance.GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (_visual != null)
            {
                _spin += Time.deltaTime * 90f;
                _visual.localRotation = Quaternion.Euler(0f, 0f, 45f + _spin);
            }

            if (PlayerReference.Instance == null || _definition == null) return;

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
            if (_playerStats != null)
                _definition.Apply(_playerStats);
            InventoryService.AddEquipment(_definition.Id);
            Sfx.PlayAt(AudioCueIds.EquipmentPickup, transform.position);
            Destroy(gameObject);
        }
    }
}
