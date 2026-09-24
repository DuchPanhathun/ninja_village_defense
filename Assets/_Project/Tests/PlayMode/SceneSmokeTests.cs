using System.Collections;
using System.IO;
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
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
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
                ScreenIds.Store, ScreenIds.Profile, ScreenIds.Settings, "leaderboard", "account");
            yield return new WaitForSecondsRealtime(0.5f);
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
                ScreenIds.Heroes, ScreenIds.Pets, ScreenIds.Inventory, ScreenIds.Talents);

            // Upgrading the Dojo exercises cost, save, event and map refresh paths.
            Assert.IsTrue(VillageService.TryUpgrade(VillageService.Get(BuildingIds.Dojo), out var blocker), $"Dojo upgrade blocked: {blocker}");
            yield return new WaitForSecondsRealtime(0.5f);
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
