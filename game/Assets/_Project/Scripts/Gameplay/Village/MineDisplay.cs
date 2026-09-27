using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// What the mine has dug up (EPIC 24 Phase 5): a pile of iron bars at the entrance that grows as it fills, a
    /// label with the count ("FULL!" when it stops), and a burst of bars when they're collected (the mine building
    /// itself handles the tap). Hidden until the mine is built, and in someone else's village.
    /// </summary>
    public class MineDisplay : MonoBehaviour
    {
        private const int PileBars = 8;

        private readonly SpriteRenderer[] _pile = new SpriteRenderer[PileBars];
        private TextMeshPro _label;
        private Sprite _bar;
        private bool _visiting;
        private float _nextRefresh;

        public void Initialize(VillageArt art, bool visiting)
        {
            _visiting = visiting;
            name = "MineDisplay";
            var mine = VillageService.Get(BuildingIds.Mine);
            Vector2 front = mine != null ? mine.PlotPosition - new Vector2(0f, mine.Footprint.y * 0.5f) : new Vector2(17.25f, 11f);
            transform.position = front;
            _bar = art != null && art.Bars.Length > 0 ? art.Bars[0] : null;

            for (int i = 0; i < PileBars; i++)
            {
                var bar = new GameObject($"Bar{i}").AddComponent<SpriteRenderer>();
                bar.transform.SetParent(transform, false);
                bar.sprite = _bar;
                bar.transform.localScale = Vector3.one * 0.5f;
                int row = i < 4 ? 0 : i < 7 ? 1 : 2;
                int col = i < 4 ? i : i < 7 ? i - 4 : 0;
                float width = row == 0 ? 4 : row == 1 ? 3 : 1;
                bar.transform.localPosition = new Vector3(-1.6f + (col - (width - 1) * 0.5f) * 0.5f, 0.05f + row * 0.28f, 0f);
                bar.sortingOrder = VillageSorting.Order(front.y) + 5 + row;
                _pile[i] = bar;
            }

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.4f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(6f, 1f);
            _label.sortingOrder = VillageSorting.Labels;
            Refresh();
        }

        private void OnEnable() => EventBus<MineCollectedEvent>.Subscribe(OnCollected);
        private void OnDisable() => EventBus<MineCollectedEvent>.Unsubscribe(OnCollected);

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh || _label == null) return;
            _nextRefresh = Time.unscaledTime + 0.3f;
            Refresh();
        }

        private void Refresh()
        {
            bool show = !_visiting && MineService.IsBuilt;
            _label.gameObject.SetActive(show);
            int shown = show ? Mathf.CeilToInt(MineService.Fill * PileBars) : 0;
            for (int i = 0; i < _pile.Length; i++) _pile[i].enabled = i < shown;
            if (!show) return;
            int available = MineService.Available, capacity = MineService.Capacity;
            _label.text = available >= capacity
                ? $"<color=#FFD24D>{available} bars · FULL!</color>"
                : $"{available} / {capacity} bars";
        }

        private void OnCollected(MineCollectedEvent evt)
        {
            if (_visiting || evt.Haul.Bars <= 0) return;
            FloatingText.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), $"+{evt.Haul}", new Color(0.85f, 0.9f, 1f));
            CoinBurst.Spawn(transform.position + new Vector3(-1.6f, 0.4f, 0f), _bar, Mathf.Clamp(evt.Haul.Bars, 4, 12));
            Refresh();
        }
    }
}
