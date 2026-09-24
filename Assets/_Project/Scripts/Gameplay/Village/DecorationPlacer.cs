using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Placing (or moving) a decoration: a see-through copy follows the finger — drag it, or tap anywhere on
    /// the map to jump it there — snapping to the half-unit grid, with a ring under it that turns green where
    /// it fits and red where it doesn't. The HUD's placement bar calls <see cref="Confirm"/> / <see cref="Cancel"/>
    /// / <see cref="Flip"/>. Buying is only paid on Confirm, so cancelling is free.
    /// </summary>
    public class DecorationPlacer : MonoBehaviour
    {
        public static DecorationPlacer Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance.Definition != null;

        public DecorationDefinition Definition { get; private set; }
        /// <summary>The decoration being moved (0 when buying a new one).</summary>
        public int MovingUid { get; private set; }
        public PlacementBlocker Blocker { get; private set; }
        public bool Fits => Blocker == PlacementBlocker.None;

        private Ghost _ghost;
        private DecorationView _hidden;
        private bool _flip;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Starts placing <paramref name="definition"/> at <paramref name="start"/> (moving <paramref name="uid"/> when non-zero).</summary>
        public void Begin(DecorationDefinition definition, Vector2 start, int uid = 0, bool flip = false, DecorationView original = null)
        {
            End(raise: false);
            Definition = definition;
            MovingUid = uid;
            _flip = flip;
            _hidden = original;
            if (_hidden != null) _hidden.gameObject.SetActive(false);

            var go = new GameObject("PlacementGhost");
            go.transform.SetParent(transform, false);
            _ghost = go.AddComponent<Ghost>();
            _ghost.Build(this, definition, flip);
            MoveTo(start);
            EventBus<DecorationPlacementEvent>.Raise(new DecorationPlacementEvent(true));
        }

        public void MoveTo(Vector2 world)
        {
            if (_ghost == null) return;
            Vector2 feet = DecorationRules.Snap(world);
            Blocker = DecorationService.CheckPlacement(Definition, feet, MovingUid);
            _ghost.Show(feet, Fits);
            EventBus<DecorationPlacementEvent>.Raise(new DecorationPlacementEvent(true));
        }

        public void Flip()
        {
            if (_ghost == null) return;
            _flip = !_flip;
            _ghost.SetFlip(_flip);
        }

        /// <summary>Buys (or moves) the decoration at the ghost's spot. On failure the ghost stays for another try.</summary>
        public bool Confirm(out string error)
        {
            error = null;
            if (_ghost == null) return false;
            Vector2 feet = _ghost.transform.position;
            bool ok = MovingUid != 0
                ? DecorationService.TryMove(MovingUid, feet, _flip, out error)
                : DecorationService.TryBuy(Definition, feet, _flip, out error);
            if (ok)
            {
                _hidden = null; // the map rebuilds its decorations from the save
                End(raise: true);
            }
            return ok;
        }

        public void Cancel() => End(raise: true);

        private void End(bool raise)
        {
            if (_hidden != null) _hidden.gameObject.SetActive(true);
            _hidden = null;
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            bool wasActive = Definition != null;
            Definition = null;
            MovingUid = 0;
            if (raise && wasActive) EventBus<DecorationPlacementEvent>.Raise(new DecorationPlacementEvent(false));
        }

        public Price? PriceToPay => Definition != null && MovingUid == 0 ? Definition.Price : null;

        /// <summary>The draggable preview.</summary>
        private class Ghost : MonoBehaviour, IVillageDraggable
        {
            private DecorationPlacer _owner;
            private SpriteRenderer _sprite, _ring;
            private Vector2 _grabOffset;

            public bool CanDrag => true;

            public void Build(DecorationPlacer owner, DecorationDefinition definition, bool flip)
            {
                _owner = owner;
                _ring = GeneratedSprites.CreateRenderer(transform, "Ring", GeneratedSprites.Circle, Color.green, VillageSorting.Labels - 2,
                    Vector2.zero, new Vector2(definition.Radius * 2.2f, definition.Radius * 1.3f));
                _sprite = DecorationView.Draw(gameObject, definition, flip);
                _sprite.sortingOrder = VillageSorting.Labels - 1;
                var collider = gameObject.AddComponent<CircleCollider2D>();
                collider.radius = Mathf.Max(1.2f, definition.Radius + 0.6f); // easy to grab
                collider.offset = new Vector2(0f, definition.Sprite != null ? definition.Sprite.bounds.extents.y : 0f);
            }

            public void SetFlip(bool flip) => _sprite.flipX = flip;

            public void Show(Vector2 feet, bool fits)
            {
                transform.position = feet;
                _ring.color = fits ? new Color(0.3f, 1f, 0.4f, 0.45f) : new Color(1f, 0.25f, 0.25f, 0.5f);
                _sprite.color = fits ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.55f, 0.55f, 0.75f);
            }

            public void BeginDrag(Vector2 world) => _grabOffset = (Vector2)transform.position - world;
            public void Drag(Vector2 world) => _owner.MoveTo(world + _grabOffset);
            public void EndDrag() { }
        }
    }
}
