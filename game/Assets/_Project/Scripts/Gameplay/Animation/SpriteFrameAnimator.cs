using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>
    /// Plays a <see cref="CharacterSpriteSet"/> on a SpriteRenderer: idle or move depending on how fast
    /// the object is moving, attack when <see cref="PlayAttack"/> is called (the
    /// <see cref="ProceduralSpriteAnimator"/> forwards its Punch), hurt and death from the Health events.
    /// Only swaps sprites: position belongs to the Rigidbody, scale to the controllers and the
    /// procedural animator (which still adds the hit squash and death fade on top).
    /// </summary>
    [DisallowMultipleComponent]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private CharacterSpriteSet spriteSet;
        [Tooltip("Found in children if empty.")]
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private float movingSpeedThreshold = 0.15f;

        private Health _health;
        private CharacterAnim _anim = CharacterAnim.Idle;
        private float _time;
        private bool _oneShot;
        private bool _dead;
        private Vector3 _lastPosition;

        public CharacterSpriteSet SpriteSet => spriteSet;
        public bool HasSet => spriteSet != null;

        private void Awake()
        {
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            _health = GetComponentInParent<Health>();
            _lastPosition = transform.position;
            ShowFirstFrame();
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.OnDamaged += OnDamaged;
            _health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnDamaged -= OnDamaged;
            _health.OnDeath -= OnDeath;
        }

        public bool Has(CharacterAnim anim) => spriteSet != null && spriteSet.Has(anim);

        /// <summary>Switches to another character's frames (hero choice, skins, pets).</summary>
        public void SetSpriteSet(CharacterSpriteSet set)
        {
            spriteSet = set;
            _oneShot = false;
            _anim = CharacterAnim.Idle;
            _time = 0f;
            ShowFirstFrame();
        }

        public void PlayAttack() => PlayOnce(CharacterAnim.Attack);

        /// <summary>Back to idle after a revive.</summary>
        public void ResetState()
        {
            _dead = false;
            _oneShot = false;
            _anim = CharacterAnim.Idle;
            _time = 0f;
            ShowFirstFrame();
        }

        private void OnDamaged(float amount, float current, float max)
        {
            if (current > 0f) PlayOnce(CharacterAnim.Hurt);
        }

        private void OnDeath(Health health)
        {
            if (!Has(CharacterAnim.Death)) return;
            _dead = true;
            _oneShot = false;
            _anim = CharacterAnim.Death;
            _time = 0f;
        }

        private void PlayOnce(CharacterAnim anim)
        {
            if (_dead || !Has(anim)) return;
            _oneShot = true;
            _anim = anim;
            _time = 0f;
        }

        private void ShowFirstFrame()
        {
            if (target != null && spriteSet != null && spriteSet.DefaultSprite != null)
                target.sprite = spriteSet.DefaultSprite;
        }

        private void LateUpdate()
        {
            if (spriteSet == null || target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            Vector3 position = transform.position;
            float speed = (position - _lastPosition).magnitude / dt;
            _lastPosition = position;
            _time += dt;

            if (!_dead && !_oneShot)
            {
                var wanted = speed > movingSpeedThreshold && Has(CharacterAnim.Move) ? CharacterAnim.Move : CharacterAnim.Idle;
                if (wanted != _anim)
                {
                    _anim = wanted;
                    _time = 0f;
                }
            }

            var frames = spriteSet.Frames(_anim);
            if (frames == null || frames.Length == 0) frames = spriteSet.Frames(CharacterAnim.Idle);
            if (frames == null || frames.Length == 0) return;

            int index = Mathf.FloorToInt(_time * spriteSet.Fps(_anim));
            if (_dead)
            {
                index = Mathf.Min(index, frames.Length - 1); // hold the last death frame
            }
            else if (_oneShot && index >= frames.Length)
            {
                _oneShot = false;
                _anim = CharacterAnim.Idle;
                _time = 0f;
                frames = spriteSet.Frames(CharacterAnim.Idle);
                if (frames == null || frames.Length == 0) return;
                index = 0;
            }
            else
            {
                index %= frames.Length;
            }

            var sprite = frames[index];
            if (sprite != null && target.sprite != sprite) target.sprite = sprite;
        }
    }
}
