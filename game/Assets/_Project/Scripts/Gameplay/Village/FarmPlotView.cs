using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// One farm bed on the village map: tilled soil (darker once watered) with the crop's current stage —
    /// seed, leafy growth, then the ripe crop bobbing on a glow with "Ready!" — and the time left. Tapping a
    /// ripe crop harvests it right there ("+3 Rice" floats up); tapping anything else asks the HUD for the
    /// seed picker / plot status. Locked beds show the castle level that opens them. In someone else's
    /// village it shows their crops from the snapshot and ignores taps.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FarmPlotView : MonoBehaviour, IVillageTappable
    {
        public int Plot { get; private set; }

        private SpriteRenderer _soil, _crop, _glow;
        private TextMeshPro _label;
        private VillageSnapshot _visiting;
        private float _phase;

        public void Initialize(int plot, VillageArt art, VillageSnapshot visiting)
        {
            Plot = plot;
            _visiting = visiting;
            name = $"FarmPlot_{plot}";
            transform.position = VillageLayout.FarmPlot(plot);

            var soilSprite = art != null ? art.FarmSoil : null;
            _soil = GeneratedSprites.CreateRenderer(transform, "Soil", soilSprite != null ? soilSprite : GeneratedSprites.Square,
                soilSprite != null ? Color.white : new Color(0.45f, 0.3f, 0.18f), VillageSorting.Paths + 2);
            if (soilSprite == null) _soil.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            _glow = GeneratedSprites.CreateRenderer(transform, "Glow", GeneratedSprites.Glow, new Color(1f, 0.9f, 0.45f, 0.5f), 0,
                new Vector2(0f, 0.2f), new Vector2(1.9f, 1.9f));
            _crop = new GameObject("Crop").AddComponent<SpriteRenderer>();
            _crop.transform.SetParent(transform, false);

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.transform.localPosition = new Vector3(0f, -0.78f, 0f); // inside the bed, clear of the one below
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.25f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(3f, 0.8f);
            _label.sortingOrder = VillageSorting.Labels;

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(2.3f, 2.3f);
            _phase = plot * 0.9f;
            Redraw();
        }

        private void Update() => Redraw();

        /// <summary>The plot's state: live from the farm for your own village, from the snapshot when visiting.</summary>
        private (bool locked, FarmPlotState state) Read()
        {
            if (_visiting != null)
            {
                int castle = Mathf.Max(1, _visiting.BuildingLevel(BuildingIds.Castle));
                FarmPlotState found = null;
                foreach (var p in _visiting.Farm)
                    if (p != null && p.Plot == Plot) found = p;
                return (Plot >= FarmRules.UnlockedPlots(castle), found);
            }
            return (!FarmService.IsUnlocked(Plot), FarmService.GetPlot(Plot));
        }

        private void Redraw()
        {
            var (locked, state) = Read();
            var now = GameClock.UtcNow;
            var crop = state != null ? FarmService.GetCrop(state.CropId) : null;
            var stage = state == null ? CropStage.Empty : FarmRules.Stage(now, state.PlantedTicks, state.ReadyTicks);

            _soil.color = locked ? new Color(1f, 1f, 1f, 0.35f) : state != null && state.Watered ? new Color(0.72f, 0.72f, 0.82f) : Color.white;
            int order = VillageSorting.Order(transform.position.y - 0.9f);
            _crop.sortingOrder = order;
            _glow.sortingOrder = order - 1;

            Sprite sprite = crop == null ? null : stage switch
            {
                CropStage.Seed => crop.SeedSprite,
                CropStage.Growing => crop.GrowingSprite,
                CropStage.Ripe => crop.RipeSprite,
                _ => null,
            };
            _crop.sprite = sprite;
            _crop.enabled = sprite != null;
            bool ripe = stage == CropStage.Ripe && crop != null;
            _glow.enabled = ripe;

            // Ripe crops bob so they catch the eye from across the village.
            float bob = ripe ? Mathf.Abs(Mathf.Sin(Time.time * 3f + _phase)) * 0.18f : 0f;
            _crop.transform.localPosition = new Vector3(0f, 0.1f + bob, 0f);
            _crop.transform.localScale = Vector3.one * (stage == CropStage.Growing ? 0.9f : 1f);

            if (locked) _label.text = $"<color=#F2A0A0>Castle Lv {FarmRules.CastleLevelForPlot(Plot)}</color>";
            else if (state == null) _label.text = _visiting != null ? "" : "<color=#E8D8B8>Plant</color>";
            else if (ripe) _label.text = "<color=#FFD24D>Ready!</color>";
            else _label.text = FarmRules.Format(FarmRules.TimeLeft(now, state.ReadyTicks));
        }

        public void OnTapped()
        {
            if (_visiting != null || VillageVisit.IsVisiting) return;
            if (FarmService.Stage(Plot) == CropStage.Ripe)
            {
                if (FarmService.Harvest(Plot, out var harvest, out int amount) == FarmResult.Success && harvest != null)
                {
                    Sfx.Play(AudioCueIds.RewardClaim);
                    FloatingText.Spawn(transform.position + new Vector3(0f, 0.9f, 0f), $"+{amount} {harvest.NameOrId}", new Color(0.7f, 1f, 0.5f));
                }
                return;
            }
            EventBus<FarmPlotTappedEvent>.Raise(new FarmPlotTappedEvent(Plot));
        }
    }

    /// <summary>World text that rises and fades ("+3 Rice").</summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _age;

        public static void Spawn(Vector3 position, string text, Color color)
        {
            var label = new GameObject("FloatingText").AddComponent<TextMeshPro>();
            label.transform.position = position;
            label.text = text;
            label.color = color;
            label.fontSize = 3f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color32(20, 27, 27, 255);
            label.rectTransform.sizeDelta = new Vector2(5f, 1f);
            label.sortingOrder = VillageSorting.Labels + 20;
            label.gameObject.AddComponent<FloatingText>()._text = label;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * (1.2f * Time.deltaTime);
            var c = _text.color;
            c.a = Mathf.Clamp01(1.4f - _age);
            _text.color = c;
            if (_age > 1.4f) Destroy(gameObject);
        }
    }
}
