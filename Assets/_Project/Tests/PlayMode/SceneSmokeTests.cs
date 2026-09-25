using System.Collections;
using System.IO;
using System.Linq;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using NinjaVillage.UI.Village;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NinjaVillage.Tests
{
    /// <summary>
    /// End-to-end smoke tests: load each real scene, open every screen, and play a stretch of battle.
    /// Any exception or Debug.LogError during a test fails it, so this catches missing wiring, null
    /// references in Awake/Build/Refresh and broken content that unit tests can't see. Saves go to a
    /// temp folder so the developer's real save is never touched.
    /// </summary>
    public class SceneSmokeTests
    {
        private string _tempSaveDir;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaveDir = Path.Combine(Path.GetTempPath(), "nv_smoke_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSaveDir);
            SaveSystem.OverrideDirectory = _tempSaveDir;
            SaveService.ResetAll();
            // Rich enough to open every screen meaningfully.
            SaveService.Data.Wallet.Add(CurrencyType.Coins, 100000);
            SaveService.Data.Wallet.Add(CurrencyType.Gems, 5000);
            SaveService.SaveNow();
            Time.timeScale = 1f;
            // A clear noon, whatever the real clock says, so rain never waters crops mid-test.
            var noon = new System.DateTime(2026, 6, 1, 12, 0, 0, System.DateTimeKind.Local);
            while (AtmosphereRules.WeatherAt(noon) != Weather.Clear) noon = noon.AddDays(1);
            NinjaVillage.Gameplay.Village.VillageAtmosphere.OverrideLocalTime = () => noon;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            NinjaVillage.Gameplay.Village.VillageAtmosphere.OverrideLocalTime = null;
            yield return null;
            SaveSystem.OverrideDirectory = null;
            SaveService.Load();
            try { Directory.Delete(_tempSaveDir, true); } catch { /* best effort */ }
        }

        private static IEnumerator LoadScene(string name)
        {
            SceneManager.LoadScene(name);
            yield return null; // Awake/OnEnable
            yield return null; // Start
            Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        }

        private static IEnumerator ShowEveryRegisteredScreen(params string[] ids)
        {
            var navigator = UIScreenNavigator.Instance;
            foreach (var id in ids)
            {
                if (!navigator.Has(id)) continue;
                navigator.Show(id);
                yield return null;
                Assert.IsTrue(navigator.Current != null && navigator.Current.ScreenId == id, $"screen '{id}' did not open");
                navigator.Back();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator MainMenu_AllScreensOpen()
        {
            yield return LoadScene(SceneNames.MainMenu);
            var navigator = UIScreenNavigator.Instance;
            Assert.IsTrue(navigator.Has(ScreenIds.Home));
            Assert.AreEqual(ScreenIds.Home, navigator.Current.ScreenId, "Home is the root screen");

            yield return ShowEveryRegisteredScreen(
                ScreenIds.Heroes, ScreenIds.Pets, ScreenIds.Talents, ScreenIds.Inventory, ScreenIds.Collection,
                ScreenIds.DailyLogin, ScreenIds.Quests, ScreenIds.Achievements, ScreenIds.BattlePass, ScreenIds.Events,
                ScreenIds.Store, ScreenIds.Profile, ScreenIds.Settings, ScreenIds.Chapters, ScreenIds.Equipment, ScreenIds.Meals, "leaderboard");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest]
        public IEnumerator MainMenu_EquipmentShowsTheLoadoutAndActsOnIt()
        {
            yield return LoadScene(SceneNames.MainMenu);
            NinjaVillage.Systems.Heroes.HeroService.EnsureDefaults();
            var inv = NinjaVillage.Systems.Inventory.InventoryService.Data;
            var before = NinjaVillage.Systems.Inventory.LoadoutPower.Compute(SaveService.Data);

            // Gear adds attack; so does levelling the weapon.
            NinjaVillage.Systems.Inventory.InventoryService.AddEquipment("iron_ring");
            Assert.AreEqual(NinjaVillage.Systems.Inventory.EquipResult.Ok, NinjaVillage.Systems.Inventory.InventoryService.EquipEquipment("iron_ring"));
            var withRing = NinjaVillage.Systems.Inventory.LoadoutPower.Compute(SaveService.Data);
            Assert.Greater(withRing.Attack, before.Attack, "equipped gear raises ATK");
            Assert.Greater(withRing.Health, 0);

            NinjaVillage.UI.Equipment.EquipmentScreen.Open(NinjaVillage.UI.Equipment.EquipmentScreen.Tab.Gear);
            yield return null;
            var screen = UIScreenNavigator.Instance.Current;
            Assert.AreEqual(ScreenIds.Equipment, screen.ScreenId);
            var root = screen.Root;
            Assert.Greater(root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, 10, "slots, tabs and tiles are built");

            // Tapping a tile opens its card.
            var weaponTile = root.Find("Column").GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.name == "Tile");
            Assert.IsNotNull(weaponTile, "the weapon grid has tiles");
            weaponTile.onClick.Invoke();
            yield return null;
            Assert.IsTrue(root.Find("Popup").gameObject.activeSelf, "a card opens");

            // Every tile wears a small type badge; three of a grade merge into the next (here via "Merge all").
            Assert.IsTrue(root.Find("Column").GetComponentsInChildren<UnityEngine.UI.Image>().Any(i => i.name == "Type" && i.enabled), "type badges");
            NinjaVillage.Systems.Inventory.InventoryService.AddEquipment("iron_ring", 2); // three Common rings with the equipped one
            screen.Refresh();
            var mergeAll = root.Find("Column").GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.name == "MergeAll");
            Assert.IsNotNull(mergeAll, "a Merge all button when something can merge");
            mergeAll.onClick.Invoke();
            Assert.AreEqual(1, inv.GetEquipmentCount("iron_ring", (int)NinjaVillage.Systems.Inventory.ItemGrade.Rare), "3 Common → 1 Rare");
            Assert.IsTrue(inv.IsEquipmentEquipped("iron_ring"), "still equipped, now Rare");

            // Heroes tab: selecting through the card works.
            NinjaVillage.UI.Equipment.EquipmentScreen.Open(NinjaVillage.UI.Equipment.EquipmentScreen.Tab.Heroes);
            yield return null;
            Assert.IsFalse(root.Find("Popup").gameObject.activeSelf, "reopening starts without a card");
            int heroes = NinjaVillage.Systems.Heroes.HeroService.GetSortedHeroes().Count;
            Assert.AreEqual(heroes, root.Find("Column").GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => b.name == "Tile"));

            // Mounts tab: buy the Brown Horse through its card — the showcase hero now sits on it.
            var horse = NinjaVillage.Systems.Mounts.MountService.Get("horse_brown");
            Assert.IsNotNull(horse, "mount catalog missing");
            NinjaVillage.UI.Equipment.EquipmentScreen.Open(NinjaVillage.UI.Equipment.EquipmentScreen.Tab.Mounts);
            yield return null;
            var mountTiles = root.Find("Column").GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name == "Tile").ToList();
            Assert.AreEqual(NinjaVillage.Systems.Mounts.MountService.GetSorted().Count, mountTiles.Count, "a tile per mount");
            var showcaseMount = root.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i => i.name == "Mount");
            Assert.IsFalse(showcaseMount.gameObject.activeSelf, "on foot at first");
            mountTiles[NinjaVillage.Systems.Mounts.MountService.GetSorted().IndexOf(horse)].onClick.Invoke();
            yield return null;
            var unlock = root.Find("Popup").GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>()?.text.StartsWith("Unlock") == true);
            Assert.IsNotNull(unlock, "the card sells the mount");
            unlock.onClick.Invoke();
            yield return null;
            Assert.IsTrue(NinjaVillage.Systems.Mounts.MountService.IsActive(horse), "bought and ridden");
            Assert.IsTrue(showcaseMount.gameObject.activeSelf, "the hero sits on the horse in the showcase");
            Assert.AreEqual(horse.Frames[0], showcaseMount.sprite);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        /// <summary>
        /// Supply crates live in the Shop: the free crate (badged on the Shop) opens with a reveal (chest, flash, the
        /// item pops out) and lands in the inventory; at the pity a Surprise Box hands out an S-class item, shown with
        /// its gold "S"; the Shop lists the S-class collection.
        /// </summary>
        [UnityTest]
        public IEnumerator MainMenu_ShopOpensCratesWithARevealAndSurpriseBoxesHoldSClass()
        {
            yield return LoadScene(SceneNames.MainMenu);
            Assert.IsTrue(ScreenBadges.Has(ScreenIds.Store), "the Shop shows a badge while today's free crate waits");
            UIScreenNavigator.Instance.Show(ScreenIds.Store);
            yield return null;
            var shop = (NinjaVillage.UI.Store.StoreScreen)UIScreenNavigator.Instance.Current;
            var crates = shop.Crates;
            Assert.IsNotNull(crates, "the Shop has a crate shelf");
            Assert.IsNull(shop.Root.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "SClass"), "no S-class list on the Shop page itself");

            // Every crate wears a "?" showing what's inside: each item's picture with its own chance per box.
            var rates = shop.Root.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.name == "Rates").ToList();
            Assert.AreEqual(NinjaVillage.Systems.Crates.CrateRules.Crates.Length, rates.Count, "every crate wears a ?");
            rates[0].onClick.Invoke(); // the Wooden Crate: weapons and gear, no S items
            yield return null;
            Assert.IsTrue(crates.RatesOpen, "the ? opens what's inside");
            var popup = shop.Root.Find("Rates");
            var wood = NinjaVillage.Systems.Crates.CrateRules.Get("wood");
            var gearGrid = popup.GetComponentsInChildren<RectTransform>().First(r => r.name == "Gear");
            Assert.AreEqual(NinjaVillage.Systems.Crates.CrateService.RegularGear().Count, gearGrid.childCount, "every gear piece is pictured");
            Assert.AreEqual(NinjaVillage.Systems.Crates.CrateService.RegularWeapons().Count,
                popup.GetComponentsInChildren<RectTransform>().First(r => r.name == "Weapons").childCount, "and every weapon");
            string gearEach = $"{NinjaVillage.Systems.Crates.CrateService.RegularItemChance(wood, false) * 100f:0.##}%";
            Assert.IsTrue(gearGrid.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == gearEach), $"with its chance ({gearEach})");
            Assert.IsNull(popup.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "SClass"), "no S items in a Wooden Crate");
            crates.CloseRates();

            var box0 = NinjaVillage.Systems.Crates.CrateRules.Get("surprise");
            crates.ShowRates(box0);
            yield return null;
            var sclass = popup.GetComponentsInChildren<RectTransform>().First(r => r.name == "SClass");
            Assert.AreEqual(NinjaVillage.Systems.Crates.CrateService.SPool(box0).Count, sclass.childCount, "every S item is listed");
            string each = $"{NinjaVillage.Systems.Crates.CrateService.SItemChance(box0) * 100f:0.##}%";
            Assert.IsTrue(sclass.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == each), $"each shows its chance ({each})");
            crates.CloseRates();
            Assert.IsFalse(crates.RatesOpen);
            int gearBefore = NinjaVillage.Systems.Inventory.InventoryService.Data.Equipment.Sum(e => e.Count);
            int weaponsBefore = NinjaVillage.Systems.Inventory.InventoryService.Data.WeaponCopies.Sum(e => e.Count);

            var free = shop.Root.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "Open FREE");
            Assert.IsNotNull(free, "a free crate a day");
            free.onClick.Invoke();
            Assert.IsTrue(crates.OverlayOpen && crates.IsRevealing, "the reveal plays");
            float deadline = Time.realtimeSinceStartup + 6f;
            while (crates.IsRevealing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(crates.IsRevealing, "the reveal finishes");
            Assert.AreEqual(1, crates.LastDrops.Count);
            int gearAfter = NinjaVillage.Systems.Inventory.InventoryService.Data.Equipment.Sum(e => e.Count);
            int weaponsAfter = NinjaVillage.Systems.Inventory.InventoryService.Data.WeaponCopies.Sum(e => e.Count);
            Assert.AreEqual(gearBefore + weaponsBefore + 1, gearAfter + weaponsAfter, "the item is in the inventory");
            crates.CloseReveal();
            Assert.IsFalse(ScreenBadges.Has(ScreenIds.Store), "badge gone once it's opened");

            var box = NinjaVillage.Systems.Crates.CrateRules.Get("surprise");
            SaveService.Data.Crates.SurprisePity = box.Pity - 1;
            crates.Open(box, 1, false);
            yield return null;
            shop.Root.Find("Reveal").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // tap to hurry it along
            deadline = Time.realtimeSinceStartup + 4f;
            while (crates.IsRevealing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(crates.LastDrops[0].Special, "S-class at the pity");
            Assert.IsNotNull(shop.Root.Find("Reveal/Results").GetComponentsInChildren<UnityEngine.UI.Image>().FirstOrDefault(i => i.name == "S"), "shown with its S");
            yield return new WaitForSecondsRealtime(0.2f);
        }

        [UnityTest]
        public IEnumerator Village_MapBuildsAndEveryBuildingMenuOpens()
        {
            yield return LoadScene(SceneNames.Village);
            Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.VillageMap>());

            var catalog = VillageService.Catalog;
            Assert.IsNotNull(catalog, "BuildingCatalog missing — run the content generator");
            Assert.Greater(Object.FindObjectsByType<NinjaVillage.Gameplay.Village.BuildingView>(FindObjectsSortMode.None).Length, 0);

            foreach (var building in catalog.All)
            {
                BuildingScreen.Open(building.Id);
                yield return null;
                Assert.AreEqual(ScreenIds.Building, UIScreenNavigator.Instance.Current.ScreenId);
                UIScreenNavigator.Instance.Back();
                yield return null;
            }

            yield return ShowEveryRegisteredScreen(ScreenIds.Forge, ScreenIds.Shrine, ScreenIds.Market,
                ScreenIds.Heroes, ScreenIds.Pets, ScreenIds.Inventory, ScreenIds.Talents, ScreenIds.Decorations,
                ScreenIds.Neighbours, ScreenIds.Profile, ScreenIds.Storehouse, ScreenIds.Equipment, ScreenIds.Kitchen, ScreenIds.Requests, ScreenIds.House, ScreenIds.Fishing, ScreenIds.Store);

            // Upgrading the Dojo exercises cost, save, event and map refresh paths.
            Assert.IsTrue(VillageService.TryUpgrade(VillageService.Get(BuildingIds.Dojo), out var blocker), $"Dojo upgrade blocked: {blocker}");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest]
        public IEnumerator Village_ShowsYourHeroesPetsGearAndTalents()
        {
            NinjaVillage.Systems.Heroes.HeroService.EnsureDefaults();
            SaveService.Data.Pets.Owned.SetLevel("fox", 1);
            SaveService.Data.Pets.ActivePetId = "fox";
            yield return LoadScene(SceneNames.Village);
            yield return null;

            var map = NinjaVillage.Gameplay.Village.VillageMap.Instance;
            Assert.IsNotNull(map);
            var residents = Object.FindObjectsByType<NinjaVillage.Gameplay.Village.VillageResident>(FindObjectsSortMode.None);
            Assert.IsTrue(System.Array.Exists(residents, r => r.name.StartsWith("Hero_")), "your heroes live in the village");
            Assert.IsTrue(System.Array.Exists(residents, r => r.name == "Pet_fox"), "your pets live in the village");
            Assert.IsTrue(System.Array.Exists(residents, r => r.name.StartsWith("npc_")), "townsfolk walk around");
            Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.ArmoryDisplay>());
            Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.TalentTreeDisplay>());

            // Buildings use their pack sprites, not placeholder shapes.
            var castle = GameObject.Find("Building_castle").transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            Assert.AreNotEqual(NinjaVillage.Core.Utilities.GeneratedSprites.Square, castle.sprite);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest]
        public IEnumerator Village_BuyPlaceMoveAndSellADecoration()
        {
            yield return LoadScene(SceneNames.Village);
            yield return null;
            var placer = NinjaVillage.Gameplay.Village.DecorationPlacer.Instance;
            Assert.IsNotNull(placer, "your own village can be decorated");
            var well = NinjaVillage.Systems.Village.DecorationService.Get("well");
            Assert.IsNotNull(well, "decoration catalog missing — run the content generator");
            int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);

            placer.Begin(well, new Vector2(-4f, 6f));
            Assert.IsTrue(placer.Fits, $"test spot should be free: {placer.Blocker}");
            Assert.IsTrue(placer.Confirm(out var error), error);
            yield return null;
            Assert.AreEqual(coins - well.Price.Amount, SaveService.Data.Wallet.Get(CurrencyType.Coins), "paid on placement");
            var placed = SaveService.Data.Village.Decorations[0];
            Assert.IsNotNull(NinjaVillage.Gameplay.Village.VillageMap.Instance.FindDecoration(placed.Uid), "the well stands in the village");

            placer.Begin(well, new Vector2(0f, 3.5f)); // the road up to the castle
            Assert.IsFalse(placer.Fits, "roads stay clear");
            placer.Cancel();
            Assert.AreEqual(coins - well.Price.Amount, SaveService.Data.Wallet.Get(CurrencyType.Coins), "cancelling is free");

            Assert.IsTrue(NinjaVillage.Systems.Village.DecorationService.TryMove(placed.Uid, new Vector2(-5f, 6f), false, out error), error);
            Assert.AreEqual(-5f, placed.X);
            Assert.IsTrue(NinjaVillage.Systems.Village.DecorationService.TrySell(placed.Uid, out _));
            yield return null;
            Assert.AreEqual(coins - well.Price.Amount + well.Price.Amount / 2, SaveService.Data.Wallet.Get(CurrencyType.Coins), "half back");
            Assert.IsNull(NinjaVillage.Gameplay.Village.VillageMap.Instance.FindDecoration(placed.Uid));
        }

        [UnityTest]
        public IEnumerator Village_VisitingAnotherPlayersVillage_IsReadOnly()
        {
            var other = new NinjaVillage.Systems.Village.VillageSnapshot { DisplayName = "Kage", PlayerId = "someone-else", HighestWave = 30 };
            other.Buildings.Add(new IdLevelEntry("castle", 8));
            other.Heroes.Add(new NinjaVillage.Systems.Village.VillageHero { Id = "samurai", Level = 7 });
            other.SelectedHeroId = "samurai";
            other.Pets.Add(new IdLevelEntry("wolf", 3));
            other.Decorations.Add(new PlacedDecoration { Uid = 1, Id = "well", X = -4f, Y = 6f });

            NinjaVillage.Systems.Village.VillageVisit.Visit(other);
            yield return LoadScene(SceneNames.Village);
            yield return null;

            var map = NinjaVillage.Gameplay.Village.VillageMap.Instance;
            Assert.IsFalse(map.IsOwnVillage);
            Assert.AreEqual("Kage", map.Snapshot.DisplayName);
            Assert.IsNull(NinjaVillage.Gameplay.Village.DecorationPlacer.Instance, "visitors can't decorate");
            Assert.IsNotNull(GameObject.Find("Hero_samurai"), "their heroes live there");
            Assert.IsNotNull(GameObject.Find("Pet_wolf"), "and their pets");
            Assert.IsNotNull(map.FindDecoration(1), "and their decorations");
            Assert.AreEqual(0, SaveService.Data.Village.Decorations.Count, "nothing leaks into your own save");

            NinjaVillage.Systems.GameFlow.SceneLoader.LoadMainMenu();
            yield return null;
            Assert.IsFalse(NinjaVillage.Systems.Village.VillageVisit.IsVisiting, "leaving ends the visit");
        }

        [UnityTest]
        public IEnumerator Village_FarmPlantWaterHarvestAndSell()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                yield return LoadScene(SceneNames.Village);
                yield return null;
                Assert.AreEqual(NinjaVillage.Systems.Farm.FarmRules.MaxPlots, Object.FindObjectsByType<NinjaVillage.Gameplay.Village.FarmPlotView>(FindObjectsSortMode.None).Length);

                var rice = NinjaVillage.Systems.Farm.FarmService.GetCrop("rice");
                Assert.IsNotNull(rice, "crop catalog missing — run the content generator");
                Assert.AreEqual(NinjaVillage.Systems.Farm.FarmResult.PlotLocked, NinjaVillage.Systems.Farm.FarmService.Plant(5, rice), "plot 6 needs a bigger castle");

                int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                Assert.AreEqual(NinjaVillage.Systems.Farm.FarmResult.Success, NinjaVillage.Systems.Farm.FarmService.Plant(0, rice));
                Assert.AreEqual(coins - rice.SeedCost, SaveService.Data.Wallet.Get(CurrencyType.Coins), "seeds cost coins");
                Assert.AreEqual(NinjaVillage.Systems.Farm.FarmResult.NotRipe, NinjaVillage.Systems.Farm.FarmService.Harvest(0, out _, out _));

                var before = NinjaVillage.Systems.Farm.FarmService.TimeLeft(0);
                Assert.AreEqual(NinjaVillage.Systems.Farm.FarmResult.Success, NinjaVillage.Systems.Farm.FarmService.Water(0));
                Assert.Less(NinjaVillage.Systems.Farm.FarmService.TimeLeft(0), before, "watering speeds it up");

                now = now.AddSeconds(rice.GrowSeconds); // ...time passes
                yield return null;
                Assert.AreEqual(NinjaVillage.Systems.Farm.CropStage.Ripe, NinjaVillage.Systems.Farm.FarmService.Stage(0));
                Assert.AreEqual(1, NinjaVillage.Systems.Farm.FarmService.RipeCount());

                GameObject.Find("FarmPlot_0").GetComponent<NinjaVillage.Gameplay.Village.FarmPlotView>().OnTapped(); // tap to harvest
                Assert.AreEqual(rice.HarvestAmount, NinjaVillage.Systems.Farm.GoodsService.Count("rice"));
                Assert.AreEqual(NinjaVillage.Systems.Farm.CropStage.Empty, NinjaVillage.Systems.Farm.FarmService.Stage(0));

                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                int earned = NinjaVillage.Systems.Farm.GoodsService.Sell(rice.Harvest, int.MaxValue);
                Assert.AreEqual(rice.HarvestAmount * rice.Harvest.SellPrice, earned);
                Assert.AreEqual(coins + earned, SaveService.Data.Wallet.Get(CurrencyType.Coins));
                Assert.AreEqual(0, NinjaVillage.Systems.Farm.GoodsService.Count("rice"));

                NinjaVillage.Systems.Farm.FarmService.Plant(1, rice);
                Assert.AreEqual(1, NinjaVillage.Systems.Village.VillageSnapshot.FromSave(SaveService.Data).Farm.Count, "visitors see what grows");
                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 2: build the Kitchen, cook onigiri from rice, collect it, pack it — and the next battle eats
        /// exactly one and starts with the boost (and the meal on the skill bar).
        /// </summary>
        [UnityTest]
        public IEnumerator Village_KitchenCooksAMealThatPowersUpTheNextBattle()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                SaveService.Data.Village.Buildings.SetLevel(BuildingIds.Castle, 2);
                NinjaVillage.Systems.Farm.GoodsService.Add("rice", 3);
                yield return LoadScene(SceneNames.Village);
                yield return null;
                Assert.IsNotNull(GameObject.Find($"Building_{BuildingIds.Kitchen}"), "the Kitchen stands on the map");

                var onigiri = NinjaVillage.Systems.Kitchen.KitchenService.GetRecipe("onigiri");
                Assert.IsNotNull(onigiri, "recipe catalog missing — run the content generator");
                Assert.AreEqual(NinjaVillage.Systems.Kitchen.KitchenResult.NotBuilt, NinjaVillage.Systems.Kitchen.KitchenService.Cook(onigiri));
                Assert.IsTrue(VillageService.TryUpgrade(VillageService.Get(BuildingIds.Kitchen), out var blocker), blocker.ToString());
                Assert.AreEqual(1, NinjaVillage.Systems.Kitchen.KitchenService.SlotCount);

                UIScreenNavigator.Instance.Show(ScreenIds.Kitchen);
                yield return null;
                var cook = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "Cook"
                                         && b.GetComponentInParent<UnityEngine.UI.VerticalLayoutGroup>().name.Contains("Onigiri"));
                Assert.IsNotNull(cook, "the Onigiri recipe has a Cook button");
                cook.onClick.Invoke();
                Assert.AreEqual(0, NinjaVillage.Systems.Farm.GoodsService.Count("rice"), "cooking uses the rice");
                Assert.IsNotNull(NinjaVillage.Systems.Kitchen.KitchenService.GetJob(0));
                Assert.AreEqual(NinjaVillage.Systems.Kitchen.KitchenResult.NoFreeSlot, NinjaVillage.Systems.Kitchen.KitchenService.CheckCook(onigiri));
                Assert.AreEqual(NinjaVillage.Systems.Kitchen.KitchenResult.NotReady, NinjaVillage.Systems.Kitchen.KitchenService.Collect(0, out _, out _));

                now = now.AddSeconds(onigiri.CookSeconds); // ...time passes
                yield return new WaitForSecondsRealtime(0.6f); // the stove turns to "Collect"
                Assert.AreEqual(1, NinjaVillage.Systems.Kitchen.KitchenService.ReadyCount());
                var collect = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "Collect");
                Assert.IsNotNull(collect, "a finished meal can be collected");
                collect.onClick.Invoke();
                Assert.AreEqual(1, NinjaVillage.Systems.Farm.GoodsService.Count("onigiri"));

                var meal = NinjaVillage.Systems.Farm.GoodsService.Get("onigiri");
                Assert.AreEqual(NinjaVillage.Systems.Kitchen.MealToggle.Added, NinjaVillage.Systems.Kitchen.KitchenService.ToggleMeal(meal));
                int healthWithoutMeal = NinjaVillage.Systems.Inventory.LoadoutPower.Compute(SaveService.Data).Health;

                yield return LoadScene(SceneNames.Battle);
                yield return null;
                var run = PlayerReference.Instance.GetComponent<NinjaVillage.Systems.Meta.RunBootstrapper>().Context;
                Assert.AreEqual(0, NinjaVillage.Systems.Farm.GoodsService.Count("onigiri"), "the battle ate the onigiri");
                Assert.IsTrue(NinjaVillage.Systems.Kitchen.KitchenService.IsSelected("onigiri"), "it stays packed for when you cook more");
                Assert.AreEqual(1, NinjaVillage.Systems.Kitchen.KitchenService.MealsThisRun.Count);
                // Onigiri adds 10 percentage points on top of the loadout's own health bonuses (e.g. the Assassin's -20%).
                Assert.AreEqual(healthWithoutMeal + meal.MealValue * (run.BaseMaxHealth + run.MaxHealthFlatBonus), run.FinalMaxHealth, 1f,
                    "onigiri: +10% max health this run");
                Assert.AreEqual(run.FinalMaxHealth, PlayerReference.Instance.GetComponent<NinjaVillage.Core.Combat.Health>().MaxHealth, 0.01f);
                Assert.IsNotNull(GameObject.Find("Meal_onigiri"), "the meal shows on the skill bar");
            }
            finally
            {
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 3: villagers with a "!" stand on the plaza; tapping one opens their request; delivering pays out,
        /// stat requests count through the quest feed, and a decoration reward can be placed for free.
        /// </summary>
        [UnityTest]
        public IEnumerator Village_VillagersAskForFavours()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                var requests = SaveService.Data.Requests;
                requests.Day = NinjaVillage.Core.Utilities.GameClock.Today;
                requests.Active.Clear();
                requests.Active.Add(new VillagerRequestState { Id = "bring_rice", VillagerKey = "npc_villager", Target = 6 });
                requests.Active.Add(new VillagerRequestState { Id = "harvest_crops", VillagerKey = "npc_villager3", Target = 12 });
                requests.Active.Add(new VillagerRequestState { Id = "clear_chapter", VillagerKey = "npc_master", Target = 1, Chapter = 1 });

                yield return LoadScene(SceneNames.Village);
                yield return null;
                var givers = NinjaVillage.Gameplay.Village.VillageMap.Instance.RequestGivers;
                Assert.AreEqual(3, givers.Count, "three villagers with requests on the plaza");
                Assert.IsTrue(givers.All(g => g.HasAlert), "each wears a !");

                givers[0].OnTapped();
                yield return null;
                Assert.AreEqual(ScreenIds.Requests, UIScreenNavigator.Instance.Current.ScreenId, "tapping a villager opens the requests");
                UnityEngine.UI.Button Deliver() => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.interactable && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "Deliver");
                Assert.IsNull(Deliver(), "no rice yet");

                NinjaVillage.Systems.Farm.GoodsService.Add("rice", 6);
                UIScreenNavigator.Instance.Current.Refresh();
                int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                Deliver().onClick.Invoke();
                Assert.AreEqual(0, NinjaVillage.Systems.Farm.GoodsService.Count("rice"), "the rice was handed over");
                Assert.Greater(SaveService.Data.Wallet.Get(CurrencyType.Coins), coins, "and paid for");
                Assert.IsTrue(requests.Active[0].Delivered);
                Assert.IsFalse(givers[0].HasAlert, "no more ! once it's done");

                NinjaVillage.Core.Events.Progress.Report(NinjaVillage.Core.Events.ProgressStatIds.CropsHarvested, 12); // via the quest tracker
                Assert.IsTrue(NinjaVillage.Systems.Requests.RequestService.CanDeliver(requests.Active[1]), "harvests count towards the request");

                SaveService.Data.Chapters.HighestCleared = 1;
                Assert.IsTrue(NinjaVillage.Systems.Requests.RequestService.Deliver(requests.Active[2], out var reward));
                Assert.IsNotNull(reward.Decoration, "clearing a chapter earns a decoration");

                UIScreenNavigator.Instance.Back();
                var placer = NinjaVillage.Gameplay.Village.DecorationPlacer.Instance;
                int placed = DecorationService.Placed.Count;
                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                placer.BeginGift(reward.Decoration, new Vector2(-8f, 9f));
                Assert.IsTrue(placer.Fits, placer.Blocker.ToString());
                Assert.IsNull(placer.PriceToPay, "gifts are free");
                Assert.IsTrue(placer.Confirm(out var error), error);
                Assert.AreEqual(placed + 1, DecorationService.Placed.Count);
                Assert.AreEqual(coins, SaveService.Data.Wallet.Get(CurrencyType.Coins));
                Assert.AreEqual(0, DecorationService.GiftCount(reward.Decoration));
                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 4: tap a house plot, build, repaint for free and grow it — families move in — then the treasury
        /// fills over (fake) time and a tap collects it.
        /// </summary>
        [UnityTest]
        public IEnumerator Village_BuildHousesAndCollectTheTreasury()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                SaveService.Data.Village.Buildings.SetLevel(BuildingIds.Castle, 3);
                yield return LoadScene(SceneNames.Village);
                yield return null;
                var houses = Object.FindObjectsByType<NinjaVillage.Gameplay.Village.HouseView>(FindObjectsSortMode.None);
                Assert.AreEqual(HousingRules.MaxPlots, houses.Length, "six house plots on the lane");
                var treasury = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.TreasuryView>();
                Assert.IsNotNull(treasury, "the treasury stands on the plaza");
                int townsfolk = GameObject.Find("Townsfolk").transform.childCount;

                var plot0 = houses.First(h => h.Plot == 0);
                plot0.OnTapped();
                yield return null;
                Assert.AreEqual(ScreenIds.House, UIScreenNavigator.Instance.Current.ScreenId, "tapping a plot opens the house menu");
                UnityEngine.UI.Button ButtonStartingWith(string text) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.interactable && (b.GetComponentInChildren<TMPro.TMP_Text>()?.text ?? "").StartsWith(text));

                int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                ButtonStartingWith("Build").onClick.Invoke();
                Assert.IsNotNull(HouseService.Get(0), "built");
                Assert.AreEqual(coins - HousingRules.BuildCost(0), SaveService.Data.Wallet.Get(CurrencyType.Coins));

                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                GameObject.Find("Style_clay").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.AreEqual("clay", HouseService.Get(0).Style, "repainted");
                Assert.AreEqual(coins, SaveService.Data.Wallet.Get(CurrencyType.Coins), "for free");

                ButtonStartingWith("Grow").onClick.Invoke();
                Assert.AreEqual(2, HouseService.Get(0).Level);
                yield return null;
                Assert.AreEqual(townsfolk + 2, GameObject.Find("Townsfolk").transform.childCount, "a family per house level");
                Assert.IsTrue(plot0.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Body" && r.enabled && r.sprite != null), "the house is drawn");

                UIScreenNavigator.Instance.Back();
                houses.First(h => h.Plot == 5).OnTapped();
                yield return null;
                Assert.AreEqual(ScreenIds.VillageHud, UIScreenNavigator.Instance.Current.ScreenId, "a locked plot only says when it opens");

                now = now.AddHours(3);
                int expected = Mathf.FloorToInt(HousingRules.CoinsPerHour(3, 2) * 3f);
                Assert.AreEqual(expected, TreasuryService.Available);
                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                treasury.OnTapped();
                Assert.AreEqual(coins + expected, SaveService.Data.Wallet.Get(CurrencyType.Coins), "tap to collect");
                Assert.AreEqual(0, TreasuryService.Available);
                yield return null;
                Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.CoinBurst>(), "with a burst of coins");
                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 5: tap the pond, cast, wait for a bite and reel in while the marker is in the zone; the mine digs
        /// bars over (fake) time and a tap on it collects them; Sushi is on the Kitchen's menu.
        /// </summary>
        [UnityTest]
        public IEnumerator Village_FishAtThePondAndCollectFromTheMine()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                SaveService.Data.Village.Buildings.SetLevel(BuildingIds.Castle, 3);
                SaveService.Data.Village.Buildings.SetLevel(BuildingIds.Mine, 1);
                yield return LoadScene(SceneNames.Village);
                yield return null;

                var pond = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.PondView>();
                Assert.IsNotNull(pond, "the pond is on the map");
                pond.OnTapped();
                yield return null;
                Assert.AreEqual(ScreenIds.Fishing, UIScreenNavigator.Instance.Current.ScreenId);
                var fishing = (NinjaVillage.UI.Village.FishingScreen)UIScreenNavigator.Instance.Current;
                Assert.AreEqual(PondMineRules.MaxCasts, FishingService.Casts, "a full set of casts to start");

                UnityEngine.UI.Button ButtonLabelled(string text) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.interactable && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == text);
                ButtonLabelled("Cast").onClick.Invoke();
                Assert.AreEqual(PondMineRules.MaxCasts - 1, FishingService.Casts, "casting uses a cast");

                float deadline = Time.realtimeSinceStartup + 4f;
                while (!fishing.IsBiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(fishing.IsBiting, "something bites");
                deadline = Time.realtimeSinceStartup + 4f;
                while (!fishing.MarkerInZone && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(fishing.MarkerInZone, "the marker sweeps through the zone");
                ButtonLabelled("REEL IN!").onClick.Invoke();
                Assert.Greater(FishingService.TotalCaught, 0, "reeling in on the zone lands it");
                UIScreenNavigator.Instance.Back();
                yield return null;

                now = now.AddHours(2);
                Assert.AreEqual(8, MineService.Available, "4 bars an hour at Mine Lv 1");
                GameObject.Find($"Building_{BuildingIds.Mine}").GetComponent<NinjaVillage.Gameplay.Village.BuildingView>().OnTapped();
                Assert.AreEqual(8, NinjaVillage.Systems.Farm.GoodsService.Count("iron_bar"), "a tap collects the bars");
                Assert.AreEqual(0, MineService.Available);
                Assert.AreEqual(ScreenIds.VillageHud, UIScreenNavigator.Instance.Current.ScreenId, "collecting doesn't open a menu");

                Assert.IsNotNull(NinjaVillage.Systems.Kitchen.KitchenService.GetRecipe("sushi"), "fish make Sushi now");
                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 6 on the in-memory backend: visiting a neighbour you can like (weekly), leave a gift and water their
        /// crops (daily); back home, your visitors' records turn into coins, watered crops, this week's likes and —
        /// having topped last week's ranking — a trophy decoration.
        /// </summary>
        [UnityTest]
        public IEnumerator Village_VisitorsLikeGiftAndWater_AndTheOwnerGetsIt()
        {
            var now = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc);
            NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = () => now;
            try
            {
                var provider = NinjaVillage.Systems.Backend.BackendService.Provider;
                Assert.IsNotNull(provider, "backend not started");
                int today = NinjaVillage.Core.Utilities.GameClock.Today, week = NinjaVillage.Core.Utilities.GameClock.ThisWeek;
                string me = NinjaVillage.Systems.Social.SocialService.MyId;

                // --- visiting Aiko
                var friend = new VillageSnapshot { PlayerId = "friend_aiko", DisplayName = "Aiko" };
                friend.Buildings.Add(new IdLevelEntry(BuildingIds.Castle, 2));
                VillageVisit.Visit(friend);
                yield return LoadScene(SceneNames.Village);
                yield return null;
                UnityEngine.UI.Button Labelled(string text) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == text);
                Assert.IsNotNull(Labelled("Like"), "the visitor bar is up");

                int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                Labelled("Like").onClick.Invoke();
                Labelled("Gift").onClick.Invoke();
                Labelled("Water").onClick.Invoke();
                yield return null;
                var mine = provider.ListVisitsAsync("friend_aiko").Result.FirstOrDefault(v => v.VisitorId == me);
                Assert.IsNotNull(mine, "our visit is in their visitors' book");
                Assert.AreEqual(week, mine.LikedWeek);
                Assert.AreEqual(today, mine.GiftDay);
                Assert.AreEqual(today, mine.WaterDay);
                Assert.AreEqual(coins + NinjaVillage.Systems.Social.SocialRules.HelperCoins, SaveService.Data.Wallet.Get(CurrencyType.Coins), "helping pays");
                Assert.IsNotNull(Labelled("Liked"), "a like a week");
                Assert.IsFalse(Labelled("Liked").interactable);
                VillageVisit.ReturnHome();

                // --- back home: two neighbours came by
                SaveService.Data.Wallet.Add(CurrencyType.Coins, 100);
                NinjaVillage.Systems.Farm.FarmService.Plant(0, NinjaVillage.Systems.Farm.FarmService.GetCrop("radish"));
                provider.WriteVisitAsync(new NinjaVillage.Systems.Backend.VisitRecord
                {
                    VillageId = me, VisitorId = "friend_kenji", VisitorName = "Kenji", VisitDay = today, LikedWeek = week,
                    GiftDay = today, WaterDay = today, WaterTicks = now.AddMinutes(1).Ticks,
                });
                provider.WriteVisitAsync(new NinjaVillage.Systems.Backend.VisitRecord
                {
                    VillageId = me, VisitorId = "friend_jin", VisitorName = "Jin", VisitDay = today, LikedWeek = week,
                });
                provider.WriteVillageAsync(new NinjaVillage.Systems.Backend.PublicVillage { UserId = me, DisplayName = "Me", Likes = 9, LikesWeek = week - 1 });
                SaveService.Data.Social.TrophyCheckedWeek = -1;
                now = now.AddMinutes(2);

                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                var refresh = NinjaVillage.Systems.Social.SocialService.RefreshAsync(force: true);
                while (!refresh.IsCompleted) yield return null;
                var news = refresh.Result;
                Assert.AreEqual(2, news.VisitorsToday.Count);
                Assert.AreEqual(2, NinjaVillage.Systems.Social.SocialService.LikesThisWeek);
                Assert.AreEqual(coins + NinjaVillage.Systems.Social.SocialRules.GiftCoins, SaveService.Data.Wallet.Get(CurrencyType.Coins), "Kenji's gift");
                Assert.IsTrue(NinjaVillage.Systems.Farm.FarmService.GetPlot(0).Watered, "Kenji watered the radishes");
                Assert.AreEqual(1, news.TrophyRank, "most liked last week");
                Assert.AreEqual(1, DecorationService.GiftCount(DecorationService.Get(NinjaVillage.Systems.Social.SocialRules.TrophyDecorationId)));
                StringAssert.Contains("2 ninjas visited today", NinjaVillage.Systems.Social.SocialService.Describe(news));

                coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
                refresh = NinjaVillage.Systems.Social.SocialService.RefreshAsync(force: true);
                while (!refresh.IsCompleted) yield return null;
                Assert.AreEqual(coins, SaveService.Data.Wallet.Get(CurrencyType.Coins), "a gift pays out once");
                Assert.AreEqual(0, refresh.Result.TrophyRank, "one trophy a week");
                yield return new WaitForSecondsRealtime(0.2f);
            }
            finally
            {
                VillageVisit.ReturnHome();
                NinjaVillage.Core.Utilities.GameClock.OverrideUtcNow = null;
            }
        }

        /// <summary>
        /// Phase 7: on a rainy night (phone clock pinned) the map is tinted blue, a lantern and the castle glow, rain
        /// falls and waters the crops; a pet can be petted and fed from its care bar (hearts, a heart badge, +15% today);
        /// at noon the tint and glows are gone.
        /// </summary>
        [UnityTest]
        public IEnumerator Village_RainyNightLanterns_AndPetCare()
        {
            var night = new System.DateTime(2026, 6, 1, 22, 0, 0, System.DateTimeKind.Local);
            while (AtmosphereRules.WeatherAt(night) != Weather.Rain) night = night.AddDays(1);
            var clock = night;
            NinjaVillage.Gameplay.Village.VillageAtmosphere.OverrideLocalTime = () => clock;
            try
            {
                var pet = NinjaVillage.Systems.Pets.PetService.GetSortedPets().FirstOrDefault();
                Assert.IsNotNull(pet, "pet catalog missing");
                SaveService.Data.Pets.Owned.SetLevel(pet.Id, 1);
                SaveService.Data.Pets.ActivePetId = pet.Id;
                SaveService.Data.Village.Decorations.Add(new PlacedDecoration { Uid = 90, Id = "lantern_post", X = -4f, Y = 6f });
                NinjaVillage.Systems.Farm.FarmService.Plant(0, NinjaVillage.Systems.Farm.FarmService.GetCrop("rice"));
                NinjaVillage.Systems.Farm.GoodsService.Add("fish", 1);

                yield return LoadScene(SceneNames.Village);
                yield return new WaitForSecondsRealtime(0.5f);
                var sky = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Village.VillageAtmosphere>();
                Assert.IsNotNull(sky, "the village has an atmosphere");
                Assert.AreEqual(Weather.Rain, sky.CurrentWeather);
                Assert.Greater(sky.Darkness, 0.9f);
                Assert.Greater(sky.SkyTint.a, 0.3f, "night tint");
                Assert.GreaterOrEqual(sky.GlowCount, 2, "the lantern and the castle");
                Assert.IsTrue(sky.Glows.All(g => g.color.a > 0.3f), "glowing in the dark");
                Assert.GreaterOrEqual(GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "Raindrop"), 100);
                Assert.IsTrue(NinjaVillage.Systems.Farm.FarmService.GetPlot(0).Watered, "the rain watered the rice");

                var resident = NinjaVillage.Gameplay.Village.VillageMap.Instance.FindPet(pet.Id);
                Assert.IsNotNull(resident, "the pet lives in the village");
                resident.OnTapped();
                yield return null;
                Assert.IsTrue(GameObject.Find("PetBar") != null && GameObject.Find("PetBar").activeInHierarchy, "its care bar opens");
                UnityEngine.UI.Button Starting(string text) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.interactable && (b.GetComponentInChildren<TMPro.TMP_Text>()?.text ?? "").StartsWith(text));
                Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .First(b => b.isActiveAndEnabled && b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "Pet").onClick.Invoke(); // not "Pets"
                yield return null;
                Assert.IsFalse(NinjaVillage.Systems.Pets.PetCareService.CanPet(pet.Id));
                Assert.Greater(Object.FindObjectsByType<NinjaVillage.Gameplay.Village.FloatingHeart>(FindObjectsSortMode.None).Length, 0, "hearts float up");
                Assert.IsTrue(resident.HasBadge, "a happy pet wears a heart");
                Starting("Feed").onClick.Invoke();
                Assert.AreEqual(0, NinjaVillage.Systems.Farm.GoodsService.Count("fish"), "it ate the fish");
                Assert.AreEqual(1.15f, NinjaVillage.Systems.Pets.PetCareService.PowerScale(SaveService.Data, pet.Id), 0.0001f);

                clock = new System.DateTime(2026, 6, 1, 12, 0, 0, System.DateTimeKind.Local);
                while (AtmosphereRules.WeatherAt(clock) != Weather.Clear) clock = clock.AddDays(1);
                yield return null;
                yield return null;
                Assert.AreEqual(0f, sky.SkyTint.a, 0.001f, "noon: no tint");
                Assert.IsTrue(sky.Glows.All(g => g.color.a < 0.01f), "lanterns off by day");
                Assert.AreEqual(0, GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "Raindrop"), "the rain stopped");
            }
            finally
            {
                NinjaVillage.Gameplay.Village.VillageAtmosphere.OverrideLocalTime = null;
            }
        }

        [UnityTest]
        public IEnumerator Battle_PlaysForAWhileWithoutErrors()
        {
            yield return LoadScene(SceneNames.Battle);
            Assert.IsNotNull(PlayerReference.Instance, "no player in Battle");

            // Level-ups pause the game for the skill choice; keep picking the first card so the run continues.
            float end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end)
            {
                if (Time.timeScale == 0f)
                {
                    var panel = Object.FindAnyObjectByType<NinjaVillage.UI.Battle.SkillChoicePanel>();
                    var buttons = panel != null ? panel.GetComponentsInChildren<UnityEngine.UI.Button>() : null;
                    if (buttons != null && buttons.Length > 0) buttons[0].onClick.Invoke();
                    else Time.timeScale = 1f;
                }
                yield return null;
            }

            var runStart = PlayerReference.Instance.GetComponent<NinjaVillage.Systems.Meta.RunBootstrapper>();
            Assert.IsNotNull(runStart, "RunBootstrapper missing on the player");
            Assert.IsNotNull(runStart.Context, "run start modifiers never ran");
        }

