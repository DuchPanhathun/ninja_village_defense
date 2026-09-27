using System;
using System.Collections;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>The overlay behind <see cref="SimulatedAdsProvider"/>.</summary>
    public sealed class SimulatedAdOverlay : MonoBehaviour
    {
        private Action<bool> _onComplete;
        private TextMeshProUGUI _countdown;
        private Button _close;
        private bool _finished;
        private bool _done;

        public static void Show(string title, float seconds, bool rewarded, Action<bool> onComplete)
        {
            var canvas = UIBuilder.CreateCanvas("[Simulated Ad]", 500);
            var overlay = canvas.gameObject.AddComponent<SimulatedAdOverlay>();
            overlay._onComplete = onComplete;

            var bg = UIBuilder.Panel(canvas.transform, "Background", new Color(0.02f, 0.02f, 0.05f, 0.97f));
            var column = UIBuilder.Vertical(bg, "Column", 30f, 80, TextAnchor.MiddleCenter);
            UIBuilder.Stretch((RectTransform)column.transform);
            UIBuilder.Text(column.transform, "ADVERTISEMENT", UITheme.TitleSize, TextAlignmentOptions.Center, UITheme.Gold, FontStyles.Bold);
            UIBuilder.Text(column.transform, title + "\n(simulated — no ad network yet)", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
            overlay._countdown = UIBuilder.Text(column.transform, "", UITheme.HeaderSize, TextAlignmentOptions.Center);
            overlay._close = UIBuilder.Button(column.transform, rewarded ? "Skip (no reward)" : "Close", overlay.Close, UITheme.ButtonSecondary);
            overlay._close.gameObject.SetActive(rewarded);
            overlay.StartCoroutine(overlay.Run(seconds, rewarded));
        }

        private IEnumerator Run(float seconds, bool rewarded)
        {
            for (float left = seconds; left > 0f; left -= Time.unscaledDeltaTime)
            {
                _countdown.text = Mathf.CeilToInt(left).ToString();
                yield return null;
            }
            _finished = true;
            _countdown.text = rewarded ? "Reward earned!" : string.Empty;
            _close.gameObject.SetActive(true);
            UIBuilder.SetLabel(_close, rewarded ? "Collect reward" : "Close");
        }

        private void Close()
        {
            if (_done) return;
            _done = true;
            var callback = _onComplete;
            Destroy(gameObject);
            callback?.Invoke(_finished);
        }

        private void OnDestroy()
        {
            // Scene changed underneath the ad: report a skip so callers never hang.
            if (_done) return;
            _done = true;
            _onComplete?.Invoke(false);
        }
    }
}
