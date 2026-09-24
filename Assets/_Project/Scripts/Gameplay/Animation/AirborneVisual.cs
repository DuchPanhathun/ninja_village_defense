using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.World;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>
    /// Lifts a character's sprite off the ground for a jump or leap without moving its physics body: the
    /// real renderer hides and a copy that mirrors it every frame (sprite, flip, tint) is drawn raised by
    /// <see cref="Height"/>, over a shadow that stays on the ground and shrinks as the character rises.
    /// While airborne the character draws above scenery (<see cref="DepthSort.Airborne"/>).
    /// </summary>
    public class AirborneVisual : MonoBehaviour
    {
        private SpriteRenderer _source, _copy, _shadow;
        private DepthSort _depth;
        private float _feet;

        public bool IsAirborne => _copy != null;
        /// <summary>World units above the ground.</summary>
        public float Height { get; set; }

        public void Begin()
        {
            if (IsAirborne) return;
            _source = GetComponent<SpriteRenderer>();
            if (_source == null) _source = GetComponentInChildren<SpriteRenderer>();
            if (_source == null) return;
            _depth = GetComponent<DepthSort>();
            if (_depth != null) _depth.Airborne = true;

            _feet = _source.bounds.min.y - _source.transform.position.y;
            _shadow = GeneratedSprites.CreateRenderer(transform, "JumpShadow", GeneratedSprites.Circle, new Color(0f, 0f, 0f, 0.35f), _source.sortingOrder - 1);
            _copy = new GameObject("AirborneSprite").AddComponent<SpriteRenderer>();
            _copy.transform.SetParent(_source.transform, false);
            _source.enabled = false;
            Height = 0f;
            LateUpdate();
        }

        public void End()
        {
            if (_copy != null) Destroy(_copy.gameObject);
            if (_shadow != null) Destroy(_shadow.gameObject);
            _copy = null;
            _shadow = null;
            if (_source != null) _source.enabled = true;
            if (_depth != null) _depth.Airborne = false;
            Height = 0f;
        }

        private void OnDisable() => End();

        private void LateUpdate()
        {
            if (_copy == null || _source == null) return;
            _copy.sprite = _source.sprite;
            _copy.flipX = _source.flipX;
            _copy.color = _source.color;
            _copy.sortingOrder = _source.sortingOrder + 2;

            // Local offsets are in the (possibly flipped/scaled) parent's space.
            float scaleY = Mathf.Max(0.01f, Mathf.Abs(_source.transform.lossyScale.y));
            _copy.transform.localPosition = new Vector3(0f, Height / scaleY, 0f);

            float width = _source.sprite != null ? _source.sprite.bounds.size.x * Mathf.Abs(_source.transform.lossyScale.x) : 1f;
            float shrink = Mathf.Clamp01(1f - Height * 0.25f);
            float parentX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x)), parentY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            _shadow.transform.localPosition = new Vector3(0f, _feet / parentY + 0.05f, 0f);
            _shadow.transform.localScale = new Vector3(width * 0.7f * shrink / parentX, width * 0.25f * shrink / parentY, 1f);
            _shadow.sortingOrder = _source.sortingOrder - 1;
        }
    }
}
