using UnityEngine;

namespace NinjaVillage.Core.Input
{
    /// <summary>
    /// Decouples the player's movement source from PlayerController. The on-screen
    /// mobile joystick and the Editor keyboard fallback both implement this so
    /// PlayerController never needs to know which one is active.
    /// </summary>
    public interface IMoveInputProvider
    {
        /// <summary>Normalized-ish direction in [-1,1] on each axis. Zero when idle.</summary>
        Vector2 GetMoveInput();
    }
}
