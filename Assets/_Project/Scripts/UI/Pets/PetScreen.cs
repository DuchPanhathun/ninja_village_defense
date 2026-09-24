using NinjaVillage.Core;
using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Pets;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Pets
{
    /// <summary>
    /// Pet management (EPIC 12, Pet House "Pet management", Village UI "Pet inventory"): unlock and
    /// level companions, choose the one that joins the next run, and buy/equip pet gear. Pet slots and
    /// level caps come from the Pet House, so blocked actions explain what to upgrade.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class PetScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Pets;
        protected override string Title => "Pets";

        /// <summary>Pet whose gear is being chosen; null = normal list.</summary>
        private PetDefinition _equipTarget;

        protected override void OnHidden() => _equipTarget = null;

        protected override void Populate(RectTransform content)
        {
            if (_equipTarget != null)
            {
                PopulateGearPicker(content, _equipTarget);
                return;
            }

            var active = PetService.GetActive();
            InfoText.text = $"Pet House Lv {PetService.PetHouseLevel} — {PetService.OwnedCount}/{PetService.MaxOwnedPets} pets." +
                            $"\nJoins your next run: <b>{(active != null ? active.NameOrId : "nobody")}</b>";

            var pets = PetService.GetSortedPets();
            if (pets.Count == 0)
            {
                UIBuilder.Text(content, "No pets found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            UIBuilder.SectionHeader(content, "Companions");
            foreach (var pet in pets) AddPetCard(content, pet);

            UIBuilder.SectionHeader(content, "Pet Gear Shop");
            foreach (var item in PetService.GetSortedEquipment()) AddShopCard(content, item);
        }

        private void AddPetCard(RectTransform content, PetDefinition pet)
        {
            bool unlocked = PetService.IsUnlocked(pet);
            int level = PetService.GetLevel(pet);
            string title = pet.NameOrId + (pet.IsPremium ? "  [Premium]" : string.Empty) +
                           (unlocked ? $"   Lv {level}/{PetService.GetLevelCap(pet)}" : "   (locked)");

            var bonus = pet.GetOwnerBonus(Mathf.Max(1, level));
            string body = string.IsNullOrEmpty(pet.AbilitySummary) ? pet.Description : pet.AbilitySummary;
            if (!bonus.IsEmpty) body += "\nPassive: " + bonus.Describe();
            var gear = PetService.GetEquippedItem(pet);
            if (unlocked) body += "\nGear: " + (gear != null ? RarityColors.Colorize(gear.NameOrId, gear.Rarity) : "none");

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, pet.PlaceholderColor);

            if (!unlocked)
            {
                var check = PetService.CheckUnlock(pet);
                var unlock = UIBuilder.SmallButton(actions.transform, $"Unlock {new Price(pet.UnlockCurrency, pet.UnlockCost)}",
                    () => Feedback(PetService.TryUnlock(pet), pet));
                if (check != PetActionResult.Success)
                {
                    UIBuilder.SetEnabled(unlock, false);
                    if (check != PetActionResult.NotEnoughCurrency)
                        UIBuilder.Text(actions.transform, PetService.Describe(check, pet), UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Negative);
                }
                return;
            }

            if (PetService.IsActive(pet))
                UIBuilder.SmallButton(actions.transform, "Leave home", () => { PetService.ClearActive(); Refresh(); }, UITheme.ButtonSecondary, 230f);
            else
                UIBuilder.SmallButton(actions.transform, "Take along", () => Feedback(PetService.TrySetActive(pet), pet), UITheme.ButtonSecondary, 230f);

            UIBuilder.SmallButton(actions.transform, "Gear", () =>
            {
                Sfx.Play(AudioCueIds.UiClick);
                _equipTarget = pet;
                Refresh();
            }, UITheme.ButtonSecondary, 150f);

            var check2 = PetService.CheckUpgrade(pet);
            string label = check2 == PetActionResult.MaxLevel ? "Max level" : $"Lv up {PetService.GetUpgradeCost(pet)}";
            var upgrade = UIBuilder.SmallButton(actions.transform, label, () => Feedback(PetService.TryUpgrade(pet), pet), null, 260f);
            if (check2 != PetActionResult.Success) UIBuilder.SetEnabled(upgrade, false);
        }

        private void AddShopCard(RectTransform content, PetEquipmentDefinition item)
        {
            bool owned = PetService.OwnsItem(item);
            string body = item.Bonus.Describe();
            var actions = UIBuilder.ActionCard(content, item.NameOrId, body, out _, out _, RarityColors.For(item.Rarity));
            if (owned)
            {
                UIBuilder.Text(actions.transform, "Owned", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                return;
            }

            var check = PetService.CheckBuyItem(item);
            var buy = UIBuilder.SmallButton(actions.transform, $"Buy {new Price(item.CostCurrency, item.Cost)}",
                () => Feedback(PetService.TryBuyItem(item), null));
            if (check != PetActionResult.Success)
            {
                UIBuilder.SetEnabled(buy, false);
                if (check == PetActionResult.PetHouseLevelTooLow)
                    UIBuilder.SetLabel(buy, $"Pet House Lv {item.RequiredPetHouseLevel}");
            }
        }

        private void PopulateGearPicker(RectTransform content, PetDefinition pet)
        {
            InfoText.text = $"Choose gear for <b>{pet.NameOrId}</b>. Gear moved from another pet is unequipped there.";
            UIBuilder.SmallButton(content, "< Back to pets", () => { _equipTarget = null; Refresh(); }, UITheme.ButtonSecondary, 320f);

            var current = PetService.GetEquippedItem(pet);
            var none = UIBuilder.ActionCard(content, "No gear", string.Empty, out _, out _);
            if (current != null)
                UIBuilder.SmallButton(none.transform, "Unequip", () => { PetService.TryEquip(pet, null); _equipTarget = null; Refresh(); });

            bool any = false;
            foreach (var item in PetService.GetSortedEquipment())
            {
                if (!PetService.OwnsItem(item)) continue;
                any = true;
                var actions = UIBuilder.ActionCard(content, item.NameOrId, item.Bonus.Describe(), out _, out _, RarityColors.For(item.Rarity));
                if (current == item)
                {
                    UIBuilder.Text(actions.transform, "Equipped", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                    continue;
                }
                UIBuilder.SmallButton(actions.transform, "Equip", () =>
                {
                    Feedback(PetService.TryEquip(pet, item), pet);
                    _equipTarget = null;
                    Refresh();
                });
            }
            if (!any) UIBuilder.Text(content, "You don't own any pet gear yet — buy some in the Pet Gear Shop.", UITheme.BodySize, TextAlignmentOptions.Left, UITheme.TextMuted);
        }

        private void Feedback(PetActionResult result, PetDefinition pet)
        {
            if (result != PetActionResult.Success)
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(PetService.Describe(result, pet));
            }
            Refresh();
        }
    }
}
