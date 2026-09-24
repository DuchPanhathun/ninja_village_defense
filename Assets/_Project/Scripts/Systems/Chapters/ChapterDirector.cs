using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Chapters;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Gameplay.World;
using UnityEngine;

namespace NinjaVillage.Systems.Chapters
{
    /// <summary>
    /// Battle-scene setup for the chapter being played (<see cref="ChapterService.Selected"/>): hands its
    /// wave plan to the <see cref="WaveManager"/> (finite, ending in the final boss), and dresses the map —
    /// ground texture, sky colour and scattered props. Runs in Awake so everything is in place before the
    /// first wave starts. Without a chapter catalog the scene keeps its own endless waves.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ChapterDirector : MonoBehaviour
    {
        [SerializeField] private WaveManager waveManager;
        [Tooltip("The InfiniteGround's renderer. Found by type when empty.")]
        [SerializeField] private SpriteRenderer ground;
        [SerializeField] private PropScatter props;

        /// <summary>The chapter of the battle in progress (null outside a chapter battle).</summary>
        public static ChapterDefinition Current { get; private set; }

        private static ChapterDirector _owner;

        private void Awake()
        {
            _owner = this;
            Current = ChapterService.Selected;
            if (Current == null) return;

            if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
            if (waveManager != null) waveManager.Configure(Current.Waves, endless: false, Current.Difficulty);

            if (ground == null)
            {
                var infinite = FindAnyObjectByType<InfiniteGround>();
                if (infinite != null) ground = infinite.GetComponent<SpriteRenderer>();
            }
            if (ground != null && Current.Ground != null)
            {
                ground.sprite = Current.Ground;
                ground.color = Current.GroundTint;
            }

            var cam = UnityEngine.Camera.main;
            if (cam != null) cam.backgroundColor = Current.SkyColor;

            if (props == null)
            {
                props = GetComponentInChildren<PropScatter>();
                if (props == null) props = new GameObject("Props").AddComponent<PropScatter>();
            }
            props.Configure(Current.Props, Current.PropDensity, Color.white, Current.Number * 7919);
        }

        private void Start()
        {
            if (Current != null) EventBus<ChapterStartedEvent>.Raise(new ChapterStartedEvent(Current));
        }

        private void OnDestroy()
        {
            // Only the director that set it clears it (a stale one from an unloading scene mustn't).
            if (_owner != this) return;
            _owner = null;
            Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _owner = null;
            Current = null;
        }
    }
}
