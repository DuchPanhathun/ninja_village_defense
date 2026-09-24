using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// The Armory beside the Forge: a weapon rack with the equipped weapon floating above it on a glow and
    /// the equipped gear in a row beneath — what this ninja fights with, on show. Tap to open the Gear screen.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ArmoryDisplay : MonoBehaviour, IVillageTappable
    {
        private const float WeaponSize = 1.05f;
        private const float GearSize = 0.62f;

        private readonly List<SpriteRenderer> _gear = new();
        private SpriteRenderer _rack, _weapon, _glow;
        private TextMeshPro _label;
        private float _phase;

        public void Show(VillageArt art, VillageSnapshot snapshot)
        {
            if (_rack == null) Build(art);

            var weapon = InventoryService.GetWeapon(snapshot.WeaponId);
            _weapon.sprite = weapon != null ? weapon.Icon : null;
            Fit(_weapon, WeaponSize);
            _weapon.enabled = _glow.enabled = _weapon.sprite != null;

            foreach (var renderer in _gear) Destroy(renderer.gameObject);
            _gear.Clear();
            var icons = new List<Sprite>();
            foreach (var id in snapshot.EquipmentIds)
            {
                var item = InventoryService.GetEquipment(id);
                if (item != null && item.Icon != null) icons.Add(item.Icon);
            }
            for (int i = 0; i < icons.Count; i++)
            {
                var renderer = GeneratedSprites.CreateRenderer(transform, "Gear", icons[i], Color.white, 0);
                renderer.transform.localPosition = new Vector3((i - (icons.Count - 1) * 0.5f) * 0.72f, 0.95f, 0f);
                Fit(renderer, GearSize);
                _gear.Add(renderer);
            }

            _label.text = weapon != null ? $"Armory\n<size=70%><color=#FFD24D>{weapon.DisplayName}</color></size>" : "Armory";
            ApplySorting();
        }

        private void Build(VillageArt art)
        {
            _rack = GeneratedSprites.CreateRenderer(transform, "Rack", art != null && art.WeaponRack != null ? art.WeaponRack : GeneratedSprites.Square,
                art != null && art.WeaponRack != null ? Color.white : new Color(0.45f, 0.3f, 0.18f), 0);
            if (_rack.sprite == GeneratedSprites.Square) _rack.transform.localScale = new Vector3(2.2f, 0.9f, 1f);
            _glow = GeneratedSprites.CreateRenderer(transform, "Glow", GeneratedSprites.Glow, new Color(1f, 0.85f, 0.4f, 0.55f), 0,
                new Vector2(0f, 2.05f), new Vector2(1.8f, 1.8f));
            _weapon = GeneratedSprites.CreateRenderer(transform, "Weapon", null, Color.white, 0, new Vector2(0f, 2.05f));

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.transform.localPosition = new Vector3(0f, 3.25f, 0f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.6f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
            _label.sortingOrder = VillageSorting.Labels;

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(2.4f, 3f);
            box.offset = new Vector2(0f, 1.2f);
        }

        private void ApplySorting()
        {
            int order = VillageSorting.Order(VillageSorting.Feet(transform.position, _rack.sprite));
            _rack.sortingOrder = order;
            _glow.sortingOrder = order + 1;
            _weapon.sortingOrder = order + 2;
            foreach (var gear in _gear) gear.sortingOrder = order + 3;
        }

        private static void Fit(SpriteRenderer renderer, float size)
        {
            if (renderer.sprite == null) return;
            Vector2 native = renderer.sprite.bounds.size;
            renderer.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, Mathf.Max(native.x, native.y)));
        }

        private void Update()
        {
            if (_weapon == null) return;
            _phase += Time.deltaTime;
            _weapon.transform.localPosition = new Vector3(0f, 2.05f + Mathf.Sin(_phase * 2f) * 0.12f, 0f);
            _glow.transform.localScale = Vector3.one * (1.8f + Mathf.Sin(_phase * 3f) * 0.12f);
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<VillageDisplayTappedEvent>.Raise(new VillageDisplayTappedEvent(VillageDisplayKind.Gear));
        }
    }
}
