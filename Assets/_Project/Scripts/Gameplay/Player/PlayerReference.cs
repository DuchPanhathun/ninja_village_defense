using UnityEngine;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Lightweight scene-local lookup so enemies/pets/camera can find "the player"
    /// without a full DI container — there is only ever one local player.
    /// </summary>
    public class PlayerReference : MonoBehaviour
    {
        public static PlayerReference Instance { get; private set; }

        public Transform PlayerTransform => transform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
