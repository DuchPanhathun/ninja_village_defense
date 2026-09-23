using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Builds a working pet in the battle scene from a <see cref="PetDefinition"/>: instantiates the
    /// definition's prefab — or, when there is none, a placeholder GameObject with a tinted generated
    /// sprite — then makes sure it has a <see cref="PetController"/> and the ability component for
    /// its <see cref="PetAbilityType"/>, and feeds both the level/gear-scaled stats. Shared by the
    /// active pet (Pet system) and the Beast Ninja's wolves (Hero system).
    /// </summary>
    public static class PetFactory
    {
        public const int PlaceholderSortingOrder = 20;

        /// <param name="powerScale">Multiplier on the pet's power (companions spawned by a hero are weaker than a real pet).</param>
        public static PetController Spawn(PetDefinition definition, int level, PetEquipmentBonus gear, float powerScale,
            Transform owner, PlayerStats ownerStats, LayerMask enemyMask, Vector2 position)
        {
            if (definition == null) return null;

            GameObject go = definition.Prefab != null
                ? Object.Instantiate(definition.Prefab, position, Quaternion.identity)
                : CreatePlaceholder(definition, position);

            return Configure(go, definition, level, gear, powerScale, owner, ownerStats, enemyMask);
        }

        /// <summary>Configures an already spawned pet object (e.g. one the RunBootstrapper instantiated from a prefab).</summary>
        public static PetController Configure(GameObject go, PetDefinition definition, int level, PetEquipmentBonus gear,
            float powerScale, Transform owner, PlayerStats ownerStats, LayerMask enemyMask)
        {
            if (go == null || definition == null) return null;

            var stats = definition.GetStats(level, gear);
            stats.Power *= Mathf.Max(0f, powerScale);

            if (!go.TryGetComponent<PetController>(out var controller))
                controller = go.AddComponent<PetController>();
            controller.Initialize(owner, stats.MoveSpeed, definition.FollowDistance);

            if (!go.TryGetComponent<PetAbility>(out var ability))
                ability = AddAbility(go, definition.AbilityType);
            if (ability != null)
                ability.Initialize(definition, stats, ownerStats, enemyMask);

            return controller;
        }

        /// <summary>Adds the behaviour component for <paramref name="type"/>; null for <see cref="PetAbilityType.None"/>.</summary>
        public static PetAbility AddAbility(GameObject go, PetAbilityType type)
        {
            switch (type)
            {
                case PetAbilityType.XpCollector: return go.AddComponent<FoxXpCollectorAbility>();
                case PetAbilityType.Bite: return go.AddComponent<WolfBiteAbility>();
                case PetAbilityType.TreasureHunter: return go.AddComponent<HawkTreasureAbility>();
                case PetAbilityType.BananaThrow: return go.AddComponent<MonkeyBananaAbility>();
                case PetAbilityType.FireBreath: return go.AddComponent<DragonFireBreathAbility>();
                default: return null;
            }
        }

        /// <summary>
        /// Art-free pet: a root object plus a "Visual" child with the definition's icon, or a tinted
        /// generated disc when there's no icon either. The child is what bobs/flips.
        /// </summary>
        public static GameObject CreatePlaceholder(PetDefinition definition, Vector2 position)
        {
            var root = new GameObject($"Pet_{definition.Id}");
            root.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * Mathf.Max(0.1f, definition.PlaceholderScale);

            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            bool hasIcon = definition.Icon != null;
            spriteRenderer.sprite = hasIcon ? definition.Icon : PetPlaceholderSprites.Circle;
            spriteRenderer.color = hasIcon ? Color.white : definition.PlaceholderColor;
            spriteRenderer.sortingOrder = PlaceholderSortingOrder;

            if (!hasIcon)
            {
                // A small darker "snout" dot so facing direction reads on a plain disc.
                var snout = new GameObject("Snout");
                snout.transform.SetParent(visual.transform, false);
                snout.transform.localPosition = new Vector3(0.32f, 0.08f, 0f);
                snout.transform.localScale = Vector3.one * 0.35f;
                var snoutRenderer = snout.AddComponent<SpriteRenderer>();
                snoutRenderer.sprite = PetPlaceholderSprites.Circle;
                Color c = definition.PlaceholderColor;
                snoutRenderer.color = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 1f);
                snoutRenderer.sortingOrder = PlaceholderSortingOrder + 1;
            }

            return root;
        }
    }
}
