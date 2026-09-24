using System.Collections;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// One building on the village map, drawn from placeholder shapes in its definition's colors.
    /// Unbuilt plots show a faint foundation with "Tap to build"; built ones grow with their level and
    /// show their stage name ("Hut" → "Castle"), so the village itself is the visible progress meter
    /// (goal.text "Village becomes a visual indicator of progress"). Tapping raises
    /// <see cref="VillageBuildingTappedEvent"/>.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BuildingView : MonoBehaviour, IVillageTappable
    {
        private const int BaseSortingOrder = 10;

        public string BuildingId { get; private set; }

        private BuildingDefinition _definition;
        private SpriteRenderer _foundation;
        private SpriteRenderer _body;
        private SpriteRenderer _roof;
        private SpriteRenderer _door;
        private TextMeshPro _label;
        private Transform _visual;
        private BoxCollider2D _collider;
        private Coroutine _pop;

        public void Initialize(BuildingDefinition definition)
        {
            _definition = definition;
            BuildingId = definition.Id;
            name = $"Building_{definition.Id}";
            transform.position = definition.PlotPosition;

            // Buildings lower on the map are drawn in front.
            int order = BaseSortingOrder + Mathf.RoundToInt(-definition.PlotPosition.y * 4f);
            Vector2 size = definition.Footprint;

            _foundation = GeneratedSprites.CreateRenderer(transform, "Foundation", GeneratedSprites.Square,
                new Color(0.35f, 0.28f, 0.2f, 0.55f), order, new Vector2(0f, -size.y * 0.45f), new Vector2(size.x * 1.1f, size.y * 0.25f));

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _body = GeneratedSprites.CreateRenderer(_visual, "Body", GeneratedSprites.Square, definition.Color, order + 1,
                new Vector2(0f, -size.y * 0.1f), new Vector2(size.x, size.y * 0.7f));
            _roof = GeneratedSprites.CreateRenderer(_visual, "Roof", GeneratedSprites.Triangle, definition.RoofColor, order + 2,
                new Vector2(0f, size.y * 0.42f), new Vector2(size.x * 1.2f, size.y * 0.5f));
            _door = GeneratedSprites.CreateRenderer(_visual, "Door", GeneratedSprites.Square, new Color(0.2f, 0.12f, 0.08f, 1f), order + 3,
                new Vector2(0f, -size.y * 0.3f), new Vector2(size.x * 0.18f, size.y * 0.3f));

            _label = new GameObject("Label").AddComponent<TextMeshPro>();
            _label.transform.SetParent(transform, false);
            _label.transform.localPosition = new Vector3(0f, size.y * 0.95f, 0f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 3.2f;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = new Color32(0, 0, 0, 200);
            _label.rectTransform.sizeDelta = new Vector2(size.x * 2.2f, 2f);
            _label.sortingOrder = 200;

            _collider = GetComponent<BoxCollider2D>();
            _collider.size = new Vector2(size.x * 1.1f, size.y * 1.2f);
            _collider.offset = new Vector2(0f, size.y * 0.1f);

            Refresh();
        }

        private void OnEnable() => EventBus<BuildingUpgradedEvent>.Subscribe(OnBuildingUpgraded);
        private void OnDisable() => EventBus<BuildingUpgradedEvent>.Unsubscribe(OnBuildingUpgraded);

        public void Refresh()
        {
            if (_definition == null) return;
            int level = VillageService.GetLevel(_definition.Id);
            bool built = level > 0;

            _visual.gameObject.SetActive(built);
            _foundation.color = built ? new Color(0.35f, 0.28f, 0.2f, 0.55f) : new Color(1f, 1f, 1f, 0.25f);

            if (built)
            {
                // Grows from 75% to 125% of its footprint over its levels.
                float t = _definition.MaxLevel > 1 ? (level - 1f) / (_definition.MaxLevel - 1f) : 1f;
                float scale = Mathf.Lerp(0.75f, 1.25f, t);
                _visual.localScale = new Vector3(scale, scale, 1f);
                _label.text = $"{_definition.StageName(level)}\n<size=70%>Lv {level}</size>";
            }
            else
            {
                bool locked = VillageService.CastleLevel < _definition.RequiredCastleLevel;
                _label.text = locked
                    ? $"{_definition.NameOrId}\n<size=70%>Castle Lv {_definition.RequiredCastleLevel}</size>"
                    : $"{_definition.NameOrId}\n<size=70%>Tap to build</size>";
            }
        }

        public void OnTapped() => EventBus<VillageBuildingTappedEvent>.Raise(new VillageBuildingTappedEvent(BuildingId));

        private void OnBuildingUpgraded(BuildingUpgradedEvent evt)
        {
            // The Castle gates every other plot's "locked" label, so refresh on any upgrade.
            Refresh();
            if (evt.BuildingId != BuildingId) return;
            if (_pop != null) StopCoroutine(_pop);
            _pop = StartCoroutine(PopRoutine());
        }

        private IEnumerator PopRoutine()
        {
            Vector3 baseScale = _visual.localScale;
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f + Mathf.Sin(t / duration * Mathf.PI) * 0.25f;
                _visual.localScale = baseScale * k;
                yield return null;
            }
            _visual.localScale = baseScale;
            _pop = null;
        }
    }
}
