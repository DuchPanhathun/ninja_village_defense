using System.Collections.Generic;
using NinjaVillage.Core.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Gameplay.Vfx
{
    /// <summary>
    /// Lightweight, pooled, art-free visual effects (EPIC 23 "Visual effects"): each effect is one
    /// SpriteRenderer using a <see cref="GeneratedSprites"/> shape that scales, rotates and fades over a
    /// short lifetime. No ParticleSystem materials or Shader.Find, so nothing can render pink or be
    /// stripped from a mobile build. Call from anywhere: <c>Vfx.Explosion(pos, radius)</c>.
    /// </summary>
    public static class Vfx
    {
        public static readonly Color HitColor = new(1f, 0.95f, 0.8f, 1f);
        public static readonly Color CritColor = new(1f, 0.6f, 0.15f, 1f);
        public static readonly Color FireColor = new(1f, 0.45f, 0.1f, 1f);
        public static readonly Color LightningColor = new(0.65f, 0.85f, 1f, 1f);
        public static readonly Color GoldColor = new(1f, 0.85f, 0.3f, 1f);

        private static readonly Stack<VfxInstance> Free = new();

        public static void HitSpark(Vector2 position, bool critical) =>
            Spawn(GeneratedSprites.Glow, position, critical ? CritColor : HitColor, critical ? 0.9f : 0.55f, critical ? 1.5f : 1.1f, 0.14f);

        public static void DeathPuff(Vector2 position, Color color) =>
            Spawn(GeneratedSprites.Circle, position, new Color(color.r, color.g, color.b, 0.7f), 0.4f, 1.4f, 0.35f);

        /// <summary>Expanding ring to <paramref name="radius"/> plus a bright flash — explosions, shockwaves, area skills.</summary>
        public static void Explosion(Vector2 position, float radius, Color? color = null)
        {
            Color c = color ?? FireColor;
            float diameter = Mathf.Max(0.5f, radius * 2f);
            Spawn(GeneratedSprites.Ring, position, c, diameter * 0.2f, diameter, 0.35f);
            Spawn(GeneratedSprites.Glow, position, new Color(c.r, c.g, c.b, 0.8f), diameter * 0.5f, diameter * 0.9f, 0.2f);
        }

        /// <summary>A bolt from the sky onto <paramref name="position"/>.</summary>
        public static void Lightning(Vector2 position)
        {
            var bolt = Spawn(GeneratedSprites.Square, position + new Vector2(0f, 3f), LightningColor, 1f, 1f, 0.18f);
            bolt.SetScaleAxes(new Vector2(0.18f, 6f), new Vector2(0.05f, 6f));
            Spawn(GeneratedSprites.Glow, position, LightningColor, 0.8f, 2f, 0.25f);
        }

        /// <summary>Sword crescent facing <paramref name="direction"/> — Katana, Shadow Army.</summary>
        public static void Slash(Vector2 position, Vector2 direction, float radius, Color? color = null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var slash = Spawn(GeneratedSprites.Arc, position, color ?? HitColor, radius * 1.6f, radius * 2.1f, 0.16f);
            slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            slash.SetSpin(angle, angle + 40f);
        }

        /// <summary>Radial burst — level up, evolution unlock, chest open.</summary>
        public static void Burst(Vector2 position, Color color, float size = 3f, float duration = 0.5f)
        {
            Spawn(GeneratedSprites.Ring, position, color, size * 0.2f, size, duration);
            Spawn(GeneratedSprites.Glow, position, new Color(color.r, color.g, color.b, 0.6f), size * 0.4f, size * 0.8f, duration * 0.7f);
        }

        /// <summary>Plays one effect. Returned instance may be tweaked (rotation, axes) the same frame.</summary>
        public static VfxInstance Spawn(Sprite sprite, Vector2 position, Color color, float startSize, float endSize, float duration,
            int sortingOrder = 150)
        {
            VfxInstance instance = null;
            while (Free.Count > 0 && instance == null) instance = Free.Pop();
            if (instance == null)
            {
                var go = new GameObject("Vfx");
                instance = go.AddComponent<VfxInstance>();
            }
            instance.Play(sprite, position, color, startSize, endSize, duration, sortingOrder);
            return instance;
        }

        internal static void Return(VfxInstance instance)
        {
            instance.gameObject.SetActive(false);
            Free.Push(instance);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Free.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        // Effect objects live in the scene; forget them when it unloads.
        private static void OnSceneUnloaded(Scene scene) => Free.Clear();
    }
}
