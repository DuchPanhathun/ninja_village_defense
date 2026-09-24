using System.Collections.Generic;
using NinjaVillage.Systems.Monetization;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class MonetizationTests
    {
        [Test]
        public void Catalog_HasApprovedProductsWithCorrectTypes()
        {
            var ids = new HashSet<string>();
            foreach (var p in StoreProducts.All)
            {
                Assert.IsTrue(p.Id.StartsWith("com.thun.ninjavillagedefense."), p.Id);
                Assert.IsTrue(ids.Add(p.Id), "duplicate product id " + p.Id);
                Assert.IsFalse(string.IsNullOrEmpty(p.FallbackPrice));
            }
            Assert.AreEqual(6, StoreProducts.All.Count);
            Assert.AreEqual(StoreProductType.NonConsumable, StoreProducts.Get(StoreProducts.RemoveAds).Type);
            Assert.AreEqual(StoreProductType.NonConsumable, StoreProducts.Get(StoreProducts.StarterPack).Type);
            Assert.AreEqual(StoreProductType.Consumable, StoreProducts.Get(StoreProducts.Gems550).Type);
            Assert.AreEqual(StoreProductType.Consumable, StoreProducts.Get(StoreProducts.BattlePassPremium).Type, "re-buyable every season");
        }

        [Test]
        public void GemPacks_GiveTheirAmounts()
        {
            Assert.AreEqual(100, StoreProducts.GemsFor(StoreProducts.Gems100));
            Assert.AreEqual(550, StoreProducts.GemsFor(StoreProducts.Gems550));
            Assert.AreEqual(1200, StoreProducts.GemsFor(StoreProducts.Gems1200));
            Assert.AreEqual(0, StoreProducts.GemsFor(StoreProducts.RemoveAds));
        }

        [Test]
        public void Interstitials_PacedAndNeverAfterRemoveAds()
        {
            Assert.IsFalse(AdsService.ShouldShowInterstitial(2, 3, adsRemoved: false));
            Assert.IsTrue(AdsService.ShouldShowInterstitial(3, 3, adsRemoved: false));
            Assert.IsFalse(AdsService.ShouldShowInterstitial(10, 3, adsRemoved: true), "Remove Ads stops interstitials");
            Assert.IsFalse(AdsService.ShouldShowInterstitial(10, 0, adsRemoved: false), "0 disables interstitials remotely");
        }
    }
}
