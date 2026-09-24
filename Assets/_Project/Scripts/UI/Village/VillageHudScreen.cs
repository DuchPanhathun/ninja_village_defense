using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.Systems.Requests;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Village scene's overlay (transparent HUD over the map), styled like the home screen: a wood top
    /// bar (village name, castle stage, coins/gems, Home), Decorate and Visit (neighbours) buttons, and a
    /// bottom bar with Heroes, Pets, BATTLE, Gear and Talents. Tapping a building opens its <see cref="BuildingScreen"/>; tapping a
    /// hero, pet, the Armory or the Talent Tree opens that screen; tapping a decoration offers Move / Sell;
    /// while a decoration is being placed a bar offers Flip / Cancel / Place. The Farm button glides the view
    /// to the field ("!" when something is ripe); tapping a bed opens the seed picker or its status (Water,
    /// Dig up). The Kitchen button glides to the Kitchen and opens it ("!" when a meal is done); Requests (or tapping
    /// a villager with a "!") opens today's villager requests; tapping a house plot opens its <see cref="HouseScreen"/>;
    /// Fish (left) or tapping the pond opens the fishing mini-game; tapping the mine collects its bars.
    /// Visiting someone else's village
    /// hides everything that would change it, and the back button returns to your own village.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class VillageHudScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.VillageHud;
        protected override bool IsHud => true;

        private const float TabBarHeight = 210f;

        private TextMeshProUGUI _title, _stage, _coins, _gems;
        private GameObject _decorate, _decorateBadge, _visit, _farm, _farmBadge, _kitchen, _kitchenBadge, _requests, _requestsBadge, _fish, _fishBadge, _tabBar;
        private RectTransform _placeBar, _decoBar;
        private Image _placeIcon;
        private TextMeshProUGUI _placeName, _placeHint, _decoName;
        private Button _placeConfirm, _decoSell;
        private int _selectedUid;

        private RectTransform _farmBar, _farmSeeds, _farmActions;
        private Image _farmIcon;
        private TextMeshProUGUI _farmTitle, _farmHint;
        private Button _farmWater;
        private int _selectedPlot = -1;
        private float _nextFarmRefresh;

        protected override void Build(RectTransform body)
        {
            var safe = UIBuilder.Rect(Root, "SafeArea");
            UIBuilder.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildTopBar(safe);
            BuildSideButtons(safe);
            BuildTabBar(safe);
            BuildPlacementBar(safe);
            BuildDecorationBar(safe);
            BuildFarmBar(safe);
        }

        // ------------------------------------------------------------------ layout

        private void BuildTopBar(RectTransform parent)
        {
            var bar = UIStyle.Sprite(parent, "TopBar", "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f));
            bar.color = new Color(0.12f, 0.1f, 0.08f, 0.92f);
            UIStyle.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(1060f, 150f));
            var rt = bar.rectTransform;

            var home = UIStyle.Frame(rt, "Home", "panel_wood_panel", () => { Sfx.Play(AudioCueIds.UiBack); GoBack(); });
            UIStyle.Place((RectTransform)home.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(112f, 112f));
            var arrow = UIBuilder.Image(home.transform, "Arrow", Color.white, UIArt.Get("arrow_left"));
            arrow.raycastTarget = false;
            arrow.preserveAspect = true;
            UIBuilder.Stretch(arrow.rectTransform, 26f);

            _title = UIStyle.Label(rt, "", 40f, Color.white, TextAlignmentOptions.Left, 0.25f);
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 26f;
            _title.fontSizeMax = 40f;
            UIStyle.Place(_title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(146f, 2f), new Vector2(400f, 54f));
            _stage = UIStyle.Label(rt, "", 28f, UITheme.Gold, TextAlignmentOptions.Left, 0.22f);
            UIStyle.Place(_stage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(146f, -2f), new Vector2(400f, 40f));

            _gems = Pill(rt, "store_gems_100", new Vector2(-16f, 0f), UITheme.Gem);
            _coins = Pill(rt, "pickup_coin_0", new Vector2(-240f, 0f), UITheme.Gold);
        }

        private static TextMeshProUGUI Pill(RectTransform bar, string icon, Vector2 position, Color color)
        {
            var pill = UIStyle.Sprite(bar, "Currency", "panel_wood_bg_2", new Color(0.1f, 0.08f, 0.07f));
            pill.color = new Color(0.1f, 0.08f, 0.07f, 0.95f);
            UIStyle.Place(pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(214f, 78f));
            var image = UIBuilder.Image(pill.transform, "Icon", Color.white, UIArt.Get(icon));
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(76f, 76f));
            var amount = UIStyle.Label(pill.transform, "0", 34f, color, TextAlignmentOptions.Right, 0.2f);
            UIStyle.Place(amount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(140f, 56f));
            return amount;
        }

        private void BuildSideButtons(RectTransform parent)
        {
            var decorate = UIStyle.Icon(parent, "menu_village", "Decorate", () => Open(ScreenIds.Decorations), 140f);
            UIStyle.Place(decorate.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -180f), decorate.Root.sizeDelta);
            // A decoration as the picture: it says "shop for your village" better than any menu icon.
            var picture = DecorationService.Get("flower_cart");
            var icon = decorate.Button.transform.Find("Icon")?.GetComponent<Image>();
            if (picture != null && icon != null) icon.sprite = picture.Icon;
            _decorate = decorate.Root.gameObject;
            _decorateBadge = decorate.Badge; // a villager's gift is waiting to be placed

            var visit = UIStyle.Icon(parent, "menu_leaderboard", "Visit", () => Open(ScreenIds.Neighbours), 140f);
            UIStyle.Place(visit.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -380f), visit.Root.sizeDelta);
            _visit = visit.Root.gameObject;

            var farm = UIStyle.Icon(parent, "item_carrot", "Farm", FocusFarm, 140f);
            UIStyle.Place(farm.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -580f), farm.Root.sizeDelta);
            _farm = farm.Root.gameObject;
            _farmBadge = farm.Badge;

            var kitchen = UIStyle.Icon(parent, "item_onigiri", "Kitchen", OpenKitchen, 140f);
            UIStyle.Place(kitchen.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -780f), kitchen.Root.sizeDelta);
            _kitchen = kitchen.Root.gameObject;
            _kitchenBadge = kitchen.Badge;

            var requests = UIStyle.Icon(parent, "icon_item_scroll", "Requests", () => RequestsScreen.Open(-1), 140f);
            UIStyle.Place(requests.Root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -980f), requests.Root.sizeDelta);
            _requests = requests.Root.gameObject;
            _requestsBadge = requests.Badge;

            // Left side: the pond (the right column is full).
            var fish = UIStyle.Icon(parent, "tool_fishing_rod", "Fish", OpenFishing, 140f);
            UIStyle.Place(fish.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -180f), fish.Root.sizeDelta);
            _fish = fish.Root.gameObject;
            _fishBadge = fish.Badge;
        }

        /// <summary>Glides to the pond and opens the fishing mini-game.</summary>
        private static void OpenFishing()
        {
            var cam = UnityEngine.Camera.main;
            var controller = cam != null ? cam.GetComponent<VillageCameraController>() : null;
            if (controller != null) controller.FocusOn(VillageLayout.Pond.center);
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Show(ScreenIds.Fishing);
        }

        /// <summary>Glides to the Kitchen and opens it — or its build menu while it isn't built yet.</summary>
        private static void OpenKitchen()
        {
            var cam = UnityEngine.Camera.main;
            var controller = cam != null ? cam.GetComponent<VillageCameraController>() : null;
            var kitchen = VillageService.Get(BuildingIds.Kitchen);
            if (controller != null && kitchen != null) controller.FocusOn(kitchen.PlotPosition + new Vector2(0f, 1f));
            if (KitchenService.IsBuilt)
            {
                Sfx.Play(AudioCueIds.UiClick);
                UIScreenNavigator.Instance.Show(ScreenIds.Kitchen);
            }
            else
            {
                BuildingScreen.Open(BuildingIds.Kitchen);
            }
        }

        private static void FocusFarm()
        {
            Sfx.Play(AudioCueIds.UiClick);
            var cam = UnityEngine.Camera.main;
            var controller = cam != null ? cam.GetComponent<VillageCameraController>() : null;
            if (controller != null) controller.FocusOn(VillageLayout.FarmCenter);
        }

        /// <summary>Plot status (Water / Dig up / Storehouse / Close) or, on an empty plot, the seed picker.</summary>
        private void BuildFarmBar(RectTransform parent)
        {
            _farmBar = Bar(parent, "FarmBar", out _farmIcon, out _farmTitle, out _farmHint);
            _farmBar.sizeDelta = new Vector2(1060f, 400f);

            _farmActions = UIBuilder.Rect(_farmBar, "Actions");
            UIStyle.Place(_farmActions, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(1060f, 140f));
            SmallButton(_farmActions, "Close", new Vector2(-750f, 0f), UITheme.ButtonSecondary, () => SelectPlot(-1));
            SmallButton(_farmActions, "Storehouse", new Vector2(-510f, 0f), UITheme.ButtonSecondary, () => Open(ScreenIds.Storehouse));
            SmallButton(_farmActions, "Dig up", new Vector2(-270f, 0f), new Color(0.75f, 0.3f, 0.28f), DigUp);
            _farmWater = SmallButton(_farmActions, "Water", new Vector2(-30f, 0f), new Color(0.35f, 0.6f, 0.95f), WaterPlot);

            _farmSeeds = UIBuilder.Rect(_farmBar, "Seeds");
            UIStyle.Place(_farmSeeds, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1030f, 250f));
            var crops = FarmService.GetCrops();
            float width = 1030f / Mathf.Max(1, crops.Count);
            for (int i = 0; i < crops.Count; i++)
            {
                var crop = crops[i];
                var button = UIStyle.Frame(_farmSeeds, crop.Id, "panel_wood_panel", () => PlantSelected(crop), new Color(0.3f, 0.22f, 0.15f));
                UIStyle.Place((RectTransform)button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * width + 4f, 0f),
                    new Vector2(width - 8f, 240f));
                var icon = UIBuilder.Image(button.transform, "Icon", Color.white, crop.Icon);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UIStyle.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(90f, 90f));
                var name = UIStyle.Label(button.transform, crop.NameOrId, 26f);
                UIStyle.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(width, 34f));
                var info = UIStyle.Label(button.transform, "", 21f, UIStyle.Cream, TextAlignmentOptions.Center, 0.2f);
                info.name = "Info";
                UIStyle.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(width, 48f));
            }
            _farmBar.gameObject.SetActive(false);
        }

        private void SelectPlot(int plot)
        {
            _selectedPlot = plot;
            if (plot >= 0) SelectDecoration(0);
            RefreshFarmBar();
        }

        private void RefreshFarmBar()
        {
            bool show = _selectedPlot >= 0 && !DecorationPlacer.IsActive;
            _farmBar.gameObject.SetActive(show);
            if (!show) return;

            var state = FarmService.GetPlot(_selectedPlot);
            var crop = state != null ? FarmService.GetCrop(state.CropId) : null;
            bool empty = state == null;
            _farmSeeds.gameObject.SetActive(empty);
            _farmActions.gameObject.SetActive(!empty);
            _farmBar.sizeDelta = new Vector2(1060f, empty ? 400f : 280f);

            if (empty)
            {
                _farmIcon.sprite = UIArt.Get("tool_hoe");
                _farmTitle.text = $"Plot {_selectedPlot + 1} · choose a seed";
                _farmHint.text = "Crops keep growing while you're away.";
                foreach (var crop2 in FarmService.GetCrops())
                {
                    var button = _farmSeeds.Find(crop2.Id);
                    if (button == null) continue;
                    bool unlocked = FarmService.IsCropUnlocked(crop2);
                    bool afford = CurrencyService.CanAfford(new Price(CurrencyType.Coins, crop2.SeedCost));
                    var info = button.Find("Info")?.GetComponent<TextMeshProUGUI>();
                    if (info != null)
                        info.text = unlocked
                            ? $"{FarmRules.Format(System.TimeSpan.FromSeconds(crop2.GrowSeconds))}\n<color=#FFD24D>{crop2.SeedCost} coins</color>"
                            : $"<color=#F2A0A0>Castle Lv {crop2.RequiredCastleLevel}</color>";
                    UIBuilder.SetEnabled(button.GetComponent<Button>(), unlocked && afford);
                }
                return;
            }

            _farmIcon.sprite = crop != null ? crop.Icon : null;
            var stage = FarmService.Stage(_selectedPlot);
            string name = crop != null ? crop.NameOrId : "Crop";
            _farmTitle.text = stage == CropStage.Ripe ? $"{name} · ready!" : $"{name} · {FarmRules.Format(FarmService.TimeLeft(_selectedPlot))} left";
            _farmHint.text = stage == CropStage.Ripe
                ? "Tap the plot to harvest."
                : state.Watered ? "Watered — growing faster." : $"Water it once to grow {Mathf.RoundToInt(FarmRules.WaterSpeedUp * 100f)}% faster.";
            UIBuilder.SetEnabled(_farmWater, !state.Watered && stage != CropStage.Ripe);
            UIBuilder.SetLabel(_farmWater, state.Watered ? "Watered" : "Water");
        }

        private void PlantSelected(CropDefinition crop)
        {
            var result = FarmService.Plant(_selectedPlot, crop);
            if (result == FarmResult.Success)
            {
                Sfx.Play(AudioCueIds.UiPurchase);
                RefreshFarmBar();
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast(FarmService.Describe(result, crop));
            }
        }

        private void WaterPlot()
        {
            if (FarmService.Water(_selectedPlot) == FarmResult.Success) Sfx.Play(AudioCueIds.UiUpgrade);
            RefreshFarmBar();
        }

        private void DigUp()
        {
            if (FarmService.Clear(_selectedPlot)) Sfx.Play(AudioCueIds.UiBack);
            RefreshFarmBar();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextFarmRefresh || _farmBadge == null) return;
            _nextFarmRefresh = Time.unscaledTime + 0.5f;
            _farmBadge.SetActive(!VillageVisit.IsVisiting && FarmService.RipeCount() > 0);
            if (_kitchenBadge != null) _kitchenBadge.SetActive(!VillageVisit.IsVisiting && KitchenService.ReadyCount() > 0);
            if (_requestsBadge != null) _requestsBadge.SetActive(!VillageVisit.IsVisiting && RequestService.DeliverableCount() > 0);
            if (_decorateBadge != null) _decorateBadge.SetActive(!VillageVisit.IsVisiting && DecorationService.HasGifts());
            if (_fishBadge != null) _fishBadge.SetActive(!VillageVisit.IsVisiting && FishingService.Casts >= PondMineRules.MaxCasts);
            if (_selectedPlot >= 0) RefreshFarmBar(); // live countdown
        }

        private void BuildTabBar(RectTransform parent)
        {
            var bar = UIStyle.Sprite(parent, "TabBar", "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f));
            bar.color = new Color(0.12f, 0.1f, 0.08f, 0.97f);
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(12f, TabBarHeight);
            _tabBar = bar.gameObject;

            var tabs = new (string icon, string label, string screenId, bool tile)[]
            {
                ("menu_gear", "Equipment", ScreenIds.Equipment, false), ("menu_talents", "Talents", ScreenIds.Talents, false),
                ("menu_play", "BATTLE", null, false),
                ("pickup_chest_0", "Storehouse", ScreenIds.Storehouse, true), ("menu_profile", "Profile", ScreenIds.Profile, true),
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                var (icon, label, screenId, tile) = tabs[i];
                bool battle = screenId == null;
                float x = (i + 0.5f) / tabs.Length;
                if (battle)
                {
                    var glow = UIStyle.Sprite(rt, "Battle", "panel_wood_panel", UITheme.Button);
                    glow.color = UITheme.Button;
                    glow.raycastTarget = false;
                    UIStyle.Place(glow.rectTransform, new Vector2(x, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(206f, 236f));
                }
                string id = screenId;
                var button = UIStyle.Icon(rt, icon, label, battle ? () => SceneLoader.LoadBattle() : () => Open(id), battle ? 150f : 118f, tile);
                UIStyle.Place(button.Root, new Vector2(x, 0f), new Vector2(0.5f, 0f), new Vector2(0f, battle ? 34f : 22f), button.Root.sizeDelta);
            }
        }

        /// <summary>Shown while placing: picture, name/price, a live hint, and Flip / Cancel / Place.</summary>
        private void BuildPlacementBar(RectTransform parent)
        {
            _placeBar = Bar(parent, "PlacementBar", out _placeIcon, out _placeName, out _placeHint);
            SmallButton(_placeBar, "Flip", new Vector2(-470f, 0f), UITheme.ButtonSecondary, () => DecorationPlacer.Instance?.Flip());
            SmallButton(_placeBar, "Cancel", new Vector2(-250f, 0f), new Color(0.75f, 0.3f, 0.28f), () => DecorationPlacer.Instance?.Cancel());
            _placeConfirm = SmallButton(_placeBar, "Place", new Vector2(-30f, 0f), UITheme.Positive, Confirm);
            _placeBar.gameObject.SetActive(false);
        }

        /// <summary>Shown after tapping a placed decoration: Move / Sell / Close.</summary>
        private void BuildDecorationBar(RectTransform parent)
        {
            _decoBar = Bar(parent, "DecorationBar", out _, out _decoName, out _);
            SmallButton(_decoBar, "Close", new Vector2(-470f, 0f), UITheme.ButtonSecondary, () => SelectDecoration(0));
            _decoSell = SmallButton(_decoBar, "Sell", new Vector2(-250f, 0f), new Color(0.75f, 0.3f, 0.28f), Sell);
            SmallButton(_decoBar, "Move", new Vector2(-30f, 0f), UITheme.Button, Move);
            _decoBar.gameObject.SetActive(false);
        }

        private static RectTransform Bar(RectTransform parent, string name, out Image icon, out TextMeshProUGUI title, out TextMeshProUGUI hint)
        {
            var bar = UIStyle.Sprite(parent, name, "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f));
            bar.color = new Color(0.14f, 0.1f, 0.08f, 0.96f);
            var rt = bar.rectTransform;
            UIStyle.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, TabBarHeight + 16f), new Vector2(1060f, 250f));

            var tile = UIStyle.Sprite(rt, "Tile", "panel_tint", new Color(0.16f, 0.11f, 0.08f));
            tile.color = new Color(0.16f, 0.11f, 0.08f, 1f);
            UIStyle.Place(tile.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(110f, 110f));
            icon = UIBuilder.Image(tile.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIBuilder.Stretch(icon.rectTransform, 12f);

            title = UIStyle.Label(rt, "", 36f, Color.white, TextAlignmentOptions.Left, 0.22f);
            UIStyle.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(148f, -20f), new Vector2(880f, 50f));
            hint = UIStyle.Label(rt, "", 26f, UIStyle.Cream, TextAlignmentOptions.Left, 0.2f);
            UIStyle.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(148f, -74f), new Vector2(880f, 40f));
            return rt;
        }

        private static Button SmallButton(RectTransform bar, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var button = UIStyle.Frame(bar, label, "button_tint", onClick, color);
            button.image.color = color;
            UIStyle.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), position + new Vector2(0f, 18f), new Vector2(200f, 96f));
            var text = UIStyle.Label(button.transform, label, 36f);
            UIBuilder.Stretch(text.rectTransform, 8f);
            return button;
        }

        // ------------------------------------------------------------------ behaviour

        private static void Open(string screenId)
        {
            if (DecorationPlacer.IsActive) return;
            Sfx.Play(AudioCueIds.UiClick);
            var navigator = UIScreenNavigator.Instance;
            if (navigator.Has(screenId)) navigator.Show(screenId);
        }

        /// <summary>Visiting: back to your own village. At home: back to the main menu.</summary>
        private static void GoBack()
        {
            if (VillageVisit.IsVisiting)
            {
                VillageVisit.ReturnHome();
                SceneLoader.LoadVillage();
                return;
            }
            SceneLoader.LoadMainMenu();
        }

        protected override void Start()
        {
            base.Start();
            UIScreenNavigator.Instance.ShowRoot(ScreenId);
        }

        private void OnEnable()
        {
            EventBus<VillageBuildingTappedEvent>.Subscribe(OnBuildingTapped);
            EventBus<VillageDisplayTappedEvent>.Subscribe(OnDisplayTapped);
            EventBus<DecorationTappedEvent>.Subscribe(OnDecorationTapped);
            EventBus<DecorationPlacementEvent>.Subscribe(OnPlacement);
            EventBus<FarmPlotTappedEvent>.Subscribe(OnPlotTapped);
            EventBus<VillagerRequestTappedEvent>.Subscribe(OnRequestTapped);
            EventBus<HousePlotTappedEvent>.Subscribe(OnHouseTapped);
            EventBus<PondTappedEvent>.Subscribe(OnPondTapped);
            EventBus<FarmChangedEvent>.Subscribe(OnFarmChanged);
            EventBus<CurrencyChangedEvent>.Subscribe(OnCurrencyChanged);
        }

        private void OnDisable()
        {
            EventBus<VillageBuildingTappedEvent>.Unsubscribe(OnBuildingTapped);
            EventBus<VillageDisplayTappedEvent>.Unsubscribe(OnDisplayTapped);
            EventBus<DecorationTappedEvent>.Unsubscribe(OnDecorationTapped);
            EventBus<DecorationPlacementEvent>.Unsubscribe(OnPlacement);
            EventBus<FarmPlotTappedEvent>.Unsubscribe(OnPlotTapped);
            EventBus<VillagerRequestTappedEvent>.Unsubscribe(OnRequestTapped);
            EventBus<HousePlotTappedEvent>.Unsubscribe(OnHouseTapped);
            EventBus<PondTappedEvent>.Unsubscribe(OnPondTapped);
            EventBus<FarmChangedEvent>.Unsubscribe(OnFarmChanged);
            EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);
        }

        private bool IsCurrent => UIScreenNavigator.Instance.Current == this;

        private void OnBuildingTapped(VillageBuildingTappedEvent evt)
        {
            // Only react while the map is actually what the player is looking at.
            if (!IsCurrent || DecorationPlacer.IsActive) return;
            SelectDecoration(0);
            SelectPlot(-1);
            // The mine hands over its bars with a tap (like the treasury); with nothing to collect it opens its menu.
            if (evt.BuildingId == BuildingIds.Mine && MineService.Available > 0)
            {
                var haul = MineService.Collect();
                Sfx.Play(AudioCueIds.RewardClaim);
                UIScreenNavigator.Instance.Toast($"Mine: +{haul}");
                return;
            }
            BuildingScreen.Open(evt.BuildingId);
        }

        private void OnPondTapped(PondTappedEvent evt)
        {
            if (!IsCurrent || DecorationPlacer.IsActive || VillageVisit.IsVisiting) return;
            SelectDecoration(0);
            SelectPlot(-1);
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Show(ScreenIds.Fishing);
        }

        private void OnHouseTapped(HousePlotTappedEvent evt)
        {
            if (!IsCurrent || DecorationPlacer.IsActive || VillageVisit.IsVisiting) return;
            SelectDecoration(0);
            SelectPlot(-1);
            if (!HouseService.IsUnlocked(evt.Plot))
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast(HouseService.Describe(HouseResult.PlotLocked, evt.Plot));
                return;
            }
            HouseScreen.Open(evt.Plot);
        }

        private void OnRequestTapped(VillagerRequestTappedEvent evt)
        {
            if (!IsCurrent || DecorationPlacer.IsActive || VillageVisit.IsVisiting) return;
            SelectDecoration(0);
            SelectPlot(-1);
            RequestsScreen.Open(evt.Index);
        }

        private void OnPlotTapped(FarmPlotTappedEvent evt)
        {
            if (!IsCurrent || DecorationPlacer.IsActive) return;
            if (!FarmService.IsUnlocked(evt.Plot))
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast($"This plot opens at Castle Lv {FarmRules.CastleLevelForPlot(evt.Plot)}.");
                return;
            }
            Sfx.Play(AudioCueIds.UiClick);
            SelectPlot(evt.Plot);
        }

        private void OnFarmChanged(FarmChangedEvent evt)
        {
            if (evt.Plot == _selectedPlot) RefreshFarmBar();
        }

        private void OnDisplayTapped(VillageDisplayTappedEvent evt)
        {
            if (!IsCurrent) return;
            SelectDecoration(0);
            SelectPlot(-1);
            // Heroes, pets and the Armory all live on the Equipment screen now.
            if (evt.Kind is VillageDisplayKind.Heroes or VillageDisplayKind.Pets or VillageDisplayKind.Gear)
            {
                Sfx.Play(AudioCueIds.UiClick);
                Equipment.EquipmentScreen.Open(evt.Kind == VillageDisplayKind.Heroes ? Equipment.EquipmentScreen.Tab.Heroes
                    : evt.Kind == VillageDisplayKind.Pets ? Equipment.EquipmentScreen.Tab.Pets : Equipment.EquipmentScreen.Tab.Gear);
                return;
            }
            Open(evt.Kind switch
            {
                VillageDisplayKind.Heroes => ScreenIds.Heroes,
                VillageDisplayKind.Pets => ScreenIds.Pets,
                VillageDisplayKind.Gear => ScreenIds.Inventory,
                VillageDisplayKind.Profile => ScreenIds.Profile,
                VillageDisplayKind.Storehouse => ScreenIds.Storehouse,
                _ => ScreenIds.Talents,
            });
        }

        private void OnDecorationTapped(DecorationTappedEvent evt)
        {
            if (!IsCurrent || DecorationPlacer.IsActive) return;
            SelectDecoration(evt.Uid);
        }

        private void SelectDecoration(int uid)
        {
            _selectedUid = uid;
            if (uid != 0 && _selectedPlot >= 0)
            {
                _selectedPlot = -1;
                RefreshFarmBar();
            }
            var placed = uid != 0 ? DecorationService.Find(uid) : null;
            var definition = placed != null ? DecorationService.Get(placed.Id) : null;
            _decoBar.gameObject.SetActive(definition != null);
            if (definition == null) return;
            _decoName.text = definition.NameOrId;
            var refund = new Price(definition.Price.Currency, DecorationRules.Refund(definition.Price.Amount));
            UIBuilder.SetLabel(_decoSell, refund.IsFree ? "Remove" : $"Sell +{refund.Amount}");
            var icon = _decoBar.Find("Tile/Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = definition.Icon;
        }

        private void Move()
        {
            var placed = DecorationService.Find(_selectedUid);
            var definition = placed != null ? DecorationService.Get(placed.Id) : null;
            if (definition == null || DecorationPlacer.Instance == null) return;
            var view = VillageMap.Instance != null ? VillageMap.Instance.FindDecoration(_selectedUid) : null;
            _decoBar.gameObject.SetActive(false);
            DecorationPlacer.Instance.Begin(definition, new Vector2(placed.X, placed.Y), placed.Uid, placed.Flip, view);
        }

        private void Sell()
        {
            if (!DecorationService.TrySell(_selectedUid, out var refund)) return;
            Sfx.Play(AudioCueIds.RewardClaim);
            if (!refund.IsFree) UIScreenNavigator.Instance.Toast($"Sold for {refund}");
            SelectDecoration(0);
        }

        private void Confirm()
        {
            var placer = DecorationPlacer.Instance;
            if (placer == null) return;
            if (placer.Confirm(out var error)) Sfx.Play(AudioCueIds.UiUpgrade);
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                if (!string.IsNullOrEmpty(error)) UIScreenNavigator.Instance.Toast(error);
            }
        }

        private void OnPlacement(DecorationPlacementEvent evt)
        {
            var placer = DecorationPlacer.Instance;
            bool active = evt.Active && placer != null && placer.Definition != null;
            _placeBar.gameObject.SetActive(active);
            if (active && _selectedPlot >= 0) SelectPlot(-1);
            _tabBar.SetActive(!active);
            _decorate.SetActive(!active && !VillageVisit.IsVisiting);
            _kitchen.SetActive(!active && !VillageVisit.IsVisiting);
            _requests.SetActive(!active && !VillageVisit.IsVisiting);
            _fish.SetActive(!active && !VillageVisit.IsVisiting);
            _visit.SetActive(!active);
            if (!active) return;

            var definition = placer.Definition;
            _placeIcon.sprite = definition.Icon;
            var price = placer.PriceToPay;
            _placeName.text = price.HasValue ? $"{definition.NameOrId}  <color=#FFD24D>{price.Value}</color>"
                : placer.IsGift ? $"{definition.NameOrId}  <color=#9CFF8A>gift</color>" : $"Move {definition.NameOrId}";
            _placeHint.text = placer.Fits ? "Drag it, or tap where it should go." : $"<color=#FF8A7A>{DecorationRules.Describe(placer.Blocker)}</color>";
            UIBuilder.SetEnabled(_placeConfirm, placer.Fits && (!price.HasValue || CurrencyService.CanAfford(price.Value)));
        }

        private void OnCurrencyChanged(CurrencyChangedEvent evt) => RefreshCurrencies();

        private void RefreshCurrencies()
        {
            var wallet = SaveService.Data.Wallet;
            _coins.text = UIBuilder.FormatAmount(wallet.Get(CurrencyType.Coins));
            _gems.text = UIBuilder.FormatAmount(wallet.Get(CurrencyType.Gems));
        }

        public override void Refresh()
        {
            // Coming back from Heroes / Pets / Gear / Talents / the shop: the village shows what changed.
            if (VillageMap.Instance != null) VillageMap.Instance.RefreshFromSave();

            var snapshot = VillageMap.Instance != null ? VillageMap.Instance.Snapshot : null;
            string owner = snapshot != null && !string.IsNullOrEmpty(snapshot.DisplayName) ? snapshot.DisplayName : SaveService.Data.Profile.DisplayName;
            _title.text = $"{owner}'s Village";
            var castle = VillageService.Get(BuildingIds.Castle);
            int level = snapshot != null ? Mathf.Max(1, snapshot.BuildingLevel(BuildingIds.Castle)) : VillageService.CastleLevel;
            _stage.text = (VillageVisit.IsVisiting ? "Visiting  ·  " : "") +
                          (castle != null ? $"{castle.StageName(level)}  ·  Castle Lv {level}" : $"Castle Lv {level}");
            RefreshCurrencies();

            bool visiting = VillageVisit.IsVisiting;
            _decorate.SetActive(!visiting && !DecorationPlacer.IsActive);
            _kitchen.SetActive(!visiting && !DecorationPlacer.IsActive);
            _requests.SetActive(!visiting && !DecorationPlacer.IsActive);
            _fish.SetActive(!visiting && !DecorationPlacer.IsActive);
            _tabBar.SetActive(!visiting && !DecorationPlacer.IsActive);
        }
    }
}
