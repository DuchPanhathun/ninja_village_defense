using System.Collections.Generic;

namespace NinjaVillage.Systems.Monetization
{
    public enum StoreProductType
    {
        Consumable,
        NonConsumable
    }

    /// <summary>One real-money product. Prices come from the store (Play Console / App Store Connect).</summary>
    public sealed class StoreProduct
    {
        public string Id;
        public StoreProductType Type;
        public string Title;
        public string Description;
        /// <summary>Shown only until the store returns the localized price (Editor / offline).</summary>
        public string FallbackPrice;
    }

    /// <summary>
    /// The approved IAP catalog (EPIC 21). Create these exact ids in Google Play Console and App Store
    /// Connect. Rewards are applied by <see cref="PurchaseGrants"/>. Premium heroes and skins are bought
    /// with gems in-game, not with direct IAP.
    /// </summary>
    public static class StoreProducts
    {
        private const string Prefix = "com.thun.ninjavillagedefense.";

        public const string Gems100 = Prefix + "gems_100";
        public const string Gems550 = Prefix + "gems_550";
        public const string Gems1200 = Prefix + "gems_1200";
        public const string StarterPack = Prefix + "starter_pack";
        public const string RemoveAds = Prefix + "remove_ads";
        public const string BattlePassPremium = Prefix + "battle_pass_premium";

        /// <summary>The premium hero included in the starter pack.</summary>
        public const string StarterPackHeroId = "beast_ninja";
        public const int StarterPackGems = 300;
        public const int StarterPackCoins = 5000;

        public static readonly IReadOnlyList<StoreProduct> All = new List<StoreProduct>
        {
            new() { Id = Gems100, Type = StoreProductType.Consumable, Title = "Pouch of Gems", Description = "100 gems", FallbackPrice = "$0.99" },
            new() { Id = Gems550, Type = StoreProductType.Consumable, Title = "Chest of Gems", Description = "550 gems (+10% bonus)", FallbackPrice = "$4.99" },
            new() { Id = Gems1200, Type = StoreProductType.Consumable, Title = "Vault of Gems", Description = "1,200 gems (+20% bonus)", FallbackPrice = "$9.99" },
            new() { Id = StarterPack, Type = StoreProductType.NonConsumable, Title = "Starter Pack",
                    Description = $"{StarterPackGems} gems, {StarterPackCoins} coins and the Beast Ninja hero — once only", FallbackPrice = "$2.99" },
            new() { Id = RemoveAds, Type = StoreProductType.NonConsumable, Title = "Remove Ads",
                    Description = "No more interstitial ads. Optional reward ads stay available.", FallbackPrice = "$3.99" },
            // Consumable so it can be bought again each season; PurchaseGrants ties it to the current season.
            new() { Id = BattlePassPremium, Type = StoreProductType.Consumable, Title = "Premium Battle Pass",
                    Description = "Unlock this season's premium reward track (retroactive)", FallbackPrice = "$4.99" },
        };

        public static StoreProduct Get(string id)
        {
            foreach (var product in All) if (product.Id == id) return product;
            return null;
        }

        public static int GemsFor(string productId)
        {
            switch (productId)
            {
                case Gems100: return 100;
                case Gems550: return 550;
                case Gems1200: return 1200;
                default: return 0;
            }
        }
    }
}
