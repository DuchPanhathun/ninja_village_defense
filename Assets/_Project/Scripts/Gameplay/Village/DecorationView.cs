using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// A bought decoration standing in the village (animated when it has frames, e.g. flags). In your own
    /// village, tapping it raises <see cref="DecorationTappedEvent"/> so the HUD can offer Move / Sell.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class DecorationView : MonoBehaviour, IVillageTappable
    {
        public int Uid { get; private set; }
        public string DecorationId { get; private set; }
        public SpriteRenderer Renderer { get; private set; }

        public static DecorationView Create(Transform parent, DecorationDefinition definition, PlacedDecoration placed)
        {
            var go = new GameObject($"Decoration_{definition.Id}_{placed.Uid}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<DecorationView>();
            view.Uid = placed.Uid;
            view.DecorationId = definition.Id;
            view.Renderer = Draw(go, definition, placed.Flip);
            view.MoveTo(new Vector2(placed.X, placed.Y));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.radius = Mathf.Max(0.45f, definition.Radius);
            collider.offset = new Vector2(0f, definition.Sprite != null ? definition.Sprite.bounds.extents.y * 0.6f : 0f);
            return view;
        }

        /// <summary>Adds the renderer (and animation) for <paramref name="definition"/>: feet at the object's origin.</summary>
        public static SpriteRenderer Draw(GameObject go, DecorationDefinition definition, bool flip)
        {
            var renderer = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(go.transform, false);
            renderer.sprite = definition.Sprite;
            renderer.flipX = flip;
            if (definition.Sprite != null) renderer.transform.localPosition = new Vector3(0f, definition.Sprite.bounds.extents.y, 0f);
            if (definition.Frames.Length > 1) renderer.gameObject.AddComponent<SpriteLoop>().SetFrames(definition.Frames, definition.Fps);
            return renderer;
        }

        /// <summary>Positions are the decoration's feet (where it touches the ground).</summary>
        public void MoveTo(Vector2 feet)
        {
            transform.position = feet;
            Renderer.sortingOrder = VillageSorting.Order(feet.y);
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<DecorationTappedEvent>.Raise(new DecorationTappedEvent(Uid));
        }
    }
}
