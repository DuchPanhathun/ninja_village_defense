using System;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    public enum Weather
    {
        Clear,
        Rain,
        Snow,
    }

    /// <summary>
    /// Village atmosphere as pure functions (EPIC 24 Phase 7): the sky follows the phone's own clock — dawn, day,
    /// dusk, night — as a tint over the map, lanterns glow as it gets dark, and now and then it rains (snows in
    /// winter) for a few hours. Weather is decided per 3-hour block of the day, so everyone sees a steady forecast
    /// rather than flickering rolls.
    /// </summary>
    public static class AtmosphereRules
    {
        /// <summary>Tint over the map by hour of day (0..24): rgb = colour, a = strength. Linear between keys.</summary>
        private static readonly (float hour, Color tint)[] Sky =
        {
            (0f, new Color(0.04f, 0.07f, 0.24f, 0.46f)),
            (4.5f, new Color(0.04f, 0.07f, 0.24f, 0.46f)),
            (6f, new Color(0.95f, 0.5f, 0.45f, 0.2f)),    // dawn
            (8f, new Color(1f, 0.95f, 0.8f, 0f)),         // day
            (17f, new Color(1f, 0.95f, 0.8f, 0f)),
            (18.5f, new Color(0.98f, 0.45f, 0.18f, 0.24f)), // dusk
            (20f, new Color(0.04f, 0.07f, 0.24f, 0.46f)),   // night
            (24f, new Color(0.04f, 0.07f, 0.24f, 0.46f)),
        };

        public const float WeatherBlockHours = 3f;
        /// <summary>Share of 3-hour blocks with rain (or snow in winter).</summary>
        public const float WetChance = 0.18f;

        public static float HourOf(DateTime local) => local.Hour + local.Minute / 60f + local.Second / 3600f;

        public static Color SkyTint(DateTime local)
        {
            float hour = HourOf(local);
            for (int i = 1; i < Sky.Length; i++)
            {
                if (hour > Sky[i].hour) continue;
                var (h0, c0) = Sky[i - 1];
                var (h1, c1) = Sky[i];
                return Color.Lerp(c0, c1, Mathf.InverseLerp(h0, h1, hour));
            }
            return Sky[^1].tint;
        }

        /// <summary>0 in daylight .. 1 at night: how brightly lanterns glow and fireflies show.</summary>
        public static float Darkness(DateTime local)
        {
            var tint = SkyTint(local);
            float nightAlpha = Sky[0].tint.a;
            // Only the blue night tint counts as dark; the warm dawn / dusk tint is half-light at most.
            float blueness = Mathf.Clamp01((tint.b - tint.r) / 0.2f + 0.5f);
            return Mathf.Clamp01(tint.a / nightAlpha * Mathf.Lerp(0.5f, 1f, blueness));
        }

        public static bool IsNight(DateTime local) => Darkness(local) > 0.6f;

        /// <summary>The weather for the 3-hour block <paramref name="local"/> falls in: rain, snow in Dec-Feb, or clear.</summary>
        public static Weather WeatherAt(DateTime local)
        {
            int block = (int)(local.Hour / WeatherBlockHours);
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + local.Year;
                hash = hash * 31 + local.DayOfYear;
                hash = hash * 31 + block;
                double roll = new System.Random(hash).NextDouble();
                if (roll >= WetChance) return Weather.Clear;
            }
            return local.Month == 12 || local.Month <= 2 ? Weather.Snow : Weather.Rain;
        }
    }
}
