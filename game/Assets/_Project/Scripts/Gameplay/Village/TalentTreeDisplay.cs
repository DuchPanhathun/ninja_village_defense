using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// The Talent Tree beside the Shrine: a cherry tree that grows with every talent rank learned and
    /// carries a glowing blossom per few ranks, so talent progress is visible from across the village.
    /// Tap to open the Talents screen.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TalentTreeDisplay : MonoBehaviour, IVillageTappable
    {
        private const int RanksForFullSize = 30;
        private const int MaxBlossoms = 14;

        private readonly List<(SpriteRenderer renderer, float phase)> _blossoms = new();
        private SpriteRenderer _tree;
        private TextMeshPro _label;

        public void Show(VillageArt art, VillageSnapshot snapshot)
        {
            int ranks = Mathf.Max(0, snapshot.TalentRanks);
            if (_tree == null) Build(art);

            float growth = Mathf.Clamp01(ranks / (float)RanksForFullSize);
            float scale = Mathf.Lerp(0.65f, 1.15f, growth);
            _tree.transform.localScale = Vector3.one * scale;
            Vector2 size = _tree.sprite != null ? (Vector2)_tree.sprite.bounds.size * scale : Vector2.one * 3f;
            _tree.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            int order = VillageSorting.Order(transform.position.y);
            _tree.sortingOrder = order;

            foreach (var (renderer, _) in _blossoms) Destroy(renderer.gameObject);
            _blossoms.Clear();
            int count = Mathf.Min(MaxBlossoms, ranks == 0 ? 0 : 1 + ranks / 3);
            var rng = new System.Random(77);
            for (int i = 0; i < count; i++)
            {
                // Scattered over the canopy (the top ~60% of the tree).
                var p = new Vector2(((float)rng.NextDouble() - 0.5f) * size.x * 0.75f, size.y * (0.45f + (float)rng.NextDouble() * 0.45f));
                var blossom = GeneratedSprites.CreateRenderer(transform, "Blossom", GeneratedSprites.Glow,
                    i % 3 == 0 ? new Color(1f, 0.9f, 0.45f, 0.9f) : new Color(1f, 0.6f, 0.85f, 0.9f), order + 1, p, Vector2.one * 0.55f);
                _blossoms.Add((blossom, (float)rng.NextDouble() * 6f));
            }

            _label.transform.localPosition = new Vector3(0f, size.y + 0.45f, 0f);
            _label.text = ranks > 0 ? $"Talent Tree\n<size=70%><color=#FFB0DA>{ranks} ranks learned</color></size>" : "Talent Tree\n<size=70%>Learn talents to make it bloom</size>";

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(size.x * 0.8f, size.y);
            box.offset = new Vector2(0f, size.y * 0.5f);
        }

        private void Build(VillageArt art)
        {
            _tree = GeneratedSprites.CreateRenderer(transform, "Tree", art != null && art.TalentTree != null ? art.TalentTree : GeneratedSprites.Circle,
                art != null && art.TalentTree != null ? Color.white : new Color(1f, 0.72f, 0.82f), 0);
            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.6f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(7f, 1.6f);
            _label.sortingOrder = VillageSorting.Labels;
        }

        private void Update()
        {
            // Blossoms twinkle.
            float t = Time.time;
            foreach (var (renderer, phase) in _blossoms)
                renderer.transform.localScale = Vector3.one * (0.45f + 0.15f * Mathf.Sin(t * 2.5f + phase));
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<VillageDisplayTappedEvent>.Raise(new VillageDisplayTappedEvent(VillageDisplayKind.Talents));
        }
    }
}
