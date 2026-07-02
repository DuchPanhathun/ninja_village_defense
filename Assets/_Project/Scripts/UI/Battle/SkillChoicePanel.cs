using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The "LEVEL UP! Choose ONE" screen. Pauses the game while open. Queues
    /// multiple pending level-ups so back-to-back levels each get a pick.
    /// </summary>
    public class SkillChoicePanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [Header("One entry per choice card (3)")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] nameTexts;
        [SerializeField] private TMP_Text[] descriptionTexts;

        private readonly Queue<SkillDefinition[]> _pendingChoices = new();
        private SkillDefinition[] _currentChoices;
        private SkillManager _skillManager;
        private bool _isShowing;

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i; // capture per-button index
                choiceButtons[i].onClick.AddListener(() => Pick(index));
            }
        }

        private void OnEnable() => EventBus<SkillChoicesReadyEvent>.Subscribe(OnChoicesReady);
        private void OnDisable() => EventBus<SkillChoicesReadyEvent>.Unsubscribe(OnChoicesReady);

        private void OnChoicesReady(SkillChoicesReadyEvent evt)
        {
            _pendingChoices.Enqueue(evt.Choices);
            if (!_isShowing)
                ShowNext();
        }

        private void ShowNext()
        {
            if (_pendingChoices.Count == 0)
            {
                Hide();
                return;
            }

            _currentChoices = _pendingChoices.Dequeue();
            _isShowing = true;
            Time.timeScale = 0f;
            panelRoot.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool hasChoice = i < _currentChoices.Length;
                choiceButtons[i].gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;

                if (i < nameTexts.Length && nameTexts[i] != null)
                    nameTexts[i].text = _currentChoices[i].DisplayName;
                if (i < descriptionTexts.Length && descriptionTexts[i] != null)
                    descriptionTexts[i].text = _currentChoices[i].Description;
            }
        }

        private void Pick(int index)
        {
            if (_currentChoices == null || index >= _currentChoices.Length) return;

            if (_skillManager == null && PlayerReference.Instance != null)
                _skillManager = PlayerReference.Instance.GetComponent<SkillManager>();

            _skillManager?.SelectSkill(_currentChoices[index]);
            _currentChoices = null;
            ShowNext(); // next queued level-up, or hide + unpause
        }

        private void Hide()
        {
            _isShowing = false;
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