#if UNITY_EDITOR
        /// <summary>Each ultimate spawns its pixel-art effect (not the placeholder shapes) without errors.</summary>
        [UnityTest]
        public IEnumerator Battle_EveryUltimatePlaysItsArtEffect()
        {
            yield return LoadScene(SceneNames.Battle);
            var player = PlayerReference.Instance;
            var runner = player.GetComponent<NinjaVillage.Gameplay.Ultimates.UltimateController>();
            var stats = player.GetComponent<PlayerStats>();
            var mask = player.GetComponent<NinjaVillage.Gameplay.Combat.AutoAttackController>().EnemyMask;

            foreach (var (asset, effect) in new[]
                     {
                         ("Ultimate_DragonSlash", "DragonSlash_Dragon"),
                         ("Ultimate_HeavenlyStorm", "HeavenlyStorm_Shuriken"),
                         ("Ultimate_ShadowCloneArmy", "ShadowBody"),
                     })
            {
                var ultimate = UnityEditor.AssetDatabase.LoadAssetAtPath<NinjaVillage.Gameplay.Ultimates.UltimateDefinition>(
                    $"Assets/_Project/Data/Ultimates/{asset}.asset");
                Assert.IsNotNull(ultimate, asset);
                ultimate.Activate(new NinjaVillage.Gameplay.Ultimates.UltimateContext(runner, player.transform, stats, mask));

                SpriteRenderer found = null;
                for (float end = Time.realtimeSinceStartup + 2f; found == null && Time.realtimeSinceStartup < end;)
                {
                    Time.timeScale = 1f; // level-ups from the kills would pause the effect
                    var go = GameObject.Find(effect);
                    found = go != null ? go.GetComponent<SpriteRenderer>() : null;
                    yield return null;
                }
                Assert.IsNotNull(found, $"{asset}: '{effect}' never appeared");
                Assert.IsNotNull(found.sprite, $"{asset}: '{effect}' has no art");
            }
            yield return new WaitForSecondsRealtime(1f);
        }
