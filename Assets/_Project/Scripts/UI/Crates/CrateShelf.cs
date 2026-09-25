using System.Collections;
using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Crates;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Mounts;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Crates
{
    /// <summary>
    /// Supply crates, as a shelf in the Shop: a free Wooden Crate every day, Silver Crates and Surprise Boxes (the only
    /// source of S-class equipment and mounts, certain by the 30th box), each ×1 or ×10, with their odds shown. Opening
    /// plays a reveal over the whole screen: the chest shakes, bursts open in a flash with light rays, and the items pop
    /// out one by one in their grade's colour — S items with a gold "S". Every crate wears a "?" on its picture that
    /// shows what's inside (<see cref="ShowRates"/>): grade odds, the S-class chance and pity, and every item it can
    /// hold — S items, weapons and gear — with its picture and its own chance per box. The host screen adds the shelf with <see cref="Attach"/> and fills
    /// it from its Populate.
    /// </summary>
    public sealed class CrateShelf : MonoBehaviour
    {
        private static readonly Color Gold = new(1f, 0.82f, 0.3f);

        private UIScreen _host;

        private GameObject _overlay, _rates;
        private TextMeshProUGUI _ratesTitle;
        private RectTransform _ratesContent;
        private ScrollRect _ratesScroll;
        private Image _chest, _flash;
        private RectTransform _rays, _results;
        private TextMeshProUGUI _headline, _hint;
        private Coroutine _reveal;
        private bool _skip;
        private readonly List<CrateDrop> _lastDrops = new();

        /// <summary>The reveal is still playing (tests / taps skip it).</summary>
        public bool IsRevealing { get; private set; }
        public bool OverlayOpen => _overlay != null && _overlay.activeSelf;
        public IReadOnlyList<CrateDrop> LastDrops => _lastDrops;
        /// <summary>The drop-rates popup is showing.</summary>
        public bool RatesOpen => _rates != null && _rates.activeSelf;

        /// <summary>Adds a shelf (and its reveal overlay, over <paramref name="host"/>'s whole screen) to a built screen.</summary>
        public static CrateShelf Attach(UIScreen host)
        {
            var shelf = host.gameObject.AddComponent<CrateShelf>();
            shelf._host = host;
            shelf.BuildOverlay(host.Root);
            shelf.BuildRates(host.Root);
            return shelf;
        }

        // ------------------------------------------------------------------ list

        /// <summary>A "Supply crates" section: one card per crate with its odds and Open buttons.</summary>
        public void AddCrates(RectTransform content)
        {
            UIBuilder.SectionHeader(content, "Supply crates");
            UIBuilder.Text(content, "Weapons and gear at random grades. <color=#FFD24D>Surprise Boxes</color> hold S-class equipment and mounts! " +
                               "Tap <color=#7FB2FF>?</color> on a crate to see what's inside and the odds.",
                UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            foreach (var crate in CrateRules.Crates) CrateCard(content, crate);
        }

        private void CrateCard(RectTransform content, CrateKind crate)
        {
            string body = crate.Blurb + "\n" + CrateRules.DescribeOdds(crate);
            if (crate.Pity > 0) body += $"\n<color=#FFD24D>S-class certain within {CrateService.PityLeft(crate)} box{(CrateService.PityLeft(crate) == 1 ? "" : "es")}.</color>";
            var actions = UIBuilder.ActionCard(content, crate.Name, body, out _, out _, crate.Pity > 0 ? Gold : UITheme.Text, UIArt.Get(crate.ClosedIcon));
            RatesButton(actions.transform.parent, crate);
            if (CrateService.CanOpenFree(crate))
                UIBuilder.SmallButton(actions.transform, "Open FREE", () => Open(crate, 1, true), UITheme.Positive, 240f);
            var one = UIBuilder.SmallButton(actions.transform, $"Open {crate.Price}", () => Open(crate, 1, false), UITheme.Button, 260f);
            UIBuilder.SetEnabled(one, CrateService.Check(crate, 1, false) == CrateResult.Opened);
            var ten = UIBuilder.SmallButton(actions.transform, $"×{CrateRules.MultiOpen} {CrateService.PriceFor(crate, CrateRules.MultiOpen)}",
                () => Open(crate, CrateRules.MultiOpen, false), UITheme.Gold, 280f);
            UIBuilder.SetEnabled(ten, CrateService.Check(crate, CrateRules.MultiOpen, false) == CrateResult.Opened);
        }

        /// <summary>A round "?" on the crate's picture (the picture itself is tappable too) that shows what's inside and the odds.</summary>
        private void RatesButton(Transform card, CrateKind crate)
        {
            var tile = card.Find("Row/IconTile");
            if (tile == null) return;
            var tileButton = tile.gameObject.AddComponent<Button>();
            tileButton.transition = Selectable.Transition.None;
            tileButton.onClick.AddListener(() => ShowRates(crate));

            // White rim, blue disc, white "?" (children draw over their parent).
            var help = UIBuilder.Image(tile, "Rates", Color.white, Core.Utilities.GeneratedSprites.Circle);
            UIStyle.Place(help.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(12f, -12f), new Vector2(66f, 66f)); // top-left: clear of the title
            help.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowRates(crate));
            var disc = UIBuilder.Image(help.transform, "Disc", new Color(0.2f, 0.45f, 0.85f), Core.Utilities.GeneratedSprites.Circle);
            disc.raycastTarget = false;
            UIBuilder.Stretch(disc.rectTransform, 5f);
            var mark = UIStyle.Label(help.transform, "?", 44f, Color.white, TextAlignmentOptions.Center, 0.3f);
            UIBuilder.Stretch(mark.rectTransform);
        }

        // ------------------------------------------------------------------ drop rates

        private void BuildRates(RectTransform root)
        {
            var dim = UIBuilder.Image(root, "Rates", new Color(0f, 0f, 0f, 0.7f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseRates); // tap outside to close
            _rates = dim.gameObject;

            var card = UIStyle.Sprite(dim.transform, "Card", "panel_wood_bg", UITheme.Panel);
            card.color = new Color(0.2f, 0.14f, 0.1f, 0.98f);
            var cardRt = card.rectTransform;
            UIStyle.Place(cardRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1000f, 1200f)); // scrolls if the S list grows
            card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // swallow taps on the card

            _ratesTitle = UIStyle.Label(cardRt, "", 50f, Gold, TextAlignmentOptions.Center, 0.3f);
            UIStyle.Place(_ratesTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(800f, 70f));

            _ratesScroll = UIBuilder.ScrollList(cardRt, "Items", out _ratesContent, 16f);
            var scrollRt = (RectTransform)_ratesScroll.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(30f, 30f);
            scrollRt.offsetMax = new Vector2(-30f, -130f);

            var close = UIStyle.Frame(cardRt, "Close", "button_tint", CloseRates, new Color(0.75f, 0.3f, 0.28f));
            close.image.color = new Color(0.75f, 0.3f, 0.28f);
            UIStyle.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(84f, 84f));
            var x = UIStyle.Label(close.transform, "X", 44f);
            UIBuilder.Stretch(x.rectTransform);

            _rates.SetActive(false);
        }

        private static string Percent(float chance) => $"{chance * 100f:0.##}%";

        /// <summary>
        /// What's inside <paramref name="crate"/>: its grade odds, S-class chance and pity, the weapon/gear split, and every
        /// item it can hold — S items, weapons and gear — with its picture and its own chance per box (and whether you own
        /// it). Public so tests can drive it like a tap.
        /// </summary>
        public void ShowRates(CrateKind crate)
        {
            if (crate == null) return;
            Sfx.Play(AudioCueIds.UiClick);
            _rates.SetActive(true); // before filling: texts set up their materials when they wake
            _rates.transform.SetAsLastSibling();
            _ratesTitle.text = $"{crate.Name} — what's inside";
            UIBuilder.ClearChildren(_ratesContent);

            var pool = CrateService.SPool(crate);
            var weapons = CrateService.RegularWeapons();
            var gear = CrateService.RegularGear();
            float weaponChance = CrateService.RegularItemChance(crate, true);
            float gearChance = CrateService.RegularItemChance(crate, false);

            string text = $"<b>Grade of every item:</b> {CrateRules.DescribeOdds(crate)}";
            if (crate.Pity > 0 && pool.Count > 0)
            {
                int left = CrateService.PityLeft(crate);
                text += $"\n<color=#FFD24D>Your next S-class item is certain within {left} box{(left == 1 ? "" : "es")}.</color>";
            }
            text += $"\n\n<b>Each box holds one item.</b> The percentage under each picture is its chance per box";
            text += pool.Count > 0
                ? $" — S-class {Percent(crate.SChance)} (each S item {Percent(CrateService.SItemChance(crate))}), " +
                  $"weapons {Percent(weaponChance * weapons.Count)}, gear {Percent(gearChance * gear.Count)}."
                : $" — weapons {Percent(weaponChance * weapons.Count)}, gear {Percent(gearChance * gear.Count)}. No S-class items in this crate.";
            if (pool.Count > 0)
                text += "\n<color=#AAAAB5>S weapons and gear come at Elite; an S mount you already own levels up instead.</color>";
            var body = UIBuilder.Text(_ratesContent, text, UITheme.SmallSize + 2f, TextAlignmentOptions.Left, UITheme.Text);
            body.richText = true;

            var inv = InventoryService.Data;
            if (pool.Count > 0)
            {
                var cells = RatesGrid(crate.SMounts ? "S-class equipment & mounts" : "S-class equipment", "SClass");
                string each = Percent(CrateService.SItemChance(crate));
                foreach (var (kind, id) in pool)
                {
                    switch (kind)
                    {
                        case CrateDropKind.Weapon:
                            ItemCard(cells, Icon(true, id), WeaponName(id), ItemGrade.Elite, true, inv.OwnsWeapon(id) ? "Owned" : null, false, each);
                            break;
                        case CrateDropKind.Gear:
                            ItemCard(cells, Icon(false, id), GearName(id), ItemGrade.Elite, true, inv.GetEquipmentCount(id) > 0 ? "Owned" : null, false, each);
                            break;
                        default:
                            var mount = MountService.Get(id);
                            ItemCard(cells, MountIcon(mount, id), mount != null ? mount.NameOrId : id, ItemGrade.Legendary, true,
                                mount != null && MountService.IsOwned(mount) ? $"Lv {MountService.Level(mount)}" : null, false, each);
                            break;
                    }
                }
            }

            // Regular items come at the rolled grade, so they wear a plain frame rather than a grade colour.
            if (weaponChance > 0f)
            {
                var cells = RatesGrid("Weapons", "Weapons");
                foreach (var weapon in weapons)
                    ItemCard(cells, Icon(true, weapon.Id), WeaponName(weapon.Id), ItemGrade.Common, false,
                        inv.OwnsWeapon(weapon.Id) ? "Owned" : null, false, Percent(weaponChance), Plain);
            }
            if (gearChance > 0f)
            {
                var cells = RatesGrid("Gear", "Gear");
                foreach (var item in gear)
                    ItemCard(cells, Icon(false, item.Id), GearName(item.Id), ItemGrade.Common, false,
                        inv.GetEquipmentCount(item.Id) > 0 ? "Owned" : null, false, Percent(gearChance), Plain);
            }

            _ratesScroll.verticalNormalizedPosition = 1f;
        }

        private static readonly Color Plain = new(0.78f, 0.62f, 0.45f);

        private RectTransform RatesGrid(string title, string name)
        {
            UIBuilder.SectionHeader(_ratesContent, title);
            var grid = UIBuilder.Rect(_ratesContent, name).gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(176f, 214f);
            grid.spacing = new Vector2(10f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            return (RectTransform)grid.transform;
        }

        private static string WeaponName(string id)
        {
            var weapon = InventoryService.GetWeapon(id);
            return weapon != null ? DefinitionNames.Of(weapon) : id;
        }

        private static string GearName(string id)
        {
            var item = InventoryService.GetEquipment(id);
            return item != null ? DefinitionNames.Of(item) : id;
        }

        public void CloseRates()
        {
            if (_rates != null) _rates.SetActive(false);
        }

        /// <summary>Opens <paramref name="count"/> crates and plays the reveal. Public so tests can drive it like a tap.</summary>
        public void Open(CrateKind crate, int count, bool free)
        {
            if (IsRevealing) return;
            var result = CrateService.Open(crate.Id, count, free, out var drops);
            if (result != CrateResult.Opened)
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast(CrateService.Describe(result, crate));
                return;
            }
            Sfx.Play(AudioCueIds.ChestOpen);
            _lastDrops.Clear();
            _lastDrops.AddRange(drops);
            if (_reveal != null) StopCoroutine(_reveal);
            _reveal = StartCoroutine(Reveal(crate, drops));
            _host.Refresh();
        }

        private static Sprite Icon(CrateDrop drop) =>
            drop.Mount ? MountIcon(MountService.Get(drop.Id), drop.Id) : Icon(drop.Weapon, drop.Id);

        private static Sprite MountIcon(MountDefinition mount, string id)
        {
            var icon = UIIcons.Mount(id);
            return icon != null ? icon : mount != null && mount.Frames.Length > 0 ? mount.Frames[0] : null;
        }

        private static Sprite Icon(bool weapon, string id)
        {
            if (weapon)
            {
                var def = InventoryService.GetWeapon(id);
                return def != null && def.Icon != null ? def.Icon : UIIcons.Weapon(id);
            }
            var item = InventoryService.GetEquipment(id);
            return item != null && item.Icon != null ? item.Icon : UIIcons.Equipment(id);
        }

        /// <summary>A card for one item: grade-coloured frame, icon, name, grade, an "S" tag and a NEW / Owned tag.</summary>
        private static RectTransform ItemCard(RectTransform parent, Sprite icon, string name, ItemGrade grade, bool special, string tag, bool tagIsNew,
            string gradeLabel = null, Color? frameColor = null)
        {
            var color = frameColor ?? GradeColors.For(grade);
            var frame = UIStyle.Sprite(parent, "Item", "panel_wood_panel", color);
            frame.color = Color.Lerp(Color.white, color, 0.8f);
            frame.raycastTarget = false;
            var rt = frame.rectTransform;
            var image = UIBuilder.Image(rt, "Icon", Color.white, icon);
            image.raycastTarget = false;
            image.preserveAspect = true;
            UIStyle.Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(116f, 116f));
            var nameText = UIStyle.Label(rt, name, 20f, UIStyle.Cream, TextAlignmentOptions.Center, 0.25f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 13f;
            nameText.fontSizeMax = 20f;
            UIStyle.Place(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(166f, 28f));
            var gradeText = UIStyle.Label(rt, gradeLabel ?? grade.ToString(), 20f, Color.Lerp(Color.white, color, 0.7f), TextAlignmentOptions.Center, 0.25f);
            UIStyle.Place(gradeText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(166f, 28f));
            if (special)
            {
                var s = UIBuilder.Image(rt, "S", new Color(0.8f, 0.12f, 0.12f), Core.Utilities.GeneratedSprites.Circle);
                s.raycastTarget = false;
                UIStyle.Place(s.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(20f, -20f), new Vector2(48f, 48f));
                var letter = UIStyle.Label(s.transform, "S", 32f, Gold, TextAlignmentOptions.Center, 0.3f);
                UIBuilder.Stretch(letter.rectTransform);
            }
            if (!string.IsNullOrEmpty(tag))
            {
                var pill = UIBuilder.Image(rt, "Tag", tagIsNew ? new Color(0.2f, 0.62f, 0.25f) : new Color(0.25f, 0.45f, 0.7f));
                pill.raycastTarget = false;
                UIStyle.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(76f, 26f));
                var text = UIStyle.Label(pill.transform, tag, 17f);
                UIBuilder.Stretch(text.rectTransform);
            }
            return rt;
        }

        // ------------------------------------------------------------------ reveal

        private void BuildOverlay(RectTransform root)
        {
            var dim = UIBuilder.Image(root, "Reveal", new Color(0.02f, 0.02f, 0.05f, 0.9f));
            UIBuilder.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(OnOverlayTap);
            _overlay = dim.gameObject;

            _rays = UIBuilder.Rect(dim.transform, "Rays");
            UIStyle.Place(_rays, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -520f), new Vector2(10f, 10f));
            for (int i = 0; i < 10; i++)
            {
                var ray = UIBuilder.Image(_rays, "Ray", new Color(1f, 0.85f, 0.4f, 0.22f));
                ray.raycastTarget = false;
                ray.rectTransform.sizeDelta = new Vector2(70f, 1300f);
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 18f);
            }
            var glow = UIBuilder.Image(dim.transform, "Glow", new Color(1f, 0.9f, 0.55f, 0.5f), Core.Utilities.GeneratedSprites.Glow);
            glow.raycastTarget = false;
            UIStyle.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -520f), new Vector2(620f, 620f));

            _chest = UIBuilder.Image(dim.transform, "Chest", Color.white);
            _chest.raycastTarget = false;
            _chest.preserveAspect = true;
            UIStyle.Place(_chest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -520f), new Vector2(380f, 380f));

            _headline = UIStyle.Label(dim.transform, "", 64f, Color.white, TextAlignmentOptions.Center, 0.3f);
            UIStyle.Place(_headline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1000f, 110f));

            _results = UIBuilder.Rect(dim.transform, "Results");
            UIStyle.Place(_results, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -800f), new Vector2(1000f, 500f));
            var grid = _results.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 214f);
            grid.spacing = new Vector2(12f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;

            _hint = UIStyle.Label(dim.transform, "", 34f, UIStyle.Cream, TextAlignmentOptions.Center, 0.25f);
            UIStyle.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1000f, 60f));

            _flash = UIBuilder.Image(dim.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            _flash.raycastTarget = false;
            UIBuilder.Stretch(_flash.rectTransform);

            _overlay.SetActive(false);
        }

        private void OnOverlayTap()
        {
            if (IsRevealing) _skip = true; // hurry the show along
            else CloseReveal();
        }

        public void CloseReveal()
        {
            if (_reveal != null) StopCoroutine(_reveal);
            _reveal = null;
            IsRevealing = false;
            if (_overlay != null) _overlay.SetActive(false);
            _host.Refresh();
        }

        private void Update()
        {
            if (OverlayOpen && _rays.gameObject.activeSelf) _rays.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 25f);
        }

        private IEnumerator Reveal(CrateKind crate, List<CrateDrop> drops)
        {
            IsRevealing = true;
            _skip = false;
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
            UIBuilder.ClearChildren(_results);
            _rays.gameObject.SetActive(false);
            _chest.sprite = UIArt.Get(crate.ClosedIcon);
            _chest.rectTransform.localRotation = Quaternion.identity;
            _headline.text = crate.Name;
            _headline.color = Color.white;
            _hint.text = "";
            _flash.color = new Color(1f, 1f, 1f, 0f);

            // Pop in, then shake harder and harder.
            for (float t = 0f; t < 0.25f && !_skip; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.25f;
                _chest.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI)) * Mathf.Min(1f, k * 1.4f);
                yield return null;
            }
            _chest.rectTransform.localScale = Vector3.one;
            for (float t = 0f; t < 0.8f && !_skip; t += Time.unscaledDeltaTime)
            {
                _chest.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * 10f * (t / 0.8f));
                yield return null;
            }
            _chest.rectTransform.localRotation = Quaternion.identity;

            // Burst open.
            bool anySpecial = drops.Exists(d => d.Special);
            var best = ItemGrade.Common;
            foreach (var d in drops) if (d.Grade > best) best = d.Grade;
            _chest.sprite = UIArt.Get(crate.OpenIcon);
            _rays.gameObject.SetActive(true);
            foreach (var ray in _rays.GetComponentsInChildren<Image>())
            {
                var c = anySpecial ? Gold : GradeColors.For(best);
                ray.color = new Color(c.r, c.g, c.b, 0.24f);
            }
            _headline.text = anySpecial ? "S-CLASS!" : drops.Count > 1 ? "You got:" : "You got";
            _headline.color = anySpecial ? Gold : Color.Lerp(Color.white, GradeColors.For(best), 0.6f);
            Sfx.Play(AudioCueIds.RewardClaim);
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                _flash.color = new Color(1f, 1f, 1f, 1f - t / 0.35f);
                if (_skip) break;
                yield return null;
            }
            _flash.color = new Color(1f, 1f, 1f, 0f);

            // The items, one by one (S items get a moment of their own).
            foreach (var drop in drops)
            {
                var card = ItemCard(_results, Icon(drop), drop.Name, drop.Grade, drop.Special, drop.New ? "NEW" : drop.Mount ? "Lv up" : null, true,
                    drop.Mount ? "Mount" : null);
                if (_skip) continue;
                for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
                {
                    card.localScale = Vector3.one * (0.3f + 0.7f * (t / 0.18f) + 0.15f * Mathf.Sin(t / 0.18f * Mathf.PI));
                    yield return null;
                }
                card.localScale = Vector3.one;
                if (drop.Special)
                {
                    Sfx.Play(AudioCueIds.UiUpgrade);
                    for (float t = 0f; t < 0.4f && !_skip; t += Time.unscaledDeltaTime) yield return null;
                }
                for (float t = 0f; t < 0.08f && !_skip; t += Time.unscaledDeltaTime) yield return null;
            }
            foreach (RectTransform child in _results) child.localScale = Vector3.one;

            _hint.text = "Tap to continue";
            IsRevealing = false;
            _reveal = null;
        }
    }
}
