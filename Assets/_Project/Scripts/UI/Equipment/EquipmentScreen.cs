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
using NinjaVillage.Systems.Mounts;
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
    /// them — weapon and gear slots on the left and right, the pet in the last slot, and the hero on their mount if
    /// they ride one — then Gear / Heroes / Pets / Mounts tabs over a grid of tiles framed in their grade's colour (Common → Rare → Elite → Epic → Legendary), each with
    /// a small type badge (weapon, ring, amulet, armour, helmet, talisman, pet). Three copies of a grade merge into the
    /// next ("Merge all", or per item). Tapping a tile or slot opens its card with the right actions (Equip, Level
    /// up, Merge, Select, Take along, Upgrade, Unlock). Replaces separate Gear, Heroes and
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
            Mounts,
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
        private Image _mountImage;
        private UIImageAnimator _mountAnimator;
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
            public Image Frame, Icon, Badge;
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

            // The mount they ride, drawn over the rider's legs like in battle; tapping it opens its card.
            _mountImage = UIBuilder.Image(heroButton.transform, "Mount", Color.white);
            _mountImage.preserveAspect = true;
            UIStyle.Place(_mountImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 200f));
            var mountButton = _mountImage.gameObject.AddComponent<Button>();
            mountButton.transition = Selectable.Transition.None;
            mountButton.onClick.AddListener(() =>
            {
                var mount = MountService.Active;
                if (mount != null) ShowMount(mount);
            });
            _mountAnimator = _mountImage.gameObject.AddComponent<UIImageAnimator>();
            _mountImage.gameObject.SetActive(false);

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
                slot.Badge = TypeBadge(slot.Button.transform, 46f);
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
            foreach (var (tab, icon, label) in new[]
                     { (Tab.Gear, "menu_gear", "Gear"), (Tab.Heroes, "menu_heroes", "Heroes"), (Tab.Pets, "menu_pets", "Pets"), (Tab.Mounts, "icon_mount_horse_brown", "Mounts") })
            {
                var t = tab;
                var button = UIStyle.Frame(row.transform, label, "panel_wood_panel", () => SelectTab(t), UITheme.ButtonSecondary);
                var image = UIBuilder.Image(button.transform, "Icon", Color.white, UIArt.Get(icon));
                image.raycastTarget = false;
                image.preserveAspect = true;
                UIStyle.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(68f, 68f));
                var text = UIStyle.Label(button.transform, label, 32f);
                UIStyle.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 0f), new Vector2(170f, 60f));
                _tabButtons[tab] = button;
            }
        }

        // ------------------------------------------------------------------ behaviour

        protected override void OnShown()
        {
            bool switched = _tab != _pendingTab;
            _tab = _pendingTab;
            _pendingTab = Tab.Gear;
            HidePopup();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
            if (switched) Refresh(); // the screen refreshed before this; redraw on the requested tab
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
                case Tab.Mounts: PopulateMounts(); break;
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
            var weaponGrade = weapon != null ? InventoryService.WeaponGrade(weapon.Id) : ItemGrade.Common;
            SetSlot(_slots[0], weapon != null ? WeaponIcon(weapon) : null, weapon != null ? GradeColors.For(weaponGrade) : (Color?)null,
                weapon != null ? $"{weaponGrade} · Lv {ForgeService.GetLevel(weapon.Id)}" : "Weapon", UIIcons.WeaponType);

            int open = InventoryService.EquipmentSlots;
            int[] gearSlots = { 1, 2, 3, 4 };
            for (int g = 0; g < GearSlots; g++)
            {
                var slot = _slots[gearSlots[g]];
                if (g >= open) { SetSlot(slot, null, null, "<color=#F2A0A0>Castle</color>", null, locked: true); continue; }
                var item = g < inv.EquippedEquipmentIds.Count ? InventoryService.GetEquipment(inv.EquippedEquipmentIds[g]) : null;
                var grade = item != null ? InventoryService.GearGrade(item) : ItemGrade.Common;
                SetSlot(slot, item != null ? GearIcon(item) : null, item != null ? GradeColors.For(grade) : (Color?)null,
                    item != null ? grade.ToString() : "Empty", item != null ? UIIcons.GearType(item.Kind) : null);
            }

            var pet = PetService.GetActive();
            SetSlot(_slots[5], pet != null ? UIIcons.Pet(pet.Id) : null, pet != null ? PetColor(pet) : (Color?)null,
                pet != null ? $"Lv {PetService.GetLevel(pet)}" : "Pet", UIIcons.PetType);
        }

        private static void SetSlot(Slot slot, Sprite icon, Color? color, string label, Sprite badge, bool locked = false)
        {
            slot.Icon.sprite = icon;
            slot.Icon.enabled = icon != null;
            slot.Frame.color = color.HasValue ? Color.Lerp(Color.white, color.Value, 0.75f) : locked ? new Color(0.3f, 0.26f, 0.24f) : new Color(0.55f, 0.47f, 0.4f);
            slot.Label.text = label;
            slot.Label.color = color.HasValue ? Color.Lerp(Color.white, color.Value, 0.55f) : Color.white;
            slot.Badge.sprite = badge;
            slot.Badge.enabled = badge != null && icon != null;
        }

        /// <summary>The small type icon in a tile's or slot's top-left corner (weapon, ring, amulet, armour, pet...).</summary>
        private static Image TypeBadge(Transform parent, float size)
        {
            var badge = UIBuilder.Image(parent, "Type", Color.white);
            badge.raycastTarget = false;
            badge.preserveAspect = true;
            UIStyle.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 6f), new Vector2(size, size));
            badge.enabled = false;
            return badge;
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
            var mount = MountService.Active;
            if (mount != null && (mount.Frames.Length == 0 || mount.Frames[0] == null)) mount = null;
            string shown = mount != null ? key + "|" + mount.Id : key;
            if (shown == _shownHeroKey) return;
            var set = CharacterSpriteLibrary.Find(key);
            if (set == null) return;
            _shownHeroKey = shown;
            var frames = set.Frames(CharacterAnim.Idle);
            var sprite = set.DefaultSprite;
            _heroAnimator.Play(frames.Length > 0 ? frames : new[] { sprite }, set.Fps(CharacterAnim.Idle), frames.Length > 1 || mount != null ? 0f : 12f);
            LayoutRider(sprite, mount);
        }

        /// <summary>
        /// The hero alone, or seated on <paramref name="mount"/> exactly as <see cref="Gameplay.Mounts.MountVisual"/> seats
        /// them in battle (mount's feet on the hero's feet, rider at the mount's rider offset), a bit smaller so it fits.
        /// </summary>
        private void LayoutRider(Sprite heroSprite, MountDefinition mount)
        {
            // Pack heroes are 16 px cells, the Beast Ninja 32 px: size by cell so bodies match.
            float cells = heroSprite != null ? heroSprite.rect.width / 128f : 1f;
            _mountImage.gameObject.SetActive(mount != null);
            if (mount == null)
            {
                _heroImage.rectTransform.sizeDelta = Vector2.one * Mathf.Min(320f * cells, 640f);
                _heroAnimator.SetPosition(Vector2.zero);
                return;
            }
            float px = cells > 1.5f ? 10f : 15f;              // UI px per pack pixel (20 on foot)
            const float feet = -175f;
            float heroSize = 16f * px * cells;
            _heroImage.rectTransform.sizeDelta = Vector2.one * heroSize;
            var first = mount.Frames[0];
            var mountSize = first.rect.size / 8f * px;        // textures are 8x the pack art
            _mountImage.rectTransform.sizeDelta = mountSize;
            _mountAnimator.Play(mount.Frames, mount.Fps * 0.5f);
            _mountAnimator.SetPosition(new Vector2(0f, feet + mountSize.y * 0.5f));
            var seat = mount.RiderOffset / 0.075f * px;       // world units → pack pixels → UI px
            _heroAnimator.SetPosition(new Vector2(seat.x, feet + heroSize * 0.5f + seat.y));
        }

        private void OnSlot(int index)
        {
            Sfx.Play(AudioCueIds.UiClick);
            var inv = InventoryService.Data;
            if (index == 0)
            {
                var weapon = InventoryService.GetWeapon(inv.EquippedWeaponId);
                if (weapon != null) ShowWeapon(weapon, InventoryService.WeaponGrade(weapon.Id));
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
            if (item != null) ShowGear(item, InventoryService.GearGrade(item));
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

        /// <summary>
        /// A grade/rarity-framed tile: icon, a small type badge (top-left), level/count (bottom-right), a green E when
        /// equipped/selected (top-right), a green MERGE tag when three of it can merge, dimmed when locked.
        /// </summary>
        private static void Tile(RectTransform grid, Sprite icon, Color frame, string corner, string caption, bool equipped, bool locked,
            UnityEngine.Events.UnityAction onClick, Sprite badge = null, bool mergeable = false, bool special = false)
        {
            var button = UIStyle.Frame(grid, "Tile", "panel_wood_panel", onClick, frame);
            button.image.color = locked ? new Color(0.35f, 0.3f, 0.28f) : Color.Lerp(Color.white, frame, 0.8f);
            var image = UIBuilder.Image(button.transform, "Icon", locked ? new Color(0.25f, 0.2f, 0.2f, 0.9f) : Color.white, icon);
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(112f, 112f));
            image.enabled = icon != null;

            if (badge != null)
            {
                var type = TypeBadge(button.transform, 48f);
                type.sprite = badge;
                type.enabled = true;
            }

            var cornerText = UIStyle.Label(button.transform, corner, 24f, Color.white, badge != null ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.TopLeft, 0.28f);
            if (badge != null) UIStyle.Place(cornerText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 34f), new Vector2(150f, 30f));
            else UIStyle.Place(cornerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(150f, 30f));
            var captionText = UIStyle.Label(button.transform, caption, 20f, UIStyle.Cream, TextAlignmentOptions.Center, 0.25f);
            captionText.enableAutoSizing = true;
            captionText.fontSizeMin = 14f;
            captionText.fontSizeMax = 20f;
            UIStyle.Place(captionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(166f, 28f));

            if (equipped)
            {
                var e = UIBuilder.Image(button.transform, "Equipped", UITheme.Positive, Core.Utilities.GeneratedSprites.Circle);
                e.raycastTarget = false;
                UIStyle.Place(e.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-18f, -18f), new Vector2(40f, 40f));
                var letter = UIStyle.Label(e.transform, "E", 24f);
                UIBuilder.Stretch(letter.rectTransform);
            }

            if (special)
            {
                // S-class: a gold "S" on red at the top centre.
                var s = UIBuilder.Image(button.transform, "SClass", new Color(0.8f, 0.12f, 0.12f), Core.Utilities.GeneratedSprites.Circle);
                s.raycastTarget = false;
                UIStyle.Place(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(40f, 40f));
                var letter = UIStyle.Label(s.transform, "S", 28f, new Color(1f, 0.82f, 0.3f), TextAlignmentOptions.Center, 0.3f);
                UIBuilder.Stretch(letter.rectTransform);
            }

            if (mergeable)
            {
                var tag = UIBuilder.Image(button.transform, "Merge", new Color(0.2f, 0.62f, 0.25f, 0.95f));
                tag.raycastTarget = false;
                UIStyle.Place(tag.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 36f), new Vector2(92f, 28f));
                var text = UIStyle.Label(tag.transform, "MERGE", 18f);
                UIBuilder.Stretch(text.rectTransform);
            }
        }

        private void PopulateGear()
        {
            var inv = InventoryService.Data;
            var top = UIBuilder.Horizontal(_list, "GearActions", 14f);
            UIBuilder.SetPreferredSize(top, -1f, 100f);
            var crates = UIBuilder.Button(top.transform, "Open crates", () =>
            {
                Sfx.Play(AudioCueIds.UiClick);
                UIScreenNavigator.Instance.Show(ScreenIds.Store); // crates are sold in the Shop
            }, UITheme.ButtonSecondary, 100f, UITheme.BodySize);
            crates.name = "OpenCrates";
            int sets = MergeService.MergeableCount();
            if (sets > 0)
            {
                var all = UIBuilder.Button(top.transform, $"Merge all  ·  {sets} ready", MergeAll, UITheme.Gold, 100f, UITheme.BodySize);
                all.name = "MergeAll";
            }

            // One tile per weapon per grade you own copies of, best grade first; the E marks the grade in use.
            var weapons = Grid("Weapons");
            foreach (var entry in inv.Weapons)
            {
                var weapon = entry != null ? InventoryService.GetWeapon(entry.Id) : null;
                if (weapon == null) continue;
                int best = inv.BestWeaponGrade(weapon.Id);
                for (int g = (int)GradeRules.Max; g >= 0; g--)
                {
                    int copies = inv.GetWeaponCopies(weapon.Id, g);
                    if (copies <= 0) continue;
                    var grade = (ItemGrade)g;
                    string corner = $"Lv {ForgeService.GetLevel(weapon.Id)}" + (copies > 1 ? $" ×{copies}" : "");
                    Tile(weapons, WeaponIcon(weapon), GradeColors.For(grade), corner, DefinitionNames.Of(weapon),
                        inv.EquippedWeaponId == weapon.Id && g == best, false, () => ShowWeapon(weapon, grade),
                        UIIcons.WeaponType, MergeService.CheckWeapon(weapon.Id, grade) == MergeResult.Merged && copies >= GradeRules.MergeCount,
                        weapon.IsSpecial);
                }
            }

            var gear = Grid($"Gear  ·  {inv.EquippedEquipmentIds.Count}/{InventoryService.EquipmentSlots} equipped");
            int shown = 0;
            var stacks = new List<OwnedEquipment>(inv.Equipment);
            stacks.RemoveAll(st => st == null || st.Count <= 0 || InventoryService.GetEquipment(st.Id) == null);
            stacks.Sort((a, b) => a.Id != b.Id ? string.CompareOrdinal(a.Id, b.Id) : b.Grade.CompareTo(a.Grade));
            foreach (var stack in stacks)
            {
                var item = InventoryService.GetEquipment(stack.Id);
                var grade = GradeRules.Clamp(stack.Grade);
                shown++;
                Tile(gear, GearIcon(item), GradeColors.For(grade), stack.Count > 1 ? $"×{stack.Count}" : "",
                    DefinitionNames.Of(item), inv.IsEquipmentEquipped(item.Id) && stack.Grade == inv.BestEquipmentGrade(item.Id), false,
                    () => ShowGear(item, grade), UIIcons.GearType(item.Kind), stack.Count >= GradeRules.MergeCount && !GradeRules.IsMax(grade),
                    item.IsSpecial);
            }
            if (shown == 0)
                UIBuilder.Text(_list, "No gear yet — enemies, chests and the Market drop it.", UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }

        private void MergeAll()
        {
            int merges = MergeService.MergeAll();
            Sfx.Play(merges > 0 ? AudioCueIds.RewardClaim : AudioCueIds.UiError);
            UIScreenNavigator.Instance.Toast(merges > 0 ? $"{merges} merge{(merges == 1 ? "" : "s")} done — your gear got stronger!" : "Nothing to merge yet.");
            Refresh();
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
                    unlocked ? $"Lv {PetService.GetLevel(pet)}" : "", pet.NameOrId, PetService.IsActive(pet), !unlocked, () => ShowPet(p), UIIcons.PetType);
            }
        }

        private void PopulateMounts()
        {
            var active = MountService.Active;
            var grid = Grid(active != null ? $"Mounts  ·  riding the {active.NameOrId}" : "Mounts  ·  on foot");
            foreach (var mount in MountService.GetSorted())
            {
                var m = mount;
                bool owned = MountService.IsOwned(mount);
                Tile(grid, MountIcon(mount), MountColor(mount), owned ? $"Lv {MountService.Level(mount)}" : "", mount.NameOrId,
                    MountService.IsActive(mount), !owned, () => ShowMount(m), UIIcons.MountType, special: mount.IsSpecial);
            }
            UIBuilder.Text(_list, "Your hero rides the mount into every battle and around the village; its bonuses apply to every run.",
                UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }

        /// <summary>S-class mounts are framed Legendary gold, gem mounts Epic, coin mounts Rare.</summary>
        private static Color MountColor(MountDefinition mount) => GradeColors.For(
            mount.IsSpecial ? ItemGrade.Legendary : mount.UnlockPrice.Currency == CurrencyType.Gems ? ItemGrade.Epic : ItemGrade.Rare);

        private static Sprite MountIcon(MountDefinition mount)
        {
            var icon = UIIcons.Mount(mount.Id);
            return icon != null ? icon : mount.Icon != null ? mount.Icon : mount.Frames.Length > 0 ? mount.Frames[0] : null;
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

        /// <summary>The Merge button for a card: "Merge 3 → Rare", or "Merge 2 + 4 Iron Bars" when bars make up the third copy.</summary>
        private void MergeButton(RectTransform row, ItemGrade grade, int copies, MergeResult check, System.Func<MergeResult> merge, string name)
        {
            if (GradeRules.IsMax(grade)) return;
            var next = GradeRules.Next(grade);
            var (bar, amount) = GradeRules.BarsFor(next);
            var barGoods = bar != null ? NinjaVillage.Systems.Farm.GoodsService.Get(bar) : null;
            string label = copies >= GradeRules.MergeCount || check != MergeResult.Merged || barGoods == null
                ? $"Merge 3 → {next}"
                : $"Merge 2 + {amount} {barGoods.NameOrId}s";
            var button = Action(row, label, () =>
            {
                var result = merge();
                if (result == MergeResult.Merged) UIScreenNavigator.Instance.Toast($"{name} is now {next}!");
                AfterAction(result == MergeResult.Merged, MergeService.Describe(result, grade));
            }, UITheme.Gold, 380f);
            if (check != MergeResult.Merged) UIBuilder.SetEnabled(button, false);
        }

        private static string MergeHint(ItemGrade grade, int copies)
        {
            if (GradeRules.IsMax(grade)) return "<color=#FFD24D>Legendary — the best grade there is.</color>";
            var next = GradeRules.Next(grade);
            var (bar, amount) = GradeRules.BarsFor(next);
            var barGoods = bar != null ? NinjaVillage.Systems.Farm.GoodsService.Get(bar) : null;
            return $"<color=#AAAAB5>Merge {GradeRules.MergeCount} {grade} copies into 1 {GradeColors.Colorize(next.ToString(), next)}" +
                   (barGoods != null ? $" (or 2 + {amount} {barGoods.NameOrId}s from the mine)" : "") + $". You have {copies}.</color>";
        }

        private void ShowWeapon(WeaponDefinition weapon, ItemGrade grade)
        {
            _popupRefresh = () => ShowWeapon(weapon, grade);
            var inv = InventoryService.Data;
            int level = ForgeService.GetLevel(weapon.Id);
            int copies = inv.GetWeaponCopies(weapon.Id, (int)grade);
            bool equipped = inv.EquippedWeaponId == weapon.Id;
            bool inUse = equipped && InventoryService.WeaponGrade(weapon.Id) == grade;
            float bonus = GradeRules.WeaponAttackBonus(grade);
            string body = $"Damage <color=#FFD24D>{weapon.GetDamage(level):0.#}</color>   ·   {weapon.GetAttacksPerSecond(level):0.##} attacks/s   ·   Range {weapon.Range:0.#}";
            body += bonus > 0f ? $"\n<color=#9CFF8A>{grade}: +{bonus * 100f:0}% attack</color>" : $"\n<color=#AAAAB5>{grade}: no grade bonus yet</color>";
            if (!GradeRules.IsMax(grade))
                body += $"\nNext: {GradeColors.Colorize(GradeRules.Next(grade).ToString(), GradeRules.Next(grade))} +{GradeRules.WeaponAttackBonus(GradeRules.Next(grade)) * 100f:0}% attack";
            body += "\n\n" + MergeHint(grade, copies) + (weapon.IsSpecial
                ? "\n<color=#AAAAB5>More copies: Surprise Boxes.</color>"
                : "\n<color=#AAAAB5>More copies: Forge, battle chests and supply crates.</color>");
            ShowCard(WeaponIcon(weapon), GradeColors.For(grade), $"{(weapon.IsSpecial ? "S · " : "")}{grade} {DefinitionNames.Of(weapon)}",
                $"{(weapon.IsSpecial ? "S-class weapon" : "Weapon")}  ·  Lv {level}  ·  ×{copies}{(inUse ? "  ·  In use" : "")}", body, row =>
                {
                    var equip = Action(row, equipped ? "Equipped" : "Equip", () => AfterAction(InventoryService.EquipWeapon(weapon.Id), null), UITheme.ButtonSecondary, 200f);
                    if (equipped) UIBuilder.SetEnabled(equip, false);
                    var blocker = ForgeService.CheckUpgrade(weapon);
                    if (blocker != ForgeBlocker.MaxLevel)
                    {
                        var up = Action(row, $"Lv up {ForgeService.UpgradePrice(weapon)}", () =>
                        {
                            bool ok = ForgeService.TryUpgrade(weapon, out var b);
                            AfterAction(ok, ok ? null : ForgeService.DescribeBlocker(b, weapon));
                        }, null, 260f);
                        if (blocker != ForgeBlocker.None) UIBuilder.SetEnabled(up, false);
                    }
                    MergeButton(row, grade, copies, MergeService.CheckWeapon(weapon.Id, grade), () => MergeService.MergeWeapon(weapon.Id, grade), DefinitionNames.Of(weapon));
                });
        }

        private void ShowGear(EquipmentDefinition item, ItemGrade grade)
        {
            _popupRefresh = () => ShowGear(item, grade);
            var inv = InventoryService.Data;
            bool equipped = inv.IsEquipmentEquipped(item.Id);
            int copies = inv.GetEquipmentCount(item.Id, (int)grade);
            var native = InventoryService.NativeGrade(item);
            string bonuses = item.DescribeBonuses("\n", GradeRules.StatScale(native, grade));
            string body = (string.IsNullOrEmpty(bonuses) ? "" : $"<color=#9CFF8A>{bonuses}</color>");
            if (!GradeRules.IsMax(grade))
            {
                var next = GradeRules.Next(grade);
                body += $"\nNext: {GradeColors.Colorize(next.ToString(), next)} {item.DescribeBonuses(" · ", GradeRules.StatScale(native, next))}";
            }
            body += "\n\n" + MergeHint(grade, copies);
            if (equipped && grade != InventoryService.GearGrade(item))
                body += $"\n<color=#AAAAB5>Equipped pieces use your best grade ({InventoryService.GearGrade(item)}).</color>";
            ShowCard(GearIcon(item), GradeColors.For(grade), $"{(item.IsSpecial ? "S · " : "")}{grade} {DefinitionNames.Of(item)}",
                $"{(item.IsSpecial ? "S-class " : "")}{item.Kind}  ·  ×{copies}", body, row =>
            {
                Action(row, equipped ? "Unequip" : "Equip", () =>
                {
                    var result = equipped ? InventoryService.UnequipEquipment(item.Id) : InventoryService.EquipEquipment(item.Id);
                    AfterAction(result == EquipResult.Ok, InventoryService.DescribeEquipResult(result));
                }, equipped ? UITheme.ButtonSecondary : (Color?)null, 260f);
                MergeButton(row, grade, copies, MergeService.CheckGear(item.Id, grade), () => MergeService.MergeGear(item.Id, grade), DefinitionNames.Of(item));
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

        private void ShowMount(MountDefinition mount)
        {
            _popupRefresh = () => ShowMount(mount);
            bool owned = MountService.IsOwned(mount);
            bool riding = MountService.IsActive(mount);
            int level = MountService.Level(mount);
            string body = mount.Description + $"\n\n<color=#9CFF8A>{MountService.DescribeBonuses(mount, Mathf.Max(1, level), "\n")}</color>";
            if (owned && level < MountRules.MaxLevel)
                body += $"\n<color=#AAAAB5>Lv {level + 1}: {MountService.DescribeBonuses(mount, level + 1)}</color>";
            if (mount.IsSpecial)
                body += owned ? "\n\n<color=#AAAAB5>More from Surprise Boxes level it up.</color>" : "\n\n<color=#FFD24D>S-class: found only in Surprise Boxes.</color>";
            string sub = owned ? $"Lv {level}/{MountRules.MaxLevel}{(riding ? "  ·  Riding" : "")}" : "Locked";
            ShowCard(MountIcon(mount), MountColor(mount), $"{(mount.IsSpecial ? "S · " : "")}{mount.NameOrId}", sub, body, row =>
            {
                if (!owned)
                {
                    if (mount.IsSpecial)
                    {
                        Action(row, "Open crates", () =>
                        {
                            Sfx.Play(AudioCueIds.UiClick);
                            HidePopup();
                            UIScreenNavigator.Instance.Show(ScreenIds.Store); // crates are sold in the Shop
                        }, UITheme.Gold, 360f);
                        return;
                    }
                    var check = MountService.CheckUnlock(mount);
                    var unlock = Action(row, $"Unlock {mount.UnlockPrice}", () =>
                    {
                        var r = MountService.TryUnlock(mount);
                        if (r == MountResult.Success) UIScreenNavigator.Instance.Toast($"You ride the {mount.NameOrId}!");
                        AfterAction(r == MountResult.Success, MountService.Describe(r, mount));
                    }, null, 420f);
                    if (check != MountResult.Success)
                    {
                        UIBuilder.SetEnabled(unlock, false);
                        _popupBody.text += $"\n\n<color=#F25A5A>{MountService.Describe(check, mount)}</color>";
                    }
                    return;
                }
                Action(row, riding ? "Dismount" : "Ride", () =>
                {
                    if (riding) MountService.Dismount();
                    else MountService.Ride(mount);
                    AfterAction(true, null);
                }, UITheme.ButtonSecondary, 260f);
                var upgradeCheck = MountService.CheckUpgrade(mount);
                var upgrade = Action(row, upgradeCheck == MountResult.MaxLevel ? "Max level" : $"Upgrade {MountService.UpgradePrice(mount)}", () =>
                {
                    var r = MountService.TryUpgrade(mount);
                    AfterAction(r == MountResult.Success, MountService.Describe(r, mount));
                }, null, 420f);
                if (upgradeCheck != MountResult.Success) UIBuilder.SetEnabled(upgrade, false);
            });
        }
    }
}
