using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.LiveOps;
using NinjaVillage.Systems.Monetization;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.MainMenu
{
    /// <summary>
    /// The Main Menu's home screen (EPIC 17 "Home"), laid out like a modern mobile action game:
    /// a top bar (hero portrait, name, gems, coins), a battle-pass season banner, icon columns on both
    /// sides, the selected hero animated in the middle, a big START button between Heroes and Village,
    /// and a bottom tab bar. Every link opens its screen; screens with something to claim
    /// (<see cref="ScreenBadges"/>) show a red "!" badge, and links whose screen isn't in this scene hide.
    /// Art comes from <see cref="UIArt"/> (Ninja Adventure pack + generated icons).
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class HomeScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Home;

        private const float Side = 190f;          // width reserved for each icon column
        private const float TabBarHeight = 210f;
        private const float ActionRowHeight = 190f;

        private static readonly (string icon, string label, string screenId, bool tile)[] LeftColumn =
        {
            ("menu_daily", "Daily", ScreenIds.DailyLogin, true),
            ("store_starter_pack", "Offers", ScreenIds.Store, true),
            ("menu_events", "Events", ScreenIds.Events, true),
            ("menu_achievements", "Trophies", ScreenIds.Achievements, true),
        };

        private static readonly (string icon, string label, string screenId, bool tile)[] RightColumn =
        {
            ("menu_quests", "Missions", ScreenIds.Quests, true),
            ("menu_collection", "Collection", ScreenIds.Collection, true),
            ("menu_leaderboard", "Ranking", "leaderboard", true),
            ("menu_account", "Account", "account", true),
            ("menu_settings", "Settings", ScreenIds.Settings, false),
        };

        private static readonly (string icon, string label, string screenId, bool tile)[] Tabs =
        {
            ("menu_shop", "Shop", ScreenIds.Store, true),
            ("menu_gear", "Gear", ScreenIds.Inventory, false),
            ("menu_play", "Battle", null, false),
            ("menu_talents", "Talents", ScreenIds.Talents, false),
            ("menu_pets", "Pets", ScreenIds.Pets, true),
        };

        private readonly List<(UIStyle.IconButton button, string screenId)> _links = new();
        private TextMeshProUGUI _playerName, _heroLine, _gems, _coins, _seasonTitle, _seasonProgress, _tier, _heroName, _stats;
        private Image _portrait, _seasonFill, _heroImage;
        private UIImageAnimator _heroAnimator;
        private GameObject _seasonBadge, _heroesBadge;
        private string _shownHeroKey;

        protected override void Build(RectTransform body)
        {
            if (Root.TryGetComponent<Image>(out var backdrop)) backdrop.color = Color.black;

            var background = UIBuilder.Image(Root, "Background", Color.white, UIArt.Get("bg_mainmenu"));
            background.preserveAspect = false;
            UIBuilder.Stretch(background.rectTransform);
            if (background.sprite != null)
            {
                var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
            }
            else background.color = new Color(0.1f, 0.12f, 0.18f);
            var dim = UIBuilder.Image(Root, "Dim", new Color(0f, 0f, 0f, 0.32f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = false;

            var safe = UIBuilder.Rect(Root, "SafeArea");
            UIBuilder.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildTopBar(safe);
            BuildSeasonBanner(safe);
            BuildColumn(safe, LeftColumn, left: true);
            BuildColumn(safe, RightColumn, left: false);
            BuildShowcase(safe);
            BuildActionRow(safe);
            BuildTabBar(safe);
        }

        // ------------------------------------------------------------------ top bar

        private void BuildTopBar(RectTransform parent)
        {
            var bar = UIBuilder.Rect(parent, "TopBar");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 170f);

            var shade = UIStyle.Sprite(bar, "Shade", "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f, 0.9f));
            UIBuilder.Stretch(shade.rectTransform, 8f);
            shade.raycastTarget = false;

            var avatar = UIStyle.Frame(bar, "Avatar", "panel_wood_panel", () => Open(ScreenIds.Profile));
            UIStyle.Place((RectTransform)avatar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(140f, 140f));
            _portrait = UIBuilder.Image(avatar.transform, "Portrait", Color.white);
            _portrait.raycastTarget = false;
            _portrait.preserveAspect = true;
            UIBuilder.Stretch(_portrait.rectTransform, 18f);

            _playerName = UIStyle.Label(bar, "", 44f, Color.white, TextAlignmentOptions.Left);
            UIStyle.Place(_playerName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 4f), new Vector2(360f, 56f));
            _heroLine = UIStyle.Label(bar, "", 28f, UIStyle.Cream, TextAlignmentOptions.Left, 0.18f);
            UIStyle.Place(_heroLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(180f, -2f), new Vector2(360f, 40f));

            _gems = CurrencyPill(bar, "store_gems_100", new Vector2(-22f, 0f), UITheme.Gem);
            _coins = CurrencyPill(bar, "pickup_coin", new Vector2(-282f, 0f), UITheme.Gold);
        }

        private TextMeshProUGUI CurrencyPill(RectTransform bar, string icon, Vector2 position, Color color)
        {
            var pill = UIStyle.Frame(bar, "Currency", "panel_wood_bg_2", () => Open(ScreenIds.Store), new Color(0.1f, 0.08f, 0.07f, 0.95f));
            UIStyle.Place((RectTransform)pill.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(245f, 84f));

            var image = UIBuilder.Image(pill.transform, "Icon", Color.white, UIArt.Get(icon));
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(82f, 82f));

            var amount = UIStyle.Label(pill.transform, "0", 36f, color, TextAlignmentOptions.Right, 0.2f);
            UIStyle.Place(amount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(130f, 60f));

            var plus = UIStyle.Sprite(pill.transform, "Plus", "button_normal", UITheme.Positive);
            plus.raycastTarget = false;
            UIStyle.Place(plus.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(54f, 54f));
            var plusText = UIStyle.Label(plus.transform, "+", 44f, Color.white, TextAlignmentOptions.Center, 0.2f);
            UIBuilder.Stretch(plusText.rectTransform);
            return amount;
        }

        // ------------------------------------------------------------------ season banner

        private void BuildSeasonBanner(RectTransform parent)
        {
            var banner = UIStyle.Frame(parent, "Season", "panel_map", () => Open(ScreenIds.BattlePass), new Color(0.95f, 0.7f, 0.3f));
            UIStyle.Place((RectTransform)banner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(880f, 120f));

            var tier = UIBuilder.Image(banner.transform, "Tier", new Color(0.95f, 0.55f, 0.18f), Core.Utilities.GeneratedSprites.Circle);
            tier.raycastTarget = false;
            UIStyle.Place(tier.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(104f, 104f));
            _tier = UIStyle.Label(tier.transform, "1", 50f);
            UIBuilder.Stretch(_tier.rectTransform);

            _seasonTitle = UIStyle.Label(banner.transform, "", 32f, Color.white, TextAlignmentOptions.Left, 0.22f);
            UIStyle.Place(_seasonTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(140f, 2f), new Vector2(700f, 48f));

            var track = UIStyle.Sprite(banner.transform, "Track", "bar_hp_bg", new Color(0.15f, 0.15f, 0.15f));
            track.raycastTarget = false;
            UIStyle.Place(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(140f, -4f), new Vector2(560f, 40f));
            _seasonFill = UIBuilder.Image(track.transform, "Fill", Color.white, UIArt.Get("bar_xp_fill"));
            _seasonFill.raycastTarget = false;
            _seasonFill.type = Image.Type.Filled;
            _seasonFill.fillMethod = Image.FillMethod.Horizontal;
            UIBuilder.Stretch(_seasonFill.rectTransform, 6f);
            _seasonProgress = UIStyle.Label(track.transform, "", 26f, Color.white, TextAlignmentOptions.Center, 0.25f);
            UIBuilder.Stretch(_seasonProgress.rectTransform);

            _seasonBadge = UIStyle.Badge(banner.transform, 120f);
        }

        // ------------------------------------------------------------------ side columns

        private void BuildColumn(RectTransform parent, (string icon, string label, string screenId, bool tile)[] items, bool left)
        {
            var column = UIBuilder.Rect(parent, left ? "LeftColumn" : "RightColumn");
            column.anchorMin = column.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            column.pivot = new Vector2(left ? 0f : 1f, 1f);
            column.anchoredPosition = new Vector2(left ? 16f : -16f, -326f);
            column.sizeDelta = new Vector2(Side - 16f, 1100f);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            foreach (var (icon, label, screenId, tile) in items)
            {
                string id = screenId;
                var button = UIStyle.Icon(column, icon, label, () => Open(id), 142f, tile);
                _links.Add((button, id));
            }
        }

        // ------------------------------------------------------------------ hero showcase

        private void BuildShowcase(RectTransform parent)
        {
            var area = UIBuilder.Rect(parent, "Showcase");
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Side, TabBarHeight + ActionRowHeight + 40f);
            area.offsetMax = new Vector2(-Side, -320f);

            var title = UIStyle.Label(area, "NINJA VILLAGE\nDEFENSE", 70f, UITheme.Gold, TextAlignmentOptions.Center, 0.28f);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = -18f;
            UIStyle.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(700f, 170f));

            var glow = UIBuilder.Image(area, "Glow", new Color(1f, 0.85f, 0.45f, 0.35f), Core.Utilities.GeneratedSprites.Glow);
            glow.raycastTarget = false;
            UIStyle.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(620f, 620f));

            var shadow = UIBuilder.Image(area, "Shadow", new Color(0f, 0f, 0f, 0.35f), Core.Utilities.GeneratedSprites.Circle);
            shadow.raycastTarget = false;
            UIStyle.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(300f, 70f));

            var heroButton = UIStyle.Frame(area, "Hero", null, () => Open(ScreenIds.Heroes), Color.clear);
            heroButton.image.color = new Color(1f, 1f, 1f, 0f);
            UIStyle.Place((RectTransform)heroButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(460f, 460f));
            _heroImage = UIBuilder.Image(heroButton.transform, "Sprite", Color.white);
            _heroImage.raycastTarget = false;
            _heroImage.preserveAspect = true;
            UIStyle.Place(_heroImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(430f, 430f));
            _heroAnimator = _heroImage.gameObject.AddComponent<UIImageAnimator>();

            var ribbon = UIStyle.Sprite(area, "Ribbon", "panel_red", UITheme.Button);
            ribbon.raycastTarget = false;
            UIStyle.Place(ribbon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(560f, 110f));
            _heroName = UIStyle.Label(ribbon.transform, "", 46f);
            UIBuilder.Stretch(_heroName.rectTransform, 10f);

            _stats = UIStyle.Label(area, "", 30f, UIStyle.Cream, TextAlignmentOptions.Center, 0.2f);
            UIStyle.Place(_stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -382f), new Vector2(700f, 44f));
        }

        // ------------------------------------------------------------------ START row

        private void BuildActionRow(RectTransform parent)
        {
            var row = UIBuilder.Rect(parent, "ActionRow");
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, TabBarHeight + 24f);
            row.sizeDelta = new Vector2(0f, ActionRowHeight);

            var start = UIStyle.Frame(row, "Start", "button_normal", () => SceneLoader.LoadBattle(), UITheme.Button);
            UIStyle.Place((RectTransform)start.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 180f));
            var startText = UIStyle.Label(start.transform, "START", 92f, Color.white, TextAlignmentOptions.Center, 0.28f);
            UIBuilder.Stretch(startText.rectTransform);

            var heroes = UIStyle.Icon(row, "menu_heroes", "Heroes", () => Open(ScreenIds.Heroes), 150f);
            UIStyle.Place(heroes.Root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), heroes.Root.sizeDelta);
            _heroesBadge = heroes.Badge;
            _links.Add((heroes, ScreenIds.Heroes));

            var village = UIStyle.Icon(row, "menu_village", "Village", () => SceneLoader.LoadVillage(), 150f, tile: false);
            UIStyle.Place(village.Root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), village.Root.sizeDelta);
        }

        // ------------------------------------------------------------------ bottom tabs

        private void BuildTabBar(RectTransform parent)
        {
            var bar = UIStyle.Sprite(parent, "TabBar", "panel_wood_bg", new Color(0.12f, 0.1f, 0.08f, 0.97f));
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(12f, TabBarHeight);

            for (int i = 0; i < Tabs.Length; i++)
            {
                var (icon, label, screenId, tile) = Tabs[i];
                bool home = screenId == null;
                float x = (i + 0.5f) / Tabs.Length;

                if (home)
                {
                    var selected = UIStyle.Sprite(rt, "Selected", "panel_wood_panel", UITheme.Gold);
                    selected.raycastTarget = false;
                    UIStyle.Place(selected.rectTransform, new Vector2(x, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(206f, 230f));
                }

                string id = screenId;
                var button = UIStyle.Icon(rt, icon, label, home ? null : () => Open(id), home ? 150f : 118f, tile);
                UIStyle.Place(button.Root, new Vector2(x, 0f), new Vector2(0.5f, 0f), new Vector2(0f, home ? 30f : 22f), button.Root.sizeDelta);
                if (!home) _links.Add((button, id));
            }
        }

        // ------------------------------------------------------------------ behaviour

        private static void Open(string screenId)
        {
            if (string.IsNullOrEmpty(screenId)) return;
            var navigator = UIScreenNavigator.Instance;
            if (navigator.Has(screenId)) navigator.Show(screenId);
        }

        protected override void Start()
        {
            base.Start();
            UIScreenNavigator.Instance.ShowRoot(ScreenId);
        }

        protected override void OnShown() => EventBus<CurrencyChangedEvent>.Subscribe(OnCurrencyChanged);
        protected override void OnHidden() => EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);
        protected override void OnDestroy()
        {
            EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);
            base.OnDestroy();
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
            var profile = SaveService.Data.Profile;
            _playerName.text = profile.DisplayName;
            RefreshCurrencies();
            RefreshHero(profile);
            RefreshSeason();

            var navigator = UIScreenNavigator.Instance;
            foreach (var (button, screenId) in _links)
            {
                bool present = navigator.Has(screenId);
                button.Root.gameObject.SetActive(present);
                if (present) button.Badge.SetActive(ScreenBadges.Has(screenId));
            }
            _seasonBadge.SetActive(ScreenBadges.Has(ScreenIds.BattlePass));
        }

        private void RefreshHero(ProfileSaveData profile)
        {
            var hero = HeroService.GetSelected();
            string heroId = hero != null ? hero.Id : null;
            string heroName = hero != null ? hero.DisplayName : "Ninja";
            int level = hero != null ? HeroService.GetLevel(hero) : 1;

            _heroName.text = $"{heroName.ToUpperInvariant()}  <size=70%>Lv {level}</size>";
            _heroLine.text = $"{heroName} · Lv {level}";
            _stats.text = profile.TotalRuns > 0
                ? $"Best wave {profile.HighestWaveReached}  ·  {profile.TotalKills} demons defeated"
                : "Your village needs you!";

            // The equipped skin's frames when it has its own art, else the hero's.
            string heroKey = CharacterSpriteLibrary.HeroKey(heroId ?? "assassin");
            string key = heroKey;
            if (heroId != null)
            {
                var skin = SkinService.Catalog != null ? SkinService.Catalog.Get(SkinService.EquippedSkinId(heroId)) : null;
                if (skin != null && SkinService.Owns(skin) && CharacterSpriteLibrary.Find(skin.Id) != null) key = skin.Id;
            }
            if (key == _shownHeroKey) return;
            _shownHeroKey = key;

            var set = CharacterSpriteLibrary.Find(key);
            if (set != null)
            {
                var frames = set.Frames(CharacterAnim.Idle);
                var sprite = set.DefaultSprite;
                // Pack heroes are drawn in 16 px cells, the Beast Ninja in 32 px ones: size by cell so bodies match.
                float cells = sprite != null ? sprite.rect.width / 128f : 1f;
                _heroImage.rectTransform.sizeDelta = Vector2.one * Mathf.Min(430f * cells, 860f);
                _heroAnimator.Play(frames.Length > 0 ? frames : new[] { sprite }, set.Fps(CharacterAnim.Idle), frames.Length > 1 ? 0f : 14f);
            }

            var portrait = UIArt.Get($"portrait_hero_{heroKey.Substring("hero_".Length)}");
            _portrait.sprite = portrait != null ? portrait : set != null ? set.DefaultSprite : null;
            _portrait.color = _portrait.sprite != null ? Color.white : Color.clear;
        }

        private void RefreshSeason()
        {
            var season = BattlePassService.EnsureSeason();
            if (season == null)
            {
                _seasonTitle.text = "Battle Pass";
                _tier.text = "-";
                _seasonProgress.text = "";
                _seasonFill.fillAmount = 0f;
                return;
            }
            int xp = BattlePassService.Xp;
            int reached = BattlePassService.TiersReached;
            float progress = LiveOpsRules.TierProgress(xp, season.XpPerTier, season.TierCount);
            _seasonTitle.text = season.DisplayName;
            _tier.text = Mathf.Max(1, reached).ToString();
            _seasonFill.fillAmount = progress;
            _seasonProgress.text = reached >= season.TierCount ? "MAX" : $"{Mathf.RoundToInt(progress * season.XpPerTier)}/{season.XpPerTier}";
        }
    }
}
