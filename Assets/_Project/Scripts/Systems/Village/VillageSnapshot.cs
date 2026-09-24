using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>A hero standing in someone's village: which one, its level and the skin it wears.</summary>
    [Serializable]
    public class VillageHero
    {
        public string Id;
        public int Level;
        public string SkinId;
    }

    /// <summary>
    /// Everything the village map shows about one player, in one JsonUtility-friendly object: buildings,
    /// decorations, the heroes and pets living there, the gear on the Armory rack, talent progress and
    /// profile highlights. The map is drawn from a snapshot rather than straight from the save, so another
    /// player's village can be drawn the same way once snapshots are shared online (upload
    /// <see cref="FromSave"/>'s JSON, download someone else's and hand it to <see cref="VillageVisit"/>).
    /// </summary>
    [Serializable]
    public class VillageSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string PlayerId;
        public string DisplayName;
        public long TakenUtcTicks;

        public List<IdLevelEntry> Buildings = new();
        public List<PlacedDecoration> Decorations = new();

        public List<VillageHero> Heroes = new();
        public string SelectedHeroId;
        public List<IdLevelEntry> Pets = new();
        public string ActivePetId;

        public string WeaponId;
        public List<string> EquipmentIds = new();
        public int TalentRanks;

        // Profile highlights (for the profile board / visitors).
        public int HighestWave;
        public int TotalKills;
        public int ChaptersCleared;
        public int AchievementTiers;

        public int BuildingLevel(string buildingId) => Buildings.GetLevel(buildingId);

        public int TotalBuildingLevels
        {
            get
            {
                int total = 0;
                foreach (var entry in Buildings)
                    if (entry != null && entry.Level > 0) total += entry.Level;
                return total;
            }
        }

        public static VillageSnapshot FromSave(SaveData save)
        {
            var snapshot = new VillageSnapshot { TakenUtcTicks = DateTime.UtcNow.Ticks };
            if (save == null) return snapshot;

            snapshot.PlayerId = save.Profile?.PlayerId;
            snapshot.DisplayName = save.Profile?.DisplayName;
            snapshot.HighestWave = save.Profile?.HighestWaveReached ?? 0;
            snapshot.TotalKills = save.Profile?.TotalKills ?? 0;
            snapshot.ChaptersCleared = save.Chapters?.HighestCleared ?? 0;

            if (save.Village != null)
            {
                foreach (var b in save.Village.Buildings)
                    if (b != null) snapshot.Buildings.Add(new IdLevelEntry(b.Id, b.Level));
                foreach (var d in save.Village.Decorations)
                    if (d != null) snapshot.Decorations.Add(new PlacedDecoration { Uid = d.Uid, Id = d.Id, X = d.X, Y = d.Y, Flip = d.Flip });
            }

            if (save.Heroes != null)
            {
                snapshot.SelectedHeroId = save.Heroes.SelectedHeroId;
                foreach (var hero in save.Heroes.Owned)
                    if (hero != null && !string.IsNullOrEmpty(hero.Id))
                        snapshot.Heroes.Add(new VillageHero { Id = hero.Id, Level = Math.Max(1, hero.Level), SkinId = EquippedSkin(save, hero.Id) });
            }

            if (save.Pets != null)
            {
                snapshot.ActivePetId = save.Pets.ActivePetId;
                foreach (var pet in save.Pets.Owned)
                    if (pet != null && !string.IsNullOrEmpty(pet.Id)) snapshot.Pets.Add(new IdLevelEntry(pet.Id, Math.Max(1, pet.Level)));
            }

            if (save.Inventory != null)
            {
                snapshot.WeaponId = save.Inventory.EquippedWeaponId;
                if (save.Inventory.EquippedEquipmentIds != null) snapshot.EquipmentIds.AddRange(save.Inventory.EquippedEquipmentIds);
            }

            snapshot.TalentRanks = save.Talents?.TotalRanks ?? 0;
            if (save.Daily?.Achievements != null)
                foreach (var achievement in save.Daily.Achievements)
                    if (achievement != null) snapshot.AchievementTiers += achievement.ClaimedTiers;
            return snapshot;
        }

        private static string EquippedSkin(SaveData save, string heroId)
        {
            if (save.Store?.EquippedSkins == null) return null;
            foreach (var selection in save.Store.EquippedSkins)
                if (selection != null && selection.HeroId == heroId && save.Store.OwnedSkinIds.Contains(selection.SkinId))
                    return selection.SkinId;
            return null;
        }
    }

    /// <summary>
    /// Whose village the Village scene shows. Null <see cref="Target"/> = your own (editable); a snapshot =
    /// visiting someone (read-only). Online visiting will fetch a snapshot and call <see cref="Visit"/>.
    /// </summary>
    public static class VillageVisit
    {
        public static VillageSnapshot Target { get; private set; }
        public static bool IsVisiting => Target != null;

        public static void Visit(VillageSnapshot snapshot) => Target = snapshot;
        public static void ReturnHome() => Target = null;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Target = null;

        // A visit ends as soon as you go anywhere but the Village (menu, battle), however you leave.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            NinjaVillage.Core.Events.EventBus<GameFlow.SceneChangingEvent>.UnsubscribePersistent(OnSceneChanging);
            NinjaVillage.Core.Events.EventBus<GameFlow.SceneChangingEvent>.SubscribePersistent(OnSceneChanging);
        }

        private static void OnSceneChanging(GameFlow.SceneChangingEvent evt)
        {
            if (evt.ToScene != GameFlow.SceneNames.Village) Target = null;
        }
    }
}
