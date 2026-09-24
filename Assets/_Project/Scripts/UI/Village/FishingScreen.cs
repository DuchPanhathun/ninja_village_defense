using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The fishing mini-game (EPIC 24 Phase 5): Cast uses one of the day's casts; after a moment something bites and
    /// a marker sweeps a bar — tap REEL IN while it's inside the green zone (right in the middle for a double catch).
    /// Rarer fish have a narrower zone and a faster marker; wait too long and it gets away. Opened by tapping the
    /// pond or the HUD's Fish button.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class FishingScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Fishing;
        protected override string Title => "Fishing";

        private enum State { Idle, Waiting, Bite, Result }

        /// <summary>Seconds the fish stays on the hook before it gets away.</summary>
        private const float BiteWindow = 5f;

        private TextMeshProUGUI _casts, _status, _legend;
        private Image _fishIcon;
        private RectTransform _bar, _zone, _perfect, _marker;
        private Button _cast, _reel;

        private State _state;
        private FishKind _fish;
        private float _biteAt, _biteStart, _zoneCenter, _zoneWidth, _speed, _resultUntil;
        private float _nextRefresh;

        /// <summary>Test hooks: a fish is on the line, and whether the marker is in the zone this frame.</summary>
        public bool IsBiting => _state == State.Bite;
        public bool MarkerInZone => IsBiting && PondMineRules.Judge(Marker, _zoneCenter, _zoneWidth) != CatchResult.Miss;

        private float Marker => PondMineRules.MarkerPosition(Time.unscaledTime - _biteStart, _speed);

        protected override void Build(RectTransform body)
        {
            var card = UIBuilder.Card(body, "Pond", 16f, 28);
            _casts = UIBuilder.Text(card.transform, "", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.Gold, FontStyles.Bold);
            _casts.richText = true;

            _fishIcon = UIBuilder.Image(card.transform, "Fish", Color.white, UIArt.Get("tool_fishing_rod"));
            _fishIcon.preserveAspect = true;
            UIBuilder.SetPreferredSize(_fishIcon, -1f, 220f);

            _status = UIBuilder.Text(card.transform, "", UITheme.HeaderSize, TextAlignmentOptions.Center, UITheme.Text, FontStyles.Bold);
            _status.richText = true;
            UIStyle.Chunky(_status, 0.2f);

            var bar = UIBuilder.Image(card.transform, "Bar", new Color(0.08f, 0.12f, 0.16f, 0.95f));
            UIBuilder.SetPreferredSize(bar, -1f, 84f);
            _bar = bar.rectTransform;
            _zone = UIBuilder.Image(_bar, "Zone", new Color(0.35f, 0.75f, 0.35f, 0.9f)).rectTransform;
            _perfect = UIBuilder.Image(_zone, "Perfect", new Color(0.65f, 1f, 0.45f, 1f)).rectTransform;
            _perfect.anchorMin = new Vector2(0.35f, 0f);
            _perfect.anchorMax = new Vector2(0.65f, 1f);
            _perfect.offsetMin = _perfect.offsetMax = Vector2.zero;
            _marker = UIBuilder.Image(_bar, "Marker", Color.white).rectTransform;
            _marker.sizeDelta = new Vector2(14f, 0f);

            _cast = UIBuilder.Button(card.transform, "Cast", Cast);
            _reel = UIBuilder.Button(card.transform, "REEL IN!", Reel, UITheme.Button);

            var help = UIBuilder.Card(body, "Help", 8f, 22);
            _legend = UIBuilder.Text(help.transform, "", UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            _legend.richText = true;
            SetState(State.Idle);
        }

        public override void Refresh()
        {
            int casts = FishingService.Casts;
            _casts.text = casts >= PondMineRules.MaxCasts
                ? $"Casts {casts}/{PondMineRules.MaxCasts}"
                : $"Casts {casts}/{PondMineRules.MaxCasts}  <color=#E8D8B8><size=80%>+1 in {GameClock.FormatCountdown(FishingService.UntilNextCast)}</size></color>";
            if (_state == State.Idle || _state == State.Result) UIBuilder.SetEnabled(_cast, casts > 0);

            int castle = VillageService.CastleLevel;
            var legend = new System.Text.StringBuilder("Tap <b>REEL IN!</b> while the marker is in the green (dead centre = two fish). Rarer fish are quicker.\n");
            foreach (var fish in PondMineRules.Fish)
            {
                bool open = castle >= fish.RequiredCastleLevel;
                legend.Append(open ? $"<color=#E8D8B8>{fish.Name}</color> ({fish.Rarity})" : $"<color=#8A8A95>{fish.Name} (Castle Lv {fish.RequiredCastleLevel})</color>");
                legend.Append("   ");
            }
            legend.Append("\nFish sell in the Storehouse or become Sushi in the Kitchen. A Golden Koi becomes a pond for your village!");
            _legend.text = legend.ToString();
        }

        private void SetState(State state)
        {
            _state = state;
            bool bite = state == State.Bite;
            _bar.gameObject.SetActive(bite);
            _reel.gameObject.SetActive(bite);
            _cast.gameObject.SetActive(!bite && state != State.Waiting);
            switch (state)
            {
                case State.Idle:
                    _status.text = "Cast your line!";
                    _fishIcon.sprite = UIArt.Get("tool_fishing_rod");
                    _fishIcon.color = Color.white;
                    break;
                case State.Waiting:
                    _status.text = "Waiting for a bite...";
                    break;
                case State.Bite:
                    _status.text = $"<color=#FFD24D>Something's biting!</color>\n<size=70%>{Hint(_fish.Rarity)}</size>";
                    _fishIcon.color = new Color(0.1f, 0.15f, 0.2f, 0.8f); // a shadow until it's landed
                    _fishIcon.sprite = FishIcon(_fish);
                    break;
            }
        }

        private static string Hint(FishRarity rarity) => rarity switch
        {
            FishRarity.Legendary => "It's HUGE... and very fast!",
            FishRarity.Rare => "A strong one — careful!",
            FishRarity.Uncommon => "It's pulling hard.",
            _ => "A little nibble.",
        };

        private static Sprite FishIcon(FishKind fish)
        {
            if (fish.GoodsId == null) return UIArt.Get("item_golden_koi");
            var goods = GoodsService.Get(fish.GoodsId);
            return goods != null && goods.Icon != null ? goods.Icon : UIArt.Get("item_fish");
        }

        private void Cast()
        {
            if (!FishingService.TryCast(out _fish))
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast($"No casts left. The next one is ready in {GameClock.FormatCountdown(FishingService.UntilNextCast)}.");
                return;
            }
            Sfx.Play(AudioCueIds.UiClick);
            _biteAt = Time.unscaledTime + Random.Range(0.8f, 2f);
            SetState(State.Waiting);
            Refresh();
        }

        private void Bite()
        {
            _zoneWidth = PondMineRules.ZoneWidth(_fish.Difficulty);
            _zoneCenter = Random.Range(0.1f + _zoneWidth * 0.5f, 0.9f - _zoneWidth * 0.5f);
            _speed = PondMineRules.MarkerSpeed(_fish.Difficulty);
            _biteStart = Time.unscaledTime;
            _zone.anchorMin = new Vector2(_zoneCenter - _zoneWidth * 0.5f, 0f);
            _zone.anchorMax = new Vector2(_zoneCenter + _zoneWidth * 0.5f, 1f);
            _zone.offsetMin = _zone.offsetMax = Vector2.zero;
            Sfx.Play(AudioCueIds.UiUpgrade);
            SetState(State.Bite);
        }

        private void Reel()
        {
            if (_state != State.Bite) return;
            var result = PondMineRules.Judge(Marker, _zoneCenter, _zoneWidth);
            Finish(result);
        }

        private void Finish(CatchResult result)
        {
            int amount = FishingService.Land(_fish, result);
            _fishIcon.color = amount > 0 ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            if (amount <= 0)
            {
                Sfx.Play(AudioCueIds.UiError);
                _status.text = $"<color=#FF8A7A>It got away!</color>\n<size=70%>That was a {_fish.Name}.</size>";
            }
            else if (_fish.GoodsId == null)
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                _status.text = "<color=#FFD24D>A GOLDEN KOI!</color>\n<size=70%>A koi pond is waiting in the Decorate shop's gifts.</size>";
            }
            else
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                _status.text = result == CatchResult.Perfect
                    ? $"<color=#9CFF8A>Perfect! 2 × {_fish.Name}!</color>"
                    : $"<color=#9CFF8A>Caught a {_fish.Name}!</color>";
            }
            _resultUntil = Time.unscaledTime + 2.2f;
            SetState(State.Result);
            _bar.gameObject.SetActive(false);
            Refresh();
        }

        private void Update()
        {
            if (Root == null || !Root.gameObject.activeInHierarchy) return;
            switch (_state)
            {
                case State.Waiting when Time.unscaledTime >= _biteAt:
                    Bite();
                    break;
                case State.Bite:
                    float marker = Marker;
                    _marker.anchorMin = new Vector2(marker, 0f);
                    _marker.anchorMax = new Vector2(marker, 1f);
                    _marker.anchoredPosition = Vector2.zero;
                    if (Time.unscaledTime - _biteStart > BiteWindow) Finish(CatchResult.Miss);
                    break;
                case State.Result when Time.unscaledTime >= _resultUntil:
                    SetState(State.Idle);
                    Refresh();
                    break;
            }
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1f;
                Refresh();
            }
        }

        protected override void OnHidden()
        {
            // Walking away with a fish on the line loses it (the cast was already used).
            if (_state == State.Waiting || _state == State.Bite) FishingService.Land(_fish, CatchResult.Miss);
            if (_state != State.Idle) SetState(State.Idle);
        }
    }
}
