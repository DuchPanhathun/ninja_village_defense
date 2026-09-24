using NinjaVillage.Gameplay.Animation;
using UnityEngine;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Picture for a game thing, by id: weapons, equipment, pet gear, talents, store products (from
    /// <see cref="UIArt"/>), heroes, pets and skins (portrait or character sprite). Null when there's no art —
    /// cards then just show text.
    /// </summary>
    public static class UIIcons
    {
        public static Sprite Weapon(string id) => Named("icon_weapon_", id?.ToLowerInvariant());
        public static Sprite Equipment(string id) => Named("icon_equip_", id);
        public static Sprite PetGear(string id) => Named("icon_", id);                 // ids are already "petgear_*"
        public static Sprite Talent(string id) => Named("icon_talent_", id);
        public static Sprite Evolution(string id) => Named("icon_evolution_", id);

        /// <summary>Store product ids are reverse-DNS ("com.thun.ninjavillagedefense.gems_100").</summary>
        public static Sprite StoreProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            int dot = productId.LastIndexOf('.');
            return UIArt.Get("store_" + (dot >= 0 ? productId.Substring(dot + 1) : productId));
        }

        public static Sprite Hero(string heroId)
        {
            string key = CharacterSpriteLibrary.HeroKey(heroId);
            if (key == null) return null;
            return UIArt.Get("portrait_" + key) ?? Character(key);
        }

        public static Sprite Pet(string petId) => Character(CharacterSpriteLibrary.PetKey(petId));
        public static Sprite Skin(string skinId) => Character(skinId);

        private static Sprite Character(string key)
        {
            var set = key != null ? CharacterSpriteLibrary.Find(key) : null;
            return set != null ? set.DefaultSprite : null;
        }

        private static Sprite Named(string prefix, string id) => string.IsNullOrEmpty(id) ? null : UIArt.Get(prefix + id);
    }
}
