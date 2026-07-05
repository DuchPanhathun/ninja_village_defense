using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>Top-of-screen boss bar. Appears on BossSpawnedEvent, hides when the boss dies.</summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text bossNameText;

        private Health _bossHealth;

        private void Awake()
        {
            // Subscribe before disabling barRoot below — if this script lives on
            // barRoot itself, OnEnable would never fire once it's inactive.
            EventBus<BossSpawnedEvent>.Subscribe(OnBossSpawned);

            if (barRoot != null)
                barRoot.SetActive(false);
        }

        private void OnDestroy() => EventBus<BossSpawnedEvent>.Unsubscribe(OnBossSpawned);

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            _bossHealth = evt.Health;
            if (bossNameText != null)
                bossNameText.text = evt.Definition.DisplayName;
            barRoot.SetActive(true);
        }

        private void Update()
        {
            if (_bossHealth == null || !barRoot.activeSelf) return;

            if (!_bossHealth.IsAlive)
            {
                barRoot.SetActive(false);
                _bossHealth = null;
                return;
            }

            if (fillImage != null)
                fillImage.fillAmount = _bossHealth.CurrentHealth / _bossHealth.MaxHealth;
        }
    }
}
