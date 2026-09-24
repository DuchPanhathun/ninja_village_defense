using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// One house plot on the residential lane (EPIC 24 Phase 4): a bare lot with a signpost ("Tap to build")
    /// while it's empty, faded with the castle level that opens it while locked, else the house in its style
    /// (drawn a bit smaller than the main buildings) with its level. Tapping raises
    /// <see cref="HousePlotTappedEvent"/> for the HUD; in someone else's village it shows their houses and ignores taps.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HouseView : MonoBehaviour, IVillageTappable
    {
        /// <summary>Houses are drawn at this scale so the village's main buildings stay the landmarks.</summary>
        public const float Scale = 0.75f;

        public int Plot { get; private set; }

        private VillageArt _art;
        private VillageSnapshot _visiting;
        private SpriteRenderer _lot, _sign, _body, _shadow;
        private TextMeshPro _label;
        private BoxCollider2D _collider;
        private string _shownKey;

        public void Initialize(int plot, VillageArt art, VillageSnapshot visiting)
        {
            Plot = plot;
            _art = art;
            _visiting = visiting;
            name = $"House_{plot}";
            transform.position = VillageLayout.HousePlot(plot);

            _lot = GeneratedSprites.CreateRenderer(transform, "Lot", GeneratedSprites.Square, new Color(0.55f, 0.43f, 0.28f, 0.45f),
                VillageSorting.Paths + 1, new Vector2(0f, 1.1f), new Vector2(3.2f, 2.3f));
            _shadow = GeneratedSprites.CreateRenderer(transform, "Shadow", GeneratedSprites.Circle, new Color(0f, 0f, 0f, 0.22f),
                VillageSorting.Paths + 1, new Vector2(0f, 0.1f), new Vector2(3f, 0.8f));
            _sign = new GameObject("Sign").AddComponent<SpriteRenderer>();
            _sign.transform.SetParent(transform, false);
            _sign.sprite = art != null ? art.HouseSign : null;
            if (_sign.sprite != null) _sign.transform.localPosition = new Vector3(-1.1f, _sign.sprite.bounds.extents.y, 0f);
            _sign.sortingOrder = VillageSorting.Order(transform.position.y);
            _body = new GameObject("Body").AddComponent<SpriteRenderer>();
            _body.transform.SetParent(transform, false);
            _body.transform.localScale = Vector3.one * Scale;
            _body.sortingOrder = VillageSorting.Order(transform.position.y);

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.6f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
            _label.sortingOrder = VillageSorting.Labels;

            _collider = GetComponent<BoxCollider2D>();
            Redraw();
        }

        private void Update() => Redraw();

        /// <summary>(locked, house) — live for your own village, from the snapshot when visiting.</summary>
        private (bool locked, HouseState house) Read()
        {
            if (_visiting != null)
            {
                int castle = Mathf.Max(1, _visiting.BuildingLevel(BuildingIds.Castle));
                HouseState found = null;
                foreach (var h in _visiting.Houses)
                    if (h != null && h.Plot == Plot) found = h;
                return (Plot >= HousingRules.UnlockedPlots(castle), found);
            }
            return (!HouseService.IsUnlocked(Plot), HouseService.Get(Plot));
        }

        private void Redraw()
        {
            var (locked, house) = Read();
            string key = $"{locked}|{house?.Style}|{house?.Level}";
            if (key == _shownKey) return;
            _shownKey = key;

            bool built = house != null && house.Level > 0;
            var sprite = built && _art != null ? _art.HouseSprite(house.Style) : null;
            _body.sprite = sprite;
            _body.enabled = sprite != null;
            Vector2 size = sprite != null ? (Vector2)sprite.bounds.size * Scale : new Vector2(3.2f, 2.3f);
            if (sprite != null) _body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            _shadow.enabled = built;
            _shadow.transform.localScale = new Vector3(size.x * 0.95f, 0.8f, 1f);
            _lot.enabled = !built;
            _lot.color = locked ? new Color(0.45f, 0.4f, 0.32f, 0.25f) : new Color(0.55f, 0.43f, 0.28f, 0.45f);
            _sign.enabled = !built && !locked && _sign.sprite != null;

            _collider.size = built ? size : new Vector2(3.2f, 2.3f);
            _collider.offset = new Vector2(0f, built ? size.y * 0.5f : 1.1f);

            if (built)
            {
                var style = HousingRules.Style(house.Style);
                _label.text = $"{(style.HasValue ? style.Value.Name : "House")}\n<size=70%><color=#FFD24D>Lv {house.Level}</color></size>";
                _label.transform.localPosition = new Vector3(0f, size.y + 0.45f, 0f);
            }
            else
            {
                _label.text = locked ? $"House plot\n<size=70%><color=#F2A0A0>Castle Lv {HousingRules.CastleLevelForPlot(Plot)}</color></size>"
                    : _visiting != null ? "" : "House plot\n<size=70%><color=#9CFF8A>Tap to build</color></size>";
                _label.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            }
        }

        public void OnTapped()
        {
            if (_visiting != null || VillageVisit.IsVisiting) return;
            EventBus<HousePlotTappedEvent>.Raise(new HousePlotTappedEvent(Plot));
        }
    }
}
