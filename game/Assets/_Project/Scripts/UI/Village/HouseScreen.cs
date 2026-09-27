using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// A house plot's menu (EPIC 24 Phase 4): pick a style and build, grow the house a level (each level is a
    /// family: one more villager and more treasury income), or repaint it in another style for free — plus what
    /// the treasury makes now. Opened by tapping a house plot on the map.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class HouseScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.House;
        protected override string Title => "House";

        private const int Columns = 4;
        private static int _pendingPlot = -1;
        private int _plot;
        private string _chosenStyle;

        public static void Open(int plot)
        {
            _pendingPlot = plot;
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Show(ScreenIds.House);
        }

        protected override void Populate(RectTransform content)
        {
            if (_pendingPlot >= 0)
            {
                _plot = _pendingPlot;
                _pendingPlot = -1;
                _chosenStyle = null;
            }
            var house = HouseService.Get(_plot);
            if (house != null) _chosenStyle = house.Style;
            if (_chosenStyle == null || !Unlocked(_chosenStyle)) _chosenStyle = HousingRules.Styles[0].Id;
            var art = VillageArt.Load();
            float perLevel = HousingRules.CoinsPerHourPerHouseLevel;

            InfoText.text = $"Every house level is a family: one more villager, and <color=#FFD24D>+{perLevel:0} coins an hour</color> for the Treasury.";
            var style = HousingRules.Style(_chosenStyle).Value;
            Sprite picture = art != null ? art.HouseSprite(_chosenStyle) : null;

            if (house == null)
            {
                TitleText.text = $"House Plot {_plot + 1}";
                var actions = UIBuilder.ActionCard(content, "Empty plot",
                    $"Pick a style below, then build.\nA new family moves in: <color=#FFD24D>+{perLevel:0} coins/hour</color>.",
                    out _, out _, UITheme.Text, picture);
                var price = HouseService.BuildPrice;
                var build = UIBuilder.SmallButton(actions.transform, $"Build {style.Name} — {price}", Build, UITheme.Button, 560f);
                UIBuilder.SetEnabled(build, HouseService.CheckBuild(_plot, _chosenStyle) == HouseResult.Success);
            }
            else
            {
                TitleText.text = style.Name;
                string next = house.Level >= HousingRules.MaxLevel ? "Biggest it can be."
                    : $"Next level: another family, <color=#FFD24D>+{perLevel:0} coins/hour</color>.";
                var actions = UIBuilder.ActionCard(content, $"{style.Name}  Lv {house.Level} / {HousingRules.MaxLevel}",
                    $"{house.Level} {(house.Level == 1 ? "family" : "families")} · <color=#FFD24D>+{perLevel * house.Level:0} coins/hour</color>\n{next}",
                    out _, out _, UITheme.Gold, picture);
                if (house.Level < HousingRules.MaxLevel)
                {
                    var check = HouseService.CheckUpgrade(_plot);
                    string label = check == HouseResult.CastleLevel
                        ? $"Needs Castle Lv {HousingRules.CastleLevelForHouseLevel(house.Level + 1)}"
                        : $"Grow to Lv {house.Level + 1} — {HouseService.UpgradePrice(house)}";
                    var grow = UIBuilder.SmallButton(actions.transform, label, Grow, UITheme.Button, 560f);
                    UIBuilder.SetEnabled(grow, check == HouseResult.Success);
                }
            }

            UIBuilder.SectionHeader(content, house == null ? "Style" : "Repaint (free)");
            var grid = UIBuilder.Rect(content, "Styles").gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 230f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            foreach (var option in HousingRules.Styles) StyleTile((RectTransform)grid.transform, art, option);

            UIBuilder.SectionHeader(content, "Treasury");
            UIBuilder.ActionCard(content, $"+{TreasuryService.CoinsPerHour:0} coins an hour",
                $"Holds up to {TreasuryService.Capacity} coins ({HousingRules.CapHours(VillageService.CastleLevel):0} hours' worth). " +
                "A bigger castle and more families fill it faster. Tap the chest on the plaza to collect.",
                out _, out _, UITheme.Gold, art != null ? art.TreasuryChest : null);
        }

        private static bool Unlocked(string styleId)
        {
            var style = HousingRules.Style(styleId);
            return style.HasValue && HouseService.IsStyleUnlocked(style.Value);
        }

        private void StyleTile(RectTransform grid, VillageArt art, HouseStyle style)
        {
            bool unlocked = HouseService.IsStyleUnlocked(style);
            bool chosen = style.Id == _chosenStyle;
            var tile = UIStyle.Frame(grid, "Style_" + style.Id, "panel_wood_panel", () => Choose(style), chosen ? UITheme.Gold : UITheme.ButtonSecondary);
            tile.image.color = !unlocked ? new Color(0.35f, 0.3f, 0.28f) : chosen ? UITheme.Gold : Color.white;
            var image = UIBuilder.Image(tile.transform, "Picture", unlocked ? Color.white : new Color(0.2f, 0.18f, 0.18f, 0.9f),
                art != null ? art.HouseSprite(style.Id) : null);
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(170f, 150f));
            var caption = UIStyle.Label(tile.transform, unlocked ? style.Name : $"Castle Lv {style.RequiredCastleLevel}", 22f,
                unlocked ? UIStyle.Cream : new Color(0.95f, 0.63f, 0.63f), TextAlignmentOptions.Center, 0.25f);
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 14f;
            caption.fontSizeMax = 22f;
            UIStyle.Place(caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(206f, 30f));
        }

        private void Choose(HouseStyle style)
        {
            if (!HouseService.IsStyleUnlocked(style))
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast($"{style.Name} needs Castle Lv {style.RequiredCastleLevel}.");
                return;
            }
            Sfx.Play(AudioCueIds.UiClick);
            _chosenStyle = style.Id;
            if (HouseService.Get(_plot) != null && HouseService.Restyle(_plot, style.Id) == HouseResult.Success)
                Toast($"Repainted as a {style.Name}!");
            Refresh();
        }

        private void Build()
        {
            var result = HouseService.Build(_plot, _chosenStyle);
            if (result == HouseResult.Success)
            {
                Sfx.Play(AudioCueIds.UiUpgrade);
                Toast("A new family moved in!");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(HouseService.Describe(result, _plot));
            }
            Refresh();
        }

        private void Grow()
        {
            var result = HouseService.Upgrade(_plot);
            if (result == HouseResult.Success)
            {
                Sfx.Play(AudioCueIds.UiUpgrade);
                Toast($"The house grew to Lv {HouseService.Get(_plot).Level}. Another family moved in!");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(HouseService.Describe(result, _plot));
            }
            Refresh();
        }
    }
}
