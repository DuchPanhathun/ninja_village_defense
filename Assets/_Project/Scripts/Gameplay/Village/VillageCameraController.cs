using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Village "Camera navigation" (EPIC 11): one-finger / mouse drag pans with a little inertia,
    /// pinch / scroll wheel zooms, the view is clamped to the <see cref="VillageMap"/> bounds, and a
    /// short tap (no drag) on a building or villager calls its <see cref="IVillageTappable.OnTapped"/>.
    /// Gestures that start on UI are ignored so the HUD's buttons never pan the map.
    /// Put on the Village scene's orthographic camera.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class VillageCameraController : MonoBehaviour
    {
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 11f;
        [SerializeField] private float scrollZoomSpeed = 0.01f;
        [Tooltip("Screen pixels a press may move and still count as a tap.")]
        [SerializeField] private float tapThresholdPixels = 18f;
        [SerializeField, Range(0f, 20f)] private float inertiaDamping = 6f;

        private UnityEngine.Camera _camera;
        private bool _pressing;
        private bool _pressStartedOnUi;
        private bool _dragged;
        private Vector2 _pressStartScreen;
        private Vector2 _lastScreen;
        private Vector2 _velocity;
        private float _lastPinchDistance = -1f;

        private static readonly List<RaycastResult> UiHits = new();

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _camera.orthographic = true;
        }

        private void Update()
        {
            bool touchActive = Touchscreen.current != null && HandleTouches();
            if (!touchActive) HandleMouse();

            if (!_pressing && _velocity.sqrMagnitude > 0.0001f)
            {
                transform.position += (Vector3)(_velocity * Time.unscaledDeltaTime);
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, inertiaDamping * Time.unscaledDeltaTime);
            }
            ClampToBounds();
        }

        /// <summary>Returns true when touch input was active this frame.</summary>
        private bool HandleTouches()
        {
            var touches = Touchscreen.current.touches;
            int active = 0;
            Vector2 p0 = default, p1 = default;
            for (int i = 0; i < touches.Count && active < 2; i++)
            {
                if (!touches[i].press.isPressed) continue;
                if (active == 0) p0 = touches[i].position.ReadValue();
                else p1 = touches[i].position.ReadValue();
                active++;
            }

            if (active >= 2)
            {
                _dragged = true; // a pinch is never a tap
                float distance = Vector2.Distance(p0, p1);
                if (_lastPinchDistance > 0f && !_pressStartedOnUi)
                    Zoom(_lastPinchDistance / Mathf.Max(1f, distance), (p0 + p1) * 0.5f);
                _lastPinchDistance = distance;
                _lastScreen = (p0 + p1) * 0.5f;
                return true;
            }

            _lastPinchDistance = -1f;
            if (active == 1) { PointerHeld(p0); return true; }
            if (_pressing) { PointerReleased(_lastScreen); return true; }
            return false;
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUi(mouse.position.ReadValue()))
                Zoom(1f - scroll * scrollZoomSpeed, mouse.position.ReadValue());

            if (mouse.leftButton.isPressed) PointerHeld(mouse.position.ReadValue());
            else if (_pressing) PointerReleased(mouse.position.ReadValue());
        }

        private void PointerHeld(Vector2 screen)
        {
            if (!_pressing)
            {
                _pressing = true;
                _dragged = false;
                _pressStartScreen = screen;
                _lastScreen = screen;
                _velocity = Vector2.zero;
                _pressStartedOnUi = IsOverUi(screen);
                return;
            }
            if (_pressStartedOnUi) return;

            if (!_dragged && (screen - _pressStartScreen).sqrMagnitude > tapThresholdPixels * tapThresholdPixels)
                _dragged = true;

            if (_dragged)
            {
                Vector2 worldDelta = ScreenToWorld(_lastScreen) - ScreenToWorld(screen);
                transform.position += (Vector3)worldDelta;
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                _velocity = Vector2.Lerp(_velocity, worldDelta / dt, 0.5f);
            }
            _lastScreen = screen;
        }

        private void PointerReleased(Vector2 screen)
        {
            _pressing = false;
            if (_pressStartedOnUi) { _velocity = Vector2.zero; return; }
            if (!_dragged) { _velocity = Vector2.zero; TryTap(screen); }
        }

        private void TryTap(Vector2 screen)
        {
            var hit = Physics2D.OverlapPoint(ScreenToWorld(screen));
            if (hit == null) return;
            for (Transform t = hit.transform; t != null; t = t.parent)
            {
                if (t.TryGetComponent<IVillageTappable>(out var tappable))
                {
                    tappable.OnTapped();
                    return;
                }
            }
        }

        private void Zoom(float factor, Vector2 screenFocus)
        {
            Vector2 before = ScreenToWorld(screenFocus);
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize * factor, minZoom, maxZoom);
            Vector2 after = ScreenToWorld(screenFocus);
            transform.position += (Vector3)(before - after); // keep the point under the fingers fixed
        }

        private void ClampToBounds()
        {
            if (VillageMap.Instance == null) return;
            Rect b = VillageMap.Instance.Bounds;
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            Vector3 p = transform.position;
            p.x = b.width > halfW * 2f ? Mathf.Clamp(p.x, b.xMin + halfW, b.xMax - halfW) : b.center.x;
            p.y = b.height > halfH * 2f ? Mathf.Clamp(p.y, b.yMin + halfH, b.yMax - halfH) : b.center.y;
            transform.position = p;
        }

        private Vector2 ScreenToWorld(Vector2 screen) => _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));

        private static bool IsOverUi(Vector2 screen)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            var data = new PointerEventData(eventSystem) { position = screen };
            UiHits.Clear();
            eventSystem.RaycastAll(data, UiHits);
            return UiHits.Count > 0;
        }
    }
}
