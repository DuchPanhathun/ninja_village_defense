using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Loot;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Hawk — "Reveals treasure". Every <c>Cooldown</c> seconds it flies to a random spot up to
    /// <c>Range</c> from the player and uncovers a small treasure: a cluster of coin pickups worth
    /// <c>Power</c> coins in total (boosted by the Gold Bonus stat) under a pulsing marker. The
    /// Hawk's passive Lucky Drop bonus comes from its definition's owner bonus.
    /// Uses the definition's ability prefab as the coin if set (e.g. Coin.prefab), otherwise a
    /// placeholder gold disc with a <see cref="CoinPickup"/>.
    /// </summary>
    public class HawkTreasureAbility : PetAbility
    {
        [SerializeField] private float firstRevealDelay = 8f;
        [SerializeField] private float scoutTimeout = 2.5f;
        [SerializeField] private float scoutSpeedMultiplier = 1.8f;
        [SerializeField] private float treasureLifetime = 25f;
        [SerializeField] private int minCoins = 3;
        [SerializeField] private int maxCoins = 8;
        [SerializeField] private Color treasureColor = new(1f, 0.82f, 0.2f, 1f);

        private float _cooldown;
        private bool _scouting;
        private float _scoutTimer;
        private Vector2 _spot;

        protected override void OnInitialized()
        {
            _cooldown = Mathf.Min(firstRevealDelay, Stats.Cooldown);
            _scouting = false;
        }

        private void Update()
        {
            if (!IsReady) return;
            var owner = Owner;
            if (owner == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (!_scouting)
            {
                _cooldown -= dt;
                if (_cooldown <= 0f) BeginScouting(owner.position);
                return;
            }

            _scoutTimer -= dt;
            if (Controller.IsNear(_spot, 0.35f) || _scoutTimer <= 0f)
                Reveal();
        }

        private void BeginScouting(Vector2 ownerPosition)
        {
            Vector2 direction = Random.insideUnitCircle;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            float distance = Random.Range(Stats.Range * 0.5f, Stats.Range);

            _spot = ownerPosition + direction * distance;
            _scouting = true;
            _scoutTimer = scoutTimeout;
            Controller.SetMoveTarget(_spot, scoutSpeedMultiplier);
        }

        private void Reveal()
        {
            _scouting = false;
            _cooldown = Stats.Cooldown;
            Controller.ClearMoveTarget();
            SpawnTreasure(_spot);
        }

        private void SpawnTreasure(Vector2 at)
        {
            float goldMultiplier = OwnerStats != null ? OwnerStats.GoldBonusMultiplier : 1f;
            int total = Mathf.Max(1, Mathf.RoundToInt(Stats.Power * goldMultiplier));
            int count = Mathf.Clamp(total / 10, Mathf.Max(1, minCoins), Mathf.Max(1, maxCoins));
            count = Mathf.Min(count, total); // every coin is worth at least 1

            int perCoin = total / count;
            int remainder = total - perCoin * count;

            var coins = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                int amount = perCoin + (i < remainder ? 1 : 0);
                Vector2 position = at + Random.insideUnitCircle * 0.6f;
                coins[i] = SpawnCoin(position, amount);
            }

            TreasureMarker.Spawn(at, coins, treasureLifetime, treasureColor);
            PetTimedFade.Spawn(PetPlaceholderSprites.Ring, at, 0f, Vector3.one * 0.8f, treasureColor, 0.5f, 2.5f);
            Sfx.PlayAt(AudioCueIds.ChestOpen, at);
        }

        private GameObject SpawnCoin(Vector2 position, int amount)
        {
            GameObject go;
            if (Definition != null && Definition.AbilityPrefab != null)
            {
                go = Instantiate(Definition.AbilityPrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject("HawkTreasureCoin");
                go.transform.position = position;
                go.transform.localScale = Vector3.one * 0.35f;
                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = PetPlaceholderSprites.Circle;
                spriteRenderer.color = treasureColor;
                spriteRenderer.sortingOrder = 5;
            }

            if (!go.TryGetComponent<CoinPickup>(out var coin))
                coin = go.AddComponent<CoinPickup>();
            coin.Initialize(amount);
            return go;
        }
    }
}
