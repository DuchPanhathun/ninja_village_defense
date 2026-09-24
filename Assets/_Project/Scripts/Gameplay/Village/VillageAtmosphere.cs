using System;
using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>Raised when rain in your village watered crops (the HUD says so).</summary>
    public readonly struct RainWateredEvent : IGameEvent
    {
        public readonly int Crops;
        public RainWateredEvent(int crops) => Crops = crops;
    }

    /// <summary>
    /// The village's mood (EPIC 24 Phase 7), by the phone's own clock: a sky tint over the map (dawn pink, day clear,
    /// dusk orange, night blue), warm glows at lanterns, fire pits, the houses, the Kitchen and the Castle once it
    /// gets dark, fireflies at night, and rain — snow in winter — for a few hours now and then
    /// (<see cref="AtmosphereRules"/>). Rain in your own village waters the growing crops. Built by
    /// <see cref="VillageMap"/>; everything is drawn above the world and below the labels, following the camera.
    /// </summary>
    public class VillageAtmosphere : MonoBehaviour
    {
        /// <summary>Tests and screenshots can pin the clock; null = the phone's local time.</summary>
        public static Func<DateTime> OverrideLocalTime;
        public static DateTime LocalNow => OverrideLocalTime?.Invoke() ?? DateTime.Now;

        private const int OverlayOrder = VillageSorting.Labels - 60;
        private const int GlowOrder = OverlayOrder + 5;
        private const int WeatherOrder = OverlayOrder + 10;
        private const int Drops = 110;
        private const int Fireflies = 14;

        private static readonly HashSet<string> GlowingDecorations = new() { "lantern_post", "fire_pit", "camp_tent" };

        private UnityEngine.Camera _camera;
        private SpriteRenderer _overlay;
        private readonly Dictionary<Transform, SpriteRenderer> _glows = new();
        private readonly List<(SpriteRenderer renderer, Vector2 velocity, float phase)> _drops = new();
        private readonly List<(SpriteRenderer renderer, Vector2 target, float phase)> _flies = new();
        private Transform _root;
        private Weather _weather = (Weather)(-1);
        private float _darkness;
        private float _nextGlowScan, _nextRainCheck;

        public Weather CurrentWeather => _weather;
        public float Darkness => _darkness;
        public Color SkyTint => _overlay != null ? _overlay.color : Color.clear;
        public int GlowCount => _glows.Count;
        public IEnumerable<SpriteRenderer> Glows => _glows.Values;

        private void Start()
        {
            _camera = UnityEngine.Camera.main;
            _root = new GameObject("Atmosphere").transform;
            _root.SetParent(transform, false);
            _overlay = GeneratedSprites.CreateRenderer(_root, "Sky", GeneratedSprites.Square, Color.clear, OverlayOrder);

            for (int i = 0; i < Fireflies; i++)
            {
                var fly = GeneratedSprites.CreateRenderer(_root, "Firefly", GeneratedSprites.Glow, new Color(0.85f, 1f, 0.45f, 0f), GlowOrder + 1);
                fly.transform.localScale = Vector3.one * 0.35f;
                _flies.Add((fly, Vector2.zero, UnityEngine.Random.value * 10f));
            }
            Tick(force: true);
        }

        private void LateUpdate() => Tick(force: false);

        private void Tick(bool force)
        {
            if (_camera == null) _camera = UnityEngine.Camera.main;
            if (_camera == null) return;
            var now = LocalNow;
            Rect view = ViewRect();

            // Weather first: rain and snow grey the sky a little.
            var weather = AtmosphereRules.WeatherAt(now);
            if (weather != _weather) SetWeather(weather);
            var tint = AtmosphereRules.SkyTint(now);
            if (_weather != Weather.Clear)
            {
                var grey = _weather == Weather.Snow ? new Color(0.85f, 0.9f, 1f, 0.12f) : new Color(0.3f, 0.36f, 0.48f, 0.2f);
                tint = new Color(Mathf.Lerp(tint.r, grey.r, 0.5f), Mathf.Lerp(tint.g, grey.g, 0.5f), Mathf.Lerp(tint.b, grey.b, 0.5f), Mathf.Max(tint.a, grey.a));
            }
            _overlay.color = tint;
            _overlay.transform.position = new Vector3(view.center.x, view.center.y, 0f);
            _overlay.transform.localScale = new Vector3(view.width + 2f, view.height + 2f, 1f);
            _darkness = AtmosphereRules.Darkness(now);

            if (force || Time.unscaledTime >= _nextGlowScan)
            {
                _nextGlowScan = Time.unscaledTime + 2f;
                ScanGlows();
            }
            float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 7f);
            foreach (var glow in _glows.Values)
                if (glow != null) glow.color = new Color(1f, 0.78f, 0.4f, 0.75f * _darkness * flicker);

            UpdateFireflies(view);
            UpdateWeather(view);
            RainWatersCrops();
        }

        private Rect ViewRect()
        {
            float h = _camera.orthographicSize * 2f, w = h * _camera.aspect;
            Vector2 c = _camera.transform.position;
            return new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
        }

        // ------------------------------------------------------------------ glows

        /// <summary>Keeps a warm glow on every lit thing on the map: lantern decorations, built houses, the Kitchen and the Castle.</summary>
        private void ScanGlows()
        {
            var wanted = new List<(Transform target, Vector2 offset, float size)>();
            foreach (var deco in FindObjectsByType<DecorationView>(FindObjectsSortMode.None))
                if (deco.isActiveAndEnabled && GlowingDecorations.Contains(deco.DecorationId))
                    wanted.Add((deco.transform, new Vector2(0f, deco.Renderer != null && deco.Renderer.sprite != null ? deco.Renderer.sprite.bounds.size.y * 0.75f : 1f), 2.4f));
            foreach (var house in FindObjectsByType<HouseView>(FindObjectsSortMode.None))
            {
                var body = house.transform.Find("Body")?.GetComponent<SpriteRenderer>();
                if (body != null && body.enabled) wanted.Add((house.transform, new Vector2(0f, 0.7f), 2.6f));
            }
            var snapshot = VillageMap.Instance != null ? VillageMap.Instance.Snapshot : null;
            foreach (var building in FindObjectsByType<BuildingView>(FindObjectsSortMode.None))
                if ((building.BuildingId == BuildingIds.Castle || building.BuildingId == BuildingIds.Kitchen)
                    && snapshot != null && snapshot.BuildingLevel(building.BuildingId) > 0)
                    wanted.Add((building.transform, new Vector2(0f, 0.2f), 3.2f));

            var keep = new HashSet<Transform>();
            foreach (var (target, offset, size) in wanted)
            {
                keep.Add(target);
                if (!_glows.TryGetValue(target, out var glow) || glow == null)
                {
                    glow = GeneratedSprites.CreateRenderer(_root, "Glow", GeneratedSprites.Glow, Color.clear, GlowOrder);
                    _glows[target] = glow;
                }
                glow.transform.position = (Vector2)target.position + offset;
                glow.transform.localScale = Vector3.one * size;
            }
            var gone = new List<Transform>();
            foreach (var pair in _glows)
                if (pair.Key == null || !keep.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var key in gone)
            {
                if (_glows[key] != null) Destroy(_glows[key].gameObject);
                _glows.Remove(key);
            }
        }

        private void UpdateFireflies(Rect view)
        {
            for (int i = 0; i < _flies.Count; i++)
            {
                var (renderer, target, phase) = _flies[i];
                Vector2 pos = renderer.transform.position;
                if (!view.Contains(pos) || (target - pos).sqrMagnitude < 0.05f || target == Vector2.zero)
                {
                    target = new Vector2(UnityEngine.Random.Range(view.xMin, view.xMax), UnityEngine.Random.Range(view.yMin, view.yMax));
                    if (!view.Contains(pos)) renderer.transform.position = target;
                }
                renderer.transform.position = Vector2.MoveTowards(pos, target, 0.6f * Time.deltaTime);
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f + phase);
                renderer.color = new Color(0.85f, 1f, 0.45f, Mathf.Clamp01(_darkness * 1.4f - 0.4f) * blink * (_weather == Weather.Rain ? 0.3f : 1f));
                _flies[i] = (renderer, target, phase);
            }
        }

        // ------------------------------------------------------------------ rain & snow

        private void SetWeather(Weather weather)
        {
            _weather = weather;
            foreach (var (renderer, _, _) in _drops) if (renderer != null) Destroy(renderer.gameObject);
            _drops.Clear();
            if (weather == Weather.Clear || _camera == null) return;

            Rect view = ViewRect();
            bool snow = weather == Weather.Snow;
            for (int i = 0; i < Drops; i++)
            {
                var drop = GeneratedSprites.CreateRenderer(_root, snow ? "Snowflake" : "Raindrop", snow ? GeneratedSprites.Circle : GeneratedSprites.Square,
                    snow ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.75f, 0.85f, 1f, 0.55f), WeatherOrder);
                drop.transform.localScale = snow ? Vector3.one * UnityEngine.Random.Range(0.08f, 0.16f) : new Vector3(0.05f, 0.55f, 1f);
                if (!snow) drop.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
                drop.transform.position = new Vector2(UnityEngine.Random.Range(view.xMin, view.xMax), UnityEngine.Random.Range(view.yMin, view.yMax));
                var velocity = snow ? new Vector2(0f, -UnityEngine.Random.Range(0.8f, 1.6f)) : new Vector2(-4f, -15f) * UnityEngine.Random.Range(0.85f, 1.15f);
                _drops.Add((drop, velocity, UnityEngine.Random.value * 10f));
            }
        }

        private void UpdateWeather(Rect view)
        {
            bool snow = _weather == Weather.Snow;
            foreach (var (renderer, velocity, phase) in _drops)
            {
                Vector2 pos = renderer.transform.position;
                pos += velocity * Time.deltaTime;
                if (snow) pos.x += Mathf.Sin(Time.time * 1.5f + phase) * 0.5f * Time.deltaTime;
                // Wrap around the view so the camera can pan without running out of weather.
                if (pos.y < view.yMin - 1f) pos.y = view.yMax + UnityEngine.Random.Range(0f, 1f);
                if (pos.y > view.yMax + 2f) pos.y = view.yMin;
                if (pos.x < view.xMin - 1f) pos.x += view.width + 2f;
                if (pos.x > view.xMax + 1f) pos.x -= view.width + 2f;
                renderer.transform.position = pos;
            }
        }

        /// <summary>Rain in your own village waters every growing crop that isn't watered yet (checked every few seconds).</summary>
        private void RainWatersCrops()
        {
            if (_weather != Weather.Rain || VillageVisit.IsVisiting || Time.unscaledTime < _nextRainCheck) return;
            _nextRainCheck = Time.unscaledTime + 3f;
            int watered = FarmService.WaterAllByRain();
            if (watered > 0) EventBus<RainWateredEvent>.Raise(new RainWateredEvent(watered));
        }
    }
}
