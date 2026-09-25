using System;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>Front: facing the camera (menus such as the Equipment showcase); the rest face right.</summary>
    public enum CharacterAnim { Idle, Move, Attack, Hurt, Death, Front }

    /// <summary>
    /// The frames of one character's animations — idle, move (walk/run), attack, hurt, death — built by
    /// the art hookup generator from the imported sprites (e.g. <c>hero_assassin_run_0..3</c>).
    /// Sprites face right; the controllers flip the character for left. Played by <see cref="SpriteFrameAnimator"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterSprites", menuName = "Ninja Village/Character Sprite Set")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [SerializeField] private string key;
        [SerializeField] private Sprite[] idle = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] move = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attack = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] hurt = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] death = Array.Empty<Sprite>();
        [Tooltip("Facing the camera (idle), for menus. Empty when the art only exists side-on.")]
        [SerializeField] private Sprite[] front = Array.Empty<Sprite>();

        [Header("Frames per second")]
        [SerializeField] private float idleFps = 5f;
        [SerializeField] private float moveFps = 9f;
        [SerializeField] private float attackFps = 14f;
        [SerializeField] private float hurtFps = 12f;
        [SerializeField] private float deathFps = 8f;

        public string Key => key;

        /// <summary>First idle frame (or first move frame): what to show before anything plays.</summary>
        public Sprite DefaultSprite => idle.Length > 0 ? idle[0] : move.Length > 0 ? move[0] : null;

        public Sprite[] Frames(CharacterAnim anim) => anim switch
        {
            CharacterAnim.Idle => idle,
            CharacterAnim.Move => move,
            CharacterAnim.Attack => attack,
            CharacterAnim.Hurt => hurt,
            CharacterAnim.Death => death,
            CharacterAnim.Front => front,
            _ => idle,
        };

        public float Fps(CharacterAnim anim) => anim switch
        {
            CharacterAnim.Idle => idleFps,
            CharacterAnim.Move => moveFps,
            CharacterAnim.Attack => attackFps,
            CharacterAnim.Hurt => hurtFps,
            CharacterAnim.Death => deathFps,
            CharacterAnim.Front => idleFps,
            _ => idleFps,
        };

        public bool Has(CharacterAnim anim)
        {
            var frames = Frames(anim);
            return frames != null && frames.Length > 0;
        }
    }
}
