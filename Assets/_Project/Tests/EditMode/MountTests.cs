using System;
using System.IO;
using NinjaVillage.Core;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Mounts;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Crates;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Mounts;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;
using NinjaVillage.Systems.Village;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    /// <summary>Mounts: buying, riding, levelling, S mounts from Surprise Boxes, and the ride into battle.</summary>
    public class MountTests
    {
        // ------------------------------------------------------------------ rules & content

        [Test]
        public void Levels_GrowTheBonuses_AndEachCostsMore()
        {
            Assert.AreEqual(1f, MountRules.LevelScale(1), 1e-5f);
            Assert.AreEqual(1.9f, MountRules.LevelScale(MountRules.MaxLevel), 1e-5f);
            Assert.Less(MountRules.UpgradeCost(1), MountRules.UpgradeCost(2));
        }

        [Test]
        public void EveryMount_HasGallopFrames_AndSpeed_TheTwoSClassOnesComeLast()
        {
            var mounts = MountService.GetSorted();
            Assume.That(mounts.Count >= 8, "mount catalog missing — run the content generator");
            int special = 0;
            foreach (var mount in mounts)
            {
                Assert.GreaterOrEqual(mount.Frames.Length, 2, mount.Id);
                foreach (var frame in mount.Frames) Assert.IsNotNull(frame, mount.Id);
                Assert.IsNotNull(mount.Icon, mount.Id);
                bool fast = false;
                foreach (var bonus in mount.Bonuses)
                    if (bonus.Stat == TalentStat.MoveSpeed && bonus.Value > 0f) fast = true;
                Assert.IsTrue(fast, $"{mount.Id} makes you faster");
                Assert.Greater(mount.RiderOffset.y, 0f, $"{mount.Id}: the rider sits on its back");
                if (mount.IsSpecial) special++;
                else Assert.Greater(mount.UnlockPrice.Amount, 0, mount.Id);
            }
            Assert.AreEqual(2, special);
            Assert.IsTrue(mounts[^1].IsSpecial && mounts[^2].IsSpecial, "S mounts sort last");
        }

        // ------------------------------------------------------------------ service (temp save)

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_mounts_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
            GameClock.OverrideUtcNow = () => new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            GameClock.OverrideUtcNow = null;
            SaveSystem.OverrideDirectory = null;
            SaveService.Load();
            try { Directory.Delete(_dir, true); } catch { /* best effort */ }
        }

        [Test]
        public void BuyingAMount_RidesIt_LevellingCostsCoins_AndSClassCantBeBought()
        {
            var horse = MountService.Get("horse_brown");
            var qilin = MountService.Get("golden_qilin");
            Assume.That(horse != null && qilin != null, "mount catalog missing — run the content generator");
            var wallet = SaveService.Data.Wallet;
            wallet.TrySpend(CurrencyType.Coins, wallet.Get(CurrencyType.Coins));
            Assert.AreEqual(MountResult.NotEnoughCurrency, MountService.TryUnlock(horse));

            wallet.Add(CurrencyType.Coins, 100000);
            Assert.AreEqual(MountResult.Success, MountService.TryUnlock(horse));
            Assert.IsTrue(MountService.IsActive(horse), "a new mount is ridden straight away");
            Assert.AreEqual(1, MountService.Level(horse));
            Assert.AreEqual(MountResult.AlreadyOwned, MountService.TryUnlock(horse));
            Assert.AreEqual(MountResult.SurpriseBoxOnly, MountService.TryUnlock(qilin));
            Assert.AreEqual(MountResult.NotOwned, MountService.Ride(qilin));

            int coins = wallet.Get(CurrencyType.Coins);
            Assert.AreEqual(MountResult.Success, MountService.TryUpgrade(horse));
            Assert.AreEqual(2, MountService.Level(horse));
            Assert.AreEqual(coins - MountRules.UpgradeCost(1), wallet.Get(CurrencyType.Coins));

            var riding = NinjaVillage.Systems.Inventory.LoadoutPower.Compute(SaveService.Data);
            MountService.Dismount();
            Assert.IsNull(MountService.Active);
            Assert.Greater(riding.Health, NinjaVillage.Systems.Inventory.LoadoutPower.Compute(SaveService.Data).Health, "the horse's +HP shows in the totals");
            Assert.IsNull(VillageSnapshot.FromSave(SaveService.Data).ActiveMountId, "on foot in the village");
            Assert.AreEqual(MountResult.Success, MountService.Ride(horse));
            Assert.AreEqual("horse_brown", VillageSnapshot.FromSave(SaveService.Data).ActiveMountId, "visitors see you ride");
        }

        [Test]
        public void AnSMountFromABox_IsNewOnce_ThenEachCopyLevelsItUp()
        {
            var qilin = MountService.Get("golden_qilin");
            Assume.That(qilin != null, "mount catalog missing — run the content generator");
            Assert.IsTrue(MountService.Grant(qilin));
            Assert.IsTrue(MountService.IsActive(qilin), "your first mount is ridden");
            Assert.AreEqual(1, MountService.Level(qilin));
            Assert.IsFalse(MountService.Grant(qilin));
            Assert.AreEqual(2, MountService.Level(qilin));
            for (int i = 0; i < 20; i++) MountService.Grant(qilin);
            Assert.AreEqual(MountRules.MaxLevel, MountService.Level(qilin));
        }

        [Test]
        public void OnlySurpriseBoxes_HoldTheSMounts()
        {
            Assert.IsTrue(CrateRules.Get("surprise").SMounts);
            Assert.IsFalse(CrateRules.Get("silver").SMounts);
            Assert.IsFalse(CrateRules.Get("wood").SMounts);
            Assume.That(CrateService.SpecialMounts().Count == 2 && CrateService.SpecialGear().Count > 0, "S content missing — run the content generator");

            var box = CrateRules.Get("surprise");
            SaveService.Data.Wallet.Add(CurrencyType.Gems, 100000);
            bool gotMount = false;
            for (int seed = 0; seed < 300 && !gotMount; seed++)
            {
                SaveService.Data.Crates.SurprisePity = box.Pity - 1; // S certain
                Assert.AreEqual(CrateResult.Opened, CrateService.Open("surprise", 1, false, out var drops, new System.Random(seed)));
                if (!drops[0].Mount) continue;
                gotMount = true;
                Assert.IsTrue(drops[0].Special);
                Assert.IsTrue(MountService.IsOwned(MountService.Get(drops[0].Id)), "the mount is yours");
            }
            Assert.IsTrue(gotMount, "S rolls sometimes land on a mount");
        }

        [Test]
        public void RidingIntoBattle_AddsTheBonusesAtItsLevel_AndSeatsThePlayer()
        {
            var horse = MountService.Get("horse_brown");
            Assume.That(horse != null, "mount catalog missing — run the content generator");
            var save = new SaveData();
            save.Migrate();
            save.Mounts.Owned.SetLevel(horse.Id, 3);
            save.Mounts.ActiveMountId = horse.Id;

            var player = new GameObject("Player");
            try
            {
                var body = player.AddComponent<SpriteRenderer>();
                body.sortingOrder = 40;
                var stats = player.AddComponent<PlayerStats>();
                var context = new RunStartContext { Save = save, Player = player, Stats = stats, BaseMaxHealth = 100f };
                new MountRunModifier().Apply(context);

                float scale = MountRules.LevelScale(3);
                float speed = 0f, health = 0f;
                foreach (var bonus in horse.Bonuses)
                {
                    if (bonus.Stat == TalentStat.MoveSpeed) speed += bonus.Value * scale;
                    if (bonus.Stat == TalentStat.MaxHealth) health += bonus.Value * scale;
                }
                Assert.AreEqual(1f + speed, stats.MoveSpeedMultiplier, 1e-4f);
                Assert.AreEqual(1f + health, context.MaxHealthMultiplier, 1e-4f);

                var visual = player.GetComponent<MountVisual>();
                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.IsMounted);
                Assert.IsFalse(body.enabled, "the rider copy draws the hero");
                Assert.AreEqual(40, visual.RiderRenderer.sortingOrder);
                Assert.AreEqual(41, visual.MountRenderer.sortingOrder, "the mount covers the rider's legs");
                Assert.Greater(visual.RiderRenderer.transform.localPosition.y, visual.MountRenderer.transform.localPosition.y);
                visual.Dismount();
                Assert.IsTrue(body.enabled);

                var notOwned = new SaveData();
                notOwned.Migrate();
                notOwned.Mounts.ActiveMountId = horse.Id;
                var bare = new RunStartContext { Save = notOwned, BaseMaxHealth = 100f };
                new MountRunModifier().Apply(bare);
                Assert.AreEqual(1f, bare.MaxHealthMultiplier, 1e-5f, "no mount you don't own");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }
    }
}
