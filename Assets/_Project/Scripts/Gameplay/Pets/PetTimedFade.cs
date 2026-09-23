using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Short-lived placeholder VFX (fire-breath cone, banana splat, treasure reveal flash): grows
    /// and fades its SpriteRenderers, then destroys itself. Uses scaled time so it freezes with
    /// the game when paused.
    /// </summary>
    public class PetTimedFade : MonoBehaviour
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float endScaleMultiplier = 1.2f;

        private SpriteRenderer[] _renderers;
        private float[] _startAlpha;
        private Vector3 _startScale;
        private float _elapsed;

        public void Configure(float seconds, float scaleMultiplier)
        {
            duration = Mathf.Max(0.01f, seconds);
            endScaleMultiplier = scaleMultiplier;
        }

        private void Start()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _startAlpha = new float[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _startAlpha[i] = _renderers[i].color.a;
            _startScale = transform.localScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            transform.localScale = _startScale * Mathf.Lerp(1f, endScaleMultiplier, t);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var c = _renderers[i].color;
                c.a = _startAlpha[i] * (1f - t);
                _renderers[i].color = c;
            }

            if (t >= 1f) Destroy(gameObject);
        }

        /// <summary>Spawns a tinted placeholder sprite that fades out — no prefab needed.</summary>
        public static GameObject Spawn(Sprite sprite, Vector2 position, float angleDegrees, Vector3 scale, Color color,
            float seconds, float scaleMultiplier = 1.2f, int sortingOrder = 15)
        {
            var go = new GameObject("PetFx");
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDegrees));
            go.transform.localScale = scale;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            var fade = go.AddComponent<PetTimedFade>();
            fade.Configure(seconds, scaleMultiplier);
            return go;
        }
    }
}
