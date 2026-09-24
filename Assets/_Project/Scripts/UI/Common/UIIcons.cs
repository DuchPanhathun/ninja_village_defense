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

        /// <summary>An enemy or boss: its portrait art if it has one, else its prefab's (animated) sprite.</summary>
        public static Sprite Enemy(NinjaVillage.Gameplay.Enemies.EnemyDefinition enemy)
        {
            if (enemy == null) return null;
            if (enemy.Icon != null) return enemy.Icon;
            if (enemy.EnemyPrefab == null) return null;
            var frames = enemy.EnemyPrefab.GetComponentInChildren<SpriteFrameAnimator>(true);
            if (frames != null && frames.SpriteSet != null && frames.SpriteSet.DefaultSprite != null) return frames.SpriteSet.DefaultSprite;
            var renderer = enemy.EnemyPrefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }
        public static Sprite Skin(string skinId) => Character(skinId);

        private static Sprite Character(string key)
        {
            var set = key != null ? CharacterSpriteLibrary.Find(key) : null;
            return set != null ? set.DefaultSprite : null;
        }

        public static Sprite Blessing(string blessingId) => Named("icon_blessing_", blessingId);

        /// <summary>A Market offer: the item itself, the crate for random gear, gems, or coins (a chest for big piles).</summary>
        public static Sprite MarketOffer(NinjaVillage.Systems.Village.MarketOfferDefinition offer)
        {
            if (offer == null) return null;
            switch (offer.RewardType)
            {
                case NinjaVillage.Systems.Village.MarketRewardType.Equipment:
                    return offer.Equipment == null ? null : offer.Equipment.Icon != null ? offer.Equipment.Icon : Equipment(offer.Equipment.Id);
                case NinjaVillage.Systems.Village.MarketRewardType.RandomEquipment:
                    string rarity = offer.RandomRarity.ToString().ToLowerInvariant();
                    return UIArt.Get("icon_crate_" + rarity) ?? UIArt.Get("icon_crate_epic");
                case NinjaVillage.Systems.Village.MarketRewardType.Gems:
                    return UIArt.Get(offer.RewardAmount >= 500 ? "store_gems_550" : "store_gems_100");
                default:
                    return UIArt.Get(offer.RewardAmount >= 2000 ? "pickup_bigchest_0" : "icon_item_money");
            }
        }

        /// <summary>A village building as it looks at <paramref name="level"/> (the Castle changes with its stage).</summary>
        public static Sprite Building(string buildingId, int level)
        {
            var art = NinjaVillage.Gameplay.Village.VillageArt.Load();
            return art != null ? art.BuildingSprite(buildingId, Mathf.Max(1, level)) : null;
        }

        private static Sprite Named(string prefix, string id) => string.IsNullOrEmpty(id) ? null : UIArt.Get(prefix + id);
    }
}
