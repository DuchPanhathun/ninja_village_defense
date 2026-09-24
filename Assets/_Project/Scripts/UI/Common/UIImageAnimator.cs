using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Loops sprite frames on a UI Image (unscaled time, so it runs in paused menus) with an optional
    /// gentle bob — used for the hero showcase on the home screen.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIImageAnimator : MonoBehaviour
    {
        private Sprite[] _frames = System.Array.Empty<Sprite>();
        private float _fps = 6f;
        private float _bob;
        private float _time;
        private Image _image;
        private RectTransform _rt;
        private Vector2 _basePosition;

        public void Play(Sprite[] frames, float fps, float bobPixels = 0f)
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
                _rt = (RectTransform)transform;
                _basePosition = _rt.anchoredPosition;
            }
            _frames = frames ?? System.Array.Empty<Sprite>();
            _fps = fps;
            _bob = bobPixels;
            _time = 0f;
            if (_frames.Length > 0) _image.sprite = _frames[0];
        }

        private void Update()
        {
            if (_image == null) return;
            _time += Time.unscaledDeltaTime;
            if (_frames.Length > 1) _image.sprite = _frames[(int)(_time * _fps) % _frames.Length];
            if (_bob > 0f) _rt.anchoredPosition = _basePosition + new Vector2(0f, Mathf.Abs(Mathf.Sin(_time * 2.2f)) * _bob);
        }
    }
}
