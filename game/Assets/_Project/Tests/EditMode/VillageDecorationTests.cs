using System.Collections.Generic;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    public class VillageDecorationTests
    {
        private static readonly Rect Bounds = new(-15f, -11f, 30f, 22f);

        [Test]
        public void Placement_SnapsToTheHalfUnitGrid()
        {
            Assert.AreEqual(new Vector2(1.5f, -2f), DecorationRules.Snap(new Vector2(1.37f, -2.2f)));
        }

        [Test]
        public void Placement_Blockers_InOrder()
        {
            var plot = new[] { new Rect(-2f, -2f, 4f, 4f) };
            var reserved = new List<(Vector2, float)> { (new Vector2(10f, 0f), 1.4f) };
            var others = new List<(Vector2, float)> { (new Vector2(-6f, 5f), 0.6f) };

            Assert.AreEqual(PlacementBlocker.OutsideVillage, DecorationRules.Check(new Vector2(14.8f, 0f), 0.5f, Bounds, plot, reserved, others));
            Assert.AreEqual(PlacementBlocker.OnBuilding, DecorationRules.Check(new Vector2(2.3f, 0f), 0.5f, Bounds, plot, reserved, others));
            Assert.AreEqual(PlacementBlocker.OnDisplay, DecorationRules.Check(new Vector2(9f, 0.5f), 0.5f, Bounds, plot, reserved, others));
            Assert.AreEqual(PlacementBlocker.OnDecoration, DecorationRules.Check(new Vector2(-6.5f, 5f), 0.5f, Bounds, plot, reserved, others));
            Assert.AreEqual(PlacementBlocker.None, DecorationRules.Check(new Vector2(-6f, -6f), 0.5f, Bounds, plot, reserved, others));
        }

        [Test]
        public void Selling_RefundsHalf_RoundedDown()
        {
            Assert.AreEqual(150, DecorationRules.Refund(300));
            Assert.AreEqual(7, DecorationRules.Refund(15));
            Assert.AreEqual(0, DecorationRules.Refund(0));
        }

        [Test]
        public void Snapshot_CopiesWhatTheVillageShows()
        {
            var save = new SaveData();
            save.Migrate();
            save.Profile.DisplayName = "Kage";
            save.Village.Buildings.SetLevel("castle", 4);
            save.Village.Decorations.Add(new PlacedDecoration { Uid = 1, Id = "well", X = 3f, Y = -5f });
            save.Heroes.Owned.SetLevel("samurai", 3);
            save.Heroes.SelectedHeroId = "samurai";
            save.Store.OwnedSkinIds.Add("skin_samurai_gold");
            save.Store.EquippedSkins.Add(new SkinSelection { HeroId = "samurai", SkinId = "skin_samurai_gold" });
            save.Pets.Owned.SetLevel("fox", 2);
            save.Pets.ActivePetId = "fox";
            save.Inventory.EquippedWeaponId = "Katana";
            save.Inventory.EquippedEquipmentIds.Add("iron_ring");
            save.Talents.Nodes.SetLevel("talent_attack", 5);

            var snapshot = VillageSnapshot.FromSave(save);
            Assert.AreEqual("Kage", snapshot.DisplayName);
            Assert.AreEqual(4, snapshot.BuildingLevel("castle"));
            Assert.AreEqual("skin_samurai_gold", snapshot.Heroes.Find(h => h.Id == "samurai").SkinId);
            Assert.AreEqual("fox", snapshot.ActivePetId);
            Assert.AreEqual("Katana", snapshot.WeaponId);
            CollectionAssert.Contains(snapshot.EquipmentIds, "iron_ring");
            Assert.AreEqual(5, snapshot.TalentRanks);

            // A copy, not a view: later edits to the save don't leak into a shared snapshot.
            save.Village.Decorations[0].X = 99f;
            Assert.AreEqual(3f, snapshot.Decorations[0].X);
        }

        [Test]
        public void Snapshot_SurvivesJson_ForSharingOnline()
        {
            var save = new SaveData();
            save.Migrate();
            save.Village.Decorations.Add(new PlacedDecoration { Uid = 7, Id = "sakura_tree", X = -4.5f, Y = 2f, Flip = true });
            save.Heroes.Owned.SetLevel("monk", 2);

            var json = JsonUtility.ToJson(VillageSnapshot.FromSave(save));
            var back = JsonUtility.FromJson<VillageSnapshot>(json);
            Assert.AreEqual(VillageSnapshot.CurrentVersion, back.Version);
            Assert.AreEqual("sakura_tree", back.Decorations[0].Id);
            Assert.IsTrue(back.Decorations[0].Flip);
            Assert.AreEqual("monk", back.Heroes[0].Id);
        }

        [Test]
        public void OldSaves_GetADecorationList()
        {
            var save = new SaveData();
            save.Village.Decorations = null;
            save.Migrate();
            Assert.IsNotNull(save.Village.Decorations);
        }
    }
}
