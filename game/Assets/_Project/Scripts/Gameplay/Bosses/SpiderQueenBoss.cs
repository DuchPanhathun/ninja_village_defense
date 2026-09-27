using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Spider Queen mechanics:
    ///  - WEB ZONE: drops slow-zone triggers that linger on the ground.
    ///  - SPIDER SPAWN: periodically spawns spiderling enemies from the pool.
    /// </summary>
    public class SpiderQueenBoss : BossController
    {
        [Header("Web Attack")]
        [SerializeField] private float webCooldown = 7f;
        [SerializeField] private int webCount = 3;
        [SerializeField] private float webRadius = 1.4f;
        [SerializeField] private float webSlowMultiplier = 0.35f;
        [SerializeField] private float webDuration = 6f;
        [SerializeField] private float webSpreadRadius = 4f;

        [Header("Spiderling Spawn")]
        [SerializeField] private GameObject spiderlingPrefab;
        [SerializeField] private float spawnCooldown = 10f;
        [SerializeField] private int spiderlingsPerWave = 3;
        [SerializeField] private float spawnRadius = 2f;

        [Header("Targets")]
        [SerializeField] private LayerMask playerMask;

        private float _webReadyAt;
        private float _spawnReadyAt;
        private bool _busy;

        protected override void TickBehavior()
        {
            if (_busy) return;

            if (Time.time >= _spawnReadyAt)
            {
                StartCoroutine(SpiderSpawnRoutine());
            }
            else if (Time.time >= _webReadyAt)
            {
                StartCoroutine(WebAttackRoutine());
            }
            else
            {
                base.TickBehavior();
            }
        }

        protected override void OnPhaseStarted(int phase)
        {
            webCooldown *= 0.75f;
            spawnCooldown *= 0.75f;
            spiderlingsPerWave += 1;
        }

        private IEnumerator WebAttackRoutine()
        {
            _busy = true;
            Body.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(0.4f);

            for (int i = 0; i < webCount; i++)
            {
                Vector2 dropPos = (Vector2)PlayerTransform.position + Random.insideUnitCircle * webSpreadRadius;
                DropWebZone(dropPos);
            }

            _webReadyAt = Time.time + webCooldown;
            _busy = false;
        }

        private void DropWebZone(Vector2 center)
        {
            var go = new GameObject("WebZone");
            go.transform.position = center;
            var zone = go.AddComponent<WebSlowZone>();
            zone.Initialize(webRadius, webSlowMultiplier, webDuration, playerMask);
        }

        private IEnumerator SpiderSpawnRoutine()
        {
            _busy = true;
            yield return new WaitForSeconds(0.5f);

            if (spiderlingPrefab != null)
            {
                for (int i = 0; i < spiderlingsPerWave; i++)
                {
                    Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
                    Instantiate(spiderlingPrefab, (Vector2)transform.position + offset, Quaternion.identity);
                }
            }

            _spawnReadyAt = Time.time + spawnCooldown;
            _busy = false;
        }
    }

}
