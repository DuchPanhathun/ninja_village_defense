using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Shadow Army evolution: shadow samurai orbit the player; on their own staggered timers each one
    /// slashes a katana arc toward the nearest demon in reach.
    /// </summary>
    public class ShadowArmyBehavior : MonoBehaviour
    {
        private const float OrbitRadius = 2f;

        private readonly List<Transform> _soldiers = new();
        private readonly List<float> _nextSlash = new();
        private float _damage, _interval, _reach;
        private float _angle;
        private LayerMask _enemyMask;

        public void Configure(int soldiers, float damage, float attackInterval, float reach)
        {
            _damage = damage;
            _interval = attackInterval;
            _reach = reach;
            if (TryGetComponent<AutoAttackController>(out var attack)) _enemyMask = attack.EnemyMask;

            while (_soldiers.Count < soldiers)
            {
                var go = new GameObject($"ShadowSamurai_{_soldiers.Count}");
                GeneratedSprites.CreateRenderer(go.transform, "Body", GeneratedSprites.Circle, new Color(0.15f, 0.05f, 0.25f, 0.85f), 26,
                    Vector2.zero, new Vector2(0.65f, 0.85f));
                GeneratedSprites.CreateRenderer(go.transform, "Aura", GeneratedSprites.Glow, new Color(0.7f, 0.2f, 0.9f, 0.45f), 25,
                    Vector2.zero, new Vector2(1.5f, 1.5f));
                _soldiers.Add(go.transform);
                _nextSlash.Add(Time.time + _interval * _soldiers.Count / soldiers); // staggered
            }
        }

        private void Update()
        {
            _angle += Time.deltaTime * 45f;
            for (int i = 0; i < _soldiers.Count; i++)
            {
                var soldier = _soldiers[i];
                if (soldier == null) continue;
                float a = (_angle + i * 360f / _soldiers.Count) * Mathf.Deg2Rad;
                soldier.position = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * OrbitRadius;

                if (Time.time < _nextSlash[i]) continue;
                var target = TargetFinder.FindNearest(soldier.position, _reach, _enemyMask);
                if (target == null) continue;

                _nextSlash[i] = Time.time + _interval;
                Vector2 facing = ((Vector2)target.position - (Vector2)soldier.position).normalized;
                MeleeArc.DamageArc(soldier.position, facing, _reach, 120f, _enemyMask, _damage, 3f, false, gameObject);
                NinjaVillage.Gameplay.Vfx.Vfx.Slash((Vector2)soldier.position + facing * (_reach * 0.35f), facing, _reach * 0.6f,
                    new Color(0.75f, 0.4f, 1f));
                Sfx.PlayAt(AudioCueIds.KatanaSlash, soldier.position, 0.5f);
            }
        }

        private void OnDestroy()
        {
            foreach (var soldier in _soldiers)
                if (soldier != null) Destroy(soldier.gameObject);
        }
    }
}
