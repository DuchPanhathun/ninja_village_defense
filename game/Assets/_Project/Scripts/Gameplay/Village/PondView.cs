using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// The fishing pond (EPIC 24 Phase 5): a grassy-banked pond stretched to <see cref="VillageLayout.Pond"/>, with
    /// ripples, lily pads, fish shadows gliding under the surface, a little boat, a wooden dock at the end of the path
    /// and a net full of the day's catch. Its label shows the casts left; tapping raises <see cref="PondTappedEvent"/>
    /// so the HUD opens the fishing mini-game. In someone else's village it's scenery.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PondView : MonoBehaviour, IVillageTappable
    {
        private TextMeshPro _label;
        private bool _visiting;
        private float _nextRefresh;

        public void Initialize(VillageArt art, bool visiting)
        {
            _visiting = visiting;
            name = "Pond";
            var rect = VillageLayout.Pond;
            transform.position = rect.center;

            var water = new GameObject("Water").AddComponent<SpriteRenderer>();
            water.transform.SetParent(transform, false);
            if (art != null && art.PondWater != null)
            {
                water.sprite = art.PondWater;
                water.drawMode = SpriteDrawMode.Sliced;
                water.size = rect.size;
            }
            else
            {
                water.sprite = GeneratedSprites.Square;
                water.color = new Color(0.44f, 0.86f, 0.93f);
                water.transform.localScale = new Vector3(rect.width, rect.height, 1f);
            }
            water.sortingOrder = VillageSorting.Paths + 1;

            var inner = new Rect(rect.xMin + 1.2f, rect.yMin + 1f, rect.width - 2.4f, rect.height - 2f);
            var rng = new System.Random(77);
            Vector2 Spot() => new(inner.xMin + (float)rng.NextDouble() * inner.width, inner.yMin + (float)rng.NextDouble() * inner.height);

            if (art != null && art.PondRipples.Length > 0)
                for (int i = 0; i < 5; i++)
                {
                    var ripple = GeneratedSprites.CreateRenderer(transform, "Ripple", art.PondRipples[0], new Color(1f, 1f, 1f, 0.8f),
                        VillageSorting.Paths + 2, Spot() - rect.center);
                    ripple.transform.localScale = Vector3.one * 0.7f;
                    ripple.gameObject.AddComponent<SpriteLoop>().SetFrames(art.PondRipples, 3f);
                }

            if (art != null && art.Fish.Length > 0)
                for (int i = 0; i < 4; i++)
                {
                    var fish = GeneratedSprites.CreateRenderer(transform, "FishShadow", art.Fish[i % 2], new Color(0.35f, 0.45f, 0.55f, 0.55f),
                        VillageSorting.Paths + 2, Spot() - rect.center);
                    fish.transform.localScale = Vector3.one * 0.6f;
                    fish.gameObject.AddComponent<PondFish>().Swim(inner, 0.5f + i * 0.15f);
                }

            if (art != null && art.PondLily != null)
                foreach (var offset in new[] { new Vector2(2.6f, -1.2f), new Vector2(-2.4f, 0.9f), new Vector2(1.7f, 0.8f) })
                    GeneratedSprites.CreateRenderer(transform, "Lily", art.PondLily, Color.white, VillageSorting.Paths + 3, offset)
                        .transform.localScale = Vector3.one * 0.7f;

            if (art != null && art.PondBoat != null)
            {
                var boat = GeneratedSprites.CreateRenderer(transform, "Boat", art.PondBoat, Color.white, VillageSorting.Paths + 4, new Vector2(-2f, -0.9f));
                boat.transform.localScale = Vector3.one * 0.75f;
                boat.gameObject.AddComponent<PondFish>().Bob();
            }

            if (art != null && art.PondDock != null)
            {
                var dock = GeneratedSprites.CreateRenderer(transform, "Dock", art.PondDock, Color.white, VillageSorting.Paths + 5,
                    VillageLayout.PondDock - rect.center);
                dock.transform.localScale = Vector3.one * 0.6f;
            }

            if (art != null && art.PondNet != null)
            {
                var feet = new Vector2(VillageLayout.PondDock.x + 1.5f, rect.yMax - 0.1f);
                GeneratedSprites.CreateRenderer(transform, "Net", art.PondNet, Color.white, VillageSorting.Order(feet.y),
                    feet + new Vector2(0f, art.PondNet.bounds.extents.y * 0.8f) - rect.center).transform.localScale = Vector3.one * 0.8f;
            }

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.transform.localPosition = new Vector3(-rect.width * 0.5f + 1.6f, rect.height * 0.5f + 0.55f, 0f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.6f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
            _label.sortingOrder = VillageSorting.Labels;

            var box = GetComponent<BoxCollider2D>();
            box.size = rect.size;
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            if (_visiting)
            {
                _label.text = "Fishing Pond";
                return;
            }
            int casts = FishingService.Casts;
            _label.text = casts > 0
                ? $"Fishing Pond\n<size=70%><color=#9CFF8A>Casts {casts}/{PondMineRules.MaxCasts}</color></size>"
                : $"Fishing Pond\n<size=70%><color=#F2A0A0>+1 cast in {GameClock.FormatCountdown(FishingService.UntilNextCast)}</color></size>";
        }

        public void OnTapped()
        {
            if (_visiting || VillageVisit.IsVisiting) return;
            EventBus<PondTappedEvent>.Raise(new PondTappedEvent());
        }
    }

    /// <summary>A fish shadow gliding between spots in the pond, or (with <see cref="Bob"/>) the boat rocking on the water.</summary>
    public class PondFish : MonoBehaviour
    {
        private Rect _area;
        private float _speed;
        private Vector2 _target;
        private bool _bob;
        private Vector3 _home;
        private SpriteRenderer _renderer;

        public void Swim(Rect area, float speed)
        {
            _area = area;
            _speed = speed;
            _renderer = GetComponent<SpriteRenderer>();
            PickTarget();
        }

        public void Bob()
        {
            _bob = true;
            _home = transform.localPosition;
        }

        private void PickTarget() =>
            _target = new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));

        private void Update()
        {
            if (_bob)
            {
                transform.localPosition = _home + new Vector3(0f, Mathf.Sin(Time.time * 1.3f) * 0.06f, 0f);
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.9f) * 3f);
                return;
            }
            Vector2 pos = transform.position;
            Vector2 to = _target - pos;
            if (to.sqrMagnitude < 0.04f)
            {
                PickTarget();
                return;
            }
            transform.position = pos + to.normalized * (_speed * Time.deltaTime);
            if (_renderer != null && Mathf.Abs(to.x) > 0.01f) _renderer.flipX = to.x > 0f; // pack fish face left
        }
    }
}
