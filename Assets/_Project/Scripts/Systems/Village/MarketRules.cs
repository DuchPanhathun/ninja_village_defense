using System.Collections.Generic;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>An offer as the daily roll sees it — just the data the roll depends on.</summary>
    public struct MarketCandidate
    {
        public string Id;
        public float Weight;
        public int RequiredMarketLevel;

        public MarketCandidate(string id, float weight, int requiredMarketLevel)
        {
            Id = id;
            Weight = weight;
            RequiredMarketLevel = requiredMarketLevel;
        }
    }

    /// <summary>
    /// The Market's daily stock roll as pure, deterministic functions: the same day + player + pool
    /// always yields the same offers (so reinstalling or reopening the app can't re-roll the shop),
    /// while different days rotate the stock. Weighted pick without replacement, gated by Market level.
    /// </summary>
    public static class MarketRules
    {
        public static int SeedFor(int dayIndex, string playerId) =>
            StableHash.Combine(dayIndex, StableHash.Fnv1a(playerId ?? string.Empty));

        /// <summary>Fills <paramref name="results"/> with up to <paramref name="count"/> distinct offer ids.</summary>
        public static void Roll(IReadOnlyList<MarketCandidate> pool, int marketLevel, int count, int seed, List<string> results)
        {
            results.Clear();
            if (pool == null || count <= 0) return;

            var eligible = new List<MarketCandidate>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                var candidate = pool[i];
                if (string.IsNullOrEmpty(candidate.Id) || candidate.Weight <= 0f) continue;
                if (marketLevel < candidate.RequiredMarketLevel) continue;
                if (ContainsId(eligible, candidate.Id)) continue;
                eligible.Add(candidate);
            }

            var rng = new SeededRandom(seed);
            while (results.Count < count && eligible.Count > 0)
            {
                float total = 0f;
                for (int i = 0; i < eligible.Count; i++) total += eligible[i].Weight;

                float roll = rng.NextFloat() * total;
                int pick = eligible.Count - 1;
                for (int i = 0; i < eligible.Count; i++)
                {
                    roll -= eligible[i].Weight;
                    if (roll < 0f)
                    {
                        pick = i;
                        break;
                    }
                }

                results.Add(eligible[pick].Id);
                eligible.RemoveAt(pick);
            }
        }

        /// <summary>
        /// Rolls a new stock when the day changed; tops the stock up (keeping what was already bought)
        /// when the Market was upgraded mid-day. Returns true if the saved stock changed.
        /// </summary>
        public static bool RefreshStock(VillageSaveData village, IReadOnlyList<MarketCandidate> pool, int today,
            int marketLevel, int count, int seed)
        {
            if (village == null) return false;
            village.MarketOffers ??= new List<MarketOfferState>();
            var rolled = new List<string>();

            if (village.MarketStockDay != today)
            {
                Roll(pool, marketLevel, count, seed, rolled);
                village.MarketOffers.Clear();
                foreach (var id in rolled)
                    village.MarketOffers.Add(new MarketOfferState { OfferId = id, Purchased = false });
                village.MarketStockDay = today;
                return true;
            }

            if (village.MarketOffers.Count >= count) return false;

            // Same day, bigger Market: roll a longer list with the same seed and append unseen ids.
            Roll(pool, marketLevel, count + village.MarketOffers.Count, seed, rolled);
            bool changed = false;
            foreach (var id in rolled)
            {
                if (village.MarketOffers.Count >= count) break;
                if (village.FindMarketOffer(id) != null) continue;
                village.MarketOffers.Add(new MarketOfferState { OfferId = id, Purchased = false });
                changed = true;
            }
            return changed;
        }

        private static bool ContainsId(List<MarketCandidate> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == id) return true;
            return false;
        }
    }
}
