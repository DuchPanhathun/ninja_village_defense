using System.Collections;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// One building on the village map, drawn with its pack sprite standing on its plot (the Castle
    /// changes look as it levels: hut → house → manor → castle). Unbuilt plots show a dark silhouette with
    /// "Tap to build", so the village itself is the visible progress meter. Levels come from the map's
    /// <see cref="VillageSnapshot"/> (<see cref="VillageMap"/> calls <see cref="Refresh"/>). Tapping raises
    /// <see cref="VillageBuildingTappedEvent"/> (not while visiting someone else's village).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BuildingView : MonoBehaviour, IVillageTappable
    {
        public string BuildingId { get; private set; }

        private BuildingDefinition _definition;
        private SpriteRenderer _body, _shadow, _sign, _crane;
        private SpriteRenderer[] _flags = System.Array.Empty<SpriteRenderer>();
        private TextMeshPro _label;
        private Transform _visual;
        private BoxCollider2D _collider;
        private Coroutine _pop;
        private int _shownLevel = -1;

        public void Initialize(BuildingDefinition definition)
        {
            _definition = definition;
            BuildingId = definition.Id;
            name = $"Building_{definition.Id}";
            transform.position = definition.PlotPosition;

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _shadow = GeneratedSprites.CreateRenderer(transform, "Shadow", GeneratedSprites.Circle, new Color(0f, 0f, 0f, 0.22f), 0);
            _body = new GameObject("Body").AddComponent<SpriteRenderer>();
            _body.transform.SetParent(_visual, false);

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 3f;
            _label.fontStyle = FontStyles.Bold;
            _label.color = Color.white;
            _label.outlineWidth = 0.22f;
            _label.outlineColor = new Color32(20, 27, 27, 255);
            _label.rectTransform.sizeDelta = new Vector2(8f, 2f);
            _label.sortingOrder = VillageSorting.Labels;

            _collider = GetComponent<BoxCollider2D>();
        }

        /// <summary>Redraws for <paramref name="level"/> (0 = not built). <paramref name="castleLevel"/> gates unbuilt plots.</summary>
        public void Refresh(int level, int castleLevel)
        {
            if (_definition == null) return;
            bool built = level > 0;
            var art = VillageArt.Load();
            var sprite = art != null ? art.BuildingSprite(_definition.Id, Mathf.Max(1, level)) : null;
            if (sprite == null) sprite = GeneratedSprites.Square;

            _body.sprite = sprite;
            Vector2 size = sprite == GeneratedSprites.Square ? _definition.Footprint : (Vector2)sprite.bounds.size;
            if (sprite == GeneratedSprites.Square) _body.transform.localScale = new Vector3(size.x, size.y, 1f);

            // The sprite's base sits on the plot's bottom edge.
            float baseY = -_definition.Footprint.y * 0.5f;
            _body.transform.localPosition = new Vector3(0f, baseY + size.y * 0.5f, 0f);
            int order = VillageSorting.Order(transform.position.y + baseY);
            _body.sortingOrder = order;
            _body.color = built ? (sprite == GeneratedSprites.Square ? _definition.Color : Color.white) : new Color(0.08f, 0.1f, 0.12f, 0.45f);

            _shadow.transform.localPosition = new Vector3(0f, baseY + 0.1f, 0f);
            _shadow.transform.localScale = new Vector3(size.x * 0.9f, 0.9f, 1f);
            _shadow.sortingOrder = VillageSorting.Paths + 1;
            _shadow.enabled = built;

            _collider.size = size;
            _collider.offset = new Vector2(0f, baseY + size.y * 0.5f);

            RefreshExtras(art, built, level, size, baseY, order);

            _label.transform.localPosition = new Vector3(0f, baseY + size.y + 0.45f, 0f);
            if (built)
            {
                _label.text = $"{_definition.StageName(level)}\n<size=70%><color=#FFD24D>Lv {level}</color></size>";
            }
            else
            {
                bool locked = castleLevel < _definition.RequiredCastleLevel;
                _label.text = locked
                    ? $"{_definition.NameOrId}\n<size=70%><color=#F2A0A0>Castle Lv {_definition.RequiredCastleLevel}</color></size>"
                    : $"{_definition.NameOrId}\n<size=70%><color=#9CFF8A>Tap to build</color></size>";
            }

            if (_shownLevel >= 0 && level > _shownLevel)
            {
                if (_pop != null) StopCoroutine(_pop);
                _pop = StartCoroutine(PopRoutine());
            }
            _shownLevel = level;
        }

        /// <summary>The Dojo's sign, flags beside the Castle once it's grown, and the Mine's crane.</summary>
        private void RefreshExtras(VillageArt art, bool built, int level, Vector2 size, float baseY, int order)
        {
            if (art == null) return;
            if (_definition.Id == BuildingIds.Dojo && art.DojoSign != null)
            {
                if (_sign == null)
                {
                    _sign = new GameObject("Sign").AddComponent<SpriteRenderer>();
                    _sign.transform.SetParent(_visual, false);
                    _sign.sprite = art.DojoSign;
                }
                _sign.transform.localPosition = new Vector3(0f, baseY + size.y * 0.42f, 0f);
                _sign.sortingOrder = order + 1;
                _sign.enabled = built;
            }

            if (_definition.IsCastle && art.FlagFrames.Length > 0)
            {
                if (_flags.Length == 0)
                {
                    _flags = new SpriteRenderer[2];
                    for (int i = 0; i < 2; i++)
                    {
                        _flags[i] = new GameObject($"Flag{i}").AddComponent<SpriteRenderer>();
                        _flags[i].transform.SetParent(_visual, false);
                        _flags[i].gameObject.AddComponent<SpriteLoop>().SetFrames(art.FlagFrames, 6f);
                        _flags[i].flipX = i == 0;
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    float x = (i == 0 ? -1f : 1f) * (size.x * 0.5f + 0.5f);
                    _flags[i].transform.localPosition = new Vector3(x, baseY + 0.7f, 0f);
                    _flags[i].sortingOrder = order;
                    _flags[i].enabled = built && level >= 3;
                }
            }

            if (_definition.Id == BuildingIds.Mine && art.MineCrane != null)
            {
                if (_crane == null)
                {
                    _crane = new GameObject("Crane").AddComponent<SpriteRenderer>();
                    _crane.transform.SetParent(_visual, false);
                    _crane.sprite = art.MineCrane;
                }
                // The hoist stands on the right of the rock face, lowering its hook into the pit.
                _crane.transform.localPosition = new Vector3(size.x * 0.5f + 0.4f, baseY + art.MineCrane.bounds.extents.y, 0f);
                _crane.sortingOrder = order;
                _crane.enabled = built;
            }
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<VillageBuildingTappedEvent>.Raise(new VillageBuildingTappedEvent(BuildingId));
        }

        private IEnumerator PopRoutine()
        {
            Vector3 baseScale = Vector3.one;
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f + Mathf.Sin(t / duration * Mathf.PI) * 0.12f;
                _visual.localScale = baseScale * k;
                yield return null;
            }
            _visual.localScale = baseScale;
            _pop = null;
        }
    }
}
