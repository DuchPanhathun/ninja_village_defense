using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Runtime entry point for village progression: reads building levels from the save, answers
    /// "can I upgrade this and why not", performs upgrades (spend → level up → save → report), and
    /// turns levels into effect values other systems consume (Forge weapon-level cap, Castle equipment
    /// slots, Market offers per day, Shrine rank cap, Dojo attack bonus).
    ///
    /// Static because it is pure save + catalog logic with no scene presence; the village map and
    /// every building screen share it.
    /// </summary>
    public static class VillageService
    {
        /// <summary>Used only if the BuildingCatalog asset is missing, so the Dojo still matches goal.text (Lv1 +2% … Lv20 +80%).</summary>
        public const int DojoFallbackMaxLevel = 20;
        public const float DojoFallbackFirst = 0.02f;
        public const float DojoFallbackMax = 0.80f;

        private static SaveData _ensuredFor;

        public static BuildingCatalog Catalog => CatalogCache<BuildingCatalog>.Get();

        /// <summary>The village save section, with defaults (Castle Lv1) applied once per loaded save.</summary>
        public static VillageSaveData Data
        {
            get
            {
                var save = SaveService.Data;
                if (!ReferenceEquals(_ensuredFor, save))
                {
                    _ensuredFor = save;
                    if (EnsureDefaults(save.Village, Catalog))
                        SaveService.MarkDirty();
                }
                return save.Village;
            }
        }

        /// <summary>Fresh-save state: the Castle (starting hut) exists at Lv1, plus any definition with a start level. Returns true if anything changed.</summary>
        public static bool EnsureDefaults(VillageSaveData data, BuildingCatalog catalog = null)
        {
            if (data == null) return false;
            bool changed = false;
            data.Buildings ??= new System.Collections.Generic.List<IdLevelEntry>();
            data.Blessings ??= new System.Collections.Generic.List<IdLevelEntry>();
            data.MarketOffers ??= new System.Collections.Generic.List<MarketOfferState>();

            if (data.GetBuildingLevel(BuildingIds.Castle) < 1)
            {
                data.Buildings.SetLevel(BuildingIds.Castle, 1);
                changed = true;
            }

            if (catalog != null)
            {
                foreach (var def in catalog.All)
                {
                    if (def == null || string.IsNullOrEmpty(def.Id) || def.StartLevel <= 0) continue;
                    if (data.GetBuildingLevel(def.Id) >= def.StartLevel) continue;
                    data.Buildings.SetLevel(def.Id, def.StartLevel);
                    changed = true;
                }
            }
            return changed;
        }

        public static BuildingDefinition Get(string buildingId)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(buildingId) : null;
        }

        public static int GetLevel(string buildingId) => Data.GetBuildingLevel(buildingId);

        public static int CastleLevel => Mathf.Max(1, GetLevel(BuildingIds.Castle));

        public static int HighestWave => SaveService.Data.Profile.HighestWaveReached;

        public static int TotalBuildingLevels => Data.TotalBuildingLevels();

        /// <summary>The building's data-defined effect at its current level, or <paramref name="fallback"/> when the definition is missing.</summary>
        public static float GetEffect(string buildingId, float fallback = 0f)
        {
            var def = Get(buildingId);
            return def != null ? def.EffectAt(GetLevel(buildingId)) : fallback;
        }

        public static int LevelCap(BuildingDefinition def) => def != null ? def.LevelCap(CastleLevel) : 0;

        public static Price UpgradePrice(BuildingDefinition def) => def != null ? def.UpgradePrice(GetLevel(def.Id)) : default;

        public static UpgradeQuery BuildQuery(BuildingDefinition def)
        {
            int level = GetLevel(def.Id);
            var price = def.UpgradePrice(level);
            return new UpgradeQuery
            {
                CurrentLevel = level,
                MaxLevel = def.MaxLevel,
                IsCastle = def.IsCastle,
                CastleLevel = CastleLevel,
                RequiredCastleLevel = def.RequiredCastleLevel,
                LevelsPerCastleLevel = def.LevelsPerCastleLevel,
                HighestWave = HighestWave,
                RequiredWave = def.RequiredWaveFor(level),
                Cost = price,
                Balance = CurrencyService.Balance(price.Currency),
            };
        }

        public static UpgradeBlocker CheckUpgrade(BuildingDefinition def)
        {
            if (def == null) return UpgradeBlocker.NoDefinition;
            return VillageRules.Check(BuildQuery(def));
        }

        /// <summary>Spends the price and raises the level. Saves, reports <see cref="ProgressStatIds.BuildingUpgraded"/> and raises <see cref="BuildingUpgradedEvent"/>.</summary>
        public static bool TryUpgrade(BuildingDefinition def, out UpgradeBlocker blocker)
        {
            blocker = CheckUpgrade(def);
            if (blocker != UpgradeBlocker.None) return false;

            int level = GetLevel(def.Id);
            if (!CurrencyService.TrySpend(def.UpgradePrice(level), def.Id))
            {
                blocker = UpgradeBlocker.NotEnoughCurrency;
                return false;
            }

            int newLevel = level + 1;
            if (def.IsCastle) TreasuryService.Accrue(); // the castle sets the treasury's rate and size
            if (def.Id == BuildingIds.Mine) MineService.Accrue(); // ...and the mine's level its digging rate
            Data.Buildings.SetLevel(def.Id, newLevel);
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.BuildingUpgraded, 1, def.Id);
            EventBus<BuildingUpgradedEvent>.Raise(new BuildingUpgradedEvent(def.Id, newLevel));
            return true;
        }

        /// <summary>Player-facing reason for a blocker ("Requires Castle Lv 3").</summary>
        public static string DescribeBlocker(UpgradeBlocker blocker, BuildingDefinition def)
        {
            if (def == null) return "Unknown building";
            int level = GetLevel(def.Id);
            switch (blocker)
            {
                case UpgradeBlocker.None: return string.Empty;
                case UpgradeBlocker.MaxLevel: return "Max level reached";
                case UpgradeBlocker.Locked: return $"Requires Castle Lv {def.RequiredCastleLevel}";
                case UpgradeBlocker.CastleLevel:
                    return $"Upgrade the Castle to Lv {VillageRules.CastleLevelNeededFor(level + 1, def.LevelsPerCastleLevel, def.RequiredCastleLevel)}";
                case UpgradeBlocker.WaveRequirement:
                    return $"Reach wave {def.RequiredWaveFor(level)} in battle (best: {HighestWave})";
                case UpgradeBlocker.NotEnoughCurrency:
                    return def.UpgradePrice(level).Currency == CurrencyType.Coins ? "Not enough coins" : "Not enough gems";
                default: return "Unavailable";
            }
        }

        /// <summary>Dojo attack bonus + every Shrine blessing rank in <paramref name="save"/>.</summary>
        public static VillageBonuses ComputeBonuses(SaveData save)
        {
            var bonuses = new VillageBonuses();
            if (save == null || save.Village == null) return bonuses;
            var village = save.Village;

            int dojoLevel = village.GetBuildingLevel(BuildingIds.Dojo);
            var dojo = Get(BuildingIds.Dojo);
            bonuses.AttackDamage += dojo != null
                ? dojo.EffectAt(dojoLevel)
                : VillageRules.EffectAt(dojoLevel, DojoFallbackMaxLevel, DojoFallbackFirst, DojoFallbackMax);

            var blessings = ShrineService.Catalog;
            if (blessings != null)
            {
                foreach (var blessing in blessings.All)
                {
                    if (blessing == null) continue;
                    int rank = village.GetBlessingRank(blessing.Id);
                    if (rank > 0) bonuses.AddBlessing(blessing.Stat, blessing.ValueAt(rank));
                }
            }
            return bonuses;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _ensuredFor = null;
    }
}
