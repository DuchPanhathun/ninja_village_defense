using System.Collections.Generic;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.World;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// Round minimap in the HUD's top-right corner: the player in the middle, enemies as red dots, elites
    /// orange, and the things worth walking to — chests, equipment drops and bosses — drawn with their own
    /// pictures and pinned to the rim, pointing the way, when they're out of range. Tap to toggle a bigger,
    /// wider-range view. Reads <see cref="MinimapMarker.Active"/>; built in code on a RectTransform.
    /// </summary>
    public class MinimapUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float size = 200f;
        [SerializeField] private float worldRadius = 14f;
        [SerializeField] private float expandedSize = 480f;
        [SerializeField] private float expandedWorldRadius = 32f;
        [SerializeField] private int maxEnemyDots = 80;

        private const float Rim = 10f;

        private readonly List<(Image image, MinimapMarkerKind kind)> _pool = new();
        private RectTransform _root, _map;
        private Sprite _chestSprite;
        private bool _expanded;
        private int _used;

        private void Start()
        {
            _root = (RectTransform)transform;
            var art = Core.Data.CatalogLoader.Load<PickupArt>();
            _chestSprite = art != null ? art.ChestClosed : null;

            var frame = UIBuilder.Image(_root, "Frame", new Color(0.2f, 0.13f, 0.08f, 0.95f), GeneratedSprites.Circle);
            UIBuilder.Stretch(frame.rectTransform); // also the tap target
            var ring = UIBuilder.Image(_root, "Ring", UITheme.Gold, GeneratedSprites.Circle);
            ring.raycastTarget = false;
            UIBuilder.Stretch(ring.rectTransform, 5f);

            var chapter = ChapterDirector.Current;
            Color ground = chapter != null ? chapter.SkyColor : new Color(0.29f, 0.45f, 0.2f);
            var map = UIBuilder.Image(_root, "Map", Color.Lerp(ground, Color.black, 0.35f), GeneratedSprites.Circle);
            map.raycastTarget = false;
            UIBuilder.Stretch(map.rectTransform, Rim);
            map.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _map = map.rectTransform;

            var player = UIBuilder.Image(_root, "Player", Color.white, GeneratedSprites.Circle);
            player.raycastTarget = false;
            UIStyle.Place(player.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            var playerCore = UIBuilder.Image(player.transform, "Core", new Color(0.35f, 0.75f, 1f), GeneratedSprites.Circle);
            playerCore.raycastTarget = false;
            UIBuilder.Stretch(playerCore.rectTransform, 3f);

            ApplySize();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _expanded = !_expanded;
            ApplySize();
        }

        private void ApplySize()
        {
            float s = _expanded ? expandedSize : size;
            _root.sizeDelta = new Vector2(s, s);
        }

        private void LateUpdate()
        {
            if (_map == null) return;
            _used = 0;
            var player = PlayerReference.Instance;
            if (player != null)
            {
                Vector2 center = player.PlayerTransform.position;
                float radius = _expanded ? expandedWorldRadius : worldRadius;
                float mapRadius = _root.sizeDelta.x * 0.5f - Rim;
                int enemyDots = 0;
                var markers = MinimapMarker.Active;

                // Two passes: dots first, then the important pictures on top of them.
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (var marker in markers)
                    {
                        if (marker == null) continue;
                        bool important = marker.PinToEdge;
                        if (important != (pass == 1)) continue;

                        Vector2 offset = ((Vector2)marker.transform.position - center) / radius;
                        float distance = offset.magnitude;
                        if (distance > 1f)
                        {
                            if (!important) continue;
                            offset /= distance; // pinned to the rim, pointing the way
                        }
                        if (!important && ++enemyDots > maxEnemyDots) continue;
                        Show(marker.Kind, offset * (mapRadius - MarkerSize(marker.Kind) * 0.5f), distance > 1f);
                    }
                }
            }
            for (int i = _used; i < _pool.Count; i++)
                if (_pool[i].image.gameObject.activeSelf) _pool[i].image.gameObject.SetActive(false);
        }

        private static float MarkerSize(MinimapMarkerKind kind) => kind switch
        {
            MinimapMarkerKind.Enemy => 9f,
            MinimapMarkerKind.Elite => 15f,
            MinimapMarkerKind.Boss => 40f,
            MinimapMarkerKind.Chest => 34f,
            _ => 26f,
        };

        private void Show(MinimapMarkerKind kind, Vector2 position, bool pinned)
        {
            if (_used == _pool.Count)
            {
                var created = UIBuilder.Image(_map, "Marker", Color.white);
                created.raycastTarget = false;
                created.preserveAspect = true;
                created.rectTransform.anchorMin = created.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _pool.Add((created, (MinimapMarkerKind)(-1)));
            }
            var (image, shownKind) = _pool[_used];
            if (shownKind != kind)
            {
                StyleMarker(image, kind);
                _pool[_used] = (image, kind);
            }
            _used++;
            if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);

            var rt = image.rectTransform;
            rt.anchoredPosition = position;
            // Things to walk to pulse, faster when they're off the map.
            float pulse = kind is MinimapMarkerKind.Chest or MinimapMarkerKind.Item or MinimapMarkerKind.Boss
                ? 1f + 0.15f * Mathf.Sin(Time.unscaledTime * (pinned ? 9f : 5f)) : 1f;
            rt.localScale = Vector3.one * pulse;
        }

        private void StyleMarker(Image image, MinimapMarkerKind kind)
        {
            float s = MarkerSize(kind);
            image.rectTransform.sizeDelta = new Vector2(s, s);
            image.rectTransform.localRotation = Quaternion.identity;
            switch (kind)
            {
                case MinimapMarkerKind.Enemy:
                    image.sprite = GeneratedSprites.Circle;
                    image.color = new Color(1f, 0.3f, 0.25f);
                    break;
                case MinimapMarkerKind.Elite:
                    image.sprite = GeneratedSprites.Circle;
                    image.color = new Color(1f, 0.62f, 0.15f);
                    break;
                case MinimapMarkerKind.Boss:
                    image.sprite = GeneratedSprites.Circle; // a big red disc; the boss picture would be unreadable this small
                    image.color = new Color(0.9f, 0.1f, 0.12f);
                    break;
                case MinimapMarkerKind.Chest:
                    image.sprite = _chestSprite != null ? _chestSprite : GeneratedSprites.Square;
                    image.color = _chestSprite != null ? Color.white : UITheme.Gold;
                    break;
                default:
                    image.sprite = GeneratedSprites.Square;
                    image.color = new Color(0.75f, 0.45f, 1f);
                    image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
            }
        }
    }
}
