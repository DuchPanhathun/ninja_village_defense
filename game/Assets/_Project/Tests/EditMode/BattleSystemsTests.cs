using System.Collections.Generic;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Evolution;
using NinjaVillage.Systems.Performance;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    public class BattleSystemsTests
    {
        [Test]
        public void EvolutionRecipe_NeedsAllSkillsAndTheWeapon()
        {
            var levels = new Dictionary<string, int> { { "fire", 2 }, { "wind", 1 } };
            int Level(string id) => levels.TryGetValue(id, out var l) ? l : 0;

            Assert.IsTrue(EvolutionRules.IsSatisfied(new[] { "fire", "wind" }, Level, null, "Kunai"));
            Assert.IsFalse(EvolutionRules.IsSatisfied(new[] { "fire", "lightning" }, Level, null, "Kunai"));
            Assert.IsTrue(EvolutionRules.IsSatisfied(new[] { "fire" }, Level, "Kunai", "Kunai"));
            Assert.IsFalse(EvolutionRules.IsSatisfied(new[] { "fire" }, Level, "Katana", "Kunai"), "wrong weapon equipped");
            Assert.IsFalse(EvolutionRules.IsSatisfied(new[] { "fire" }, Level, "Katana", null), "no weapon equipped");
            Assert.IsTrue(EvolutionRules.IsSatisfied(new string[] { null, "fire" }, Level, "", "anything"), "null ingredients are ignored");
        }

        [Test]
        public void PrefabPool_ReusesReleasedInstances()
        {
            PrefabPool.Clear();
            var prefab = new GameObject("PoolTestPrefab");
            try
            {
                var first = PrefabPool.Get(prefab, Vector3.zero, Quaternion.identity);
                Assert.AreNotSame(prefab, first);

                PrefabPool.Release(first);
                Assert.IsFalse(first.activeSelf, "released instances are deactivated");
                Assert.AreEqual(1, PrefabPool.PooledCount(prefab));

                var second = PrefabPool.Get(prefab, new Vector3(1f, 2f, 0f), Quaternion.identity);
                Assert.AreSame(first, second, "the released instance is reused");
                Assert.IsTrue(second.activeSelf);
                Assert.AreEqual(new Vector3(1f, 2f, 0f), second.transform.position);

                PrefabPool.Release(second);
                PrefabPool.Release(second);
                Assert.AreEqual(1, PrefabPool.PooledCount(prefab), "double release is ignored");
                Object.DestroyImmediate(second);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                PrefabPool.Clear();
            }
        }

        [Test]
        public void SleepPolicy_AwakeOnlyInBattle()
        {
            Assert.AreEqual(SleepTimeout.NeverSleep, PerformanceBootstrap.SleepTimeoutFor("Battle"));
            Assert.AreEqual(SleepTimeout.SystemSetting, PerformanceBootstrap.SleepTimeoutFor("Village"));
            Assert.AreEqual(SleepTimeout.SystemSetting, PerformanceBootstrap.SleepTimeoutFor("MainMenu"));
        }
    }
}
