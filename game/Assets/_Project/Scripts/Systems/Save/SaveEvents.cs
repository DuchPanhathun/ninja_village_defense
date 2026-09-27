using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Raised after the save is (re)loaded — e.g. first access, or a cloud copy replacing the local one.</summary>
    public readonly struct SaveLoadedEvent : IGameEvent
    {
        public readonly SaveData Data;
        public SaveLoadedEvent(SaveData data) => Data = data;
    }

    /// <summary>Raised after the save is written to disk. The cloud-save sync listens for this.</summary>
    public readonly struct SaveWrittenEvent : IGameEvent
    {
        public readonly SaveData Data;
        public SaveWrittenEvent(SaveData data) => Data = data;
    }
}
