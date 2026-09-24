using System;
using System.Collections.Generic;
using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>
    /// Every <see cref="CharacterSpriteSet"/> by key, for characters chosen at runtime: heroes
    /// (<c>hero_assassin</c>), skins (<c>skin_assassin_crimson</c>, the skin's id) and pets
    /// (<c>pet_fox</c>). Enemies and bosses reference their set directly on their prefab.
    /// Lives in <c>Resources/Catalogs/CharacterSpriteLibrary</c>; built by the art hookup generator.
    /// </summary>
    public class CharacterSpriteLibrary : ScriptableObject
    {
        [SerializeField] private CharacterSpriteSet[] sets = Array.Empty<CharacterSpriteSet>();

        private Dictionary<string, CharacterSpriteSet> _byKey;

        public IReadOnlyList<CharacterSpriteSet> Sets => sets;

        public CharacterSpriteSet Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_byKey == null)
            {
                _byKey = new Dictionary<string, CharacterSpriteSet>(StringComparer.Ordinal);
                foreach (var set in sets)
                    if (set != null && !string.IsNullOrEmpty(set.Key)) _byKey[set.Key] = set;
            }
            return _byKey.TryGetValue(key, out var found) ? found : null;
        }

        /// <summary>The set for <paramref name="key"/>, or null when there's no library or no such set.</summary>
        public static CharacterSpriteSet Find(string key)
        {
            var library = CatalogLoader.Load<CharacterSpriteLibrary>();
            return library != null ? library.Get(key) : null;
        }

        /// <summary>Hero ids use underscores (<c>beast_ninja</c>); sprite keys don't (<c>hero_beastninja</c>).</summary>
        public static string HeroKey(string heroId) =>
            string.IsNullOrEmpty(heroId) ? null : "hero_" + heroId.Replace("_", string.Empty);

        /// <summary>Pet ids map to their sprite key; the dragon pet is drawn as the baby dragon.</summary>
        public static string PetKey(string petId) =>
            string.IsNullOrEmpty(petId) ? null : petId == "dragon" ? "pet_babydragon" : "pet_" + petId.Replace("_", string.Empty);

#if UNITY_EDITOR
        public void EditorSetSets(CharacterSpriteSet[] value)
        {
            sets = value;
            _byKey = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
