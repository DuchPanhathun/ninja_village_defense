using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.Heroes
{
    /// <summary>
    /// Hero roster (EPIC 13 + EPIC 17 Village UI "Hero screen", Dojo "Hero upgrade"): every hero with
    /// its role, current stats, and Unlock / Select / Upgrade actions. The hero level cap comes from the
    /// Dojo, so locked upgrades explain which Dojo level they need.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class HeroScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Heroes;
        protected override string Title => "Heroes";

        protected override void Populate(RectTransform content)
        {
            var selected = HeroService.GetSelected();
            InfoText.text = $"Dojo Lv {HeroService.DojoLevel} — hero level cap rises with the Dojo." +
                            (selected != null ? $"\nSelected: <b>{selected.NameOrId}</b>" : string.Empty);

            var heroes = HeroService.GetSortedHeroes();
            if (heroes.Count == 0)
            {
                UIBuilder.Text(content, "No heroes found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            foreach (var hero in heroes)
                AddHeroCard(content, hero);
        }

        private void AddHeroCard(RectTransform content, HeroDefinition hero)
        {
            bool unlocked = HeroService.IsUnlocked(hero);
            int level = HeroService.GetLevel(hero);
            int cap = HeroService.GetLevelCap(hero);

            string title = hero.NameOrId + (hero.IsPremium ? "  [Premium]" : string.Empty) +
                           (unlocked ? $"   Lv {level}/{cap}" : "   (locked)");
            string body = (string.IsNullOrEmpty(hero.RoleSummary) ? hero.Description : hero.RoleSummary) + "\n" +
                          hero.GetStatsAtLevel(Mathf.Max(1, level)).Describe(" · ");
            if (hero.SignatureWeapon != null) body += $"\nWeapon: {hero.SignatureWeapon.DisplayName}";

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, hero.ThemeColor);

            if (!unlocked)
            {
                var check = HeroService.CheckUnlock(hero);
                string price = new Price(hero.UnlockCurrency, hero.UnlockCost).ToString();
                var unlock = UIBuilder.SmallButton(actions.transform, $"Unlock {price}", () =>
                {
                    var result = HeroService.TryUnlock(hero);
                    Feedback(result, hero);
                });
                if (check != HeroActionResult.Success) UIBuilder.SetEnabled(unlock, false);
                if (check == HeroActionResult.DojoLevelTooLow)
                    UIBuilder.Text(actions.transform, HeroService.Describe(check, hero), UITheme.SmallSize, TMPro.TextAlignmentOptions.Right, UITheme.Negative);
                return;
            }

            if (HeroService.IsSelected(hero))
            {
                var selected = UIBuilder.SmallButton(actions.transform, "Selected", null, UITheme.ButtonSecondary, 200f);
                selected.interactable = false;
            }
            else
            {
                UIBuilder.SmallButton(actions.transform, "Select", () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    Feedback(HeroService.TrySelect(hero), hero);
                }, UITheme.ButtonSecondary, 200f);
            }

            var upgradeCheck = HeroService.CheckUpgrade(hero);
            string label = upgradeCheck == HeroActionResult.MaxLevel ? "Max level" : $"Upgrade {HeroService.GetUpgradeCost(hero)} Coins";
            var upgrade = UIBuilder.SmallButton(actions.transform, label, () => Feedback(HeroService.TryUpgrade(hero), hero), null, 340f);
            if (upgradeCheck != HeroActionResult.Success)
            {
                UIBuilder.SetEnabled(upgrade, false);
                if (upgradeCheck == HeroActionResult.LevelCapReached)
                    UIBuilder.SetLabel(upgrade, HeroService.Describe(upgradeCheck, hero));
            }
        }

        private void Feedback(HeroActionResult result, HeroDefinition hero)
        {
            if (result == HeroActionResult.Success) Sfx.Play(AudioCueIds.UiUpgrade);
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(HeroService.Describe(result, hero));
            }
            Refresh();
        }
    }
}
