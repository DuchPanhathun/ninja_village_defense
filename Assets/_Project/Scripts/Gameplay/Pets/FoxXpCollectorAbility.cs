using System.Collections.Generic;
using NinjaVillage.Gameplay.Progression;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Fox — "Collects XP". Every scan it finds XP orbs within <c>Range</c> of the player and
    /// drags them to the player at <c>Power</c> units/second; each orb then collects itself through
    /// its own pickup check, so <see cref="XpOrb"/> needs no changes. The fox dashes out toward the
    /// farthest orb it is pulling for readability. <c>Cooldown</c> = scan interval (0.2–1s).
    /// </summary>
    public class FoxXpCollectorAbility : PetAbility
    {
        [Tooltip("How much faster than its follow speed the fox dashes toward orbs.")]
        [SerializeField] private float dashSpeedMultiplier = 1.5f;

        private readonly List<XpOrb> _tracked = new();
        private float _scanTimer;

        protected override void OnInitialized()
        {
            _scanTimer = 0f;
            _tracked.Clear();
        }

        private void Update()
        {
            if (!IsReady) return;
            var owner = Owner;
            if (owner == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 ownerPosition = owner.position;

            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = Mathf.Clamp(Stats.Cooldown, 0.2f, 1f);
                Scan(ownerPosition);
            }

            float step = Stats.Power * dt;
            XpOrb farthest = null;
            float farthestSqr = 0f;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                var orb = _tracked[i];
                if (orb == null)
                {
                    _tracked.RemoveAt(i);
                    continue;
                }

                Transform orbTransform = orb.transform;
                Vector2 next = Vector2.MoveTowards(orbTransform.position, ownerPosition, step);
                orbTransform.position = new Vector3(next.x, next.y, orbTransform.position.z);

                float sqr = (next - ownerPosition).sqrMagnitude;
                if (sqr > farthestSqr)
                {
                    farthestSqr = sqr;
                    farthest = orb;
                }
            }

            if (farthest != null && farthestSqr > 1f)
                Controller.SetMoveTarget(farthest.transform.position, dashSpeedMultiplier);
            else if (Controller.HasMoveTarget)
                Controller.ClearMoveTarget();
        }

        private void Scan(Vector2 ownerPosition)
        {
            _tracked.Clear();
            // Allocates one array per scan (a few times a second, not per frame). XpOrb has no
            // registry and is outside this feature, so a scene query is the least invasive option.
            var orbs = Object.FindObjectsByType<XpOrb>(FindObjectsSortMode.None);
            float rangeSqr = Stats.Range * Stats.Range;

            for (int i = 0; i < orbs.Length; i++)
            {
                var orb = orbs[i];
                if (orb == null) continue;
                if (((Vector2)orb.transform.position - ownerPosition).sqrMagnitude <= rangeSqr)
                    _tracked.Add(orb);
            }
        }

        private void OnDisable()
        {
            _tracked.Clear();
        }
    }
}
