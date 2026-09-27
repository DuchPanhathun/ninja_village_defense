using UnityEngine;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Keeps this RectTransform inside <see cref="Screen.safeArea"/> so nothing sits under a camera hole,
    /// notch or rounded corner. Put it on a full-screen container; backgrounds go outside it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void OnEnable() => Apply();
        private void Update() => Apply();

        private void Apply()
        {
            var safe = Screen.safeArea;
            if (safe == _applied && _screen.x == Screen.width && _screen.y == Screen.height) return;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