#endif

        [UnityTest]
        public IEnumerator Battle_ChapterSetsUpItsMapWavesAndHud()
        {
            var chapters = NinjaVillage.Systems.Chapters.ChapterService.GetChapters();
            Assert.GreaterOrEqual(chapters.Count, 2, "chapter catalog missing — run the content generator");
            SaveService.Data.Chapters.HighestCleared = 1;
            var second = chapters[1];
            Assert.IsTrue(NinjaVillage.Systems.Chapters.ChapterService.TrySelect(second));

            yield return LoadScene(SceneNames.Battle);
            Assert.AreEqual(second, NinjaVillage.Systems.Chapters.ChapterDirector.Current);

            var waves = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Waves.WaveManager>();
            Assert.IsFalse(waves.Endless, "chapters end with their final boss");
            Assert.AreEqual(second.WaveCount, waves.Waves.Count);
            Assert.IsTrue(waves.Waves[waves.Waves.Count - 1].IsBossWave, "the last wave is the final boss");

            var ground = Object.FindAnyObjectByType<NinjaVillage.Gameplay.World.InfiniteGround>();
            Assert.AreEqual(second.Ground, ground.GetComponent<SpriteRenderer>().sprite);
            Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.UI.Battle.WaveRoadmapUI>(), "roadmap missing from the HUD");
            Assert.IsNotNull(Object.FindAnyObjectByType<NinjaVillage.UI.Battle.MinimapUI>(), "minimap missing from the HUD");

            yield return new WaitForSecondsRealtime(1f);
            var props = Object.FindAnyObjectByType<NinjaVillage.Gameplay.World.PropScatter>();
            Assert.Greater(props.GetComponentsInChildren<SpriteRenderer>().Length, 0, "no scenery scattered around the player");
        }

        [UnityTest]
        public IEnumerator Battle_BeatingTheFinalWaveClearsTheChapter()
        {
            var chapters = NinjaVillage.Systems.Chapters.ChapterService.GetChapters();
            yield return LoadScene(SceneNames.Battle);
            Assert.AreEqual(chapters[0], NinjaVillage.Systems.Chapters.ChapterDirector.Current, "a new player starts at chapter 1");
            int gems = SaveService.Data.Wallet.Get(CurrencyType.Gems);

            EventBus<NinjaVillage.Gameplay.Waves.AllWavesCompleteEvent>.Raise(new NinjaVillage.Gameplay.Waves.AllWavesCompleteEvent());
            yield return null;

            var result = NinjaVillage.Systems.Chapters.ChapterService.LastResult;
            Assert.IsTrue(result != null && result.Victory && result.FirstClear);
            Assert.AreEqual(1, SaveService.Data.Chapters.HighestCleared);
            Assert.AreEqual(gems + chapters[0].ClearGems, SaveService.Data.Wallet.Get(CurrencyType.Gems));
            Assert.AreEqual(chapters[1], NinjaVillage.Systems.Chapters.ChapterService.Selected, "START moves on to chapter 2");
            Assert.AreEqual(chapters[0].Id, SaveService.Data.Profile.RecentRuns[0].ChapterId);

            var title = GameObject.Find("GameOverPanel").transform.Find("TitleText").GetComponent<TMPro.TMP_Text>();
            StringAssert.Contains("CLEAR", title.text);
        }

