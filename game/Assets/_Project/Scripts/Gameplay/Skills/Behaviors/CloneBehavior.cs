using System.Collections.Generic;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Ultimates;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Keeps <see cref="CloneSkill"/>'s shadow clones orbiting the player; each clone attacks on its
    /// own with a <see cref="ShadowCloneAttacker"/> (shared with the Shadow Clone Army ultimate).
    /// </summary>
    public class CloneBehavior : MonoBehaviour
    {
        private readonly List<GameObject> _clones = new();
        private float _damageFraction;
        private float _orbitRadius;
        private float _angle;
        private PlayerStats _stats;
        private LayerMask _enemyMask;

        public int CloneCount => _clones.Count;

        public void Configure(int count, float damageFraction, float orbitRadius)
        {
            _stats = GetComponent<PlayerStats>();
            _enemyMask = TryGetComponent<AutoAttackController>(out var attack) ? attack.EnemyMask : default;
            _damageFraction = damageFraction;
            _orbitRadius = orbitRadius;

            while (_clones.Count < count)
            {
                var clone = new GameObject($"ShadowClone_{_clones.Count}");
                clone.transform.position = transform.position;
                clone.AddComponent<ShadowCloneAttacker>().Initialize(transform, _stats, _damageFraction, _enemyMask);
                _clones.Add(clone);
            }
            foreach (var clone in _clones)
                if (clone != null && clone.TryGetComponent<ShadowCloneAttacker>(out var attacker))
                    attacker.Initialize(transform, _stats, _damageFraction, _enemyMask);
        }

        private void LateUpdate()
        {
            if (_clones.Count == 0) return;
            _angle += Time.deltaTime * 60f;
            for (int i = 0; i < _clones.Count; i++)
            {
                if (_clones[i] == null) continue;
                float a = (_angle + i * 360f / _clones.Count) * Mathf.Deg2Rad;
                _clones[i].transform.position = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _orbitRadius;
            }
        }

        private void OnDestroy()
        {
            foreach (var clone in _clones)
                if (clone != null) Destroy(clone);
        }
    }
}
