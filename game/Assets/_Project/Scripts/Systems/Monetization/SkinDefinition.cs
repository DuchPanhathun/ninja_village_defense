using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>
    /// A cosmetic hero skin (EPIC 21 "Cosmetic skins"): purely visual, never stronger. Bought with gems
    /// in the Shop, or earned (battle pass / events) when <see cref="SoldInShop"/> is false. Until real
    /// skin sprites exist, a skin is a color tint on the hero's sprite (<see cref="Tint"/>); an optional
    /// <see cref="Sprite"/> replaces the sprite entirely once art is available.
    /// </summary>
    [CreateAssetMenu(fileName = "Skin_New", menuName = "Ninja Village/Store/Skin")]
    public class SkinDefinition : DescriptiveScriptableObject
    {
        [Tooltip("Hero this skin is for (HeroDefinition id).")]
        [SerializeField] private string heroId;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private Sprite sprite;
        [SerializeField, Min(0)] private int gemPrice = 200;
        [SerializeField] private bool soldInShop = true;
        [SerializeField] private int sortOrder;

        public string HeroId => heroId;
        public Color Tint => tint;
        public Sprite Sprite => sprite;
        public int GemPrice => gemPrice;
        public bool SoldInShop => soldInShop;
        public int SortOrder => sortOrder;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
