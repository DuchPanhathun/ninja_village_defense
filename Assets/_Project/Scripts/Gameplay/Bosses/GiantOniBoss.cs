using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// The first boss. Signature mechanics from the design doc:
    ///  - CHARGE: telegraphs, then dashes across the map toward the player,
    ///    damaging on contact.
    ///  - GROUND SMASH: when the player is close, slams the ground after a windup,
    ///    creating an expanding shockwave ring that damages everything it passes.
    /// Later phases shorten both cooldowns.
    /// </summary>
    public class GiantOniBoss : BossController
    {
        [Header("Charge Attack")]
        [SerializeField] private float chargeCooldown = 8f;
        [SerializeField] private float chargeWindup = 0.7f;
        [SerializeField] private float chargeSpeed = 12f;
        [SerializeField] private float chargeDuration = 1.2f;
        [SerializeField] private float chargeDamage = 25f;
        [SerializeField] private float chargeHitRadius = 1.2f;

        [Header("Ground Smash")]
        [SerializeField] private float smashCooldown = 6f;
        [SerializeField] private float smashWindup = 0.9f;
        [SerializeField] private float smashTriggerRange = 3f;
        [SerializeField] private float smashDamage = 30f;
        [Tooltip("The shockwave ring expands from 0 to this radius.")]
        [SerializeField] private float shockwaveMaxRadius = 5f;
        [SerializeField] private float shockwaveExpandSpeed = 8f;
        [SerializeField] private float shockwaveDamage = 15f;

        [Header("Targets")]
        [SerializeField] private LayerMask playerMask;

        private float _chargeReadyAt;
        private float _smashReadyAt;
        private bool _busy;

        protected override void TickBehavior()
        {
            if (_busy) return;

            float distance = Vector2.Distance(transform.position, PlayerTransform.position);

            if (Time.time >= _smashReadyAt && distance <= smashTriggerRange)
            {
                StartCoroutine(GroundSmashRoutine());
            }
            else if (Time.time >= _chargeReadyAt && distance > smashTriggerRange)
            {
                StartCoroutine(ChargeRoutine());
            }
            else
            {
                base.TickBehavior(); // default chase + melee
            }
        }

        protected override void OnPhaseStarted(int phase)
        {
            // Each phase: 25% faster signature attacks.
            chargeCooldown *= 0.75f;
            smashCooldown *= 0.75f;
        }

        private IEnumerator ChargeRoutine()
        {
            _busy = true;
            Body.linearVelocity = Vector2.zero;

            // Telegraph: lock direction at windup start so the player can dodge.
            Vector2 direction = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
            yield return new WaitForSeconds(chargeWindup);

            bool hasHit = false;
            float elapsed = 0f;
            while (elapsed < chargeDuration)
            {
                Body.linearVelocity = direction * chargeSpeed;

                if (!hasHit &&
                    Vector2.Distance(transform.position, PlayerTransform.position) <= chargeHitRadius &&
                    PlayerTransform.TryGetComponent<IDamageable>(out var player) && player.IsAlive)
                {
                    player.TakeDamage(new DamageInfo(chargeDamage, false, direction, 8f, gameObject));
                    hasHit = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Body.linearVelocity = Vector2.zero;
            _chargeReadyAt = Time.time + chargeCooldown;
            _busy = false;
        }

        private IEnumerator GroundSmashRoutine()
        {
            _busy = true;
            Body.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(smashWindup);

            // The smash itself: heavy damage right at the impact point.
            AreaDamage.DamageCircle(transform.position, smashTriggerRange * 0.66f, playerMask, smashDamage, 6f, gameObject);
            EventBus<CameraShakeRequestEvent>.Raise(new CameraShakeRequestEvent(0.35f, 0.3f));

            // Expanding shockwave ring: damages the player once when the ring passes them.
            Vector2 center = transform.position;
            float radius = 0f;
            bool shockwaveHit = false;
            float previousRadius = 0f;

            while (radius < shockwaveMaxRadius)
            {
                previousRadius = radius;
                radius += shockwaveExpandSpeed * Time.deltaTime;

                if (!shockwaveHit && PlayerTransform != null)
                {
                    float playerDistance = Vector2.Distance(center, PlayerTransform.position);
                    if (playerDistance > previousRadius && playerDistance <= radius &&
                        PlayerTransform.TryGetComponent<IDamageable>(out var player) && player.IsAlive)
                    {
                        Vector2 knockDir = ((Vector2)PlayerTransform.position - center).normalized;
                        player.TakeDamage(new DamageInfo(shockwaveDamage, false, knockDir, 5f, gameObject));
                        shockwaveHit = true;
                    }
                }

                yield return null;
            }

            _smashReadyAt = Time.time + smashCooldown;
            _busy = false;

            // TODO(VFX): ground crack + expanding ring visual (EPIC 23).
        }
    }
}
