using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Progression
{
    /// <summary>
    /// Tracks player level and XP. Every 30 seconds' worth of kills (per the design
    /// doc's pacing) should roughly produce one level-up given reasonable reward
    /// tuning on EnemyDefinition/WaveDefinition assets.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private int baseXpToLevel = 10;
        [SerializeField] private float xpGrowthPerLevel = 1.15f;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int XpToNextLevel { get; private set; }

        private void Awake()
        {
            XpToNextLevel = baseXpToLevel;
        }

        private void OnEnable() => EventBus<XpGainedEvent>.Subscribe(OnXpGained);
        private void OnDisable() => EventBus<XpGainedEvent>.Unsubscribe(OnXpGained);

        private void OnXpGained(XpGainedEvent evt)
        {
            CurrentXp += evt.Amount;

            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                CurrentLevel++;
                XpToNextLevel = Mathf.RoundToInt(baseXpToLevel * Mathf.Pow(xpGrowthPerLevel, CurrentLevel - 1));
                EventBus<LevelUpEvent>.Raise(new LevelUpEvent(CurrentLevel));
            }
        }

        /// <summary>Call when starting a new run so a previous run's level doesn't carry over.</summary>
        public void ResetForNewRun()
        {
            CurrentLevel = 1;
            CurrentXp = 0;
            XpToNextLevel = baseXpToLevel;
        }
    }
}
