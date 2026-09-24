using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Inventory
{
    /// <summary>
    /// Persistent inventory (EPIC 17 Main Menu "Inventory", Village UI "Weapon inventory"): choose the
    /// weapon for the next run and equip up to <see cref="InventoryService.EquipmentSlots"/> equipment
    /// pieces found in battles, chests or the Market. Works in both the Main Menu and Village scenes.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class InventoryScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Inventory;
        protected override string Title => "Inventory";

        protected override void Populate(RectTransform content)
        {
            var inv = InventoryService.Data;
            int slots = InventoryService.EquipmentSlots;
            InfoText.text = $"Equipment slots: {inv.EquippedEquipmentIds.Count}/{slots} (the Castle adds more). " +
                            "Upgrade weapons at the Forge.";

            UIBuilder.SectionHeader(content, "Weapons");
            foreach (var entry in inv.Weapons)
            {
                if (entry == null) continue;
                var weapon = InventoryService.GetWeapon(entry.Id);
                string name = weapon != null ? DefinitionNames.Of(weapon) : DefinitionNames.Prettify(entry.Id);
                string tier = ForgeService.TierName(inv.GetWeaponTier(entry.Id));
                string title = $"{(string.IsNullOrEmpty(tier) ? string.Empty : tier + " ")}{name}   Lv {Mathf.Max(1, entry.Level)}";
                string body = weapon != null
                    ? $"Damage {weapon.GetDamage(Mathf.Max(1, entry.Level)):0.#} · {weapon.GetAttacksPerSecond(Mathf.Max(1, entry.Level)):0.##}/s"
                    : "Missing definition";
                Color accent = weapon != null ? RarityColors.For(weapon.Rarity) : UITheme.Text;

                var actions = UIBuilder.ActionCard(content, title, body, out _, out _, accent);
                if (inv.EquippedWeaponId == entry.Id)
                    UIBuilder.Text(actions.transform, "Equipped", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                else if (weapon != null)
                    UIBuilder.SmallButton(actions.transform, "Equip", () =>
                    {
                        Sfx.Play(AudioCueIds.UiClick);
                        InventoryService.EquipWeapon(entry.Id);
                        Refresh();
                    }, UITheme.ButtonSecondary, 180f);
            }

            UIBuilder.SectionHeader(content, "Equipment");
            if (inv.Equipment.Count == 0)
            {
                UIBuilder.Text(content, "No equipment yet — enemies, chests and the Market drop it.", UITheme.BodySize, TextAlignmentOptions.Left, UITheme.TextMuted);
                return;
            }

            foreach (var stack in inv.Equipment)
            {
                if (stack == null || stack.Count <= 0) continue;
                AddEquipmentCard(content, inv, stack);
            }
        }

        private void AddEquipmentCard(RectTransform content, InventorySaveData inv, OwnedEquipment stack)
        {
            EquipmentDefinition def = InventoryService.GetEquipment(stack.Id);
            string name = def != null ? DefinitionNames.Of(def) : DefinitionNames.Prettify(stack.Id);
            string title = stack.Count > 1 ? $"{name}  ×{stack.Count}" : name;
            string body = def != null ? (string.IsNullOrEmpty(def.Description) ? $"{def.Rarity}" : def.Description) : "Missing definition";
            Color accent = def != null ? RarityColors.For(def.Rarity) : UITheme.Text;

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, accent);
            bool equipped = inv.IsEquipmentEquipped(stack.Id);
            UIBuilder.SmallButton(actions.transform, equipped ? "Unequip" : "Equip", () =>
            {
                var result = equipped ? InventoryService.UnequipEquipment(stack.Id) : InventoryService.EquipEquipment(stack.Id);
                if (result == EquipResult.Ok) Sfx.Play(AudioCueIds.UiClick);
                else
                {
                    Sfx.Play(AudioCueIds.UiError);
                    Toast(InventoryService.DescribeEquipResult(result));
                }
                Refresh();
            }, equipped ? UITheme.ButtonSecondary : (Color?)null, 200f);
        }
    }
}
