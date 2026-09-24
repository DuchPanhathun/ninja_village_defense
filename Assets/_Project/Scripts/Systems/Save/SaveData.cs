using System;
using NinjaVillage.Systems.Economy;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Everything persisted between sessions. Each feature owns one section class
    /// (see <c>Sections/</c>) — keep every new field JsonUtility-friendly (no
    /// Dictionary; use arrays/lists of small serializable classes instead).
    ///
    /// Bump <see cref="CurrentVersion"/> and extend <see cref="Migrate"/> whenever a
    /// change needs old saves transformed (plain field additions don't — JsonUtility
    /// leaves missing fields at their initializer values).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        /// <summary>
        /// Deliberately defaults to 0, not CurrentVersion: a v1 file has no Version field, and an
        /// initializer would stamp it as current during deserialization and skip migration.
        /// <see cref="Migrate"/> sets it (every load path calls Migrate).
        /// </summary>
        public int Version;
        public long LastSavedUtcTicks;

        public CurrencyWallet Wallet = new();

        // v1 fields — kept so old saves still load; mirrored into Profile by Migrate().
        public int HighestWaveReached;
        public int TotalRunsCompleted;

        public ProfileSaveData Profile = new();
        public InventorySaveData Inventory = new();
        public HeroSaveData Heroes = new();
        public PetSaveData Pets = new();
        public VillageSaveData Village = new();
        public TalentSaveData Talents = new();
        public EvolutionSaveData Evolutions = new();
        public DailySaveData Daily = new();
        public SettingsSaveData Settings = new();
        public LiveOpsSaveData LiveOps = new();
        public StoreSaveData Store = new();
        public ChapterSaveData Chapters = new();

        /// <summary>
        /// Fills sections that are null (JsonUtility leaves a class field null only when
        /// the JSON explicitly contains null, but hand-edited or cloud-merged saves can)
        /// and upgrades older versions in place.
        /// </summary>
        public void Migrate()
        {
            Wallet ??= new CurrencyWallet();
            Profile ??= new ProfileSaveData();
            Inventory ??= new InventorySaveData();
            Heroes ??= new HeroSaveData();
            Pets ??= new PetSaveData();
            Village ??= new VillageSaveData();
            Talents ??= new TalentSaveData();
            Evolutions ??= new EvolutionSaveData();
            Daily ??= new DailySaveData();
            Settings ??= new SettingsSaveData();
            LiveOps ??= new LiveOpsSaveData();
            LiveOps.BattlePass ??= new BattlePassSaveData();
            Store ??= new StoreSaveData();
            Chapters ??= new ChapterSaveData();
            Chapters.BestWaves ??= new System.Collections.Generic.List<IdLevelEntry>();

            if (Version < 2)
            {
                // v1 only tracked these two numbers at the root.
                Profile.HighestWaveReached = Math.Max(Profile.HighestWaveReached, HighestWaveReached);
                Profile.TotalRuns = Math.Max(Profile.TotalRuns, TotalRunsCompleted);
            }

            Profile.EnsureInitialized();
            Version = CurrentVersion;
        }
    }
}
