using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Village scene's overlay (transparent HUD over the map): coins/gems and Castle level at the
    /// top, and the core-loop exits at the bottom — "Battle!" plus Heroes, Pets, Inventory, Talents and
    /// Home. Tapping a building on the map opens its <see cref="BuildingScreen"/>.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class VillageHudScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.VillageHud;
        protected override bool IsHud => true;

        private TextMeshProUGUI _castleText;

        protected override void Build(RectTransform body)
        {
            var top = UIBuilder.Card(body, "TopBar", 6f, 16);
            _castleText = UIBuilder.Text(top.transform, "", UITheme.HeaderSize, TextAlignmentOptions.Left, UITheme.Gold, FontStyles.Bold);
            CurrencyBar.Create(top.transform, 56f);
            UIBuilder.Text(top.transform, "Tap a building to upgrade it. Drag to look around.", UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);

            UIBuilder.FlexibleSpacer(body);

            var row = UIBuilder.Horizontal(body, "MetaButtons", 12f);
            UIBuilder.SetPreferredSize(row, -1f, 110f);
            AddNav(row.transform, "Heroes", ScreenIds.Heroes);
            AddNav(row.transform, "Pets", ScreenIds.Pets);
            AddNav(row.transform, "Gear", ScreenIds.Inventory);
            AddNav(row.transform, "Talents", ScreenIds.Talents);

            var bottom = UIBuilder.Horizontal(body, "PlayRow", 12f);
            UIBuilder.SetPreferredSize(bottom, -1f, 150f);
            var home = UIBuilder.Button(bottom.transform, "Home", () => { Sfx.Play(AudioCueIds.UiBack); SceneLoader.LoadMainMenu(); }, UITheme.ButtonSecondary, 150f);
            UIBuilder.SetPreferredSize(home, 240f, 150f);
            UIBuilder.SetFlexible(home, 0f, 0f);
            var battle = UIBuilder.Button(bottom.transform, "BATTLE!", () => { Sfx.Play(AudioCueIds.UiClick); SceneLoader.LoadBattle(); }, UITheme.Button, 150f, UITheme.TitleSize);
            UIBuilder.SetFlexible(battle, 1f, 0f);
        }

        private static void AddNav(Transform parent, string label, string screenId)
        {
            UIBuilder.Button(parent, label, () =>
            {
                Sfx.Play(AudioCueIds.UiClick);
                UIScreenNavigator.Instance.Show(screenId);
            }, UITheme.ButtonSecondary, 110f);
        }

        protected override void Start()
        {
            base.Start();
            UIScreenNavigator.Instance.ShowRoot(ScreenId);
        }

        private void OnEnable() => EventBus<VillageBuildingTappedEvent>.Subscribe(OnBuildingTapped);
        private void OnDisable() => EventBus<VillageBuildingTappedEvent>.Unsubscribe(OnBuildingTapped);

        private void OnBuildingTapped(VillageBuildingTappedEvent evt)
        {
            // Only react while the map is actually what the player is looking at.
            if (UIScreenNavigator.Instance.Current != this) return;
            BuildingScreen.Open(evt.BuildingId);
        }

        public override void Refresh()
        {
            var castle = VillageService.Get(BuildingIds.Castle);
            int level = VillageService.CastleLevel;
            _castleText.text = castle != null ? $"{castle.StageName(level)}  ·  Castle Lv {level}" : "Hidden Village";
        }
    }
}
