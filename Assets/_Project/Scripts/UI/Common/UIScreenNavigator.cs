using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Scene-local stack of <see cref="UIScreen"/>s: <see cref="Show"/> pushes, <see cref="Back"/>
    /// pops (also bound to Escape / Android back). Created automatically by the first screen
    /// that needs it, together with its canvas. Also hosts a tiny toast for feedback like
    /// "Not enough coins".
    /// </summary>
    public class UIScreenNavigator : MonoBehaviour
    {
        private static UIScreenNavigator _instance;

        public static bool HasInstance => _instance != null;

        public static UIScreenNavigator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var canvas = UIBuilder.CreateCanvas("[UI Screens]", 10);
                    _instance = canvas.gameObject.AddComponent<UIScreenNavigator>();
                }
                return _instance;
            }
        }

        public Transform CanvasTransform => transform;

        private readonly Dictionary<string, UIScreen> _screens = new();
        private readonly List<UIScreen> _stack = new();
        private TextMeshProUGUI _toastText;
        private float _toastHideAt;

        public UIScreen Current => _stack.Count > 0 ? _stack[^1] : null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void Register(UIScreen screen)
        {
            if (_screens.TryGetValue(screen.ScreenId, out var existing) && existing != screen)
                Debug.LogWarning($"UIScreenNavigator: duplicate screen id '{screen.ScreenId}'.", screen);
            _screens[screen.ScreenId] = screen;
        }

        public void Unregister(UIScreen screen)
        {
            if (_screens.TryGetValue(screen.ScreenId, out var existing) && existing == screen)
                _screens.Remove(screen.ScreenId);
            _stack.Remove(screen);
        }

        public bool TryGet<T>(out T screen) where T : UIScreen
        {
            foreach (var s in _screens.Values)
                if (s is T typed) { screen = typed; return true; }
            screen = null;
            return false;
        }

        /// <summary>Pushes the screen. A non-popup hides the screen beneath it.</summary>
        public void Show(string screenId)
        {
            if (!_screens.TryGetValue(screenId, out var screen))
            {
                Debug.LogWarning($"UIScreenNavigator: no screen '{screenId}' in this scene.");
                return;
            }

            if (Current == screen)
            {
                screen.Refresh();
                return;
            }

            _stack.Remove(screen);
            if (!screen.IsPopup && Current != null)
                Current.SetVisible(false);

            _stack.Add(screen);
            screen.SetVisible(true);
        }

        /// <summary>Pops the top screen and re-shows the one below. The bottom screen stays.</summary>
        public void Back()
        {
            if (_stack.Count <= 1) return;

            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            top.SetVisible(false);

            var below = Current;
            if (below != null)
            {
                if (!below.IsVisible) below.SetVisible(true);
                else below.Refresh();
            }
        }

        /// <summary>Closes everything above <paramref name="screenId"/> (or everything, then shows it).</summary>
        public void ShowRoot(string screenId)
        {
            while (_stack.Count > 0)
            {
                var top = _stack[^1];
                _stack.RemoveAt(_stack.Count - 1);
                top.SetVisible(false);
            }
            Show(screenId);
        }

        /// <summary>Short message at the bottom of the screen ("Not enough coins", "Purchased!").</summary>
        public void Toast(string message, float seconds = 2f)
        {
            if (_toastText == null)
            {
                var bg = UIBuilder.Image(transform, "Toast", new Color(0f, 0f, 0f, 0.8f));
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0.1f, 0.08f);
                rt.anchorMax = new Vector2(0.9f, 0.14f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                bg.raycastTarget = false;
                _toastText = UIBuilder.Text(rt, "", UITheme.BodySize, TextAlignmentOptions.Center);
                UIBuilder.Stretch(_toastText.rectTransform, 8f);
            }

            _toastText.text = message;
            _toastText.transform.parent.gameObject.SetActive(true);
            _toastText.transform.parent.SetAsLastSibling();
            _toastHideAt = Time.unscaledTime + seconds;
        }

        private void Update()
        {
            if (_toastText != null && _toastText.transform.parent.gameObject.activeSelf && Time.unscaledTime >= _toastHideAt)
                _toastText.transform.parent.gameObject.SetActive(false);

            // Escape on desktop, the system back button on Android.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _stack.Count > 1)
            {
                Sfx.Play(AudioCueIds.UiBack);
                Back();
            }
        }
    }
}
