using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Data;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    public enum SkinResult { Ok, Unknown, AlreadyOwned, NotOwned, NotEnoughGems, NotForSale }

    /// <summary>Owning, buying (gems) and equipping skins per hero; the equipped skin is applied at run start.</summary>
    public static class SkinService
    {
        public static SkinCatalog Catalog => CatalogLoader.Load<SkinCatalog>();

        private static StoreSaveData Data => SaveService.Data.Store;

        public static List<SkinDefinition> GetSorted()
        {
            var list = new List<SkinDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var skin in catalog.All) if (skin != null) list.Add(skin);
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return list;
        }

        public static bool Owns(SkinDefinition skin) => skin != null && Data.OwnedSkinIds.Contains(skin.Id);

        public static string EquippedSkinId(string heroId)
        {
            foreach (var selection in Data.EquippedSkins)
                if (selection != null && selection.HeroId == heroId) return selection.SkinId;
            return null;
        }

        public static bool IsEquipped(SkinDefinition skin) => skin != null && EquippedSkinId(skin.HeroId) == skin.Id;

        public static SkinResult TryBuy(SkinDefinition skin)
        {
            if (skin == null) return SkinResult.Unknown;
            if (Owns(skin)) return SkinResult.AlreadyOwned;
            if (!skin.SoldInShop) return SkinResult.NotForSale;
            if (!CurrencyService.TrySpend(Price.Gems(skin.GemPrice), skin.Id)) return SkinResult.NotEnoughGems;

            Data.OwnedSkinIds.Add(skin.Id);
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.UiPurchase);
            return SkinResult.Ok;
        }

        /// <summary>Equips (or, when already equipped, unequips) a skin on its hero.</summary>
        public static SkinResult ToggleEquip(SkinDefinition skin)
        {
            if (skin == null) return SkinResult.Unknown;
            if (!Owns(skin)) return SkinResult.NotOwned;

            var list = Data.EquippedSkins;
            bool wasEquipped = IsEquipped(skin);
            list.RemoveAll(s => s == null || s.HeroId == skin.HeroId);
            if (!wasEquipped) list.Add(new SkinSelection { HeroId = skin.HeroId, SkinId = skin.Id });
            SaveService.SaveNow();
            return SkinResult.Ok;
        }
    }

    /// <summary>Applies the selected hero's equipped skin to the player sprite at run start (cosmetic only).</summary>
    public sealed class SkinRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Store;

        public void Apply(RunStartContext context)
        {
            if (context.Player == null || string.IsNullOrEmpty(context.HeroId)) return;
            var catalog = SkinService.Catalog;
            string skinId = SkinService.EquippedSkinId(context.HeroId);
            var skin = catalog != null ? catalog.Get(skinId) : null;
            if (skin == null || !SkinService.Owns(skin)) return;

            var renderer = context.Player.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) return;
            if (skin.Sprite != null) renderer.sprite = skin.Sprite;
            renderer.color = skin.Tint;
            if (context.Player.TryGetComponent<HitFlash>(out var flash)) flash.RefreshBaseColor();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new SkinRunModifier());
    }
}
