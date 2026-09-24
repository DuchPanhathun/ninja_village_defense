using System.Collections.Generic;
using NinjaVillage.Core;
using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Monetization;
using NinjaVillage.Systems.Pets;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Equipment
{
    /// <summary>
    /// Everything your ninja takes into battle, in one place (like Survivor.io's equipment screen): the
    /// selected hero stands in the middle of a showcase with ATK / HP totals above and the loadout around
    /// them — weapon and gear slots on the left and right, the pet in the last slot — then Gear / Heroes /
    /// Pets tabs over a grid of rarity-framed tiles. Tapping a tile or slot opens its card with the right
    /// actions (Equip, Level up, Select, Take along, Upgrade, Unlock). Replaces separate Gear, Heroes and
    /// Pets entries in the menus; works in the Main Menu and the Village.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class EquipmentScreen : UIScreen
    {
        public enum Tab
        {
            Gear,
            Heroes,
            Pets,
        }

        public override string ScreenId => ScreenIds.Equipment;
        protected override string Title => "Equipment";

        private const int GearSlots = 4;
        private const float SlotSize = 150f;
        private const int Columns = 5;

        private static Tab _pendingTab = Tab.Gear;

        private readonly List<Slot> _slots = new();
        private readonly Dictionary<Tab, Button> _tabButtons = new();
        private Tab _tab = Tab.Gear;
        private TextMeshProUGUI _attack, _health, _heroName;
        private Image _heroImage;
        private UIImageAnimator _heroAnimator;
        private string _shownHeroKey;
        private RectTransform _list;
        private ScrollRect _scroll;

        // Detail popup
        private GameObject _popup;
        private Image _popupFrame, _popupIcon;
        private TextMeshProUGUI _popupName, _popupSub, _popupBody;
        private RectTransform _popupActions;
        private System.Action _popupRefresh;

        private sealed class Slot
        {
            public Button Button;
            public Image Frame, Icon;
            public TextMeshProUGUI Label;
        }

        /// <summary>Opens the screen on <paramref name="tab"/> (Village displays, Heroes shortcut...).</summary>
        public static void Open(Tab tab)
        {
            var navigator = UIScreenNavigator.Instance;
            if (navigator == null || !navigator.Has(ScreenIds.Equipment)) return;
            if (navigator.Current is EquipmentScreen open)
            {
                open.ShowTab(tab); // already on screen: switch in place
                return;
            }
            _pendingTab = tab;
            navigator.Show(ScreenIds.Equipment);
        }

        private void ShowTab(Tab tab)
        {
            HidePopup();
            _tab = tab;
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        // ------------------------------------------------------------------ layout

        protected override void Build(RectTransform body)
        {
            CurrencyBar.Create(body);
            BuildShowcase(body);
            BuildTabs(body);
            _scroll = UIBuilder.ScrollList(body, "Items", out _list, 14f);
            BuildPopup();
        }

        private void BuildShowcase(RectTransform body)
        {
            var showcase = UIBuilder.Rect(body, "Showcase");
            UIBuilder.SetPreferredSize(showcase, -1f, 700f);
            var bg = UIStyle.Sprite(showcase, "Bg", "panel_map", new Color(0.95f, 0.7f, 0.35f));
            bg.color = new Color(1f, 0.8f, 0.5f);
            bg.raycastTarget = false;
            UIBuilder.Stretch(bg.rectTransform);

            var glow = UIBuilder.Image(showcase, "Glow", new Color(1f, 0.9f, 0.55f, 0.55f), Core.Utilities.GeneratedSprites.Glow);
            glow.raycastTarget = false;
            UIStyle.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(560f, 560f));

            _attack = StatChip(showcase, "icon_spell_attack_upgrade", "ATK", new Vector2(-150f, -22f), UITheme.Text);
            _health = StatChip(showcase, "pickup_heart", "HP", new Vector2(150f, -22f), new Color(1f, 0.45f, 0.45f));

            var heroButton = UIStyle.Frame(showcase, "Hero", null, () => ShowHero(HeroService.GetSelected()), Color.clear);
            heroButton.image.color = new Color(1f, 1f, 1f, 0f);
            UIStyle.Place((RectTransform)heroButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(360f, 380f));
            _heroImage = UIBuilder.Image(heroButton.transform, "Sprite", Color.white);
            _heroImage.raycastTarget = false;
            _heroImage.preserveAspect = true;
            UIStyle.Place(_heroImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 320f));
            _heroAnimator = _heroImage.gameObject.AddComponent<UIImageAnimator>();

            var ribbon = UIStyle.Sprite(showcase, "Ribbon", "panel_red", UITheme.Button);
            ribbon.raycastTarget = false;
            UIStyle.Place(ribbon.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(420f, 90f));
            _heroName = UIStyle.Label(ribbon.transform, "", 36f);
            UIBuilder.Stretch(_heroName.rectTransform, 8f);

            // Weapon + 2 gear on the left, 2 gear + the pet on the right.
            for (int i = 0; i < 6; i++)
            {
                bool left = i < 3;
                int row = left ? i : i - 3;
                int index = i;
                var slot = new Slot();
                slot.Button = UIStyle.Frame(showcase, $"Slot{i}", "panel_wood_panel", () => OnSlot(index), UITheme.ButtonSecondary);
                slot.Frame = slot.Button.image;
                UIStyle.Place((RectTransform)slot.Button.transform, new Vector2(left ? 0f : 1f, 1f), new Vector2(left ? 0f : 1f, 1f),
                    new Vector2(left ? 26f : -26f, -96f - row * (SlotSize + 44f)), new Vector2(SlotSize, SlotSize));
                slot.Icon = UIBuilder.Image(slot.Button.transform, "Icon", Color.white);
                slot.Icon.raycastTarget = false;
                slot.Icon.preserveAspect = true;
                UIBuilder.Stretch(slot.Icon.rectTransform, 24f);
                slot.Label = UIStyle.Label(slot.Button.transform, "", 24f, Color.white, TextAlignmentOptions.Center, 0.25f);
                UIStyle.Place(slot.Label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(SlotSize + 30f, 34f));
                _slots.Add(slot);
            }
        }

        private static TextMeshProUGUI StatChip(RectTransform parent, string icon, string name, Vector2 position, Color color)
        {
            var chip = UIStyle.Sprite(parent, name, "panel_wood_bg_2", new Color(0.12f, 0.09f, 0.07f));
            chip.color = new Color(0.12f, 0.09f, 0.07f, 0.9f);
            chip.raycastTarget = false;
            UIStyle.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(270f, 70f));
            var image = UIBuilder.Image(chip.transform, "Icon", Color.white, UIArt.Get(icon));
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(56f, 56f));
            var label = UIStyle.Label(chip.transform, "", 34f, color, TextAlignmentOptions.Right, 0.22f);
            UIStyle.Place(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(190f, 60f));
            return label;
        }

        private void BuildTabs(RectTransform body)
        {
            var row = UIBuilder.Horizontal(body, "Tabs", 12f);
            UIBuilder.SetPreferredSize(row, -1f, 110f);
            foreach (var (tab, icon, label) in new[] { (Tab.Gear, "menu_gear", "Gear"), (Tab.Heroes, "menu_heroes", "Heroes"), (Tab.Pets, "menu_pets", "Pets") })
            {
                var t = tab;
                var button = UIStyle.Frame(row.transform, label, "panel_wood_panel", () => SelectTab(t), UITheme.ButtonSecondary);
                var image = UIBuilder.Image(button.transform, "Icon", Color.white, UIArt.Get(icon));
                image.raycastTarget = false;
                image.preserveAspect = true;
                UIStyle.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(76f, 76f));
                var text = UIStyle.Label(button.transform, label, 36f);
                UIStyle.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(200f, 60f));
                _tabButtons[tab] = button;
            }
        }

        // ------------------------------------------------------------------ behaviour

        protected override void OnShown()
        {
            _tab = _pendingTab;
            _pendingTab = Tab.Gear;
            HidePopup();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        private void SelectTab(Tab tab)
        {
            Sfx.Play(AudioCueIds.UiClick);
            _tab = tab;
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public override void Refresh()
        {
            RefreshShowcase();
            foreach (var (tab, button) in _tabButtons)
                button.image.color = tab == _tab ? UITheme.Gold : new Color(0.55f, 0.45f, 0.38f);
            UIBuilder.ClearChildren(_list);
            switch (_tab)
            {
                case Tab.Heroes: PopulateHeroes(); break;
                case Tab.Pets: PopulatePets(); break;
                default: PopulateGear(); break;
            }
            _popupRefresh?.Invoke();
        }

        private void RefreshShowcase()
        {
            var totals = LoadoutPower.Compute(SaveService.Data);
            _attack.text = UIBuilder.FormatAmount(totals.Attack);
            _health.text = UIBuilder.FormatAmount(totals.Health);

            var hero = HeroService.GetSelected();
            _heroName.text = hero != null ? $"{hero.NameOrId.ToUpperInvariant()}  <size=70%>Lv {HeroService.GetLevel(hero)}</size>" : "NINJA";
            ShowHeroSprite(hero);

            var inv = InventoryService.Data;
            var weapon = InventoryService.GetWeapon(inv.EquippedWeaponId);
            SetSlot(_slots[0], weapon != null ? WeaponIcon(weapon) : null, weapon != null ? RarityColors.For(weapon.Rarity) : (Color?)null,
                weapon != null ? $"Lv {ForgeService.GetLevel(weapon.Id)}" : "Weapon");

            int open = InventoryService.EquipmentSlots;
            int[] gearSlots = { 1, 2, 3, 4 };
            for (int g = 0; g < GearSlots; g++)
            {
                var slot = _slots[gearSlots[g]];
                if (g >= open) { SetSlot(slot, null, null, "<color=#F2A0A0>Castle</color>", locked: true); continue; }
                var item = g < inv.EquippedEquipmentIds.Count ? InventoryService.GetEquipment(inv.EquippedEquipmentIds[g]) : null;
                SetSlot(slot, item != null ? GearIcon(item) : null, item != null ? RarityColors.For(item.Rarity) : (Color?)null,
                    item != null ? item.Rarity.ToString() : "Empty");
            }

            var pet = PetService.GetActive();
            SetSlot(_slots[5], pet != null ? UIIcons.Pet(pet.Id) : null, pet != null ? PetColor(pet) : (Color?)null,
                pet != null ? $"Lv {PetService.GetLevel(pet)}" : "Pet");
        }

        private static void SetSlot(Slot slot, Sprite icon, Color? color, string label, bool locked = false)
        {
            slot.Icon.sprite = icon;
            slot.Icon.enabled = icon != null;
            slot.Frame.color = color.HasValue ? Color.Lerp(Color.white, color.Value, 0.75f) : locked ? new Color(0.3f, 0.26f, 0.24f) : new Color(0.55f, 0.47f, 0.4f);
            slot.Label.text = label;
        }

        private void ShowHeroSprite(HeroDefinition hero)
        {
            string heroKey = CharacterSpriteLibrary.HeroKey(hero != null ? hero.Id : "assassin");
            string key = heroKey;
            if (hero != null)
            {
                var skin = SkinService.Catalog != null ? SkinService.Catalog.Get(SkinService.EquippedSkinId(hero.Id)) : null;
                if (skin != null && SkinService.Owns(skin) && CharacterSpriteLibrary.Find(skin.Id) != null) key = skin.Id;
            }
            if (key == _shownHeroKey) return;
            _shownHeroKey = key;
            var set = CharacterSpriteLibrary.Find(key);
            if (set == null) return;
            var frames = set.Frames(CharacterAnim.Idle);
            var sprite = set.DefaultSprite;
            // Pack heroes are 16 px cells, the Beast Ninja 32 px: size by cell so bodies match.
            float cells = sprite != null ? sprite.rect.width / 128f : 1f;
            _heroImage.rectTransform.sizeDelta = Vector2.one * Mathf.Min(320f * cells, 640f);
            _heroAnimator.Play(frames.Length > 0 ? frames : new[] { sprite }, set.Fps(CharacterAnim.Idle), frames.Length > 1 ? 0f : 12f);
        }

        private void OnSlot(int index)
        {
            Sfx.Play(AudioCueIds.UiClick);
            var inv = InventoryService.Data;
            if (index == 0)
            {
                var weapon = InventoryService.GetWeapon(inv.EquippedWeaponId);
                if (weapon != null) ShowWeapon(weapon);
                else SelectTab(Tab.Gear);
                return;
            }
            if (index == 5)
            {
                var pet = PetService.GetActive();
                if (pet != null) ShowPet(pet);
                else SelectTab(Tab.Pets);
                return;
            }
            int g = index - 1;
            if (g >= InventoryService.EquipmentSlots)
            {
                UIScreenNavigator.Instance.Toast("Upgrade the Castle for more gear slots.");
                return;
            }
            var item = g < inv.EquippedEquipmentIds.Count ? InventoryService.GetEquipment(inv.EquippedEquipmentIds[g]) : null;
            if (item != null) ShowGear(item);
            else SelectTab(Tab.Gear);
        }

        // ------------------------------------------------------------------ grids

        private RectTransform Grid(string title)
        {
            UIBuilder.SectionHeader(_list, title);
            var grid = UIBuilder.Rect(_list, title + "Grid").gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(176f, 200f);
            grid.spacing = new Vector2(12f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(14, 0, 0, 0);
            return (RectTransform)grid.transform;
        }

        /// <summary>A rarity-framed tile: icon, level/count in the corner, a green E when equipped/selected, dimmed when locked.</summary>
        private static void Tile(RectTransform grid, Sprite icon, Color frame, string corner, string caption, bool equipped, bool locked,
            UnityEngine.Events.UnityAction onClick)
        {
            var button = UIStyle.Frame(grid, "Tile", "panel_wood_panel", onClick, frame);
            button.image.color = locked ? new Color(0.35f, 0.3f, 0.28f) : Color.Lerp(Color.white, frame, 0.8f);
            var image = UIBuilder.Image(button.transform, "Icon", locked ? new Color(0.25f, 0.2f, 0.2f, 0.9f) : Color.white, icon);
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(118f, 118f));
            image.enabled = icon != null;

            var cornerText = UIStyle.Label(button.transform, corner, 24f, Color.white, TextAlignmentOptions.TopLeft, 0.28f);
            UIStyle.Place(cornerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(150f, 30f));
            var captionText = UIStyle.Label(button.transform, caption, 20f, UIStyle.Cream, TextAlignmentOptions.Center, 0.25f);
            captionText.enableAutoSizing = true;
            captionText.fontSizeMin = 14f;
            captionText.fontSizeMax = 20f;
            UIStyle.Place(captionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(166f, 28f));

            if (equipped)
            {
                var badge = UIBuilder.Image(button.transform, "Equipped", UITheme.Positive, Core.Utilities.GeneratedSprites.Circle);
                badge.raycastTarget = false;
                UIStyle.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-18f, -18f), new Vector2(40f, 40f));
                var e = UIStyle.Label(badge.transform, "E", 24f);
                UIBuilder.Stretch(e.rectTransform);
            }
        }

        private void PopulateGear()
        {
            var inv = InventoryService.Data;
            var weapons = Grid("Weapons");
            foreach (var entry in inv.Weapons)
            {
                var weapon = entry != null ? InventoryService.GetWeapon(entry.Id) : null;
                if (weapon == null) continue;
                Tile(weapons, WeaponIcon(weapon), RarityColors.For(weapon.Rarity), $"Lv {ForgeService.GetLevel(weapon.Id)}",
                    DefinitionNames.Of(weapon), inv.EquippedWeaponId == weapon.Id, false, () => ShowWeapon(weapon));
            }

            var gear = Grid($"Gear  ·  {inv.EquippedEquipmentIds.Count}/{InventoryService.EquipmentSlots} equipped");
            int shown = 0;
            foreach (var stack in inv.Equipment)
            {
                var item = stack != null && stack.Count > 0 ? InventoryService.GetEquipment(stack.Id) : null;
                if (item == null) continue;
                shown++;
                Tile(gear, GearIcon(item), RarityColors.For(item.Rarity), stack.Count > 1 ? $"×{stack.Count}" : "",
                    DefinitionNames.Of(item), inv.IsEquipmentEquipped(item.Id), false, () => ShowGear(item));
            }
            if (shown == 0)
                UIBuilder.Text(_list, "No gear yet — enemies, chests and the Market drop it.", UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }

        private void PopulateHeroes()
        {
            var grid = Grid("Heroes");
            foreach (var hero in HeroService.GetSortedHeroes())
            {
                var h = hero;
                bool unlocked = HeroService.IsUnlocked(hero);
                Tile(grid, UIIcons.Hero(hero.Id), hero.IsPremium ? RarityColors.Legendary : hero.ThemeColor,
                    unlocked ? $"Lv {HeroService.GetLevel(hero)}" : "", hero.NameOrId, HeroService.IsSelected(hero), !unlocked, () => ShowHero(h));
            }
        }

        private void PopulatePets()
        {
            var grid = Grid($"Pets  ·  {PetService.OwnedCount}/{PetService.MaxOwnedPets} owned");
            foreach (var pet in PetService.GetSortedPets())
            {
                var p = pet;
                bool unlocked = PetService.IsUnlocked(pet);
                Tile(grid, UIIcons.Pet(pet.Id), PetColor(pet),
                    unlocked ? $"Lv {PetService.GetLevel(pet)}" : "", pet.NameOrId, PetService.IsActive(pet), !unlocked, () => ShowPet(p));
            }
        }

        /// <summary>Premium pets are framed gold; others in their own colour (a leafy green when they have none).</summary>
        private static Color PetColor(PetDefinition pet)
        {
            if (pet.IsPremium) return RarityColors.Legendary;
            var c = pet.PlaceholderColor;
            return c.r + c.g + c.b > 2.7f ? new Color(0.5f, 0.8f, 0.45f) : c;
        }

        private static Sprite WeaponIcon(WeaponDefinition weapon) => weapon.Icon != null ? weapon.Icon : UIIcons.Weapon(weapon.Id);
        private static Sprite GearIcon(EquipmentDefinition item) => item.Icon != null ? item.Icon : UIIcons.Equipment(item.Id);

        // ------------------------------------------------------------------ detail popup

        private void BuildPopup()
        {
            var dim = UIBuilder.Image(Root, "Popup", new Color(0f, 0f, 0f, 0.65f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(HidePopup); // tap outside to close
            _popup = dim.gameObject;

            var card = UIStyle.Sprite(dim.transform, "Card", "panel_wood_bg", UITheme.Panel);
            card.color = new Color(0.2f, 0.14f, 0.1f, 0.98f);
            var cardRt = card.rectTransform;
            UIStyle.Place(cardRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(940f, 900f));
            card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // swallow taps on the card

            _popupFrame = UIStyle.Sprite(cardRt, "IconFrame", "panel_wood_panel", UITheme.ButtonSecondary);
            _popupFrame.raycastTarget = false;
            UIStyle.Place(_popupFrame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(220f, 220f));
            _popupIcon = UIBuilder.Image(_popupFrame.transform, "Icon", Color.white);
            _popupIcon.raycastTarget = false;
            _popupIcon.preserveAspect = true;
            UIBuilder.Stretch(_popupIcon.rectTransform, 30f);

            _popupName = UIStyle.Label(cardRt, "", 46f);
            UIStyle.Place(_popupName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -276f), new Vector2(880f, 60f));
            _popupSub = UIStyle.Label(cardRt, "", 28f, UIStyle.Cream, TextAlignmentOptions.Center, 0.22f);
            UIStyle.Place(_popupSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(880f, 40f));
            _popupBody = UIBuilder.Text(cardRt, "", UITheme.SmallSize + 2f, TextAlignmentOptions.Top, UITheme.Text);
            _popupBody.richText = true;
            UIStyle.Place(_popupBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(840f, 300f));

            _popupActions = UIBuilder.Horizontal(cardRt, "Actions", 16f).GetComponent<RectTransform>();
            ((HorizontalLayoutGroup)_popupActions.GetComponent<HorizontalLayoutGroup>()).childAlignment = TextAnchor.MiddleCenter;
            UIStyle.Place(_popupActions, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(880f, 110f));

            var close = UIStyle.Frame(cardRt, "Close", "button_tint", HidePopup, new Color(0.75f, 0.3f, 0.28f));
            close.image.color = new Color(0.75f, 0.3f, 0.28f);
            UIStyle.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(84f, 84f));
            var x = UIStyle.Label(close.transform, "X", 44f);
            UIBuilder.Stretch(x.rectTransform);

            _popup.SetActive(false);
        }

        private void HidePopup()
        {
            _popupRefresh = null;
            if (_popup != null) _popup.SetActive(false);
        }

        /// <summary>Fills the card; <paramref name="actions"/> adds its buttons. Re-run after every action so it stays current.</summary>
        private void ShowCard(Sprite icon, Color color, string name, string sub, string body, System.Action<RectTransform> actions)
        {
            _popup.SetActive(true);
            _popup.transform.SetAsLastSibling();
            _popupIcon.sprite = icon;
            _popupIcon.enabled = icon != null;
            _popupFrame.color = Color.Lerp(Color.white, color, 0.75f);
            _popupName.text = name;
            _popupName.color = Color.Lerp(Color.white, color, 0.6f);
            _popupSub.text = sub;
            _popupBody.text = body;
            UIBuilder.ClearChildren(_popupActions);
            actions(_popupActions);
        }

        private Button Action(RectTransform row, string label, UnityEngine.Events.UnityAction onClick, Color? color = null, float width = 300f)
        {
            var button = UIBuilder.SmallButton(row, label, onClick, color, width);
            return button;
        }

        private void AfterAction(bool ok, string error)
        {
            if (ok) Sfx.Play(AudioCueIds.UiUpgrade);
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                if (!string.IsNullOrEmpty(error)) UIScreenNavigator.Instance.Toast(error);
            }
            Refresh(); // showcase, grid and (via _popupRefresh) this card
        }

        private void ShowWeapon(WeaponDefinition weapon)
        {
            _popupRefresh = () => ShowWeapon(weapon);
            int level = ForgeService.GetLevel(weapon.Id);
            string tier = ForgeService.TierName(ForgeService.GetTier(weapon.Id));
            bool equipped = InventoryService.Data.EquippedWeaponId == weapon.Id;
            string body = $"Damage <color=#FFD24D>{weapon.GetDamage(level):0.#}</color>   ·   {weapon.GetAttacksPerSecond(level):0.##} attacks/s   ·   Range {weapon.Range:0.#}";
            if (!string.IsNullOrEmpty(weapon.Description)) body += $"\n\n{weapon.Description}";
            body += "\n\n<color=#AAAAB5>Reforge to a higher tier at the Forge in your village.</color>";
            ShowCard(WeaponIcon(weapon), RarityColors.For(weapon.Rarity), DefinitionNames.Of(weapon),
                $"{(string.IsNullOrEmpty(tier) ? "" : tier + " · ")}{weapon.Rarity} weapon · Lv {level}", body, row =>
                {
                    var equip = Action(row, equipped ? "Equipped" : "Equip", () => AfterAction(InventoryService.EquipWeapon(weapon.Id), null), UITheme.ButtonSecondary, 240f);
                    if (equipped) UIBuilder.SetEnabled(equip, false);
                    var blocker = ForgeService.CheckUpgrade(weapon);
                    if (blocker == ForgeBlocker.MaxLevel) return;
                    var up = Action(row, $"Lv up {ForgeService.UpgradePrice(weapon)}", () =>
                    {
                        bool ok = ForgeService.TryUpgrade(weapon, out var b);
                        AfterAction(ok, ok ? null : ForgeService.DescribeBlocker(b, weapon));
                    }, null, 360f);
                    if (blocker != ForgeBlocker.None) UIBuilder.SetEnabled(up, false);
                });
        }

        private void ShowGear(EquipmentDefinition item)
        {
            _popupRefresh = () => ShowGear(item);
            var inv = InventoryService.Data;
            bool equipped = inv.IsEquipmentEquipped(item.Id);
            int count = 0;
            foreach (var stack in inv.Equipment)
                if (stack != null && stack.Id == item.Id) count = stack.Count;
            string bonuses = item.DescribeBonuses("\n");
            string body = (string.IsNullOrEmpty(bonuses) ? "" : $"<color=#9CFF8A>{bonuses}</color>") +
                          (string.IsNullOrEmpty(item.Description) ? "" : $"\n\n{item.Description}") +
                          "\n\n<color=#AAAAB5>Spare copies are material for reforging weapons at the Forge.</color>";
            ShowCard(GearIcon(item), RarityColors.For(item.Rarity), DefinitionNames.Of(item), $"{item.Rarity} gear · owned ×{count}", body, row =>
            {
                Action(row, equipped ? "Unequip" : "Equip", () =>
                {
                    var result = equipped ? InventoryService.UnequipEquipment(item.Id) : InventoryService.EquipEquipment(item.Id);
                    AfterAction(result == EquipResult.Ok, InventoryService.DescribeEquipResult(result));
                }, equipped ? UITheme.ButtonSecondary : (Color?)null, 300f);
            });
        }

        private void ShowHero(HeroDefinition hero)
        {
            if (hero == null) return;
            _popupRefresh = () => ShowHero(hero);
            bool unlocked = HeroService.IsUnlocked(hero);
            int level = HeroService.GetLevel(hero);
            string body = (string.IsNullOrEmpty(hero.RoleSummary) ? hero.Description : hero.RoleSummary) +
                          $"\n\n<color=#9CFF8A>{hero.GetStatsAtLevel(Mathf.Max(1, level)).Describe("\n")}</color>" +
                          (hero.SignatureWeapon != null ? $"\n\nWeapon: {hero.SignatureWeapon.DisplayName}" : "");
            string sub = unlocked ? $"Lv {level}/{HeroService.GetLevelCap(hero)}{(HeroService.IsSelected(hero) ? "  ·  Selected" : "")}" : "Locked";
            ShowCard(UIIcons.Hero(hero.Id), hero.IsPremium ? RarityColors.Legendary : hero.ThemeColor, hero.NameOrId, sub, body, row =>
            {
                if (!unlocked)
                {
                    var check = HeroService.CheckUnlock(hero);
                    var unlock = Action(row, $"Unlock {new Price(hero.UnlockCurrency, hero.UnlockCost)}", () =>
                    {
                        var r = HeroService.TryUnlock(hero);
                        AfterAction(r == HeroActionResult.Success, HeroService.Describe(r, hero));
                    }, null, 420f);
                    if (check != HeroActionResult.Success)
                    {
                        UIBuilder.SetEnabled(unlock, false);
                        _popupBody.text += $"\n\n<color=#F25A5A>{HeroService.Describe(check, hero)}</color>";
                    }
                    return;
                }
                var select = Action(row, HeroService.IsSelected(hero) ? "Selected" : "Select", () =>
                {
                    var r = HeroService.TrySelect(hero);
                    AfterAction(r == HeroActionResult.Success, HeroService.Describe(r, hero));
                }, UITheme.ButtonSecondary, 240f);
                if (HeroService.IsSelected(hero)) UIBuilder.SetEnabled(select, false);
                var upgradeCheck = HeroService.CheckUpgrade(hero);
                var upgrade = Action(row, upgradeCheck == HeroActionResult.MaxLevel ? "Max level" : $"Upgrade {HeroService.GetUpgradeCost(hero)} Coins", () =>
                {
                    var r = HeroService.TryUpgrade(hero);
                    AfterAction(r == HeroActionResult.Success, HeroService.Describe(r, hero));
                }, null, 420f);
                if (upgradeCheck != HeroActionResult.Success)
                {
                    UIBuilder.SetEnabled(upgrade, false);
                    if (upgradeCheck == HeroActionResult.LevelCapReached) UIBuilder.SetLabel(upgrade, "Needs a bigger Dojo");
                }
            });
        }

        private void ShowPet(PetDefinition pet)
        {
            _popupRefresh = () => ShowPet(pet);
            bool unlocked = PetService.IsUnlocked(pet);
            bool active = PetService.IsActive(pet);
            string body = string.IsNullOrEmpty(pet.AbilitySummary) ? pet.Description : pet.AbilitySummary;
            string sub = unlocked ? $"Lv {PetService.GetLevel(pet)}/{PetService.GetLevelCap(pet)}{(active ? "  ·  Joins your runs" : "")}" : "Locked";
            ShowCard(UIIcons.Pet(pet.Id), PetColor(pet), pet.NameOrId, sub, body, row =>
            {
                if (!unlocked)
                {
                    var check = PetService.CheckUnlock(pet);
                    var unlock = Action(row, $"Unlock {new Price(pet.UnlockCurrency, pet.UnlockCost)}", () =>
                    {
                        var r = PetService.TryUnlock(pet);
                        AfterAction(r == PetActionResult.Success, PetService.Describe(r, pet));
                    }, null, 420f);
                    if (check != PetActionResult.Success)
                    {
                        UIBuilder.SetEnabled(unlock, false);
                        _popupBody.text += $"\n\n<color=#F25A5A>{PetService.Describe(check, pet)}</color>";
                    }
                    return;
                }
                Action(row, active ? "Leave home" : "Take along", () =>
                {
                    if (active)
                    {
                        PetService.ClearActive();
                        AfterAction(true, null);
                        return;
                    }
                    var r = PetService.TrySetActive(pet);
                    AfterAction(r == PetActionResult.Success, PetService.Describe(r, pet));
                }, UITheme.ButtonSecondary, 280f);
                var upgradeCheck = PetService.CheckUpgrade(pet);
                var upgrade = Action(row, $"Upgrade {PetService.GetUpgradeCost(pet)} Coins", () =>
                {
                    var r = PetService.TryUpgrade(pet);
                    AfterAction(r == PetActionResult.Success, PetService.Describe(r, pet));
                }, null, 420f);
                if (upgradeCheck != PetActionResult.Success) UIBuilder.SetEnabled(upgrade, false);
            });
        }
    }
}
