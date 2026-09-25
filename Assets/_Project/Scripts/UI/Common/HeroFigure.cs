using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Monetization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// The selected hero (or their equipped skin) standing facing you with their weapon in hand — the centrepiece of
    /// the Home screen and the Equipment showcase. Heroes without front-facing art (the Mage Ninja) stand side-on.
    /// The weapon is a child of the hero image, so it moves with the idle bob.
    /// </summary>
    public sealed class HeroFigure
    {
        private readonly Image _hero, _weapon;
        private readonly UIImageAnimator _animator;
        private readonly float _size;
        private string _shownKey;

        public Image HeroImage => _hero;
        public Image WeaponImage => _weapon;

        /// <param name="parent">Where the figure stands (it is centred there, at <paramref name="center"/>).</param>
        /// <param name="size">UI px of a 16 px hero cell (bigger cells scale up so bodies match).</param>
        /// <param name="onWeapon">Tapping the weapon (null: taps pass through to whatever is behind).</param>
        public HeroFigure(Transform parent, float size, Vector2 center, UnityAction onWeapon = null)
        {
            _size = size;
            _hero = UIBuilder.Image(parent, "Sprite", Color.white);
            _hero.raycastTarget = false;
            _hero.preserveAspect = true;
            UIStyle.Place(_hero.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, new Vector2(size, size));
            _animator = _hero.gameObject.AddComponent<UIImageAnimator>();

            // Held in the hand on the viewer's right, blade up (offsets are for the 16 px body, whatever the cell).
            _weapon = UIBuilder.Image(_hero.transform, "Weapon", Color.white);
            _weapon.preserveAspect = true;
            _weapon.raycastTarget = onWeapon != null;
            UIStyle.Place(_weapon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(size * 0.43f, -size * 0.094f), new Vector2(size * 0.41f, size * 0.41f));
            if (onWeapon != null)
            {
                var button = _weapon.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(onWeapon);
            }
            _weapon.gameObject.SetActive(false);
        }

        /// <summary>Shows <paramref name="hero"/> (their equipped skin when owned and drawn) holding <paramref name="weapon"/>.</summary>
        public void Refresh(HeroDefinition hero, WeaponDefinition weapon)
        {
            string key = CharacterSpriteLibrary.HeroKey(hero != null ? hero.Id : "assassin");
            if (hero != null)
            {
                var skin = SkinService.Catalog != null ? SkinService.Catalog.Get(SkinService.EquippedSkinId(hero.Id)) : null;
                if (skin != null && SkinService.Owns(skin) && CharacterSpriteLibrary.Find(skin.Id) != null) key = skin.Id;
            }
            if (key != _shownKey)
            {
                var set = CharacterSpriteLibrary.Find(key);
                if (set != null)
                {
                    _shownKey = key;
                    var frames = set.Frames(set.Has(CharacterAnim.Front) ? CharacterAnim.Front : CharacterAnim.Idle);
                    var sprite = frames.Length > 0 ? frames[0] : set.DefaultSprite;
                    // Pack heroes are drawn in 16 px cells, the Beast Ninja in 32 px ones: size by cell so bodies match.
                    float cells = sprite != null ? sprite.rect.width / 128f : 1f;
                    _hero.rectTransform.sizeDelta = Vector2.one * Mathf.Min(_size * cells, _size * 2f);
                    _animator.Play(frames.Length > 0 ? frames : new[] { sprite }, set.Fps(CharacterAnim.Idle), frames.Length > 1 ? 0f : _size * 0.035f);
                }
            }

            var icon = weapon == null ? null : weapon.Icon != null ? weapon.Icon : UIIcons.Weapon(weapon.Id);
            _weapon.sprite = icon;
            _weapon.gameObject.SetActive(icon != null);
        }
    }
}
