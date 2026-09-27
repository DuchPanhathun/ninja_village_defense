using NinjaVillage.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Editor/desktop testing fallback — WASD / arrow keys via the new Input System.
    /// Needs no .inputactions asset, so it works the moment the Input System package
    /// is active. Ship builds use <see cref="NinjaVillage.UI.VirtualJoystick"/> instead.
    /// </summary>
    public class KeyboardMoveInputProvider : MonoBehaviour, IMoveInputProvider
    {
        public Vector2 GetMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;

            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
