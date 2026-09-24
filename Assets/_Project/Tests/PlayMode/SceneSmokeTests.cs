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
                ScreenIds.Store, ScreenIds.Profile, ScreenIds.Settings, ScreenIds.Chapters, "leaderboard");
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
                ScreenIds.Heroes, ScreenIds.Pets, ScreenIds.Inventory, ScreenIds.Talents, ScreenIds.Decorations,
                ScreenIds.Neighbours, ScreenIds.Profile);

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
