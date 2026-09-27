using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The building menu (EPIC 17 Village UI "Building menu"): level, current/next effect, upgrade cost
    /// and whatever blocks the upgrade (Castle level, best wave, coins), plus the building's own action —
    /// Dojo → Heroes, Forge → Forge, Shrine → Blessings, Pet House → Pets, Market → Shop, Kitchen → Kitchen,
    /// Mine → collect bars (plus what it has dug up), Castle → the
    /// overview of what each Castle level unlocks ("Main progression"). Opened with <see cref="Open"/>.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class BuildingScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Building;
        protected override string Title => "Building";

        private static string _pendingBuildingId;
        private string _buildingId;

        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _effectText;
        private TextMeshProUGUI _blockerText;
        private Button _upgradeButton;
        private Button _actionButton;
        private RectTransform _extra;

        /// <summary>Shows the menu for <paramref name="buildingId"/> (a <see cref="BuildingIds"/> constant).</summary>
        public static void Open(string buildingId)
        {
            _pendingBuildingId = buildingId;
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Show(ScreenIds.Building);
        }

        protected override void Build(RectTransform body)
        {
            CurrencyBar.Create(body);
            var card = UIBuilder.Card(body, "Details", 14f, 28);
            _levelText = UIBuilder.Text(card.transform, "", UITheme.HeaderSize, TextAlignmentOptions.Left, UITheme.Gold, FontStyles.Bold);
            _effectText = UIBuilder.Text(card.transform, "", UITheme.BodySize);
            _effectText.richText = true;
            _blockerText = UIBuilder.Text(card.transform, "", UITheme.BodySize, TextAlignmentOptions.Left, UITheme.Negative);

            _upgradeButton = UIBuilder.Button(body, "Upgrade", OnUpgrade);
            _actionButton = UIBuilder.Button(body, "Open", OnAction, UITheme.ButtonSecondary);

            UIBuilder.ScrollList(body, "Extra", out _extra);
        }

        public override void Refresh()
        {
            if (!string.IsNullOrEmpty(_pendingBuildingId))
            {
                _buildingId = _pendingBuildingId;
                _pendingBuildingId = null;
            }

            var def = VillageService.Get(_buildingId);
            if (def == null)
            {
                TitleText.text = "Building";
                _levelText.text = "Unknown building";
                _effectText.text = "Run Ninja Village → Generate Default Content.";
                _blockerText.text = string.Empty;
                _upgradeButton.gameObject.SetActive(false);
                _actionButton.gameObject.SetActive(false);
                return;
            }

            int level = VillageService.GetLevel(def.Id);
            int cap = VillageService.LevelCap(def);
            TitleText.text = level > 0 ? def.StageName(level) : def.NameOrId;
            _levelText.text = level > 0 ? $"{def.NameOrId}  Lv {level} / {def.MaxLevel}" : $"{def.NameOrId}  (not built)";

            string effect = string.IsNullOrEmpty(def.Description) ? string.Empty : def.Description + "\n\n";
            if (level > 0) effect += $"Now: <b>{def.FormatEffectAt(level)}</b>\n";
            if (level < def.MaxLevel) effect += $"Next: <b>{def.FormatEffectAt(level + 1)}</b>";
            if (!def.IsCastle && cap < def.MaxLevel) effect += $"\n<color=#AAAAB5>Current Castle allows up to Lv {cap}</color>";
            _effectText.text = effect;

            var blocker = VillageService.CheckUpgrade(def);
            _blockerText.text = blocker == UpgradeBlocker.None ? string.Empty : VillageService.DescribeBlocker(blocker, def);

            _upgradeButton.gameObject.SetActive(blocker != UpgradeBlocker.MaxLevel);
            UIBuilder.SetLabel(_upgradeButton, $"{(level > 0 ? "Upgrade" : "Build")} — {VillageService.UpgradePrice(def)}");
            UIBuilder.SetEnabled(_upgradeButton, blocker == UpgradeBlocker.None);

            string action = ActionLabel(def.Id);
            _actionButton.gameObject.SetActive(action != null && level > 0);
            if (action != null) UIBuilder.SetLabel(_actionButton, action);

            UIBuilder.ClearChildren(_extra);
            if (def.IsCastle) PopulateCastleOverview();
            if (def.Id == BuildingIds.Mine && level > 0) PopulateMine();
        }

        private static string ActionLabel(string buildingId)
        {
            switch (buildingId)
            {
                case BuildingIds.Dojo: return "Train Heroes";
                case BuildingIds.Forge: return "Open Forge";
                case BuildingIds.Shrine: return "Blessings";
                case BuildingIds.PetHouse: return "Manage Pets";
                case BuildingIds.Market: return "Today's Shop";
                case BuildingIds.Kitchen: return "Open Kitchen";
                case BuildingIds.Mine: return "Collect bars";
                default: return null;
            }
        }

        private void OnAction()
        {
            Sfx.Play(AudioCueIds.UiClick);
            switch (_buildingId)
            {
                case BuildingIds.Dojo: UIScreenNavigator.Instance.Show(ScreenIds.Heroes); break;
                case BuildingIds.Forge: UIScreenNavigator.Instance.Show(ScreenIds.Forge); break;
                case BuildingIds.Shrine: UIScreenNavigator.Instance.Show(ScreenIds.Shrine); break;
                case BuildingIds.PetHouse: UIScreenNavigator.Instance.Show(ScreenIds.Pets); break;
                case BuildingIds.Market: UIScreenNavigator.Instance.Show(ScreenIds.Market); break;
                case BuildingIds.Kitchen: UIScreenNavigator.Instance.Show(ScreenIds.Kitchen); break;
                case BuildingIds.Mine:
                    var haul = MineService.Collect();
                    UIScreenNavigator.Instance.Toast(haul.Bars > 0 ? $"Mine: +{haul}" : "Nothing dug up yet. Check back soon!");
                    Refresh();
                    break;
            }
        }

        private void OnUpgrade()
        {
            var def = VillageService.Get(_buildingId);
            if (VillageService.TryUpgrade(def, out var blocker))
            {
                Sfx.Play(AudioCueIds.UiUpgrade);
                UIScreenNavigator.Instance.Toast($"{def.NameOrId} reached Lv {VillageService.GetLevel(def.Id)}!");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast(VillageService.DescribeBlocker(blocker, def));
            }
            Refresh();
        }

        /// <summary>Castle "Main progression": what the current Castle level allows for every other building.</summary>
        /// <summary>What the mine has dug up, how fast, and what bars are for.</summary>
        private void PopulateMine()
        {
            UIBuilder.SectionHeader(_extra, "The mine");
            int level = MineService.Level;
            string metals = level >= 5 ? "iron, gold and mithril" : level >= 3 ? "iron and gold (mithril from Lv 5)" : "iron (gold from Lv 3, mithril from Lv 5)";
            UIBuilder.ActionCard(_extra, $"{MineService.Available} / {MineService.Capacity} bars ready",
                $"Digs up {MineService.BarsPerHour:0} bars an hour: {metals}, and a gem now and then. Tap the mine on the map to collect.\n" +
                "Bars can stand in for one missing copy when merging weapons and gear into a better grade.",
                out _, out _, UITheme.Gold, UIArt.Get("item_iron_bar"));
            foreach (var id in new[] { "iron_bar", "gold_bar", "mithril_bar" })
            {
                var goods = NinjaVillage.Systems.Farm.GoodsService.Get(id);
                if (goods == null) continue;
                UIBuilder.ActionCard(_extra, $"{goods.NameOrId}   ×{NinjaVillage.Systems.Farm.GoodsService.Count(goods)}", goods.Description,
                    out _, out _, UITheme.Text, goods.Icon);
            }
        }

        private void PopulateCastleOverview()
        {
            var catalog = VillageService.Catalog;
            if (catalog == null) return;
            UIBuilder.SectionHeader(_extra, "Village progression");
            foreach (var building in catalog.All)
            {
                if (building == null || building.IsCastle) continue;
                int level = VillageService.GetLevel(building.Id);
                int cap = VillageService.LevelCap(building);
                string state = VillageService.CastleLevel < building.RequiredCastleLevel
                    ? $"<color=#F25A5A>Unlocks at Castle Lv {building.RequiredCastleLevel}</color>"
                    : $"Lv {level} — Castle allows up to Lv {cap} of {building.MaxLevel}";
                bool locked = VillageService.CastleLevel < building.RequiredCastleLevel;
                UIBuilder.ActionCard(_extra, building.NameOrId, state, out _, out _, null, UIIcons.Building(building.Id, level),
                    level > 0 ? Color.white : locked ? new Color(0.1f, 0.1f, 0.12f, 0.6f) : new Color(0.5f, 0.5f, 0.5f, 0.8f));
            }
        }
    }
}
