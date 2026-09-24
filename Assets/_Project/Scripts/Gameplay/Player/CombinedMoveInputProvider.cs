using NinjaVillage.Core.Input;
using UnityEngine;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Merges every movement source (on-screen joystick on mobile, WASD/arrows in the Editor or on
    /// desktop) so one build works everywhere: whichever source is pushed harder this frame wins.
    /// Assign this as the PlayerController's move input; list the sources explicitly, or leave the
    /// list empty to auto-find every other <see cref="IMoveInputProvider"/> in the scene.
    /// </summary>
    public class CombinedMoveInputProvider : MonoBehaviour, IMoveInputProvider
    {
        [SerializeField] private MonoBehaviour[] sources = new MonoBehaviour[0];

        private IMoveInputProvider[] _providers;

        private void Awake()
        {
            if (sources != null && sources.Length > 0)
            {
                _providers = new IMoveInputProvider[sources.Length];
                for (int i = 0; i < sources.Length; i++)
                    _providers[i] = sources[i] as IMoveInputProvider;
                return;
            }

            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var found = new System.Collections.Generic.List<IMoveInputProvider>();
            foreach (var behaviour in all)
                if (behaviour != this && behaviour is IMoveInputProvider provider) found.Add(provider);
            _providers = found.ToArray();
        }

        public Vector2 GetMoveInput()
        {
            Vector2 best = Vector2.zero;
            if (_providers == null) return best;
            foreach (var provider in _providers)
            {
                if (provider == null) continue;
                var input = provider.GetMoveInput();
                if (input.sqrMagnitude > best.sqrMagnitude) best = input;
            }
            return Vector2.ClampMagnitude(best, 1f);
        }
    }
}
