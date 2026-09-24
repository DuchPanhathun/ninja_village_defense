using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>
    /// "Creates 8 clones for 15 seconds." Each clone mirrors the player's auto-attack:
    /// finds the nearest enemy, fires a projectile at it on a shared cooldown.
    /// </summary>
    [CreateAssetMenu(fileName = "Ultimate_ShadowCloneArmy", menuName = "Ninja Village/Ultimates/Shadow Clone Army")]
    public class ShadowCloneArmyUltimate : UltimateDefinition
    {
        [SerializeField] private int cloneCount = 8;
        [SerializeField] private float cloneDuration = 15f;
        [SerializeField] private float cloneOrbitRadius = 2f;
        [Tooltip("Damage fraction relative to the player's current weapon damage.")]
        [SerializeField] private float damageFraction = 0.5f;
        [SerializeField] private GameObject clonePrefab;

        public override void Activate(in UltimateContext context)
        {
            context.Runner.StartCoroutine(SpawnClonesRoutine(context));
        }

        private IEnumerator SpawnClonesRoutine(UltimateContext context)
        {
            var spawnedClones = new GameObject[cloneCount];

            for (int i = 0; i < cloneCount; i++)
            {
                float angle = i * (360f / cloneCount) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cloneOrbitRadius;
                Vector3 spawnPos = context.PlayerTransform.position + (Vector3)offset;

                GameObject cloneGo;
                if (clonePrefab != null)
                {
                    cloneGo = Object.Instantiate(clonePrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    cloneGo = new GameObject($"ShadowClone_{i}");
                    cloneGo.transform.position = spawnPos;
                }

                NinjaVillage.Gameplay.Vfx.Vfx.DeathPuff(spawnPos, ShadowCloneAttacker.ShadowColor); // appear in a puff of smoke
                var cloneAttacker = cloneGo.GetComponent<ShadowCloneAttacker>();
                if (cloneAttacker == null)
                    cloneAttacker = cloneGo.AddComponent<ShadowCloneAttacker>();

                cloneAttacker.Initialize(context.PlayerTransform, context.Stats, damageFraction, context.EnemyMask);
                spawnedClones[i] = cloneGo;
            }

            // Orbit + despawn
            float elapsed = 0f;
            while (elapsed < cloneDuration)
            {
                elapsed += Time.deltaTime;

                if (context.PlayerTransform == null) break;

                for (int i = 0; i < cloneCount; i++)
                {
                    if (spawnedClones[i] == null) continue;

                    float angle = (i * (360f / cloneCount) + elapsed * 30f) * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cloneOrbitRadius;
                    spawnedClones[i].transform.position = context.PlayerTransform.position + (Vector3)offset;
                }

                yield return null;
            }

            foreach (var clone in spawnedClones)
            {
                if (clone == null) continue;
                NinjaVillage.Gameplay.Vfx.Vfx.DeathPuff(clone.transform.position, ShadowCloneAttacker.ShadowColor);
                Object.Destroy(clone);
            }
        }
    }

    /// <summary>
    /// Attached to each shadow clone: finds the nearest enemy and fires a simple
    /// projectile at it on an independent cooldown.
    /// </summary>
    public class ShadowCloneAttacker : MonoBehaviour
    {
        private Transform _playerTransform;
        private PlayerStats _playerStats;
        private float _damageFraction;
        private LayerMask _enemyMask;
        private float _cooldownRemaining;
        private Transform _visual;
        private SpriteRenderer _bodyRenderer;
        private float _flickerPhase;
        private Sprite _projectileSprite;

        public static readonly Color ShadowColor = new(0.42f, 0.28f, 0.72f, 0.82f);
        /// <summary>Near the hero's own colours (a purple multiply tint turns dark heroes into blobs, and
        /// see-through bodies turn muddy on the grass); the violet aura underneath marks them as clones.</summary>
        private static readonly Color SpiritColor = new(0.85f, 0.8f, 1f, 0.92f);
        private const float BodyScale = 0.8f;

        private const float AttacksPerSecond = 2f;
        private const float ProjectileSpeed = 10f;
        private const float Range = 6f;

        public void Initialize(Transform playerTransform, PlayerStats stats, float damageFraction, LayerMask enemyMask)
        {
            _playerTransform = playerTransform;
            _playerStats = stats;
            _damageFraction = damageFraction;
            _enemyMask = enemyMask;

            // Copies of the player: the hero's (or skin's) own animated frames, a bit smaller, over a violet
            // chakra aura so they stand apart from both the player and the enemies.
            var heroFrames = playerTransform != null ? playerTransform.GetComponent<NinjaVillage.Gameplay.Animation.SpriteFrameAnimator>() : null;
            if (GetComponentInChildren<SpriteRenderer>() == null && heroFrames != null && heroFrames.SpriteSet != null)
            {
                var body = new GameObject("ShadowBody");
                body.transform.SetParent(transform, false);
                var bodyRenderer = body.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = heroFrames.SpriteSet.DefaultSprite;
                bodyRenderer.color = SpiritColor;
                bodyRenderer.sortingOrder = 25;
                body.AddComponent<NinjaVillage.Gameplay.Animation.SpriteFrameAnimator>().SetSpriteSet(heroFrames.SpriteSet);
                body.transform.localScale = Vector3.one * BodyScale;
                _visual = body.transform;
                _bodyRenderer = bodyRenderer;
                _flickerPhase = Random.value * 10f;
                GeneratedSprites.CreateRenderer(transform, "SpiritGlow", GeneratedSprites.Glow, new Color(0.7f, 0.4f, 1f, 0.6f), 24,
                    new Vector2(0f, -0.1f), new Vector2(1.5f, 1.5f));
            }

            // They throw what the player throws (the weapon's projectile sprite), else a kunai.
            var attack = playerTransform != null ? playerTransform.GetComponent<AutoAttackController>() : null;
            var prefab = attack != null && attack.Weapon != null ? attack.Weapon.Definition.ProjectilePrefab : null;
            var prefabRenderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
            _projectileSprite = prefabRenderer != null ? prefabRenderer.sprite : null;
            if (_projectileSprite == null && NinjaVillage.Gameplay.Vfx.Vfx.ArtCatalog != null)
                _projectileSprite = NinjaVillage.Gameplay.Vfx.Vfx.ArtCatalog.Kunai;

            // No hero art at all: a shadowy placeholder body.
            if (GetComponentInChildren<SpriteRenderer>() == null)
            {
                GeneratedSprites.CreateRenderer(transform, "ShadowBody", GeneratedSprites.Circle, new Color(0.25f, 0.1f, 0.4f, 0.75f), 25,
                    Vector2.zero, new Vector2(0.6f, 0.75f));
                GeneratedSprites.CreateRenderer(transform, "ShadowGlow", GeneratedSprites.Glow, new Color(0.6f, 0.3f, 1f, 0.4f), 24,
                    Vector2.zero, new Vector2(1.3f, 1.3f));
            }
        }

        private void Update()
        {
            if (_bodyRenderer != null) // a gentle ghostly flicker
            {
                var c = SpiritColor;
                c.a *= 0.88f + 0.12f * Mathf.Sin(Time.time * 7f + _flickerPhase);
                _bodyRenderer.color = c;
            }

            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining > 0f) return;

            var target = TargetFinder.FindNearest(transform.position, Range, _enemyMask);
            if (target == null) return;

            Fire(target);
            _cooldownRemaining = 1f / AttacksPerSecond;
        }

        private void Fire(Transform target)
        {
            float baseDamage = _playerStats != null ? 10f * _playerStats.AttackDamageMultiplier * _damageFraction : 5f;
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            if (_visual != null && Mathf.Abs(dir.x) > 0.01f) // face the target (sprites face right)
                _visual.localScale = new Vector3(Mathf.Sign(dir.x) * BodyScale, BodyScale, 1f);

            var go = new GameObject("CloneProjectile");
            go.transform.position = transform.position;
            go.transform.right = dir;
            go.layer = gameObject.layer;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * ProjectileSpeed;

            if (_projectileSprite != null)
            {
                var sprite = new GameObject("Sprite").AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(go.transform, false);
                sprite.transform.localScale = Vector3.one * 0.8f;
                sprite.sprite = _projectileSprite;
                sprite.color = new Color(0.75f, 0.55f, 1f, 1f);
                sprite.sortingOrder = 30;
            }
            else
            {
                GeneratedSprites.CreateRenderer(go.transform, "Glow", GeneratedSprites.Glow, new Color(0.7f, 0.4f, 1f, 0.9f), 30,
                    Vector2.zero, new Vector2(0.45f, 0.45f));
            }
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            var proj = go.AddComponent<ShadowCloneProjectile>();
            proj.Initialize(baseDamage, _enemyMask, gameObject);
        }
    }

    /// <summary>
    /// Projectile fired by shadow clones — simple hit-and-destroy on enemy layer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShadowCloneProjectile : MonoBehaviour
    {
        private float _damage;
        private LayerMask _hitMask;
        private GameObject _source;
        private float _spawnTime;
        private const float Lifetime = 3f;

        public void Initialize(float damage, LayerMask hitMask, GameObject source)
        {
            _damage = damage;
            _hitMask = hitMask;
            _source = source;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= Lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _hitMask) == 0) return;
            if (!other.TryGetComponent<IDamageable>(out var target) || !target.IsAlive) return;

            Vector2 dir = GetComponent<Rigidbody2D>().linearVelocity.normalized;
            target.TakeDamage(new DamageInfo(_damage, false, dir, 2f, _source));
            Destroy(gameObject);
        }
    }
}
