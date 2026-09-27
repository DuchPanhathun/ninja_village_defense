using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// A chest (EPIC 10 "Chest rewards") that grants a bundle of coins + optional equipment (and sometimes a copy of
    /// one of your weapons, for merging) when the
    /// player touches it. Dropped by bosses via <see cref="LootSpawner"/>; works without a prefab
    /// (<see cref="Create"/>) and falls back to the EquipmentCatalog when no equipment list is set.
    /// </summary>
    public class ChestPickup : MonoBehaviour
    {
        [Header("Rewards")]
        [SerializeField] private int minCoins = 15;
        [SerializeField] private int maxCoins = 50;
        [SerializeField] private EquipmentDefinition[] possibleEquipment;
        [Tooltip("Chance (0-1) to drop one piece of equipment from possibleEquipment.")]
        [SerializeField] private float equipmentDropChance = 0.6f;
        [SerializeField] private GameObject equipmentPickupPrefab;

        private bool _opened;

        /// <summary>Builds a placeholder chest with the given reward range at <paramref name="position"/>.</summary>
        public static ChestPickup Create(Vector2 position, int minCoins, int maxCoins, float equipmentChance)
        {
            var go = new GameObject("Chest");
            go.transform.position = position;
            GeneratedSprites.CreateRenderer(go.transform, "Glow", GeneratedSprites.Glow, new Color(1f, 0.85f, 0.3f, 0.5f), 38, Vector2.zero, new Vector2(2f, 2f));
            var art = Core.Data.CatalogLoader.Load<PickupArt>();
            if (art != null && art.ChestClosed != null)
            {
                var chestRenderer = new GameObject("Chest").AddComponent<SpriteRenderer>();
                chestRenderer.transform.SetParent(go.transform, false);
                chestRenderer.sprite = art.ChestClosed;
                chestRenderer.sortingOrder = 39;
            }
            else
            {
                GeneratedSprites.CreateRenderer(go.transform, "Box", GeneratedSprites.Square, new Color(0.6f, 0.38f, 0.18f), 39, Vector2.zero, new Vector2(0.9f, 0.6f));
                GeneratedSprites.CreateRenderer(go.transform, "Lid", GeneratedSprites.Square, new Color(1f, 0.8f, 0.25f), 40, new Vector2(0f, 0.2f), new Vector2(0.95f, 0.2f));
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1f, 0.8f);

            NinjaVillage.Gameplay.World.MinimapMarker.Add(go, NinjaVillage.Gameplay.World.MinimapMarkerKind.Chest);
            var chest = go.AddComponent<ChestPickup>();
            chest.minCoins = minCoins;
            chest.maxCoins = maxCoins;
            chest.equipmentDropChance = equipmentChance;
            return chest;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_opened) return;
            // Identify the player by component: the Battle scene's player isn't tagged "Player", which is
            // why chests could never be opened.
            var stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null && !other.CompareTag("Player")) return;

            Open(stats);
        }

        private void Open(PlayerStats playerStats)
        {
            _opened = true;

            // Coins
            int coins = Random.Range(minCoins, maxCoins + 1);
            if (playerStats != null)
                coins = Mathf.Max(1, Mathf.RoundToInt(coins * playerStats.GoldBonusMultiplier));
            CurrencyService.Grant(CurrencyType.Coins, coins, "chest");

            // Equipment
            if (Random.value <= equipmentDropChance)
            {
                var chosen = PickEquipment();
                if (chosen != null)
                {
                    Vector2 position = transform.position + (Vector3)(Random.insideUnitCircle * 0.5f);
                    if (equipmentPickupPrefab != null)
                    {
                        var go = Instantiate(equipmentPickupPrefab, position, Quaternion.identity);
                        if (go.TryGetComponent<EquipmentPickup>(out var pickup)) pickup.Initialize(chosen);
                    }
                    else
                    {
                        EquipmentPickup.Create(chosen, position);
                    }
                }
            }

            // Sometimes a copy of one of your weapons (usually the one in your hands), for merging.
            if (Random.value <= WeaponCopyChance) GrantWeaponCopy();

            Sfx.PlayAt(AudioCueIds.ChestOpen, transform.position);
            NinjaVillage.Gameplay.Vfx.Vfx.Burst(transform.position, NinjaVillage.Gameplay.Vfx.Vfx.GoldColor, 2.5f, 0.45f);
            Progress.Report(ProgressStatIds.ChestOpened);
            Destroy(gameObject);
        }

        /// <summary>Chance a chest also holds a copy of one of your weapons (merge 3 → a better grade).</summary>
        public const float WeaponCopyChance = 0.2f;

        private void GrantWeaponCopy()
        {
            var inv = NinjaVillage.Systems.Inventory.InventoryService.Data;
            string weaponId = inv.EquippedWeaponId;
            if (Random.value > 0.6f && inv.Weapons.Count > 0) weaponId = inv.Weapons[Random.Range(0, inv.Weapons.Count)].Id;
            var weapon = NinjaVillage.Systems.Inventory.InventoryService.GetWeapon(weaponId);
            if (weapon == null || weapon.IsSpecial) return;
            NinjaVillage.Systems.Inventory.InventoryService.AddWeaponCopy(weapon.Id);
            NinjaVillage.Gameplay.Village.FloatingText.Spawn(transform.position + Vector3.up * 1.2f,
                $"+1 {(string.IsNullOrEmpty(weapon.DisplayName) ? weapon.Id : weapon.DisplayName)} copy", new Color(1f, 0.85f, 0.35f));
        }

        private EquipmentDefinition PickEquipment()
        {
            if (possibleEquipment != null && possibleEquipment.Length > 0)
                return possibleEquipment[Random.Range(0, possibleEquipment.Length)];

            var catalog = CatalogCache<EquipmentCatalog>.Get();
            if (catalog == null || catalog.All.Count == 0) return null;
            var regular = new System.Collections.Generic.List<EquipmentDefinition>();
            foreach (var def in catalog.All)
                if (def != null && !def.IsSpecial) regular.Add(def); // S-class only comes from Surprise Boxes
            return regular.Count > 0 ? regular[Random.Range(0, regular.Count)] : null;
        }
    }
}
