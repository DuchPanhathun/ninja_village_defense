using System;
using NinjaVillage.Core;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Every economy knob that isn't owned by a specific definition asset (EPIC 15 "Reward balancing"):
    /// battle rewards on top of enemy drops and Forge prices (weapon grades are in GradeRules). One asset at
    /// <c>Resources/Catalogs/EconomyConfig.asset</c>; when it's missing the field defaults below are
    /// used, so the game is balanced even before content is generated.
    ///
    /// <para><b>Balance pass — rationale.</b> Target: early on, one meaningful upgrade per 1–2 runs;
    /// later, upgrades stay ~1–2 runs apart because income and prices grow together.</para>
    /// <list type="bullet">
    /// <item><b>Income per run.</b> Enemy coin drops: Bandit/Wolf 2, mid-tier 3–4, bosses 20–60
    /// (EnemyDefinition.coinReward). Wave clear bonus = <c>waveClearBaseCoins + waveClearCoinsPerWave × wave</c>
    /// (8, 11, 14, 17 … for waves 1–4). The four scripted waves (37 Bandits + Giant Oni) pay
    /// 74 + 30 + 50 ≈ <b>155 coins</b> for a full clear; a first-timer dying in wave 2–3 earns ~50–90.
    /// Endless waves add ~75+ per wave and scale with the Gold Bonus skill and Shrine's Prosperity blessing.</item>
    /// <item><b>Prices.</b> First purchases cost 60–150 coins (Kunai Lv2 60, Dojo 100, Forge 150), i.e.
    /// one per 1–2 early runs. Costs grow 28–65% per level (see each BuildingDefinition's CostCurve), a bit
    /// faster than typical income growth, and the Castle — the progression gate — also requires reaching
    /// deeper waves (2 × castle level), so coins alone can't rush the village.</item>
    /// <item><b>XP curve.</b> LevelSystem needs <c>10 × 1.15^(L−1)</c> XP per level (≈168 XP to reach Lv10).
    /// Bandits give 5 XP and the Giant Oni now 60 (it was a copy of the Bandit's 5), so a full clear of the
    /// scripted waves lands around Lv 11–12 ≈ 10 skill picks — enough to find 1–2 evolutions per run.</item>
    /// <item><b>Gems.</b> Premium currency stays scarce: <c>gemsPerBossKill</c> per boss plus Market trades
    /// (coins ↔ gems at ~80 coins per gem), so gem-priced items are aspirational, never required.</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Ninja Village/Economy/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Battle rewards (on top of enemy coin drops)")]
        [SerializeField] private int waveClearBaseCoins = 5;
        [SerializeField] private int waveClearCoinsPerWave = 3;
        [SerializeField] private int gemsPerBossKill = 2;
        [Tooltip("Paid when every scripted wave is cleared with endless mode off.")]
        [SerializeField] private int victoryBonusCoins = 150;

        [Header("Forge — weapon upgrades (level L → L+1 uses step L-1)")]
        [SerializeField] private CostCurve weaponUpgradeCost = new(CostCurveType.Exponential, CurrencyType.Coins, 60, 1.45f, 0, 5);
        [Tooltip("Cost multiplier per weapon rarity: Common, Rare, Epic, Legendary.")]
        [SerializeField] private float[] upgradeCostRarityMultiplier = { 1f, 1.5f, 2.25f, 3.5f };

        [Header("Forge — crafting weapons (unlock, then copies to merge; per rarity: Common, Rare, Epic, Legendary)")]
        [SerializeField] private int[] craftCoinCostByRarity = { 300, 900, 2500, 6000 };
        [SerializeField] private int[] craftForgeLevelByRarity = { 1, 3, 5, 7 };


        public int WaveClearBaseCoins => waveClearBaseCoins;
        public int WaveClearCoinsPerWave => waveClearCoinsPerWave;
        public int GemsPerBossKill => gemsPerBossKill;
        public int VictoryBonusCoins => victoryBonusCoins;
        public CostCurve WeaponUpgradeCost => weaponUpgradeCost;

        // ---- Lookups ----

        public int WaveClearCoins(int waveNumber) => WaveClearCoins(waveNumber, waveClearBaseCoins, waveClearCoinsPerWave);

        public Price WeaponUpgradePrice(Rarity rarity, int currentLevel)
        {
            var curve = weaponUpgradeCost ?? new CostCurve(CostCurveType.Exponential, CurrencyType.Coins, 60, 1.45f);
            int raw = curve.Evaluate(Math.Max(0, currentLevel - 1));
            return new Price(curve.Currency, ScaleCost(raw, PickFloat(upgradeCostRarityMultiplier, (int)rarity, 1f), 5));
        }

        public Price CraftPrice(Rarity rarity) => Price.Coins(PickInt(craftCoinCostByRarity, (int)rarity, 300));

        public int CraftForgeLevel(Rarity rarity) => PickInt(craftForgeLevelByRarity, (int)rarity, 1);


        // ---- Pure formulas (tested) ----

        public static int WaveClearCoins(int waveNumber, int baseCoins, int perWave) =>
            waveNumber <= 0 ? 0 : Math.Max(0, baseCoins + perWave * waveNumber);

        public static int ScaleCost(int cost, float multiplier, int roundTo) =>
            cost <= 0 ? 0 : CostCurve.Round(cost * (double)Math.Max(0f, multiplier), roundTo);

        private static int PickInt(int[] values, int index, int fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Math.Clamp(index, 0, values.Length - 1)];
        }

        private static float PickFloat(float[] values, int index, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Math.Clamp(index, 0, values.Length - 1)];
        }

        // ---- Access ----

        private static EconomyConfig _fallback;

        /// <summary>The generated asset, or an in-memory instance with the defaults above.</summary>
        public static EconomyConfig Current
        {
            get
            {
                var loaded = CatalogCache<EconomyConfig>.Get();
                if (loaded != null) return loaded;

                if (_fallback == null)
                {
                    _fallback = CreateInstance<EconomyConfig>();
                    _fallback.hideFlags = HideFlags.DontSave;
                }
                return _fallback;
            }
        }
    }
}
