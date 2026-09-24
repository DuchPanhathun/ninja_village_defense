using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    public enum DecorationCategory
    {
        Nature,
        Village,
        Statues,
        Banners,
        Special,
    }

    /// <summary>
    /// Something the player can buy and place in their village: a tree, a well, a statue, a banner...
    /// <see cref="DescriptiveScriptableObject.Icon"/> is the first frame (for shop cards).
    /// </summary>
    [CreateAssetMenu(fileName = "Decoration", menuName = "Ninja Village/Village/Decoration")]
    public class DecorationDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private DecorationCategory category;
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [Tooltip("Animation speed when there is more than one frame (flags).")]
        [SerializeField] private float fps = 6f;
        [SerializeField] private CurrencyType currency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int price = 100;
        [Tooltip("Placement footprint radius in world units: decorations can't overlap each other or buildings.")]
        [SerializeField, Min(0.1f)] private float radius = 0.6f;
        [SerializeField] private int sortOrder;

        public DecorationCategory Category => category;
        public Sprite[] Frames => frames;
        public float Fps => fps;
        public Sprite Sprite => frames.Length > 0 ? frames[0] : Icon;
        public Price Price => new(currency, price);
        public float Radius => radius;
        public int SortOrder => sortOrder;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