#if UNITY_EDITOR
        private static GameObject Wall(Vector2 at, Vector2 size)
        {
            Assert.IsTrue(NinjaVillage.Gameplay.World.Obstacles.Available, "Obstacle layer missing — run the content generator");
            var wall = new GameObject("TestWall") { layer = NinjaVillage.Gameplay.World.Obstacles.Layer };
            wall.transform.position = at;
            wall.AddComponent<BoxCollider2D>().size = size;
            return wall;
        }

        /// <summary>Holds the player still and untouchable so only the thing under test moves.</summary>
        private static GameObject QuietPlayer()
        {
            var player = PlayerReference.Instance.gameObject;
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<NinjaVillage.Gameplay.Combat.AutoAttackController>().enabled = false;
            player.GetComponent<NinjaVillage.Core.Combat.Health>().GrantInvulnerability(60f);
            return player;
        }

        [UnityTest]
        public IEnumerator Battle_RocksBlockWalking_JumpingHopsOverThem()
        {
            yield return LoadScene(SceneNames.Battle);
            var player = QuietPlayer();
            var body = player.GetComponent<Rigidbody2D>();
            Vector2 start = body.position;
            Wall(start + new Vector2(2f, 0f), new Vector2(0.8f, 4f));

            for (float t = 0f; t < 1.2f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                body.MovePosition(body.position + Vector2.right * 5f * Time.fixedDeltaTime);
            }
            Assert.Less(body.position.x, start.x + 1.7f, "walking into a rock stops you");

            Assert.IsTrue(player.GetComponent<JumpController>().TryJump());
            for (float t = 0f; t < 1.2f; t += Time.fixedDeltaTime)
            {
                Time.timeScale = 1f;
                yield return new WaitForFixedUpdate();
                body.MovePosition(body.position + Vector2.right * 5f * Time.fixedDeltaTime);
            }
            Assert.Greater(body.position.x, start.x + 2.4f, "jumping carries you over it");
            Assert.IsFalse(player.GetComponent<JumpController>().IsJumping, "and you land again");
        }

        /// <summary>
        /// Mounts: your hero rides the active mount into battle — faster, the mount covering their legs and jumping
        /// with them — and around the village.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AndVillage_YouRideYourMount()
        {
            var steed = NinjaVillage.Systems.Mounts.MountService.Get("horse_black");
            Assert.IsNotNull(steed, "mount catalog missing");
            NinjaVillage.Systems.Heroes.HeroService.EnsureDefaults();
            SaveService.Data.Mounts.Owned.SetLevel(steed.Id, 1);
            SaveService.Data.Mounts.ActiveMountId = steed.Id;

            yield return LoadScene(SceneNames.Battle);
            var player = QuietPlayer();
            var visual = player.GetComponent<NinjaVillage.Gameplay.Mounts.MountVisual>();
            Assert.IsNotNull(visual, "you ride into battle");
            Assert.IsTrue(visual.IsMounted);
            Assert.AreEqual(steed, visual.Mount);
            Assert.Greater(player.GetComponent<PlayerStats>().MoveSpeedMultiplier, 1.1f, "and you're faster");
            Assert.Greater(visual.MountRenderer.sortingOrder, visual.RiderRenderer.sortingOrder);

            var jump = player.GetComponent<JumpController>();
            Assert.IsTrue(jump.TryJump());
            float lift = 0f;
            for (float end = Time.realtimeSinceStartup + 3f; Time.realtimeSinceStartup < end && jump.IsJumping;)
            {
                Time.timeScale = 1f;
                lift = Mathf.Max(lift, visual.Lift);
                yield return null;
            }
            Assert.Greater(lift, 0.2f, "the mount jumps with you");
            yield return null;
            Assert.AreEqual(0f, visual.Lift, 0.001f, "and lands");
            Assert.IsTrue(visual.IsMounted, "still riding");
            Assert.IsTrue(visual.RiderRenderer.enabled && visual.MountRenderer.enabled);

            yield return LoadScene(SceneNames.Village);
            yield return null;
            var riders = Object.FindObjectsByType<NinjaVillage.Gameplay.Mounts.MountVisual>(FindObjectsSortMode.None);
            Assert.AreEqual(1, riders.Length, "your selected hero rides in the village");
            Assert.IsTrue(riders[0].name.StartsWith("Hero_") && riders[0].IsMounted);
            NinjaVillage.Systems.Mounts.MountService.Dismount();
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<NinjaVillage.Gameplay.Mounts.MountVisual>(FindObjectsSortMode.None).Count(r => r.IsMounted),
                "and walks again once you dismount");
            yield return new WaitForSecondsRealtime(0.3f);
        }

        [UnityTest]
        public IEnumerator Battle_EnemiesWalkAroundRocks()
        {
            yield return LoadScene(SceneNames.Battle);
            var player = QuietPlayer();
            Vector2 p = player.transform.position;
            Wall(p + new Vector2(-3f, 0f), new Vector2(0.8f, 3f));
            var bandit = UnityEditor.AssetDatabase.LoadAssetAtPath<NinjaVillage.Gameplay.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/Bandit.asset");
            var spawner = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Waves.SpawnManager>();
            var enemy = spawner.Spawn(bandit, p + new Vector2(-6f, 0f), 100f);

            float closest = float.MaxValue;
            for (float end = Time.realtimeSinceStartup + 10f; Time.realtimeSinceStartup < end && closest > 1.6f;)
            {
                Time.timeScale = 1f;
                if (enemy == null) break;
                closest = Mathf.Min(closest, Vector2.Distance(enemy.transform.position, player.transform.position));
                yield return null;
            }
            Assert.LessOrEqual(closest, 1.6f, "the bandit found its way around the rock");
        }

        [UnityTest]
        public IEnumerator Battle_BossesLeapOverRocks()
        {
            yield return LoadScene(SceneNames.Battle);
            var player = QuietPlayer();
            Vector2 p = player.transform.position;
            Wall(p + new Vector2(3f, 0f), new Vector2(0.8f, 5f));
            var oni = UnityEditor.AssetDatabase.LoadAssetAtPath<NinjaVillage.Gameplay.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/GiantOni.asset");
            var spawner = Object.FindAnyObjectByType<NinjaVillage.Gameplay.Waves.SpawnManager>();
            var boss = (NinjaVillage.Gameplay.Bosses.BossController)spawner.Spawn(oni, p + new Vector2(6.5f, 0f), 50f);

            bool leapt = false;
            for (float end = Time.realtimeSinceStartup + 8f; Time.realtimeSinceStartup < end && !(leapt && !boss.IsLeaping);)
            {
                Time.timeScale = 1f;
                leapt |= boss.IsLeaping;
                yield return null;
            }
            Assert.IsTrue(leapt, "the boss leapt");
            Assert.Less(Vector2.Distance(boss.transform.position, player.transform.position), 3.5f, "and landed next to the player, past the rock");
        }
