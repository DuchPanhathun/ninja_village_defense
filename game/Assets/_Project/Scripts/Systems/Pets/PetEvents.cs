using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>Why a pet action failed (or didn't) — the UI turns this into a hint/toast.</summary>
    public enum PetActionResult
    {
        Success,
        InvalidPet,
        InvalidItem,
        AlreadyUnlocked,
        AlreadyOwned,
        NotUnlocked,
        ItemNotOwned,
        NotEnoughCurrency,
        PetHouseLevelTooLow,
        PetSlotsFull,
        LevelCapReached,
        MaxLevel
    }

    /// <summary>Raised when a pet is unlocked (bought or granted).</summary>
    public readonly struct PetUnlockedEvent : IGameEvent
    {
        public readonly string PetId;
        public PetUnlockedEvent(string petId) => PetId = petId;
    }

    /// <summary>Raised after a pet level-up (EPIC 12 "Pet leveling").</summary>
    public readonly struct PetLevelChangedEvent : IGameEvent
    {
        public readonly string PetId;
        public readonly int NewLevel;

        public PetLevelChangedEvent(string petId, int newLevel)
        {
            PetId = petId;
            NewLevel = newLevel;
        }
    }

    /// <summary>Raised when the companion taken into battle changes. Null id = no pet.</summary>
    public readonly struct ActivePetChangedEvent : IGameEvent
    {
        public readonly string PetId;
        public ActivePetChangedEvent(string petId) => PetId = petId;
    }

    /// <summary>Raised when gear is bought, equipped or removed. ItemId null = slot emptied.</summary>
    public readonly struct PetEquipmentChangedEvent : IGameEvent
    {
        public readonly string PetId;
        public readonly string ItemId;

        public PetEquipmentChangedEvent(string petId, string itemId)
        {
            PetId = petId;
            ItemId = itemId;
        }
    }
}
