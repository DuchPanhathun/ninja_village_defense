using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The player's build at a glance, under the HP/XP bars: the weapon, the meals eaten for this run (green
    /// tiles), then every skill picked this run with its level (evolutions get a gold frame). New skills pop in; level-ups update the number.
    /// Built in code; the Battle scene only needs this component on a RectTransform in the HUD canvas.
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        [SerializeField] private float slotSize = 84f;
        [SerializeField] private int columns = 8;

        private readonly Dictionary<SkillDefinition, (RectTransform slot, TextMeshProUGUI level)> _slots = new();
        private RectTransform _grid;
        private SkillManager _skills;
        private AutoAttackController _attack;
        private Image _weaponIcon;
        private Object _shownWeapon;
        private int _shownMeals;

        private void Awake()
        {
            var layout = gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(slotSize, slotSize);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.childAlignment = TextAnchor.UpperLeft;
            _grid = (RectTransform)transform;
        }

        private void OnEnable() => EventBus<SkillLeveledEvent>.Subscribe(OnSkillLeveled);
        private void OnDisable() => EventBus<SkillLeveledEvent>.Unsubscribe(OnSkillLeveled);

        private void Start() => SyncAll();

        private void OnSkillLeveled(SkillLeveledEvent evt) => SyncAll(evt.Skill);

        private void Update()
        {
            // The weapon can be swapped by run-start modifiers after Start.
            if (_attack == null && PlayerReference.Instance != null)
                _attack = PlayerReference.Instance.GetComponent<AutoAttackController>();
            var weapon = _attack != null && _attack.Weapon != null ? _attack.Weapon.Definition : null;
            if (weapon != _shownWeapon)
            {
                _shownWeapon = weapon;
                if (_weaponIcon == null) _weaponIcon = CreateSlot(null, out _, first: true);
                _weaponIcon.sprite = weapon != null ? (weapon.Icon != null ? weapon.Icon : UIIcons.Weapon(weapon.Id)) : null;
                _weaponIcon.enabled = _weaponIcon.sprite != null;
            }

            // Meals are eaten as the run starts (MealRunModifier), after this bar may already exist.
            var meals = KitchenService.MealsThisRun;
            while (_shownMeals < meals.Count)
            {
                var meal = meals[_shownMeals];
                var icon = CreateSlot(null, out _);
                var tile = (RectTransform)icon.transform.parent;
                tile.name = $"Meal_{meal.Id}";
                tile.GetComponent<Image>().color = new Color(0.2f, 0.42f, 0.22f, 0.95f);
                tile.SetSiblingIndex(Mathf.Min((_weaponIcon != null ? 1 : 0) + _shownMeals, _grid.childCount - 1));
                icon.sprite = meal.Icon;
                icon.enabled = meal.Icon != null;
                _shownMeals++;
            }
        }

        private void SyncAll(SkillDefinition justLeveled = null)
        {
            if (_skills == null && PlayerReference.Instance != null)
                _skills = PlayerReference.Instance.GetComponent<SkillManager>();
            if (_skills == null) return;

            foreach (var (skill, level) in _skills.Levels)
            {
                if (skill == null) continue;
                if (!_slots.TryGetValue(skill, out var entry))
                {
                    var icon = CreateSlot(skill, out var levelText);
                    icon.sprite = skill.Icon;
                    icon.enabled = skill.Icon != null;
                    entry = ((RectTransform)icon.transform.parent, levelText);
                    _slots[skill] = entry;
                    if (skill == justLeveled) StartCoroutine(Pop(entry.slot));
                }
                else if (skill == justLeveled)
                {
                    StartCoroutine(Pop(entry.slot));
                }
                entry.level.text = level >= skill.MaxLevel ? "MAX" : level.ToString();
            }
        }

        /// <summary>A dark wood tile with an icon and a small level number; returns the icon Image.</summary>
        private Image CreateSlot(SkillDefinition skill, out TextMeshProUGUI levelText, bool first = false)
        {
            bool evolution = skill != null && skill.IsEvolution;
            var tile = UIBuilder.Image(_grid, skill != null ? $"Skill_{skill.Id}" : "Weapon",
                evolution ? UITheme.Gold : new Color(0.24f, 0.16f, 0.11f, 0.92f));
            UIBuilder.UseWood(tile, "panel_tint");
            tile.raycastTarget = false;
            if (first) tile.transform.SetAsFirstSibling();

            var icon = UIBuilder.Image(tile.transform, "Icon", Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIBuilder.Stretch(icon.rectTransform, slotSize * 0.14f);

            levelText = UIStyle.Label(tile.transform, "", 24f, Color.white, TextAlignmentOptions.BottomRight, 0.25f);
            UIBuilder.Stretch(levelText.rectTransform, 2f);
            if (skill == null) levelText.gameObject.SetActive(false); // the weapon slot has no skill level
            return icon;
        }

        private static System.Collections.IEnumerator Pop(RectTransform slot)
        {
            // Unscaled: skills are picked while the level-up panel has the game paused.
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.35f;
                slot.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            slot.localScale = Vector3.one;
        }
    }
}
