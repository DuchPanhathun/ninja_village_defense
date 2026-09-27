using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Nine-Tailed Fox mechanics:
    ///  - TELEPORT: vanishes and reappears at a random position near the player.
    ///  - ILLUSION CLONE: spawns decoy clones that rush the player and explode on contact.
    /// </summary>
    public class NineTailedFoxBoss : BossController
    {
        [Header("Teleport")]
        [SerializeField] private float teleportCooldown = 5f;
        [SerializeField] private float teleportMinRange = 3f;
        [SerializeField] private float teleportMaxRange = 6f;

        [Header("Illusion Clones")]
        [SerializeField] private float cloneCooldown = 8f;
        [SerializeField] private int clonesPerWave = 2;
        [SerializeField] private float cloneMoveSpeed = 4.5f;
        [SerializeField] private float cloneExplosionRadius = 1.5f;
        [SerializeField] private float cloneExplosionDamage = 20f;
        [SerializeField] private LayerMask playerMask;

        private float _teleportReadyAt;
        private float _cloneReadyAt;
        private bool _busy;

        protected override void TickBehavior()
        {
            if (_busy) return;

            if (Time.time >= _cloneReadyAt)
                StartCoroutine(SpawnClonesRoutine());
            else if (Time.time >= _teleportReadyAt)
                StartCoroutine(TeleportRoutine());
            else
                base.TickBehavior();
        }

        protected override void OnPhaseStarted(int phase)
        {
            teleportCooldown *= 0.7f;
            cloneCooldown *= 0.75f;
            clonesPerWave += 1;
        }

        private IEnumerator TeleportRoutine()
        {
            _busy = true;
            Body.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(0.25f);

            if (PlayerTransform != null)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                float dist = Random.Range(teleportMinRange, teleportMaxRange);
                transform.position = (Vector2)PlayerTransform.position + dir * dist;
            }

            _teleportReadyAt = Time.time + teleportCooldown;
            _busy = false;
        }

        private IEnumerator SpawnClonesRoutine()
        {
            _busy = true;
            Body.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(0.4f);

            for (int i = 0; i < clonesPerWave; i++)
            {
                Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 1.5f;
                var cloneGo = new GameObject("IllusionClone");
                cloneGo.transform.position = spawnPos;
                var clone = cloneGo.AddComponent<IllusionClone>();
                clone.Initialize(PlayerTransform, cloneMoveSpeed, cloneExplosionRadius, cloneExplosionDamage, playerMask, gameObject);
            }

            _cloneReadyAt = Time.time + cloneCooldown;
            _busy = false;
        }
    }

}
