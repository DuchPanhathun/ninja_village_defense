using NinjaVillage.Systems.Meta;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>
    /// At run start, dresses the player as the selected hero: finds <c>hero_&lt;id&gt;</c> in the
    /// <see cref="CharacterSpriteLibrary"/> and plays it with a <see cref="SpriteFrameAnimator"/>.
    /// Runs right after the hero's stats; the skin modifier (later) can swap the set again.
    /// </summary>
    public sealed class HeroVisualRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Hero + 1;

        public void Apply(RunStartContext context)
        {
            if (context.Player == null) return;
            var set = CharacterSpriteLibrary.Find(CharacterSpriteLibrary.HeroKey(context.HeroId));
            if (set == null) return;

            if (!context.Player.TryGetComponent<SpriteFrameAnimator>(out var animator))
                animator = context.Player.AddComponent<SpriteFrameAnimator>();
            animator.SetSpriteSet(set);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new HeroVisualRunModifier());
    }
}
