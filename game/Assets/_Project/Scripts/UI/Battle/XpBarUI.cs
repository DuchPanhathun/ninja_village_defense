using NinjaVillage.Gameplay.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>HUD XP bar + level label, driven by the scene's LevelSystem.</summary>
    public class XpBarUI : MonoBehaviour
    {
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text levelText;

        private void Update()
        {
            if (levelSystem == null) return;

            if (fillImage != null)
                fillImage.fillAmount = levelSystem.XpToNextLevel > 0
                    ? (float)levelSystem.CurrentXp / levelSystem.XpToNextLevel
                    : 0f;

            if (levelText != null)
                levelText.text = $"Lv {levelSystem.CurrentLevel}";
        }
    }
}
