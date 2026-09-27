using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The chapter's road at the top of the battle HUD: one node per wave along a track, boss waves marked
    /// with the boss's own picture (the final boss bigger), a gold marker on the current wave, the track
    /// filling as waves are cleared, and a line saying when the next boss comes. Built in code; the Battle
    /// scene only needs this component on a RectTransform in the HUD canvas. Endless runs just count waves.
    /// </summary>
    public class WaveRoadmapUI : MonoBehaviour
    {
        private const float NodeSize = 20f;
        private const float BossSize = 50f;
        private const float FinalBossSize = 64f;

        private readonly List<(RectTransform root, Image dot)> _nodes = new();
        private WaveManager _waves;
        private RectTransform _track, _marker;
        private Image _fill;
        private TextMeshProUGUI _title, _hint;
        private int _current, _cleared;
        private bool _built;

        private void OnEnable()
        {
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
            EventBus<WaveClearedEvent>.Subscribe(OnWaveCleared);
        }

        private void OnDisable()
        {
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
            EventBus<WaveClearedEvent>.Unsubscribe(OnWaveCleared);
        }

        private void Start()
        {
            _waves = FindAnyObjectByType<WaveManager>();
            Build();
            Refresh();
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            _current = evt.WaveNumber;
            if (_built) Refresh();
        }

        private void OnWaveCleared(WaveClearedEvent evt)
        {
            _cleared = Mathf.Max(_cleared, evt.WaveNumber);
            if (_built) Refresh();
        }

        private int Total => _waves != null && !_waves.Endless ? _waves.Waves.Count : 0;

        private void Build()
        {
            _built = true;
            var root = (RectTransform)transform;

            var panel = UIStyle.Sprite(root, "Panel", "panel_tint", new Color(0.12f, 0.09f, 0.07f));
            panel.color = new Color(0.12f, 0.09f, 0.07f, 0.72f);
            panel.raycastTarget = false;
            UIBuilder.Stretch(panel.rectTransform);

            _title = UIStyle.Label(root, "", 28f, Color.white, TextAlignmentOptions.Center, 0.25f);
            UIStyle.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(390f, 36f));

            _hint = UIStyle.Label(root, "", 22f, UIStyle.Cream, TextAlignmentOptions.Center, 0.25f);
            UIStyle.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(390f, 30f));

            int total = Total;
            if (total == 0) return; // endless: title only

            _track = UIBuilder.Rect(root, "Track");
            UIStyle.Place(_track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(root.rect.width - 60f, 12f));
            var trackBg = UIStyle.Sprite(_track, "Bg", "bar_hp_bg", new Color(0.1f, 0.1f, 0.1f));
            trackBg.raycastTarget = false;
            UIBuilder.Stretch(trackBg.rectTransform, -4f);
            _fill = UIBuilder.Image(_track, "Fill", UITheme.Gold);
            _fill.raycastTarget = false;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            UIBuilder.Stretch(_fill.rectTransform);

            var waves = _waves.Waves;
            for (int i = 0; i < total; i++)
            {
                var wave = waves[i];
                bool boss = wave != null && wave.IsBossWave && wave.BossDefinition != null;
                bool final = boss && i == total - 1;
                float size = final ? FinalBossSize : boss ? BossSize : NodeSize;

                var node = UIBuilder.Rect(_track, $"Wave{i + 1}");
                UIStyle.Place(node, new Vector2(NodeX(i, total), 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size);
                var dot = UIBuilder.Image(node, "Dot", Color.white, GeneratedSprites.Circle);
                dot.raycastTarget = false;
                UIBuilder.Stretch(dot.rectTransform);
                if (boss)
                {
                    var icon = UIBuilder.Image(node, "Boss", Color.white, UIIcons.Enemy(wave.BossDefinition));
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    UIBuilder.Stretch(icon.rectTransform, size * 0.08f);
                    icon.enabled = icon.sprite != null;
                }
                _nodes.Add((node, dot));
            }

            // Gold diamond above the current wave.
            var marker = UIBuilder.Image(_track, "Marker", UITheme.Gold, GeneratedSprites.Square);
            marker.raycastTarget = false;
            _marker = marker.rectTransform;
            UIStyle.Place(_marker, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(14f, 14f));
            _marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static float NodeX(int index, int total) => total <= 1 ? 0.5f : index / (float)(total - 1);

        private void Refresh()
        {
            var chapter = ChapterDirector.Current;
            int total = Total;
            string prefix = chapter != null ? $"CH {chapter.Number}  ·  " : "";
            _title.text = total > 0 ? $"{prefix}WAVE {Mathf.Max(1, _current)}/{total}" : $"{prefix}WAVE {Mathf.Max(1, _current)}";

            if (total == 0)
            {
                _hint.text = "Endless";
                return;
            }

            _fill.fillAmount = total <= 1 ? (_cleared >= total ? 1f : 0f) : Mathf.Clamp01((_cleared - 1f) / (total - 1f));
            var waves = _waves.Waves;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var (_, dot) = _nodes[i];
                bool boss = waves[i] != null && waves[i].IsBossWave && waves[i].BossDefinition != null;
                int number = i + 1;
                dot.color = number <= _cleared ? UITheme.Gold
                    : number == _current ? Color.white
                    : boss ? new Color(0.75f, 0.18f, 0.2f) : new Color(0.35f, 0.3f, 0.28f);
            }
            if (_current >= 1)
            {
                _marker.anchorMin = _marker.anchorMax = new Vector2(NodeX(_current - 1, total), 0.5f);
                var currentWave = waves[Mathf.Clamp(_current - 1, 0, total - 1)];
                _marker.anchoredPosition = new Vector2(0f, currentWave != null && currentWave.IsBossWave ? 40f : 24f);
            }
            _hint.text = NextBossHint(waves, total);
        }

        private string NextBossHint(IReadOnlyList<WaveDefinition> waves, int total)
        {
            if (_cleared >= total) return "<color=#FFD24D>Chapter clear!</color>";
            int from = Mathf.Max(1, _current);
            for (int number = from; number <= total; number++)
            {
                var wave = waves[number - 1];
                if (wave == null || !wave.IsBossWave || wave.BossDefinition == null) continue;
                string name = wave.BossDefinition.DisplayName;
                bool final = number == total;
                if (number == from) return $"<color=#FF6B5E>{(final ? "FINAL BOSS" : "BOSS")}: {name}</color>";
                int inWaves = number - from;
                return $"{(final ? "Final boss" : "Boss")} in {inWaves} wave{(inWaves == 1 ? "" : "s")}";
            }
            return "";
        }

        private void Update()
        {
            // Gentle bob on the marker so the eye finds it.
            if (_marker != null) _marker.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f));
        }
    }
}