#endif

        [UnityTest]
        public IEnumerator Battle_GettingHitKeepsTheCameraOnThePlayer()
        {
            yield return LoadScene(SceneNames.Battle);
            var player = PlayerReference.Instance.gameObject;
            player.GetComponent<PlayerController>().enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            var health = player.GetComponent<NinjaVillage.Core.Combat.Health>();
            var cam = Camera.main;

            // Far from where the battle (and the camera) started.
            body.position = new Vector2(25f, -15f);
            player.transform.position = body.position;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { Time.timeScale = 1f; yield return null; }

            Vector2 before = body.position;
            float worst = 0f;
            for (int hit = 0; hit < 6; hit++)
            {
                health.TakeDamage(new NinjaVillage.Core.Combat.DamageInfo(1f, false, Vector2.left, 3f, null));
                for (float t = 0f; t < 0.2f; t += Time.deltaTime)
                {
                    Time.timeScale = 1f;
                    yield return null;
                    worst = Mathf.Max(worst, Vector2.Distance(cam.transform.position, player.transform.position));
                }
            }
            Assert.Less(worst, 1f, "the camera stays on the player while they take hits");
            Assert.Less(Vector2.Distance(before, body.position), 0.5f, "hits barely push the player");
        }

        [UnityTest]
        public IEnumerator Battle_UltimateButtonNeverSaysReadyDuringCooldown()
        {
            yield return LoadScene(SceneNames.Battle);
            var ultimate = PlayerReference.Instance.GetComponent<NinjaVillage.Gameplay.Ultimates.UltimateController>();
            var ui = Object.FindAnyObjectByType<NinjaVillage.UI.Battle.UltimateButtonUI>();
            var button = ui.GetComponent<UnityEngine.UI.Button>();
            var label = ui.GetComponentsInChildren<TMPro.TMP_Text>(true);

            void Charge()
            {
                for (int i = 0; i < 40; i++)
                    EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(new Vector2(500f, 500f), 0, 0));
            }

            Charge();
            yield return null;
            Assert.IsTrue(ultimate.IsReady);
            Assert.IsTrue(button.interactable, "charged and off cooldown: tappable");
            button.onClick.Invoke();
            Assert.AreEqual(0f, ultimate.ChargeNormalized, "it fired");

            Charge(); // charged again straight away, but the cooldown is still running
            Time.timeScale = 1f;
            yield return null;
            Assert.IsFalse(ultimate.IsReady);
            Assert.IsFalse(button.interactable, "a tap wouldn't fire, so the button mustn't accept one");
            Assert.IsFalse(System.Array.Exists(label, t => t.text.Contains("READY")), "and mustn't say READY");
            Assert.IsTrue(System.Array.Exists(label, t => t.text.Contains(":")), "it shows the time left instead");
        }

        [UnityTest]
        public IEnumerator Battle_DeathOffersReviveThenRecordsTheRun()
        {
            yield return LoadScene(SceneNames.Battle);
            bool ended = false;
            System.Action<RunEndedEvent> onEnded = _ => ended = true;
            EventBus<RunEndedEvent>.Subscribe(onEnded);
            try
            {
                var health = PlayerReference.Instance.GetComponent<NinjaVillage.Core.Combat.Health>();
                health.TakeDamage(new NinjaVillage.Core.Combat.DamageInfo(999999f, false, Vector2.zero, 0f, null));
                yield return null;

                var gameManager = Object.FindAnyObjectByType<GameManager>();
                Assert.AreEqual(0f, Time.timeScale, "revive prompt pauses the game");
                Assert.IsFalse(ended, "the run isn't recorded while the revive is being offered");

                gameManager.DeclineRevive();
                yield return null;
                Assert.IsTrue(ended, "declining the revive ends and records the run");
                Assert.AreEqual(1, SaveService.Data.Profile.TotalRuns);
            }
            finally
            {
                EventBus<RunEndedEvent>.Unsubscribe(onEnded);
            }
        }
    }
}
