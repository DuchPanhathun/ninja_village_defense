using System;
using UnityEngine;

namespace NinjaVillage.Gameplay.Vfx
{
    /// <summary>
    /// Pixel-art frames for the <see cref="Vfx"/> effects (hit spark, smoke puff, explosion, thunder,
    /// slash, level-up ring). Lives in <c>Resources/Catalogs/VfxArt</c>; filled by the art hookup
    /// generator from the Ninja Adventure FX strips. Any empty list falls back to the generated shapes.
    /// </summary>
    public class VfxArt : ScriptableObject
    {
        [SerializeField] private Sprite[] hit = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] smoke = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] explosion = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] thunder = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] slash = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] ring = Array.Empty<Sprite>();
        [SerializeField] private float fps = 20f;

        public Sprite[] Hit => hit;
        public Sprite[] Smoke => smoke;
        public Sprite[] Explosion => explosion;
        public Sprite[] Thunder => thunder;
        public Sprite[] Slash => slash;
        public Sprite[] Ring => ring;
        public float Fps => fps;
    }
}
