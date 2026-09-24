using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// The village treasury on the plaza (EPIC 24 Phase 4): a chest with coin piles that grow as it fills and a
    /// glow once it's full, labelled with what's inside. Tapping collects everything with a burst of coins; an
    /// empty treasury says how long until it's full. In someone else's village it's just the chest.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TreasuryView : MonoBehaviour, IVillageTappable
    {
        private const int PileCoins = 14;
        private const float ChestScale = 1.9f;

        private SpriteRenderer _chest, _glow;
        private readonly SpriteRenderer[] _coins = new SpriteRenderer[PileCoins];
        private TextMeshPro _label;
        private Sprite _coinSprite;
        private bool _visiting;
        private float _nextRefresh;

        public void Initialize(VillageArt art, bool visiting)
        {
            _visiting = visiting;
            name = "Treasury";
            transform.position = VillageLayout.Treasury;
            int order = VillageSorting.Order(transform.position.y);

            _glow = GeneratedSprites.CreateRenderer(transform, "Glow", GeneratedSprites.Glow, new Color(1f, 0.85f, 0.35f, 0.55f), order - 2,
                new Vector2(0f, 0.8f), new Vector2(3.4f, 3.4f));
            _chest = new GameObject("Chest").AddComponent<SpriteRenderer>();
            _chest.transform.SetParent(transform, false);
            _chest.sprite = art != null ? art.TreasuryChest : GeneratedSprites.Square;
            _chest.transform.localScale = Vector3.one * ChestScale;
            _chest.transform.localPosition = new Vector3(0f, _chest.sprite.bounds.extents.y * ChestScale, 0f);
            _chest.sortingOrder = order;

            // Two piles either side of the chest, then a few on its lid, in the order they appear.
            _coinSprite = art != null ? art.Coin : null;
            for (int i = 0; i < PileCoins; i++)
            {
                var coin = new GameObject($"Coin{i}").AddComponent<SpriteRenderer>();
                coin.transform.SetParent(transform, false);
                coin.sprite = _coinSprite != null ? _coinSprite : GeneratedSprites.Circle;
                coin.transform.localScale = Vector3.one * 0.55f;
                int side = i % 2 == 0 ? -1 : 1;
                int row = i / 2;
                Vector2 spot = row < 5
                    ? new Vector2(side * (1.45f + (row % 2) * 0.28f), 0.12f + row * 0.17f)
                    : new Vector2(side * 0.3f * (row - 4), 1.95f);
                coin.transform.localPosition = spot;
                coin.sortingOrder = row < 5 ? order + 1 : order + 2;
                _coins[i] = coin;
            }

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 2.6f;
            _label.fontStyle = FontStyles.Bold;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
            _label.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            _label.sortingOrder = VillageSorting.Labels;

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(3.4f, 2.4f);
            box.offset = new Vector2(0f, 1f);
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
            if (_glow.enabled) _glow.color = new Color(1f, 0.85f, 0.35f, 0.4f + 0.2f * Mathf.Sin(Time.time * 3f));
        }

        private void Refresh()
        {
            if (_visiting)
            {
                _label.text = "Treasury";
                _glow.enabled = false;
                foreach (var coin in _coins) coin.enabled = false;
                return;
            }
            int available = TreasuryService.Available, capacity = TreasuryService.Capacity;
            bool full = available >= capacity;
            int shown = Mathf.CeilToInt(TreasuryService.Fill * PileCoins);
            for (int i = 0; i < _coins.Length; i++) _coins[i].enabled = i < shown;
            _glow.enabled = full;
            _label.text = full
                ? $"Treasury\n<size=75%><color=#FFD24D>{available} coins · FULL!</color></size>"
                : $"Treasury\n<size=75%><color=#FFD24D>{available}</color> / {capacity}</size>";
        }

        public void OnTapped()
        {
            if (_visiting || VillageVisit.IsVisiting) return;
            int coins = TreasuryService.Collect();
            if (coins > 0)
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                FloatingText.Spawn(transform.position + new Vector3(0f, 2.4f, 0f), $"+{coins} coins", new Color(1f, 0.85f, 0.3f));
                CoinBurst.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), _coinSprite, Mathf.Clamp(coins / 15, 6, 18));
            }
            else
            {
                Sfx.Play(AudioCueIds.UiClick);
                FloatingText.Spawn(transform.position + new Vector3(0f, 2.4f, 0f),
                    $"Filling up: +{Mathf.RoundToInt(TreasuryService.CoinsPerHour)} an hour", new Color(0.9f, 0.9f, 0.85f));
            }
            Refresh();
        }
    }

    /// <summary>Coins that leap out of the treasury, spin, fall and fade.</summary>
    public class CoinBurst : MonoBehaviour
    {
        private Vector2 _velocity;
        private SpriteRenderer _renderer;
        private float _age, _life;

        public static void Spawn(Vector3 origin, Sprite sprite, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("CoinBurst");
                go.transform.position = origin;
                go.transform.localScale = Vector3.one * 0.6f;
                var burst = go.AddComponent<CoinBurst>();
                burst._renderer = go.AddComponent<SpriteRenderer>();
                burst._renderer.sprite = sprite != null ? sprite : GeneratedSprites.Circle;
                burst._renderer.sortingOrder = VillageSorting.Labels + 15;
                float angle = Mathf.Lerp(35f, 145f, (i + Random.value) / count) * Mathf.Deg2Rad;
                burst._velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(4f, 7f);
                burst._life = Random.Range(0.8f, 1.1f);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _velocity += Vector2.down * (14f * Time.deltaTime);
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            transform.localScale = new Vector3(0.6f * Mathf.Cos(_age * 14f), 0.6f, 1f); // spin
            var c = _renderer.color;
            c.a = Mathf.Clamp01((_life - _age) * 3f);
            _renderer.color = c;
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
