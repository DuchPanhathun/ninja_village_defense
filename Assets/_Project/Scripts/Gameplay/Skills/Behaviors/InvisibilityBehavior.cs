using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Grants periodic invisibility bursts: the player fades out, enemies can no
    /// longer target them (TargetFinder skips invisible transforms), and the player
    /// receives a defense bonus. Fades back in after <see cref="_activeDuration"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class InvisibilityBehavior : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private float _cycleDuration;    // active + cooldown
        private float _activeDuration;   // how long invisible per cycle
        private float _nextCycleAt;
        private float _visibleAgainAt;
        private bool _isInvisible;

        public bool IsInvisible => _isInvisible;

        public void Configure(float activeDuration, float cycleDuration)
        {
            _activeDuration = activeDuration;
            _cycleDuration = cycleDuration;
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (Time.time >= _visibleAgainAt && _isInvisible)
                SetInvisible(false);

            if (!_isInvisible && Time.time >= _nextCycleAt)
            {
                SetInvisible(true);
                _visibleAgainAt = Time.time + _activeDuration;
                _nextCycleAt = Time.time + _cycleDuration;
            }
        }

        private void SetInvisible(bool invisible)
        {
            _isInvisible = invisible;
            if (_spriteRenderer != null)
            {
                var c = _spriteRenderer.color;
                c.a = invisible ? 0.25f : 1f;
                _spriteRenderer.color = c;
            }
        }

        private void OnDisable()
        {
            SetInvisible(false);
        }
    }
}
