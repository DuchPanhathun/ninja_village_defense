using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.LiveOps;
using NinjaVillage.Systems.Monetization;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Store
{
    /// <summary>
    /// The Shop (EPIC 21): gem packs, the one-time Starter Pack, Remove Ads and the premium Battle Pass
    /// (real money through <see cref="StoreService"/>), free gems for watching ads (capped per day),
    /// cosmetic skins and premium heroes (gems), plus Restore Purchases for store compliance.
    /// Nothing sold here is required to progress.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class StoreScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Store;
        protected override string Title => "Shop";

        private bool _busy;

        private void OnEnable() => EventBus<PurchaseResultEvent>.Subscribe(OnPurchaseResult);
        private void OnDisable() => EventBus<PurchaseResultEvent>.Unsubscribe(OnPurchaseResult);
        private void OnPurchaseResult(PurchaseResultEvent evt) { if (IsVisible) Refresh(); }

        protected override void BuildBottom(RectTransform body)
        {
            UIBuilder.Button(body, "Restore purchases", () =>
            {
                Sfx.Play(AudioCueIds.UiClick);
                StoreService.Restore((ok, error) =>
                {
                    Toast(ok ? "Purchases restored" : $"Restore failed: {error}");
                    Refresh();
                });
            }, UITheme.ButtonSecondary, 100f);
        }

        protected override void Populate(RectTransform content)
        {
            InfoText.text = StoreService.IsReady
                ? "All purchases support the game — none are needed to progress."
                : "Connecting to the store... (prices may show as estimates)";
            var store = SaveService.Data.Store;

            UIBuilder.SectionHeader(content, "Special offers");
            if (!store.StarterPackPurchased) AddProduct(content, StoreProducts.StarterPack, UITheme.Gold);
            if (!store.AdsRemoved) AddProduct(content, StoreProducts.RemoveAds, UITheme.Text);
            else UIBuilder.ActionCard(content, "Ads removed", "Thank you! Optional reward ads stay available.", out _, out _, UITheme.Positive);
            if (BattlePassService.EnsureSeason() != null && !BattlePassService.IsPremiumUnlocked)
                AddProduct(content, StoreProducts.BattlePassPremium, UITheme.Gold);

            UIBuilder.SectionHeader(content, "Gems");
            AddProduct(content, StoreProducts.Gems100, UITheme.Gem);
            AddProduct(content, StoreProducts.Gems550, UITheme.Gem);
            AddProduct(content, StoreProducts.Gems1200, UITheme.Gem);

            int left = AdsService.AdGemsLeftToday;
            var free = UIBuilder.ActionCard(content, $"Free gems ({left} left today)",
                $"Watch a short ad for {AdsService.AdGemsPerView} gems.", out _, out _, UITheme.Gem);
            var watch = UIBuilder.SmallButton(free.transform, "Watch ad", () =>
            {
                if (_busy) return;
                _busy = true;
                AdsService.WatchForGems(ok =>
                {
                    _busy = false;
                    Toast(ok ? $"+{AdsService.AdGemsPerView} gems!" : "No reward — the ad was skipped");
                    Refresh();
                });
            }, UITheme.Gem, 240f);
            if (left <= 0) UIBuilder.SetEnabled(watch, false);

            AddPremiumHeroes(content);
            AddSkins(content);
        }

        private void AddProduct(RectTransform content, string productId, Color accent)
        {
            var product = StoreProducts.Get(productId);
            if (product == null) return;
            var actions = UIBuilder.ActionCard(content, product.Title, product.Description, out _, out _, accent, UIIcons.StoreProduct(productId));
            UIBuilder.SmallButton(actions.transform, StoreService.Price(productId), () =>
            {
                if (_busy) return;
                _busy = true;
                Sfx.Play(AudioCueIds.UiClick);
                StoreService.Buy(productId, outcome =>
                {
                    _busy = false;
                    Sfx.Play(outcome == PurchaseOutcome.Success ? AudioCueIds.UiPurchase : AudioCueIds.UiError);
                    Toast(StoreService.Describe(outcome));
                    Refresh();
                });
            }, UITheme.Button, 260f);
        }

        private void AddPremiumHeroes(RectTransform content)
        {
            bool header = false;
            foreach (var hero in HeroService.GetSortedHeroes())
            {
                if (hero == null || !hero.IsPremium || HeroService.IsUnlocked(hero)) continue;
                if (!header) { UIBuilder.SectionHeader(content, "Premium heroes"); header = true; }
                var actions = UIBuilder.ActionCard(content, hero.NameOrId, hero.RoleSummary, out _, out _, hero.ThemeColor, UIIcons.Hero(hero.Id));
                UIBuilder.SmallButton(actions.transform, "View", () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    UIScreenNavigator.Instance.Show(ScreenIds.Heroes);
                }, UITheme.ButtonSecondary, 170f);
            }
        }

        private void AddSkins(RectTransform content)
        {
            var skins = SkinService.GetSorted();
            if (skins.Count == 0) return;
            UIBuilder.SectionHeader(content, "Skins");
            foreach (var skin in skins)
            {
                bool owned = SkinService.Owns(skin);
                if (!owned && !skin.SoldInShop) continue; // earned elsewhere (battle pass / events)

                var hero = HeroService.Get(skin.HeroId);
                string body = $"For {(hero != null ? hero.NameOrId : skin.HeroId)}" + (string.IsNullOrEmpty(skin.Description) ? string.Empty : "\n" + skin.Description);
                var actions = UIBuilder.ActionCard(content, skin.NameOrId, body, out _, out _, skin.Tint, UIIcons.Skin(skin.Id));

                if (owned)
                {
                    bool equipped = SkinService.IsEquipped(skin);
                    UIBuilder.SmallButton(actions.transform, equipped ? "Unequip" : "Equip", () =>
                    {
                        SkinService.ToggleEquip(skin);
                        Sfx.Play(AudioCueIds.UiClick);
                        Refresh();
                    }, equipped ? UITheme.ButtonSecondary : (Color?)null, 200f);
                }
                else
                {
                    UIBuilder.SmallButton(actions.transform, $"{skin.GemPrice} Gems", () =>
                    {
                        var result = SkinService.TryBuy(skin);
                        if (result != SkinResult.Ok)
                        {
                            Sfx.Play(AudioCueIds.UiError);
                            Toast(result == SkinResult.NotEnoughGems ? "Not enough gems" : "Unavailable");
                        }
                        Refresh();
                    }, UITheme.Gem, 240f);
                }
            }
        }
    }
}
