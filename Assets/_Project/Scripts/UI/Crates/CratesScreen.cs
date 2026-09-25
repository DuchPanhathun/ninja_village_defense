using System.Collections;
using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Crates;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Mounts;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Crates
{
    /// <summary>
    /// Supply crates — the place to open crates for weapons and gear: a free Wooden Crate every day, Silver Crates
    /// and Surprise Boxes (the only source of S-class equipment, certain by the 30th box), each ×1 or ×10, with their
    /// odds shown. Opening plays a reveal: the chest shakes, bursts open in a flash with light rays, and the items pop
    /// out one by one in their grade's colour — S items with a gold "S". Below, the S-class collection.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class CratesScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Crates;
        protected override string Title => "Supply Crates";

        private static readonly Color Gold = new(1f, 0.82f, 0.3f);

        private GameObject _overlay;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.Crates, () => CrateService.FreeAvailable);

        protected override void Build(RectTransform body)
        {
            base.Build(body);
            BuildOverlay();
        }

        // ------------------------------------------------------------------ list

        protected override void Populate(RectTransform content)
        {
            InfoText.text = "Open crates for weapons and gear at random grades. <color=#FFD24D>Surprise Boxes</color> hold S-class equipment and mounts!";
            foreach (var crate in CrateRules.Crates) CrateCard(content, crate);

            UIBuilder.SectionHeader(content, "S-class equipment & mounts");
            UIBuilder.Text(content, "Found only in Surprise Boxes. Weapons and gear drop at Elite and merge up like any other piece; " +
                               "a mount you already ride levels up instead.",
                UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            var grid = UIBuilder.Rect(content, "SClass").gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(176f, 210f);
            grid.spacing = new Vector2(12f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            var inv = InventoryService.Data;
            foreach (var weapon in CrateService.SpecialWeapons())
                ItemCard((RectTransform)grid.transform, Icon(true, weapon.Id), weapon.DisplayName, ItemGrade.Elite, true,
                    inv.OwnsWeapon(weapon.Id) ? "Owned" : null, false);
            foreach (var item in CrateService.SpecialGear())
                ItemCard((RectTransform)grid.transform, Icon(false, item.Id), item.DisplayName, ItemGrade.Elite, true,
                    inv.GetEquipmentCount(item.Id) > 0 ? "Owned" : null, false);
            foreach (var mount in CrateService.SpecialMounts())
                ItemCard((RectTransform)grid.transform, MountIcon(mount, mount.Id), mount.NameOrId, ItemGrade.Legendary, true,
                    MountService.IsOwned(mount) ? $"Lv {MountService.Level(mount)}" : null, false, "Mount");
        }

        private void CrateCard(RectTransform content, CrateKind crate)
        {
            string body = crate.Blurb + "\n" + CrateRules.DescribeOdds(crate);
            if (crate.Pity > 0) body += $"\n<color=#FFD24D>S-class certain within {CrateService.PityLeft(crate)} box{(CrateService.PityLeft(crate) == 1 ? "" : "es")}.</color>";
            var actions = UIBuilder.ActionCard(content, crate.Name, body, out _, out _, crate.Pity > 0 ? Gold : UITheme.Text, UIArt.Get(crate.ClosedIcon));
            if (CrateService.CanOpenFree(crate))
                UIBuilder.SmallButton(actions.transform, "Open FREE", () => Open(crate, 1, true), UITheme.Positive, 240f);
            var one = UIBuilder.SmallButton(actions.transform, $"Open {crate.Price}", () => Open(crate, 1, false), UITheme.Button, 260f);
            UIBuilder.SetEnabled(one, CrateService.Check(crate, 1, false) == CrateResult.Opened);
            var ten = UIBuilder.SmallButton(actions.transform, $"×{CrateRules.MultiOpen} {CrateService.PriceFor(crate, CrateRules.MultiOpen)}",
                () => Open(crate, CrateRules.MultiOpen, false), UITheme.Gold, 280f);
            UIBuilder.SetEnabled(ten, CrateService.Check(crate, CrateRules.MultiOpen, false) == CrateResult.Opened);
        }

        /// <summary>Opens <paramref name="count"/> crates and plays the reveal. Public so tests can drive it like a tap.</summary>
        public void Open(CrateKind crate, int count, bool free)
        {
            if (IsRevealing) return;
            var result = CrateService.Open(crate.Id, count, free, out var drops);
            if (result != CrateResult.Opened)
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(CrateService.Describe(result, crate));
                return;
            }
            Sfx.Play(AudioCueIds.ChestOpen);
            _lastDrops.Clear();
            _lastDrops.AddRange(drops);
            if (_reveal != null) StopCoroutine(_reveal);
            _reveal = StartCoroutine(Reveal(crate, drops));
            Refresh();
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
            string gradeLabel = null)
        {
            var color = GradeColors.For(grade);
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

        private void BuildOverlay()
        {
            var dim = UIBuilder.Image(Root, "Reveal", new Color(0.02f, 0.02f, 0.05f, 0.9f));
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
            Refresh();
        }

        protected override void OnHidden()
        {
            if (OverlayOpen) CloseReveal();
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
