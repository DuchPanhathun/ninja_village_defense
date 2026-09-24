using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Pets
{
    public enum PetCareResult
    {
        Success,
        NotOwned,
        AlreadyToday,
        NoTreat,
    }

    /// <summary>Raised when a pet is petted or fed.</summary>
    public readonly struct PetCaredEvent : IGameEvent
    {
        public readonly string PetId;
        public readonly bool Fed;
        public PetCaredEvent(string petId, bool fed)
        {
            PetId = petId;
            Fed = fed;
        }
    }

    /// <summary>
    /// Pet care (EPIC 24 Phase 7): pet and feed each of your pets once a day in the village; a pet cared for today
    /// fights harder in today's battles (<see cref="PetRunModifier"/> scales its power by <see cref="PowerScale"/>).
    /// Feeding uses a treat from the storehouse. State in <c>SaveService.Data.PetCare</c>; rules in <see cref="PetCareRules"/>.
    /// </summary>
    public static class PetCareService
    {
        private static PetCareSaveData Data => SaveService.Data.PetCare;

        public static bool CanPet(string petId) => PetCareRules.CanPet(Data.PettedDay.GetLevel(petId), GameClock.Today);
        public static bool CanFeed(string petId) => PetCareRules.CanFeed(Data.FedDay.GetLevel(petId), GameClock.Today);
        public static bool IsHappy(string petId) => !CanPet(petId) || !CanFeed(petId);

        public static float PowerScale(SaveData save, string petId) =>
            save?.PetCare == null ? 1f : PetCareRules.PowerScale(save.PetCare.PettedDay.GetLevel(petId), save.PetCare.FedDay.GetLevel(petId), GameClock.Today);

        /// <summary>The treat feeding would use now (null when the storehouse has none).</summary>
        public static string NextTreat => PetCareRules.PickTreat(GoodsService.Count);

        public static PetCareResult Pet(string petId)
        {
            if (!SaveService.Data.Pets.IsUnlocked(petId)) return PetCareResult.NotOwned;
            if (!CanPet(petId)) return PetCareResult.AlreadyToday;
            Data.PettedDay.SetLevel(petId, GameClock.Today);
            Changed(petId, fed: false);
            return PetCareResult.Success;
        }

        public static PetCareResult Feed(string petId, out string treat)
        {
            treat = null;
            if (!SaveService.Data.Pets.IsUnlocked(petId)) return PetCareResult.NotOwned;
            if (!CanFeed(petId)) return PetCareResult.AlreadyToday;
            treat = NextTreat;
            if (treat == null || !GoodsService.TrySpend(treat, 1)) return PetCareResult.NoTreat;
            Data.FedDay.SetLevel(petId, GameClock.Today);
            Changed(petId, fed: true);
            return PetCareResult.Success;
        }

        private static void Changed(string petId, bool fed)
        {
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.PetCared, 1, petId);
            EventBus<PetCaredEvent>.Raise(new PetCaredEvent(petId, fed));
        }
    }
}
