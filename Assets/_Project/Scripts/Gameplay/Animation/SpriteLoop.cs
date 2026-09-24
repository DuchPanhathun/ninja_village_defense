using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>Loops a list of frames on this object's SpriteRenderer: spinning shuriken, coins, flags.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField] private float fps = 10f;
        [Tooltip("Start each copy on a random frame so crowds of pickups don't blink in sync.")]
        [SerializeField] private bool randomStart = true;

        private SpriteRenderer _renderer;
        private float _time;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (randomStart) _time = Random.value * 10f;
        }

        public void SetFrames(Sprite[] value, float framesPerSecond)
        {
            frames = value ?? System.Array.Empty<Sprite>();
            fps = framesPerSecond;
        }

        private void Update()
        {
            if (frames.Length == 0 || _renderer == null) return;
            _time += Time.deltaTime;
            var sprite = frames[(int)(_time * fps) % frames.Length];
            if (sprite != null) _renderer.sprite = sprite;
        }
    }
}
