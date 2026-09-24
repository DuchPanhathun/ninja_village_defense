using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Forge (EPIC 11 "Weapon crafting", "Weapon upgrading"): craft weapons you don't own, level up
    /// owned weapons (capped by the Forge level), reforge them into better tiers
    /// (Iron → Steel → Golden → Legendary) using spare equipment, and equip one for the next run.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class ForgeScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Forge;
        protected override string Title => "Forge";

        protected override void Populate(RectTransform content)
        {
            InfoText.text = ForgeService.ForgeLevel > 0
                ? $"Forge Lv {ForgeService.ForgeLevel} — weapons can reach Lv {ForgeService.MaxWeaponLevel}/{RuntimeWeapon.MaxLevel}."
                : "Build the Forge in the village to craft and upgrade weapons.";

            var catalog = InventoryService.Weapons;
            if (catalog == null || catalog.All.Count == 0)
            {
                UIBuilder.Text(content, "No weapons found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            var inv = InventoryService.Data;
            UIBuilder.SectionHeader(content, "Your weapons");
            foreach (var weapon in catalog.All)
                if (weapon != null && inv.OwnsWeapon(weapon.Id)) AddOwnedCard(content, weapon);

            bool header = false;
            foreach (var weapon in catalog.All)
            {
                if (weapon == null || inv.OwnsWeapon(weapon.Id)) continue;
                if (!header) { UIBuilder.SectionHeader(content, "Craft new weapons"); header = true; }
                AddCraftCard(content, weapon);
            }
        }

        private void AddOwnedCard(RectTransform content, WeaponDefinition weapon)
        {
            int level = ForgeService.GetLevel(weapon.Id);
            int tier = ForgeService.GetTier(weapon.Id);
            string tierName = ForgeService.TierName(tier);
            bool equipped = InventoryService.Data.EquippedWeaponId == weapon.Id;

            string title = $"{(string.IsNullOrEmpty(tierName) ? string.Empty : tierName + " ")}{DefinitionNames.Of(weapon)}   Lv {level}";
            string body = $"Damage {weapon.GetDamage(level):0.#} · {weapon.GetAttacksPerSecond(level):0.##}/s · Range {weapon.Range:0.#}";
            float tierBonus = ForgeService.TierAttackBonus(weapon.Id);
            if (tierBonus > 0f) body += $"\nTier bonus: +{tierBonus * 100f:0}% attack";
            var nextTier = ForgeService.NextTier(weapon.Id);
            if (nextTier != null)
                body += $"\nNext tier <b>{nextTier.Name}</b>: {nextTier.CoinCost} coins + {nextTier.MaterialCount} spare {nextTier.MaterialMinRarity}+ gear, needs Lv {nextTier.RequiredWeaponLevel} & Forge Lv {nextTier.RequiredForgeLevel}";

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, RarityColors.For(weapon.Rarity));

            if (equipped)
                UIBuilder.Text(actions.transform, "Equipped", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
            else
                UIBuilder.SmallButton(actions.transform, "Equip", () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    InventoryService.EquipWeapon(weapon.Id);
                    Refresh();
                }, UITheme.ButtonSecondary, 170f);

            var upgradeBlocker = ForgeService.CheckUpgrade(weapon);
            if (upgradeBlocker != ForgeBlocker.MaxLevel)
            {
                var upgrade = UIBuilder.SmallButton(actions.transform, $"Lv up {ForgeService.UpgradePrice(weapon)}",
                    () => Feedback(ForgeService.TryUpgrade(weapon, out var b), b, weapon), null, 300f);
                if (upgradeBlocker != ForgeBlocker.None) UIBuilder.SetEnabled(upgrade, false);
            }

            if (nextTier != null)
            {
                var reforgeBlocker = ForgeService.CheckReforge(weapon);
                var reforge = UIBuilder.SmallButton(actions.transform, "Reforge",
                    () => Feedback(ForgeService.TryReforge(weapon, out var b), b, weapon), UITheme.Gold, 190f);
                if (reforgeBlocker != ForgeBlocker.None) UIBuilder.SetEnabled(reforge, false);
            }
        }

        private void AddCraftCard(RectTransform content, WeaponDefinition weapon)
        {
            string body = string.IsNullOrEmpty(weapon.Description) ? $"Range {weapon.Range:0.#}" : weapon.Description;
            body += $"\nRequires Forge Lv {ForgeService.CraftForgeLevel(weapon)}";
            var actions = UIBuilder.ActionCard(content, DefinitionNames.Of(weapon), body, out _, out _, RarityColors.For(weapon.Rarity));

            var blocker = ForgeService.CheckCraft(weapon);
            var craft = UIBuilder.SmallButton(actions.transform, $"Craft {ForgeService.CraftPrice(weapon)}",
                () => Feedback(ForgeService.TryCraft(weapon, out var b), b, weapon), null, 320f);
            if (blocker != ForgeBlocker.None) UIBuilder.SetEnabled(craft, false);
        }

        private void Feedback(bool success, ForgeBlocker blocker, WeaponDefinition weapon)
        {
            if (success) Sfx.Play(AudioCueIds.UiUpgrade);
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(ForgeService.DescribeBlocker(blocker, weapon));
            }
            Refresh();
        }
    }
}
