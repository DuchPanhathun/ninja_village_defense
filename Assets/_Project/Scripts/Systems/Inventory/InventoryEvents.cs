using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Inventory
{
    public enum InventoryChangeKind
    {
        WeaponAdded,
        WeaponUpgraded,
        WeaponTierChanged,
        WeaponEquipped,
        EquipmentAdded,
        EquipmentRemoved,
        EquipmentEquipped,
        EquipmentUnequipped
    }

    /// <summary>Raised after any persistent inventory change so open screens (Inventory, Forge, Pets) can refresh.</summary>
    public readonly struct InventoryChangedEvent : IGameEvent
    {
        public readonly InventoryChangeKind Kind;
        public readonly string ItemId;

        public InventoryChangedEvent(InventoryChangeKind kind, string itemId)
        {
            Kind = kind;
            ItemId = itemId;
        }
    }
}
