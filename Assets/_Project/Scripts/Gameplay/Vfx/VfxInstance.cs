using UnityEngine;

namespace NinjaVillage.Gameplay.Vfx
{
    /// <summary>One running effect: scales from start to end size and fades out, then returns to the pool.</summary>
    public class VfxInstance : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _color;
        private Vector2 _startScale, _endScale;
        private float _duration, _elapsed;
        private bool _spin;
        private float _spinFrom, _spinTo;
        private Sprite[] _frames;
        private float _fps;

        public void Play(Sprite sprite, Vector2 position, Color color, float startSize, float endSize, float duration, int sortingOrder)
        {
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            gameObject.SetActive(true);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _renderer.sprite = sprite;
            _renderer.sortingOrder = sortingOrder;
            _color = color;
            _renderer.color = color;
            _startScale = Vector2.one * startSize;
            _endScale = Vector2.one * endSize;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _spin = false;
            _frames = null;
            Apply(0f);
        }

        /// <summary>Plays a pixel-art frame strip once at a fixed size and rotation (the frames animate themselves).</summary>
        public void PlayFrames(Sprite[] frames, float fps, Vector2 position, float scale, float rotationDegrees, Color color, int sortingOrder)
        {
            Play(frames[0], position, color, scale, scale, frames.Length / Mathf.Max(1f, fps), sortingOrder);
            _frames = frames;
            _fps = fps;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            _renderer.color = color;
        }

        public void SetScaleAxes(Vector2 start, Vector2 end)
        {
            _startScale = start;
            _endScale = end;
            Apply(0f);
        }

        public void SetSpin(float fromDegrees, float toDegrees)
        {
            _spin = true;
            _spinFrom = fromDegrees;
            _spinTo = toDegrees;
        }

        private void Update()
        {
            // Unscaled: effects triggered as the level-up panel pauses the game still finish.
            _elapsed += Time.unscaledDeltaTime;
            float t = _elapsed / _duration;
            if (t >= 1f)
            {
                Vfx.Return(this);
                return;
            }
            if (_frames != null)
            {
                int index = Mathf.Min(_frames.Length - 1, (int)(_elapsed * _fps));
                _renderer.sprite = _frames[index];
                return;
            }
            Apply(t);
        }

        private void Apply(float t)
        {
            float eased = 1f - (1f - t) * (1f - t); // ease-out
            Vector2 s = Vector2.LerpUnclamped(_startScale, _endScale, eased);
            transform.localScale = new Vector3(s.x, s.y, 1f);
            var c = _color;
            c.a = _color.a * (1f - t);
            _renderer.color = c;
            if (_spin) transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(_spinFrom, _spinTo, eased));
        }
    }
}
