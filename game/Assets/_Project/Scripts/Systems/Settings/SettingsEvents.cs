using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Settings
{
    /// <summary>
    /// Raised whenever any player setting changes (or the whole settings block is re-applied
    /// after a load/reset). The AudioManager re-reads its volumes from this; UI can refresh
    /// toggles. <see cref="Settings"/> is the live save section, already sanitized.
    /// </summary>
    public readonly struct SettingsChangedEvent : IGameEvent
    {
        public readonly SettingsSaveData Settings;
        public SettingsChangedEvent(SettingsSaveData settings) => Settings = settings;
    }
}
