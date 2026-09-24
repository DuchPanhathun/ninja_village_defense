using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Pixel art for the village map (Ninja Adventure pack), loaded from <c>Resources/Catalogs/VillageArt</c>
    /// and filled by the Village art generator: tiled ground and paths, a sprite per building (the Castle
    /// grows through stages), the forest ring, townsfolk and animals, and the displays' art. Anything
    /// missing falls back to the old generated shapes.
    /// </summary>
    [CreateAssetMenu(fileName = "VillageArt", menuName = "Ninja Village/Catalogs/Village Art")]
    public class VillageArt : ScriptableObject
    {
        [SerializeField] private Sprite ground;
        [SerializeField] private Sprite path;
        [SerializeField] private string[] buildingIds = System.Array.Empty<string>();
        [SerializeField] private Sprite[] buildingSprites = System.Array.Empty<Sprite>();
        [Tooltip("Castle look per stage (hut → house → manor → castle), from these castle levels.")]
        [SerializeField] private Sprite[] castleStages = System.Array.Empty<Sprite>();
        [SerializeField] private int[] castleStageLevels = System.Array.Empty<int>();
        [SerializeField] private Sprite dojoSign;
        [SerializeField] private Sprite[] flagFrames = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] forestTrees = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] forestUndergrowth = System.Array.Empty<Sprite>();
        [SerializeField] private string[] villagerKeys = System.Array.Empty<string>();
        [SerializeField] private string[] animalKeys = System.Array.Empty<string>();
        [SerializeField] private Sprite weaponRack;
        [SerializeField] private Sprite talentTree;
        [SerializeField] private Sprite pedestal;
        [SerializeField] private Sprite noticeBoard;
        [SerializeField] private Sprite farmSoil;
        [SerializeField] private Sprite farmSign;
        [Header("Houses & treasury")]
        [SerializeField] private string[] houseStyleIds = System.Array.Empty<string>();
        [SerializeField] private Sprite[] houseStyleSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite houseSign;
        [SerializeField] private Sprite treasuryChest;
        [SerializeField] private Sprite coin;
        [Header("Fishing pond & mine")]
        [SerializeField] private Sprite pondWater;
        [SerializeField] private Sprite pondDock;
        [SerializeField] private Sprite pondLily;
        [SerializeField] private Sprite pondBoat;
        [SerializeField] private Sprite pondNet;
        [SerializeField] private Sprite[] pondRipples = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] fish = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite mineCrane;
        [Tooltip("Iron, gold, mithril.")]
        [SerializeField] private Sprite[] bars = System.Array.Empty<Sprite>();

        public Sprite Ground => ground;
        public Sprite Path => path;
        public Sprite DojoSign => dojoSign;
        public Sprite[] FlagFrames => flagFrames;
        public Sprite[] ForestTrees => forestTrees;
        public Sprite[] ForestUndergrowth => forestUndergrowth;
        public string[] VillagerKeys => villagerKeys;
        public string[] AnimalKeys => animalKeys;
        public Sprite WeaponRack => weaponRack;
        public Sprite TalentTree => talentTree;
        public Sprite Pedestal => pedestal;
        public Sprite NoticeBoard => noticeBoard;
        public Sprite FarmSoil => farmSoil;
        public Sprite FarmSign => farmSign;
        public Sprite HouseSign => houseSign;
        public Sprite TreasuryChest => treasuryChest;
        public Sprite Coin => coin;
        public Sprite PondWater => pondWater;
        public Sprite PondDock => pondDock;
        public Sprite PondLily => pondLily;
        public Sprite PondBoat => pondBoat;
        public Sprite PondNet => pondNet;
        public Sprite[] PondRipples => pondRipples;
        public Sprite[] Fish => fish;
        public Sprite MineCrane => mineCrane;
        public Sprite[] Bars => bars;

        /// <summary>The picture for a house style id (<see cref="NinjaVillage.Systems.Village.HousingRules.Styles"/>).</summary>
        public Sprite HouseSprite(string styleId)
        {
            for (int i = 0; i < houseStyleIds.Length && i < houseStyleSprites.Length; i++)
                if (houseStyleIds[i] == styleId) return houseStyleSprites[i];
            return houseStyleSprites.Length > 0 ? houseStyleSprites[0] : null;
        }

        public Sprite BuildingSprite(string buildingId, int level)
        {
            if (buildingId == "castle" && castleStages.Length > 0)
            {
                int stage = 0;
                for (int i = 0; i < castleStages.Length && i < castleStageLevels.Length; i++)
                    if (level >= castleStageLevels[i]) stage = i;
                return castleStages[stage];
            }
            for (int i = 0; i < buildingIds.Length && i < buildingSprites.Length; i++)
                if (buildingIds[i] == buildingId) return buildingSprites[i];
            return null;
        }

        private static VillageArt _cached;

        public static VillageArt Load()
        {
            if (_cached == null) _cached = Resources.Load<VillageArt>("Catalogs/VillageArt");
            return _cached;
        }
    }
}
