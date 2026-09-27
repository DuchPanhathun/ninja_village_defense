using NinjaVillage.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NinjaVillage.UI
{
    /// <summary>
    /// On-screen touch joystick — the primary movement input for one-handed mobile play.
    /// Attach to a UI Image (the background circle) with a child Image (the handle).
    /// Both must have "Raycast Target" enabled and sit under a full-screen invisible
    /// touch-catcher panel so a touch anywhere in the movement zone works, not just
    /// directly on the graphic.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IMoveInputProvider, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 100f;
        [Tooltip("If true, the joystick jumps to wherever the player first touches instead of staying fixed.")]
        [SerializeField] private bool dynamicPosition = true;
        [SerializeField] private Canvas canvas;

        private Vector2 _input;
        private RectTransform _rootRect;

        private void Awake()
        {
            _rootRect = GetComponent<RectTransform>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            SetVisible(dynamicPosition == false);
        }

        public Vector2 GetMoveInput() => _input;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (dynamicPosition)
            {
                background.position = eventData.position;
                SetVisible(true);
            }
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 screenPoint = eventData.position;
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            Vector2 delta = screenPoint - (Vector2)background.position;
            Vector2 clamped = Vector2.ClampMagnitude(delta, handleRange * scale);

            handle.anchoredPosition = clamped / scale;
            _input = clamped / (handleRange * scale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            if (dynamicPosition)
                SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (background != null) background.gameObject.SetActive(visible);
        }
    }
}
