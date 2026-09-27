using System.Collections.Generic;
using System.Text;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;

namespace NinjaVillage.Systems.Mounts
{
    public enum MountResult
    {
        Success,
        Unknown,
        AlreadyOwned,
        NotOwned,
        SurpriseBoxOnly,
        MaxLevel,
        NotEnoughCurrency,
    }

    /// <summary>Raised when a mount is unlocked, ridden, dismounted or levelled.</summary>
    public readonly struct MountsChangedEvent : IGameEvent { }

    /// <summary>
    /// Mounts: buy one (coins or gems; S-class mounts only from Surprise Boxes), ride it — your hero sits on it in
    /// battle and in the village, and its bonuses apply to every run (<see cref="MountRunModifier"/>) — and level it
    /// up with coins. State in <c>SaveService.Data.Mounts</c>; rules in <see cref="MountRules"/>.
    /// </summary>
    public static class MountService
    {
        public static MountCatalog Catalog => CatalogLoader.Load<MountCatalog>();

        private static MountSaveData Data => SaveService.Data.Mounts;

        public static MountDefinition Get(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        /// <summary>Every mount, cheapest first, S-class last (a new list per call).</summary>
        public static List<MountDefinition> GetSorted()
        {
            var list = new List<MountDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var mount in catalog.All)
                if (mount != null) list.Add(mount);
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return list;
        }

        public static bool IsOwned(MountDefinition mount) => mount != null && Data.Owned.ContainsId(mount.Id);
        public static int Level(MountDefinition mount) => mount != null ? System.Math.Max(1, Data.Owned.GetLevel(mount.Id)) : 0;
        public static MountDefinition Active => Get(Data.ActiveMountId);
        public static bool IsActive(MountDefinition mount) => mount != null && Data.ActiveMountId == mount.Id && IsOwned(mount);

        public static MountResult CheckUnlock(MountDefinition mount)
        {
            if (mount == null) return MountResult.Unknown;
            if (IsOwned(mount)) return MountResult.AlreadyOwned;
            if (mount.IsSpecial) return MountResult.SurpriseBoxOnly;
            if (!CurrencyService.CanAfford(mount.UnlockPrice)) return MountResult.NotEnoughCurrency;
            return MountResult.Success;
        }

        /// <summary>Buys a mount and rides it straight away.</summary>
        public static MountResult TryUnlock(MountDefinition mount)
        {
            var check = CheckUnlock(mount);
            if (check != MountResult.Success) return check;
            if (!CurrencyService.TrySpend(mount.UnlockPrice, $"mount_{mount.Id}")) return MountResult.NotEnoughCurrency;
            Own(mount);
            Data.ActiveMountId = mount.Id;
            Changed();
            return MountResult.Success;
        }

        /// <summary>
        /// A mount from a Surprise Box: unlocked the first time (and ridden if you had none), a level up after that.
        /// Returns true when it was new.
        /// </summary>
        public static bool Grant(MountDefinition mount)
        {
            if (mount == null) return false;
            bool isNew = !IsOwned(mount);
            if (isNew)
            {
                Own(mount);
                if (Active == null) Data.ActiveMountId = mount.Id;
            }
            else if (Level(mount) < MountRules.MaxLevel)
            {
                Data.Owned.SetLevel(mount.Id, Level(mount) + 1);
            }
            Changed();
            return isNew;
        }

        private static void Own(MountDefinition mount)
        {
            Data.Owned.SetLevel(mount.Id, 1);
            Progress.Report(ProgressStatIds.MountUnlocked, 1, mount.Id);
        }

        public static MountResult Ride(MountDefinition mount)
        {
            if (!IsOwned(mount)) return MountResult.NotOwned;
            Data.ActiveMountId = mount.Id;
            Changed();
            return MountResult.Success;
        }

        public static void Dismount()
        {
            Data.ActiveMountId = null;
            Changed();
        }

        public static Price UpgradePrice(MountDefinition mount) => Price.Coins(MountRules.UpgradeCost(Level(mount)));

        public static MountResult CheckUpgrade(MountDefinition mount)
        {
            if (!IsOwned(mount)) return MountResult.NotOwned;
            if (Level(mount) >= MountRules.MaxLevel) return MountResult.MaxLevel;
            if (!CurrencyService.CanAfford(UpgradePrice(mount))) return MountResult.NotEnoughCurrency;
            return MountResult.Success;
        }

        public static MountResult TryUpgrade(MountDefinition mount)
        {
            var check = CheckUpgrade(mount);
            if (check != MountResult.Success) return check;
            if (!CurrencyService.TrySpend(UpgradePrice(mount), $"mount_{mount.Id}_up")) return MountResult.NotEnoughCurrency;
            Data.Owned.SetLevel(mount.Id, Level(mount) + 1);
            Changed();
            return MountResult.Success;
        }

        /// <summary>"+10% move speed · +8% attack damage" at a level.</summary>
        public static string DescribeBonuses(MountDefinition mount, int level, string separator = " · ")
        {
            if (mount == null) return string.Empty;
            var text = new StringBuilder();
            float scale = MountRules.LevelScale(level);
            foreach (var bonus in mount.Bonuses)
            {
                if (text.Length > 0) text.Append(separator);
                text.Append(TalentService.FormatValue(bonus.Stat, bonus.Value * scale));
            }
            return text.ToString();
        }

        public static string Describe(MountResult result, MountDefinition mount) => result switch
        {
            MountResult.AlreadyOwned => "You already own this mount.",
            MountResult.NotOwned => "Unlock this mount first.",
            MountResult.SurpriseBoxOnly => "S-class mount: only found in Surprise Boxes.",
            MountResult.MaxLevel => "Max level.",
            MountResult.NotEnoughCurrency => mount != null && mount.UnlockPrice.Currency == CurrencyType.Gems && !IsOwned(mount) ? "Not enough gems." : "Not enough coins.",
            MountResult.Unknown => "Unknown mount.",
            _ => string.Empty,
        };

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<MountsChangedEvent>.Raise(new MountsChangedEvent());
        }
    }
}
