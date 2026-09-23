using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Pulsing ring the Hawk leaves on a revealed treasure so the player spots it. Removes itself
    /// once every coin is collected; after its lifetime any uncollected coins vanish with it so
    /// the arena doesn't fill up with treasure nobody walked to.
    /// </summary>
    public class TreasureMarker : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        private GameObject[] _watched;
        private float _lifetime = 25f;
        private float _age;
        private float _checkTimer;
        private Vector3 _baseScale;

        public static TreasureMarker Spawn(Vector2 position, GameObject[] watched, float lifetime, Color color)
        {
            var go = new GameObject("HawkTreasureMarker");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.4f;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PetPlaceholderSprites.Ring;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 0.75f);
            spriteRenderer.sortingOrder = 4;

            var marker = go.AddComponent<TreasureMarker>();
            marker._watched = watched;
            marker._lifetime = Mathf.Max(1f, lifetime);
            return marker;
        }

        private void Awake() => _baseScale = transform.localScale;

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            transform.localScale = _baseScale * (1f + 0.12f * Mathf.Sin(_age * 4f));

            if (_age >= _lifetime)
            {
                Expire();
                return;
            }

            _checkTimer -= dt;
            if (_checkTimer > 0f) return;
            _checkTimer = CheckInterval;

            if (AllCollected()) Destroy(gameObject);
        }

        private bool AllCollected()
        {
            if (_watched == null) return true;
            for (int i = 0; i < _watched.Length; i++)
                if (_watched[i] != null) return false;
            return true;
        }

        private void Expire()
        {
            if (_watched != null)
            {
                for (int i = 0; i < _watched.Length; i++)
                    if (_watched[i] != null) Destroy(_watched[i]);
            }
            Destroy(gameObject);
        }
    }
}
