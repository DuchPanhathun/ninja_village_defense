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
    /// The Forge (EPIC 11 "Weapon crafting", "Weapon upgrading"): craft weapons you don't own, level up owned weapons
    /// (capped by the Forge level), forge extra copies and merge three of a grade into the next
    /// (Common → Rare → Elite → Epic → Legendary), and equip one for the next run.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class ForgeScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Forge;
        protected override string Title => "Forge";

        protected override void Populate(RectTransform content)
        {
            InfoText.text = ForgeService.ForgeLevel > 0
                ? $"Forge Lv {ForgeService.ForgeLevel} — weapons can reach Lv {ForgeService.MaxWeaponLevel}/{RuntimeWeapon.MaxLevel}. " +
                  $"Forge copies and merge {GradeRules.MergeCount} of a grade into the next."
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
                if (weapon == null || inv.OwnsWeapon(weapon.Id) || weapon.IsSpecial) continue; // S weapons: Surprise Boxes only
                if (!header) { UIBuilder.SectionHeader(content, "Craft new weapons"); header = true; }
                AddCraftCard(content, weapon);
            }
        }

        private void AddOwnedCard(RectTransform content, WeaponDefinition weapon)
        {
            int level = ForgeService.GetLevel(weapon.Id);
            var grade = InventoryService.WeaponGrade(weapon.Id);
            bool equipped = InventoryService.Data.EquippedWeaponId == weapon.Id;

            string title = $"{(weapon.IsSpecial ? "S · " : "")}{grade} {DefinitionNames.Of(weapon)}   Lv {level}";
            string body = $"Damage {weapon.GetDamage(level):0.#} · {weapon.GetAttacksPerSecond(level):0.##}/s · Range {weapon.Range:0.#}";
            float gradeBonus = GradeRules.WeaponAttackBonus(grade);
            if (gradeBonus > 0f) body += $"\n{grade} grade: +{gradeBonus * 100f:0}% attack";
            body += $"\nCopies: {MergeService.DescribeWeaponCopies(weapon.Id)}";

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, GradeColors.For(grade), WeaponIcon(weapon));

            if (!equipped)
                UIBuilder.SmallButton(actions.transform, "Equip", () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    InventoryService.EquipWeapon(weapon.Id);
                    Refresh();
                }, UITheme.ButtonSecondary, 150f);

            var upgradeBlocker = ForgeService.CheckUpgrade(weapon);
            if (upgradeBlocker != ForgeBlocker.MaxLevel)
            {
                var upgrade = UIBuilder.SmallButton(actions.transform, $"Lv up {ForgeService.UpgradePrice(weapon)}",
                    () => Feedback(ForgeService.TryUpgrade(weapon, out var b), b, weapon), null, 260f);
                if (upgradeBlocker != ForgeBlocker.None) UIBuilder.SetEnabled(upgrade, false);
            }

            if (!weapon.IsSpecial) // S weapons: copies only from Surprise Boxes
            {
                var copy = UIBuilder.SmallButton(actions.transform, $"Copy {ForgeService.CraftPrice(weapon)}",
                    () => Feedback(ForgeService.TryCraft(weapon, out var b), b, weapon), UITheme.ButtonSecondary, 250f);
                if (ForgeService.CheckCraft(weapon) != ForgeBlocker.None) UIBuilder.SetEnabled(copy, false);
            }

            var next = MergeService.NextWeaponMerge(weapon.Id);
            var merge = UIBuilder.SmallButton(actions.transform, next.HasValue ? $"Merge → {GradeRules.Next(next.Value)}" : "Merge", () =>
            {
                if (!next.HasValue) return;
                Sfx.Play(AudioCueIds.RewardClaim);
                MergeService.MergeWeapon(weapon.Id, next.Value);
                Toast($"{DefinitionNames.Of(weapon)} merged into {GradeRules.Next(next.Value)}!");
                Refresh();
            }, UITheme.Gold, 250f);
            if (!next.HasValue) UIBuilder.SetEnabled(merge, false);
        }

        private void AddCraftCard(RectTransform content, WeaponDefinition weapon)
        {
            string body = string.IsNullOrEmpty(weapon.Description) ? $"Range {weapon.Range:0.#}" : weapon.Description;
            body += $"\nRequires Forge Lv {ForgeService.CraftForgeLevel(weapon)}  ·  starts {InventoryService.NativeGrade(weapon)}";
            var actions = UIBuilder.ActionCard(content, DefinitionNames.Of(weapon), body, out _, out _, GradeColors.For(InventoryService.NativeGrade(weapon)),
                WeaponIcon(weapon), new Color(0.55f, 0.5f, 0.45f)); // not owned yet: shown dimmed

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

        private static Sprite WeaponIcon(Gameplay.Weapons.WeaponDefinition weapon) =>
            weapon.Icon != null ? weapon.Icon : UIIcons.Weapon(weapon.Id);
    }
}
