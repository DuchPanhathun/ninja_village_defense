using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
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
    /// bar (village name, castle stage, coins/gems, Home), a Decorate button, and a bottom bar with Heroes,
    /// Pets, BATTLE, Gear and Talents. Tapping a building opens its <see cref="BuildingScreen"/>; tapping a
    /// hero, pet, the Armory or the Talent Tree opens that screen; tapping a decoration offers Move / Sell;
    /// while a decoration is being placed a bar offers Flip / Cancel / Place. Visiting someone else's village
    /// hides everything that would change it.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class VillageHudScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.VillageHud;
        protected override bool IsHud => true;

        private const float TabBarHeight = 210f;

        private TextMeshProUGUI _title, _stage, _coins, _gems;
        private GameObject _decorate, _tabBar;
        private RectTransform _placeBar, _decoBar;
        private Image _placeIcon;
        private TextMeshProUGUI _placeName, _placeHint, _decoName;
        private Button _placeConfirm, _decoSell;
        private int _selectedUid;

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
        }

        // ------------------------------------------------------------------ layout

        private void BuildTopBar(RectTransform parent)
        {
            var bar = UIStyle.Sprite(parent, "TopBar", "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f));
            bar.color = new Color(0.12f, 0.1f, 0.08f, 0.92f);
            UIStyle.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(1060f, 150f));
            var rt = bar.rectTransform;

            var home = UIStyle.Frame(rt, "Home", "panel_wood_panel", () => { Sfx.Play(AudioCueIds.UiBack); GoHome(); });
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
                ("menu_heroes", "Heroes", ScreenIds.Heroes, true), ("menu_pets", "Pets", ScreenIds.Pets, true),
                ("menu_play", "BATTLE", null, false),
                ("menu_gear", "Gear", ScreenIds.Inventory, false), ("menu_talents", "Talents", ScreenIds.Talents, false),
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

        private static void GoHome()
        {
            if (VillageVisit.IsVisiting) VillageVisit.ReturnHome();
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
            EventBus<CurrencyChangedEvent>.Subscribe(OnCurrencyChanged);
        }

        private void OnDisable()
        {
            EventBus<VillageBuildingTappedEvent>.Unsubscribe(OnBuildingTapped);
            EventBus<VillageDisplayTappedEvent>.Unsubscribe(OnDisplayTapped);
            EventBus<DecorationTappedEvent>.Unsubscribe(OnDecorationTapped);
            EventBus<DecorationPlacementEvent>.Unsubscribe(OnPlacement);
            EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);
        }

        private bool IsCurrent => UIScreenNavigator.Instance.Current == this;

        private void OnBuildingTapped(VillageBuildingTappedEvent evt)
        {
            // Only react while the map is actually what the player is looking at.
            if (!IsCurrent || DecorationPlacer.IsActive) return;
            SelectDecoration(0);
            BuildingScreen.Open(evt.BuildingId);
        }

        private void OnDisplayTapped(VillageDisplayTappedEvent evt)
        {
            if (!IsCurrent) return;
            SelectDecoration(0);
            Open(evt.Kind switch
            {
                VillageDisplayKind.Heroes => ScreenIds.Heroes,
                VillageDisplayKind.Pets => ScreenIds.Pets,
                VillageDisplayKind.Gear => ScreenIds.Inventory,
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
            _tabBar.SetActive(!active);
            _decorate.SetActive(!active && !VillageVisit.IsVisiting);
            if (!active) return;

            var definition = placer.Definition;
            _placeIcon.sprite = definition.Icon;
            var price = placer.PriceToPay;
            _placeName.text = price.HasValue ? $"{definition.NameOrId}  <color=#FFD24D>{price.Value}</color>" : $"Move {definition.NameOrId}";
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
            _stage.text = castle != null ? $"{castle.StageName(level)}  ·  Castle Lv {level}" : $"Castle Lv {level}";
            RefreshCurrencies();

            bool visiting = VillageVisit.IsVisiting;
            _decorate.SetActive(!visiting && !DecorationPlacer.IsActive);
            _tabBar.SetActive(!visiting && !DecorationPlacer.IsActive);
        }
    }
}
