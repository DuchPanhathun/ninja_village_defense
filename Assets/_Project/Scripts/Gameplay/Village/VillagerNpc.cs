using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// A villager wandering between random spots in the village (EPIC 11 "NPC system"). Tapping one
    /// shows a speech bubble with a gameplay tip, which doubles as a light tutorial. The number of
    /// villagers grows with village progress (<see cref="VillageMap"/>).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class VillagerNpc : MonoBehaviour, IVillageTappable
    {
        private static readonly string[] Tips =
        {
            "The Dojo trains your heroes — every level adds attack power!",
            "Blessings from the Shrine stay with you in every battle.",
            "The Market has new wares every day. Don't miss a bargain!",
            "Upgrade the Castle to let every other building grow taller.",
            "The Forge can reforge weapons into Steel, Gold... even Legendary!",
            "Spare equipment makes great material for the Forge.",
            "The Pet House gives companions room to grow stronger.",
            "They say certain skills combine into forbidden techniques...",
            "Fire and wind together? I've heard tales of a firestorm.",
            "Dash through enemies — you can't be hurt mid-dash!",
            "Bosses drop gems. Gems buy rare heroes at the Castle gate.",
            "Talents are permanent, but a reset refunds every coin.",
            "Our village was just a hut once. Look at it now!",
        };

        [SerializeField] private float moveSpeed = 1.1f;
        [SerializeField] private float bubbleSeconds = 3.5f;

        private Rect _area;
        private Vector2 _target;
        private float _idleUntil;
        private float _bobPhase;
        private Transform _visual;
        private GameObject _bubble;
        private TextMeshPro _bubbleText;
        private float _bubbleHideAt;

        public static VillagerNpc Spawn(Transform parent, Vector2 position, Rect area, Color clothes, int seed)
        {
            var go = new GameObject($"Villager_{seed}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var npc = go.AddComponent<VillagerNpc>();
            npc._area = area;
            npc._bobPhase = seed * 1.37f;
            npc.Build(clothes);
            npc.PickTarget();
            return npc;
        }

        private void Build(Color clothes)
        {
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            GeneratedSprites.CreateRenderer(_visual, "Body", GeneratedSprites.Circle, clothes, 60, new Vector2(0f, 0f), new Vector2(0.55f, 0.7f));
            GeneratedSprites.CreateRenderer(_visual, "Head", GeneratedSprites.Circle, new Color(1f, 0.85f, 0.7f, 1f), 61, new Vector2(0f, 0.45f), new Vector2(0.38f, 0.38f));

            var col = GetComponent<CircleCollider2D>();
            col.radius = 0.45f;
            col.offset = new Vector2(0f, 0.2f);

            _bubble = new GameObject("Bubble");
            _bubble.transform.SetParent(transform, false);
            _bubble.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            GeneratedSprites.CreateRenderer(_bubble.transform, "Background", GeneratedSprites.Square, new Color(1f, 1f, 1f, 0.92f), 300,
                Vector2.zero, new Vector2(5.2f, 1.5f));
            _bubbleText = new GameObject("Text").AddComponent<TextMeshPro>();
            _bubbleText.transform.SetParent(_bubble.transform, false);
            _bubbleText.rectTransform.sizeDelta = new Vector2(5f, 1.4f);
            _bubbleText.fontSize = 2.2f;
            _bubbleText.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            _bubbleText.alignment = TextAlignmentOptions.Center;
            _bubbleText.textWrappingMode = TextWrappingModes.Normal;
            _bubbleText.sortingOrder = 301;
            _bubble.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 pos = transform.position;

            if (Time.time >= _idleUntil)
            {
                Vector2 toTarget = _target - pos;
                if (toTarget.sqrMagnitude < 0.02f)
                {
                    _idleUntil = Time.time + Random.Range(1.5f, 4f);
                    PickTarget();
                }
                else
                {
                    transform.position = pos + toTarget.normalized * (moveSpeed * dt);
                    var s = _visual.localScale;
                    s.x = Mathf.Sign(toTarget.x) * Mathf.Abs(s.x == 0f ? 1f : s.x);
                    _visual.localScale = s;
                }
            }

            // Walk bob (a little hop while moving, gentle breathing while idle).
            bool moving = Time.time >= _idleUntil;
            _bobPhase += dt * (moving ? 10f : 2f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.08f : Mathf.Sin(_bobPhase) * 0.02f;
            _visual.localPosition = new Vector3(0f, bob, 0f);

            if (_bubble.activeSelf && Time.time >= _bubbleHideAt) _bubble.SetActive(false);
        }

        private void PickTarget()
        {
            _target = new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));
        }

        public void OnTapped()
        {
            Sfx.Play(AudioCueIds.UiClick);
            _bubbleText.text = Tips[Random.Range(0, Tips.Length)];
            _bubble.SetActive(true);
            _bubbleHideAt = Time.time + bubbleSeconds;
            _idleUntil = Time.time + bubbleSeconds; // stop to talk
        }
    }
}
