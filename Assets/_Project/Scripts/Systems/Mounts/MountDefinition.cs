using System;
using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Talents;
using UnityEngine;

namespace NinjaVillage.Systems.Mounts
{
    /// <summary>One stat a mount gives, in talent units (0.1 = +10%), at Lv 1.</summary>
    [Serializable]
    public struct MountBonus
    {
        public TalentStat Stat;
        public float Value;
    }

    /// <summary>
    /// A mount your hero rides into battle and around the village (side-view gallop frames from the pack). It gives
    /// stat bonuses that grow with its level. S-class mounts come only from Surprise Boxes.
    /// </summary>
    [CreateAssetMenu(fileName = "Mount", menuName = "Ninja Village/Mount")]
    public class MountDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField] private float fps = 8f;
        [Tooltip("Where the rider sits, in world units from the mount's centre, facing right (x < 0 = further back).")]
        [SerializeField] private Vector2 riderOffset = new(-0.15f, 0.675f);
        [SerializeField] private List<MountBonus> bonuses = new();
        [SerializeField] private CurrencyType unlockCurrency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int unlockCost = 1000;
        [SerializeField] private bool sClass;
        [SerializeField] private int sortOrder;

        public Sprite[] Frames => frames;
        public float Fps => fps;
        public Vector2 RiderOffset => riderOffset;
        public IReadOnlyList<MountBonus> Bonuses => bonuses;
        public Price UnlockPrice => new(unlockCurrency, unlockCost);
        public bool IsSpecial => sClass;
        public int SortOrder => sortOrder;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
