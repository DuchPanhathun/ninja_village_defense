using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Pet follow AI (EPIC 12). Idle pets orbit the player on evenly spaced slots (so several pets —
    /// e.g. the active pet plus Beast Ninja wolves — never stack on the player or each other) and
    /// trail smoothly behind as the player moves. Abilities take over movement temporarily with
    /// <see cref="SetMoveTarget"/> (Wolf chasing an enemy, Hawk scouting a treasure spot).
    /// Everything runs on scaled time, so pets freeze while the level-up/pause panels are open.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private float followDistance = 1.4f;
        [SerializeField] private float orbitDegreesPerSecond = 25f;
        [SerializeField] private float smoothTime = 0.22f;
        [SerializeField] private float maxSpeed = 7f;
        [Tooltip("Pets further than this from the player snap back next to them (e.g. after a teleport).")]
        [SerializeField] private float leashDistance = 14f;

        [Header("Visual")]
        [Tooltip("Optional. Found in children if empty. A child visual is flipped/bobbed; a sprite on the root just flips.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobFrequency = 3f;

        private static readonly List<PetController> ActivePets = new();

        private Transform _owner;
        private Vector2 _position;
        private Vector2 _velocity;
        private bool _hasMoveTarget;
        private Vector2 _moveTarget;
        private float _speedMultiplier = 1f;
        private float _orbitAngle;
        private Transform _visual;
        private Vector3 _visualBaseLocalPosition;
        private float _bobPhase;

        public Transform Owner => _owner;
        public Vector2 Position => _position;
        public bool HasMoveTarget => _hasMoveTarget;
        public float MaxSpeed => maxSpeed;

        /// <summary>Called by <see cref="PetFactory"/> right after spawning.</summary>
        public void Initialize(Transform owner, float moveSpeed, float orbitDistance)
        {
            _owner = owner;
            maxSpeed = Mathf.Max(0.5f, moveSpeed);
            followDistance = Mathf.Max(0.6f, orbitDistance);
            _position = transform.position;
            _velocity = Vector2.zero;
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            // Bob a child visual only — bobbing the root would disturb ability range checks.
            if (spriteRenderer != null && spriteRenderer.transform != transform)
            {
                _visual = spriteRenderer.transform;
                _visualBaseLocalPosition = _visual.localPosition;
            }
            _position = transform.position;
            _orbitAngle = Random.Range(0f, 360f);
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable() => ActivePets.Add(this);
        private void OnDisable() => ActivePets.Remove(this);

        /// <summary>Move toward a world point instead of orbiting (until <see cref="ClearMoveTarget"/>).</summary>
        public void SetMoveTarget(Vector2 worldPosition, float speedMultiplier = 1f)
        {
            _hasMoveTarget = true;
            _moveTarget = worldPosition;
            _speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        }

        public void ClearMoveTarget()
        {
            _hasMoveTarget = false;
            _speedMultiplier = 1f;
        }

        public bool IsNear(Vector2 worldPosition, float tolerance) =>
            (worldPosition - _position).sqrMagnitude <= tolerance * tolerance;

        /// <summary>Instantly faces left/right toward a point (e.g. before breathing fire).</summary>
        public void FaceToward(Vector2 worldPosition)
        {
            float dx = worldPosition.x - _position.x;
            if (Mathf.Abs(dx) > 0.01f) SetFacingLeft(dx < 0f);
        }

        /// <summary>Flips the child visual (so attached details like a snout flip too) or the sprite itself.</summary>
        private void SetFacingLeft(bool left)
        {
            if (_visual != null)
            {
                var scale = _visual.localScale;
                float x = Mathf.Abs(scale.x) * (left ? -1f : 1f);
                if (!Mathf.Approximately(scale.x, x))
                {
                    scale.x = x;
                    _visual.localScale = scale;
                }
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = left;
            }
        }

        private void Update()
        {
            if (_owner == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            _position = transform.position;
            Vector2 ownerPosition = _owner.position;

            // Lost track of the player (teleport, spawn far away) → snap to a slot next to them.
            if ((_position - ownerPosition).sqrMagnitude > leashDistance * leashDistance)
            {
                _position = ownerPosition + SlotOffset();
                _velocity = Vector2.zero;
                ClearMoveTarget();
            }

            _orbitAngle = Mathf.Repeat(_orbitAngle + orbitDegreesPerSecond * dt, 360f);

            Vector2 desired = _hasMoveTarget ? _moveTarget : ownerPosition + SlotOffset();
            float speed = maxSpeed * (_hasMoveTarget ? _speedMultiplier : 1f);
            // Catch up faster when far behind the orbit slot so pets never trail off-screen.
            if (!_hasMoveTarget && (desired - _position).sqrMagnitude > 16f) speed *= 1.6f;

            _position = Vector2.SmoothDamp(_position, desired, ref _velocity, smoothTime, speed, dt);
            transform.position = new Vector3(_position.x, _position.y, transform.position.z);

            if (Mathf.Abs(_velocity.x) > 0.05f)
                SetFacingLeft(_velocity.x < 0f);

            if (_visual != null && bobAmplitude > 0f)
            {
                _bobPhase += dt * bobFrequency * Mathf.PI * 2f;
                if (_bobPhase > Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;
                _visual.localPosition = _visualBaseLocalPosition + new Vector3(0f, Mathf.Sin(_bobPhase) * bobAmplitude, 0f);
            }
        }

        /// <summary>This pet's orbit slot: evenly spaced around the player, slowly rotating.</summary>
        private Vector2 SlotOffset()
        {
            int count = ActivePets.Count;
            int index = ActivePets.IndexOf(this);

            // All pets share the first pet's rotation so the ring stays evenly spaced.
            float baseAngle = _orbitAngle;
            if (index > 0 && ActivePets[0] != null) baseAngle = ActivePets[0]._orbitAngle;
            float slotAngle = baseAngle + (count > 1 && index > 0 ? 360f * index / count : 0f);

            float rad = slotAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * followDistance;
        }
    }
}
