namespace NinjaVillage.Core.Events
{
    /// <summary>
    /// Marker interface for every event that travels through the <see cref="EventBus"/>.
    /// Implement it on a lightweight <c>readonly struct</c> so events stay allocation-free.
    /// </summary>
    public interface IGameEvent { }
}
