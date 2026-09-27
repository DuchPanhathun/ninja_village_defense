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
        private NinjaVillage.Gameplay.Mounts.MountVisual _mount;

        public bool IsAirborne => _copy != null || _mount != null;
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

            // Riding: the mount (and its rider) jump together; the mount does the drawing.
            var mount = GetComponent<NinjaVillage.Gameplay.Mounts.MountVisual>();
            if (mount != null && mount.IsMounted)
            {
                _mount = mount;
                if (_source.sprite != null) _feet = _source.sprite.bounds.min.y * Mathf.Abs(_source.transform.lossyScale.y);
                Height = 0f;
                LateUpdate();
                return;
            }
            _copy = new GameObject("AirborneSprite").AddComponent<SpriteRenderer>();
            _copy.transform.SetParent(_source.transform, false);
            _source.enabled = false;
            Height = 0f;
            LateUpdate();
        }

        public void End()
        {
            if (_mount != null) _mount.Lift = 0f;
            bool wasMounted = _mount != null;
            _mount = null;
            if (_copy != null) Destroy(_copy.gameObject);
            if (_shadow != null) Destroy(_shadow.gameObject);
            _copy = null;
            _shadow = null;
            if (_source != null && !wasMounted) _source.enabled = true; // a rider's own sprite stays hidden
            if (_depth != null) _depth.Airborne = false;
            Height = 0f;
        }

        private void OnDisable() => End();

        private void LateUpdate()
        {
            if (_mount != null && _source != null)
            {
                float scale = Mathf.Max(0.01f, Mathf.Abs(_source.transform.lossyScale.y));
                _mount.Lift = Height / scale;
                float w = _mount.MountRenderer != null && _mount.MountRenderer.sprite != null ? _mount.MountRenderer.sprite.bounds.size.x * Mathf.Abs(_source.transform.lossyScale.x) : 1f;
                float s = Mathf.Clamp01(1f - Height * 0.25f);
                float px = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x)), py = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
                _shadow.transform.localPosition = new Vector3(0f, _feet / py + 0.05f, 0f);
                _shadow.transform.localScale = new Vector3(w * 0.7f * s / px, w * 0.2f * s / py, 1f);
                _shadow.sortingOrder = _source.sortingOrder - 1;
                return;
            }
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
