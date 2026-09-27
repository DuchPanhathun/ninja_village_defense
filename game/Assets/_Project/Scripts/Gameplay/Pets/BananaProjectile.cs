using NinjaVillage.Core.Audio;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// The Monkey's banana: a lobbed arc (no physics) that homes on its target's current position,
    /// then splashes — damaging and stunning every enemy in a small radius via
    /// <c>StatusEffectReceiver.ApplyStun</c>. Works with or without a custom visual prefab.
    /// </summary>
    public class BananaProjectile : MonoBehaviour
    {
        [SerializeField] private float spinDegreesPerSecond = 720f;
        [SerializeField] private Color splatColor = new(1f, 0.9f, 0.3f, 0.8f);

        private Vector2 _start;
        private Vector2 _end;
        private Transform _target;
        private float _flightTime;
        private float _elapsed;
        private float _arcHeight;
        private float _damage;
        private float _stunSeconds;
        private float _splashRadius;
        private LayerMask _mask;
        private GameObject _source;
        private bool _launched;

        public void Launch(Vector2 start, Transform target, float flightTime, float arcHeight, float damage,
            float stunSeconds, float splashRadius, LayerMask mask, GameObject source)
        {
            _start = start;
            _target = target;
            _end = target != null ? (Vector2)target.position : start;
            _flightTime = Mathf.Max(0.05f, flightTime);
            _arcHeight = arcHeight;
            _damage = damage;
            _stunSeconds = stunSeconds;
            _splashRadius = Mathf.Max(0.1f, splashRadius);
            _mask = mask;
            _source = source;
            _elapsed = 0f;
            _launched = true;
            transform.position = start;
        }

        private void Update()
        {
            if (!_launched) return;

            float dt = Time.deltaTime;
            _elapsed += dt;
            if (_target != null) _end = _target.position;

            float t = Mathf.Clamp01(_elapsed / _flightTime);
            Vector2 flat = Vector2.Lerp(_start, _end, t);
            float height = _arcHeight * 4f * t * (1f - t);
            transform.position = new Vector3(flat.x, flat.y + height, transform.position.z);
            transform.Rotate(0f, 0f, spinDegreesPerSecond * dt);

            if (t >= 1f) Land();
        }

        private void Land()
        {
            _launched = false;
            PetCombat.HitArea(_end, _splashRadius, _mask, _damage, 0.5f, _source, default, 180f, _stunSeconds);
            PetTimedFade.Spawn(PetPlaceholderSprites.Circle, _end, 0f, Vector3.one * (_splashRadius * 1.6f), splatColor, 0.3f, 1.3f);
            Sfx.PlayAt(AudioCueIds.EnemyHit, _end, 0.6f);
            Destroy(gameObject);
        }

        /// <summary>Placeholder banana (yellow ellipse) for pets without an ability prefab.</summary>
        public static GameObject CreatePlaceholder(Vector2 position)
        {
            var go = new GameObject("Banana");
            go.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.38f, 0.18f, 1f);
            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PetPlaceholderSprites.Circle;
            spriteRenderer.color = new Color(1f, 0.92f, 0.25f, 1f);
            spriteRenderer.sortingOrder = 16;
            return go;
        }
    }
}
