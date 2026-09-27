using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Vfx
{
    /// <summary>
    /// Turns combat events into effects so no gameplay script needs VFX code: hit sparks on every
    /// damage number, a puff on each kill, a big blast on boss death, a burst on level-up, and a ring
    /// for every area-damage call (explosions, shockwaves, ultimates). Weapon-specific visuals
    /// (katana slash, lightning bolt) are spawned at their call sites.
    /// </summary>
    public static class VfxDirector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            EventBus<EntityDamagedEvent>.UnsubscribePersistent(OnDamaged);
            EventBus<EnemyKilledEvent>.UnsubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<LevelUpEvent>.UnsubscribePersistent(OnLevelUp);
            AreaDamage.AreaHit -= OnAreaHit;

            EventBus<EntityDamagedEvent>.SubscribePersistent(OnDamaged);
            EventBus<EnemyKilledEvent>.SubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<LevelUpEvent>.SubscribePersistent(OnLevelUp);
            AreaDamage.AreaHit += OnAreaHit;
        }

        private static void OnDamaged(EntityDamagedEvent evt) => Vfx.HitSpark(evt.Position, evt.IsCritical);

        private static void OnEnemyKilled(EnemyKilledEvent evt) => Vfx.DeathPuff(evt.Position, new Color(0.55f, 0.5f, 0.6f));

        private static void OnBossDefeated(BossDefeatedEvent evt)
        {
            Vfx.Explosion(evt.Position, 4f, Vfx.GoldColor);
            Vfx.Burst(evt.Position, Color.white, 7f, 0.8f);
        }

        private static void OnLevelUp(LevelUpEvent evt)
        {
            if (PlayerReference.Instance != null)
                Vfx.Burst(PlayerReference.Instance.PlayerTransform.position, Vfx.GoldColor, 4f, 0.6f);
        }

        private static void OnAreaHit(Vector2 position, float radius) => Vfx.Explosion(position, radius);
    }
}
