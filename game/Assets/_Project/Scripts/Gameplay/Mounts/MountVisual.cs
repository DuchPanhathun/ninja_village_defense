using NinjaVillage.Systems.Mounts;
using UnityEngine;

namespace NinjaVillage.Gameplay.Mounts
{
    /// <summary>
    /// Seats a character on a mount without touching its physics: the character's own renderer hides and a copy that
    /// mirrors it every frame (sprite, flip, tint — so hit flashes and animations still show) sits on the mount's back,
    /// drawn just behind the mount so the legs disappear into the saddle. The mount gallops while the character moves,
    /// faces the same way, and rises with it when it jumps (<see cref="Lift"/>, set by AirborneVisual).
    /// </summary>
    public class MountVisual : MonoBehaviour
    {
        private SpriteRenderer _source, _rider, _mount;
        private MountDefinition _definition;
        private Vector3 _lastPosition;
        private float _movingUntil;

        public MountDefinition Mount => _definition;
        public bool IsMounted => _definition != null && _mount != null;
        /// <summary>World units the rider and mount are raised (a jump).</summary>
        public float Lift { get; set; }
        public SpriteRenderer MountRenderer => _mount;
        public SpriteRenderer RiderRenderer => _rider;

        /// <summary>Puts the character on <paramref name="definition"/> (null dismounts).</summary>
        public void Ride(MountDefinition definition)
        {
            Dismount();
            if (definition == null || definition.Frames == null || definition.Frames.Length == 0) return;
            _source = GetComponent<SpriteRenderer>();
            if (_source == null) _source = GetComponentInChildren<SpriteRenderer>();
            if (_source == null) return;
            _definition = definition;

            var parent = _source.transform;
            _mount = new GameObject("Mount").AddComponent<SpriteRenderer>();
            _mount.transform.SetParent(parent, false);
            _rider = new GameObject("Rider").AddComponent<SpriteRenderer>();
            _rider.transform.SetParent(parent, false);
            _source.enabled = false;
            _lastPosition = transform.position;
            LateUpdate();
        }

        public void Dismount()
        {
            if (_mount != null) Remove(_mount.gameObject);
            if (_rider != null) Remove(_rider.gameObject);
            _mount = null;
            _rider = null;
            if (_source != null) _source.enabled = true;
            _definition = null;
        }

        private void OnDisable() => Dismount();

        private static void Remove(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go); // edit-mode tests and previews
        }

        private void LateUpdate()
        {
            if (!IsMounted || _source == null) return;
            var sprite = _source.sprite;
            bool flip = _source.flipX;

            // Gallop while moving (a short grace so a stutter doesn't stop it).
            if ((transform.position - _lastPosition).sqrMagnitude > 0.0000025f) _movingUntil = Time.time + 0.12f;
            _lastPosition = transform.position;
            var frames = _definition.Frames;
            int frame = Time.time < _movingUntil && frames.Length > 1 ? (int)(Time.time * _definition.Fps) % frames.Length : 0;
            _mount.sprite = frames[frame];
            _mount.flipX = flip;
            _mount.color = new Color(1f, 1f, 1f, _source.color.a);

            // Mount feet on the character's feet; the rider on its back.
            float feet = sprite != null ? sprite.bounds.min.y : -0.6f;
            float mountHalf = _mount.sprite != null ? _mount.sprite.bounds.extents.y : 0.6f;
            _mount.transform.localPosition = new Vector3(0f, feet + mountHalf + Lift, 0f);
            var offset = _definition.RiderOffset;
            _rider.transform.localPosition = new Vector3(flip ? -offset.x : offset.x, offset.y + Lift, 0f);

            _rider.sprite = sprite;
            _rider.flipX = flip;
            _rider.color = _source.color;
            _rider.sortingOrder = _source.sortingOrder;
            _mount.sortingOrder = _source.sortingOrder + 1;
        }
    }
}
