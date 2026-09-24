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

        [Header("Ultimates")]
        [SerializeField] private Sprite[] shuriken = Array.Empty<Sprite>();     // Heavenly Storm (spin frames)
        [SerializeField] private Sprite[] dragon = Array.Empty<Sprite>();       // Dragon Slash (flying, faces right)
        [SerializeField] private Sprite[] bigSlash = Array.Empty<Sprite>();     // Dragon Slash trail
        [SerializeField] private Sprite kunai;                                  // Shadow clones' throw (fallback)

        public Sprite[] Hit => hit;
        public Sprite[] Smoke => smoke;
        public Sprite[] Explosion => explosion;
        public Sprite[] Thunder => thunder;
        public Sprite[] Slash => slash;
        public Sprite[] Ring => ring;
        public float Fps => fps;
        public Sprite[] Shuriken => shuriken;
        public Sprite[] Dragon => dragon;
        public Sprite[] BigSlash => bigSlash;
        public Sprite Kunai => kunai;
    }
}
